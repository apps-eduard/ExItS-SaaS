using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.PrivacyCompliance;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.PrivacyCompliance;

namespace ExItS.Platform.Api.PrivacyCompliance;

internal static class PrivacyComplianceEndpoints
{
    public static IEndpointRouteBuilder MapPrivacyComplianceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/platform/privacy-compliance");

        group.MapGet("/overview", async (
            GetPrivacyComplianceOverview useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPrivacyCompliance,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(ComplianceRequirement),
                "overview",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var overview = await useCase.ExecuteAsync(ct).ConfigureAwait(false);
            return Results.Ok(overview);
        });

        group.MapGet("/requirements", async (
            string? category,
            ListComplianceRequirements useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPrivacyCompliance,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(ComplianceRequirement),
                "list",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            ComplianceItemCategory? parsedCategory = null;
            if (!string.IsNullOrWhiteSpace(category))
            {
                if (!Enum.TryParse<ComplianceItemCategory>(category, ignoreCase: true, out var value)
                    || !Enum.IsDefined(value))
                {
                    return PlatformApiResults.Problem(
                        DomainErrorCodes.InvalidComplianceRequirementField,
                        $"Unrecognized compliance category '{category}'.",
                        StatusCodes.Status400BadRequest);
                }

                parsedCategory = value;
            }

            var items = await useCase.ExecuteAsync(parsedCategory, ct).ConfigureAwait(false);
            return Results.Ok(items);
        });

        group.MapGet("/requirements/{id:guid}", async (
            Guid id,
            GetComplianceRequirement useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPrivacyCompliance,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(ComplianceRequirement),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var requirement = await useCase.ExecuteAsync(id, ct).ConfigureAwait(false);
            return requirement is null
                ? PlatformApiResults.Problem(
                    ApplicationErrorCodes.ComplianceRequirementNotFound,
                    "Compliance requirement was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(requirement);
        });

        group.MapPost("/ensure-catalog", async (
            EnsurePrivacyComplianceCatalog useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePrivacyCompliance,
                PlatformAuditActions.PrivacyComplianceCatalogEnsured,
                nameof(ComplianceRequirement),
                "ensure-catalog",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(ct).ConfigureAwait(false);
            await authz.AuditSucceededAsync(
                PlatformAuditActions.PrivacyComplianceCatalogEnsured,
                nameof(ComplianceRequirement),
                "ensure-catalog",
                summary: $"Ensured privacy compliance catalog (requirements +{result.RequirementsAdded}, systems +{result.SystemsAdded}, evidence +{result.EvidenceAdded}).",
                cancellationToken: ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapPatch("/requirements/{id:guid}/status", async (
            Guid id,
            UpdateComplianceRequirementStatusRequest body,
            UpdateComplianceRequirementStatus useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePrivacyCompliance,
                PlatformAuditActions.PrivacyComplianceRequirementStatusUpdated,
                nameof(ComplianceRequirement),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(id, body.Status, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.PrivacyComplianceRequirementStatusUpdated,
                    nameof(ComplianceRequirement),
                    id.ToString("D"),
                    summary: $"Updated compliance requirement status to {body.Status}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        group.MapPatch("/requirements/{id:guid}", async (
            Guid id,
            UpdateComplianceRequirementDetailsRequest body,
            UpdateComplianceRequirementDetails useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePrivacyCompliance,
                PlatformAuditActions.PrivacyComplianceRequirementDetailsUpdated,
                nameof(ComplianceRequirement),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(id, body, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.PrivacyComplianceRequirementDetailsUpdated,
                    nameof(ComplianceRequirement),
                    id.ToString("D"),
                    summary: "Updated compliance requirement details.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        group.MapGet("/requirements/{id:guid}/evidence", async (
            Guid id,
            ListComplianceEvidence useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPrivacyCompliance,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(ComplianceEvidenceReference),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(id, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        group.MapPost("/evidence", async (
            AddComplianceEvidenceRequest body,
            AddComplianceEvidence useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePrivacyCompliance,
                PlatformAuditActions.PrivacyComplianceEvidenceAdded,
                nameof(ComplianceEvidenceReference),
                body.RequirementId.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(body, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.PrivacyComplianceEvidenceAdded,
                    nameof(ComplianceEvidenceReference),
                    result.Value!.Id.ToString("D"),
                    summary: $"Added compliance evidence '{result.Value.Label}'.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(
                result,
                e => Results.Created($"/api/v1/platform/privacy-compliance/requirements/{e.RequirementId}/evidence", e));
        });

        group.MapGet("/systems", async (
            ListProcessingSystems useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPrivacyCompliance,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(ProcessingSystemRecord),
                "list",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var systems = await useCase.ExecuteAsync(ct).ConfigureAwait(false);
            return Results.Ok(systems);
        });

        group.MapGet("/requirements/{id:guid}/export.pdf", async (
            Guid id,
            string? companyName,
            ExportComplianceRequirementPdf useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPrivacyCompliance,
                PlatformAuditActions.PrivacyComplianceRequirementExported,
                nameof(ComplianceRequirement),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(id, companyName, ct).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                return PlatformApiResults.FromResult(result, _ => Results.Empty);
            }

            await authz.AuditSucceededAsync(
                PlatformAuditActions.PrivacyComplianceRequirementExported,
                nameof(ComplianceRequirement),
                id.ToString("D"),
                summary: "Exported compliance requirement PDF.",
                cancellationToken: ct).ConfigureAwait(false);

            return Results.File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
        });

        return app;
    }
}
