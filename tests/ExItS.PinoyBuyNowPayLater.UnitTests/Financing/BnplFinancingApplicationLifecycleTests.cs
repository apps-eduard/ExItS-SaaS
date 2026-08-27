using ExItS.PinoyBuyNowPayLater.Application.Common;
using ExItS.PinoyBuyNowPayLater.Application.Customers;
using ExItS.PinoyBuyNowPayLater.Application.Financing;
using ExItS.PinoyBuyNowPayLater.Domain.Customers;
using ExItS.PinoyBuyNowPayLater.Domain.Financing;

namespace ExItS.PinoyBuyNowPayLater.UnitTests.Financing;

public sealed class BnplFinancingApplicationLifecycleTests
{
    private static readonly Guid Org = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid Branch = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid CustomerId = Guid.Parse("33333333-3333-4333-8333-333333333333");
    private static readonly Guid Actor = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    private static readonly Guid Approver = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");

    [Fact]
    public void Happy_path_reaches_approved_pending_sale_without_active_or_debt()
    {
        var app = CreateDraft();
        app.Submit(Now());
        Assert.Equal(BnplFinancingApplicationStatus.PendingEligibility, app.Status);

        app.ApproveEligibility(Approver, Now());
        var offer = app.CreateOffer(Approver, Now());
        Assert.Equal(BnplFinancingApplicationStatus.Offered, app.Status);

        app.AcceptOffer(offer.Id.Value, Actor, Now());
        Assert.Equal(BnplFinancingApplicationStatus.CustomerAccepted, app.Status);

        app.Approve(Approver, Now());
        Assert.Equal(BnplFinancingApplicationStatus.ApprovedPendingSale, app.Status);
        Assert.False(app.HasOutstandingDebt);
        Assert.False(app.HasInstallments);
        Assert.False(app.AreRepaymentsAllowed);
        Assert.NotNull(app.AcceptedOffer);
        Assert.True(app.AcceptedOffer!.IsAccepted);
    }

    [Fact]
    public void Active_is_prohibited()
    {
        var app = CreateDraft();
        var ex = Assert.Throws<BnplFinancingDomainException>(() => app.ActivateProhibited());
        Assert.Equal(BnplFinancingErrorCodes.ActiveProhibited, ex.ErrorCode);
        Assert.DoesNotContain(Enum.GetNames<BnplFinancingApplicationStatus>(), s => s.Equals("Active", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Accepted_offer_is_immutable_and_superseded_cannot_be_accepted()
    {
        var app = CreateDraft();
        app.Submit(Now());
        app.ApproveEligibility(Approver, Now());
        var first = app.CreateOffer(Approver, Now(), BnplFinancingOfferId.From(Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc")));
        var second = app.CreateOffer(Approver, Now(), BnplFinancingOfferId.From(Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd")));
        Assert.True(first.IsSuperseded);

        Assert.Throws<BnplFinancingDomainException>(() => app.AcceptOffer(first.Id.Value, Actor, Now()));
        app.AcceptOffer(second.Id.Value, Actor, Now());
        Assert.Throws<BnplFinancingDomainException>(() =>
            app.CreateOffer(Approver, Now()));
    }

    [Fact]
    public void Eligibility_decline_is_terminal_non_active()
    {
        var app = CreateDraft();
        app.Submit(Now());
        app.DeclineEligibility(Approver, Now(), "insufficient docs");
        Assert.Equal(BnplFinancingApplicationStatus.Declined, app.Status);
        Assert.Throws<BnplFinancingDomainException>(() => app.CreateOffer(Approver, Now()));
    }

    [Fact]
    public void Invalid_transitions_are_rejected()
    {
        var app = CreateDraft();
        Assert.Throws<BnplFinancingDomainException>(() => app.Approve(Approver, Now()));
        Assert.Throws<BnplFinancingDomainException>(() => app.AcceptOffer(Guid.NewGuid(), Actor, Now()));
    }

    [Fact]
    public void Concurrency_conflict_on_stale_version()
    {
        var app = CreateDraft();
        Assert.Throws<BnplFinancingDomainException>(() => app.Submit(Now(), expectedVersion: 999));
    }

    [Fact]
    public void Cancellation_from_approved_pending_sale_is_allowed()
    {
        var app = ReachApprovedPendingSale();
        app.Cancel(Actor, Now());
        Assert.Equal(BnplFinancingApplicationStatus.Cancelled, app.Status);
    }

    [Fact]
    public async Task Create_use_case_is_idempotent_and_enforces_customer_org()
    {
        var harness = CreateHarness();
        await harness.Customers.AddAsync(BnplCustomer.Create(Org, "Buyer", Now(), customerId: BnplCustomerId.From(CustomerId)));

        var id = Guid.Parse("eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee");
        var first = await harness.Create.ExecuteAsync(Org, Branch, CustomerId, Actor, 1000m, 100m, id);
        Assert.True(first.IsSuccess);
        var retry = await harness.Create.ExecuteAsync(Org, Branch, CustomerId, Actor, 1000m, 100m, id);
        Assert.True(retry.IsSuccess);
        Assert.Equal(1, harness.Applications.Count);

        var conflict = await harness.Create.ExecuteAsync(Org, Branch, CustomerId, Actor, 2000m, 0m, id);
        Assert.False(conflict.IsSuccess);
        Assert.Equal(409, conflict.SuggestedHttpStatus);

        var otherOrg = Guid.Parse("99999999-9999-4999-8999-999999999999");
        var mismatch = await harness.Create.ExecuteAsync(otherOrg, Branch, CustomerId, Actor, 1000m, 0m);
        Assert.False(mismatch.IsSuccess);
    }

    [Fact]
    public async Task Lifecycle_commands_are_idempotent()
    {
        var harness = CreateHarness();
        await harness.Customers.AddAsync(BnplCustomer.Create(Org, "Buyer", Now(), customerId: BnplCustomerId.From(CustomerId)));
        var created = await harness.Create.ExecuteAsync(Org, Branch, CustomerId, Actor, 500m, 0m);
        var id = created.Value!.Id.Value;

        Assert.True((await harness.Submit.ExecuteAsync(Org, id)).IsSuccess);
        Assert.True((await harness.Submit.ExecuteAsync(Org, id)).IsSuccess);
        Assert.True((await harness.ApproveEligibility.ExecuteAsync(Org, id, Approver)).IsSuccess);
        Assert.True((await harness.ApproveEligibility.ExecuteAsync(Org, id, Approver)).IsSuccess);

        var offerId = Guid.Parse("ffffffff-ffff-4fff-8fff-ffffffffffff");
        Assert.True((await harness.CreateOffer.ExecuteAsync(Org, id, Approver, offerId)).IsSuccess);
        Assert.True((await harness.CreateOffer.ExecuteAsync(Org, id, Approver, offerId)).IsSuccess);
        var afterOffers = await harness.Applications.GetByIdAsync(Org, BnplFinancingApplicationId.From(id));
        Assert.Single(afterOffers!.Offers);

        Assert.True((await harness.Accept.ExecuteAsync(Org, id, offerId, Actor)).IsSuccess);
        Assert.True((await harness.Accept.ExecuteAsync(Org, id, offerId, Actor)).IsSuccess);
        Assert.True((await harness.Approve.ExecuteAsync(Org, id, Approver)).IsSuccess);
        Assert.True((await harness.Approve.ExecuteAsync(Org, id, Approver)).IsSuccess);
        Assert.Equal(
            BnplFinancingApplicationStatus.ApprovedPendingSale,
            (await harness.Get.ExecuteAsync(Org, id)).Value!.Status);
    }

    private static BnplFinancingApplication CreateDraft() =>
        BnplFinancingApplication.Create(Org, Branch, CustomerId, Actor, 1000m, 200m, Now());

    private static BnplFinancingApplication ReachApprovedPendingSale()
    {
        var app = CreateDraft();
        app.Submit(Now());
        app.ApproveEligibility(Approver, Now());
        var offer = app.CreateOffer(Approver, Now());
        app.AcceptOffer(offer.Id.Value, Actor, Now());
        app.Approve(Approver, Now());
        return app;
    }

    private static DateTimeOffset Now() => DateTimeOffset.Parse("2026-08-27T12:00:00Z");

    private static Harness CreateHarness()
    {
        var customers = new InMemoryCustomerRepo();
        var apps = new InMemoryApplicationRepo();
        var uow = new NoOpUow();
        var clock = new FixedClock(Now());
        return new Harness(
            customers,
            apps,
            new CreateBnplFinancingApplication(apps, customers, uow, clock),
            new GetBnplFinancingApplication(apps),
            new SubmitBnplFinancingApplication(apps, uow, clock),
            new ApproveBnplFinancingEligibility(apps, uow, clock),
            new CreateBnplFinancingOffer(apps, uow, clock),
            new AcceptBnplFinancingOffer(apps, uow, clock),
            new ApproveBnplFinancingApplication(apps, uow, clock));
    }

    private sealed record Harness(
        InMemoryCustomerRepo Customers,
        InMemoryApplicationRepo Applications,
        CreateBnplFinancingApplication Create,
        GetBnplFinancingApplication Get,
        SubmitBnplFinancingApplication Submit,
        ApproveBnplFinancingEligibility ApproveEligibility,
        CreateBnplFinancingOffer CreateOffer,
        AcceptBnplFinancingOffer Accept,
        ApproveBnplFinancingApplication Approve);

    private sealed class FixedClock(DateTimeOffset utcNow) : IBnplClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }

    private sealed class NoOpUow : IBnplUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryCustomerRepo : IBnplCustomerRepository
    {
        private readonly Dictionary<(Guid, Guid), BnplCustomer> _items = new();

        public Task<BnplCustomer?> GetByIdAsync(Guid organizationId, BnplCustomerId customerId, CancellationToken cancellationToken = default)
        {
            _items.TryGetValue((organizationId, customerId.Value), out var c);
            return Task.FromResult(c);
        }

        public Task<BnplCustomer?> FindByLinkedPersonalPublicUserIdAsync(Guid organizationId, string linkedPersonalPublicUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<BnplCustomer?>(null);

        public Task<BnplCustomer?> FindByLinkedCommerceCustomerIdAsync(Guid organizationId, Guid linkedCommerceCustomerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<BnplCustomer?>(null);

        public Task<(IReadOnlyList<BnplCustomer> Items, int TotalCount)> SearchAsync(Guid organizationId, string? search, BnplCustomerStatus? status, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<BnplCustomer>)Array.Empty<BnplCustomer>(), 0));

        public Task AddAsync(BnplCustomer customer, CancellationToken cancellationToken = default)
        {
            _items[(customer.OrganizationId, customer.Id.Value)] = customer;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(BnplCustomer customer, CancellationToken cancellationToken = default) => AddAsync(customer, cancellationToken);
    }

    private sealed class InMemoryApplicationRepo : IBnplFinancingApplicationRepository
    {
        private readonly Dictionary<(Guid, Guid), BnplFinancingApplication> _items = new();
        public int Count => _items.Count;

        public Task<BnplFinancingApplication?> GetByIdAsync(Guid organizationId, BnplFinancingApplicationId applicationId, CancellationToken cancellationToken = default)
        {
            _items.TryGetValue((organizationId, applicationId.Value), out var a);
            return Task.FromResult(a);
        }

        public Task<(IReadOnlyList<BnplFinancingApplication> Items, int TotalCount)> SearchAsync(
            Guid organizationId, Guid? branchId, Guid? customerId, BnplFinancingApplicationStatus? status, int skip, int take, CancellationToken cancellationToken = default)
        {
            var list = _items.Values.Where(a => a.OrganizationId == organizationId).ToList();
            return Task.FromResult(((IReadOnlyList<BnplFinancingApplication>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task AddAsync(BnplFinancingApplication application, CancellationToken cancellationToken = default)
        {
            _items[(application.OrganizationId, application.Id.Value)] = application;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(BnplFinancingApplication application, CancellationToken cancellationToken = default) =>
            AddAsync(application, cancellationToken);
    }
}
