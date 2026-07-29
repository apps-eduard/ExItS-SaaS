using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Contracts;

/// <summary>
/// Product access projection. Distinct from subscription entitlement and clinical permissions.
/// </summary>
public sealed class ProductAccessProjection
{
    public PlatformOrganizationId PlatformOrganizationId { get; }
    public PlatformUserId? PlatformUserId { get; }
    public ProductCode ProductCode { get; }
    public ProductAccessStatus AccessStatus { get; }
    public DateTimeOffset EffectiveAtUtc { get; }
    public DateTimeOffset? RevokedAtUtc { get; }
    public int SourceVersion { get; }

    public ProductAccessProjection(
        PlatformOrganizationId platformOrganizationId,
        ProductCode productCode,
        ProductAccessStatus accessStatus,
        DateTimeOffset effectiveAtUtc,
        int sourceVersion,
        PlatformUserId? platformUserId = null,
        DateTimeOffset? revokedAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(platformOrganizationId);
        ArgumentNullException.ThrowIfNull(productCode);

        if (effectiveAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ContractException(DomainErrorCodes.InvalidUtcTimestamp, "EffectiveAt must be UTC.");
        }

        if (revokedAtUtc is not null && revokedAtUtc.Value.Offset != TimeSpan.Zero)
        {
            throw new ContractException(DomainErrorCodes.InvalidUtcTimestamp, "RevokedAt must be UTC.");
        }

        if (sourceVersion < 1)
        {
            throw new ContractException(ContractErrorCodes.InvalidSourceVersion, "Source version must be positive.");
        }

        if (!Enum.IsDefined(accessStatus))
        {
            throw new ContractException(ContractErrorCodes.InvalidContractEnvelope, "Product access status is invalid.");
        }

        if (accessStatus == ProductAccessStatus.Revoked && revokedAtUtc is null)
        {
            throw new ContractException(
                ContractErrorCodes.InvalidContractEnvelope,
                "Revoked product access requires RevokedAtUtc.");
        }

        PlatformOrganizationId = platformOrganizationId;
        PlatformUserId = platformUserId;
        ProductCode = productCode;
        AccessStatus = accessStatus;
        EffectiveAtUtc = effectiveAtUtc;
        RevokedAtUtc = revokedAtUtc;
        SourceVersion = sourceVersion;
    }
}
