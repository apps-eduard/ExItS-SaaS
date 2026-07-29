using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Contracts;

public enum OrganizationMappingStatus
{
    Active = 1,
    Inactive = 2,
    Retired = 3
}

/// <summary>
/// Explicit reversible mapping: Platform Organization ↔ opaque HealthCare operational org/clinic ID.
/// Supports one Platform Organization → many external IDs. No PHI.
/// </summary>
public sealed class OrganizationMappingProjection
{
    public Guid MappingId { get; }
    public PlatformOrganizationId PlatformOrganizationId { get; }
    public ProductCode ProductCode { get; }
    public string ExternalOrganizationId { get; }
    public OrganizationMappingStatus Status { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; }
    public int SourceVersion { get; }
    public string? MigrationBatchId { get; }
    public string? OperationalNote { get; }

    public OrganizationMappingProjection(
        Guid mappingId,
        PlatformOrganizationId platformOrganizationId,
        ProductCode productCode,
        string externalOrganizationId,
        OrganizationMappingStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        int sourceVersion,
        string? migrationBatchId = null,
        string? operationalNote = null)
    {
        ArgumentNullException.ThrowIfNull(platformOrganizationId);
        ArgumentNullException.ThrowIfNull(productCode);

        if (mappingId == Guid.Empty)
        {
            throw new ContractException(
                ContractErrorCodes.InvalidOrganizationMapping,
                "Mapping ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(externalOrganizationId))
        {
            throw new ContractException(
                ContractErrorCodes.InvalidOrganizationMapping,
                "External organization ID cannot be blank.");
        }

        if (createdAtUtc.Offset != TimeSpan.Zero || updatedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ContractException(DomainErrorCodes.InvalidUtcTimestamp, "Mapping timestamps must be UTC.");
        }

        if (sourceVersion < 1)
        {
            throw new ContractException(ContractErrorCodes.InvalidSourceVersion, "Source version must be positive.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new ContractException(ContractErrorCodes.InvalidOrganizationMapping, "Mapping status is invalid.");
        }

        if (!string.Equals(productCode.Value, ProductCode.HealthCare, StringComparison.Ordinal))
        {
            throw new ContractException(
                ContractErrorCodes.ProductCodeMismatch,
                "Organization mapping for HealthCare adaptation requires healthcare ProductCode.");
        }

        if (operationalNote is { Length: > 256 })
        {
            throw new ContractException(
                ContractErrorCodes.InvalidOrganizationMapping,
                "Operational note must be at most 256 characters.");
        }

        MappingId = mappingId;
        PlatformOrganizationId = platformOrganizationId;
        ProductCode = productCode;
        ExternalOrganizationId = externalOrganizationId.Trim();
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        SourceVersion = sourceVersion;
        MigrationBatchId = string.IsNullOrWhiteSpace(migrationBatchId) ? null : migrationBatchId.Trim();
        OperationalNote = string.IsNullOrWhiteSpace(operationalNote) ? null : operationalNote.Trim();
    }
}
