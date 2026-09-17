using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Quotations;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Quotations;

namespace ExItS.PinoyBusinessPOS.Api.Quotations;

/// <summary>
/// Seller quotation endpoints (MVP). Mutations require CreateSale; reads require ViewSales.
/// </summary>
internal static class QuotationEndpoints
{
    public static IEndpointRouteBuilder MapQuotationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/quotations");

        group.MapGet("/", async (
            HttpRequest request,
            string? status,
            Guid? customerId,
            Guid? branchId,
            string? quotationNumber,
            string? fromIssuedDate,
            string? toIssuedDate,
            int? page,
            int? pageSize,
            QuotationQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewSales, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!TryParseStatus(status, out var parsedStatus, out problem)
                || !TryParseDate(fromIssuedDate, "fromIssuedDate", out var parsedFrom, out problem)
                || !TryParseDate(toIssuedDate, "toIssuedDate", out var parsedTo, out problem))
            {
                return problem!;
            }

            var filter = new QuotationFilter(parsedStatus, customerId, branchId, quotationNumber, parsedFrom, parsedTo);
            var result = await queries.ListAsync(organizationId, filter, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapPost("/", async (
            HttpRequest request,
            CreateQuotationRequest body,
            CreateQuotationDraft useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.CreateSale, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, body, actorId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(
                result,
                dto => Results.Created($"/api/v1/pos/quotations/{dto.QuotationId:D}", dto));
        });

        group.MapGet("/{quotationId:guid}", async (
            HttpRequest request,
            Guid quotationId,
            QuotationQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewSales, out var organizationId, out var problem))
            {
                return problem!;
            }

            var quotation = await queries.GetByIdAsync(organizationId, quotationId, ct).ConfigureAwait(false);
            return quotation is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.QuotationNotFound,
                    "Quotation was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(quotation);
        });

        group.MapPut("/{quotationId:guid}", async (
            HttpRequest request,
            Guid quotationId,
            UpdateQuotationRequest body,
            UpdateQuotationDraft useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.CreateSale, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, quotationId, body, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        group.MapPost("/{quotationId:guid}/issue", async (
            HttpRequest request,
            Guid quotationId,
            IssueQuotation useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.CreateSale, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, quotationId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        group.MapPost("/{quotationId:guid}/cancel", async (
            HttpRequest request,
            Guid quotationId,
            CancelQuotation useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.CreateSale, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, quotationId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        group.MapPost("/{quotationId:guid}/accept", async (
            HttpRequest request,
            Guid quotationId,
            AcceptQuotation useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.CreateSale, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, quotationId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        group.MapPost("/{quotationId:guid}/decline", async (
            HttpRequest request,
            Guid quotationId,
            DeclineQuotation useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.CreateSale, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, quotationId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        group.MapPost("/{quotationId:guid}/expire", async (
            HttpRequest request,
            Guid quotationId,
            ExpireQuotation useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.CreateSale, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, quotationId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        group.MapPost("/{quotationId:guid}/mark-converted", async (
            HttpRequest request,
            Guid quotationId,
            MarkConvertedRequest body,
            MarkQuotationConverted useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.CreateSale, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, quotationId, body.SaleId, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        return app;
    }

    private sealed record MarkConvertedRequest(Guid SaleId);

    private static bool TryAuthorize(
        HttpRequest request,
        IPosCommercialAccessAccessor access,
        UtangCapability capability,
        out Guid organizationId,
        out IResult? problem)
    {
        organizationId = default;
        problem = null;
        if (!PosOrganizationScope.TryGetOrganizationId(request, out organizationId, out problem))
        {
            return false;
        }

        return PosCommercialScope.TryAuthorize(access, capability, out problem);
    }

    private static bool TryParseStatus(
        string? status,
        out QuotationStatus? parsed,
        out IResult? problem)
    {
        parsed = null;
        problem = null;
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        if (!Enum.TryParse<QuotationStatus>(status, ignoreCase: true, out var value))
        {
            problem = PosApiResults.Problem(
                DomainErrorCodes.InvalidQuotationStatus,
                $"Unrecognized quotation status '{status}'.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        parsed = value;
        return true;
    }

    private static bool TryParseDate(
        string? value,
        string paramName,
        out DateOnly? parsed,
        out IResult? problem)
    {
        parsed = null;
        problem = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!DateOnly.TryParse(value, out var date))
        {
            problem = PosApiResults.Problem(
                ApplicationErrorCodes.DomainViolation,
                $"Invalid {paramName} '{value}'. Use ISO date (yyyy-MM-dd).",
                StatusCodes.Status400BadRequest);
            return false;
        }

        parsed = date;
        return true;
    }
}
