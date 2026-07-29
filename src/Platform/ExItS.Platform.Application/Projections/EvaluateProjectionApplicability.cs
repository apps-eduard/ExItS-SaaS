using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Contracts;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Projections;

public sealed class EvaluateProjectionApplicability
{
    private readonly IProjectionCheckpointStore _checkpoints;
    private readonly ProjectionApplicabilityEvaluator _evaluator;
    private readonly IClock _clock;

    public EvaluateProjectionApplicability(
        IProjectionCheckpointStore checkpoints,
        ProjectionApplicabilityEvaluator evaluator,
        IClock clock)
    {
        _checkpoints = checkpoints;
        _evaluator = evaluator;
        _clock = clock;
    }

    public async Task<ApplicationResult<ProjectionApplyResult>> ExecuteAsync(
        string consumerName,
        string contractName,
        Guid messageId,
        int sourceAggregateVersion,
        ContractVersion schemaVersion,
        PlatformOrganizationId? organizationId,
        ProductCode? productCode,
        bool isReconciliationSnapshot = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _checkpoints
                .GetAsync(consumerName, contractName, organizationId, productCode, cancellationToken)
                .ConfigureAwait(false)
                ?? ProjectionCheckpoint.Empty(consumerName, contractName);

            var result = _evaluator.Evaluate(
                existing,
                messageId,
                sourceAggregateVersion,
                schemaVersion,
                _clock.UtcNow,
                organizationId,
                productCode,
                isReconciliationSnapshot);

            if (result.Outcome == ProjectionApplyOutcome.Applied && result.UpdatedCheckpoint is not null)
            {
                await _checkpoints.SaveAsync(result.UpdatedCheckpoint, cancellationToken).ConfigureAwait(false);
            }

            return ApplicationResult<ProjectionApplyResult>.Success(result);
        }
        catch (ContractException ex)
        {
            return ApplicationResult<ProjectionApplyResult>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
