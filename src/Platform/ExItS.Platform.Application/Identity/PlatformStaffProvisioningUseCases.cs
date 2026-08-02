using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Application.Identity;

/// <summary>
/// Platform Administrator staff provisioning: identity + required Platform role + Platform Account only.
/// </summary>
public sealed record CreatePlatformStaffUserResult(
    PlatformUser User,
    PlatformSystemRole PlatformRole,
    bool EmailVerificationIssued,
    string? EmailVerificationDebugToken,
    DateTimeOffset? EmailVerificationExpiresAtUtc);

public sealed class CreatePlatformStaffUser
{
    private readonly CreatePlatformUser _createUser;
    private readonly IStaffNumberGenerator _staffNumbers;
    private readonly AssignPlatformRole _assignPlatformRole;
    private readonly EnsureAccountProfilesForUser _ensureProfiles;
    private readonly SetPlatformUserPassword _setPassword;
    private readonly IssueEmailVerificationForUser _issueEmailVerification;

    public CreatePlatformStaffUser(
        CreatePlatformUser createUser,
        IStaffNumberGenerator staffNumbers,
        AssignPlatformRole assignPlatformRole,
        EnsureAccountProfilesForUser ensureProfiles,
        SetPlatformUserPassword setPassword,
        IssueEmailVerificationForUser issueEmailVerification)
    {
        _createUser = createUser;
        _staffNumbers = staffNumbers;
        _assignPlatformRole = assignPlatformRole;
        _ensureProfiles = ensureProfiles;
        _setPassword = setPassword;
        _issueEmailVerification = issueEmailVerification;
    }

    public async Task<ApplicationResult<CreatePlatformStaffUserResult>> ExecuteAsync(
        string firstName,
        string lastName,
        string displayName,
        string email,
        PlatformSystemRole platformRole,
        string actorIdentifier,
        AuditActorType actorType = AuditActorType.PlatformUser,
        string? username = null,
        string? phone = null,
        string? employeeCode = null,
        bool requireEmailVerification = false,
        string? initialPassword = null,
        Guid? createdByUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (requireEmailVerification && string.IsNullOrWhiteSpace(initialPassword))
        {
            return ApplicationResult<CreatePlatformStaffUserResult>.Failure(
                ApplicationErrorCodes.DomainViolation,
                "Initial password is required when requireEmailVerification is true.");
        }

        var staffNumber = await _staffNumbers.GenerateNextAsync(cancellationToken).ConfigureAwait(false);
        var created = await _createUser
            .ExecuteForStaffAsync(
                username,
                firstName,
                lastName,
                displayName,
                email,
                staffNumber,
                requireEmailVerification,
                phone,
                employeeCode,
                createdByUserId,
                cancellationToken)
            .ConfigureAwait(false);
        if (!created.IsSuccess || created.Value is null)
        {
            return ApplicationResult<CreatePlatformStaffUserResult>.Failure(
                created.ErrorCode ?? ApplicationErrorCodes.DomainViolation,
                created.ErrorMessage ?? "Platform User create failed.");
        }

        var user = created.Value;
        var roleResult = await _assignPlatformRole
            .ExecuteAsync(
                user.Id.Value,
                platformRole,
                organizationId: null,
                actorIdentifier: actorIdentifier,
                actorType: actorType,
                reason: "platform staff provisioning",
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (!roleResult.IsSuccess)
        {
            return ApplicationResult<CreatePlatformStaffUserResult>.Failure(
                roleResult.ErrorCode ?? ApplicationErrorCodes.DomainViolation,
                roleResult.ErrorMessage ?? "Platform role assignment failed.");
        }

        await _ensureProfiles
            .ExecuteAsync(
                user.Id,
                AccountClass.Platform,
                exclusivePreferredClass: true,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var emailVerificationIssued = false;
        string? debugToken = null;
        DateTimeOffset? expiresAt = null;
        if (requireEmailVerification)
        {
            var passwordResult = await _setPassword
                .ExecuteAsync(user.Id.Value, initialPassword!, cancellationToken)
                .ConfigureAwait(false);
            if (!passwordResult.IsSuccess)
            {
                return ApplicationResult<CreatePlatformStaffUserResult>.Failure(
                    passwordResult.ErrorCode ?? ApplicationErrorCodes.DomainViolation,
                    passwordResult.ErrorMessage ?? "Initial password set failed.");
            }

            var verification = await _issueEmailVerification
                .ExecuteAsync(user.Id.Value, actorIdentifier, actorType, cancellationToken)
                .ConfigureAwait(false);
            if (!verification.IsSuccess || verification.Value is null)
            {
                return ApplicationResult<CreatePlatformStaffUserResult>.Failure(
                    verification.ErrorCode ?? ApplicationErrorCodes.DomainViolation,
                    verification.ErrorMessage ?? "Email verification issue failed.");
            }

            emailVerificationIssued = true;
            debugToken = verification.Value.DebugToken;
            expiresAt = verification.Value.ExpiresAtUtc;
        }

        return ApplicationResult<CreatePlatformStaffUserResult>.Success(
            new CreatePlatformStaffUserResult(
                user,
                platformRole,
                emailVerificationIssued,
                debugToken,
                expiresAt));
    }
}

/// <summary>
/// Admin/system-initiated email verification for a specific Platform User (opt-in staff create flow).
/// </summary>
public sealed class IssueEmailVerificationForUser
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IPlatformCredentialTokenRepository _tokens;
    private readonly IPlatformSessionTokenService _tokenService;
    private readonly IPlatformAuthOutboundMessageSink _messages;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly PlatformCredentialLifecycleOptions _lifecycle;

    public IssueEmailVerificationForUser(
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IPlatformCredentialTokenRepository tokens,
        IPlatformSessionTokenService tokenService,
        IPlatformAuthOutboundMessageSink messages,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IOptions<PlatformCredentialLifecycleOptions> lifecycle)
    {
        _users = users;
        _credentials = credentials;
        _tokens = tokens;
        _tokenService = tokenService;
        _messages = messages;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _lifecycle = lifecycle.Value;
    }

    public async Task<ApplicationResult<CredentialWorkflowAckDto>> ExecuteAsync(
        Guid userId,
        string actorIdentifier,
        AuditActorType actorType = AuditActorType.PlatformUser,
        CancellationToken cancellationToken = default)
    {
        var id = PlatformUserId.From(userId);
        var user = await _users.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status is not (AccountStatus.Active or AccountStatus.PendingVerification))
        {
            return ApplicationResult<CredentialWorkflowAckDto>.Failure(
                ApplicationErrorCodes.AccountNotEligibleForLogin,
                "Account is not eligible.");
        }

        var credential = await _credentials.GetByUserIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (credential is null)
        {
            return ApplicationResult<CredentialWorkflowAckDto>.Failure(
                ApplicationErrorCodes.CredentialNotFound,
                "Platform User has no credential. Set a password before issuing email verification.");
        }

        if (credential.EmailVerifiedAtUtc is not null)
        {
            return ApplicationResult<CredentialWorkflowAckDto>.Success(
                new CredentialWorkflowAckDto("Email is already verified.", null, null));
        }

        var utcNow = _clock.UtcNow;
        await _tokens.InvalidateActiveForUserAsync(
            id,
            PlatformCredentialTokenPurpose.EmailVerification,
            utcNow,
            cancellationToken).ConfigureAwait(false);

        var opaque = _tokenService.CreateOpaqueToken();
        var lifetime = TimeSpan.FromHours(Math.Max(1, _lifecycle.EmailVerificationTokenLifetimeHours));
        var token = PlatformCredentialToken.Create(
            id,
            PlatformCredentialTokenPurpose.EmailVerification,
            _tokenService.HashToken(opaque),
            utcNow,
            lifetime);
        await _tokens.AddAsync(token, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _messages.PublishAsync(
            new PlatformAuthOutboundMessage(
                PlatformAuthOutboundMessageKinds.EmailVerification,
                user.Id.Value,
                user.NormalizedEmail,
                opaque,
                token.ExpiresAtUtc),
            cancellationToken).ConfigureAwait(false);

        await _auditWriter.WriteAsync(
            actorIdentifier,
            actorType,
            PlatformAuditActions.PlatformAuthEmailVerificationRequested,
            nameof(PlatformCredentialToken),
            token.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            summary: $"Email verification token issued for Platform User {id.Value:D} (staff provisioning).",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ApplicationResult<CredentialWorkflowAckDto>.Success(
            new CredentialWorkflowAckDto(
                "Email verification token issued.",
                _lifecycle.ExposeDebugTokens ? opaque : null,
                token.ExpiresAtUtc));
    }
}

/// <summary>
/// Ensures an Organization-scoped identity exists for an invitation email (Organization Account only).
/// </summary>
public sealed class EnsureOrganizationStaffIdentity
{
    private readonly CreatePlatformUser _createUser;
    private readonly IPlatformUserRepository _users;
    private readonly EnsureAccountProfilesForUser _ensureProfiles;

    public EnsureOrganizationStaffIdentity(
        CreatePlatformUser createUser,
        IPlatformUserRepository users,
        EnsureAccountProfilesForUser ensureProfiles)
    {
        _createUser = createUser;
        _users = users;
        _ensureProfiles = ensureProfiles;
    }

    public async Task<ApplicationResult<PlatformUser>> ExecuteAsync(
        string email,
        string? displayNameHint,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = PlatformUser.NormalizeEmail(email);
        var existing = await _users.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            await _ensureProfiles
                .ExecuteAsync(
                    existing.Id,
                    AccountClass.Organization,
                    exclusivePreferredClass: true,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return ApplicationResult<PlatformUser>.Success(existing);
        }

        var usernameBase = PlatformUsernameDerivation.DeriveFromEmail(normalizedEmail);
        var displayName = string.IsNullOrWhiteSpace(displayNameHint)
            ? usernameBase
            : displayNameHint.Trim();

        for (var attempt = 0; attempt < 8; attempt++)
        {
            var username = attempt == 0 ? usernameBase : $"{usernameBase}{attempt + 1}";
            if (username.Length < 3)
            {
                username = $"user{attempt + 1}{username}";
            }

            var created = await _createUser
                .ExecuteAsync(username, displayName, normalizedEmail, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (created.IsSuccess && created.Value is not null)
            {
                await _ensureProfiles
                    .ExecuteAsync(
                        created.Value.Id,
                        AccountClass.Organization,
                        exclusivePreferredClass: true,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return ApplicationResult<PlatformUser>.Success(created.Value);
            }

            if (created.ErrorCode == ApplicationErrorCodes.EmailConflict)
            {
                existing = await _users.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken)
                    .ConfigureAwait(false);
                if (existing is not null)
                {
                    await _ensureProfiles
                        .ExecuteAsync(
                            existing.Id,
                            AccountClass.Organization,
                            exclusivePreferredClass: true,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    return ApplicationResult<PlatformUser>.Success(existing);
                }
            }

            if (created.ErrorCode != ApplicationErrorCodes.UsernameConflict)
            {
                return ApplicationResult<PlatformUser>.Failure(
                    created.ErrorCode ?? ApplicationErrorCodes.DomainViolation,
                    created.ErrorMessage ?? "Organization staff identity create failed.");
            }
        }

        return ApplicationResult<PlatformUser>.Failure(
            ApplicationErrorCodes.UsernameConflict,
            "Unable to allocate a unique username for the invited staff identity.");
    }
}
