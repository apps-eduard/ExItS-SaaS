namespace ExItS.PinoyBuyNowPayLater.Infrastructure.Persistence.Financing;

internal sealed class BnplFinancingApplicationRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal PurchaseAmount { get; set; }
    public decimal DownPaymentAmount { get; set; }
    public decimal RequestedFinanceAmount { get; set; }
    public string? PurchaseDescription { get; set; }
    public string? MerchantProductReference { get; set; }
    public int AggregateVersion { get; set; }
    public bool EligibilityApproved { get; set; }
    public DateTimeOffset? EligibilityDecidedAtUtc { get; set; }
    public Guid? EligibilityDecidedByActorId { get; set; }
    public string? EligibilityNote { get; set; }
    public Guid? CurrentOfferId { get; set; }
    public Guid? AcceptedOfferId { get; set; }
    public Guid CreatedByActorId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }

    public List<BnplFinancingOfferRecord> Offers { get; set; } = [];
    public List<BnplFinancingDecisionRecord> Decisions { get; set; } = [];
    public List<BnplInstallmentPlanRecord> InstallmentPlans { get; set; } = [];
}

internal sealed class BnplFinancingOfferRecord
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public int Version { get; set; }
    public decimal PurchaseAmount { get; set; }
    public decimal DownPaymentAmount { get; set; }
    public decimal FinancedPrincipal { get; set; }
    public Guid CreatedByActorId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public bool IsSuperseded { get; set; }
    public DateTimeOffset? AcceptedAtUtc { get; set; }
    public Guid? AcceptedByActorId { get; set; }

    public BnplFinancingApplicationRecord? Application { get; set; }
}

internal sealed class BnplFinancingDecisionRecord
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string Stage { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public Guid ActorId { get; set; }
    public DateTimeOffset DecidedAtUtc { get; set; }
    public string? Note { get; set; }
    public Guid? OfferId { get; set; }

    public BnplFinancingApplicationRecord? Application { get; set; }
}

internal sealed class BnplInstallmentPlanRecord
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid OfferId { get; set; }
    public int Version { get; set; }
    public Guid CreatedByActorId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public bool IsSuperseded { get; set; }
    public bool IsLocked { get; set; }

    public BnplFinancingApplicationRecord? Application { get; set; }
    public List<BnplInstallmentPlanItemRecord> Items { get; set; } = [];
}

internal sealed class BnplInstallmentPlanItemRecord
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public int SequenceNumber { get; set; }
    public decimal PrincipalAmount { get; set; }
    public DateOnly DueDate { get; set; }

    public BnplInstallmentPlanRecord? Plan { get; set; }
}
