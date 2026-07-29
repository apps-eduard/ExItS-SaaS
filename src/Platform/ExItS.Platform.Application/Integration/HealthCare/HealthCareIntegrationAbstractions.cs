using ExItS.Platform.Application.Contracts;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Integration.HealthCare;

public enum ReconciliationOutcome
{
    Reconciled = 1,
    NoChange = 2,
    SourceUnavailable = 3,
    InvalidSnapshot = 4,
    Conflict = 5,
    Failed = 6
}

public sealed class ReconciliationRequest
{
    public string ConsumerName { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public ProductCode ProductCode { get; }
    public int? ExpectedSourceVersion { get; }
    public int? CurrentSourceVersion { get; }
    public string Reason { get; }
    public Guid CorrelationId { get; }

    public ReconciliationRequest(
        string consumerName,
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        string reason,
        Guid correlationId,
        int? expectedSourceVersion = null,
        int? currentSourceVersion = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(productCode);

        if (string.IsNullOrWhiteSpace(consumerName))
        {
            throw new ContractException(ContractErrorCodes.InvalidContractEnvelope, "Consumer name is required.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ContractException(ContractErrorCodes.InvalidContractEnvelope, "Reconciliation reason is required.");
        }

        if (correlationId == Guid.Empty)
        {
            throw new ContractException(ContractErrorCodes.InvalidContractEnvelope, "Correlation ID cannot be empty.");
        }

        if (!string.Equals(productCode.Value, ProductCode.HealthCare, StringComparison.Ordinal))
        {
            throw new ContractException(
                ContractErrorCodes.ProductCodeMismatch,
                "HealthCare reconciliation requires healthcare ProductCode.");
        }

        ConsumerName = consumerName.Trim();
        OrganizationId = organizationId;
        ProductCode = productCode;
        ExpectedSourceVersion = expectedSourceVersion;
        CurrentSourceVersion = currentSourceVersion;
        Reason = reason.Trim();
        CorrelationId = correlationId;
    }
}

public sealed class ReconciliationResult
{
    public ReconciliationOutcome Outcome { get; }
    public string? Detail { get; }

    public ReconciliationResult(ReconciliationOutcome outcome, string? detail = null)
    {
        Outcome = outcome;
        Detail = detail;
    }

    public static ReconciliationResult Reconciled(string? detail = null) =>
        new(ReconciliationOutcome.Reconciled, detail);

    public static ReconciliationResult NoChange() =>
        new(ReconciliationOutcome.NoChange, "Checkpoint already current.");

    public static ReconciliationResult Conflict(string detail) =>
        new(ReconciliationOutcome.Conflict, detail);

    public static ReconciliationResult Failed(string detail) =>
        new(ReconciliationOutcome.Failed, detail);

    public static ReconciliationResult InvalidSnapshot(string detail) =>
        new(ReconciliationOutcome.InvalidSnapshot, detail);

    public static ReconciliationResult SourceUnavailable(string detail) =>
        new(ReconciliationOutcome.SourceUnavailable, detail);
}

/// <summary>
/// Future reconciliation boundary. No HTTP/broker/persistence implementation in this WP.
/// </summary>
public interface IPlatformProjectionReconciliationService
{
    Task<ReconciliationResult> RequestReconciliationAsync(
        ReconciliationRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Narrow Platform-side delivery abstraction for future HealthCare consumption.
/// No HealthCare entity or DbContext types. No implementation in this WP.
/// </summary>
public interface IHealthCareUserProjectionDelivery
{
    Task DeliverUserProjectionAsync(
        ContractEnvelope<PlatformUserProjection> envelope,
        CancellationToken cancellationToken = default);
}

public interface IHealthCareMembershipProjectionDelivery
{
    Task DeliverMembershipProjectionAsync(
        ContractEnvelope<OrganizationMembershipProjection> envelope,
        CancellationToken cancellationToken = default);
}

public interface IHealthCareOrganizationMappingDelivery
{
    Task DeliverOrganizationMappingAsync(
        ContractEnvelope<OrganizationMappingProjection> envelope,
        CancellationToken cancellationToken = default);
}

public interface IHealthCareProductAccessProjectionDelivery
{
    Task DeliverProductAccessProjectionAsync(
        ContractEnvelope<ProductAccessProjection> envelope,
        CancellationToken cancellationToken = default);
}

public interface IHealthCareSubscriptionProjectionDelivery
{
    Task DeliverSubscriptionProjectionAsync(
        ContractEnvelope<SubscriptionProjection> envelope,
        CancellationToken cancellationToken = default);
}

public interface IHealthCareEntitlementSnapshotDelivery
{
    Task DeliverEntitlementSnapshotAsync(
        ContractEnvelope<EntitlementSnapshotProjection> envelope,
        CancellationToken cancellationToken = default);
}
