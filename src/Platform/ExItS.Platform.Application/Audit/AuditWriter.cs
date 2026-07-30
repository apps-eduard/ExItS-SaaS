using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Audit;

/// <summary>Writes append-only Platform audit records. Never updates or deletes existing records.</summary>
public interface IAuditWriter
{
    Task WriteAsync(
        string actorIdentifier,
        AuditActorType actorType,
        string actionCode,
        string targetType,
        string targetId,
        AuditOutcome outcome,
        PlatformOrganizationId? organizationId = null,
        ProductCode? productCode = null,
        string? correlationId = null,
        string? reason = null,
        string? summary = null,
        CancellationToken cancellationToken = default);

    /// <summary>Convenience overload that derives actor identifier/type/correlation id from an actor context.</summary>
    Task WriteAsync(
        PlatformActorContext actor,
        string actionCode,
        string targetType,
        string targetId,
        AuditOutcome outcome,
        PlatformOrganizationId? organizationId = null,
        ProductCode? productCode = null,
        string? reason = null,
        string? summary = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return WriteAsync(
            actor.ActorIdentifier,
            actor.ActorType,
            actionCode,
            targetType,
            targetId,
            outcome,
            organizationId,
            productCode,
            actor.CorrelationId,
            reason,
            summary,
            cancellationToken);
    }
}

public sealed class AuditWriter : IAuditWriter
{
    private readonly IAuditRecordRepository _auditRecords;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AuditWriter(IAuditRecordRepository auditRecords, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _auditRecords = auditRecords;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task WriteAsync(
        string actorIdentifier,
        AuditActorType actorType,
        string actionCode,
        string targetType,
        string targetId,
        AuditOutcome outcome,
        PlatformOrganizationId? organizationId = null,
        ProductCode? productCode = null,
        string? correlationId = null,
        string? reason = null,
        string? summary = null,
        CancellationToken cancellationToken = default)
    {
        var record = AuditRecord.Create(
            _clock.UtcNow,
            actorIdentifier,
            actorType,
            actionCode,
            targetType,
            targetId,
            outcome,
            organizationId,
            productCode,
            correlationId,
            reason,
            summary);

        await _auditRecords.AddAsync(record, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
