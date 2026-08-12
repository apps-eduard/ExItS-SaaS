using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Api.Organizations;

/// <summary>
/// Organization/product-owned Business Customers, Credit Customers, Customer Link Requests,
/// and Linked Customer App Users. Distinct from Organization Staff and Personal Utang.
/// </summary>
internal static class BusinessCustomerEndpoints
{
    public static IEndpointRouteBuilder MapBusinessCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        MapCustomerRoutes(app, "/api/v1/organizations/{organizationId:guid}/customers");
        MapCustomerRoutes(app, "/api/v1/organizations/{organizationId:guid}/products/{productCode}/customers");

        app.MapGet("/api/v1/organizations/{organizationId:guid}/credit-customers", async (
            Guid organizationId,
            int? page,
            int? pageSize,
            CreditCustomerQueryService queries,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.PlatformAccessChecked,
                nameof(CreditCustomer),
                organizationId.ToString("D"),
                organizationId,
                summary: "List credit customers.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await queries.ListAsync(organizationId, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        app.MapPost("/api/v1/organizations/{organizationId:guid}/credit-customers/{creditCustomerId:guid}/close", async (
            Guid organizationId,
            Guid creditCustomerId,
            CloseCreditCustomer useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.CreditCustomerClosed,
                nameof(CreditCustomer),
                creditCustomerId.ToString("D"),
                organizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(
                    CreditCustomerId.From(creditCustomerId),
                    PlatformOrganizationId.From(organizationId),
                    ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await membershipAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.CreditCustomerClosed,
                    nameof(CreditCustomer),
                    creditCustomerId.ToString("D"),
                    organizationId,
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        app.MapGet("/api/v1/organizations/{organizationId:guid}/customer-link-requests", async (
            Guid organizationId,
            string? status,
            int? page,
            int? pageSize,
            CustomerLinkRequestQueryService queries,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.PlatformAccessChecked,
                nameof(CustomerLinkRequest),
                organizationId.ToString("D"),
                organizationId,
                summary: "List customer link requests.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            if (!TryParseLinkStatus(status, out var parsed, out var error))
            {
                return error!;
            }

            var result = await queries
                .ListByOrganizationAsync(organizationId, parsed, page, pageSize, ct)
                .ConfigureAwait(false);
            var sanitized = result with
            {
                Items = result.Items.Select(i => i with { AcceptToken = null }).ToList()
            };
            return Results.Ok(sanitized);
        });

        app.MapGet("/api/v1/organizations/{organizationId:guid}/customer-link-requests/stats", async (
            Guid organizationId,
            CustomerLinkRequestStatsQuery stats,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.PlatformAccessChecked,
                nameof(CustomerLinkRequest),
                organizationId.ToString("D"),
                organizationId,
                summary: "Customer link request stats.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await stats
                .CountByOrganizationAsync(PlatformOrganizationId.From(organizationId), ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        app.MapPost("/api/v1/organizations/{organizationId:guid}/customers/with-personal-link", async (
            Guid organizationId,
            CreateBusinessCustomerWithPersonalLinkBody body,
            CreateBusinessCustomerWithPersonalLink useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.BusinessCustomerCreated,
                nameof(BusinessCustomer),
                organizationId.ToString("D"),
                organizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var customerRequest = new CreateBusinessCustomerRequest(
                body.DisplayName ?? string.Empty,
                body.Email,
                body.Phone,
                body.Notes,
                body.OwningProductCode);
            var targetId = body.TargetUserIdentityId is Guid g && g != Guid.Empty
                ? PlatformUserId.From(g)
                : null;
            var result = await useCase
                .ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    customerRequest,
                    membershipAuthz.Inner.CurrentActor.PlatformUserId,
                    targetId,
                    body.PublicUserId,
                    ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await membershipAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.CustomerLinkRequestCreated,
                    nameof(CustomerLinkRequest),
                    result.Value!.LinkRequest.Id.ToString("D"),
                    organizationId,
                    summary: "Created business customer with personal link request.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(
                result,
                dto => Results.Created(
                    $"/api/v1/organizations/{organizationId}/customers/{dto.Customer.Id}",
                    dto with { LinkRequest = dto.LinkRequest with { AcceptToken = null } }));
        });

        app.MapPost("/api/v1/organizations/{organizationId:guid}/customers/{customerId:guid}/link-requests", async (
            Guid organizationId,
            Guid customerId,
            CreateCustomerLinkBody body,
            CreateCustomerLinkRequest useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.CustomerLinkRequestCreated,
                nameof(CustomerLinkRequest),
                customerId.ToString("D"),
                organizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var targetId = body.TargetUserIdentityId is Guid g && g != Guid.Empty
                ? PlatformUserId.From(g)
                : null;
            var result = await useCase
                .ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    BusinessCustomerId.From(customerId),
                    body.Email,
                    membershipAuthz.Inner.CurrentActor.PlatformUserId,
                    targetId,
                    body.PublicUserId,
                    ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await membershipAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.CustomerLinkRequestCreated,
                    nameof(CustomerLinkRequest),
                    result.Value!.Id.ToString("D"),
                    organizationId,
                    summary: "Created customer link request.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(
                result,
                dto => Results.Created(
                    $"/api/v1/organizations/{organizationId}/customer-link-requests/{dto.Id}",
                    dto));
        });

        app.MapGet("/api/v1/organizations/{organizationId:guid}/customers/{customerId:guid}/link-requests", async (
            Guid organizationId,
            Guid customerId,
            CustomerLinkRequestQueryService queries,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.PlatformAccessChecked,
                nameof(CustomerLinkRequest),
                customerId.ToString("D"),
                organizationId,
                summary: "List customer link request history.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var items = await queries
                .ListByBusinessCustomerAsync(organizationId, customerId, ct)
                .ConfigureAwait(false);
            return Results.Ok(items.Select(i => i with { AcceptToken = null }).ToList());
        });

        app.MapGet("/api/v1/organizations/{organizationId:guid}/customers/{customerId:guid}/link-status", async (
            Guid organizationId,
            Guid customerId,
            GetCustomerLinkStatusForBusinessCustomer useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.PlatformAccessChecked,
                nameof(BusinessCustomer),
                customerId.ToString("D"),
                organizationId,
                summary: "Get customer link status.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    BusinessCustomerId.From(customerId),
                    ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        app.MapGet("/api/v1/organizations/{organizationId:guid}/notifications", async (
            Guid organizationId,
            ListOrganizationInAppNotifications useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var actor = membershipAuthz.Inner.CurrentActor;
            if (actor.PlatformUserId is null)
            {
                return PlatformApiResults.Problem(
                    DomainErrorCodes.AuthorizationDenied,
                    "Authentication is required.",
                    StatusCodes.Status401Unauthorized);
            }

            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.PlatformAccessChecked,
                nameof(OrganizationInAppNotification),
                organizationId.ToString("D"),
                organizationId,
                summary: "List organization in-app notifications.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var list = await useCase
                .ExecuteAsync(PlatformOrganizationId.From(organizationId), actor.PlatformUserId, ct)
                .ConfigureAwait(false);
            return Results.Ok(list);
        });

        app.MapPost("/api/v1/organizations/{organizationId:guid}/notifications/{notificationId:guid}/read", async (
            Guid organizationId,
            Guid notificationId,
            MarkOrganizationInAppNotificationRead useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var actor = membershipAuthz.Inner.CurrentActor;
            if (actor.PlatformUserId is null)
            {
                return PlatformApiResults.Problem(
                    DomainErrorCodes.AuthorizationDenied,
                    "Authentication is required.",
                    StatusCodes.Status401Unauthorized);
            }

            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.PlatformAccessChecked,
                nameof(OrganizationInAppNotification),
                notificationId.ToString("D"),
                organizationId,
                summary: "Mark organization notification read.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    actor.PlatformUserId,
                    notificationId,
                    ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        app.MapPost("/api/v1/organizations/{organizationId:guid}/customer-link-requests/{requestId:guid}/resend", async (
            Guid organizationId,
            Guid requestId,
            ResendCustomerLinkRequest useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.CustomerLinkRequestResent,
                nameof(CustomerLinkRequest),
                requestId.ToString("D"),
                organizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(CustomerLinkRequestId.From(requestId), PlatformOrganizationId.From(organizationId), ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await membershipAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.CustomerLinkRequestResent,
                    nameof(CustomerLinkRequest),
                    requestId.ToString("D"),
                    organizationId,
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        app.MapPost("/api/v1/organizations/{organizationId:guid}/customer-link-requests/{requestId:guid}/revoke", async (
            Guid organizationId,
            Guid requestId,
            RevokeCustomerLinkRequest useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.CustomerLinkRequestRevoked,
                nameof(CustomerLinkRequest),
                requestId.ToString("D"),
                organizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(CustomerLinkRequestId.From(requestId), PlatformOrganizationId.From(organizationId), ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await membershipAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.CustomerLinkRequestRevoked,
                    nameof(CustomerLinkRequest),
                    requestId.ToString("D"),
                    organizationId,
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto with { AcceptToken = null }));
        });

        app.MapPost("/api/v1/organizations/customer-link-requests/accept", async (
            AcceptCustomerLinkBody body,
            AcceptCustomerLinkRequest useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var actor = membershipAuthz.Inner.CurrentActor;
            if (actor.PlatformUserId is null)
            {
                return PlatformApiResults.Problem(
                    DomainErrorCodes.AuthorizationDenied,
                    "Accepting a customer link requires an authenticated user.",
                    StatusCodes.Status401Unauthorized);
            }

            var result = await useCase
                .ExecuteAsync(
                    body.Token ?? string.Empty,
                    actor.PlatformUserId,
                    actor.AccountClass ?? AccountClass.Platform,
                    ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await membershipAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.CustomerLinkRequestAccepted,
                    nameof(CustomerLinkRequest),
                    result.Value!.LinkRequestId.ToString("D"),
                    summary: "Accepted customer link request (no staff membership).",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        app.MapPost("/api/v1/organizations/customer-link-requests/decline", async (
            AcceptCustomerLinkBody body,
            DeclineCustomerLinkRequest useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(body.Token ?? string.Empty, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await membershipAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.CustomerLinkRequestDeclined,
                    nameof(CustomerLinkRequest),
                    result.Value!.Id.ToString("D"),
                    result.Value.OrganizationId,
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto with { AcceptToken = null }));
        });

        app.MapGet("/api/v1/organizations/{organizationId:guid}/linked-customer-app-users", async (
            Guid organizationId,
            int? page,
            int? pageSize,
            LinkedCustomerAppUserQueryService queries,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.PlatformAccessChecked,
                nameof(LinkedCustomerAppUser),
                organizationId.ToString("D"),
                organizationId,
                summary: "List linked customer app users.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await queries.ListByOrganizationAsync(organizationId, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        app.MapPost("/api/v1/organizations/{organizationId:guid}/linked-customer-app-users/{linkedCustomerId:guid}/revoke", async (
            Guid organizationId,
            Guid linkedCustomerId,
            UnlinkAcceptedCustomerLink useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.LinkedCustomerAppUserRevoked,
                nameof(LinkedCustomerAppUser),
                linkedCustomerId.ToString("D"),
                organizationId,
                summary: "Revoke accepted customer link.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(
                    LinkedCustomerAppUserId.From(linkedCustomerId),
                    PlatformOrganizationId.From(organizationId),
                    ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await membershipAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.LinkedCustomerAppUserRevoked,
                    nameof(LinkedCustomerAppUser),
                    linkedCustomerId.ToString("D"),
                    organizationId,
                    summary: "Revoked accepted customer link (no financial records deleted).",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        app.MapGet("/api/v1/organizations/{organizationId:guid}/staff-invitations", async (
            Guid organizationId,
            string? status,
            int? page,
            int? pageSize,
            OrganizationInvitationQueryService queries,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.PlatformAccessChecked,
                nameof(OrganizationInvitation),
                organizationId.ToString("D"),
                organizationId,
                summary: "List organization staff invitations.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            if (!TryParseInvitationStatus(status, out var parsed, out var error))
            {
                return error!;
            }

            var result = await queries
                .ListByOrganizationAsync(organizationId, parsed, page, pageSize, ct)
                .ConfigureAwait(false);
            var sanitized = result with
            {
                Items = result.Items.Select(i => i with { AcceptToken = null }).ToList()
            };
            return Results.Ok(sanitized);
        });

        app.MapPost("/api/v1/organizations/{organizationId:guid}/staff-invitations", async (
            Guid organizationId,
            CreateInvitationRequest body,
            CreateOrganizationInvitation useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            if (!TryParseRole(body.Role, out var role, out var error))
            {
                return error!;
            }

            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.InvitationCreated,
                nameof(OrganizationInvitation),
                organizationId.ToString("D"),
                organizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var authority = await membershipAuthz
                .ResolveActorMembershipAuthorityAsync(organizationId, ct)
                .ConfigureAwait(false);

            var result = await useCase
                .ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    body.Email ?? string.Empty,
                    role,
                    membershipAuthz.Inner.CurrentActor.PlatformUserId,
                    authority.ActorMembershipRole,
                    authority.HasPlatformManageMemberships,
                    cancellationToken: ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await membershipAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.InvitationCreated,
                    nameof(OrganizationInvitation),
                    result.Value!.Id.ToString("D"),
                    organizationId,
                    summary: "Created organization staff invitation.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(
                result,
                dto => Results.Created(
                    $"/api/v1/organizations/{organizationId}/staff-invitations/{dto.Id}",
                    dto));
        });

        return app;
    }

    private static void MapCustomerRoutes(IEndpointRouteBuilder app, string basePath)
    {
        app.MapGet(basePath, async (
            Guid organizationId,
            string? productCode,
            int? page,
            int? pageSize,
            BusinessCustomerQueryService queries,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.PlatformAccessChecked,
                nameof(BusinessCustomer),
                organizationId.ToString("D"),
                organizationId,
                summary: "List business customers.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await queries
                .ListAsync(organizationId, productCode, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        app.MapPost(basePath, async (
            Guid organizationId,
            string? productCode,
            CreateBusinessCustomerRequest body,
            CreateBusinessCustomer useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.BusinessCustomerCreated,
                nameof(BusinessCustomer),
                organizationId.ToString("D"),
                organizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var request = body with
            {
                OwningProductCode = string.IsNullOrWhiteSpace(body.OwningProductCode)
                    ? productCode
                    : body.OwningProductCode
            };
            var result = await useCase
                .ExecuteAsync(PlatformOrganizationId.From(organizationId), request, ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await membershipAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.BusinessCustomerCreated,
                    nameof(BusinessCustomer),
                    result.Value!.Id.ToString("D"),
                    organizationId,
                    summary: "Created business customer.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(
                result,
                dto => Results.Created(
                    $"/api/v1/organizations/{organizationId}/customers/{dto.Id}",
                    dto));
        });

        app.MapGet(basePath + "/{customerId:guid}", async (
            Guid organizationId,
            Guid customerId,
            BusinessCustomerQueryService queries,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.PlatformAccessChecked,
                nameof(BusinessCustomer),
                customerId.ToString("D"),
                organizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var dto = await queries.GetByIdAsync(customerId, ct).ConfigureAwait(false);
            if (dto is null || dto.OrganizationId != organizationId)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.BusinessCustomerNotFound,
                    "Business customer was not found.",
                    StatusCodes.Status404NotFound);
            }

            return Results.Ok(dto);
        });

        app.MapPut(basePath + "/{customerId:guid}", async (
            Guid organizationId,
            Guid customerId,
            UpdateBusinessCustomerRequest body,
            UpdateBusinessCustomer useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.BusinessCustomerUpdated,
                nameof(BusinessCustomer),
                customerId.ToString("D"),
                organizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(
                    BusinessCustomerId.From(customerId),
                    PlatformOrganizationId.From(organizationId),
                    body,
                    ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await membershipAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.BusinessCustomerUpdated,
                    nameof(BusinessCustomer),
                    customerId.ToString("D"),
                    organizationId,
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        app.MapPost(basePath + "/{customerId:guid}/archive", async (
            Guid organizationId,
            Guid customerId,
            ArchiveBusinessCustomer useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.BusinessCustomerArchived,
                nameof(BusinessCustomer),
                customerId.ToString("D"),
                organizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(
                    BusinessCustomerId.From(customerId),
                    PlatformOrganizationId.From(organizationId),
                    ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await membershipAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.BusinessCustomerArchived,
                    nameof(BusinessCustomer),
                    customerId.ToString("D"),
                    organizationId,
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        app.MapPost(basePath + "/{customerId:guid}/credit", async (
            Guid organizationId,
            Guid customerId,
            EnableCreditBody? body,
            EnableCreditCustomer useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.CreditCustomerEnabled,
                nameof(CreditCustomer),
                customerId.ToString("D"),
                organizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    BusinessCustomerId.From(customerId),
                    body?.CurrencyCode,
                    ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await membershipAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.CreditCustomerEnabled,
                    nameof(CreditCustomer),
                    result.Value!.Id.ToString("D"),
                    organizationId,
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(
                result,
                dto => Results.Created(
                    $"/api/v1/organizations/{organizationId}/credit-customers/{dto.Id}",
                    dto));
        });

        app.MapPost(basePath + "/{customerId:guid}/promote-to-staff", (
            Guid organizationId,
            Guid customerId,
            RejectPromoteBusinessCustomerToStaff useCase,
            PlatformMembershipAuthz membershipAuthz) =>
        {
            _ = organizationId;
            _ = customerId;
            _ = membershipAuthz;
            var result = useCase.Execute();
            return PlatformApiResults.FromResult(result, _ => Results.Ok());
        });
    }

    private static bool TryParseRole(string? role, out OrganizationRole parsed, out IResult? error)
    {
        parsed = default;
        error = null;
        if (string.IsNullOrWhiteSpace(role) || !Enum.TryParse(role, ignoreCase: true, out parsed))
        {
            error = PlatformApiResults.Problem(
                DomainErrorCodes.InvalidOrganizationRole,
                "Role must be OrganizationOwner (Owner) or OrganizationMember (Staff). OrganizationAdministrator is legacy-only.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        return true;
    }

    private static bool TryParseInvitationStatus(string? status, out InvitationStatus? parsed, out IResult? error)
    {
        parsed = null;
        error = null;
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        if (!Enum.TryParse<InvitationStatus>(status, ignoreCase: true, out var value))
        {
            error = PlatformApiResults.Problem(
                DomainErrorCodes.InvalidInvitationStatusTransition,
                $"Unrecognized invitation status '{status}'.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        parsed = value;
        return true;
    }

    private static bool TryParseLinkStatus(string? status, out CustomerLinkRequestStatus? parsed, out IResult? error)
    {
        parsed = null;
        error = null;
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        if (!Enum.TryParse<CustomerLinkRequestStatus>(status, ignoreCase: true, out var value))
        {
            error = PlatformApiResults.Problem(
                DomainErrorCodes.InvalidCustomerLinkRequestStatusTransition,
                $"Unrecognized customer link status '{status}'.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        parsed = value;
        return true;
    }
}

internal sealed record CreateCustomerLinkBody(
    string? Email = null,
    string? PublicUserId = null,
    Guid? TargetUserIdentityId = null);

internal sealed record CreateBusinessCustomerWithPersonalLinkBody(
    string? DisplayName,
    string? Email = null,
    string? Phone = null,
    string? Notes = null,
    string? OwningProductCode = null,
    string? PublicUserId = null,
    Guid? TargetUserIdentityId = null);

internal sealed record AcceptCustomerLinkBody(string? Token);
internal sealed record EnableCreditBody(string? CurrencyCode);
