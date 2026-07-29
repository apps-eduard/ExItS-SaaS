using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Contracts;

/// <summary>
/// Transport-independent versioned envelope. No broker/HTTP/serialization dependencies.
/// </summary>
public sealed class ContractEnvelope<TPayload>
{
    public string ContractName { get; }
    public ContractVersion SchemaVersion { get; }
    public Guid MessageId { get; }
    public Guid CorrelationId { get; }
    public Guid? CausationId { get; }
    public DateTimeOffset OccurredAtUtc { get; }
    public DateTimeOffset ProducedAtUtc { get; }
    public string SourceSystem { get; }
    public string SourceAggregateId { get; }
    public int SourceAggregateVersion { get; }
    public PlatformOrganizationId? OrganizationId { get; }
    public ProductCode? ProductCode { get; }
    public TPayload Payload { get; }

    private ContractEnvelope(
        string contractName,
        ContractVersion schemaVersion,
        Guid messageId,
        Guid correlationId,
        Guid? causationId,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset producedAtUtc,
        string sourceSystem,
        string sourceAggregateId,
        int sourceAggregateVersion,
        PlatformOrganizationId? organizationId,
        ProductCode? productCode,
        TPayload payload)
    {
        ContractName = contractName;
        SchemaVersion = schemaVersion;
        MessageId = messageId;
        CorrelationId = correlationId;
        CausationId = causationId;
        OccurredAtUtc = occurredAtUtc;
        ProducedAtUtc = producedAtUtc;
        SourceSystem = sourceSystem;
        SourceAggregateId = sourceAggregateId;
        SourceAggregateVersion = sourceAggregateVersion;
        OrganizationId = organizationId;
        ProductCode = productCode;
        Payload = payload;
    }

    public static ContractEnvelope<TPayload> Create(
        string contractName,
        ContractVersion schemaVersion,
        Guid messageId,
        Guid correlationId,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset producedAtUtc,
        string sourceSystem,
        string sourceAggregateId,
        int sourceAggregateVersion,
        TPayload payload,
        Guid? causationId = null,
        PlatformOrganizationId? organizationId = null,
        ProductCode? productCode = null)
    {
        if (string.IsNullOrWhiteSpace(contractName))
        {
            throw new ContractException(ContractErrorCodes.InvalidContractEnvelope, "Contract name is required.");
        }

        if (messageId == Guid.Empty)
        {
            throw new ContractException(ContractErrorCodes.InvalidContractEnvelope, "Message ID cannot be empty.");
        }

        if (correlationId == Guid.Empty)
        {
            throw new ContractException(ContractErrorCodes.InvalidContractEnvelope, "Correlation ID cannot be empty.");
        }

        if (occurredAtUtc.Offset != TimeSpan.Zero || producedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ContractException(
                DomainErrorCodes.InvalidUtcTimestamp,
                "Contract timestamps must be UTC.");
        }

        if (!string.Equals(sourceSystem, ContractSourceSystems.ExItsPlatform, StringComparison.Ordinal))
        {
            throw new ContractException(
                ContractErrorCodes.InvalidContractEnvelope,
                "Source system must be exits-platform.");
        }

        if (string.IsNullOrWhiteSpace(sourceAggregateId))
        {
            throw new ContractException(
                ContractErrorCodes.InvalidContractEnvelope,
                "Source aggregate ID is required.");
        }

        if (sourceAggregateVersion < 1)
        {
            throw new ContractException(
                ContractErrorCodes.InvalidSourceVersion,
                "Source aggregate version must be positive.");
        }

        ArgumentNullException.ThrowIfNull(payload);

        return new ContractEnvelope<TPayload>(
            contractName.Trim(),
            schemaVersion,
            messageId,
            correlationId,
            causationId,
            occurredAtUtc,
            producedAtUtc,
            sourceSystem,
            sourceAggregateId.Trim(),
            sourceAggregateVersion,
            organizationId,
            productCode,
            payload);
    }
}
