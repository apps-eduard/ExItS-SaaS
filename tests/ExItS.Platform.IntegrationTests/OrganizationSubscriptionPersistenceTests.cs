using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class OrganizationSubscriptionPersistenceTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private static async Task<(string productCode, PlanId planId, Domain.Catalog.PlanVersionId versionId, TrialDefinitionId trialId)>
        SeedTrialEligibleCatalogAsync(IServiceProvider provider, string productPrefix)
    {
        var candidate = Unique(productPrefix);
        var productCode = candidate[..Math.Min(30, candidate.Length)];
        var createProduct = provider.GetRequiredService<CreateProduct>();
        var createFeature = provider.GetRequiredService<CreateFeatureDefinition>();
        var createPlan = provider.GetRequiredService<CreatePlan>();
        var activatePlan = provider.GetRequiredService<ActivatePlan>();
        var createVersion = provider.GetRequiredService<CreateDraftPlanVersion>();
        var publish = provider.GetRequiredService<PublishExistingPlanVersion>();
        var createTrial = provider.GetRequiredService<CreateTrialDefinition>();

        await createProduct.ExecuteAsync(productCode, "POS").ConfigureAwait(false);
        await createFeature.ExecuteAsync(
            productCode, FeatureCode.CustomerCreditView, "View", FeatureValueType.Boolean).ConfigureAwait(false);

        var plan = (await createPlan.ExecuteAsync(productCode, "utang", "Utang").ConfigureAwait(false)).Value!;
        await activatePlan.ExecuteAsync(plan.Id).ConfigureAwait(false);

        var grants = new[] { FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true) };
        var version = (await createVersion
            .ExecuteAsync(plan.Id, 1, BillingPeriod.Monthly, true, grants, T0)
            .ConfigureAwait(false)).Value!;
        await publish.ExecuteAsync(plan.Id, 1).ConfigureAwait(false);

        var trial = (await createTrial
            .ExecuteAsync(productCode, "Trial", TimeSpan.FromDays(21), grants, Array.Empty<FeatureGrantSpec>())
            .ConfigureAwait(false)).Value!;

        return (productCode, plan.Id, version.Id, trial.Id);
    }

    [Fact]
    public async Task CreatePlatformOrganization_persists_and_reloads_and_enforces_unique_slug()
    {
        var slug = Unique("acme");
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString);
        var create = provider.GetRequiredService<CreatePlatformOrganization>();
        var organizations = provider.GetRequiredService<IPlatformOrganizationRepository>();

        var result = await create.ExecuteAsync("Acme Group", slug);
        Assert.True(result.IsSuccess);

        var reloaded = await organizations.GetByIdAsync(result.Value!.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(slug, reloaded!.Slug);
        Assert.Equal(OrganizationStatus.Active, reloaded.Status);

        var duplicate = await create.ExecuteAsync("Acme Group Two", slug);
        Assert.False(duplicate.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.SlugConflict, duplicate.ErrorCode);
    }

    [Fact]
    public async Task StartTrialSubscription_persists_reloads_and_computes_UTC_trial_end()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var (_, planId, versionId, trialId) = await SeedTrialEligibleCatalogAsync(provider, "trial-persist");

        var createOrg = provider.GetRequiredService<CreatePlatformOrganization>();
        var org = (await createOrg.ExecuteAsync("Trial Org", Unique("trial-org"))).Value!;

        var startTrial = provider.GetRequiredService<StartTrialSubscription>();
        var subscriptions = provider.GetRequiredService<ISubscriptionRepository>();

        var started = await startTrial.ExecuteAsync(org.Id, planId, versionId, trialId);
        Assert.True(started.IsSuccess);
        Assert.Equal(SubscriptionStatus.Trialing, started.Value!.Status);
        Assert.Equal(T0.AddDays(21), started.Value.TrialEndUtc);
        Assert.Equal(TimeSpan.Zero, started.Value.TrialEndUtc!.Value.Offset);

        var reloaded = await subscriptions.GetByIdAsync(started.Value.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(SubscriptionStatus.Trialing, reloaded!.Status);
        Assert.Equal(T0.AddDays(21), reloaded.TrialEndUtc);
        Assert.Equal(TimeSpan.Zero, reloaded.CreatedAtUtc.Offset);
    }

    [Fact]
    public async Task StartTrialSubscription_second_active_like_for_same_org_product_returns_409_conflict()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var (_, planId, versionId, trialId) = await SeedTrialEligibleCatalogAsync(provider, "trial-dup");

        var createOrg = provider.GetRequiredService<CreatePlatformOrganization>();
        var org = (await createOrg.ExecuteAsync("Dup Org", Unique("dup-org"))).Value!;

        var startTrial = provider.GetRequiredService<StartTrialSubscription>();
        var first = await startTrial.ExecuteAsync(org.Id, planId, versionId, trialId);
        Assert.True(first.IsSuccess);

        var second = await startTrial.ExecuteAsync(org.Id, planId, versionId, trialId);
        Assert.False(second.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ActiveSubscriptionConflict, second.ErrorCode);
    }

    [Fact]
    public async Task Cancelled_subscription_does_not_block_a_brand_new_trial_for_the_same_org_product()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var (_, planId, versionId, trialId) = await SeedTrialEligibleCatalogAsync(provider, "trial-history");

        var createOrg = provider.GetRequiredService<CreatePlatformOrganization>();
        var org = (await createOrg.ExecuteAsync("History Org", Unique("history-org"))).Value!;

        var startTrial = provider.GetRequiredService<StartTrialSubscription>();
        var cancel = provider.GetRequiredService<CancelSubscription>();

        var first = await startTrial.ExecuteAsync(org.Id, planId, versionId, trialId);
        Assert.True(first.IsSuccess);
        Assert.True((await cancel.ExecuteAsync(first.Value!.Id)).IsSuccess);

        var second = await startTrial.ExecuteAsync(org.Id, planId, versionId, trialId);
        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Value.Id, second.Value!.Id);
    }

    [Fact]
    public async Task Subscription_full_lifecycle_grace_pastdue_suspend_reactivate_persists_each_step()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var (_, planId, versionId, trialId) = await SeedTrialEligibleCatalogAsync(provider, "lifecycle");

        var createOrg = provider.GetRequiredService<CreatePlatformOrganization>();
        var org = (await createOrg.ExecuteAsync("Lifecycle Org", Unique("lifecycle-org"))).Value!;

        var startTrial = provider.GetRequiredService<StartTrialSubscription>();
        var activate = provider.GetRequiredService<ActivateSubscription>();
        var enterGrace = provider.GetRequiredService<EnterSubscriptionGracePeriod>();
        var markPastDue = provider.GetRequiredService<MarkSubscriptionPastDue>();
        var suspend = provider.GetRequiredService<SuspendSubscription>();
        var reactivate = provider.GetRequiredService<ReactivateSubscription>();
        var cancel = provider.GetRequiredService<CancelSubscription>();
        var subscriptions = provider.GetRequiredService<ISubscriptionRepository>();

        var sub = (await startTrial.ExecuteAsync(org.Id, planId, versionId, trialId)).Value!;

        var activated = await activate.ExecuteAsync(sub.Id, T0, T0.AddDays(30));
        Assert.True(activated.IsSuccess);
        Assert.Equal(SubscriptionStatus.Active, (await subscriptions.GetByIdAsync(sub.Id))!.Status);

        var grace = await enterGrace.ExecuteAsync(sub.Id, T0.AddDays(37));
        Assert.True(grace.IsSuccess);
        Assert.Equal(SubscriptionStatus.GracePeriod, (await subscriptions.GetByIdAsync(sub.Id))!.Status);

        var pastDue = await markPastDue.ExecuteAsync(sub.Id);
        Assert.True(pastDue.IsSuccess);
        var afterPastDue = await subscriptions.GetByIdAsync(sub.Id);
        Assert.Equal(SubscriptionStatus.PastDue, afterPastDue!.Status);
        Assert.NotNull(afterPastDue.PastDueAtUtc);

        var suspended = await suspend.ExecuteAsync(sub.Id);
        Assert.True(suspended.IsSuccess);
        Assert.Equal(SubscriptionStatus.Suspended, (await subscriptions.GetByIdAsync(sub.Id))!.Status);

        var reactivated = await reactivate.ExecuteAsync(sub.Id);
        Assert.True(reactivated.IsSuccess);
        var afterReactivate = await subscriptions.GetByIdAsync(sub.Id);
        Assert.Equal(SubscriptionStatus.Active, afterReactivate!.Status);
        Assert.Null(afterReactivate.SuspendedAtUtc);
        Assert.Null(afterReactivate.PastDueAtUtc);
        Assert.Null(afterReactivate.GracePeriodEndUtc);

        var cancelled = await cancel.ExecuteAsync(sub.Id);
        Assert.True(cancelled.IsSuccess);
        var terminal = await subscriptions.GetByIdAsync(sub.Id);
        Assert.Equal(SubscriptionStatus.Cancelled, terminal!.Status);
        Assert.NotNull(terminal.CancelledAtUtc);

        var reactivateTerminal = await reactivate.ExecuteAsync(sub.Id);
        Assert.False(reactivateTerminal.IsSuccess);
    }

    [Fact]
    public async Task Expire_subscription_persists_ExpiredAtUtc_and_is_terminal()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var (_, planId, versionId, trialId) = await SeedTrialEligibleCatalogAsync(provider, "expire");

        var createOrg = provider.GetRequiredService<CreatePlatformOrganization>();
        var org = (await createOrg.ExecuteAsync("Expire Org", Unique("expire-org"))).Value!;

        var startTrial = provider.GetRequiredService<StartTrialSubscription>();
        var expire = provider.GetRequiredService<ExpireSubscription>();
        var subscriptions = provider.GetRequiredService<ISubscriptionRepository>();

        var sub = (await startTrial.ExecuteAsync(org.Id, planId, versionId, trialId)).Value!;
        var expired = await expire.ExecuteAsync(sub.Id);
        Assert.True(expired.IsSuccess);

        var reloaded = await subscriptions.GetByIdAsync(sub.Id);
        Assert.Equal(SubscriptionStatus.Expired, reloaded!.Status);
        Assert.NotNull(reloaded.ExpiredAtUtc);

        var reactivate = provider.GetRequiredService<ReactivateSubscription>();
        Assert.False((await reactivate.ExecuteAsync(sub.Id)).IsSuccess);
    }

    [Fact]
    public async Task GetCurrentForOrganizationProduct_prefers_active_like_subscription()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var (productCode, planId, versionId, trialId) = await SeedTrialEligibleCatalogAsync(provider, "current");

        var createOrg = provider.GetRequiredService<CreatePlatformOrganization>();
        var org = (await createOrg.ExecuteAsync("Current Org", Unique("current-org"))).Value!;

        var startTrial = provider.GetRequiredService<StartTrialSubscription>();
        var cancel = provider.GetRequiredService<CancelSubscription>();
        var subscriptions = provider.GetRequiredService<ISubscriptionRepository>();

        var historical = (await startTrial.ExecuteAsync(org.Id, planId, versionId, trialId)).Value!;
        await cancel.ExecuteAsync(historical.Id);

        var current = (await startTrial.ExecuteAsync(org.Id, planId, versionId, trialId)).Value!;

        var resolved = await subscriptions.GetCurrentForOrganizationProductAsync(org.Id, ProductCode.Create(productCode));
        Assert.NotNull(resolved);
        Assert.Equal(current.Id, resolved!.Id);
    }

    [Fact]
    public async Task Concurrent_updates_to_the_same_subscription_surface_a_concurrency_conflict()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var (_, planId, versionId, trialId) = await SeedTrialEligibleCatalogAsync(provider, "xmin");

        var createOrg = provider.GetRequiredService<CreatePlatformOrganization>();
        var org = (await createOrg.ExecuteAsync("Xmin Org", Unique("xmin-org"))).Value!;

        var startTrial = provider.GetRequiredService<StartTrialSubscription>();
        var sub = (await startTrial.ExecuteAsync(org.Id, planId, versionId, trialId)).Value!;

        await using var scopeA = provider.CreateAsyncScope();
        await using var scopeB = provider.CreateAsyncScope();

        var subsA = scopeA.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
        var subsB = scopeB.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
        var uowA = scopeA.ServiceProvider.GetRequiredService<IPlatformUnitOfWork>();
        var uowB = scopeB.ServiceProvider.GetRequiredService<IPlatformUnitOfWork>();

        var subA = await subsA.GetByIdAsync(sub.Id);
        var subB = await subsB.GetByIdAsync(sub.Id);
        Assert.NotNull(subA);
        Assert.NotNull(subB);

        subA!.Cancel(T0.AddMinutes(1));
        subB!.Cancel(T0.AddMinutes(2));

        // Both repositories attach/track their own copy of the row (at the same original xmin)
        // before either unit of work commits, so the second SaveChanges races against a stale token.
        await subsA.UpdateAsync(subA);
        await subsB.UpdateAsync(subB);

        await uowA.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => uowB.SaveChangesAsync());
    }
}
