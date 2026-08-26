using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Access;

/// <summary>
/// Browser self-evaluation of effective product access for the current Platform session.
/// Resolves user and organization from the opaque session; never from caller-supplied ids.
/// Delegates commercial logic to <see cref="EvaluateEffectiveProductAccess"/>.
/// This is not Platform→product-server commercial-state transport (D-P12-03 remains open).
/// Read-only: does not write mutation-style audit records (the privileged evaluator endpoint
/// audits because of its authorization gate; this self-check has no such gate).
/// </summary>
public sealed class EvaluateCurrentSessionProductAccess
{
    private readonly IPlatformAuthSessionRepository _sessions;
    private readonly IPlatformSessionTokenService _tokens;
    private readonly EvaluateEffectiveProductAccess _evaluate;
    private readonly IClock _clock;

    public EvaluateCurrentSessionProductAccess(
        IPlatformAuthSessionRepository sessions,
        IPlatformSessionTokenService tokens,
        EvaluateEffectiveProductAccess evaluate,
        IClock clock)
    {
        _sessions = sessions;
        _tokens = tokens;
        _evaluate = evaluate;
        _clock = clock;
    }

    public async Task<ApplicationResult<EffectiveProductAccessResult>> ExecuteAsync(
        string? opaqueToken,
        string? productCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(opaqueToken))
        {
            return ApplicationResult<EffectiveProductAccessResult>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Session is invalid.");
        }

        var utcNow = _clock.UtcNow;
        var session = await _sessions
            .GetByTokenHashAsync(_tokens.HashToken(opaqueToken), cancellationToken)
            .ConfigureAwait(false);
        if (session is null || session.RevokedAtUtc is not null)
        {
            return ApplicationResult<EffectiveProductAccessResult>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Session is invalid.");
        }

        if (session.ExpiresAtUtc <= utcNow || session.AbsoluteExpiresAtUtc <= utcNow)
        {
            return ApplicationResult<EffectiveProductAccessResult>.Failure(
                ApplicationErrorCodes.SessionExpired,
                "Session has expired.");
        }

        if (session.AccountClass is not AccountClass.Organization)
        {
            return ApplicationResult<EffectiveProductAccessResult>.Failure(
                ApplicationErrorCodes.AccountScopeDenied,
                session.AccountClass is AccountClass.Platform
                    ? "Organization product entry requires an Organization account session."
                    : "Product self-access evaluation requires an Organization account session.");
        }

        if (session.SelectedOrganizationId is null)
        {
            return ApplicationResult<EffectiveProductAccessResult>.Failure(
                ApplicationErrorCodes.OrganizationContextRequired,
                "Selected organization context is required.");
        }

        ProductCode code;
        try
        {
            code = ProductCode.Create(productCode ?? string.Empty);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<EffectiveProductAccessResult>.Failure(ex.ErrorCode, ex.Message);
        }

        var result = await _evaluate
            .ExecuteAsync(session.UserId, session.SelectedOrganizationId, code.Value, cancellationToken)
            .ConfigureAwait(false);
        return ApplicationResult<EffectiveProductAccessResult>.Success(result);
    }
}
