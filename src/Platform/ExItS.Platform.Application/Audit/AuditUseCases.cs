using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Audit;

public sealed record AuditRecordDto(
    Guid Id,
    DateTimeOffset OccurredAtUtc,
    string ActorIdentifier,
    string ActorType,
    string ActionCode,
    string TargetType,
    string TargetId,
    Guid? OrganizationId,
    string? ProductCode,
    string? CorrelationId,
    string Outcome,
    string? Reason,
    string? Summary);

public sealed class QueryAuditRecords
{
    private readonly IAuditRecordRepository _auditRecords;

    public QueryAuditRecords(IAuditRecordRepository auditRecords) => _auditRecords = auditRecords;

    public async Task<PagedResult<AuditRecordDto>> ExecuteAsync(
        DateTimeOffset? occurredFromUtc,
        DateTimeOffset? occurredToUtc,
        string? actorIdentifier,
        AuditActorType? actorType,
        string? actionCode,
        string? targetType,
        string? targetId,
        Guid? organizationId,
        string? productCode,
        AuditOutcome? outcome,
        string? correlationId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var filter = new AuditRecordFilter(
            occurredFromUtc,
            occurredToUtc,
            actorIdentifier,
            actorType,
            actionCode,
            targetType,
            targetId,
            organizationId.HasValue ? PlatformOrganizationId.From(organizationId.Value) : null,
            string.IsNullOrWhiteSpace(productCode) ? null : ProductCode.Create(productCode),
            outcome,
            correlationId);

        var (items, total) = await _auditRecords.QueryAsync(filter, skip, take, cancellationToken).ConfigureAwait(false);
        return new PagedResult<AuditRecordDto>(
            items.Select(Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public static AuditRecordDto Map(AuditRecord record) =>
        new(
            record.Id.Value,
            record.OccurredAtUtc,
            record.ActorIdentifier,
            record.ActorType.ToString(),
            record.ActionCode,
            record.TargetType,
            record.TargetId,
            record.OrganizationId?.Value,
            record.ProductCode?.Value,
            record.CorrelationId,
            record.Outcome.ToString(),
            record.Reason,
            record.Summary);
}

public sealed class GetAuditRecord
{
    private readonly IAuditRecordRepository _auditRecords;

    public GetAuditRecord(IAuditRecordRepository auditRecords) => _auditRecords = auditRecords;

    public async Task<AuditRecordDto?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _auditRecords
            .GetByIdAsync(AuditRecordId.From(id), cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : QueryAuditRecords.Map(record);
    }
}
