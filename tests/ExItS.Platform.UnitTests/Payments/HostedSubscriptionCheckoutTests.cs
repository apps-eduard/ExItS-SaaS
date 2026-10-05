using System.Collections.Concurrent;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.Infrastructure.Payments;

namespace ExItS.Platform.UnitTests.Payments;

public sealed class HostedSubscriptionCheckoutTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 8, 0, 0, TimeSpan.Zero);
    private static readonly PlatformUserId UserId = PlatformUserId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly PlatformUserId OtherUserId = PlatformUserId.From(Guid.Parse("99999999-9999-9999-9999-999999999999"));
    private static readonly PlatformOrganizationId OrgId =
        PlatformOrganizationId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly PlatformOrganizationId OtherOrgId =
        PlatformOrganizationId.From(Guid.Parse("33333333-3333-3333-3333-333333333333"));

    [Fact]
    public async Task Checkout_uses_authoritative_plan_price_not_a_client_amount()
    {
        var gateway = new FakeGateway();
        var repo = new MemoryPayments();
        var plans = new MemoryPlans(ActivePlan(monthly: 1499m));
        var start = new StartHostedSubscriptionCheckout(repo, plans, gateway, new MemoryUnitOfWork(), new FixedClock(Now));

        var result = await start.ExecuteForPlanAsync(UserId, "pro", BillingCycle.Monthly, OrgId, "http://127.0.0.1:5177");

        Assert.True(result.IsSuccess);
        Assert.Equal(1499m, gateway.LastRequest!.Amount);
        Assert.Equal("PHP", gateway.LastRequest.CurrencyCode);
        Assert.Equal(1499m, result.Value!.Amount);
        Assert.StartsWith("https://checkout.paymongo.test/", result.Value.CheckoutUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invalid_plan_does_not_create_a_checkout_session()
    {
        var gateway = new FakeGateway();
        var start = new StartHostedSubscriptionCheckout(
            new MemoryPayments(),
            new MemoryPlans(),
            gateway,
            new MemoryUnitOfWork(),
            new FixedClock(Now));

        var result = await start.ExecuteForPlanAsync(UserId, "missing", BillingCycle.Monthly, null, "http://127.0.0.1:5177");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PlanNotFound, result.ErrorCode);
        Assert.Null(gateway.LastRequest);
    }

    [Fact]
    public void Missing_webhook_signature_is_rejected()
    {
        Assert.False(PayMongoWebhookSignature.Matches("{}", null, "whsec_test_only"));
        Assert.False(PayMongoWebhookSignature.Matches("{}", "t=1,te=abc", " "));
    }

    [Fact]
    public async Task Unconfigured_provider_does_not_open_a_local_test_payment_page()
    {
        var gateway = new FakeGateway { Configured = false };
        var repo = new MemoryPayments();
        var payment = Pending(organizationId: null);
        await repo.AddAsync(payment);
        var start = new StartHostedSubscriptionCheckout(repo, new MemoryPlans(), gateway, new MemoryUnitOfWork(), new FixedClock(Now));

        var result = await start.ExecuteForPaymentAsync(
            payment.Id.Value,
            UserId,
            expectedOrganizationId: null,
            "https://my.exitsapps.com");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PaymentNotConfigured, result.ErrorCode);
        Assert.Equal(0, gateway.CreateCount);
    }

    [Fact]
    public async Task Organization_mismatch_does_not_start_checkout()
    {
        var gateway = new FakeGateway();
        var repo = new MemoryPayments();
        var payment = Pending(OrgId);
        await repo.AddAsync(payment);
        var start = new StartHostedSubscriptionCheckout(repo, new MemoryPlans(), gateway, new MemoryUnitOfWork(), new FixedClock(Now));

        var result = await start.ExecuteForPaymentAsync(payment.Id.Value, UserId, OtherOrgId.Value, "http://127.0.0.1:5177");

        Assert.Equal(DomainErrorCodes.AuthorizationDenied, result.ErrorCode);
        Assert.Null(gateway.LastRequest);
    }

    [Fact]
    public async Task Repeating_checkout_reuses_the_existing_session()
    {
        var gateway = new FakeGateway();
        var repo = new MemoryPayments();
        var plans = new MemoryPlans(ActivePlan(monthly: 1499m));
        var start = new StartHostedSubscriptionCheckout(repo, plans, gateway, new MemoryUnitOfWork(), new FixedClock(Now));

        var first = await start.ExecuteForPlanAsync(UserId, "pro", BillingCycle.Monthly, OrgId, "http://127.0.0.1:5177");
        var second = await start.ExecuteForPlanAsync(UserId, "pro", BillingCycle.Monthly, OrgId, "http://127.0.0.1:5177");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.CheckoutUrl, second.Value!.CheckoutUrl);
        Assert.Equal(1, gateway.CreateCount);
    }

    [Fact]
    public async Task Paid_event_activates_once_and_repeat_stays_idempotent()
    {
        var repo = new MemoryPayments();
        var payment = Pending(OrgId);
        payment.AttachHostedCheckout("cs_test_1", "https://checkout.paymongo.test/cs_test_1", Now);
        await repo.AddAsync(payment);
        var activator = new CountingActivator();
        var apply = new ApplyTrustedHostedCheckoutPayment(repo, activator, new MemoryUnitOfWork(), new FixedClock(Now));

        var first = await apply.ExecuteAsync("cs_test_1", 1499m, "PHP", "evt_1");
        var second = await apply.ExecuteAsync("cs_test_1", 1499m, "PHP", "evt_1");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(SubscriptionPaymentStatus.Paid.ToString(), first.Value!.Status);
        Assert.True(first.Value.SubscriptionActivated);
        Assert.Equal(1, activator.Calls);
    }

    [Fact]
    public async Task Amount_mismatch_does_not_activate()
    {
        var (apply, activator, payment) = await ReadyPaymentAsync();
        var result = await apply.ExecuteAsync(payment.ProviderReference!, 1m, "PHP", "evt_bad_amount");

        Assert.Equal(ApplicationErrorCodes.PaymentAmountMismatch, result.ErrorCode);
        Assert.Equal(0, activator.Calls);
        Assert.Equal(SubscriptionPaymentStatus.Processing, payment.Status);
    }

    [Fact]
    public async Task Currency_mismatch_does_not_activate()
    {
        var (apply, activator, _) = await ReadyPaymentAsync();
        var result = await apply.ExecuteAsync("cs_test_1", 1499m, "USD", "evt_bad_currency");

        Assert.Equal(ApplicationErrorCodes.PaymentCurrencyMismatch, result.ErrorCode);
        Assert.Equal(0, activator.Calls);
    }

    [Fact]
    public async Task Unknown_checkout_does_not_activate()
    {
        var activator = new CountingActivator();
        var apply = new ApplyTrustedHostedCheckoutPayment(
            new MemoryPayments(),
            activator,
            new MemoryUnitOfWork(),
            new FixedClock(Now));

        var result = await apply.ExecuteAsync("cs_missing", 1499m, "PHP", "evt_missing");

        Assert.Equal(ApplicationErrorCodes.PaymentNotFound, result.ErrorCode);
        Assert.Equal(0, activator.Calls);
    }

    [Fact]
    public void Webhook_signature_matches_the_test_header()
    {
        const string secret = "whsec_test_only";
        const string body = """{"data":{"id":"evt_1"}}""";
        var signature = PayMongoWebhookSignature.Compute(secret, "1700000000", body);

        Assert.True(PayMongoWebhookSignature.Matches(body, $"t=1700000000,te={signature}", secret));
        Assert.False(PayMongoWebhookSignature.Matches(body, "t=1700000000,te=deadbeef", secret));
    }

    [Fact]
    public void Paid_webhook_payload_reads_centavos_as_php()
    {
        const string body = """
            {
              "data": {
                "id": "evt_1",
                "attributes": {
                  "type": "checkout_session.payment.paid",
                  "data": {
                    "id": "cs_test_1",
                    "attributes": {
                      "payments": [
                        { "id": "pay_1", "attributes": { "amount": 149900, "currency": "PHP", "status": "paid" } }
                      ]
                    }
                  }
                }
              }
            }
            """;

        Assert.True(PayMongoCheckoutPayload.TryParsePaidEvent(body, out var eventId, out var state));
        Assert.Equal("evt_1", eventId);
        Assert.Equal("cs_test_1", state.SessionId);
        Assert.Equal(1499m, state.PaidAmount);
        Assert.Equal("PHP", state.CurrencyCode);
    }

    [Fact]
    public void Checkout_requests_card_gcash_maya_and_qr_ph()
    {
        Assert.Equal(["card", "gcash", "paymaya", "qrph"], PayMongoCheckoutGateway.SubscriptionPaymentMethodTypes);
        Assert.DoesNotContain("grab_pay", PayMongoCheckoutGateway.SubscriptionPaymentMethodTypes);
    }

    [Fact]
    public async Task Personal_context_cannot_confirm_an_organization_checkout()
    {
        var gateway = new FakeGateway
        {
            SessionState = new HostedCheckoutProviderState("cs_test_1", true, false, false, 1499m, "PHP", "pay_1"),
        };
        var repo = new MemoryPayments();
        var payment = Pending(OrgId);
        payment.AttachHostedCheckout("cs_test_1", "https://checkout.paymongo.test/cs_test_1", Now);
        await repo.AddAsync(payment);
        var activator = new CountingActivator();
        var sync = new SyncHostedSubscriptionCheckout(
            repo,
            gateway,
            new ApplyTrustedHostedCheckoutPayment(repo, activator, new MemoryUnitOfWork(), new FixedClock(Now)),
            new MemoryUnitOfWork(),
            new FixedClock(Now));

        var result = await sync.ExecuteAsync(payment.Id.Value, UserId, expectedOrganizationId: null);

        Assert.Equal(DomainErrorCodes.AuthorizationDenied, result.ErrorCode);
        Assert.Equal(0, gateway.GetCount);
        Assert.Equal(0, activator.Calls);
        Assert.Equal(SubscriptionPaymentStatus.Processing, payment.Status);
    }

    [Fact]
    public async Task Concurrent_personal_checkout_starts_create_one_session()
    {
        var gateway = new FakeGateway { CreateDelay = TimeSpan.FromMilliseconds(150) };
        var repo = new MemoryPayments();
        var start = new StartHostedSubscriptionCheckout(
            repo,
            new MemoryPlans(ActivePlan(monthly: 1499m)),
            gateway,
            new MemoryUnitOfWork(),
            new FixedClock(Now));

        var results = await Task.WhenAll(
            start.ExecuteForPlanAsync(UserId, "pro", BillingCycle.Monthly, null, "http://127.0.0.1:5177"),
            start.ExecuteForPlanAsync(UserId, "pro", BillingCycle.Monthly, null, "http://127.0.0.1:5177"));

        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Equal(results[0].Value!.PaymentId, results[1].Value!.PaymentId);
        Assert.Equal(results[0].Value!.CheckoutUrl, results[1].Value!.CheckoutUrl);
        Assert.Equal(1, gateway.CreateCount);
        Assert.Equal(1, gateway.MaxInFlight);
    }

    [Fact]
    public async Task Concurrent_organization_checkout_starts_create_one_session()
    {
        var gateway = new FakeGateway { CreateDelay = TimeSpan.FromMilliseconds(150) };
        var repo = new MemoryPayments();
        var start = new StartHostedSubscriptionCheckout(
            repo,
            new MemoryPlans(ActivePlan(monthly: 1499m)),
            gateway,
            new MemoryUnitOfWork(),
            new FixedClock(Now));

        var results = await Task.WhenAll(
            start.ExecuteForPlanAsync(UserId, "pro", BillingCycle.Monthly, OrgId, "http://127.0.0.1:5177"),
            start.ExecuteForPlanAsync(UserId, "pro", BillingCycle.Monthly, OrgId, "http://127.0.0.1:5177"));

        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Equal(results[0].Value!.PaymentId, results[1].Value!.PaymentId);
        Assert.Equal(results[0].Value!.CheckoutUrl, results[1].Value!.CheckoutUrl);
        Assert.Equal(OrgId.Value, results[0].Value!.OrganizationId);
        Assert.Equal(1, gateway.CreateCount);
        Assert.Equal(1, gateway.MaxInFlight);
    }

    [Fact]
    public async Task Concurrent_starts_of_the_same_payment_create_one_session()
    {
        var gateway = new FakeGateway { CreateDelay = TimeSpan.FromMilliseconds(150) };
        var repo = new MemoryPayments();
        var payment = Pending(null);
        await repo.AddAsync(payment);
        var start = new StartHostedSubscriptionCheckout(
            repo,
            new MemoryPlans(),
            gateway,
            new MemoryUnitOfWork(),
            new FixedClock(Now));

        var results = await Task.WhenAll(
            start.ExecuteForPaymentAsync(payment.Id.Value, UserId, null, "http://127.0.0.1:5177"),
            start.ExecuteForPaymentAsync(payment.Id.Value, UserId, null, "http://127.0.0.1:5177"));

        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Equal(payment.Id.Value, results[0].Value!.PaymentId);
        Assert.Equal(results[0].Value!.CheckoutUrl, results[1].Value!.CheckoutUrl);
        Assert.Equal(1, gateway.CreateCount);
        Assert.Equal(1, gateway.MaxInFlight);
    }

    [Fact]
    public async Task Another_subscriber_or_organization_does_not_reuse_the_checkout()
    {
        var gateway = new FakeGateway();
        var repo = new MemoryPayments();
        var plans = new MemoryPlans(ActivePlan(monthly: 1499m));
        var start = new StartHostedSubscriptionCheckout(repo, plans, gateway, new MemoryUnitOfWork(), new FixedClock(Now));

        var owner = await start.ExecuteForPlanAsync(UserId, "pro", BillingCycle.Monthly, null, "http://127.0.0.1:5177");
        var otherUser = await start.ExecuteForPlanAsync(OtherUserId, "pro", BillingCycle.Monthly, null, "http://127.0.0.1:5177");
        var denied = await start.ExecuteForPaymentAsync(owner.Value!.PaymentId, OtherUserId, null, "http://127.0.0.1:5177");

        Assert.True(owner.IsSuccess);
        Assert.True(otherUser.IsSuccess);
        Assert.NotEqual(owner.Value!.PaymentId, otherUser.Value!.PaymentId);
        Assert.NotEqual(owner.Value.CheckoutUrl, otherUser.Value.CheckoutUrl);
        Assert.Equal(DomainErrorCodes.AuthorizationDenied, denied.ErrorCode);
        Assert.Equal(2, gateway.CreateCount);

        var org = await start.ExecuteForPlanAsync(UserId, "pro", BillingCycle.Monthly, OrgId, "http://127.0.0.1:5177");
        var otherOrg = await start.ExecuteForPlanAsync(UserId, "pro", BillingCycle.Monthly, OtherOrgId, "http://127.0.0.1:5177");
        var wrongOrg = await start.ExecuteForPaymentAsync(org.Value!.PaymentId, UserId, OtherOrgId.Value, "http://127.0.0.1:5177");

        Assert.True(org.IsSuccess);
        Assert.True(otherOrg.IsSuccess);
        Assert.NotEqual(org.Value!.CheckoutUrl, otherOrg.Value!.CheckoutUrl);
        Assert.Equal(DomainErrorCodes.AuthorizationDenied, wrongOrg.ErrorCode);
        Assert.Equal(4, gateway.CreateCount);
    }

    [Fact]
    public async Task Paid_cancelled_and_failed_payments_keep_their_terminal_behavior()
    {
        var gateway = new FakeGateway();
        var repo = new MemoryPayments();
        var start = new StartHostedSubscriptionCheckout(
            repo,
            new MemoryPlans(ActivePlan(monthly: 1499m)),
            gateway,
            new MemoryUnitOfWork(),
            new FixedClock(Now));

        var paidStart = await start.ExecuteForPlanAsync(UserId, "pro", BillingCycle.Monthly, null, "http://127.0.0.1:5177");
        var paid = (await repo.GetByIdAsync(SubscriptionPaymentTransactionId.From(paidStart.Value!.PaymentId)))!;
        paid.MarkPaid(Now, Now, Now.AddMonths(1));
        var paidAgain = await start.ExecuteForPaymentAsync(paid.Id.Value, UserId, null, "http://127.0.0.1:5177");
        var afterPaid = await start.ExecuteForPlanAsync(UserId, "pro", BillingCycle.Monthly, null, "http://127.0.0.1:5177");

        Assert.Equal(ApplicationErrorCodes.PaymentAlreadyConfirmed, paidAgain.ErrorCode);
        Assert.True(afterPaid.IsSuccess);
        Assert.NotEqual(paid.Id.Value, afterPaid.Value!.PaymentId);
        Assert.Equal(2, gateway.CreateCount);

        var cancelled = Pending(OrgId);
        cancelled.AttachHostedCheckout("cs_cancelled", "https://checkout.paymongo.test/cs_cancelled", Now);
        cancelled.CancelOpenCheckout(Now, "Customer cancelled");
        await repo.AddAsync(cancelled);
        var cancelledAgain = await start.ExecuteForPaymentAsync(cancelled.Id.Value, UserId, OrgId.Value, "http://127.0.0.1:5177");

        var failed = Pending(OrgId);
        failed.MarkFailed("card_declined", "Declined", Now);
        await repo.AddAsync(failed);
        var failedAgain = await start.ExecuteForPaymentAsync(failed.Id.Value, UserId, OrgId.Value, "http://127.0.0.1:5177");

        Assert.Equal(ApplicationErrorCodes.PaymentInvalidTransition, cancelledAgain.ErrorCode);
        Assert.Equal(ApplicationErrorCodes.PaymentInvalidTransition, failedAgain.ErrorCode);
        Assert.Equal(2, gateway.CreateCount);
    }

    [Fact]
    public async Task Another_user_cannot_start_checkout()
    {
        var gateway = new FakeGateway();
        var repo = new MemoryPayments();
        var payment = Pending(null);
        await repo.AddAsync(payment);
        var start = new StartHostedSubscriptionCheckout(repo, new MemoryPlans(), gateway, new MemoryUnitOfWork(), new FixedClock(Now));

        var result = await start.ExecuteForPaymentAsync(payment.Id.Value, OtherUserId, null, "http://127.0.0.1:5177");

        Assert.Equal(DomainErrorCodes.AuthorizationDenied, result.ErrorCode);
        Assert.Null(gateway.LastRequest);
    }

    private static async Task<(ApplyTrustedHostedCheckoutPayment Apply, CountingActivator Activator, SubscriptionPaymentTransaction Payment)> ReadyPaymentAsync()
    {
        var repo = new MemoryPayments();
        var payment = Pending(OrgId);
        payment.AttachHostedCheckout("cs_test_1", "https://checkout.paymongo.test/cs_test_1", Now);
        await repo.AddAsync(payment);
        var activator = new CountingActivator();
        var apply = new ApplyTrustedHostedCheckoutPayment(repo, activator, new MemoryUnitOfWork(), new FixedClock(Now));
        return (apply, activator, payment);
    }

    private static SubscriptionPaymentTransaction Pending(PlatformOrganizationId? organizationId) =>
        SubscriptionPaymentTransaction.CreatePending(
            "PAY-20261004-000001",
            UserId,
            "pro",
            BillingCycle.Monthly,
            SubscriptionBillingPricing.Quote("pro", 1499m, 14990m, "PHP", BillingCycle.Monthly),
            Now,
            organizationId);

    private static Plan ActivePlan(decimal monthly)
    {
        var plan = Plan.CreateDraft(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            PlanCode.Create("pro"),
            "Pro",
            Now);
        plan.UpdateCommercialPackage(
            description: null,
            maxBranches: 3,
            maxActiveStaff: 10,
            maxActivePosDevices: 5,
            maxActiveBusinessTypes: 1,
            customerCreditEnabled: true,
            advancedReportsEnabled: true,
            exportEnabled: true,
            trialAllowed: false,
            defaultTrialDays: 0,
            sortOrder: 2,
            monthlyPrice: monthly,
            annualPrice: monthly * 10m,
            currencyCode: "PHP",
            utcNow: Now);
        plan.Activate(Now);
        return plan;
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class MemoryUnitOfWork : IPlatformUnitOfWork
    {
        private readonly ConcurrentDictionary<(Guid, Guid), SemaphoreSlim> _locks = new();

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public async Task ExecuteWithAdvisoryLockAsync(
            Guid lockKeyA,
            Guid lockKeyB,
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken = default)
        {
            var gate = _locks.GetOrAdd((lockKeyA, lockKeyB), _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await action(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                gate.Release();
            }
        }
    }

    private sealed class CountingActivator : IHostedCheckoutSubscriptionActivator
    {
        public int Calls { get; private set; }

        public Task<ApplicationResult> ActivateAsync(
            SubscriptionPaymentTransaction payment,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            payment.MarkSubscriptionActivated(SubscriptionId.New(), Now);
            return Task.FromResult(ApplicationResult.Success());
        }
    }

    private sealed class FakeGateway : ISubscriptionCheckoutGateway
    {
        private int _createCount;
        private int _inFlight;
        private int _maxInFlight;

        public int CreateCount => _createCount;
        public int MaxInFlight => _maxInFlight;
        public int GetCount { get; private set; }
        public TimeSpan CreateDelay { get; set; }
        public HostedCheckoutCreateRequest? LastRequest { get; private set; }
        public HostedCheckoutProviderState SessionState { get; set; } =
            new("cs_test_1", false, false, false, null, null, null);
        public bool Configured { get; set; } = true;

        public bool IsConfigured => Configured;

        public async Task<HostedCheckoutSessionResult> CreateSessionAsync(
            HostedCheckoutCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            var current = Interlocked.Increment(ref _inFlight);
            int observed;
            do
            {
                observed = _maxInFlight;
            }
            while (current > observed
                && Interlocked.CompareExchange(ref _maxInFlight, current, observed) != observed);

            try
            {
                if (CreateDelay > TimeSpan.Zero)
                {
                    await Task.Delay(CreateDelay, cancellationToken).ConfigureAwait(false);
                }

                var n = Interlocked.Increment(ref _createCount);
                LastRequest = request;
                return new HostedCheckoutSessionResult(
                    $"cs_test_{n}",
                    $"https://checkout.paymongo.test/cs_test_{n}",
                    IsTest: true);
            }
            finally
            {
                Interlocked.Decrement(ref _inFlight);
            }
        }

        public Task<HostedCheckoutProviderState> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            GetCount++;
            return Task.FromResult(SessionState);
        }
    }

    private sealed class MemoryPlans(params Plan[] plans) : IPlanRepository
    {
        public Task<Plan?> GetByIdAsync(PlanId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(plans.FirstOrDefault(plan => plan.Id == id));

        public Task<Plan?> GetByProductAndCodeAsync(ProductCode productCode, PlanCode planCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(plans.FirstOrDefault(plan => plan.Code == planCode));

        public Task<IReadOnlyList<Plan>> ListByProductAsync(ProductCode productCode, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Plan>>(plans);

        public Task<(IReadOnlyList<Plan> Items, int TotalCount)> ListAsync(ProductCode? productCode, PlanStatus? status, string? search, CatalogListSortBy sortBy, bool sortDescending, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Plan>, int)>((plans, plans.Length));

        public Task AddAsync(Plan plan, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(Plan plan, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<PlanVersion?> GetVersionByIdAsync(PlanVersionId id, CancellationToken cancellationToken = default) => Task.FromResult<PlanVersion?>(null);
        public Task<PlanVersion?> GetVersionByPlanAndNumberAsync(PlanId planId, int versionNumber, CancellationToken cancellationToken = default) => Task.FromResult<PlanVersion?>(null);
        public Task<IReadOnlyList<PlanVersion>> ListVersionsAsync(PlanId planId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PlanVersion>>([]);
        public Task<PlanVersion?> GetLatestPublishedVersionAsync(PlanId planId, CancellationToken cancellationToken = default) => Task.FromResult<PlanVersion?>(null);
        public Task<int> GetMaxVersionNumberAsync(PlanId planId, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task AddVersionAsync(PlanVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateVersionAsync(PlanVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class MemoryPayments : ISubscriptionPaymentTransactionRepository
    {
        private readonly List<SubscriptionPaymentTransaction> _items = [];

        public Task<SubscriptionPaymentTransaction?> GetByIdAsync(SubscriptionPaymentTransactionId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(item => item.Id == id));

        public Task<SubscriptionPaymentTransaction?> GetByReferenceAsync(string referenceNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(item => item.ReferenceNumber == referenceNumber));

        public Task<SubscriptionPaymentTransaction?> GetByProviderReferenceAsync(string providerReference, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(item => item.ProviderReference == providerReference));

        public Task<SubscriptionPaymentTransaction?> FindLatestOpenAsync(PlatformUserId initiatedByUserId, PlatformOrganizationId? organizationId, string planKey, BillingCycle billingCycle, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.LastOrDefault(item =>
                item.InitiatedByUserId == initiatedByUserId
                && item.OrganizationId == organizationId
                && item.PlanKey == planKey
                && item.BillingCycle == billingCycle
                && item.Status is SubscriptionPaymentStatus.Pending or SubscriptionPaymentStatus.Processing));

        public Task<long> GetNextSequenceAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult((long)_items.Count + 1);

        public Task AddAsync(SubscriptionPaymentTransaction payment, CancellationToken cancellationToken = default)
        {
            lock (_items)
            {
                var duplicateOpen = payment.Status is SubscriptionPaymentStatus.Pending or SubscriptionPaymentStatus.Processing
                    && _items.Any(item =>
                        item.InitiatedByUserId == payment.InitiatedByUserId
                        && item.OrganizationId == payment.OrganizationId
                        && item.PlanKey == payment.PlanKey
                        && item.BillingCycle == payment.BillingCycle
                        && item.Status is SubscriptionPaymentStatus.Pending or SubscriptionPaymentStatus.Processing);
                if (duplicateOpen)
                {
                    throw new PersistenceConflictException(
                        ApplicationErrorCodes.ConcurrencyConflict,
                        "An open subscription checkout already exists for this plan.");
                }

                _items.Add(payment);
            }

            return Task.CompletedTask;
        }

        public Task UpdateAsync(SubscriptionPaymentTransaction payment, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<SubscriptionPaymentTransaction>> ListRecentAsync(int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SubscriptionPaymentTransaction>>(_items.Take(take).ToArray());
    }
}
