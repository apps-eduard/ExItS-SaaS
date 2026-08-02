using ExItS.PinoyBusinessPOS.Application.Permissions;
using ExItS.PinoyBusinessPOS.Domain.Permissions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Application.LocalValidation;

public sealed class InitializePosLocalValidationRoles
{
    private readonly PosLocalValidationOptions _options;
    private readonly AssignPosRole _assignPosRole;
    private readonly ILogger<InitializePosLocalValidationRoles> _logger;

    public InitializePosLocalValidationRoles(
        IOptions<PosLocalValidationOptions> options,
        AssignPosRole assignPosRole,
        ILogger<InitializePosLocalValidationRoles> logger)
    {
        _options = options.Value;
        _assignPosRole = assignPosRole;
        _logger = logger;
    }

    public async Task ExecuteAsync(
        IReadOnlyList<PlatformLocalValidationIdentityDto> identities,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var owner = identities.FirstOrDefault(i =>
            string.Equals(i.Key, "rafael-torres", StringComparison.OrdinalIgnoreCase));
        var cashier = identities.FirstOrDefault(i =>
            string.Equals(i.Key, "maria-santos", StringComparison.OrdinalIgnoreCase));
        var storeManager = identities.FirstOrDefault(i =>
            string.Equals(i.Key, "carlo-reyes", StringComparison.OrdinalIgnoreCase));

        if (owner is null || owner.OrganizationId is null || owner.OrganizationId == Guid.Empty)
        {
            throw new InvalidOperationException("Local validation rafael-torres identity is missing organization id.");
        }

        var organizationId = owner.OrganizationId.Value;
        var actorUserId = owner.UserId;

        var ownerResult = await _assignPosRole
            .ExecuteAsync(organizationId, owner.UserId, PosRoleCodes.Owner, actorUserId, ct: cancellationToken)
            .ConfigureAwait(false);
        if (!ownerResult.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Local validation POS Owner bootstrap failed: {ownerResult.ErrorCode} {ownerResult.ErrorMessage}");
        }

        if (cashier is not null)
        {
            var cashierResult = await _assignPosRole
                .ExecuteAsync(
                    organizationId,
                    cashier.UserId,
                    PosRoleCodes.Cashier,
                    actorUserId,
                    ct: cancellationToken)
                .ConfigureAwait(false);
            if (!cashierResult.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Local validation POS Cashier assign failed: {cashierResult.ErrorCode} {cashierResult.ErrorMessage}");
            }
        }

        if (storeManager is not null)
        {
            var storeManagerResult = await _assignPosRole
                .ExecuteAsync(
                    organizationId,
                    storeManager.UserId,
                    PosRoleCodes.StoreManager,
                    actorUserId,
                    ct: cancellationToken)
                .ConfigureAwait(false);
            if (!storeManagerResult.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Local validation POS Store Manager assign failed: {storeManagerResult.ErrorCode} {storeManagerResult.ErrorMessage}");
            }
        }

        _logger.LogInformation("POS local validation role initialization completed for organization {OrganizationId}.", organizationId);
    }
}
