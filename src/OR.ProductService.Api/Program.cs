using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OR.ProductService.Api.Extensions;
using OR.ProductService.Api.Middleware;
using OR.ProductService.Application.Interfaces;
using OR.ProductService.Application.Services;
using OR.ProductService.Application.Settings;
using OR.ProductService.Application.Validators;
using OR.ProductService.Infrastructure.BackgroundServices;
using OR.ProductService.Infrastructure.Persistence;
using OR.ProductService.Infrastructure.Repositories;
using OR.Shared.Auth;
using Serilog;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.FluentValidation;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, config) =>
{
    config
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("ServiceName", "ProductService")
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{ServiceName}] {SourceContext} - {Message:lj}{NewLine}{Exception}");
});

// EF Core + PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseNpgsql(connectionString));

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
    });
builder.Services.AddAuthorization();

// Application services
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductAppService, ProductAppService>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductCommandValidator>();

// Processed events cleanup configuration and background service
builder.Services.Configure<ProcessedEventsCleanupSettings>(
    builder.Configuration.GetSection(ProcessedEventsCleanupSettings.SectionName));
builder.Services.AddHostedService<ProcessedEventCleanupService>();

// Wolverine
var rabbitUri = builder.Configuration["RabbitMQ:Uri"]!;
builder.Host.UseWolverine(opts =>
{
    opts.ServiceName = "ProductService";
    
    opts.PersistMessagesWithPostgresql(connectionString);

    opts.UseRabbitMq(new Uri(rabbitUri))
        .AutoProvision()
        .EnableWolverineControlQueues();

    opts.UseEntityFrameworkCoreTransactions();

    opts.ListenToRabbitQueue("product-inventory-added")
        .Sequential()
        .UseDurableInbox();

    opts.PublishMessage<OR.Shared.Events.ProductCreatedEvent>()
        .ToRabbitQueue("product-created")
        .UseDurableOutbox();

    // Product not found - straight to DLQ, no retries
    opts.OnException<OR.ProductService.Application.Exceptions.ProductNotFoundException>()
        .MoveToErrorQueue();

    // All other errors - retry then DLQ
    opts.OnException<Exception>()
        .RetryTimes(3)
        .Then.MoveToErrorQueue();

    opts.UseFluentValidation();

    opts.Policies.AddMiddleware(typeof(WolverineLoggingMiddleware));
});

// OpenTelemetry
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"]!;
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("ProductService"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddNpgsql()
            .AddSource("Wolverine")
            .AddOtlpExporter(opts =>
            {
                opts.Endpoint = new Uri(otlpEndpoint);
            });
    });

builder.Services.AddControllers();
builder.Services.AddSwaggerConfiguration();

var app = builder.Build();

// Apply pending migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    await db.Database.MigrateAsync();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();