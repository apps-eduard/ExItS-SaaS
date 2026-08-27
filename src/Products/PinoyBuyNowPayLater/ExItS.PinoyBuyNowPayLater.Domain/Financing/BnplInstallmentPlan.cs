namespace ExItS.PinoyBuyNowPayLater.Domain.Financing;

public readonly record struct BnplInstallmentPlanId(Guid Value)
{
    public static BnplInstallmentPlanId New() => new(Guid.NewGuid());

    public static BnplInstallmentPlanId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.InvalidPlanId,
                "InstallmentPlanId must be a non-empty Guid.");
        }

        return new BnplInstallmentPlanId(value);
    }
}

public readonly record struct BnplInstallmentPlanItemId(Guid Value)
{
    public static BnplInstallmentPlanItemId New() => new(Guid.NewGuid());

    public static BnplInstallmentPlanItemId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.InvalidPlanItemId,
                "InstallmentPlanItemId must be a non-empty Guid.");
        }

        return new BnplInstallmentPlanItemId(value);
    }
}

/// <summary>
/// Explicit principal-only installment row. Due dates are caller-supplied business dates (BNPL-D-00-14 OPEN).
/// </summary>
public sealed class BnplInstallmentPlanItem
{
    public BnplInstallmentPlanItemId Id { get; }
    public int SequenceNumber { get; }
    public decimal PrincipalAmount { get; }
    public DateOnly DueDate { get; }

    public BnplInstallmentPlanItem(
        BnplInstallmentPlanItemId id,
        int sequenceNumber,
        decimal principalAmount,
        DateOnly dueDate)
    {
        Id = id;
        SequenceNumber = sequenceNumber;
        PrincipalAmount = principalAmount;
        DueDate = dueDate;
    }
}

/// <summary>
/// Explicit installment plan attached to a financing offer. Not collectible debt until ACTIVE (BNPL-07).
/// No automatic frequency/term generator (BNPL-D-00-14 OPEN). Principal-only (BNPL-D-00-15 OPEN).
/// </summary>
public sealed class BnplInstallmentPlan
{
    public const int MaxItems = 120;

    private readonly List<BnplInstallmentPlanItem> _items;

    public BnplInstallmentPlanId Id { get; }
    public Guid OfferId { get; }
    public int Version { get; }
    public Guid CreatedByActorId { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public bool IsSuperseded { get; private set; }
    public IReadOnlyList<BnplInstallmentPlanItem> Items => _items;
    public decimal TotalScheduledPrincipal { get; }

    /// <summary>True when the owning offer has been accepted — plan must not mutate.</summary>
    public bool IsLocked { get; private set; }

    private BnplInstallmentPlan(
        BnplInstallmentPlanId id,
        Guid offerId,
        int version,
        Guid createdByActorId,
        DateTimeOffset createdAtUtc,
        bool isSuperseded,
        bool isLocked,
        IReadOnlyList<BnplInstallmentPlanItem> items)
    {
        Id = id;
        OfferId = offerId;
        Version = version;
        CreatedByActorId = createdByActorId;
        CreatedAtUtc = createdAtUtc;
        IsSuperseded = isSuperseded;
        IsLocked = isLocked;
        _items = items.OrderBy(i => i.SequenceNumber).ToList();
        TotalScheduledPrincipal = decimal.Round(_items.Sum(i => i.PrincipalAmount), 2, MidpointRounding.AwayFromZero);
    }

    public static BnplInstallmentPlan Create(
        BnplInstallmentPlanId id,
        Guid offerId,
        int version,
        Guid createdByActorId,
        DateTimeOffset createdAtUtc,
        decimal financedPrincipal,
        IReadOnlyList<BnplInstallmentPlanItemDraft> itemDrafts)
    {
        var items = ValidateAndBuildItems(itemDrafts, financedPrincipal);
        return new BnplInstallmentPlan(
            id,
            offerId,
            version,
            createdByActorId,
            createdAtUtc,
            isSuperseded: false,
            isLocked: false,
            items);
    }

    public static BnplInstallmentPlan Reconstitute(
        BnplInstallmentPlanId id,
        Guid offerId,
        int version,
        Guid createdByActorId,
        DateTimeOffset createdAtUtc,
        bool isSuperseded,
        bool isLocked,
        IReadOnlyList<BnplInstallmentPlanItem> items) =>
        new(id, offerId, version, createdByActorId, createdAtUtc, isSuperseded, isLocked, items);

    public bool IsCompatiblePayload(
        Guid offerId,
        decimal financedPrincipal,
        IReadOnlyList<BnplInstallmentPlanItemDraft> itemDrafts)
    {
        if (OfferId != offerId || IsSuperseded)
        {
            return false;
        }

        try
        {
            var expected = ValidateAndBuildItems(itemDrafts, financedPrincipal);
            if (expected.Count != _items.Count)
            {
                return false;
            }

            for (var i = 0; i < expected.Count; i++)
            {
                var a = expected[i];
                var b = _items[i];
                if (a.Id.Value != b.Id.Value
                    || a.SequenceNumber != b.SequenceNumber
                    || a.PrincipalAmount != b.PrincipalAmount
                    || a.DueDate != b.DueDate)
                {
                    return false;
                }
            }

            return true;
        }
        catch (BnplFinancingDomainException)
        {
            return false;
        }
    }

    internal void MarkSuperseded()
    {
        if (IsLocked)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.PlanImmutable,
                "An accepted installment plan cannot be superseded.");
        }

        IsSuperseded = true;
    }

    internal void MarkLocked()
    {
        IsLocked = true;
    }

    public static IReadOnlyList<BnplInstallmentPlanItem> ValidateAndBuildItems(
        IReadOnlyList<BnplInstallmentPlanItemDraft> drafts,
        decimal financedPrincipal)
    {
        if (drafts is null || drafts.Count == 0)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.PlanEmpty,
                "Installment plan must contain at least one item.");
        }

        if (drafts.Count > MaxItems)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.PlanTooLarge,
                $"Installment plan cannot exceed {MaxItems} items.");
        }

        var itemIds = new HashSet<Guid>();
        var sequences = new HashSet<int>();
        var items = new List<BnplInstallmentPlanItem>(drafts.Count);
        DateOnly? previousDue = null;

        foreach (var draft in drafts.OrderBy(d => d.SequenceNumber))
        {
            if (draft.ItemId == Guid.Empty)
            {
                throw new BnplFinancingDomainException(
                    BnplFinancingErrorCodes.InvalidPlanItemId,
                    "InstallmentPlanItemId must be a non-empty Guid.");
            }

            if (!itemIds.Add(draft.ItemId))
            {
                throw new BnplFinancingDomainException(
                    BnplFinancingErrorCodes.DuplicatePlanItemId,
                    "Duplicate InstallmentPlanItemId in plan.");
            }

            if (draft.SequenceNumber < 1 || !sequences.Add(draft.SequenceNumber))
            {
                throw new BnplFinancingDomainException(
                    BnplFinancingErrorCodes.InvalidPlanSequence,
                    "SequenceNumber must be unique and >= 1.");
            }

            if (draft.PrincipalAmount <= 0)
            {
                throw new BnplFinancingDomainException(
                    BnplFinancingErrorCodes.InvalidPlanAmount,
                    "Installment PrincipalAmount must be greater than zero.");
            }

            var amount = decimal.Round(draft.PrincipalAmount, 2, MidpointRounding.AwayFromZero);
            if (amount != draft.PrincipalAmount)
            {
                throw new BnplFinancingDomainException(
                    BnplFinancingErrorCodes.InvalidPlanAmount,
                    "Installment PrincipalAmount must have at most 2 decimal places.");
            }

            if (previousDue is DateOnly prev && draft.DueDate <= prev)
            {
                throw new BnplFinancingDomainException(
                    BnplFinancingErrorCodes.InvalidPlanDueDate,
                    "DueDates must be strictly increasing.");
            }

            previousDue = draft.DueDate;
            items.Add(new BnplInstallmentPlanItem(
                BnplInstallmentPlanItemId.From(draft.ItemId),
                draft.SequenceNumber,
                amount,
                draft.DueDate));
        }

        for (var i = 1; i <= items.Count; i++)
        {
            if (!sequences.Contains(i))
            {
                throw new BnplFinancingDomainException(
                    BnplFinancingErrorCodes.InvalidPlanSequence,
                    "SequenceNumber values must be contiguous 1..N.");
            }
        }

        var total = decimal.Round(items.Sum(i => i.PrincipalAmount), 2, MidpointRounding.AwayFromZero);
        var expected = decimal.Round(financedPrincipal, 2, MidpointRounding.AwayFromZero);
        if (total != expected)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.PlanTotalMismatch,
                "Sum of installment principal amounts must exactly equal FinancedPrincipal.");
        }

        return items.OrderBy(i => i.SequenceNumber).ToList();
    }
}

public sealed record BnplInstallmentPlanItemDraft(
    Guid ItemId,
    int SequenceNumber,
    decimal PrincipalAmount,
    DateOnly DueDate);
