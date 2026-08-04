using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Application.Identity;

public sealed record PublicIdentityDto(
    string PublicUserId,
    string QrPayload,
    string DisplayName,
    string Status);

public sealed record ResolvePublicUserIdRequest(
    string PublicUserIdOrQrPayload,
    string? Purpose = null);

public sealed record ResolvedPublicUserDto(
    string PublicUserId,
    Guid UserIdentityId,
    string DisplayName,
    string? MaskedEmail,
    string Status,
    bool IsSelf);

/// <summary>Ensures the caller has an immutable public ExItS ID and returns QR payload data.</summary>
public sealed class GetOrAssignPublicIdentity
{
    private readonly IPlatformUserRepository _users;
    private readonly IPublicUserIdGenerator _generator;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IAuditWriter _audit;

    public GetOrAssignPublicIdentity(
        IPlatformUserRepository users,
        IPublicUserIdGenerator generator,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IAuditWriter audit)
    {
        _users = users;
        _generator = generator;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _audit = audit;
    }

    public async Task<ApplicationResult<PublicIdentityDto>> ExecuteAsync(
        PlatformUserId userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
            if (user is null)
            {
                return ApplicationResult<PublicIdentityDto>.Failure(
                    ApplicationErrorCodes.UserNotFound,
                    "User was not found.");
            }

            if (string.IsNullOrWhiteSpace(user.PublicUserId))
            {
                var assigned = false;
                for (var attempt = 0; attempt < 8 && !assigned; attempt++)
                {
                    var candidate = await _generator.GenerateUniqueAsync(cancellationToken).ConfigureAwait(false);
                    user.AssignPublicUserId(candidate, _clock.UtcNow);
                    await _users.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
                    try
                    {
                        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                        assigned = true;
                        await _audit.WriteAsync(
                            $"platform-user:{userId.Value:D}",
                            AuditActorType.PlatformUser,
                            PlatformAuditActions.PlatformUserPublicIdAssigned,
                            nameof(PlatformUser),
                            userId.Value.ToString("D"),
                            AuditOutcome.Succeeded,
                            summary: "Public ExItS ID assigned.",
                            cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Collision race — reload and retry with a new candidate.
                        user = await _users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
                        if (user is null)
                        {
                            return ApplicationResult<PublicIdentityDto>.Failure(
                                ApplicationErrorCodes.UserNotFound,
                                "User was not found.");
                        }

                        if (!string.IsNullOrWhiteSpace(user.PublicUserId))
                        {
                            assigned = true;
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(user.PublicUserId))
                {
                    return ApplicationResult<PublicIdentityDto>.Failure(
                        ApplicationErrorCodes.ConcurrencyConflict,
                        "Could not allocate an ExItS ID.");
                }
            }

            return ApplicationResult<PublicIdentityDto>.Success(new PublicIdentityDto(
                user.PublicUserId!,
                PublicUserIdRules.BuildQrPayload(user.PublicUserId!),
                user.DisplayName,
                user.Status.ToString()));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PublicIdentityDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

/// <summary>
/// Exact-match public ID lookup. Never supports partial search.
/// Returns a generic not-found for unknown/blocked identities to reduce enumeration.
/// </summary>
public sealed class ResolvePublicUserId
{
    private readonly IPlatformUserRepository _users;
    private readonly IAuditWriter _audit;

    public ResolvePublicUserId(IPlatformUserRepository users, IAuditWriter audit)
    {
        _users = users;
        _audit = audit;
    }

    public async Task<ApplicationResult<ResolvedPublicUserDto>> ExecuteAsync(
        PlatformUserId actorUserId,
        ResolvePublicUserIdRequest request,
        CancellationToken cancellationToken = default)
    {
        string normalized;
        try
        {
            normalized = PublicUserIdRules.TryExtractFromQrPayload(request.PublicUserIdOrQrPayload);
        }
        catch (DomainException)
        {
            return ApplicationResult<ResolvedPublicUserDto>.Failure(
                DomainErrorCodes.InvalidPublicUserId,
                "ExItS ID format is invalid.");
        }

        var purpose = string.IsNullOrWhiteSpace(request.Purpose)
            ? "unspecified"
            : request.Purpose.Trim().ToLowerInvariant();
        if (purpose.Length > 64)
        {
            purpose = purpose[..64];
        }

        var target = await _users.GetByPublicUserIdAsync(normalized, cancellationToken).ConfigureAwait(false);

        await _audit.WriteAsync(
            $"platform-user:{actorUserId.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PlatformUserPublicIdResolved,
            "public_user_id",
            normalized,
            target is null ? AuditOutcome.Denied : AuditOutcome.Succeeded,
            summary: $"purpose={purpose}",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        // Generic not-found for missing or non-active users (no existence leak).
        if (target is null || target.Status is not AccountStatus.Active)
        {
            return ApplicationResult<ResolvedPublicUserDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "No active user matched that ExItS ID.");
        }

        return ApplicationResult<ResolvedPublicUserDto>.Success(new ResolvedPublicUserDto(
            target.PublicUserId!,
            target.Id.Value,
            target.DisplayName,
            MaskEmail(target.NormalizedEmail),
            target.Status.ToString(),
            IsSelf: target.Id == actorUserId));
    }

    private static string MaskEmail(string normalizedEmail)
    {
        var at = normalizedEmail.IndexOf('@');
        if (at <= 1)
        {
            return "***";
        }

        return normalizedEmail[0] + "***" + normalizedEmail[at..];
    }
}
