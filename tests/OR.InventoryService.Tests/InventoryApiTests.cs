using Alba;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using JasperFx;
using JasperFx.CommandLine;
using Shouldly;
using Wolverine.Tracking;
using OR.Shared.Events;
using OR.InventoryService.Infrastructure.Persistence;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.PostgreSql;
using Xunit;

namespace OR.InventoryService.Tests;

using OR.InventoryService.Api.Models;
using OR.InventoryService.Domain.Entities;
using OR.Shared.Auth;

using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

public sealed class InfraFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; } =
        new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("inventory_test")
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
public class InventoryApiTests
{

    public InventoryApiTests()
    {
        JasperFxEnvironment.AutoStartHost = true;
    }

    [Fact]
    public async Task PostInventory_ValidRequest_Returns201AndPublishesEvent()
    {
        var productId = Guid.NewGuid();
        var quantity = 5;
        var writeToken = GetToken("write");
        using var host = await AlbaHost.For<Program>();

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        db.KnownProducts.Add(new KnownProduct(productId, DateTime.UtcNow));
        await db.SaveChangesAsync();

        var tracked = await host.ExecuteAndWaitAsync(async () =>
        {
            await host.Scenario(x =>
            {
                var request = new AddInventoryRequest(productId, quantity);
                x.Post.Json(request).ToUrl("/inventory");
                x.WithBearerToken(writeToken);
                x.StatusCodeShouldBe(201);
            });
        });

        var publishedMessage = tracked.FindSingleTrackedMessageOfType<ProductInventoryAddedEvent>();
        publishedMessage.ShouldNotBeNull();
        publishedMessage.ProductId.ShouldBeEquivalentTo(productId);
        publishedMessage.Quantity.ShouldBeEquivalentTo(quantity);

        using var nested = host.Services.CreateScope();
        var context = nested.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var item = await context.Inventories.FirstOrDefaultAsync(x => x.ProductId == productId);
        item.ShouldNotBeNull();
        item.ProductId.ShouldBeEquivalentTo(productId);
        item.Quantity.ShouldBeEquivalentTo(quantity);
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