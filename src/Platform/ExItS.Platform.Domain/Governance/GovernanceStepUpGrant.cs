using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Domain.Governance;

/// <summary>
/// One-time password step-up grant for a scoped organization governance mutation.
/// Only the token hash is persisted — never the raw token or password.
/// </summary>
public sealed class GovernanceStepUpGrant
{
    public GovernanceStepUpGrantId Id { get; }
    public PlatformUserId UserId { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public string ActionCode { get; }
    public string TargetType { get; }
    public Guid? TargetId { get; }
    public string TokenHash { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset ExpiresAtUtc { get; }
    public DateTimeOffset? ConsumedAtUtc { get; private set; }

    private GovernanceStepUpGrant(
        GovernanceStepUpGrantId id,
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        string actionCode,
        string targetType,
        Guid? targetId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? consumedAtUtc)
    {
        Id = id;
        UserId = userId;
        OrganizationId = organizationId;
        ActionCode = actionCode;
        TargetType = targetType;
        TargetId = targetId;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        ConsumedAtUtc = consumedAtUtc;
    }

    public static GovernanceStepUpGrant Create(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        string actionCode,
        string targetType,
        Guid? targetId,
        string tokenHash,
        DateTimeOffset utcNow,
        TimeSpan lifetime,
        GovernanceStepUpGrantId? id = null)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(organizationId);
        DomainTime.EnsureUtc(utcNow);
        if (lifetime <= TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Step-up grant lifetime must be positive.");
        }

        if (string.IsNullOrWhiteSpace(actionCode) || actionCode.Length > 128)
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Action code is invalid.");
        }

        if (string.IsNullOrWhiteSpace(targetType) || targetType.Length > 64)
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Target type is invalid.");
        }

        if (string.IsNullOrWhiteSpace(tokenHash) || tokenHash.Length > 128)
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Token hash is invalid.");
        }

        return new GovernanceStepUpGrant(
            id ?? GovernanceStepUpGrantId.New(),
            userId,
            organizationId,
            actionCode.Trim(),
            targetType.Trim(),
            targetId,
            tokenHash.Trim(),
            utcNow,
            utcNow.Add(lifetime),
            consumedAtUtc: null);
    }

    public static GovernanceStepUpGrant Rehydrate(
        GovernanceStepUpGrantId id,
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        string actionCode,
        string targetType,
        Guid? targetId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? consumedAtUtc) =>
        new(id, userId, organizationId, actionCode, targetType, targetId, tokenHash, createdAtUtc, expiresAtUtc, consumedAtUtc);

    public bool IsRedeemable(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        return ConsumedAtUtc is null && ExpiresAtUtc > utcNow;
    }

    public void Consume(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (!IsRedeemable(utcNow))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidAccountStatusTransition,
                "Governance step-up grant is expired or already consumed.");
        }

        ConsumedAtUtc = utcNow;
    }

    public bool MatchesScope(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        string actionCode,
        string targetType,
        Guid? targetId) =>
        UserId == userId
        && OrganizationId == organizationId
        && string.Equals(ActionCode, actionCode, StringComparison.Ordinal)
        && string.Equals(TargetType, targetType, StringComparison.Ordinal)
        && TargetId == targetId;
}
