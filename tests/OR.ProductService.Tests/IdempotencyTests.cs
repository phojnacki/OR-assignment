using Alba;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using JasperFx;
using JasperFx.CommandLine;
using Shouldly;
using Wolverine;
using Wolverine.Tracking;
using OR.ProductService.Api.Models;
using OR.ProductService.Domain.Entities;
using OR.ProductService.Infrastructure.Persistence;
using OR.Shared.Events;
using OR.Shared.Auth;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace OR.ProductService.Tests;

public sealed class InfraFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; } =
        new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("product_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    public RabbitMqContainer Rabbit { get; } =
        new RabbitMqBuilder("rabbitmq:3-management-alpine")
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();

    public async Task InitializeAsync()
    {
        await Postgres.StartAsync();
        await Rabbit.StartAsync();

        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            Postgres.GetConnectionString()
        );

        Environment.SetEnvironmentVariable(
            "RabbitMQ__Uri",
            Rabbit.GetConnectionString()
        );
    }

    public async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
        Environment.SetEnvironmentVariable("RabbitMQ__Uri", null);

        await Rabbit.DisposeAsync().AsTask();
        await Postgres.DisposeAsync().AsTask();
    }
}

[CollectionDefinition("infra", DisableParallelization = true)]
public class InfraCollection : ICollectionFixture<InfraFixture> { }

[Collection("infra")]
public class ProductApiTests
{
    public ProductApiTests()
    {
        JasperFxEnvironment.AutoStartHost = true;
    }

    [Fact]
    public async Task PostProduct_ValidRequest_Returns201AndPublishesEvent()
    {
        var writeToken = GetToken("write");
        using var host = await AlbaHost.For<Program>();

        var tracked = await host.ExecuteAndWaitAsync(async () =>
        {
            await host.Scenario(x =>
            {
                var request = new CreateProductRequest("Test Product", "Test Description", 99.99m);
                x.Post.Json(request).ToUrl("/products");
                x.WithBearerToken(writeToken);
                x.StatusCodeShouldBe(201);
            });
        });

        var publishedMessage = tracked.FindSingleTrackedMessageOfType<ProductCreatedEvent>();
        publishedMessage.ShouldNotBeNull();
        publishedMessage.ProductId.ShouldNotBe(Guid.Empty);

        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

        var product = await context.Products.FirstOrDefaultAsync(x => x.Id == publishedMessage.ProductId);
        product.ShouldNotBeNull();
        product.Name.ShouldBe("Test Product");
        product.Description.ShouldBe("Test Description");
        product.Price.ShouldBe(99.99m);
        product.Amount.ShouldBe(0);
    }

    [Fact]
    public async Task SameEvent_ProcessedTwice_UpdatesAmountOnlyOnce()
    {
        using var host = await AlbaHost.For<Program>();

        // Arrange: Create a product first
        var productId = Guid.NewGuid();
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
            var product = Product.Create("Test Product", "Description", 10.00m);
            typeof(Product).GetProperty("Id")!.SetValue(product, productId);
            db.Products.Add(product);
            await db.SaveChangesAsync();
        }

        var eventId = Guid.NewGuid();
        var quantity = 5;

        var inventoryEvent = new ProductInventoryAddedEvent(eventId, productId, quantity, DateTime.UtcNow);

        // Act: Send the same event twice
        using (var scope = host.Services.CreateScope())
        {
            var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await messageBus.InvokeAsync(inventoryEvent);
            
            // Send it again with the same EventId
            await messageBus.InvokeAsync(inventoryEvent);
        }

        // Give time for processing
        await Task.Delay(500);

        // Assert: Amount should be 5 (not 10)
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
            var updatedProduct = await db.Products.AsNoTracking().FirstAsync(p => p.Id == productId);
            updatedProduct.Amount.ShouldBe(quantity);
            
            // Verify the event was only processed once
            var processedCount = await db.ProcessedEvents.CountAsync(e => e.EventId == eventId);
            processedCount.ShouldBe(1);
        }
    }

    [Fact]
    public async Task DifferentEvents_BothProcessed_UpdatesAmountTwice()
    {
        using var host = await AlbaHost.For<Program>();

        // Arrange: Create a product first
        var productId = Guid.NewGuid();
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
            var product = Product.Create("Another Product", "Description", 20.00m);
            typeof(Product).GetProperty("Id")!.SetValue(product, productId);
            db.Products.Add(product);
            await db.SaveChangesAsync();
        }

        var event1Id = Guid.NewGuid();
        var event2Id = Guid.NewGuid();

        var inventoryEvent1 = new ProductInventoryAddedEvent(event1Id, productId, 5, DateTime.UtcNow);

        var inventoryEvent2 = new ProductInventoryAddedEvent(event2Id, productId, 3, DateTime.UtcNow);

        // Act: Send two different events
        using (var scope = host.Services.CreateScope())
        {
            var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await messageBus.InvokeAsync(inventoryEvent1);
            await messageBus.InvokeAsync(inventoryEvent2);
        }

        // Give time for processing
        await Task.Delay(500);

        // Assert: Amount should be 8 (5 + 3)
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
            var updatedProduct = await db.Products.AsNoTracking().FirstAsync(p => p.Id == productId);
            updatedProduct.Amount.ShouldBe(8);
            
            // Verify both events were processed
            var processedCount = await db.ProcessedEvents.CountAsync(e => 
                e.EventId == event1Id || e.EventId == event2Id);
            processedCount.ShouldBe(2);
        }
    }

    private static string GetToken(params string[] roles)
    {
        var settings = new JwtSettings
        {
            Secret = "ThisIsADevelopmentSecretKeyThatIsLongEnoughForHmacSha256!!",
            Issuer = "OR.AuthService",
            Audience = "OR.Services",
            ExpirationMinutes = 60
        };
        return JwtTokenGenerator.GenerateToken(settings, "e2e-test-user", roles);
    }
}
