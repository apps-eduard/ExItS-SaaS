using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Integration.HealthCare;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Integration.HealthCare;

/// <summary>
/// Validates reconciliation requests only. No transport — returns SourceUnavailable by design
/// until a later WP implements delivery.
/// </summary>
public sealed class RequestProjectionReconciliation
{
    public Task<ApplicationResult<ReconciliationResult>> ExecuteAsync(
        ReconciliationRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);

        if (request.ExpectedSourceVersion is int expected
            && request.CurrentSourceVersion is int current
            && expected == current)
        {
            return Task.FromResult(ApplicationResult<ReconciliationResult>.Success(ReconciliationResult.NoChange()));
        }

        if (request.ExpectedSourceVersion is int e
            && request.CurrentSourceVersion is int c
            && e < c)
        {
            return Task.FromResult(ApplicationResult<ReconciliationResult>.Success(
                ReconciliationResult.Conflict("Expected source version is behind current checkpoint.")));
        }

        return Task.FromResult(ApplicationResult<ReconciliationResult>.Success(
            ReconciliationResult.SourceUnavailable(
                "Reconciliation transport is not implemented in P2-WP04.")));
    }
}
