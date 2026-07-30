using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Suppliers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Api.Suppliers;

/// <summary>
/// Organization-scoped supplier master-data endpoints (P10-WP01 Option A).
/// Reference data only — no purchasing, receiving, payables, cost, or stock surface.
/// </summary>
internal static class SupplierEndpoints
{
    public static IEndpointRouteBuilder MapSupplierEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/suppliers");

        group.MapGet("/", async (
            HttpRequest request,
            string? supplierCode,
            string? name,
            string? contactPerson,
            string? email,
            string? mobile,
            string? taxOrRegistrationNumber,
            string? status,
            int? page,
            int? pageSize,
            SupplierQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewSuppliers, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!TryParseStatus(status, out var parsedStatus, out problem))
            {
                return problem!;
            }

            var filter = new SupplierFilter(
                supplierCode,
                name,
                contactPerson,
                email,
                mobile,
                taxOrRegistrationNumber,
                parsedStatus);

            var result = await queries
                .ListAsync(organizationId, filter, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapPost("/", async (
            HttpRequest request,
            CreateSupplierRequest body,
            CreateSupplier useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageSuppliers, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, body, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(
                result,
                dto => Results.Created($"/api/v1/pos/suppliers/{dto.SupplierId:D}", dto));
        });

        group.MapGet("/{supplierId:guid}", async (
            HttpRequest request,
            Guid supplierId,
            SupplierQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewSuppliers, out var organizationId, out var problem))
            {
                return problem!;
            }

            var supplier = await queries.GetByIdAsync(organizationId, supplierId, ct).ConfigureAwait(false);
            return supplier is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.SupplierNotFound,
                    "Supplier was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(supplier);
        });

        group.MapPut("/{supplierId:guid}", async (
            HttpRequest request,
            Guid supplierId,
            UpdateSupplierRequest body,
            UpdateSupplier useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageSuppliers, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, supplierId, body, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        group.MapPost("/{supplierId:guid}/activate", async (
            HttpRequest request,
            Guid supplierId,
            ActivateSupplier useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageSuppliers, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, supplierId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        group.MapPost("/{supplierId:guid}/deactivate", async (
            HttpRequest request,
            Guid supplierId,
            DeactivateSupplier useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageSuppliers, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, supplierId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        return app;
    }

    private static bool TryAuthorize(
        HttpRequest request,
        IPosCommercialAccessAccessor access,
        UtangCapability capability,
        out Guid organizationId,
        out IResult? problem)
    {
        if (!PosOrganizationScope.TryGetOrganizationId(request, out organizationId, out problem))
        {
            return false;
        }

        return PosCommercialScope.TryAuthorize(access, capability, out problem);
    }

    private static bool TryParseStatus(
        string? status,
        out SupplierStatus? parsed,
        out IResult? problem)
    {
        parsed = null;
        problem = null;
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        if (!Enum.TryParse<SupplierStatus>(status, ignoreCase: true, out var value))
        {
            problem = PosApiResults.Problem(
                DomainErrorCodes.InvalidSupplierStatus,
                $"Unrecognized supplier status '{status}'.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        parsed = value;
        return true;
    }
}
