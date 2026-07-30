using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
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
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
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
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, body.DisplayName, body.MobileNumber, body.Address, body.Notes, ct)
                .ConfigureAwait(false);

            return PosApiResults.FromResult(result, c => Results.Created(
                $"/api/v1/pos/customers/{c.Id.Value:D}",
                POSCustomerQueryService.Map(c)));
        });

        group.MapGet("/{customerId:guid}", async (
            HttpRequest request,
            Guid customerId,
            POSCustomerQueryService queries,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
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
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, customerId, body.DisplayName, body.MobileNumber, body.Address, body.Notes, ct)
                .ConfigureAwait(false);

            return PosApiResults.FromResult(result, c => Results.Ok(POSCustomerQueryService.Map(c)));
        });

        group.MapPost("/{customerId:guid}/deactivate", async (
            HttpRequest request,
            Guid customerId,
            DeactivatePOSCustomer useCase,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
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
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
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
    string? Notes);

public sealed record UpdateCustomerRequest(
    string DisplayName,
    string? MobileNumber,
    string? Address,
    string? Notes);
