using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Governance;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Governance;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Api.Organizations;

internal static class BranchAndDeviceEndpoints
{
    public static IEndpointRouteBuilder MapBranchAndDeviceEndpoints(this IEndpointRouteBuilder app)
    {
        var root = app.MapGroup("/api/v1/platform/organizations/{organizationId:guid}");
        root.MapGet("/branches", async (
            Guid organizationId,
            ListBranches useCase,
            PlatformOrganizationAuthz authz,
            PlatformAuthz platformAuthz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = platformAuthz.CurrentActor.PlatformUserId;
            if (actor is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.SessionInvalid,
                    "Authenticated Platform user is required.",
                    StatusCodes.Status401Unauthorized);
            }

            return Results.Ok(await useCase.ExecuteAsync(
                PlatformOrganizationId.From(organizationId),
                actor,
                ct).ConfigureAwait(false));
        });
        root.MapGet("/primary-branch", async (
            Guid organizationId,
            GetOrganizationPrimaryBranch useCase,
            PlatformOrganizationAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            return PlatformApiResults.FromResult(
                await useCase.ExecuteAsync(PlatformOrganizationId.From(organizationId), ct).ConfigureAwait(false),
                dto => Results.Ok(dto));
        });
        root.MapPut("/branch-context", async (
            Guid organizationId,
            SelectBranchContextRequest body,
            SelectOrganizationBranchContext useCase,
            PlatformOrganizationAuthz authz,
            PlatformAuthz platformAuthz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            if (body.BranchId == Guid.Empty)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.BranchNotFound,
                    "BranchId cannot be an empty GUID.",
                    StatusCodes.Status400BadRequest);
            }

            var actor = platformAuthz.CurrentActor.PlatformUserId;
            if (actor is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.SessionInvalid,
                    "Authenticated Platform user is required.",
                    StatusCodes.Status401Unauthorized);
            }

            return PlatformApiResults.FromResult(
                await useCase.ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    OrganizationBranchId.From(body.BranchId),
                    actor,
                    ct).ConfigureAwait(false),
                Results.Ok);
        });
        root.MapGet("/branches/capacity", async (Guid organizationId, GetBranchCapacity useCase, PlatformOrganizationAuthz authz, CancellationToken ct) =>
        {
            var denied = await authz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            return PlatformApiResults.FromResult(await useCase.ExecuteAsync(PlatformOrganizationId.From(organizationId), ct).ConfigureAwait(false), Results.Ok);
        });
        root.MapGet("/branches/management-summary", async (
            Guid organizationId,
            ListBranchManagementSummaries useCase,
            PlatformOrganizationAuthz authz,
            PlatformAuthz platformAuthz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            var actor = platformAuthz.CurrentActor.PlatformUserId;
            if (actor is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.AccountScopeDenied,
                    "Authenticated organization user is required.",
                    StatusCodes.Status403Forbidden);
            }

            return PlatformApiResults.FromResult(
                await useCase.ExecuteAsync(PlatformOrganizationId.From(organizationId), actor, ct).ConfigureAwait(false),
                Results.Ok);
        });
        root.MapPost("/branches", async (Guid organizationId, CreateBranchRequest body, CreateBranch useCase, PlatformOrganizationAuthz authz, CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(organizationId, PlatformAuditActions.OrganizationBranchCreated, ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            var result = await useCase.ExecuteAsync(PlatformOrganizationId.From(organizationId),
                new CreateBranchCommand(
                    body.Code ?? string.Empty,
                    body.Name ?? string.Empty,
                    body.AddressLine1,
                    body.AddressLine2,
                    body.City,
                    body.Region,
                    body.PostalCode,
                    body.CountryCode,
                    body.Latitude,
                    body.Longitude,
                    body.PickupEnabled ?? false,
                    body.DeliveryEnabled ?? false,
                    body.CustomerOrderingEnabled ?? false,
                    body.ContactPhone,
                    body.TimeZoneId), ct).ConfigureAwait(false);
            if (result.IsSuccess && result.Value is not null)
            {
                await OrganizationGovernanceAuditWriter.WriteBranchAsync(
                    authz,
                    PlatformAuditActions.OrganizationBranchCreated,
                    result.Value,
                    organizationId,
                    OrganizationGovernanceAuditWriter.BranchSummary(result.Value, "Created"),
                    ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, x => Results.Created($"/api/v1/platform/organizations/{organizationId}/branches/{x.Id}", x));
        });
        root.MapPut("/branches/{branchId:guid}", async (Guid organizationId, Guid branchId, UpdateBranchRequest body, UpdateBranch useCase, PlatformOrganizationAuthz authz, CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(organizationId, PlatformAuditActions.OrganizationBranchUpdated, ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            var status = string.IsNullOrWhiteSpace(body.Status) ? null : Enum.TryParse<OrganizationBranchStatus>(body.Status, true, out var value) ? value : (OrganizationBranchStatus?)null;
            if (!string.IsNullOrWhiteSpace(body.Status) && status is null) return PlatformApiResults.Problem(ApplicationErrorCodes.DomainViolation, "Branch status is invalid.", StatusCodes.Status400BadRequest);
            if (status is not null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.StepUpRequired,
                    "Branch status changes require suspend, reactivate, or archive actions with password step-up.",
                    StatusCodes.Status403Forbidden);
            }
            var result = await useCase.ExecuteAsync(PlatformOrganizationId.From(organizationId), OrganizationBranchId.From(branchId),
                new UpdateBranchCommand(
                    body.Name ?? string.Empty,
                    body.AddressLine1,
                    body.AddressLine2,
                    body.City,
                    body.Region,
                    body.PostalCode,
                    body.CountryCode,
                    status,
                    body.Latitude,
                    body.Longitude,
                    body.ClearCoordinates,
                    body.ContactPhone,
                    body.TimeZoneId), ct).ConfigureAwait(false);
            if (result.IsSuccess && result.Value is not null)
            {
                await OrganizationGovernanceAuditWriter.WriteBranchAsync(
                    authz,
                    PlatformAuditActions.OrganizationBranchUpdated,
                    result.Value,
                    organizationId,
                    OrganizationGovernanceAuditWriter.BranchSummary(result.Value, "Updated"),
                    ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });
        root.MapPost("/branches/{branchId:guid}/suspend", async (
            Guid organizationId,
            Guid branchId,
            GovernanceCriticalActionRequest body,
            SuspendBranch useCase,
            ConsumeGovernanceStepUpGrant stepUp,
            PlatformOrganizationAuthz authz,
            PlatformAuthz platformAuthz,
            CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(organizationId, PlatformAuditActions.OrganizationBranchSuspended, ct).ConfigureAwait(false);
            if (denied is not null) return denied;

            var reasonError = GovernanceCriticalActionReason.ValidateRequired(body.Reason);
            if (reasonError is not null)
            {
                return PlatformApiResults.Problem(reasonError.ErrorCode!, reasonError.ErrorMessage!, StatusCodes.Status400BadRequest);
            }

            var actor = platformAuthz.CurrentActor.PlatformUserId;
            var stepUpDenied = await GovernanceStepUpHelper.EnsureConsumedAsync(
                stepUp,
                actor,
                PlatformOrganizationId.From(organizationId),
                GovernanceCriticalActionCodes.BranchSuspend,
                GovernanceStepUpTargetTypes.OrganizationBranch,
                branchId,
                body.StepUpToken,
                ct).ConfigureAwait(false);
            if (stepUpDenied is not null) return stepUpDenied;
            if (actor is null) return PlatformApiResults.Problem(ApplicationErrorCodes.SessionInvalid, "Authenticated Platform user is required.", StatusCodes.Status401Unauthorized);

            var result = await useCase.ExecuteAsync(
                PlatformOrganizationId.From(organizationId),
                OrganizationBranchId.From(branchId),
                actor,
                body.Reason!.Trim(),
                ct).ConfigureAwait(false);
            if (result.IsSuccess && result.Value is not null)
            {
                await OrganizationGovernanceAuditWriter.WriteBranchAsync(
                    authz,
                    PlatformAuditActions.OrganizationBranchSuspended,
                    result.Value,
                    organizationId,
                    OrganizationGovernanceAuditWriter.BranchSummary(result.Value, $"Suspended. Reason: {body.Reason!.Trim()}"),
                    ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });
        root.MapPost("/branches/{branchId:guid}/reactivate", async (
            Guid organizationId,
            Guid branchId,
            GovernanceCriticalActionRequest body,
            ReactivateBranch useCase,
            ConsumeGovernanceStepUpGrant stepUp,
            PlatformOrganizationAuthz authz,
            PlatformAuthz platformAuthz,
            CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(organizationId, PlatformAuditActions.OrganizationBranchReactivated, ct).ConfigureAwait(false);
            if (denied is not null) return denied;

            var actor = platformAuthz.CurrentActor.PlatformUserId;
            var stepUpDenied = await GovernanceStepUpHelper.EnsureConsumedAsync(
                stepUp,
                actor,
                PlatformOrganizationId.From(organizationId),
                GovernanceCriticalActionCodes.BranchReactivate,
                GovernanceStepUpTargetTypes.OrganizationBranch,
                branchId,
                body.StepUpToken,
                ct).ConfigureAwait(false);
            if (stepUpDenied is not null) return stepUpDenied;

            var result = await useCase.ExecuteAsync(
                PlatformOrganizationId.From(organizationId),
                OrganizationBranchId.From(branchId),
                ct).ConfigureAwait(false);
            if (result.IsSuccess && result.Value is not null)
            {
                await OrganizationGovernanceAuditWriter.WriteBranchAsync(
                    authz,
                    PlatformAuditActions.OrganizationBranchReactivated,
                    result.Value,
                    organizationId,
                    OrganizationGovernanceAuditWriter.BranchSummary(result.Value, "Reactivated"),
                    ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });
        root.MapPost("/branches/{branchId:guid}/archive", async (Guid organizationId, Guid branchId, GovernanceCriticalActionRequest body, ArchiveBranch useCase, ConsumeGovernanceStepUpGrant stepUp, PlatformOrganizationAuthz authz, PlatformAuthz platformAuthz, CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(organizationId, PlatformAuditActions.OrganizationBranchArchived, ct).ConfigureAwait(false);
            if (denied is not null) return denied;

            var reasonError = GovernanceCriticalActionReason.ValidateRequired(body.Reason);
            if (reasonError is not null)
            {
                return PlatformApiResults.Problem(reasonError.ErrorCode!, reasonError.ErrorMessage!, StatusCodes.Status400BadRequest);
            }

            var actor = platformAuthz.CurrentActor.PlatformUserId;
            var stepUpDenied = await GovernanceStepUpHelper.EnsureConsumedAsync(
                stepUp,
                actor,
                PlatformOrganizationId.From(organizationId),
                GovernanceCriticalActionCodes.BranchArchive,
                GovernanceStepUpTargetTypes.OrganizationBranch,
                branchId,
                body.StepUpToken,
                ct).ConfigureAwait(false);
            if (stepUpDenied is not null) return stepUpDenied;

            var result = await useCase.ExecuteAsync(PlatformOrganizationId.From(organizationId), OrganizationBranchId.From(branchId), ct).ConfigureAwait(false);
            if (result.IsSuccess && result.Value is not null)
            {
                await OrganizationGovernanceAuditWriter.WriteBranchAsync(
                    authz,
                    PlatformAuditActions.OrganizationBranchArchived,
                    result.Value,
                    organizationId,
                    OrganizationGovernanceAuditWriter.BranchSummary(result.Value, $"Archived. Reason: {body.Reason!.Trim()}"),
                    ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });
        root.MapPost("/branches/{branchId:guid}/set-primary", async (
            Guid organizationId,
            Guid branchId,
            GovernanceCriticalActionRequest body,
            SetPrimaryBranch useCase,
            ConsumeGovernanceStepUpGrant stepUp,
            PlatformOrganizationAuthz authz,
            PlatformAuthz platformAuthz,
            CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(
                organizationId,
                PlatformAuditActions.OrganizationBranchPrimaryChanged,
                ct).ConfigureAwait(false);
            if (denied is not null) return denied;

            var reasonError = GovernanceCriticalActionReason.ValidateRequired(body.Reason);
            if (reasonError is not null)
            {
                return PlatformApiResults.Problem(reasonError.ErrorCode!, reasonError.ErrorMessage!, StatusCodes.Status400BadRequest);
            }

            var actor = platformAuthz.CurrentActor.PlatformUserId;
            var stepUpDenied = await GovernanceStepUpHelper.EnsureConsumedAsync(
                stepUp,
                actor,
                PlatformOrganizationId.From(organizationId),
                GovernanceCriticalActionCodes.BranchSetPrimary,
                GovernanceStepUpTargetTypes.OrganizationBranch,
                branchId,
                body.StepUpToken,
                ct).ConfigureAwait(false);
            if (stepUpDenied is not null) return stepUpDenied;

            var result = await useCase.ExecuteAsync(
                PlatformOrganizationId.From(organizationId),
                OrganizationBranchId.From(branchId),
                ct).ConfigureAwait(false);
            if (result.IsSuccess && result.Value is not null)
            {
                await OrganizationGovernanceAuditWriter.WriteBranchAsync(
                    authz,
                    PlatformAuditActions.OrganizationBranchPrimaryChanged,
                    result.Value,
                    organizationId,
                    OrganizationGovernanceAuditWriter.BranchSummary(
                        result.Value,
                        $"Primary branch changed. Reason: {body.Reason!.Trim()}"),
                    ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });
        root.MapGet("/branches/{branchId:guid}/staff-access", async (
            Guid organizationId,
            Guid branchId,
            ListBranchStaffAccess useCase,
            PlatformOrganizationAuthz authz,
            CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(
                organizationId,
                PlatformAuditActions.PlatformAccessChecked,
                ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            return PlatformApiResults.FromResult(
                await useCase.ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    OrganizationBranchId.From(branchId),
                    ct).ConfigureAwait(false),
                Results.Ok);
        });
        root.MapPut("/branches/{branchId:guid}/delivery-policy", async (Guid organizationId, Guid branchId, UpsertBranchDeliveryPolicyRequest body, UpsertBranchDeliveryPolicy useCase, PlatformOrganizationAuthz authz, CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(organizationId, PlatformAuditActions.OrganizationBranchDeliveryPolicyUpdated, ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            var result = await useCase.ExecuteAsync(
                PlatformOrganizationId.From(organizationId),
                OrganizationBranchId.From(branchId),
                new UpsertBranchDeliveryPolicyCommand(
                    body.MinimumOrderAmount,
                    body.BaseDeliveryFee,
                    body.IncludedDistanceKm,
                    body.AdditionalFeePerKm,
                    body.MaximumDeliveryDistanceKm,
                    body.FreeDeliveryThreshold),
                ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.OrganizationBranchDeliveryPolicyUpdated,
                    nameof(OrganizationBranch),
                    branchId.ToString("D"),
                    organizationId,
                    summary: OrganizationGovernanceAuditWriter.BranchConfigSummary(branchId, "Updated delivery policy"),
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });
        root.MapPost("/branches/{branchId:guid}/delivery-fee-preview", async (Guid organizationId, Guid branchId, DeliveryFeePreviewRequest body, PreviewBranchDeliveryFee useCase, PlatformOrganizationAuthz authz, CancellationToken ct) =>
        {
            var denied = await authz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            return PlatformApiResults.FromResult(await useCase.ExecuteAsync(
                PlatformOrganizationId.From(organizationId),
                OrganizationBranchId.From(branchId),
                body,
                ct).ConfigureAwait(false), Results.Ok);
        });

        root.MapGet("/branches/{branchId:guid}/fulfillment-readiness", async (
            Guid organizationId,
            Guid branchId,
            GetBranchFulfillmentReadiness useCase,
            PlatformOrganizationAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            return PlatformApiResults.FromResult(
                await useCase.ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    OrganizationBranchId.From(branchId),
                    ct).ConfigureAwait(false),
                Results.Ok);
        });

        root.MapGet("/branches/{branchId:guid}/operating-hours", async (
            Guid organizationId,
            Guid branchId,
            GetBranchOperatingHours useCase,
            PlatformOrganizationAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            return PlatformApiResults.FromResult(
                await useCase.ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    OrganizationBranchId.From(branchId),
                    ct).ConfigureAwait(false),
                Results.Ok);
        });

        root.MapPut("/branches/{branchId:guid}/operating-hours", async (
            Guid organizationId,
            Guid branchId,
            UpsertBranchOperatingHoursRequest body,
            UpsertBranchOperatingHours useCase,
            PlatformOrganizationAuthz authz,
            CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(organizationId, PlatformAuditActions.OrganizationBranchHoursUpdated, ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            var result = await useCase.ExecuteAsync(
                PlatformOrganizationId.From(organizationId),
                OrganizationBranchId.From(branchId),
                new UpsertBranchOperatingHoursCommand(body.Days ?? []),
                ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.OrganizationBranchHoursUpdated,
                    nameof(OrganizationBranch),
                    branchId.ToString("D"),
                    organizationId,
                    summary: OrganizationGovernanceAuditWriter.BranchConfigSummary(branchId, "Updated operating hours"),
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        root.MapPut("/branches/{branchId:guid}/fulfillment-settings", async (
            Guid organizationId,
            Guid branchId,
            UpdateBranchFulfillmentSettingsRequest body,
            UpdateBranchFulfillmentSettings useCase,
            PlatformOrganizationAuthz authz,
            CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(organizationId, PlatformAuditActions.OrganizationBranchFulfillmentUpdated, ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            var result = await useCase.ExecuteAsync(
                PlatformOrganizationId.From(organizationId),
                OrganizationBranchId.From(branchId),
                new UpdateBranchFulfillmentSettingsCommand(
                    body.CustomerOrderingEnabled,
                    body.PickupEnabled,
                    body.DeliveryEnabled),
                ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.OrganizationBranchFulfillmentUpdated,
                    nameof(OrganizationBranch),
                    branchId.ToString("D"),
                    organizationId,
                    summary: OrganizationGovernanceAuditWriter.BranchConfigSummary(branchId, "Updated fulfillment settings"),
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        root.MapPost("/branches/{branchId:guid}/online-orders-pause", async (
            Guid organizationId,
            Guid branchId,
            SetBranchOnlineOrdersPausedRequest body,
            SetBranchOnlineOrdersPaused useCase,
            PlatformOrganizationAuthz authz,
            CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(organizationId, PlatformAuditActions.OrganizationBranchOrdersPaused, ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            var result = await useCase.ExecuteAsync(
                PlatformOrganizationId.From(organizationId),
                OrganizationBranchId.From(branchId),
                new SetBranchOnlineOrdersPausedCommand(body.Paused, body.Reason),
                ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.OrganizationBranchOrdersPaused,
                    nameof(OrganizationBranch),
                    branchId.ToString("D"),
                    organizationId,
                    summary: OrganizationGovernanceAuditWriter.PauseSummary(branchId, body.Paused, body.Reason),
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        root.MapGet("/branches/{branchId:guid}/delivery-service-areas", async (
            Guid organizationId,
            Guid branchId,
            ListBranchDeliveryServiceAreas useCase,
            PlatformOrganizationAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            return PlatformApiResults.FromResult(
                await useCase.ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    OrganizationBranchId.From(branchId),
                    ct).ConfigureAwait(false),
                Results.Ok);
        });

        root.MapPost("/branches/{branchId:guid}/delivery-service-areas", async (
            Guid organizationId,
            Guid branchId,
            AddBranchDeliveryServiceAreaRequest body,
            AddBranchDeliveryServiceArea useCase,
            PlatformOrganizationAuthz authz,
            CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(
                organizationId,
                PlatformAuditActions.OrganizationBranchDeliveryServiceAreaAdded,
                ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            var result = await useCase.ExecuteAsync(
                PlatformOrganizationId.From(organizationId),
                OrganizationBranchId.From(branchId),
                new AddBranchDeliveryServiceAreaCommand(body.PsgcCode ?? string.Empty),
                ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.OrganizationBranchDeliveryServiceAreaAdded,
                    nameof(OrganizationBranch),
                    branchId.ToString("D"),
                    organizationId,
                    summary: OrganizationGovernanceAuditWriter.BranchConfigSummary(branchId, "Added delivery service area"),
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        root.MapDelete("/branches/{branchId:guid}/delivery-service-areas/{areaId:guid}", async (
            Guid organizationId,
            Guid branchId,
            Guid areaId,
            DeactivateBranchDeliveryServiceArea useCase,
            PlatformOrganizationAuthz authz,
            CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(
                organizationId,
                PlatformAuditActions.OrganizationBranchDeliveryServiceAreaDeactivated,
                ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            var result = await useCase.ExecuteAsync(
                PlatformOrganizationId.From(organizationId),
                OrganizationBranchId.From(branchId),
                BranchDeliveryServiceAreaId.From(areaId),
                ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.OrganizationBranchDeliveryServiceAreaDeactivated,
                    nameof(OrganizationBranch),
                    branchId.ToString("D"),
                    organizationId,
                    summary: OrganizationGovernanceAuditWriter.BranchConfigSummary(branchId, "Deactivated delivery service area"),
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        root.MapGet("/pos-devices", async (Guid organizationId, ListDevices useCase, PlatformOrganizationAuthz authz, CancellationToken ct) =>
        {
            var denied = await authz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            return denied ?? Results.Ok(await useCase.ExecuteAsync(PlatformOrganizationId.From(organizationId), ct).ConfigureAwait(false));
        });
        // Governing/support history including revoked — not the normal customer Device Management list.
        root.MapGet("/pos-devices/history", async (Guid organizationId, ListAllDevices useCase, PlatformOrganizationAuthz authz, CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(
                organizationId,
                PlatformAuditActions.PlatformAccessChecked,
                ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            return Results.Ok(await useCase.ExecuteAsync(PlatformOrganizationId.From(organizationId), ct).ConfigureAwait(false));
        });
        root.MapGet("/pos-devices/capacity", async (Guid organizationId, GetDeviceCapacity useCase, PlatformOrganizationAuthz authz, CancellationToken ct) =>
        {
            var denied = await authz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            return PlatformApiResults.FromResult(await useCase.ExecuteAsync(PlatformOrganizationId.From(organizationId), ct).ConfigureAwait(false), Results.Ok);
        });
        root.MapPost("/pos-devices/register", async (Guid organizationId, RegisterPosDeviceRequest body, RegisterCurrentDevice useCase, PlatformOrganizationAuthz authz, CancellationToken ct) =>
        {
            // Any active org member may register *this* installation for POS execution.
            // Capacity and branch rules stay authoritative in RegisterCurrentDevice.
            // Governing-admin-only create-token remains for MAUI compatibility.
            var denied = await authz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            var result = await useCase.ExecuteAsync(PlatformOrganizationId.From(organizationId),
                new(body.BranchId, body.InstallationDeviceId ?? string.Empty, body.FriendlyName ?? string.Empty, body.Platform, body.Model, body.AppVersion), ct).ConfigureAwait(false);
            if (result.IsSuccess && result.Value is not null)
            {
                var auditAction = result.Value.Kind switch
                {
                    PosDeviceRegisterKind.Reactivated => PlatformAuditActions.PosDeviceReactivated,
                    PosDeviceRegisterKind.New => PlatformAuditActions.PosDeviceRegistered,
                    _ => null,
                };
                if (auditAction is not null)
                {
                    var summaryLabel = result.Value.Kind == PosDeviceRegisterKind.Reactivated ? "Reactivated" : "Registered";
                    await OrganizationGovernanceAuditWriter.WriteDeviceAsync(
                        authz,
                        auditAction,
                        result.Value.Device,
                        organizationId,
                        OrganizationGovernanceAuditWriter.DeviceSummary(result.Value.Device, summaryLabel),
                        ct).ConfigureAwait(false);
                }
            }

            return PlatformApiResults.FromResult(
                result.IsSuccess && result.Value is not null
                    ? ApplicationResult<PosDeviceDto>.Success(result.Value.Device)
                    : ApplicationResult<PosDeviceDto>.Failure(result.ErrorCode!, result.ErrorMessage!),
                Results.Ok);
        });
        root.MapPost("/pos-devices/registration-tokens", async (
            Guid organizationId,
            CreatePosDeviceRegistrationToken useCase,
            PlatformOrganizationAuthz authz,
            CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(
                organizationId,
                PlatformAuditActions.PosDeviceRegistrationTokenCreated,
                ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            if (authz.Inner.CurrentActor.PlatformUserId is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.PosDeviceNotAuthorized,
                    "A signed-in Platform user is required.",
                    StatusCodes.Status403Forbidden);
            }

            return PlatformApiResults.FromResult(
                await useCase.ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    authz.Inner.CurrentActor.PlatformUserId,
                    ct).ConfigureAwait(false),
                Results.Ok);
        });
        root.MapPost("/pos-devices/registration-tokens/redeem", async (
            Guid organizationId,
            RedeemPosDeviceRegistrationTokenRequest body,
            RedeemPosDeviceRegistrationToken useCase,
            PlatformOrganizationAuthz authz,
            CancellationToken ct) =>
        {
            // Membership is enforced inside the use case; allow any authenticated member session
            // (not only governing admins) so a staff operator on the scanning device can redeem.
            var denied = await authz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            if (authz.Inner.CurrentActor.PlatformUserId is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.PosDeviceNotAuthorized,
                    "A signed-in Platform user is required.",
                    StatusCodes.Status403Forbidden);
            }

            return PlatformApiResults.FromResult(
                await useCase.ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    authz.Inner.CurrentActor.PlatformUserId,
                    new RedeemPosDeviceRegistrationTokenCommand(
                        body.Token ?? string.Empty,
                        body.BranchId,
                        body.InstallationDeviceId ?? string.Empty,
                        body.FriendlyName ?? string.Empty,
                        body.Platform,
                        body.Model,
                        body.AppVersion),
                    ct).ConfigureAwait(false),
                Results.Ok);
        });
        root.MapGet("/pos-devices/registration-tokens/{tokenId:guid}", async (
            Guid organizationId,
            Guid tokenId,
            GetPosDeviceRegistrationTokenMetadata useCase,
            PlatformOrganizationAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            return PlatformApiResults.FromResult(
                await useCase.ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    PosDeviceRegistrationTokenId.From(tokenId),
                    ct).ConfigureAwait(false),
                Results.Ok);
        });
        root.MapPut("/pos-devices/{deviceId:guid}", async (Guid organizationId, Guid deviceId, RenamePosDeviceRequest body, RenameDevice useCase, PlatformOrganizationAuthz authz, CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(organizationId, PlatformAuditActions.PosDeviceRenamed, ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            var result = await useCase.ExecuteAsync(PlatformOrganizationId.From(organizationId), PosDeviceId.From(deviceId), body.FriendlyName ?? string.Empty, ct).ConfigureAwait(false);
            if (result.IsSuccess && result.Value is not null)
            {
                await OrganizationGovernanceAuditWriter.WriteDeviceAsync(
                    authz,
                    PlatformAuditActions.PosDeviceRenamed,
                    result.Value,
                    organizationId,
                    OrganizationGovernanceAuditWriter.DeviceSummary(result.Value, "Renamed"),
                    ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });
        root.MapPost("/pos-devices/{deviceId:guid}/revoke", async (Guid organizationId, Guid deviceId, GovernanceCriticalActionRequest body, RevokeDevice useCase, ConsumeGovernanceStepUpGrant stepUp, PlatformOrganizationAuthz authz, PlatformAuthz platformAuthz, CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(organizationId, PlatformAuditActions.PosDeviceRevoked, ct).ConfigureAwait(false);
            if (denied is not null) return denied;

            var reasonError = GovernanceCriticalActionReason.ValidateRequired(body.Reason);
            if (reasonError is not null)
            {
                return PlatformApiResults.Problem(reasonError.ErrorCode!, reasonError.ErrorMessage!, StatusCodes.Status400BadRequest);
            }

            var actor = platformAuthz.CurrentActor.PlatformUserId;
            if (actor is null) return PlatformApiResults.Problem(ApplicationErrorCodes.PosDeviceNotAuthorized, "A signed-in Platform user is required.", StatusCodes.Status403Forbidden);

            var stepUpDenied = await GovernanceStepUpHelper.EnsureConsumedAsync(
                stepUp,
                actor,
                PlatformOrganizationId.From(organizationId),
                GovernanceCriticalActionCodes.PosDeviceRevoke,
                GovernanceStepUpTargetTypes.PosDevice,
                deviceId,
                body.StepUpToken,
                ct).ConfigureAwait(false);
            if (stepUpDenied is not null) return stepUpDenied;

            var result = await useCase.ExecuteAsync(PlatformOrganizationId.From(organizationId), PosDeviceId.From(deviceId), actor, ct).ConfigureAwait(false);
            if (result.IsSuccess && result.Value is not null)
            {
                await OrganizationGovernanceAuditWriter.WriteDeviceAsync(
                    authz,
                    PlatformAuditActions.PosDeviceRevoked,
                    result.Value,
                    organizationId,
                    OrganizationGovernanceAuditWriter.DeviceSummary(result.Value, $"Revoked. Reason: {body.Reason!.Trim()}"),
                    ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });
        root.MapPost("/pos-devices/authorize", async (Guid organizationId, AuthorizePosDeviceRequest body, AuthorizeForTransactions useCase, PlatformOrganizationAuthz authz, CancellationToken ct) =>
        {
            var denied = await authz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            return PlatformApiResults.FromResult(await useCase.ExecuteAsync(PlatformOrganizationId.From(organizationId), body.InstallationDeviceId ?? string.Empty,
                body.BranchId is Guid branchId ? OrganizationBranchId.From(branchId) : null, ct).ConfigureAwait(false), Results.Ok);
        });
        return app;
    }
}

internal sealed record CreateBranchRequest(
    string? Code,
    string? Name,
    string? AddressLine1 = null,
    string? AddressLine2 = null,
    string? City = null,
    string? Region = null,
    string? PostalCode = null,
    string? CountryCode = null,
    decimal? Latitude = null,
    decimal? Longitude = null,
    bool? PickupEnabled = null,
    bool? DeliveryEnabled = null,
    bool? CustomerOrderingEnabled = null,
    string? ContactPhone = null,
    string? TimeZoneId = null);
internal sealed record UpdateBranchRequest(
    string? Name,
    string? AddressLine1 = null,
    string? AddressLine2 = null,
    string? City = null,
    string? Region = null,
    string? PostalCode = null,
    string? CountryCode = null,
    string? Status = null,
    decimal? Latitude = null,
    decimal? Longitude = null,
    bool? ClearCoordinates = null,
    string? ContactPhone = null,
    string? TimeZoneId = null);
internal sealed record UpsertBranchDeliveryPolicyRequest(
    decimal MinimumOrderAmount,
    decimal BaseDeliveryFee,
    decimal IncludedDistanceKm,
    decimal AdditionalFeePerKm,
    decimal MaximumDeliveryDistanceKm,
    decimal? FreeDeliveryThreshold = null);
internal sealed record RegisterPosDeviceRequest(Guid BranchId, string? InstallationDeviceId, string? FriendlyName, string? Platform = null, string? Model = null, string? AppVersion = null);
internal sealed record RedeemPosDeviceRegistrationTokenRequest(
    string? Token,
    Guid BranchId,
    string? InstallationDeviceId,
    string? FriendlyName,
    string? Platform = null,
    string? Model = null,
    string? AppVersion = null);
internal sealed record RenamePosDeviceRequest(string? FriendlyName);
internal sealed record AuthorizePosDeviceRequest(string? InstallationDeviceId, Guid? BranchId = null);
internal sealed record SelectBranchContextRequest(Guid BranchId);

internal sealed record UpsertBranchOperatingHoursRequest(IReadOnlyList<BranchOperatingHoursDayDto>? Days);

internal sealed record UpdateBranchFulfillmentSettingsRequest(
    bool? CustomerOrderingEnabled = null,
    bool? PickupEnabled = null,
    bool? DeliveryEnabled = null);

internal sealed record SetBranchOnlineOrdersPausedRequest(bool Paused, string? Reason = null);

internal sealed record AddBranchDeliveryServiceAreaRequest(string? PsgcCode);
