using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Api.Organizations;

internal static class SalesDocumentCapabilityEndpoints
{
    public static IEndpointRouteBuilder MapSalesDocumentCapabilityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/v1/platform/organizations/{organizationId:guid}/sales-document-capability",
            async (
                Guid organizationId,
                GetOrganizationSalesDocumentCapability useCase,
                PlatformMembershipAuthz membershipAuthz,
                CancellationToken ct) =>
            {
                var denied = await membershipAuthz.EnsureActiveOrganizationMemberAsync(
                    PlatformAuditActions.PlatformAccessChecked,
                    nameof(OrganizationSalesDocumentCapability),
                    organizationId.ToString("D"),
                    organizationId,
                    summary: "Read organization sales-document capability.",
                    cancellationToken: ct).ConfigureAwait(false);
                if (denied is not null)
                {
                    return denied;
                }

                var result = await useCase
                    .ExecuteAsync(PlatformOrganizationId.From(organizationId), ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
            });

        app.MapGet(
            "/api/v1/platform/organizations/{organizationId:guid}/compliance-status",
            async (
                Guid organizationId,
                GetOrganizationComplianceStatus useCase,
                PlatformOrganizationAuthz orgAuthz,
                PlatformMembershipAuthz membershipAuthz,
                CancellationToken ct) =>
            {
                var viewDenied = await orgAuthz
                    .EnsureCanViewOrganizationAsync(organizationId, ct)
                    .ConfigureAwait(false);
                if (viewDenied is not null)
                {
                    var memberDenied = await membershipAuthz.EnsureActiveOrganizationMemberAsync(
                        PlatformAuditActions.PlatformAccessChecked,
                        nameof(OrganizationSalesDocumentCapability),
                        organizationId.ToString("D"),
                        organizationId,
                        summary: "Read organization compliance status.",
                        cancellationToken: ct).ConfigureAwait(false);
                    if (memberDenied is not null)
                    {
                        return viewDenied;
                    }
                }

                var result = await useCase
                    .ExecuteAsync(PlatformOrganizationId.From(organizationId), ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
            });

        app.MapPost(
            "/api/v1/platform/organizations/{organizationId:guid}/compliance/request",
            async (
                Guid organizationId,
                RequestOrganizationComplianceReview useCase,
                PlatformMembershipAuthz membershipAuthz,
                CancellationToken ct) =>
            {
                var denied = await membershipAuthz.EnsureActiveOrganizationMemberAsync(
                    PlatformAuditActions.OrganizationComplianceRequested,
                    nameof(OrganizationSalesDocumentCapability),
                    organizationId.ToString("D"),
                    organizationId,
                    summary: "Request organization compliance review.",
                    cancellationToken: ct).ConfigureAwait(false);
                if (denied is not null)
                {
                    return denied;
                }

                var actor = membershipAuthz.Inner.CurrentActor;
                var authority = await membershipAuthz
                    .ResolveActorMembershipAuthorityAsync(organizationId, ct)
                    .ConfigureAwait(false);
                if (actor.PlatformUserId is null
                    || authority.ActorMembershipRole != OrganizationRole.OrganizationOwner)
                {
                    return Results.Forbid();
                }

                var result = await useCase
                    .ExecuteAsync(
                        PlatformOrganizationId.From(organizationId),
                        actor.PlatformUserId,
                        actorReference: actor.PlatformUserId.Value.ToString("D"),
                        ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
            });

        app.MapPost(
            "/api/v1/platform/organizations/{organizationId:guid}/compliance/transition",
            async (
                Guid organizationId,
                ComplianceTransitionRequest body,
                TransitionOrganizationComplianceEligibility useCase,
                PlatformOrganizationAuthz orgAuthz,
                CancellationToken ct) =>
            {
                var denied = await orgAuthz
                    .EnsureCanManageOrganizationLifecycleAsync(
                        organizationId,
                        PlatformAuditActions.OrganizationComplianceReviewStarted,
                        ct)
                    .ConfigureAwait(false);
                if (denied is not null)
                {
                    return denied;
                }

                var actor = orgAuthz.Inner.CurrentActor;
                if (actor.PlatformUserId is null)
                {
                    return Results.Unauthorized();
                }

                var result = await useCase
                    .ExecuteAsync(
                        PlatformOrganizationId.From(organizationId),
                        body.TargetStatus,
                        actorReference: actor.PlatformUserId.Value.ToString("D"),
                        ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
            });

        app.MapPost(
            "/api/v1/platform/organizations/{organizationId:guid}/compliance/tax-document-capability",
            async (
                Guid organizationId,
                TaxDocumentCapabilityRequest body,
                SetOrganizationTaxDocumentIssuanceCapability useCase,
                PlatformOrganizationAuthz orgAuthz,
                CancellationToken ct) =>
            {
                var action = body.Enabled
                    ? PlatformAuditActions.OrganizationTaxDocumentCapabilityEnabled
                    : PlatformAuditActions.OrganizationTaxDocumentCapabilityDisabled;
                var denied = await orgAuthz
                    .EnsureCanManageOrganizationLifecycleAsync(organizationId, action, ct)
                    .ConfigureAwait(false);
                if (denied is not null)
                {
                    return denied;
                }

                var actor = orgAuthz.Inner.CurrentActor;
                if (actor.PlatformUserId is null)
                {
                    return Results.Unauthorized();
                }

                var result = await useCase
                    .ExecuteAsync(
                        PlatformOrganizationId.From(organizationId),
                        body.Enabled,
                        actorReference: actor.PlatformUserId.Value.ToString("D"),
                        ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
            });

        app.MapPost(
            "/api/v1/platform/organizations/{organizationId:guid}/compliance/tax-configuration-capability",
            async (
                Guid organizationId,
                TaxConfigurationCapabilityRequest body,
                SetOrganizationTaxConfigurationCapability useCase,
                PlatformOrganizationAuthz orgAuthz,
                CancellationToken ct) =>
            {
                var action = body.Enabled
                    ? PlatformAuditActions.OrganizationTaxConfigurationCapabilityEnabled
                    : PlatformAuditActions.OrganizationTaxConfigurationCapabilityDisabled;
                var denied = await orgAuthz
                    .EnsureCanManageOrganizationLifecycleAsync(organizationId, action, ct)
                    .ConfigureAwait(false);
                if (denied is not null)
                {
                    return denied;
                }

                var actor = orgAuthz.Inner.CurrentActor;
                if (actor.PlatformUserId is null)
                {
                    return Results.Unauthorized();
                }

                var result = await useCase
                    .ExecuteAsync(
                        PlatformOrganizationId.From(organizationId),
                        body.Enabled,
                        actorReference: actor.PlatformUserId.Value.ToString("D"),
                        ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
            });

        return app;
    }

    private sealed record ComplianceTransitionRequest(string TargetStatus);
    private sealed record TaxDocumentCapabilityRequest(bool Enabled);
    private sealed record TaxConfigurationCapabilityRequest(bool Enabled);
}
