using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Audit;

/// <summary>Filters for querying the append-only audit trail. All filters are optional (AND-combined).</summary>
public sealed record AuditRecordFilter(
    DateTimeOffset? OccurredFromUtc = null,
    DateTimeOffset? OccurredToUtc = null,
    string? ActorIdentifier = null,
    AuditActorType? ActorType = null,
    string? ActionCode = null,
    string? TargetType = null,
    string? TargetId = null,
    PlatformOrganizationId? OrganizationId = null,
    ProductCode? ProductCode = null,
    AuditOutcome? Outcome = null,
    string? CorrelationId = null);

/// <summary>Append-only audit record persistence. There is intentionally no update or delete method.</summary>
public interface IAuditRecordRepository
{
    Task<AuditRecord?> GetByIdAsync(AuditRecordId id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<AuditRecord> Items, int TotalCount)> QueryAsync(
        AuditRecordFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(AuditRecord record, CancellationToken cancellationToken = default);
}
