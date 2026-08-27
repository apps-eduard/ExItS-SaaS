using ExItS.PinoyBuyNowPayLater.Application;
using ExItS.PinoyBuyNowPayLater.Application.Customers;
using ExItS.PinoyBuyNowPayLater.Domain.Customers;
using ExItS.PinoyBuyNowPayLater.Infrastructure.Persistence;
using ExItS.PinoyBuyNowPayLater.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace ExItS.PinoyBuyNowPayLater.UnitTests.Customers;

public sealed class BnplCustomerPersistenceTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable = true;

    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("ExItS_PinoyBuyNowPayLater_Test")
                .WithUsername("postgres")
                .WithPassword("bnpl_test_only")
                .Build();
            await _container.StartAsync();
        }
        catch (Exception)
        {
            _dockerAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    [Fact]
    public async Task Migration_round_trip_enforces_org_isolation_and_unique_links()
    {
        if (!_dockerAvailable || _container is null)
        {
            return;
        }

        var services = new ServiceCollection();
        services.AddBnplApplication();
        services.AddBnplCustomerUseCases();
        services.AddDbContext<BnplDbContext>(options =>
            options.UseNpgsql(_container.GetConnectionString()));
        services.AddScoped<IBnplCustomerRepository, BnplCustomerRepository>();
        services.AddScoped<IBnplUnitOfWork, BnplUnitOfWork>();
        await using var provider = services.BuildServiceProvider();

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BnplDbContext>();
            await db.Database.MigrateAsync();

            var entityTypes = db.Model.GetEntityTypes().Select(t => t.ClrType.Name).ToArray();
            Assert.Contains("BnplCustomerRecord", entityTypes);
            Assert.DoesNotContain(entityTypes, n => n.Contains("Financing", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(entityTypes, n => n.Contains("POSCustomer", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(entityTypes, n => n.Contains("PlatformUser", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(BnplDbContext.DatabaseLogicalName, "ExItS_PinoyBuyNowPayLater");
        }

        var orgA = Guid.Parse("11111111-1111-4111-8111-111111111111");
        var orgB = Guid.Parse("22222222-2222-4222-8222-222222222222");
        var customerId = Guid.Parse("33333333-3333-4333-8333-333333333333");

        await using (var scope = provider.CreateAsyncScope())
        {
            var create = scope.ServiceProvider.GetRequiredService<CreateBnplCustomer>();
            var created = await create.ExecuteAsync(
                orgA,
                "Maria Santos",
                customerId,
                mobile: "09171234567",
                linkedPersonalPublicUserId: "EX-9999-1111");
            Assert.True(created.IsSuccess, created.ErrorMessage);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var get = scope.ServiceProvider.GetRequiredService<GetBnplCustomer>();
            var found = await get.ExecuteAsync(orgA, customerId);
            Assert.True(found.IsSuccess);
            Assert.Equal("EX-9999-1111", found.Value!.LinkedPersonalPublicUserId);

            var missing = await get.ExecuteAsync(orgB, customerId);
            Assert.False(missing.IsSuccess);
            Assert.Equal(BnplCustomerErrorCodes.NotFound, missing.ErrorCode);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var create = scope.ServiceProvider.GetRequiredService<CreateBnplCustomer>();
            var conflict = await create.ExecuteAsync(
                orgA,
                "Other",
                Guid.NewGuid(),
                linkedPersonalPublicUserId: "EX-9999-1111");
            Assert.False(conflict.IsSuccess);
            Assert.Equal(BnplCustomerErrorCodes.PersonalLinkConflict, conflict.ErrorCode);

            var otherOrg = await create.ExecuteAsync(
                orgB,
                "Other Org",
                Guid.NewGuid(),
                linkedPersonalPublicUserId: "EX-9999-1111");
            Assert.True(otherOrg.IsSuccess, otherOrg.ErrorMessage);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var create = scope.ServiceProvider.GetRequiredService<CreateBnplCustomer>();
            var converge = await create.ExecuteAsync(
                orgA,
                "Maria Santos",
                customerId,
                mobile: "09171234567",
                linkedPersonalPublicUserId: "EX-9999-1111");
            Assert.True(converge.IsSuccess);
            Assert.Equal(customerId, converge.Value!.Id.Value);
        }
    }
}
