using ExItS.PinoyBuyNowPayLater.Application;
using ExItS.PinoyBuyNowPayLater.Application.Customers;
using ExItS.PinoyBuyNowPayLater.Application.Financing;
using ExItS.PinoyBuyNowPayLater.Domain.Customers;
using ExItS.PinoyBuyNowPayLater.Domain.Financing;
using ExItS.PinoyBuyNowPayLater.Infrastructure.Persistence;
using ExItS.PinoyBuyNowPayLater.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace ExItS.PinoyBuyNowPayLater.UnitTests.Financing;

public sealed class BnplInstallmentPlanPersistenceTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable = true;

    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("ExItS_PinoyBuyNowPayLater_Plan_Test")
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
    public async Task Plan_items_round_trip_with_bnpl_local_fks_only()
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
        services.AddScoped<IBnplFinancingApplicationRepository, BnplFinancingApplicationRepository>();
        services.AddScoped<IBnplUnitOfWork, BnplUnitOfWork>();
        await using var provider = services.BuildServiceProvider();

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BnplDbContext>();
            await db.Database.MigrateAsync();

            var fks = db.Model.GetEntityTypes()
                .SelectMany(t => t.GetForeignKeys())
                .Select(fk => fk.PrincipalEntityType.ClrType.Name)
                .Distinct()
                .ToArray();
            Assert.All(fks, name => Assert.StartsWith("Bnpl", name, StringComparison.Ordinal));
            Assert.DoesNotContain(fks, n => n.Contains("POS", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(fks, n => n.Contains("Platform", StringComparison.OrdinalIgnoreCase));
        }

        var org = Guid.Parse("11111111-1111-4111-8111-111111111111");
        var branch = Guid.Parse("22222222-2222-4222-8222-222222222222");
        var actor = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
        var now = DateTimeOffset.Parse("2026-08-27T12:00:00Z");

        Guid applicationId;
        Guid offerId;
        Guid planId;

        await using (var scope = provider.CreateAsyncScope())
        {
            var customers = scope.ServiceProvider.GetRequiredService<IBnplCustomerRepository>();
            var apps = scope.ServiceProvider.GetRequiredService<IBnplFinancingApplicationRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IBnplUnitOfWork>();

            var customer = BnplCustomer.Create(org, "Buyer", now);
            await customers.AddAsync(customer);

            var app = BnplFinancingApplication.Create(org, branch, customer.Id.Value, actor, 60_000m, 10_000m, now);
            app.Submit(now);
            app.ApproveEligibility(actor, now);
            var offer = app.CreateOffer(actor, now);
            var plan = app.AttachOrReplaceInstallmentPlan(
                offer.Id.Value,
                BnplInstallmentPlanId.From(Guid.Parse("bbbbbbbb-1111-4111-8111-bbbbbbbbbbbb")),
                Enumerable.Range(1, 5)
                    .Select(i => new BnplInstallmentPlanItemDraft(
                        Guid.Parse($"{i:x8}-cccc-4ccc-8ccc-cccccccccccc"),
                        i,
                        10_000m,
                        DateOnly.Parse("2026-10-01").AddMonths(i - 1)))
                    .ToArray(),
                actor,
                now);
            await apps.AddAsync(app);
            await uow.SaveChangesAsync();
            applicationId = app.Id.Value;
            offerId = offer.Id.Value;
            planId = plan.Id.Value;
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var apps = scope.ServiceProvider.GetRequiredService<IBnplFinancingApplicationRepository>();
            var loaded = await apps.GetByIdAsync(org, BnplFinancingApplicationId.From(applicationId));
            Assert.NotNull(loaded);
            var plan = loaded!.GetInstallmentPlanForOffer(offerId);
            Assert.NotNull(plan);
            Assert.Equal(planId, plan!.Id.Value);
            Assert.Equal(50_000m, plan.TotalScheduledPrincipal);
            Assert.Equal(5, plan.Items.Count);
            Assert.Equal(DateOnly.Parse("2026-10-01"), plan.Items[0].DueDate);
            Assert.Equal(DateOnly.Parse("2027-02-01"), plan.Items[4].DueDate);
            Assert.False(plan.IsLocked);
            Assert.False(loaded.HasOutstandingDebt);
            Assert.False(loaded.HasInstallments);
            Assert.False(loaded.AreRepaymentsAllowed);
        }
    }
}
