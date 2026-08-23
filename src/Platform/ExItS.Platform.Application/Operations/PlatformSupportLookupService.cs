using ExItS.Platform.Application.Admin;
using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;

namespace ExItS.Platform.Application.Operations;

public sealed class PlatformSupportLookupService
{
    private readonly OrganizationQueryService _organizations;
    private readonly AdminPortfolioQueryService _portfolio;
    private readonly SubscriptionQueryService _subscriptions;
    private readonly SaaSPaymentQueryService _payments;
    private readonly PlatformUserQueryService _users;
    private readonly ResolvePublicUserId _resolvePublicUser;
    private readonly ResolvePublicOrganizationId _resolvePublicOrganization;
    private readonly IPosDeviceRepository _devices;
    private readonly ListDevices _listDevices;
    private readonly QueryAuditRecords _auditRecords;

    public PlatformSupportLookupService(
        OrganizationQueryService organizations,
        AdminPortfolioQueryService portfolio,
        SubscriptionQueryService subscriptions,
        SaaSPaymentQueryService payments,
        PlatformUserQueryService users,
        ResolvePublicUserId resolvePublicUser,
        ResolvePublicOrganizationId resolvePublicOrganization,
        IPosDeviceRepository devices,
        ListDevices listDevices,
        QueryAuditRecords auditRecords)
    {
        _organizations = organizations;
        _portfolio = portfolio;
        _subscriptions = subscriptions;
        _payments = payments;
        _users = users;
        _resolvePublicUser = resolvePublicUser;
        _resolvePublicOrganization = resolvePublicOrganization;
        _devices = devices;
        _listDevices = listDevices;
        _auditRecords = auditRecords;
    }

    public async Task<ApplicationResult<PlatformSupportLookupResponse>> LookupAsync(
        PlatformUserId actorUserId,
        PlatformSupportLookupRequest request,
        CancellationToken cancellationToken = default)
    {
        var mode = request.Mode?.Trim() ?? string.Empty;
        var query = request.Query?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(mode) || string.IsNullOrWhiteSpace(query))
        {
            return ApplicationResult<PlatformSupportLookupResponse>.Failure(
                ApplicationErrorCodes.OperationsRequestInvalid,
                "Lookup mode and query are required.");
        }

        try
        {
            return mode switch
            {
                PlatformSupportLookupModes.Organization => await LookupOrganizationAsync(query, null, null, null, null, cancellationToken),
                PlatformSupportLookupModes.PublicOrganizationId => await LookupPublicOrganizationAsync(actorUserId, query, cancellationToken),
                PlatformSupportLookupModes.UserEmail => await LookupUserEmailAsync(query, cancellationToken),
                PlatformSupportLookupModes.PublicUserId => await LookupPublicUserAsync(actorUserId, query, cancellationToken),
                PlatformSupportLookupModes.SubscriptionId => await LookupSubscriptionAsync(query, cancellationToken),
                PlatformSupportLookupModes.PaymentId => await LookupPaymentAsync(query, cancellationToken),
                PlatformSupportLookupModes.PaymentReference => await LookupPaymentReferenceAsync(query, request.PaymentMethod, cancellationToken),
                PlatformSupportLookupModes.DeviceId => await LookupDeviceAsync(query, cancellationToken),
                _ => ApplicationResult<PlatformSupportLookupResponse>.Failure(
                    ApplicationErrorCodes.OperationsRequestInvalid,
                    "Support lookup mode is not recognized."),
            };
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformSupportLookupResponse>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<ApplicationResult<PlatformSupportLookupResponse>> LookupPublicOrganizationAsync(
        PlatformUserId actorUserId,
        string query,
        CancellationToken cancellationToken)
    {
        var resolved = await _resolvePublicOrganization
            .ExecuteAsync(actorUserId, new ResolvePublicOrganizationIdRequest(query, "platform-admin-support"), cancellationToken)
            .ConfigureAwait(false);
        if (!resolved.IsSuccess || resolved.Value is null)
        {
            return ApplicationResult<PlatformSupportLookupResponse>.Failure(
                resolved.ErrorCode ?? ApplicationErrorCodes.OrganizationNotFound,
                resolved.ErrorMessage ?? "Organization was not found.");
        }

        return await LookupOrganizationAsync(resolved.Value.OrganizationId.ToString("D"), null, null, null, null, cancellationToken);
    }

    private async Task<ApplicationResult<PlatformSupportLookupResponse>> LookupPublicUserAsync(
        PlatformUserId actorUserId,
        string query,
        CancellationToken cancellationToken)
    {
        var resolved = await _resolvePublicUser
            .ExecuteAsync(actorUserId, new ResolvePublicUserIdRequest(query, "platform-admin-support"), cancellationToken)
            .ConfigureAwait(false);
        if (!resolved.IsSuccess || resolved.Value is null)
        {
            return ApplicationResult<PlatformSupportLookupResponse>.Failure(
                resolved.ErrorCode ?? ApplicationErrorCodes.UserNotFound,
                resolved.ErrorMessage ?? "User was not found.");
        }

        var user = await _users.GetByIdAsync(resolved.Value.UserIdentityId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApplicationResult<PlatformSupportLookupResponse>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "User was not found.");
        }

        return ApplicationResult<PlatformSupportLookupResponse>.Success(
            new PlatformSupportLookupResponse(
                Organization: null,
                User: user,
                Subscription: null,
                Payment: null,
                Device: null,
                Subscriptions: [],
                LatestEntitlements: [],
                Payments: [],
                Devices: [],
                RecentAudit: []));
    }

    private async Task<ApplicationResult<PlatformSupportLookupResponse>> LookupUserEmailAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var page = await _users
            .ListAsync(status: null, search: query, page: 1, pageSize: 5, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var user = page.Items.FirstOrDefault();
        if (user is null)
        {
            return ApplicationResult<PlatformSupportLookupResponse>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "User was not found.");
        }

        return ApplicationResult<PlatformSupportLookupResponse>.Success(
            new PlatformSupportLookupResponse(
                Organization: null,
                User: user,
                Subscription: null,
                Payment: null,
                Device: null,
                Subscriptions: [],
                LatestEntitlements: [],
                Payments: [],
                Devices: [],
                RecentAudit: []));
    }

    private async Task<ApplicationResult<PlatformSupportLookupResponse>> LookupSubscriptionAsync(
        string query,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(query, out var subscriptionId))
        {
            return ApplicationResult<PlatformSupportLookupResponse>.Failure(
                ApplicationErrorCodes.OperationsRequestInvalid,
                "Subscription ID must be a GUID.");
        }

        var subscription = await _subscriptions.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return ApplicationResult<PlatformSupportLookupResponse>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found.");
        }

        return await LookupOrganizationAsync(
            subscription.OrganizationId.ToString("D"),
            subscription,
            null,
            null,
            null,
            cancellationToken);
    }

    private async Task<ApplicationResult<PlatformSupportLookupResponse>> LookupPaymentAsync(
        string query,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(query, out var paymentId))
        {
            return ApplicationResult<PlatformSupportLookupResponse>.Failure(
                ApplicationErrorCodes.OperationsRequestInvalid,
                "Payment ID must be a GUID.");
        }

        var payment = await _payments.GetByIdAsync(paymentId, cancellationToken).ConfigureAwait(false);
        if (payment is null)
        {
            return ApplicationResult<PlatformSupportLookupResponse>.Failure(
                ApplicationErrorCodes.PaymentNotFound,
                "Payment was not found.");
        }

        return await LookupOrganizationAsync(
            payment.OrganizationId.ToString("D"),
            null,
            payment,
            null,
            null,
            cancellationToken);
    }

    private async Task<ApplicationResult<PlatformSupportLookupResponse>> LookupPaymentReferenceAsync(
        string query,
        string? paymentMethod,
        CancellationToken cancellationToken)
    {
        SaaSPaymentMethod? method = null;
        if (!string.IsNullOrWhiteSpace(paymentMethod))
        {
            if (!Enum.TryParse<SaaSPaymentMethod>(paymentMethod, ignoreCase: true, out var parsed))
            {
                return ApplicationResult<PlatformSupportLookupResponse>.Failure(
                    ApplicationErrorCodes.OperationsRequestInvalid,
                    "Payment method is not recognized.");
            }

            method = parsed;
        }

        var matches = await _payments
            .FindByNormalizedReferenceAsync(query, method, cancellationToken)
            .ConfigureAwait(false);
        if (matches.Count == 0)
        {
            return ApplicationResult<PlatformSupportLookupResponse>.Failure(
                ApplicationErrorCodes.PaymentNotFound,
                "Payment was not found.");
        }

        if (matches.Count > 1)
        {
            return ApplicationResult<PlatformSupportLookupResponse>.Failure(
                ApplicationErrorCodes.OperationsRequestInvalid,
                "Multiple payments matched the reference. Provide payment method to disambiguate.");
        }

        var payment = matches[0];
        return await LookupOrganizationAsync(
            payment.OrganizationId.ToString("D"),
            null,
            payment,
            null,
            null,
            cancellationToken);
    }

    private async Task<ApplicationResult<PlatformSupportLookupResponse>> LookupDeviceAsync(
        string query,
        CancellationToken cancellationToken)
    {
        PosDevice? device = null;
        if (Guid.TryParse(query, out var deviceId))
        {
            device = await _devices.GetByIdAsync(PosDeviceId.From(deviceId), cancellationToken).ConfigureAwait(false);
        }

        device ??= await _devices
            .FindByInstallationDeviceIdAsync(query, cancellationToken)
            .ConfigureAwait(false);

        if (device is null)
        {
            return ApplicationResult<PlatformSupportLookupResponse>.Failure(
                ApplicationErrorCodes.PosDeviceNotFound,
                "POS device was not found.");
        }

        var dto = DeviceMapper.ToDto(device);
        return await LookupOrganizationAsync(
            device.OrganizationId.Value.ToString("D"),
            null,
            null,
            dto,
            null,
            cancellationToken);
    }

    private async Task<ApplicationResult<PlatformSupportLookupResponse>> LookupOrganizationAsync(
        string query,
        SubscriptionDto? matchedSubscription,
        SaaSPaymentDto? matchedPayment,
        PosDeviceDto? matchedDevice,
        PlatformUserDto? matchedUser,
        CancellationToken cancellationToken)
    {
        PlatformOrganizationDto? organization;
        if (Guid.TryParse(query, out var organizationId))
        {
            organization = await _organizations.GetByIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var page = await _organizations
                .ListAsync(1, 5, status: null, search: query, sortBy: null, sortDesc: null, productCode: null, cancellationToken)
                .ConfigureAwait(false);
            organization = page.Items.FirstOrDefault();
        }

        if (organization is null)
        {
            return ApplicationResult<PlatformSupportLookupResponse>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Organization was not found.");
        }

        var summary = await _portfolio
            .GetOrganizationCommercialSummaryAsync(organization.Id, cancellationToken)
            .ConfigureAwait(false);

        var devices = await _listDevices
            .ExecuteAsync(PlatformOrganizationId.From(organization.Id), cancellationToken)
            .ConfigureAwait(false);

        var audit = await _auditRecords.ExecuteAsync(
            occurredFromUtc: null,
            occurredToUtc: null,
            actorIdentifier: null,
            actorType: null,
            actionCode: null,
            targetType: null,
            targetId: null,
            organizationId: organization.Id,
            productCode: null,
            outcome: null,
            correlationId: null,
            page: 1,
            pageSize: 10,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ApplicationResult<PlatformSupportLookupResponse>.Success(
            new PlatformSupportLookupResponse(
                Organization: organization,
                User: matchedUser,
                Subscription: matchedSubscription,
                Payment: matchedPayment,
                Device: matchedDevice,
                Subscriptions: summary?.Subscriptions.Select(MapCommercialSubscription).ToList() ?? [],
                LatestEntitlements: summary?.LatestEntitlements.Select(MapEntitlement).ToList() ?? [],
                Payments: summary?.Payments.Select(MapCommercialPayment).ToList() ?? [],
                Devices: devices,
                RecentAudit: audit.Items));
    }

    private static PlatformSupportCommercialSubscriptionDto MapCommercialSubscription(SubscriptionDto dto) =>
        new(dto.Id, dto.ProductCode, dto.Status, dto.PlanDisplayName, dto.PlanKey);

    private static PlatformSupportCommercialEntitlementDto MapEntitlement(EntitlementLatestSummaryDto dto) =>
        new(dto.Id, dto.ProductCode, dto.SubscriptionStatus, dto.ProductDisplayName, dto.SnapshotVersion);

    private static PlatformSupportCommercialPaymentDto MapCommercialPayment(SaaSPaymentDto dto) =>
        new(dto.Id, dto.ProductCode, dto.Status, dto.ExternalReference, dto.PaidAtUtc);
}
