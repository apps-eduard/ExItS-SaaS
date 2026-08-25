using ExItS.Platform.Application.Audit;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class NoOpAuditWriter : IAuditWriter
{
    public int WriteCount { get; private set; }

    public Task WriteAsync(
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
        WriteCount++;
        return Task.CompletedTask;
    }
}

internal sealed class RecordingAuditWriter : IAuditWriter
{
    public List<(
        string Actor,
        AuditActorType ActorType,
        string Action,
        string TargetType,
        string TargetId,
        AuditOutcome Outcome,
        PlatformOrganizationId? OrganizationId,
        string? Summary)> Entries { get; } = [];

    public Task WriteAsync(
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
        Entries.Add((
            actorIdentifier,
            actorType,
            actionCode,
            targetType,
            targetId,
            outcome,
            organizationId,
            summary));
        return Task.CompletedTask;
    }
}
