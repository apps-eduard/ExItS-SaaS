using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Api.Customers;

/// <summary>
/// Organization-scoped POS customer endpoints. Development-stage only: organization scope is
/// taken from <c>X-Pos-Organization-Id</c>. Cross-organization access returns 404 (fail closed).
/// Profile and lifecycle only - credit endpoints live under CreditEndpoints.
/// </summary>
internal static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/customers");

        group.MapGet("/", async (
            HttpRequest request,
            string? status,
            string? search,
            int? page,
            int? pageSize,
            POSCustomerQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ViewCustomersAndHistory, out problem))
            {
                return problem!;
            }

            CustomerStatus? parsed = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<CustomerStatus>(status, ignoreCase: true, out var value))
                {
                    return PosApiResults.Problem(
                        DomainErrorCodes.InvalidCustomerStatusTransition,
                        $"Unrecognized customer status '{status}'.",
                        StatusCodes.Status400BadRequest);
                }

                parsed = value;
            }

            var result = await queries
                .ListAsync(organizationId, parsed, search, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapPost("/", async (
            HttpRequest request,
            CreateCustomerRequest body,
            CreatePOSCustomer useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.CreateCustomer, out problem))
            {
                return problem!;
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    OfflineOperationTypes.CustomerCreate,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(
                        organizationId,
                        body.DisplayName,
                        body.MobileNumber,
                        body.Address,
                        body.Notes,
                        body.CustomerId,
                        body.PlatformBusinessCustomerId,
                        ct2),
                    POSCustomerQueryService.Map,
                    dto => Results.Created($"/api/v1/pos/customers/{dto.CustomerId:D}", dto),
                    ct)
                .ConfigureAwait(false);
        });

        group.MapGet("/{customerId:guid}", async (
            HttpRequest request,
            Guid customerId,
            POSCustomerQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ViewCustomersAndHistory, out problem))
            {
                return problem!;
            }

            var customer = await queries.GetByIdAsync(organizationId, customerId, ct).ConfigureAwait(false);
            return customer is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.CustomerNotFound,
                    "Customer was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(customer);
        });

        group.MapPut("/{customerId:guid}", async (
            HttpRequest request,
            Guid customerId,
            UpdateCustomerRequest body,
            UpdatePOSCustomer useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.EditCustomer, out problem))
            {
                return problem!;
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    OfflineOperationTypes.CustomerUpdate,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(
                        organizationId,
                        customerId,
                        body.DisplayName,
                        body.MobileNumber,
                        body.Address,
                        body.Notes,
                        body.ExpectedUpdatedAtUtc,
                        ct2),
                    POSCustomerQueryService.Map,
                    Results.Ok,
                    ct)
                .ConfigureAwait(false);
        });

        group.MapPost("/{customerId:guid}/deactivate", async (
            HttpRequest request,
            Guid customerId,
            DeactivatePOSCustomer useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.EditCustomer, out problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, customerId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, c => Results.Ok(POSCustomerQueryService.Map(c)));
        });

        group.MapPost("/{customerId:guid}/reactivate", async (
            HttpRequest request,
            Guid customerId,
            ReactivatePOSCustomer useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.EditCustomer, out problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, customerId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, c => Results.Ok(POSCustomerQueryService.Map(c)));
        });

        group.MapGet("/by-platform-business-customer/{platformBusinessCustomerId:guid}", async (
            HttpRequest request,
            Guid platformBusinessCustomerId,
            POSCustomerQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ViewCustomersAndHistory, out problem))
            {
                return problem!;
            }

            var customer = await queries
                .GetByPlatformBusinessCustomerIdAsync(organizationId, platformBusinessCustomerId, ct)
                .ConfigureAwait(false);
            return customer is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.CustomerNotFound,
                    "Customer was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(customer);
        });

        group.MapPut("/{customerId:guid}/platform-correlation", async (
            HttpRequest request,
            Guid customerId,
            CorrelatePlatformBusinessCustomerRequest body,
            CorrelatePOSCustomerToPlatformBusinessCustomer useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.EditCustomer, out problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, customerId, body.PlatformBusinessCustomerId, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, c => Results.Ok(POSCustomerQueryService.Map(c)));
        });

        group.MapDelete("/{customerId:guid}/platform-correlation", async (
            HttpRequest request,
            Guid customerId,
            ClearPOSCustomerPlatformCorrelation useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.EditCustomer, out problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, customerId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, c => Results.Ok(POSCustomerQueryService.Map(c)));
        });

        group.MapPut("/{customerId:guid}/exits-identity/personal", async (
            HttpRequest request,
            Guid customerId,
            LinkPersonalExItsIdentityRequest body,
            LinkPOSCustomerPersonalExItsIdentity useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.EditCustomer, out problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, customerId, body.PersonalPublicUserId, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, c => Results.Ok(POSCustomerQueryService.Map(c)));
        });

        group.MapPut("/{customerId:guid}/exits-identity/organization", async (
            HttpRequest request,
            Guid customerId,
            LinkOrganizationExItsIdentityRequest body,
            LinkPOSCustomerOrganizationExItsIdentity useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.EditCustomer, out problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(
                    organizationId,
                    customerId,
                    body.BuyerOrganizationId,
                    body.BuyerPublicOrganizationId,
                    ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, c => Results.Ok(POSCustomerQueryService.Map(c)));
        });

        group.MapDelete("/{customerId:guid}/exits-identity", async (
            HttpRequest request,
            Guid customerId,
            ClearPOSCustomerExItsIdentityLink useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.EditCustomer, out problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, customerId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, c => Results.Ok(POSCustomerQueryService.Map(c)));
        });

        return app;
    }
}

public sealed record CreateCustomerRequest(
    string DisplayName,
    string? MobileNumber,
    string? Address,
    string? Notes,
    Guid? CustomerId = null,
    Guid? PlatformBusinessCustomerId = null);

public sealed record UpdateCustomerRequest(
    string DisplayName,
    string? MobileNumber,
    string? Address,
    string? Notes,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record CorrelatePlatformBusinessCustomerRequest(Guid PlatformBusinessCustomerId);

public sealed record LinkPersonalExItsIdentityRequest(string PersonalPublicUserId);

public sealed record LinkOrganizationExItsIdentityRequest(
    Guid BuyerOrganizationId,
    string BuyerPublicOrganizationId);
