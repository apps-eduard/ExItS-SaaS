using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Projections;

public enum ProjectionApplyOutcome
{
    Applied = 1,
    DuplicateIgnored = 2,
    OlderVersionIgnored = 3,
    VersionGapDetected = 4,
    UnsupportedVersion = 5,
    InvalidPayload = 6,
    Conflict = 7,
    ReconciliationRequired = 8
}

/// <summary>Product-local projection health — not authoritative subscription status.</summary>
public enum ProjectionConsumerState
{
    NeverInitialized = 1,
    Current = 2,
    Stale = 3,
    Invalid = 4,
    ReconciliationRequired = 5
}

public sealed class ProjectionCheckpoint
{
    public string ConsumerName { get; }
    public string ContractName { get; }
    public PlatformOrganizationId? OrganizationId { get; }
    public ProductCode? ProductCode { get; }
    public int? LastAppliedSourceVersion { get; }
    public Guid? LastAppliedMessageId { get; }
    public DateTimeOffset? LastAppliedAtUtc { get; }

    public ProjectionCheckpoint(
        string consumerName,
        string contractName,
        PlatformOrganizationId? organizationId,
        ProductCode? productCode,
        int? lastAppliedSourceVersion,
        Guid? lastAppliedMessageId,
        DateTimeOffset? lastAppliedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(consumerName))
        {
            throw new ArgumentException("Consumer name is required.", nameof(consumerName));
        }

        if (string.IsNullOrWhiteSpace(contractName))
        {
            throw new ArgumentException("Contract name is required.", nameof(contractName));
        }

        if (lastAppliedAtUtc is not null && lastAppliedAtUtc.Value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("LastAppliedAt must be UTC.", nameof(lastAppliedAtUtc));
        }

        ConsumerName = consumerName.Trim();
        ContractName = contractName.Trim();
        OrganizationId = organizationId;
        ProductCode = productCode;
        LastAppliedSourceVersion = lastAppliedSourceVersion;
        LastAppliedMessageId = lastAppliedMessageId;
        LastAppliedAtUtc = lastAppliedAtUtc;
    }

    public static ProjectionCheckpoint Empty(string consumerName, string contractName) =>
        new(consumerName, contractName, null, null, null, null, null);

    public ProjectionCheckpoint WithApplied(
        int sourceVersion,
        Guid messageId,
        DateTimeOffset appliedAtUtc,
        PlatformOrganizationId? organizationId,
        ProductCode? productCode) =>
        new(ConsumerName, ContractName, organizationId, productCode, sourceVersion, messageId, appliedAtUtc);
}

public sealed class ProjectionApplyResult
{
    public ProjectionApplyOutcome Outcome { get; }
    public ProjectionConsumerState ConsumerState { get; }
    public string? Detail { get; }
    public ProjectionCheckpoint? UpdatedCheckpoint { get; }

    private ProjectionApplyResult(
        ProjectionApplyOutcome outcome,
        ProjectionConsumerState consumerState,
        string? detail,
        ProjectionCheckpoint? updatedCheckpoint)
    {
        Outcome = outcome;
        ConsumerState = consumerState;
        Detail = detail;
        UpdatedCheckpoint = updatedCheckpoint;
    }

    public static ProjectionApplyResult Applied(ProjectionCheckpoint checkpoint) =>
        new(ProjectionApplyOutcome.Applied, ProjectionConsumerState.Current, null, checkpoint);

    public static ProjectionApplyResult Duplicate(ProjectionCheckpoint checkpoint) =>
        new(ProjectionApplyOutcome.DuplicateIgnored, ProjectionConsumerState.Current, "Duplicate message ID.", checkpoint);

    public static ProjectionApplyResult Older(ProjectionCheckpoint checkpoint) =>
        new(ProjectionApplyOutcome.OlderVersionIgnored, ProjectionConsumerState.Current, "Older source version ignored.", checkpoint);

    public static ProjectionApplyResult Gap(ProjectionCheckpoint checkpoint) =>
        new(ProjectionApplyOutcome.VersionGapDetected, ProjectionConsumerState.ReconciliationRequired, "Source version gap detected.", checkpoint);

    public static ProjectionApplyResult Unsupported(string detail) =>
        new(ProjectionApplyOutcome.UnsupportedVersion, ProjectionConsumerState.Invalid, detail, null);

    public static ProjectionApplyResult Conflict(ProjectionCheckpoint checkpoint) =>
        new(ProjectionApplyOutcome.Conflict, ProjectionConsumerState.ReconciliationRequired, "Same source version with different message.", checkpoint);

    public static ProjectionApplyResult ReconciliationRequired(ProjectionCheckpoint checkpoint, string detail) =>
        new(ProjectionApplyOutcome.ReconciliationRequired, ProjectionConsumerState.ReconciliationRequired, detail, checkpoint);
}
