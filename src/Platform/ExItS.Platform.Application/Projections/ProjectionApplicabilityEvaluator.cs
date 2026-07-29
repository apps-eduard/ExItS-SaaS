using ExItS.Platform.Application.Contracts;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Projections;

/// <summary>
/// Deterministic projection acceptance policy. No transport or persistence.
/// </summary>
public sealed class ProjectionApplicabilityEvaluator
{
    public const int SupportedMajorVersion = 1;

    public ProjectionApplyResult Evaluate(
        ProjectionCheckpoint checkpoint,
        Guid messageId,
        int sourceAggregateVersion,
        ContractVersion schemaVersion,
        DateTimeOffset evaluatedAtUtc,
        PlatformOrganizationId? organizationId,
        ProductCode? productCode,
        bool isReconciliationSnapshot = false)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);

        if (evaluatedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ContractException(
                ContractErrorCodes.InvalidContractEnvelope,
                "Evaluation timestamp must be UTC.");
        }

        if (messageId == Guid.Empty)
        {
            return ProjectionApplyResult.Unsupported("Message ID cannot be empty.");
        }

        if (sourceAggregateVersion < 1)
        {
            return ProjectionApplyResult.Unsupported("Source aggregate version must be positive.");
        }

        if (schemaVersion.Major > SupportedMajorVersion)
        {
            return ProjectionApplyResult.Unsupported(
                $"Unsupported contract major version {schemaVersion.Major}; max supported is {SupportedMajorVersion}.");
        }

        if (checkpoint.LastAppliedMessageId is Guid lastMsg && lastMsg == messageId)
        {
            return ProjectionApplyResult.Duplicate(checkpoint);
        }

        if (checkpoint.LastAppliedSourceVersion is null)
        {
            if (isReconciliationSnapshot || sourceAggregateVersion == 1)
            {
                var first = checkpoint.WithApplied(
                    sourceAggregateVersion, messageId, evaluatedAtUtc, organizationId, productCode);
                return ProjectionApplyResult.Applied(first);
            }

            return ProjectionApplyResult.Gap(checkpoint);
        }

        var lastVersion = checkpoint.LastAppliedSourceVersion.Value;

        if (sourceAggregateVersion < lastVersion)
        {
            return ProjectionApplyResult.Older(checkpoint);
        }

        if (sourceAggregateVersion == lastVersion)
        {
            return ProjectionApplyResult.Conflict(checkpoint);
        }

        if (sourceAggregateVersion == lastVersion + 1)
        {
            var next = checkpoint.WithApplied(
                sourceAggregateVersion, messageId, evaluatedAtUtc, organizationId, productCode);
            return ProjectionApplyResult.Applied(next);
        }

        // Gap: sourceAggregateVersion > lastVersion + 1
        if (isReconciliationSnapshot)
        {
            var replaced = checkpoint.WithApplied(
                sourceAggregateVersion, messageId, evaluatedAtUtc, organizationId, productCode);
            return ProjectionApplyResult.Applied(replaced);
        }

        return ProjectionApplyResult.Gap(checkpoint);
    }
}

public interface IProjectionCheckpointStore
{
    Task<ProjectionCheckpoint?> GetAsync(
        string consumerName,
        string contractName,
        PlatformOrganizationId? organizationId,
        ProductCode? productCode,
        CancellationToken cancellationToken = default);

    Task SaveAsync(ProjectionCheckpoint checkpoint, CancellationToken cancellationToken = default);
}
