using ExItS.PinoyBusinessPOS.Application.Permissions;
using ExItS.PinoyBusinessPOS.Domain.Permissions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Application.LivePreview;

public sealed class InitializePosLivePreviewRoles
{
    private readonly PosLivePreviewOptions _options;
    private readonly AssignPosRole _assignPosRole;
    private readonly ILogger<InitializePosLivePreviewRoles> _logger;

    public InitializePosLivePreviewRoles(
        IOptions<PosLivePreviewOptions> options,
        AssignPosRole assignPosRole,
        ILogger<InitializePosLivePreviewRoles> logger)
    {
        _options = options.Value;
        _assignPosRole = assignPosRole;
        _logger = logger;
    }

    public async Task ExecuteAsync(
        IReadOnlyList<PlatformLivePreviewIdentityDto> identities,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var orgAdmin = identities.FirstOrDefault(i =>
            string.Equals(i.Key, "org-admin", StringComparison.OrdinalIgnoreCase));
        var cashier = identities.FirstOrDefault(i =>
            string.Equals(i.Key, "pos-cashier", StringComparison.OrdinalIgnoreCase));

        if (orgAdmin is null || orgAdmin.OrganizationId is null || orgAdmin.OrganizationId == Guid.Empty)
        {
            throw new InvalidOperationException("Live preview org-admin identity is missing organization id.");
        }

        var organizationId = orgAdmin.OrganizationId.Value;

        var ownerResult = await _assignPosRole
            .ExecuteAsync(organizationId, orgAdmin.UserId, PosRoleCodes.Owner, orgAdmin.UserId, ct: cancellationToken)
            .ConfigureAwait(false);
        if (!ownerResult.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Live preview POS Owner bootstrap failed: {ownerResult.ErrorCode} {ownerResult.ErrorMessage}");
        }

        if (cashier is not null)
        {
            var cashierResult = await _assignPosRole
                .ExecuteAsync(
                    organizationId,
                    cashier.UserId,
                    PosRoleCodes.Cashier,
                    orgAdmin.UserId,
                    ct: cancellationToken)
                .ConfigureAwait(false);
            if (!cashierResult.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Live preview POS Cashier assign failed: {cashierResult.ErrorCode} {cashierResult.ErrorMessage}");
            }
        }

        _logger.LogInformation("POS live preview role initialization completed for organization {OrganizationId}.", organizationId);
    }
}
