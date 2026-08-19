using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Api.Organizations;

/// <summary>
/// Organization-scoped read access to the append-only Platform governance audit trail (P28-WP15E).
/// Mutations remain on global audit writer paths; this exposes investigation for Owner/Manager.
/// </summary>
internal static class OrganizationAuditEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/platform/organizations/{organizationId:guid}/audit");

        group.MapGet("/", async (
            Guid organizationId,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            string? actor,
            string? action,
            string? targetType,
            string? outcome,
            Guid? branchId,
            int? page,
            int? pageSize,
            QueryAuditRecords useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanViewOrganizationAuditAsync(
                PlatformAuditActions.PlatformAccessChecked,
                nameof(AuditRecord),
                "query",
                organizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            if (branchId is not null)
            {
                targetType = nameof(OrganizationBranch);
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
                actorType: null,
                action,
                targetType,
                branchId?.ToString("D"),
                organizationId,
                productCode: null,
                parsedOutcome,
                correlationId: null,
                page,
                pageSize,
                ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapGet("/{auditId:guid}", async (
            Guid organizationId,
            Guid auditId,
            GetAuditRecord useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanViewOrganizationAuditAsync(
                PlatformAuditActions.PlatformAccessChecked,
                nameof(AuditRecord),
                auditId.ToString("D"),
                organizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var record = await useCase.ExecuteAsync(auditId, ct).ConfigureAwait(false);
            if (record is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.AuditRecordNotFound,
                    "Audit record was not found.",
                    StatusCodes.Status404NotFound);
            }

            if (record.OrganizationId != organizationId)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.AuditRecordNotFound,
                    "Audit record was not found.",
                    StatusCodes.Status404NotFound);
            }

            return Results.Ok(record);
        });

        return app;
    }
}
