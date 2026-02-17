using Alba;
using JasperFx.CommandLine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using OR.InventoryService.Infrastructure.Persistence;
using OR.InventoryService.Api.Models;
using OR.ProductService.Api.Models;
using OR.Shared.Auth;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace OR.EndToEnd.Tests;

public sealed class E2EInfraFixture : IAsyncLifetime
{
    public PostgreSqlContainer ProductDb { get; } =
        new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("productdb_e2e")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    public PostgreSqlContainer InventoryDb { get; } =
        new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("inventorydb_e2e")
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
        await Task.WhenAll(
            ProductDb.StartAsync(),
            InventoryDb.StartAsync(),
            Rabbit.StartAsync());

        // Set environment variables for both services
        Environment.SetEnvironmentVariable("RabbitMQ__Uri", Rabbit.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("RabbitMQ__Uri", null);

        await Task.WhenAll(
            ProductDb.DisposeAsync().AsTask(),
            InventoryDb.DisposeAsync().AsTask(),
            Rabbit.DisposeAsync().AsTask());
    }
}

[CollectionDefinition("e2e-infra", DisableParallelization = true)]
public class E2EInfraCollection : ICollectionFixture<E2EInfraFixture> { }

[Collection("e2e-infra")]
public class EndToEndTests
{
    private readonly E2EInfraFixture _fixture;

    public EndToEndTests(E2EInfraFixture fixture)
    {
        _fixture = fixture;
        JasperFxEnvironment.AutoStartHost = true;
    }

    [Fact]
    public async Task FullFlow_AddInventory_UpdatesProductAmount()
    {
        var writeToken = GetToken("write");

        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            _fixture.ProductDb.GetConnectionString()
        );

        using var productHost = await AlbaHost.For<OR.ProductService.Api.IProductServiceMarker>();

        // Create a product in ProductService via API
        Guid productId = Guid.Empty;
        var createProductResult = await productHost.Scenario(x =>
        {
            var request = new CreateProductRequest("E2E Test Product", "Test Description", 29.99m);
            x.Post.Json(request).ToUrl("/products");
            x.WithBearerToken(writeToken);
            x.StatusCodeShouldBe(201);
        });

        // Extract the product ID from the response
        productId = createProductResult.ReadAsJson<ProductResponse>()!.Id;

        // InventoryService (needs separate connection string)
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            _fixture.InventoryDb.GetConnectionString()
        );

        using var inventoryHost = await AlbaHost.For<OR.InventoryService.Api.IInventoryServiceMarker>();

        // Wait for ProductCreatedEvent to propagate to InventoryService
        await Task.Delay(4000);

        // 2. Call POST /inventory in InventoryService
        await inventoryHost.Scenario(x =>
        {
            var request = new AddInventoryRequest(productId, 15);
            x.Post.Json(request).ToUrl("/inventory");
            x.WithBearerToken(writeToken);
            x.StatusCodeShouldBe(201);
        });

        // Verify the inventory was created
        using (var scope = inventoryHost.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var inventory = await db.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId);
            inventory.ShouldNotBeNull();
            inventory.Quantity.ShouldBe(15);
        }

        // Give time for ProductInventoryAddedEvent to propagate through RabbitMQ
        // and be processed by ProductService's handler
        await Task.Delay(10000);

        // Verify the product amount was updated in ProductService via API
        var readToken = GetToken("read");
        var getProductResult = await productHost.Scenario(x =>
        {
            x.Get.Url($"/products/{productId}");
            x.WithBearerToken(readToken);
            x.StatusCodeShouldBe(200);
        });

        var updatedProduct = getProductResult.ReadAsJson<ProductResponse>()!;
        updatedProduct.Amount.ShouldBe(15);
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
