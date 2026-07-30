using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Api.Audit;

/// <summary>
/// Read-only access to the append-only Platform audit trail (P4-WP04). No update or delete endpoint
/// exists — audit records are immutable once written.
/// </summary>
internal static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/platform/audit");

        group.MapGet("/", async (
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            string? actor,
            string? actorType,
            string? action,
            string? targetType,
            string? targetId,
            Guid? organizationId,
            string? productCode,
            string? outcome,
            string? correlationId,
            int? page,
            int? pageSize,
            QueryAuditRecords useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewAuditRecords,
                PlatformAuditActions.PlatformAccessChecked,
                "AuditRecord",
                "query",
                organizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            AuditActorType? parsedActorType = null;
            if (!string.IsNullOrWhiteSpace(actorType))
            {
                if (!Enum.TryParse<AuditActorType>(actorType, ignoreCase: true, out var value))
                {
                    return PlatformApiResults.Problem(
                        DomainErrorCodes.InvalidAuditActorType,
                        $"Unrecognized audit actor type '{actorType}'.",
                        StatusCodes.Status400BadRequest);
                }

                parsedActorType = value;
            }

            AuditOutcome? parsedOutcome = null;
            if (!string.IsNullOrWhiteSpace(outcome))
            {
                if (!Enum.TryParse<AuditOutcome>(outcome, ignoreCase: true, out var value))
                {
                    return PlatformApiResults.Problem(
                        DomainErrorCodes.InvalidAuditOutcome,
                        $"Unrecognized audit outcome '{outcome}'.",
                        StatusCodes.Status400BadRequest);
                }

                parsedOutcome = value;
            }

            var result = await useCase.ExecuteAsync(
                fromUtc,
                toUtc,
                actor,
                parsedActorType,
                action,
                targetType,
                targetId,
                organizationId,
                productCode,
                parsedOutcome,
                correlationId,
                page,
                pageSize,
                ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapGet("/{auditId:guid}", async (
            Guid auditId,
            GetAuditRecord useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewAuditRecords,
                PlatformAuditActions.PlatformAccessChecked,
                "AuditRecord",
                auditId.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var record = await useCase.ExecuteAsync(auditId, ct).ConfigureAwait(false);
            return record is null
                ? PlatformApiResults.Problem(
                    ApplicationErrorCodes.AuditRecordNotFound,
                    "Audit record was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(record);
        });

        return app;
    }
}
