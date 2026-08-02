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

        var withPos = identities
            .Where(i => i.OrganizationId is not null
                        && i.OrganizationId != Guid.Empty
                        && !string.IsNullOrWhiteSpace(i.PosLocalRoleCode))
            .ToList();

        if (withPos.Count == 0)
        {
            _logger.LogInformation("POS local validation role initialization skipped (no POS-mapped identities).");
            return;
        }

        foreach (var group in withPos.GroupBy(i => i.OrganizationId!.Value))
        {
            var organizationId = group.Key;
            var owner = group.FirstOrDefault(i =>
                            string.Equals(i.PosLocalRoleCode, PosRoleCodes.Owner, StringComparison.OrdinalIgnoreCase))
                        ?? group.First();
            var actorUserId = owner.UserId;

            foreach (var identity in group)
            {
                var roleCode = identity.PosLocalRoleCode!;
                var result = await _assignPosRole
                    .ExecuteAsync(organizationId, identity.UserId, roleCode, actorUserId, ct: cancellationToken)
                    .ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    throw new InvalidOperationException(
                        $"Local validation POS role '{roleCode}' assign failed for '{identity.Key}': {result.ErrorCode} {result.ErrorMessage}");
                }
            }

            _logger.LogInformation(
                "POS local validation roles initialized for organization {OrganizationId} ({Count} users).",
                organizationId,
                group.Count());
        }
    }
}
