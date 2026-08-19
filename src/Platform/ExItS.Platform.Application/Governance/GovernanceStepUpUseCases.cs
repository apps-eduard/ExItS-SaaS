using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Governance;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using Microsoft.Extensions.Hosting;

namespace ExItS.Platform.Application.Governance;

public sealed record IssueGovernanceStepUpCommand(
    string ActionCode,
    string TargetType,
    Guid? TargetId,
    string CurrentPassword);

public sealed record GovernanceStepUpTokenDto(
    string StepUpToken,
    DateTimeOffset ExpiresAtUtc,
    string ActionCode,
    string TargetType,
    Guid? TargetId);

public sealed class IssueGovernanceStepUpGrant
{
    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(5);

    private readonly VerifyPlatformUserPassword _verifyPassword;
    private readonly IGovernanceStepUpGrantRepository _grants;
    private readonly IPlatformSessionTokenService _tokens;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public IssueGovernanceStepUpGrant(
        VerifyPlatformUserPassword verifyPassword,
        IGovernanceStepUpGrantRepository grants,
        IPlatformSessionTokenService tokens,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _verifyPassword = verifyPassword;
        _grants = grants;
        _tokens = tokens;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<GovernanceStepUpTokenDto>> ExecuteAsync(
        PlatformUserId actorUserId,
        PlatformOrganizationId organizationId,
        IssueGovernanceStepUpCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ActionCode)
            || string.IsNullOrWhiteSpace(command.TargetType))
        {
            return ApplicationResult<GovernanceStepUpTokenDto>.Failure(
                ApplicationErrorCodes.GovernanceStepUpInvalid,
                "Action code and target type are required.");
        }

        if (string.IsNullOrWhiteSpace(command.CurrentPassword))
        {
            return ApplicationResult<GovernanceStepUpTokenDto>.Failure(
                ApplicationErrorCodes.StepUpRequired,
                "Current password is required for this governance action.");
        }

        var password = await _verifyPassword
            .ExecuteAsync(actorUserId.Value, command.CurrentPassword, cancellationToken)
            .ConfigureAwait(false);
        if (!password.IsSuccess)
        {
            return ApplicationResult<GovernanceStepUpTokenDto>.Failure(
                password.ErrorCode ?? ApplicationErrorCodes.PasswordInvalid,
                password.ErrorMessage ?? "Password verification failed.");
        }

        var opaque = _tokens.CreateOpaqueToken();
        var hash = _tokens.HashToken(opaque);
        var utcNow = _clock.UtcNow;
        var grant = GovernanceStepUpGrant.Create(
            actorUserId,
            organizationId,
            command.ActionCode.Trim(),
            command.TargetType.Trim(),
            command.TargetId,
            hash,
            utcNow,
            DefaultLifetime);

        await _grants.AddAsync(grant, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ApplicationResult<GovernanceStepUpTokenDto>.Success(
            new GovernanceStepUpTokenDto(
                opaque,
                grant.ExpiresAtUtc,
                grant.ActionCode,
                grant.TargetType,
                grant.TargetId));
    }
}

public sealed class ConsumeGovernanceStepUpGrant
{
    private readonly IGovernanceStepUpGrantRepository _grants;
    private readonly IPlatformSessionTokenService _tokens;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IHostEnvironment _environment;

    public ConsumeGovernanceStepUpGrant(
        IGovernanceStepUpGrantRepository grants,
        IPlatformSessionTokenService tokens,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IHostEnvironment environment)
    {
        _grants = grants;
        _tokens = tokens;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _environment = environment;
    }

    public async Task<ApplicationResult> ExecuteAsync(
        PlatformUserId? actorUserId,
        PlatformOrganizationId organizationId,
        string actionCode,
        string targetType,
        Guid? targetId,
        string? stepUpToken,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId is null)
        {
            if (_environment.IsProduction())
            {
                return ApplicationResult.Failure(
                    ApplicationErrorCodes.StepUpRequired,
                    "Authenticated Platform user is required for this governance action.");
            }

            return ApplicationResult.Success();
        }

        if (string.IsNullOrWhiteSpace(stepUpToken))
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.StepUpRequired,
                "Password step-up is required for this governance action.");
        }

        var hash = _tokens.HashToken(stepUpToken.Trim());
        var grant = await _grants.GetByTokenHashAsync(hash, cancellationToken).ConfigureAwait(false);
        if (grant is null)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.GovernanceStepUpInvalid,
                "Governance step-up token is invalid.");
        }

        var utcNow = _clock.UtcNow;
        if (grant.ConsumedAtUtc is not null)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.GovernanceStepUpConsumed,
                "Governance step-up token was already used.");
        }

        if (grant.ExpiresAtUtc <= utcNow)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.GovernanceStepUpExpired,
                "Governance step-up token has expired.");
        }

        if (!grant.MatchesScope(actorUserId, organizationId, actionCode, targetType, targetId))
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.GovernanceStepUpInvalid,
                "Governance step-up token does not match this action scope.");
        }

        try
        {
            grant.Consume(utcNow);
        }
        catch (DomainException ex)
        {
            return ApplicationResult.Failure(ex.ErrorCode, ex.Message);
        }

        await _grants.UpdateAsync(grant, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult.Success();
    }
}

public static class GovernanceCriticalActionReason
{
    public static ApplicationResult? ValidateRequired(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 8)
        {
            return ApplicationResult.Failure(
                DomainErrorCodes.InvalidAuditReason,
                "A reason of at least 8 characters is required.");
        }

        return null;
    }
}
