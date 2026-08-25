using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Api.Payments;

/// <summary>
/// Organization-scoped Business Utang write-off endpoints (online-only).
/// Outstanding = active credits − active repayments − active write-offs.
/// Write-offs are not repayments and must not appear as payment receipts.
/// </summary>
internal static class WriteOffEndpoints
{
    public static IEndpointRouteBuilder MapWriteOffEndpoints(this IEndpointRouteBuilder app)
    {
        var customerGroup = app.MapGroup("/api/v1/pos/customers/{customerId:guid}");

        customerGroup.MapGet("/write-offs", async (
            HttpRequest request,
            Guid customerId,
            int? page,
            int? pageSize,
            POSCustomerQueryService customers,
            WriteOffQueryService writeOffs,
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

            var customer = await customers.GetByIdAsync(organizationId, customerId, ct).ConfigureAwait(false);
            if (customer is null)
            {
                return PosApiResults.Problem(
                    ApplicationErrorCodes.CustomerNotFound,
                    "Customer was not found.",
                    StatusCodes.Status404NotFound);
            }

            var result = await writeOffs
                .ListByCustomerAsync(organizationId, customerId, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        customerGroup.MapPost("/write-offs", async (
            HttpRequest request,
            Guid customerId,
            CreateWriteOffRequest body,
            CreateWriteOff useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.WriteOff, out problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(
                    organizationId,
                    customerId,
                    body.Amount,
                    body.Reason,
                    actorId,
                    body.WriteOffId,
                    ct)
                .ConfigureAwait(false);

            return PosApiResults.FromResult(
                result,
                writeOff => Results.Created(
                    $"/api/v1/pos/write-offs/{writeOff.Id.Value:D}",
                    WriteOffQueryService.Map(writeOff)));
        });

        var writeOffGroup = app.MapGroup("/api/v1/pos/write-offs");

        writeOffGroup.MapGet("/{writeOffId:guid}", async (
            HttpRequest request,
            Guid writeOffId,
            WriteOffQueryService writeOffs,
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

            var dto = await writeOffs.GetByIdAsync(organizationId, writeOffId, ct).ConfigureAwait(false);
            return dto is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.WriteOffNotFound,
                    "Write-off was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(dto);
        });

        writeOffGroup.MapPost("/{writeOffId:guid}/reverse", async (
            HttpRequest request,
            Guid writeOffId,
            ReverseWriteOffRequest body,
            ReverseWriteOff useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ReverseWriteOff, out problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, writeOffId, body.Reason, actorId, ct)
                .ConfigureAwait(false);

            return PosApiResults.FromResult(
                result,
                writeOff => Results.Ok(WriteOffQueryService.Map(writeOff)));
        });

        return app;
    }
}

internal sealed record CreateWriteOffRequest(decimal Amount, string Reason, Guid? WriteOffId = null);

internal sealed record ReverseWriteOffRequest(string Reason);
