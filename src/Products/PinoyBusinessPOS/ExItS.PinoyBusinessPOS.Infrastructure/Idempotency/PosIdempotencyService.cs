using System.Data;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Idempotency;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Idempotency;

/// <summary>
/// PostgreSQL-backed POS idempotency. Exact replay returns the stored outcome; payload mismatch conflicts.
/// </summary>
public sealed class PosIdempotencyService(PosDbContext db, IClock clock) : IPosIdempotencyService
{
    public async Task<PosIdempotencyOutcome> ExecuteAsync(
        PosIdempotencyRequest request,
        Func<CancellationToken, Task<PosIdempotencyExecutionResult>> execute,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(execute);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ProductCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OperationType);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.IdempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PayloadHash);

        await using var tx = await db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, ct)
            .ConfigureAwait(false);

        var existing = await db.IdempotencyRecords
            .SingleOrDefaultAsync(
                r => r.OrganizationId == request.OrganizationId
                     && r.ProductCode == request.ProductCode
                     && r.OperationType == request.OperationType
                     && r.IdempotencyKey == request.IdempotencyKey,
                ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            if (!string.Equals(existing.PayloadHash, request.PayloadHash, StringComparison.OrdinalIgnoreCase))
            {
                await tx.CommitAsync(ct).ConfigureAwait(false);
                return new PosIdempotencyOutcome(
                    IsReplay: false,
                    IsConflict: true,
                    OutcomeCode: "conflict_payload_mismatch",
                    OutcomeBodyJson: null,
                    ServerReference: existing.ServerReference);
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
            return new PosIdempotencyOutcome(
                IsReplay: true,
                IsConflict: false,
                OutcomeCode: existing.OutcomeCode,
                OutcomeBodyJson: existing.OutcomeBodyJson,
                ServerReference: existing.ServerReference);
        }

        var execution = await execute(ct).ConfigureAwait(false);
        var now = clock.UtcNow;
        var record = new PosIdempotencyRecord
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            ProductCode = request.ProductCode.Trim(),
            OperationType = request.OperationType.Trim(),
            IdempotencyKey = request.IdempotencyKey.Trim(),
            PayloadHash = request.PayloadHash.Trim().ToLowerInvariant(),
            OperationId = request.OperationId,
            OutcomeCode = execution.OutcomeCode,
            OutcomeBodyJson = execution.OutcomeBodyJson,
            ServerReference = execution.ServerReference,
            CreatedAtUtc = now,
            CompletedAtUtc = now
        };

        db.IdempotencyRecords.Add(record);
        try
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Concurrent insert or serialization failure: reload the winning row.
            db.ChangeTracker.Clear();
            try
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                // Transaction may already be aborted.
            }

            var winner = await db.IdempotencyRecords
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    r => r.OrganizationId == request.OrganizationId
                         && r.ProductCode == request.ProductCode
                         && r.OperationType == request.OperationType
                         && r.IdempotencyKey == request.IdempotencyKey,
                    ct)
                .ConfigureAwait(false);

            if (winner is null)
            {
                throw;
            }

            if (!string.Equals(winner.PayloadHash, request.PayloadHash, StringComparison.OrdinalIgnoreCase))
            {
                return new PosIdempotencyOutcome(
                    false,
                    true,
                    "conflict_payload_mismatch",
                    null,
                    winner.ServerReference);
            }

            return new PosIdempotencyOutcome(
                true,
                false,
                winner.OutcomeCode,
                winner.OutcomeBodyJson,
                winner.ServerReference);
        }

        return new PosIdempotencyOutcome(
            IsReplay: false,
            IsConflict: false,
            OutcomeCode: execution.OutcomeCode,
            OutcomeBodyJson: execution.OutcomeBodyJson,
            ServerReference: execution.ServerReference);
    }
}
