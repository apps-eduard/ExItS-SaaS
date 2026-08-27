namespace ExItS.PinoyBuyNowPayLater.Domain.Financing;

/// <summary>
/// BNPL financing application aggregate through APPROVED_PENDING_SALE.
/// Does not create debt, collectible installments, inventory changes, or ACTIVE financing (BNPL-07).
/// BNPL-05 adds explicit principal-only installment plans attached to offers (not collectible until ACTIVE).
/// </summary>
public sealed class BnplFinancingApplication
{
    public const int DescriptionMaxLength = 512;
    public const int ProductReferenceMaxLength = 128;
    public const int NoteMaxLength = 512;

    private readonly List<BnplFinancingOffer> _offers = [];
    private readonly List<BnplFinancingDecision> _decisions = [];
    private readonly List<BnplInstallmentPlan> _plans = [];

    public BnplFinancingApplicationId Id { get; }
    public Guid OrganizationId { get; }
    public Guid BranchId { get; }
    public Guid CustomerId { get; }
    public BnplFinancingApplicationStatus Status { get; private set; }
    public decimal PurchaseAmount { get; private set; }
    public decimal DownPaymentAmount { get; private set; }
    public decimal RequestedFinanceAmount { get; private set; }
    public string? PurchaseDescription { get; private set; }
    public string? MerchantProductReference { get; private set; }
    public int AggregateVersion { get; private set; }
    public bool EligibilityApproved { get; private set; }
    public DateTimeOffset? EligibilityDecidedAtUtc { get; private set; }
    public Guid? EligibilityDecidedByActorId { get; private set; }
    public string? EligibilityNote { get; private set; }
    public Guid? CurrentOfferId { get; private set; }
    public Guid? AcceptedOfferId { get; private set; }
    public Guid CreatedByActorId { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyList<BnplFinancingOffer> Offers => _offers;
    public IReadOnlyList<BnplFinancingDecision> Decisions => _decisions;
    public IReadOnlyList<BnplInstallmentPlan> InstallmentPlans => _plans;

    public BnplFinancingOffer? CurrentOffer =>
        CurrentOfferId is Guid id
            ? _offers.FirstOrDefault(o => o.Id.Value == id)
            : null;

    public BnplFinancingOffer? AcceptedOffer =>
        AcceptedOfferId is Guid id
            ? _offers.FirstOrDefault(o => o.Id.Value == id)
            : null;

    public BnplInstallmentPlan? CurrentInstallmentPlan =>
        CurrentOfferId is Guid offerId
            ? _plans.FirstOrDefault(p => p.OfferId == offerId && !p.IsSuperseded)
            : null;

    public BnplInstallmentPlan? AcceptedInstallmentPlan =>
        AcceptedOfferId is Guid offerId
            ? _plans.FirstOrDefault(p => p.OfferId == offerId && p.IsLocked && !p.IsSuperseded)
            : null;

    /// <summary>APPROVED_PENDING_SALE creates no debt.</summary>
    public bool HasOutstandingDebt => false;

    /// <summary>Collectible installment debt does not exist before ACTIVE (BNPL-07).</summary>
    public bool HasInstallments => false;

    /// <summary>True when an explicit planned schedule exists for the current or accepted offer.</summary>
    public bool HasPlannedInstallmentSchedule =>
        CurrentInstallmentPlan is not null || AcceptedInstallmentPlan is not null;

    /// <summary>Repayments are unavailable before ACTIVE.</summary>
    public bool AreRepaymentsAllowed => false;

    private BnplFinancingApplication(
        BnplFinancingApplicationId id,
        Guid organizationId,
        Guid branchId,
        Guid customerId,
        BnplFinancingApplicationStatus status,
        decimal purchaseAmount,
        decimal downPaymentAmount,
        decimal requestedFinanceAmount,
        string? purchaseDescription,
        string? merchantProductReference,
        int aggregateVersion,
        bool eligibilityApproved,
        DateTimeOffset? eligibilityDecidedAtUtc,
        Guid? eligibilityDecidedByActorId,
        string? eligibilityNote,
        Guid? currentOfferId,
        Guid? acceptedOfferId,
        Guid createdByActorId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        IEnumerable<BnplFinancingOffer>? offers,
        IEnumerable<BnplFinancingDecision>? decisions,
        IEnumerable<BnplInstallmentPlan>? plans)
    {
        Id = id;
        OrganizationId = organizationId;
        BranchId = branchId;
        CustomerId = customerId;
        Status = status;
        PurchaseAmount = purchaseAmount;
        DownPaymentAmount = downPaymentAmount;
        RequestedFinanceAmount = requestedFinanceAmount;
        PurchaseDescription = purchaseDescription;
        MerchantProductReference = merchantProductReference;
        AggregateVersion = aggregateVersion;
        EligibilityApproved = eligibilityApproved;
        EligibilityDecidedAtUtc = eligibilityDecidedAtUtc;
        EligibilityDecidedByActorId = eligibilityDecidedByActorId;
        EligibilityNote = eligibilityNote;
        CurrentOfferId = currentOfferId;
        AcceptedOfferId = acceptedOfferId;
        CreatedByActorId = createdByActorId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        if (offers is not null)
        {
            _offers.AddRange(offers);
        }

        if (decisions is not null)
        {
            _decisions.AddRange(decisions);
        }

        if (plans is not null)
        {
            _plans.AddRange(plans);
        }
    }

    public static BnplFinancingApplication Create(
        Guid organizationId,
        Guid branchId,
        Guid customerId,
        Guid createdByActorId,
        decimal purchaseAmount,
        decimal downPaymentAmount,
        DateTimeOffset utcNow,
        BnplFinancingApplicationId? applicationId = null,
        string? purchaseDescription = null,
        string? merchantProductReference = null)
    {
        EnsureUtc(utcNow);
        EnsureGuid(organizationId, BnplFinancingErrorCodes.InvalidOrganizationId, "OrganizationId");
        EnsureGuid(branchId, BnplFinancingErrorCodes.InvalidBranchId, "BranchId");
        EnsureGuid(customerId, BnplFinancingErrorCodes.InvalidCustomerId, "CustomerId");
        EnsureGuid(createdByActorId, BnplFinancingErrorCodes.InvalidActorId, "CreatedByActorId");
        var (purchase, down, finance) = NormalizeAmounts(purchaseAmount, downPaymentAmount);

        return new BnplFinancingApplication(
            applicationId ?? BnplFinancingApplicationId.New(),
            organizationId,
            branchId,
            customerId,
            BnplFinancingApplicationStatus.Draft,
            purchase,
            down,
            finance,
            NormalizeOptionalText(purchaseDescription, DescriptionMaxLength),
            NormalizeOptionalText(merchantProductReference, ProductReferenceMaxLength),
            aggregateVersion: 1,
            eligibilityApproved: false,
            eligibilityDecidedAtUtc: null,
            eligibilityDecidedByActorId: null,
            eligibilityNote: null,
            currentOfferId: null,
            acceptedOfferId: null,
            createdByActorId,
            utcNow,
            utcNow,
            offers: null,
            decisions: null,
            plans: null);
    }

    public static BnplFinancingApplication Reconstitute(
        BnplFinancingApplicationId id,
        Guid organizationId,
        Guid branchId,
        Guid customerId,
        BnplFinancingApplicationStatus status,
        decimal purchaseAmount,
        decimal downPaymentAmount,
        decimal requestedFinanceAmount,
        string? purchaseDescription,
        string? merchantProductReference,
        int aggregateVersion,
        bool eligibilityApproved,
        DateTimeOffset? eligibilityDecidedAtUtc,
        Guid? eligibilityDecidedByActorId,
        string? eligibilityNote,
        Guid? currentOfferId,
        Guid? acceptedOfferId,
        Guid createdByActorId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        IEnumerable<BnplFinancingOffer> offers,
        IEnumerable<BnplFinancingDecision> decisions,
        IEnumerable<BnplInstallmentPlan>? plans = null) =>
        new(
            id,
            organizationId,
            branchId,
            customerId,
            status,
            purchaseAmount,
            downPaymentAmount,
            requestedFinanceAmount,
            purchaseDescription,
            merchantProductReference,
            aggregateVersion,
            eligibilityApproved,
            eligibilityDecidedAtUtc,
            eligibilityDecidedByActorId,
            eligibilityNote,
            currentOfferId,
            acceptedOfferId,
            createdByActorId,
            createdAtUtc,
            updatedAtUtc,
            offers,
            decisions,
            plans);

    public bool IsCompatibleCreatePayload(
        Guid organizationId,
        Guid branchId,
        Guid customerId,
        decimal purchaseAmount,
        decimal downPaymentAmount,
        string? purchaseDescription,
        string? merchantProductReference)
    {
        var (purchase, down, finance) = NormalizeAmounts(purchaseAmount, downPaymentAmount);
        return OrganizationId == organizationId
               && BranchId == branchId
               && CustomerId == customerId
               && PurchaseAmount == purchase
               && DownPaymentAmount == down
               && RequestedFinanceAmount == finance
               && string.Equals(
                   PurchaseDescription,
                   NormalizeOptionalText(purchaseDescription, DescriptionMaxLength),
                   StringComparison.Ordinal)
               && string.Equals(
                   MerchantProductReference,
                   NormalizeOptionalText(merchantProductReference, ProductReferenceMaxLength),
                   StringComparison.Ordinal);
    }

    public void EnsureExpectedVersion(int? expectedVersion)
    {
        if (expectedVersion is int expected && expected != AggregateVersion)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.ConcurrencyConflict,
                "Financing application version conflict.");
        }
    }

    public void UpdateDraft(
        decimal purchaseAmount,
        decimal downPaymentAmount,
        string? purchaseDescription,
        string? merchantProductReference,
        DateTimeOffset utcNow,
        int? expectedVersion = null)
    {
        EnsureUtc(utcNow);
        EnsureExpectedVersion(expectedVersion);
        EnsureStatus(BnplFinancingApplicationStatus.Draft);
        var (purchase, down, finance) = NormalizeAmounts(purchaseAmount, downPaymentAmount);
        PurchaseAmount = purchase;
        DownPaymentAmount = down;
        RequestedFinanceAmount = finance;
        PurchaseDescription = NormalizeOptionalText(purchaseDescription, DescriptionMaxLength);
        MerchantProductReference = NormalizeOptionalText(merchantProductReference, ProductReferenceMaxLength);
        Touch(utcNow);
    }

    public void Submit(DateTimeOffset utcNow, int? expectedVersion = null)
    {
        EnsureUtc(utcNow);
        EnsureExpectedVersion(expectedVersion);
        if (Status == BnplFinancingApplicationStatus.PendingEligibility)
        {
            return;
        }

        EnsureStatus(BnplFinancingApplicationStatus.Draft);
        Status = BnplFinancingApplicationStatus.PendingEligibility;
        Touch(utcNow);
    }

    public void ApproveEligibility(
        Guid actorId,
        DateTimeOffset utcNow,
        string? note = null,
        int? expectedVersion = null)
    {
        EnsureUtc(utcNow);
        EnsureGuid(actorId, BnplFinancingErrorCodes.InvalidActorId, "ActorId");
        EnsureExpectedVersion(expectedVersion);

        if (Status == BnplFinancingApplicationStatus.PendingEligibility
            && EligibilityApproved
            && EligibilityDecidedByActorId == actorId)
        {
            return;
        }

        if (Status is BnplFinancingApplicationStatus.Offered
            or BnplFinancingApplicationStatus.CustomerAccepted
            or BnplFinancingApplicationStatus.ApprovedPendingSale)
        {
            if (EligibilityApproved)
            {
                return;
            }
        }

        EnsureStatus(BnplFinancingApplicationStatus.PendingEligibility);
        if (EligibilityDecidedAtUtc is not null && !EligibilityApproved)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.InvalidState,
                "Eligibility was already declined.");
        }

        EligibilityApproved = true;
        EligibilityDecidedAtUtc = utcNow;
        EligibilityDecidedByActorId = actorId;
        EligibilityNote = NormalizeOptionalText(note, NoteMaxLength);
        _decisions.Add(new BnplFinancingDecision(
            Guid.NewGuid(),
            BnplFinancingDecisionStage.Eligibility,
            BnplFinancingDecisionOutcome.Approved,
            actorId,
            utcNow,
            EligibilityNote,
            offerId: null));
        Touch(utcNow);
    }

    public void DeclineEligibility(
        Guid actorId,
        DateTimeOffset utcNow,
        string? note = null,
        int? expectedVersion = null)
    {
        EnsureUtc(utcNow);
        EnsureGuid(actorId, BnplFinancingErrorCodes.InvalidActorId, "ActorId");
        EnsureExpectedVersion(expectedVersion);

        if (Status == BnplFinancingApplicationStatus.Declined
            && EligibilityDecidedAtUtc is not null
            && !EligibilityApproved)
        {
            return;
        }

        EnsureStatus(BnplFinancingApplicationStatus.PendingEligibility);
        EligibilityApproved = false;
        EligibilityDecidedAtUtc = utcNow;
        EligibilityDecidedByActorId = actorId;
        EligibilityNote = NormalizeOptionalText(note, NoteMaxLength);
        Status = BnplFinancingApplicationStatus.Declined;
        _decisions.Add(new BnplFinancingDecision(
            Guid.NewGuid(),
            BnplFinancingDecisionStage.Eligibility,
            BnplFinancingDecisionOutcome.Declined,
            actorId,
            utcNow,
            EligibilityNote,
            offerId: null));
        Touch(utcNow);
    }

    public BnplFinancingOffer CreateOffer(
        Guid actorId,
        DateTimeOffset utcNow,
        BnplFinancingOfferId? offerId = null,
        DateTimeOffset? expiresAtUtc = null,
        int? expectedVersion = null)
    {
        EnsureUtc(utcNow);
        EnsureGuid(actorId, BnplFinancingErrorCodes.InvalidActorId, "ActorId");
        EnsureExpectedVersion(expectedVersion);

        if (offerId is BnplFinancingOfferId supplied)
        {
            var existing = _offers.FirstOrDefault(o => o.Id.Value == supplied.Value);
            if (existing is not null)
            {
                if (!existing.IsCompatibleCreatePayload(
                        PurchaseAmount,
                        DownPaymentAmount,
                        RequestedFinanceAmount,
                        expiresAtUtc))
                {
                    throw new BnplFinancingDomainException(
                        BnplFinancingErrorCodes.IdempotencyConflict,
                        "OfferId already exists with conflicting terms.");
                }

                return existing;
            }
        }

        if (!EligibilityApproved)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.EligibilityRequired,
                "Eligibility must be approved before creating an offer.");
        }

        if (Status is not (BnplFinancingApplicationStatus.PendingEligibility
            or BnplFinancingApplicationStatus.Offered))
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.InvalidState,
                $"Cannot create offer from status {Status}.");
        }

        if (AcceptedOfferId is not null)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.OfferImmutable,
                "Accepted offer terms are immutable; create a new application workflow if terms must change.");
        }

        foreach (var prior in _offers.Where(o => !o.IsSuperseded && !o.IsAccepted))
        {
            prior.MarkSuperseded();
            foreach (var priorPlan in _plans.Where(p => p.OfferId == prior.Id.Value && !p.IsSuperseded))
            {
                priorPlan.MarkSuperseded();
            }
        }

        var version = _offers.Count == 0 ? 1 : _offers.Max(o => o.Version) + 1;
        var offer = BnplFinancingOffer.Create(
            offerId ?? BnplFinancingOfferId.New(),
            version,
            PurchaseAmount,
            DownPaymentAmount,
            RequestedFinanceAmount,
            actorId,
            utcNow,
            expiresAtUtc);
        _offers.Add(offer);
        CurrentOfferId = offer.Id.Value;
        Status = BnplFinancingApplicationStatus.Offered;
        Touch(utcNow);
        return offer;
    }

    /// <summary>
    /// Attach or replace an explicit principal-only installment plan on the current unaccepted offer.
    /// No automatic term/frequency generation (BNPL-D-00-14 OPEN).
    /// </summary>
    public BnplInstallmentPlan AttachOrReplaceInstallmentPlan(
        Guid offerId,
        BnplInstallmentPlanId planId,
        IReadOnlyList<BnplInstallmentPlanItemDraft> items,
        Guid actorId,
        DateTimeOffset utcNow,
        int? expectedVersion = null)
    {
        EnsureUtc(utcNow);
        EnsureGuid(actorId, BnplFinancingErrorCodes.InvalidActorId, "ActorId");
        EnsureExpectedVersion(expectedVersion);

        var existing = _plans.FirstOrDefault(p => p.Id.Value == planId.Value);
        if (existing is not null)
        {
            var offerForExisting = _offers.FirstOrDefault(o => o.Id.Value == offerId)
                ?? throw new BnplFinancingDomainException(
                    BnplFinancingErrorCodes.NotFound,
                    "Offer was not found on this application.");

            if (!existing.IsCompatiblePayload(offerId, offerForExisting.FinancedPrincipal, items))
            {
                throw new BnplFinancingDomainException(
                    BnplFinancingErrorCodes.IdempotencyConflict,
                    "PlanId already exists with a conflicting schedule.");
            }

            return existing;
        }

        var offer = _offers.FirstOrDefault(o => o.Id.Value == offerId)
            ?? throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.NotFound,
                "Offer was not found on this application.");

        if (offer.IsAccepted || AcceptedOfferId == offerId)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.PlanImmutable,
                "An accepted installment plan cannot be replaced.");
        }

        if (Status != BnplFinancingApplicationStatus.Offered)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.InvalidState,
                $"Installment plan may only be attached when status is Offered (was {Status}).");
        }

        if (CurrentOfferId != offerId)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.OfferSuperseded,
                "Installment plan may only be attached to the current offer.");
        }

        if (offer.IsSuperseded)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.PlanImmutable,
                "Cannot attach a plan to a superseded offer.");
        }

        foreach (var prior in _plans.Where(p => p.OfferId == offerId && !p.IsSuperseded))
        {
            prior.MarkSuperseded();
        }

        var planVersion = _plans.Count(p => p.OfferId == offerId) + 1;
        var plan = BnplInstallmentPlan.Create(
            planId,
            offerId,
            planVersion,
            actorId,
            utcNow,
            offer.FinancedPrincipal,
            items);
        _plans.Add(plan);
        Touch(utcNow);
        return plan;
    }

    public BnplInstallmentPlan? GetInstallmentPlanForOffer(Guid offerId) =>
        _plans
            .Where(p => p.OfferId == offerId)
            .OrderByDescending(p => p.Version)
            .FirstOrDefault(p => !p.IsSuperseded)
        ?? _plans
            .Where(p => p.OfferId == offerId)
            .OrderByDescending(p => p.Version)
            .FirstOrDefault();

    public void AcceptOffer(
        Guid offerId,
        Guid actorId,
        DateTimeOffset utcNow,
        int? expectedVersion = null)
    {
        EnsureUtc(utcNow);
        EnsureGuid(actorId, BnplFinancingErrorCodes.InvalidActorId, "ActorId");
        EnsureExpectedVersion(expectedVersion);

        if (Status == BnplFinancingApplicationStatus.CustomerAccepted
            && AcceptedOfferId == offerId)
        {
            return;
        }

        if (Status == BnplFinancingApplicationStatus.ApprovedPendingSale
            && AcceptedOfferId == offerId)
        {
            return;
        }

        EnsureStatus(BnplFinancingApplicationStatus.Offered);
        var offer = _offers.FirstOrDefault(o => o.Id.Value == offerId)
            ?? throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.NotFound,
                "Offer was not found on this application.");

        if (CurrentOfferId != offerId)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.OfferSuperseded,
                "Only the current offer version can be accepted.");
        }

        var plan = _plans.FirstOrDefault(p => p.OfferId == offerId && !p.IsSuperseded);
        if (plan is null)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.PlanRequired,
                "Customer acceptance requires a valid installment plan on the current offer.");
        }

        if (plan.TotalScheduledPrincipal != offer.FinancedPrincipal)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.PlanTotalMismatch,
                "Installment plan total must equal FinancedPrincipal before acceptance.");
        }

        offer.MarkAccepted(actorId, utcNow);
        plan.MarkLocked();
        AcceptedOfferId = offer.Id.Value;
        Status = BnplFinancingApplicationStatus.CustomerAccepted;
        Touch(utcNow);
    }

    public void Approve(
        Guid actorId,
        DateTimeOffset utcNow,
        string? note = null,
        int? expectedVersion = null)
    {
        EnsureUtc(utcNow);
        EnsureGuid(actorId, BnplFinancingErrorCodes.InvalidActorId, "ActorId");
        EnsureExpectedVersion(expectedVersion);

        if (Status == BnplFinancingApplicationStatus.ApprovedPendingSale)
        {
            return;
        }

        EnsureStatus(BnplFinancingApplicationStatus.CustomerAccepted);
        if (AcceptedOfferId is null)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.InvalidState,
                "An accepted offer is required before approval.");
        }

        var acceptedOffer = AcceptedOffer
            ?? throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.InvalidState,
                "Accepted offer was not found.");

        var plan = _plans.FirstOrDefault(p => p.OfferId == AcceptedOfferId.Value && p.IsLocked && !p.IsSuperseded);
        if (plan is null)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.PlanRequired,
                "Approval requires an accepted offer with an immutable installment plan. Legacy principal-only offers without a schedule cannot be approved for BNPL-07.");
        }

        if (plan.TotalScheduledPrincipal != acceptedOffer.FinancedPrincipal)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.PlanTotalMismatch,
                "Accepted installment plan total must equal FinancedPrincipal.");
        }

        Status = BnplFinancingApplicationStatus.ApprovedPendingSale;
        _decisions.Add(new BnplFinancingDecision(
            Guid.NewGuid(),
            BnplFinancingDecisionStage.Approval,
            BnplFinancingDecisionOutcome.Approved,
            actorId,
            utcNow,
            NormalizeOptionalText(note, NoteMaxLength),
            AcceptedOfferId));
        Touch(utcNow);
    }

    public void DeclineApproval(
        Guid actorId,
        DateTimeOffset utcNow,
        string? note = null,
        int? expectedVersion = null)
    {
        EnsureUtc(utcNow);
        EnsureGuid(actorId, BnplFinancingErrorCodes.InvalidActorId, "ActorId");
        EnsureExpectedVersion(expectedVersion);

        if (Status == BnplFinancingApplicationStatus.Declined)
        {
            return;
        }

        EnsureStatus(BnplFinancingApplicationStatus.CustomerAccepted);
        Status = BnplFinancingApplicationStatus.Declined;
        _decisions.Add(new BnplFinancingDecision(
            Guid.NewGuid(),
            BnplFinancingDecisionStage.Approval,
            BnplFinancingDecisionOutcome.Declined,
            actorId,
            utcNow,
            NormalizeOptionalText(note, NoteMaxLength),
            AcceptedOfferId));
        Touch(utcNow);
    }

    public void Cancel(
        Guid actorId,
        DateTimeOffset utcNow,
        string? note = null,
        int? expectedVersion = null)
    {
        EnsureUtc(utcNow);
        EnsureGuid(actorId, BnplFinancingErrorCodes.InvalidActorId, "ActorId");
        EnsureExpectedVersion(expectedVersion);

        if (Status == BnplFinancingApplicationStatus.Cancelled)
        {
            return;
        }

        if (Status is BnplFinancingApplicationStatus.Declined)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.InvalidState,
                "Declined applications cannot be cancelled.");
        }

        Status = BnplFinancingApplicationStatus.Cancelled;
        _decisions.Add(new BnplFinancingDecision(
            Guid.NewGuid(),
            BnplFinancingDecisionStage.Cancellation,
            BnplFinancingDecisionOutcome.Cancelled,
            actorId,
            utcNow,
            NormalizeOptionalText(note, NoteMaxLength),
            AcceptedOfferId ?? CurrentOfferId));
        Touch(utcNow);
    }

    /// <summary>ACTIVE is prohibited in BNPL-04. Always throws.</summary>
    public void ActivateProhibited()
    {
        throw new BnplFinancingDomainException(
            BnplFinancingErrorCodes.ActiveProhibited,
            "ACTIVE financing requires Commerce sale orchestration (BNPL-07) and is not available.");
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAtUtc = utcNow;
        AggregateVersion++;
    }

    private void EnsureStatus(BnplFinancingApplicationStatus expected)
    {
        if (Status != expected)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.InvalidState,
                $"Expected status {expected} but was {Status}.");
        }
    }

    private static (decimal Purchase, decimal Down, decimal Finance) NormalizeAmounts(
        decimal purchaseAmount,
        decimal downPaymentAmount)
    {
        if (purchaseAmount < 0 || downPaymentAmount < 0)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.InvalidAmount,
                "Amounts must be non-negative.");
        }

        if (downPaymentAmount > purchaseAmount)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.InvalidAmount,
                "Down payment cannot exceed purchase amount.");
        }

        var purchase = decimal.Round(purchaseAmount, 2, MidpointRounding.AwayFromZero);
        var down = decimal.Round(downPaymentAmount, 2, MidpointRounding.AwayFromZero);
        var finance = decimal.Round(purchase - down, 2, MidpointRounding.AwayFromZero);
        return (purchase, down, finance);
    }

    private static void EnsureGuid(Guid value, string errorCode, string name)
    {
        if (value == Guid.Empty)
        {
            throw new BnplFinancingDomainException(errorCode, $"{name} must be a non-empty Guid.");
        }
    }

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.InvalidAmount,
                "Timestamps must be UTC.");
        }
    }

    private static string? NormalizeOptionalText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.InvalidAmount,
                $"Text exceeds maximum length of {maxLength}.");
        }

        return trimmed;
    }
}
