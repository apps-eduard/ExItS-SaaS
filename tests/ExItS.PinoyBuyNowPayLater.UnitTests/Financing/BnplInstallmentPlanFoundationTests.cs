using ExItS.PinoyBuyNowPayLater.Domain.Financing;

namespace ExItS.PinoyBuyNowPayLater.UnitTests.Financing;

public sealed class BnplInstallmentPlanFoundationTests
{
    private static readonly Guid Org = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid Branch = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid CustomerId = Guid.Parse("33333333-3333-4333-8333-333333333333");
    private static readonly Guid Actor = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    private static readonly Guid Approver = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");

    [Fact]
    public void Valid_plan_totals_financed_principal_only()
    {
        var app = OfferedApp(60_000m, 10_000m);
        var offer = app.CurrentOffer!;
        Assert.Equal(50_000m, offer.FinancedPrincipal);

        var plan = app.AttachOrReplaceInstallmentPlan(
            offer.Id.Value,
            BnplInstallmentPlanId.From(Guid.Parse("aaaaaaaa-1111-4111-8111-aaaaaaaaaaaa")),
            FiveEqual(50_000m),
            Approver,
            Now());

        Assert.Equal(50_000m, plan.TotalScheduledPrincipal);
        Assert.Equal(5, plan.Items.Count);
        Assert.False(plan.IsLocked);
    }

    [Theory]
    [InlineData(49_999.99)]
    [InlineData(50_000.01)]
    public void Total_mismatch_is_rejected(decimal total)
    {
        var app = OfferedApp(60_000m, 10_000m);
        var offer = app.CurrentOffer!;
        var ex = Assert.Throws<BnplFinancingDomainException>(() =>
            app.AttachOrReplaceInstallmentPlan(
                offer.Id.Value,
                BnplInstallmentPlanId.New(),
                UnequalFive(total),
                Approver,
                Now()));
        Assert.Equal(BnplFinancingErrorCodes.PlanTotalMismatch, ex.ErrorCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Zero_and_negative_amounts_blocked(decimal amount)
    {
        var app = OfferedApp(1_000m, 0m);
        var offer = app.CurrentOffer!;
        var items = new[]
        {
            new BnplInstallmentPlanItemDraft(Guid.NewGuid(), 1, amount, DateOnly.Parse("2026-10-01"))
        };
        var ex = Assert.Throws<BnplFinancingDomainException>(() =>
            app.AttachOrReplaceInstallmentPlan(offer.Id.Value, BnplInstallmentPlanId.New(), items, Approver, Now()));
        Assert.Equal(BnplFinancingErrorCodes.InvalidPlanAmount, ex.ErrorCode);
    }

    [Fact]
    public void Empty_duplicate_sequence_and_nonincreasing_dates_blocked()
    {
        var app = OfferedApp(1_000m, 0m);
        var offer = app.CurrentOffer!;

        Assert.Equal(
            BnplFinancingErrorCodes.PlanEmpty,
            Assert.Throws<BnplFinancingDomainException>(() =>
                app.AttachOrReplaceInstallmentPlan(
                    offer.Id.Value,
                    BnplInstallmentPlanId.New(),
                    Array.Empty<BnplInstallmentPlanItemDraft>(),
                    Approver,
                    Now())).ErrorCode);

        Assert.Equal(
            BnplFinancingErrorCodes.InvalidPlanSequence,
            Assert.Throws<BnplFinancingDomainException>(() =>
                app.AttachOrReplaceInstallmentPlan(
                    offer.Id.Value,
                    BnplInstallmentPlanId.New(),
                    [
                        new(Guid.NewGuid(), 1, 500m, DateOnly.Parse("2026-10-01")),
                        new(Guid.NewGuid(), 1, 500m, DateOnly.Parse("2026-11-01"))
                    ],
                    Approver,
                    Now())).ErrorCode);

        Assert.Equal(
            BnplFinancingErrorCodes.InvalidPlanSequence,
            Assert.Throws<BnplFinancingDomainException>(() =>
                app.AttachOrReplaceInstallmentPlan(
                    offer.Id.Value,
                    BnplInstallmentPlanId.New(),
                    [
                        new(Guid.NewGuid(), 1, 500m, DateOnly.Parse("2026-10-01")),
                        new(Guid.NewGuid(), 3, 500m, DateOnly.Parse("2026-11-01"))
                    ],
                    Approver,
                    Now())).ErrorCode);

        var dupItem = Guid.NewGuid();
        Assert.Equal(
            BnplFinancingErrorCodes.DuplicatePlanItemId,
            Assert.Throws<BnplFinancingDomainException>(() =>
                app.AttachOrReplaceInstallmentPlan(
                    offer.Id.Value,
                    BnplInstallmentPlanId.New(),
                    [
                        new(dupItem, 1, 500m, DateOnly.Parse("2026-10-01")),
                        new(dupItem, 2, 500m, DateOnly.Parse("2026-11-01"))
                    ],
                    Approver,
                    Now())).ErrorCode);

        Assert.Equal(
            BnplFinancingErrorCodes.InvalidPlanDueDate,
            Assert.Throws<BnplFinancingDomainException>(() =>
                app.AttachOrReplaceInstallmentPlan(
                    offer.Id.Value,
                    BnplInstallmentPlanId.New(),
                    [
                        new(Guid.NewGuid(), 1, 500m, DateOnly.Parse("2026-11-01")),
                        new(Guid.NewGuid(), 2, 500m, DateOnly.Parse("2026-10-01"))
                    ],
                    Approver,
                    Now())).ErrorCode);
    }

    [Fact]
    public void Accept_requires_plan_and_locks_it()
    {
        var app = OfferedApp(60_000m, 10_000m);
        var offer = app.CurrentOffer!;
        var missing = Assert.Throws<BnplFinancingDomainException>(() =>
            app.AcceptOffer(offer.Id.Value, Actor, Now()));
        Assert.Equal(BnplFinancingErrorCodes.PlanRequired, missing.ErrorCode);

        var plan = app.AttachOrReplaceInstallmentPlan(
            offer.Id.Value,
            BnplInstallmentPlanId.New(),
            FiveEqual(50_000m),
            Approver,
            Now());
        app.AcceptOffer(offer.Id.Value, Actor, Now());
        Assert.True(plan.IsLocked);
        Assert.Equal(BnplFinancingApplicationStatus.CustomerAccepted, app.Status);
    }

    [Fact]
    public void Post_acceptance_plan_mutation_blocked_and_pre_acceptance_replace_allowed()
    {
        var app = OfferedApp(60_000m, 10_000m);
        var offer = app.CurrentOffer!;
        var firstId = BnplInstallmentPlanId.From(Guid.Parse("cccccccc-1111-4111-8111-cccccccccccc"));
        app.AttachOrReplaceInstallmentPlan(offer.Id.Value, firstId, FiveEqual(50_000m), Approver, Now());

        var secondId = BnplInstallmentPlanId.From(Guid.Parse("dddddddd-1111-4111-8111-dddddddddddd"));
        var replacement = app.AttachOrReplaceInstallmentPlan(
            offer.Id.Value,
            secondId,
            FiveEqual(50_000m, DateOnly.Parse("2026-11-01")),
            Approver,
            Now());
        Assert.True(app.InstallmentPlans.Single(p => p.Id == firstId).IsSuperseded);
        Assert.Equal(secondId, replacement.Id);

        app.AcceptOffer(offer.Id.Value, Actor, Now());
        Assert.Equal(
            BnplFinancingErrorCodes.PlanImmutable,
            Assert.Throws<BnplFinancingDomainException>(() =>
                app.AttachOrReplaceInstallmentPlan(
                    offer.Id.Value,
                    BnplInstallmentPlanId.New(),
                    FiveEqual(50_000m),
                    Approver,
                    Now())).ErrorCode);

        var locked = app.AcceptedInstallmentPlan!;
        Assert.Equal(50_000m, locked.TotalScheduledPrincipal);
        Assert.Equal(DateOnly.Parse("2026-11-01"), locked.Items[0].DueDate);
    }

    [Fact]
    public void Approval_requires_accepted_locked_plan_and_creates_no_debt()
    {
        var app = OfferedApp(60_000m, 10_000m);
        var offer = app.CurrentOffer!;
        app.AttachOrReplaceInstallmentPlan(offer.Id.Value, BnplInstallmentPlanId.New(), FiveEqual(50_000m), Approver, Now());
        app.AcceptOffer(offer.Id.Value, Actor, Now());
        app.Approve(Approver, Now());

        Assert.Equal(BnplFinancingApplicationStatus.ApprovedPendingSale, app.Status);
        Assert.False(app.HasOutstandingDebt);
        Assert.False(app.HasInstallments);
        Assert.False(app.AreRepaymentsAllowed);
        Assert.NotNull(app.AcceptedInstallmentPlan);
        Assert.Throws<BnplFinancingDomainException>(() => app.ActivateProhibited());
    }

    [Fact]
    public void Plan_idempotency_converges_and_conflicts_on_payload_change()
    {
        var app = OfferedApp(60_000m, 10_000m);
        var offer = app.CurrentOffer!;
        var planId = BnplInstallmentPlanId.From(Guid.Parse("eeeeeeee-1111-4111-8111-eeeeeeeeeeee"));
        var items = FiveEqual(50_000m);
        var first = app.AttachOrReplaceInstallmentPlan(offer.Id.Value, planId, items, Approver, Now());
        var retry = app.AttachOrReplaceInstallmentPlan(offer.Id.Value, planId, items, Approver, Now());
        Assert.Same(first, retry);
        Assert.Single(app.InstallmentPlans);

        var conflict = Assert.Throws<BnplFinancingDomainException>(() =>
            app.AttachOrReplaceInstallmentPlan(
                offer.Id.Value,
                planId,
                FiveEqual(50_000m, DateOnly.Parse("2027-01-01")),
                Approver,
                Now()));
        Assert.Equal(BnplFinancingErrorCodes.IdempotencyConflict, conflict.ErrorCode);
    }

    [Fact]
    public void Stale_version_conflicts_with_acceptance()
    {
        var app = OfferedApp(60_000m, 10_000m);
        var offer = app.CurrentOffer!;
        var version = app.AggregateVersion;
        app.AttachOrReplaceInstallmentPlan(
            offer.Id.Value,
            BnplInstallmentPlanId.New(),
            FiveEqual(50_000m),
            Approver,
            Now(),
            expectedVersion: version);

        Assert.Equal(
            BnplFinancingErrorCodes.ConcurrencyConflict,
            Assert.Throws<BnplFinancingDomainException>(() =>
                app.AcceptOffer(offer.Id.Value, Actor, Now(), expectedVersion: version)).ErrorCode);

        app.AcceptOffer(offer.Id.Value, Actor, Now(), expectedVersion: app.AggregateVersion);
        Assert.Equal(
            BnplFinancingErrorCodes.ConcurrencyConflict,
            Assert.Throws<BnplFinancingDomainException>(() =>
                app.AttachOrReplaceInstallmentPlan(
                    offer.Id.Value,
                    BnplInstallmentPlanId.New(),
                    FiveEqual(50_000m),
                    Approver,
                    Now(),
                    expectedVersion: version)).ErrorCode);
    }

    [Fact]
    public void Cannot_attach_plan_before_offered()
    {
        var app = BnplFinancingApplication.Create(Org, Branch, CustomerId, Actor, 60_000m, 10_000m, Now());
        app.Submit(Now());
        var ex = Assert.Throws<BnplFinancingDomainException>(() =>
            app.AttachOrReplaceInstallmentPlan(
                Guid.NewGuid(),
                BnplInstallmentPlanId.New(),
                FiveEqual(50_000m),
                Approver,
                Now()));
        Assert.Equal(BnplFinancingErrorCodes.NotFound, ex.ErrorCode);

        app.ApproveEligibility(Approver, Now());
        // still no offer until CreateOffer
        Assert.Equal(
            BnplFinancingErrorCodes.NotFound,
            Assert.Throws<BnplFinancingDomainException>(() =>
                app.AttachOrReplaceInstallmentPlan(
                    Guid.NewGuid(),
                    BnplInstallmentPlanId.New(),
                    FiveEqual(50_000m),
                    Approver,
                    Now())).ErrorCode);
    }

    private static BnplFinancingApplication OfferedApp(decimal purchase, decimal down)
    {
        var app = BnplFinancingApplication.Create(Org, Branch, CustomerId, Actor, purchase, down, Now());
        app.Submit(Now());
        app.ApproveEligibility(Approver, Now());
        app.CreateOffer(Approver, Now());
        return app;
    }

    private static IReadOnlyList<BnplInstallmentPlanItemDraft> FiveEqual(
        decimal total,
        DateOnly? firstDue = null)
    {
        var start = firstDue ?? DateOnly.Parse("2026-10-01");
        var each = decimal.Round(total / 5, 2, MidpointRounding.AwayFromZero);
        return Enumerable.Range(1, 5)
            .Select(i => new BnplInstallmentPlanItemDraft(
                Guid.Parse($"{i:x8}-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
                i,
                each,
                start.AddMonths(i - 1)))
            .ToArray();
    }

    private static IReadOnlyList<BnplInstallmentPlanItemDraft> UnequalFive(decimal total)
    {
        var each = decimal.Round(total / 5, 2, MidpointRounding.AwayFromZero);
        var items = new List<BnplInstallmentPlanItemDraft>();
        var allocated = 0m;
        for (var i = 1; i <= 5; i++)
        {
            var amount = i == 5 ? total - allocated : each;
            allocated += amount;
            items.Add(new BnplInstallmentPlanItemDraft(
                Guid.Parse($"{i:x8}-bbbb-4bbb-8bbb-bbbbbbbbbbbb"),
                i,
                amount,
                DateOnly.Parse("2026-10-01").AddMonths(i - 1)));
        }

        return items;
    }

    private static DateTimeOffset Now() => DateTimeOffset.Parse("2026-08-27T12:00:00Z");
}
