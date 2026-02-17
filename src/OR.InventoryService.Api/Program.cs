using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OR.InventoryService.Api.Extensions;
using OR.InventoryService.Api.Middleware;
using OR.InventoryService.Application.Interfaces;
using OR.InventoryService.Application.Services;
using OR.InventoryService.Application.Validators;
using OR.InventoryService.Infrastructure.Persistence;
using OR.InventoryService.Infrastructure.Repositories;
using OR.InventoryService.Infrastructure.Clients;
using OR.Shared.Auth;
using OR.Shared.Events;
using Serilog;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.FluentValidation;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;
using Npgsql;
using Wolverine.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, config) =>
{
    config
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("ServiceName", "InventoryService")
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{ServiceName}] {SourceContext} - {Message:lj}{NewLine}{Exception}");
});

// EF Core + PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddDbContext<InventoryDbContext>(options =>
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
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IKnownProductRepository, KnownProductRepository>();
builder.Services.AddScoped<IInventoryAppService, InventoryAppService>();
builder.Services.AddValidatorsFromAssemblyContaining<AddInventoryCommandValidator>();

// ProductService HTTP client (fallback for race conditions)
var productServiceUrl = builder.Configuration["ProductService:BaseUrl"]!;
builder.Services.AddHttpClient<IProductApiClient, ProductApiClient>(client =>
{
    client.BaseAddress = new Uri(productServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(3);
})
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 1;
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(5);
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
    options.CircuitBreaker.MinimumThroughput = 100;
});

// Wolverine
var rabbitUri = builder.Configuration["RabbitMQ:Uri"]!;
builder.Host.UseWolverine(opts =>
{
    opts.ServiceName = "InventoryService";
    
    // Explicitly scan the Api assembly for handlers (important for WebApplicationFactory tests)
    opts.Discovery.IncludeAssembly(typeof(Program).Assembly);

    opts.PersistMessagesWithPostgresql(connectionString);

    opts.UseRabbitMq(new Uri(rabbitUri))
        .AutoProvision()
        .EnableWolverineControlQueues();

    opts.UseEntityFrameworkCoreTransactions();
    
    opts.PublishMessage<ProductInventoryAddedEvent>()
        .ToRabbitQueue("product-inventory-added")
        .UseDurableOutbox();

    opts.ListenToRabbitQueue("product-created")
        .UseDurableInbox();

    // Error handling with retries + DLQ
    opts.OnException<Exception>()
        .RetryTimes(3)
        .Then.MoveToErrorQueue();

    opts.UseFluentValidation();

    opts.Policies.AddMiddleware(typeof(WolverineLoggingMiddleware));
    opts.Policies.AutoApplyTransactions();
});

// OpenTelemetry
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"]!;
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("InventoryService"))
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
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    await db.Database.MigrateAsync();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();