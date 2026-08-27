using ExItS.PinoyBuyNowPayLater.Api.Access;
using ExItS.PinoyBuyNowPayLater.Application.Access;
using ExItS.PinoyBuyNowPayLater.Application.Common;
using ExItS.PinoyBuyNowPayLater.Application.Customers;
using ExItS.PinoyBuyNowPayLater.Domain.Access;
using ExItS.PinoyBuyNowPayLater.Domain.Customers;
using Microsoft.AspNetCore.Mvc;

namespace ExItS.PinoyBuyNowPayLater.Api.Customers;

internal static class BnplCustomerEndpoints
{
    public const string BranchHeaderName = "X-Bnpl-Branch-Id";

    public static void MapBnplCustomers(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/bnpl/customers")
            .WithTags("BnplCustomers");

        group.MapPost("/", CreateAsync)
            .WithName("CreateBnplCustomer");

        group.MapGet("/{customerId:guid}", GetAsync)
            .WithName("GetBnplCustomer");

        group.MapGet("/", SearchAsync)
            .WithName("SearchBnplCustomers");

        group.MapPatch("/{customerId:guid}", UpdateAsync)
            .WithName("UpdateBnplCustomer");

        group.MapPut("/{customerId:guid}/personal-link", LinkPersonalAsync)
            .WithName("LinkBnplCustomerPersonal");

        group.MapPut("/{customerId:guid}/commerce-link", LinkCommerceAsync)
            .WithName("LinkBnplCustomerCommerce");
    }

    private static async Task<IResult> CreateAsync(
        HttpContext httpContext,
        [FromServices] CreateBnplCustomer createCustomer,
        [FromBody] CreateBnplCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var access = await AuthorizeAsync(httpContext, BnplCapabilityCodes.CustomerManage, cancellationToken)
            .ConfigureAwait(false);
        if (access.Denial is not null)
        {
            return access.Denial;
        }

        var result = await createCustomer.ExecuteAsync(
                access.Context!.OrganizationId,
                request.DisplayName,
                request.CustomerId,
                request.Mobile,
                request.Email,
                request.LinkedPersonalPublicUserId,
                request.LinkedCommerceCustomerId,
                cancellationToken)
            .ConfigureAwait(false);

        return MapCustomerResult(result, created: true);
    }

    private static async Task<IResult> GetAsync(
        HttpContext httpContext,
        [FromServices] GetBnplCustomer getCustomer,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var access = await AuthorizeAsync(httpContext, BnplCapabilityCodes.CustomerRead, cancellationToken)
            .ConfigureAwait(false);
        if (access.Denial is not null)
        {
            return access.Denial;
        }

        var result = await getCustomer.ExecuteAsync(access.Context!.OrganizationId, customerId, cancellationToken)
            .ConfigureAwait(false);
        return MapCustomerResult(result);
    }

    private static async Task<IResult> SearchAsync(
        HttpContext httpContext,
        [FromServices] SearchBnplCustomers searchCustomers,
        string? search,
        string? status,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var access = await AuthorizeAsync(httpContext, BnplCapabilityCodes.CustomerRead, cancellationToken)
            .ConfigureAwait(false);
        if (access.Denial is not null)
        {
            return access.Denial;
        }

        BnplCustomerStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<BnplCustomerStatus>(status, ignoreCase: true, out var value))
            {
                return Results.Problem(
                    detail: "Status must be Active or Inactive.",
                    statusCode: StatusCodes.Status400BadRequest,
                    extensions: new Dictionary<string, object?>
                    {
                        ["errorCode"] = BnplCustomerErrorCodes.InvalidDisplayName
                    });
            }

            parsedStatus = value;
        }

        var result = await searchCustomers.ExecuteAsync(
                access.Context!.OrganizationId,
                search,
                parsedStatus,
                page ?? 1,
                pageSize ?? 20,
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess || result.Value is null)
        {
            return MapFailure(result.ErrorCode, result.ErrorMessage, result.SuggestedHttpStatus);
        }

        return Results.Ok(new BnplCustomerSearchResponse(
            result.Value.Items.Select(ToDto).ToArray(),
            result.Value.TotalCount,
            result.Value.Page,
            result.Value.PageSize));
    }

    private static async Task<IResult> UpdateAsync(
        HttpContext httpContext,
        [FromServices] UpdateBnplCustomerProfile updateCustomer,
        Guid customerId,
        [FromBody] UpdateBnplCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var access = await AuthorizeAsync(httpContext, BnplCapabilityCodes.CustomerManage, cancellationToken)
            .ConfigureAwait(false);
        if (access.Denial is not null)
        {
            return access.Denial;
        }

        var result = await updateCustomer.ExecuteAsync(
                access.Context!.OrganizationId,
                customerId,
                request.DisplayName,
                request.Mobile,
                request.Email,
                cancellationToken)
            .ConfigureAwait(false);
        return MapCustomerResult(result);
    }

    private static async Task<IResult> LinkPersonalAsync(
        HttpContext httpContext,
        [FromServices] LinkBnplCustomerPersonalIdentity linkPersonal,
        Guid customerId,
        [FromBody] LinkPersonalRequest request,
        CancellationToken cancellationToken)
    {
        var access = await AuthorizeAsync(httpContext, BnplCapabilityCodes.CustomerManage, cancellationToken)
            .ConfigureAwait(false);
        if (access.Denial is not null)
        {
            return access.Denial;
        }

        var result = await linkPersonal.ExecuteAsync(
                access.Context!.OrganizationId,
                customerId,
                request.PersonalPublicUserId,
                cancellationToken)
            .ConfigureAwait(false);
        return MapCustomerResult(result);
    }

    private static async Task<IResult> LinkCommerceAsync(
        HttpContext httpContext,
        [FromServices] LinkBnplCustomerCommerceReference linkCommerce,
        Guid customerId,
        [FromBody] LinkCommerceRequest request,
        CancellationToken cancellationToken)
    {
        var access = await AuthorizeAsync(httpContext, BnplCapabilityCodes.CustomerManage, cancellationToken)
            .ConfigureAwait(false);
        if (access.Denial is not null)
        {
            return access.Denial;
        }

        var result = await linkCommerce.ExecuteAsync(
                access.Context!.OrganizationId,
                customerId,
                request.CommerceCustomerId,
                cancellationToken)
            .ConfigureAwait(false);
        return MapCustomerResult(result);
    }

    private static async Task<(BnplAccessContext? Context, IResult? Denial)> AuthorizeAsync(
        HttpContext httpContext,
        string capability,
        CancellationToken cancellationToken)
    {
        if (!TryResolveBranchId(httpContext, out var branchId, out var branchError))
        {
            return (null, branchError);
        }

        var guard = httpContext.RequestServices.GetRequiredService<IBnplOperationalAccessGuard>();
        var decision = await guard
            .EvaluateAsync(BnplAccessRequirement.ForBranchAndCapability(branchId, capability), cancellationToken)
            .ConfigureAwait(false);
        if (!decision.IsAllowed || decision.Context is null)
        {
            return (null, BnplApiResults.FromDenial(decision));
        }

        httpContext.Items[BnplAccessHttpContextKeys.Context] = decision.Context;
        return (decision.Context, null);
    }

    private static bool TryResolveBranchId(HttpContext httpContext, out Guid branchId, out IResult? error)
    {
        branchId = Guid.Empty;
        error = null;
        var raw = httpContext.Request.Headers[BranchHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(raw) || !Guid.TryParse(raw, out branchId) || branchId == Guid.Empty)
        {
            error = Results.Problem(
                detail: "X-Bnpl-Branch-Id header with a non-empty Guid is required for BNPL customer operations.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: new Dictionary<string, object?>
                {
                    ["errorCode"] = BnplAccessErrorCodes.BranchRequired
                });
            return false;
        }

        return true;
    }

    private static IResult MapCustomerResult(BnplApplicationResult<BnplCustomer> result, bool created = false)
    {
        if (!result.IsSuccess || result.Value is null)
        {
            return MapFailure(result.ErrorCode, result.ErrorMessage, result.SuggestedHttpStatus);
        }

        var dto = ToDto(result.Value);
        return created
            ? Results.Created($"/api/v1/bnpl/customers/{dto.CustomerId:D}", dto)
            : Results.Ok(dto);
    }

    private static IResult MapFailure(string? errorCode, string? message, int? status) =>
        Results.Problem(
            detail: message ?? "Request failed.",
            statusCode: status ?? StatusCodes.Status400BadRequest,
            extensions: new Dictionary<string, object?>
            {
                ["errorCode"] = errorCode ?? "bnpl.customer.error"
            });

    private static BnplCustomerDto ToDto(BnplCustomer customer) =>
        new(
            customer.Id.Value,
            customer.OrganizationId,
            customer.DisplayName,
            customer.Mobile,
            customer.Email,
            customer.Status.ToString(),
            customer.LinkedPersonalPublicUserId,
            customer.LinkedCommerceCustomerId,
            customer.CreatedAtUtc,
            customer.UpdatedAtUtc);
}

internal sealed record CreateBnplCustomerRequest(
    string DisplayName,
    Guid? CustomerId = null,
    string? Mobile = null,
    string? Email = null,
    string? LinkedPersonalPublicUserId = null,
    Guid? LinkedCommerceCustomerId = null);

internal sealed record UpdateBnplCustomerRequest(
    string DisplayName,
    string? Mobile = null,
    string? Email = null);

internal sealed record LinkPersonalRequest(string PersonalPublicUserId);

internal sealed record LinkCommerceRequest(Guid CommerceCustomerId);

internal sealed record BnplCustomerDto(
    Guid CustomerId,
    Guid OrganizationId,
    string DisplayName,
    string? Mobile,
    string? Email,
    string Status,
    string? LinkedPersonalPublicUserId,
    Guid? LinkedCommerceCustomerId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

internal sealed record BnplCustomerSearchResponse(
    IReadOnlyList<BnplCustomerDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
