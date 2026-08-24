using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Api.Common;

/// <summary>
/// Endpoint-facing authorization and audit helper for Platform API mutations and sensitive reads.
/// Wraps <see cref="IPlatformAuthorizationService"/> and <see cref="IAuditWriter"/> so endpoint
/// handlers do not each re-implement permission checks or audit trail writes.
/// </summary>
internal sealed class PlatformAuthz
{
    private readonly IPlatformActorAccessor _actorAccessor;
    private readonly IPlatformAuthorizationService _authorizationService;
    private readonly IAuditWriter _auditWriter;

    public PlatformAuthz(
        IPlatformActorAccessor actorAccessor,
        IPlatformAuthorizationService authorizationService,
        IAuditWriter auditWriter)
    {
        _actorAccessor = actorAccessor;
        _authorizationService = authorizationService;
        _auditWriter = auditWriter;
    }

    /// <summary>The actor resolved for the current request (development-stage; not production authentication).</summary>
    public PlatformActorContext CurrentActor => _actorAccessor.GetCurrent();

    /// <summary>Resolves the current actor's Platform permissions (platform-wide scope).</summary>
    public Task<IReadOnlySet<string>> ResolvePermissionsAsync(CancellationToken cancellationToken = default) =>
        _authorizationService.ResolvePermissionsForActorAsync(_actorAccessor.GetCurrent(), organizationId: null, cancellationToken);

    /// <summary>
    /// Checks whether the current actor holds <paramref name="permission"/> (optionally scoped to
    /// <paramref name="organizationId"/>). On denial, writes a <see cref="AuditOutcome.Denied"/> audit
    /// record and returns a 403 ProblemDetails result that the caller should return immediately.
    /// Returns <c>null</c> when the actor is permitted to proceed.
    /// </summary>
    public async Task<IResult?> EnsureAsync(
        string permission,
        string actionCode,
        string targetType,
        string targetId,
        Guid? organizationId = null,
        string? productCode = null,
        string? reason = null,
        string? summary = null,
        CancellationToken cancellationToken = default)
    {
        var actor = _actorAccessor.GetCurrent();
        var orgId = organizationId.HasValue ? PlatformOrganizationId.From(organizationId.Value) : null;
        var code = string.IsNullOrWhiteSpace(productCode) ? null : ProductCode.Create(productCode);

        var result = await _authorizationService
            .EnsurePermissionForActorAsync(actor, permission, orgId, cancellationToken)
            .ConfigureAwait(false);
        if (result.IsSuccess)
        {
            return null;
        }

        await _auditWriter.WriteAsync(
            actor,
            actionCode,
            targetType,
            targetId,
            AuditOutcome.Denied,
            orgId,
            code,
            reason: reason ?? result.ErrorMessage,
            summary: summary,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return PlatformApiResults.Problem(result.ErrorCode!, result.ErrorMessage!, StatusCodes.Status403Forbidden);
    }

    /// <summary>Requires a permission held only by Platform Administrator built-in roles (e.g. settings management).</summary>
    public Task<IResult?> EnsurePlatformAdministratorAsync(
        string actionCode,
        string targetType,
        string targetId,
        Guid? organizationId = null,
        string? productCode = null,
        string? reason = null,
        string? summary = null,
        CancellationToken cancellationToken = default) =>
        EnsureAsync(
            PlatformPermission.ManagePlatformSettings,
            actionCode,
            targetType,
            targetId,
            organizationId,
            productCode,
            reason,
            summary,
            cancellationToken);

    /// <summary>Writes a <see cref="AuditOutcome.Succeeded"/> audit record for the current actor after a mutation completes.</summary>
    public Task AuditSucceededAsync(
        string actionCode,
        string targetType,
        string targetId,
        Guid? organizationId = null,
        string? productCode = null,
        string? reason = null,
        string? summary = null,
        CancellationToken cancellationToken = default)
    {
        var actor = _actorAccessor.GetCurrent();
        var orgId = organizationId.HasValue ? PlatformOrganizationId.From(organizationId.Value) : null;
        var code = string.IsNullOrWhiteSpace(productCode) ? null : ProductCode.Create(productCode);

        return _auditWriter.WriteAsync(
            actor,
            actionCode,
            targetType,
            targetId,
            AuditOutcome.Succeeded,
            orgId,
            code,
            reason: reason,
            summary: summary,
            cancellationToken: cancellationToken);
    }
}
