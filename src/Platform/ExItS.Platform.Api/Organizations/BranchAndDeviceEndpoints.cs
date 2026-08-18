using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Api.Organizations;

internal static class BranchAndDeviceEndpoints
{
    public static IEndpointRouteBuilder MapBranchAndDeviceEndpoints(this IEndpointRouteBuilder app)
    {
        var root = app.MapGroup("/api/v1/platform/organizations/{organizationId:guid}");
        root.MapGet("/branches", async (Guid organizationId, ListBranches useCase, PlatformOrganizationAuthz authz, CancellationToken ct) =>
        {
            var denied = await authz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            return denied ?? Results.Ok(await useCase.ExecuteAsync(PlatformOrganizationId.From(organizationId), ct).ConfigureAwait(false));
        });
        root.MapPut("/branch-context", async (
            Guid organizationId,
            SelectBranchContextRequest body,
            SelectOrganizationBranchContext useCase,
            PlatformOrganizationAuthz authz,
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
            return PlatformApiResults.FromResult(
                await useCase.ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    OrganizationBranchId.From(body.BranchId),
                    ct).ConfigureAwait(false),
                Results.Ok);
        });
        root.MapGet("/branches/capacity", async (Guid organizationId, GetBranchCapacity useCase, PlatformOrganizationAuthz authz, CancellationToken ct) =>
        {
            var denied = await authz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            return PlatformApiResults.FromResult(await useCase.ExecuteAsync(PlatformOrganizationId.From(organizationId), ct).ConfigureAwait(false), Results.Ok);
        });
        root.MapPost("/branches", async (Guid organizationId, CreateBranchRequest body, CreateBranch useCase, PlatformOrganizationAuthz authz, CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(organizationId, "platform.organization.branch_created", ct).ConfigureAwait(false);
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
                    body.CustomerOrderingEnabled ?? false), ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, x => Results.Created($"/api/v1/platform/organizations/{organizationId}/branches/{x.Id}", x));
        });
        root.MapPut("/branches/{branchId:guid}", async (Guid organizationId, Guid branchId, UpdateBranchRequest body, UpdateBranch useCase, PlatformOrganizationAuthz authz, CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(organizationId, "platform.organization.branch_updated", ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            var status = string.IsNullOrWhiteSpace(body.Status) ? null : Enum.TryParse<OrganizationBranchStatus>(body.Status, true, out var value) ? value : (OrganizationBranchStatus?)null;
            if (!string.IsNullOrWhiteSpace(body.Status) && status is null) return PlatformApiResults.Problem(ApplicationErrorCodes.DomainViolation, "Branch status is invalid.", StatusCodes.Status400BadRequest);
            return PlatformApiResults.FromResult(await useCase.ExecuteAsync(PlatformOrganizationId.From(organizationId), OrganizationBranchId.From(branchId),
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
                    body.TimeZoneId), ct).ConfigureAwait(false), Results.Ok);
        });
        root.MapPost("/branches/{branchId:guid}/archive", async (Guid organizationId, Guid branchId, ArchiveBranch useCase, PlatformOrganizationAuthz authz, CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(organizationId, "platform.organization.branch_archived", ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            return PlatformApiResults.FromResult(await useCase.ExecuteAsync(PlatformOrganizationId.From(organizationId), OrganizationBranchId.From(branchId), ct).ConfigureAwait(false), Results.Ok);
        });
        root.MapPut("/branches/{branchId:guid}/delivery-policy", async (Guid organizationId, Guid branchId, UpsertBranchDeliveryPolicyRequest body, UpsertBranchDeliveryPolicy useCase, PlatformOrganizationAuthz authz, CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(organizationId, "platform.organization.branch_delivery_policy_updated", ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            return PlatformApiResults.FromResult(await useCase.ExecuteAsync(
                PlatformOrganizationId.From(organizationId),
                OrganizationBranchId.From(branchId),
                new UpsertBranchDeliveryPolicyCommand(
                    body.MinimumOrderAmount,
                    body.BaseDeliveryFee,
                    body.IncludedDistanceKm,
                    body.AdditionalFeePerKm,
                    body.MaximumDeliveryDistanceKm,
                    body.FreeDeliveryThreshold),
                ct).ConfigureAwait(false), Results.Ok);
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
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(organizationId, "platform.organization.branch_hours_updated", ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            return PlatformApiResults.FromResult(
                await useCase.ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    OrganizationBranchId.From(branchId),
                    new UpsertBranchOperatingHoursCommand(body.Days ?? []),
                    ct).ConfigureAwait(false),
                Results.Ok);
        });

        root.MapPut("/branches/{branchId:guid}/fulfillment-settings", async (
            Guid organizationId,
            Guid branchId,
            UpdateBranchFulfillmentSettingsRequest body,
            UpdateBranchFulfillmentSettings useCase,
            PlatformOrganizationAuthz authz,
            CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(organizationId, "platform.organization.branch_fulfillment_updated", ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            return PlatformApiResults.FromResult(
                await useCase.ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    OrganizationBranchId.From(branchId),
                    new UpdateBranchFulfillmentSettingsCommand(
                        body.CustomerOrderingEnabled,
                        body.PickupEnabled,
                        body.DeliveryEnabled),
                    ct).ConfigureAwait(false),
                Results.Ok);
        });

        root.MapPost("/branches/{branchId:guid}/online-orders-pause", async (
            Guid organizationId,
            Guid branchId,
            SetBranchOnlineOrdersPausedRequest body,
            SetBranchOnlineOrdersPaused useCase,
            PlatformOrganizationAuthz authz,
            CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(organizationId, "platform.organization.branch_orders_paused", ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            return PlatformApiResults.FromResult(
                await useCase.ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    OrganizationBranchId.From(branchId),
                    new SetBranchOnlineOrdersPausedCommand(body.Paused, body.Reason),
                    ct).ConfigureAwait(false),
                Results.Ok);
        });

        root.MapGet("/pos-devices", async (Guid organizationId, ListDevices useCase, PlatformOrganizationAuthz authz, CancellationToken ct) =>
        {
            var denied = await authz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            return denied ?? Results.Ok(await useCase.ExecuteAsync(PlatformOrganizationId.From(organizationId), ct).ConfigureAwait(false));
        });
        root.MapGet("/pos-devices/capacity", async (Guid organizationId, GetDeviceCapacity useCase, PlatformOrganizationAuthz authz, CancellationToken ct) =>
        {
            var denied = await authz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            return PlatformApiResults.FromResult(await useCase.ExecuteAsync(PlatformOrganizationId.From(organizationId), ct).ConfigureAwait(false), Results.Ok);
        });
        root.MapPost("/pos-devices/register", async (Guid organizationId, RegisterPosDeviceRequest body, RegisterCurrentDevice useCase, PlatformOrganizationAuthz authz, CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(organizationId, "platform.organization.pos_device_registered", ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            return PlatformApiResults.FromResult(await useCase.ExecuteAsync(PlatformOrganizationId.From(organizationId),
                new(body.BranchId, body.InstallationDeviceId ?? string.Empty, body.FriendlyName ?? string.Empty, body.Platform, body.Model, body.AppVersion), ct).ConfigureAwait(false), Results.Ok);
        });
        root.MapPost("/pos-devices/registration-tokens", async (
            Guid organizationId,
            CreatePosDeviceRegistrationToken useCase,
            PlatformOrganizationAuthz authz,
            CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(
                organizationId,
                "platform.organization.pos_device_registration_token_created",
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
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(organizationId, "platform.organization.pos_device_renamed", ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            return PlatformApiResults.FromResult(await useCase.ExecuteAsync(PlatformOrganizationId.From(organizationId), PosDeviceId.From(deviceId), body.FriendlyName ?? string.Empty, ct).ConfigureAwait(false), Results.Ok);
        });
        root.MapPost("/pos-devices/{deviceId:guid}/revoke", async (Guid organizationId, Guid deviceId, RevokeDevice useCase, PlatformOrganizationAuthz authz, CancellationToken ct) =>
        {
            var (denied, _) = await authz.EnsureCanEditOrganizationProfileAsync(organizationId, "platform.organization.pos_device_revoked", ct).ConfigureAwait(false);
            if (denied is not null) return denied;
            if (authz.Inner.CurrentActor.PlatformUserId is null) return PlatformApiResults.Problem(ApplicationErrorCodes.PosDeviceNotAuthorized, "A signed-in Platform user is required.", StatusCodes.Status403Forbidden);
            return PlatformApiResults.FromResult(await useCase.ExecuteAsync(PlatformOrganizationId.From(organizationId), PosDeviceId.From(deviceId), authz.Inner.CurrentActor.PlatformUserId, ct).ConfigureAwait(false), Results.Ok);
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
    bool? CustomerOrderingEnabled = null);
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
