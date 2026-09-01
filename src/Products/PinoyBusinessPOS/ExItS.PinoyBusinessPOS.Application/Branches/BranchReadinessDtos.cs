namespace ExItS.PinoyBusinessPOS.Application.Branches;

public enum BranchReadinessSectionStatus
{
    Complete,
    NeedsAttention,
    Optional,
    NotApplicable,
}

public enum BranchReadinessOverallStatus
{
    NotStarted,
    NeedsAttention,
    Ready,
}

public sealed record BranchReadinessSectionDto(
    string Key,
    BranchReadinessSectionStatus Status,
    string? Summary,
    int? Count,
    string? ManagementPath);

public sealed record BranchReadinessDto(
    Guid OrganizationId,
    Guid BranchId,
    BranchReadinessOverallStatus OverallStatus,
    IReadOnlyList<BranchReadinessSectionDto> Sections,
    BranchSetupProgressDto? SetupProgress);

public sealed record BranchSetupProgressDto(
    string? LastVisitedStep,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? LastVisitedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record UpsertBranchSetupProgressRequest(
    string? LastVisitedStep,
    bool MarkCompleted = false);

public sealed record GrantPartyBranchAccessRequest(Guid BranchId);
