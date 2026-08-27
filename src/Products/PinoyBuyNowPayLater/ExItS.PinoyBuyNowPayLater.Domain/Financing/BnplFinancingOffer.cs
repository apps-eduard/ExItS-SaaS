namespace ExItS.PinoyBuyNowPayLater.Domain.Financing;

/// <summary>
/// Concrete financing offer. Principal-only — no interest/fee/term engine (BNPL-D-00-14/15 OPEN).
/// Accepted offers are immutable historical evidence.
/// </summary>
public sealed class BnplFinancingOffer
{
    public BnplFinancingOfferId Id { get; }
    public int Version { get; }
    public decimal PurchaseAmount { get; }
    public decimal DownPaymentAmount { get; }
    public decimal FinancedPrincipal { get; }
    public Guid CreatedByActorId { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? ExpiresAtUtc { get; }
    public bool IsSuperseded { get; private set; }
    public DateTimeOffset? AcceptedAtUtc { get; private set; }
    public Guid? AcceptedByActorId { get; private set; }

    public bool IsAccepted => AcceptedAtUtc.HasValue;

    private BnplFinancingOffer(
        BnplFinancingOfferId id,
        int version,
        decimal purchaseAmount,
        decimal downPaymentAmount,
        decimal financedPrincipal,
        Guid createdByActorId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? expiresAtUtc,
        bool isSuperseded,
        DateTimeOffset? acceptedAtUtc,
        Guid? acceptedByActorId)
    {
        Id = id;
        Version = version;
        PurchaseAmount = purchaseAmount;
        DownPaymentAmount = downPaymentAmount;
        FinancedPrincipal = financedPrincipal;
        CreatedByActorId = createdByActorId;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        IsSuperseded = isSuperseded;
        AcceptedAtUtc = acceptedAtUtc;
        AcceptedByActorId = acceptedByActorId;
    }

    public static BnplFinancingOffer Create(
        BnplFinancingOfferId id,
        int version,
        decimal purchaseAmount,
        decimal downPaymentAmount,
        decimal financedPrincipal,
        Guid createdByActorId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? expiresAtUtc)
    {
        return new BnplFinancingOffer(
            id,
            version,
            purchaseAmount,
            downPaymentAmount,
            financedPrincipal,
            createdByActorId,
            createdAtUtc,
            expiresAtUtc,
            isSuperseded: false,
            acceptedAtUtc: null,
            acceptedByActorId: null);
    }

    public static BnplFinancingOffer Reconstitute(
        BnplFinancingOfferId id,
        int version,
        decimal purchaseAmount,
        decimal downPaymentAmount,
        decimal financedPrincipal,
        Guid createdByActorId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? expiresAtUtc,
        bool isSuperseded,
        DateTimeOffset? acceptedAtUtc,
        Guid? acceptedByActorId) =>
        new(
            id,
            version,
            purchaseAmount,
            downPaymentAmount,
            financedPrincipal,
            createdByActorId,
            createdAtUtc,
            expiresAtUtc,
            isSuperseded,
            acceptedAtUtc,
            acceptedByActorId);

    internal void MarkSuperseded()
    {
        if (IsAccepted)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.OfferImmutable,
                "An accepted offer cannot be superseded.");
        }

        IsSuperseded = true;
    }

    internal void MarkAccepted(Guid actorId, DateTimeOffset utcNow)
    {
        if (IsSuperseded)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.OfferSuperseded,
                "A superseded offer cannot be accepted.");
        }

        if (IsAccepted)
        {
            return;
        }

        if (ExpiresAtUtc is DateTimeOffset expires && utcNow > expires)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.OfferExpired,
                "Offer has expired.");
        }

        AcceptedAtUtc = utcNow;
        AcceptedByActorId = actorId;
    }

    public bool IsCompatibleCreatePayload(
        decimal purchaseAmount,
        decimal downPaymentAmount,
        decimal financedPrincipal,
        DateTimeOffset? expiresAtUtc) =>
        PurchaseAmount == purchaseAmount
        && DownPaymentAmount == downPaymentAmount
        && FinancedPrincipal == financedPrincipal
        && ExpiresAtUtc == expiresAtUtc;
}

/// <summary>Immutable decision/history entry for audit reconstruction.</summary>
public sealed class BnplFinancingDecision
{
    public Guid DecisionId { get; }
    public BnplFinancingDecisionStage Stage { get; }
    public BnplFinancingDecisionOutcome Outcome { get; }
    public Guid ActorId { get; }
    public DateTimeOffset DecidedAtUtc { get; }
    public string? Note { get; }
    public Guid? OfferId { get; }

    public BnplFinancingDecision(
        Guid decisionId,
        BnplFinancingDecisionStage stage,
        BnplFinancingDecisionOutcome outcome,
        Guid actorId,
        DateTimeOffset decidedAtUtc,
        string? note,
        Guid? offerId)
    {
        DecisionId = decisionId == Guid.Empty ? Guid.NewGuid() : decisionId;
        Stage = stage;
        Outcome = outcome;
        ActorId = actorId;
        DecidedAtUtc = decidedAtUtc;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        OfferId = offerId;
    }
}
