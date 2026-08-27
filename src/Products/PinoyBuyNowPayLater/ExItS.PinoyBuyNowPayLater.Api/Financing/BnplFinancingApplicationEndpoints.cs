using ExItS.PinoyBuyNowPayLater.Application.Common;
using ExItS.PinoyBuyNowPayLater.Application.Financing;
using ExItS.PinoyBuyNowPayLater.Domain.Access;
using ExItS.PinoyBuyNowPayLater.Domain.Financing;
using Microsoft.AspNetCore.Mvc;

namespace ExItS.PinoyBuyNowPayLater.Api.Financing;

internal static class BnplFinancingApplicationEndpoints
{
    public static void MapBnplFinancingApplications(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/bnpl/applications")
            .WithTags("BnplFinancingApplications");

        group.MapPost("/", CreateAsync).WithName("CreateBnplFinancingApplication");
        group.MapGet("/", SearchAsync).WithName("SearchBnplFinancingApplications");
        group.MapGet("/{applicationId:guid}", GetAsync).WithName("GetBnplFinancingApplication");
        group.MapPatch("/{applicationId:guid}", UpdateDraftAsync).WithName("UpdateBnplFinancingApplicationDraft");
        group.MapPost("/{applicationId:guid}/submit", SubmitAsync).WithName("SubmitBnplFinancingApplication");
        group.MapPost("/{applicationId:guid}/eligibility/approve", ApproveEligibilityAsync).WithName("ApproveBnplEligibility");
        group.MapPost("/{applicationId:guid}/eligibility/decline", DeclineEligibilityAsync).WithName("DeclineBnplEligibility");
        group.MapPost("/{applicationId:guid}/offers", CreateOfferAsync).WithName("CreateBnplFinancingOffer");
        group.MapPut("/{applicationId:guid}/offers/{offerId:guid}/installment-plan", PutInstallmentPlanAsync)
            .WithName("PutBnplInstallmentPlan");
        group.MapGet("/{applicationId:guid}/offers/{offerId:guid}/installment-plan", GetInstallmentPlanAsync)
            .WithName("GetBnplInstallmentPlan");
        group.MapPost("/{applicationId:guid}/accept-offer", AcceptOfferAsync).WithName("AcceptBnplFinancingOffer");
        group.MapPost("/{applicationId:guid}/approve", ApproveAsync).WithName("ApproveBnplFinancingApplication");
        group.MapPost("/{applicationId:guid}/decline", DeclineAsync).WithName("DeclineBnplFinancingApplication");
        group.MapPost("/{applicationId:guid}/cancel", CancelAsync).WithName("CancelBnplFinancingApplication");
    }

    private static async Task<IResult> CreateAsync(
        HttpContext httpContext,
        [FromServices] CreateBnplFinancingApplication create,
        [FromBody] CreateFinancingApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var access = await BnplStaffEndpointAuth.AuthorizeAsync(
            httpContext, BnplCapabilityCodes.ApplicationCreate, cancellationToken).ConfigureAwait(false);
        if (access.Denial is not null)
        {
            return access.Denial;
        }

        var ctx = access.Context!;
        var result = await create.ExecuteAsync(
                ctx.OrganizationId,
                ParseBranch(httpContext),
                request.CustomerId,
                ctx.ActorId,
                request.PurchaseAmount,
                request.DownPaymentAmount,
                request.ApplicationId,
                request.PurchaseDescription,
                request.MerchantProductReference,
                cancellationToken)
            .ConfigureAwait(false);
        return Map(result, created: true);
    }

    private static async Task<IResult> SearchAsync(
        HttpContext httpContext,
        [FromServices] SearchBnplFinancingApplications search,
        Guid? customerId,
        string? status,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var access = await BnplStaffEndpointAuth.AuthorizeAsync(
            httpContext, BnplCapabilityCodes.ApplicationRead, cancellationToken).ConfigureAwait(false);
        if (access.Denial is not null)
        {
            return access.Denial;
        }

        BnplFinancingApplicationStatus? parsed = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<BnplFinancingApplicationStatus>(status, ignoreCase: true, out var value))
            {
                return Results.Problem(
                    detail: "Invalid financing application status.",
                    statusCode: 400,
                    extensions: new Dictionary<string, object?> { ["errorCode"] = BnplFinancingErrorCodes.InvalidState });
            }

            parsed = value;
        }

        var result = await search.ExecuteAsync(
                access.Context!.OrganizationId,
                ParseBranch(httpContext),
                customerId,
                parsed,
                page ?? 1,
                pageSize ?? 20,
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess || result.Value is null)
        {
            return MapFailure(result.ErrorCode, result.ErrorMessage, result.SuggestedHttpStatus);
        }

        return Results.Ok(new FinancingApplicationSearchResponse(
            result.Value.Items.Select(ToDto).ToArray(),
            result.Value.TotalCount,
            result.Value.Page,
            result.Value.PageSize));
    }

    private static async Task<IResult> GetAsync(
        HttpContext httpContext,
        [FromServices] GetBnplFinancingApplication get,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var access = await BnplStaffEndpointAuth.AuthorizeAsync(
            httpContext, BnplCapabilityCodes.ApplicationRead, cancellationToken).ConfigureAwait(false);
        if (access.Denial is not null)
        {
            return access.Denial;
        }

        var result = await get.ExecuteAsync(access.Context!.OrganizationId, applicationId, cancellationToken)
            .ConfigureAwait(false);
        return Map(result);
    }

    private static async Task<IResult> UpdateDraftAsync(
        HttpContext httpContext,
        [FromServices] UpdateBnplFinancingApplicationDraft update,
        Guid applicationId,
        [FromBody] UpdateFinancingApplicationDraftRequest request,
        CancellationToken cancellationToken)
    {
        var access = await BnplStaffEndpointAuth.AuthorizeAsync(
            httpContext, BnplCapabilityCodes.ApplicationCreate, cancellationToken).ConfigureAwait(false);
        if (access.Denial is not null)
        {
            return access.Denial;
        }

        var result = await update.ExecuteAsync(
                access.Context!.OrganizationId,
                applicationId,
                request.PurchaseAmount,
                request.DownPaymentAmount,
                request.PurchaseDescription,
                request.MerchantProductReference,
                request.ExpectedVersion,
                cancellationToken)
            .ConfigureAwait(false);
        return Map(result);
    }

    private static async Task<IResult> SubmitAsync(
        HttpContext httpContext,
        [FromServices] SubmitBnplFinancingApplication submit,
        Guid applicationId,
        [FromBody] VersionedActionRequest? request,
        CancellationToken cancellationToken)
    {
        var access = await BnplStaffEndpointAuth.AuthorizeAsync(
            httpContext, BnplCapabilityCodes.ApplicationCreate, cancellationToken).ConfigureAwait(false);
        if (access.Denial is not null)
        {
            return access.Denial;
        }

        var result = await submit.ExecuteAsync(
                access.Context!.OrganizationId,
                applicationId,
                request?.ExpectedVersion,
                cancellationToken)
            .ConfigureAwait(false);
        return Map(result);
    }

    private static async Task<IResult> ApproveEligibilityAsync(
        HttpContext httpContext,
        [FromServices] ApproveBnplFinancingEligibility approve,
        Guid applicationId,
        [FromBody] DecisionRequest? request,
        CancellationToken cancellationToken)
    {
        var access = await BnplStaffEndpointAuth.AuthorizeAsync(
            httpContext, BnplCapabilityCodes.ApplicationApprove, cancellationToken).ConfigureAwait(false);
        if (access.Denial is not null)
        {
            return access.Denial;
        }

        var result = await approve.ExecuteAsync(
                access.Context!.OrganizationId,
                applicationId,
                access.Context.ActorId,
                request?.Note,
                request?.ExpectedVersion,
                cancellationToken)
            .ConfigureAwait(false);
        return Map(result);
    }

    private static async Task<IResult> DeclineEligibilityAsync(
        HttpContext httpContext,
        [FromServices] DeclineBnplFinancingEligibility decline,
        Guid applicationId,
        [FromBody] DecisionRequest? request,
        CancellationToken cancellationToken)
    {
        var access = await BnplStaffEndpointAuth.AuthorizeAsync(
            httpContext, BnplCapabilityCodes.ApplicationApprove, cancellationToken).ConfigureAwait(false);
        if (access.Denial is not null)
        {
            return access.Denial;
        }

        var result = await decline.ExecuteAsync(
                access.Context!.OrganizationId,
                applicationId,
                access.Context.ActorId,
                request?.Note,
                request?.ExpectedVersion,
                cancellationToken)
            .ConfigureAwait(false);
        return Map(result);
    }

    private static async Task<IResult> CreateOfferAsync(
        HttpContext httpContext,
        [FromServices] CreateBnplFinancingOffer createOffer,
        Guid applicationId,
        [FromBody] CreateOfferRequest? request,
        CancellationToken cancellationToken)
    {
        var access = await BnplStaffEndpointAuth.AuthorizeAsync(
            httpContext, BnplCapabilityCodes.ApplicationCreate, cancellationToken).ConfigureAwait(false);
        if (access.Denial is not null)
        {
            return access.Denial;
        }

        var result = await createOffer.ExecuteAsync(
                access.Context!.OrganizationId,
                applicationId,
                access.Context.ActorId,
                request?.OfferId,
                request?.ExpiresAtUtc,
                request?.ExpectedVersion,
                cancellationToken)
            .ConfigureAwait(false);
        return Map(result);
    }

    private static async Task<IResult> PutInstallmentPlanAsync(
        HttpContext httpContext,
        [FromServices] AttachBnplInstallmentPlan attach,
        Guid applicationId,
        Guid offerId,
        [FromBody] PutInstallmentPlanRequest request,
        CancellationToken cancellationToken)
    {
        var access = await BnplStaffEndpointAuth.AuthorizeAsync(
            httpContext, BnplCapabilityCodes.PlanManage, cancellationToken).ConfigureAwait(false);
        if (access.Denial is not null)
        {
            return access.Denial;
        }

        var items = (request.Items ?? Array.Empty<InstallmentPlanItemRequest>())
            .Select(i => new BnplInstallmentPlanItemDraft(
                i.ItemId,
                i.SequenceNumber,
                i.PrincipalAmount,
                i.DueDate))
            .ToList();

        var result = await attach.ExecuteAsync(
                access.Context!.OrganizationId,
                applicationId,
                offerId,
                request.PlanId,
                items,
                access.Context.ActorId,
                request.ExpectedVersion,
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess || result.Value is null)
        {
            return MapFailure(result.ErrorCode, result.ErrorMessage, result.SuggestedHttpStatus);
        }

        var plan = result.Value.GetInstallmentPlanForOffer(offerId);
        return Results.Ok(ToPlanDto(result.Value, plan!));
    }

    private static async Task<IResult> GetInstallmentPlanAsync(
        HttpContext httpContext,
        [FromServices] GetBnplInstallmentPlan get,
        Guid applicationId,
        Guid offerId,
        CancellationToken cancellationToken)
    {
        var access = await BnplStaffEndpointAuth.AuthorizeAsync(
            httpContext, BnplCapabilityCodes.PlanRead, cancellationToken).ConfigureAwait(false);
        if (access.Denial is not null)
        {
            return access.Denial;
        }

        var result = await get.ExecuteAsync(
                access.Context!.OrganizationId,
                applicationId,
                offerId,
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode, result.ErrorMessage, result.SuggestedHttpStatus);
        }

        var (application, plan) = result.Value!;
        return Results.Ok(ToPlanDto(application, plan));
    }

    private static async Task<IResult> AcceptOfferAsync(
        HttpContext httpContext,
        [FromServices] AcceptBnplFinancingOffer accept,
        Guid applicationId,
        [FromBody] AcceptOfferRequest request,
        CancellationToken cancellationToken)
    {
        var access = await BnplStaffEndpointAuth.AuthorizeAsync(
            httpContext, BnplCapabilityCodes.ApplicationCreate, cancellationToken).ConfigureAwait(false);
        if (access.Denial is not null)
        {
            return access.Denial;
        }

        var result = await accept.ExecuteAsync(
                access.Context!.OrganizationId,
                applicationId,
                request.OfferId,
                access.Context.ActorId,
                request.ExpectedVersion,
                cancellationToken)
            .ConfigureAwait(false);
        return Map(result);
    }

    private static async Task<IResult> ApproveAsync(
        HttpContext httpContext,
        [FromServices] ApproveBnplFinancingApplication approve,
        Guid applicationId,
        [FromBody] DecisionRequest? request,
        CancellationToken cancellationToken)
    {
        var access = await BnplStaffEndpointAuth.AuthorizeAsync(
            httpContext, BnplCapabilityCodes.ApplicationApprove, cancellationToken).ConfigureAwait(false);
        if (access.Denial is not null)
        {
            return access.Denial;
        }

        var result = await approve.ExecuteAsync(
                access.Context!.OrganizationId,
                applicationId,
                access.Context.ActorId,
                request?.Note,
                request?.ExpectedVersion,
                cancellationToken)
            .ConfigureAwait(false);
        return Map(result);
    }

    private static async Task<IResult> DeclineAsync(
        HttpContext httpContext,
        [FromServices] DeclineBnplFinancingApplication decline,
        Guid applicationId,
        [FromBody] DecisionRequest? request,
        CancellationToken cancellationToken)
    {
        var access = await BnplStaffEndpointAuth.AuthorizeAsync(
            httpContext, BnplCapabilityCodes.ApplicationApprove, cancellationToken).ConfigureAwait(false);
        if (access.Denial is not null)
        {
            return access.Denial;
        }

        var result = await decline.ExecuteAsync(
                access.Context!.OrganizationId,
                applicationId,
                access.Context.ActorId,
                request?.Note,
                request?.ExpectedVersion,
                cancellationToken)
            .ConfigureAwait(false);
        return Map(result);
    }

    private static async Task<IResult> CancelAsync(
        HttpContext httpContext,
        [FromServices] CancelBnplFinancingApplication cancel,
        Guid applicationId,
        [FromBody] DecisionRequest? request,
        CancellationToken cancellationToken)
    {
        var access = await BnplStaffEndpointAuth.AuthorizeAsync(
            httpContext, BnplCapabilityCodes.ApplicationCreate, cancellationToken).ConfigureAwait(false);
        if (access.Denial is not null)
        {
            return access.Denial;
        }

        var result = await cancel.ExecuteAsync(
                access.Context!.OrganizationId,
                applicationId,
                access.Context.ActorId,
                request?.Note,
                request?.ExpectedVersion,
                cancellationToken)
            .ConfigureAwait(false);
        return Map(result);
    }

    private static Guid ParseBranch(HttpContext httpContext)
    {
        BnplStaffEndpointAuth.TryResolveBranchId(httpContext, out var branchId, out _);
        return branchId;
    }

    private static IResult Map(BnplApplicationResult<BnplFinancingApplication> result, bool created = false)
    {
        if (!result.IsSuccess || result.Value is null)
        {
            return MapFailure(result.ErrorCode, result.ErrorMessage, result.SuggestedHttpStatus);
        }

        var dto = ToDto(result.Value);
        return created
            ? Results.Created($"/api/v1/bnpl/applications/{dto.ApplicationId:D}", dto)
            : Results.Ok(dto);
    }

    private static IResult MapFailure(string? errorCode, string? message, int? status) =>
        Results.Problem(
            detail: message ?? "Request failed.",
            statusCode: status ?? StatusCodes.Status400BadRequest,
            extensions: new Dictionary<string, object?>
            {
                ["errorCode"] = errorCode ?? "bnpl.financing.error"
            });

    private static FinancingApplicationDto ToDto(BnplFinancingApplication a) =>
        new(
            a.Id.Value,
            a.OrganizationId,
            a.BranchId,
            a.CustomerId,
            a.Status.ToString(),
            a.PurchaseAmount,
            a.DownPaymentAmount,
            a.RequestedFinanceAmount,
            a.PurchaseDescription,
            a.MerchantProductReference,
            a.AggregateVersion,
            a.EligibilityApproved,
            a.CurrentOfferId,
            a.AcceptedOfferId,
            a.AcceptedInstallmentPlan?.Id.Value,
            a.HasOutstandingDebt,
            a.HasInstallments,
            a.HasPlannedInstallmentSchedule,
            a.AreRepaymentsAllowed,
            a.Offers.Select(o =>
            {
                var plan = a.GetInstallmentPlanForOffer(o.Id.Value);
                return new FinancingOfferDto(
                    o.Id.Value,
                    o.Version,
                    o.PurchaseAmount,
                    o.DownPaymentAmount,
                    o.FinancedPrincipal,
                    o.CreatedAtUtc,
                    o.ExpiresAtUtc,
                    o.IsSuperseded,
                    o.IsAccepted,
                    o.AcceptedAtUtc,
                    plan is null ? null : ToPlanSummary(plan));
            }).ToArray(),
            a.Decisions.Select(d => new FinancingDecisionDto(
                d.DecisionId,
                d.Stage.ToString(),
                d.Outcome.ToString(),
                d.ActorId,
                d.DecidedAtUtc,
                d.OfferId)).ToArray(),
            a.CreatedAtUtc,
            a.UpdatedAtUtc);

    private static InstallmentPlanDto ToPlanDto(BnplFinancingApplication application, BnplInstallmentPlan plan) =>
        new(
            plan.Id.Value,
            plan.OfferId,
            application.Id.Value,
            plan.Version,
            plan.TotalScheduledPrincipal,
            plan.IsLocked,
            plan.IsSuperseded,
            application.AcceptedOfferId == plan.OfferId && plan.IsLocked,
            application.AcceptedOffer?.AcceptedAtUtc,
            plan.Items.Select(i => new InstallmentPlanItemDto(
                i.Id.Value,
                i.SequenceNumber,
                i.PrincipalAmount,
                i.DueDate)).ToArray());

    private static InstallmentPlanSummaryDto ToPlanSummary(BnplInstallmentPlan plan) =>
        new(
            plan.Id.Value,
            plan.Version,
            plan.TotalScheduledPrincipal,
            plan.IsLocked,
            plan.IsSuperseded,
            plan.Items.Select(i => new InstallmentPlanItemDto(
                i.Id.Value,
                i.SequenceNumber,
                i.PrincipalAmount,
                i.DueDate)).ToArray());
}

internal sealed record CreateFinancingApplicationRequest(
    Guid CustomerId,
    decimal PurchaseAmount,
    decimal DownPaymentAmount,
    Guid? ApplicationId = null,
    string? PurchaseDescription = null,
    string? MerchantProductReference = null);

internal sealed record UpdateFinancingApplicationDraftRequest(
    decimal PurchaseAmount,
    decimal DownPaymentAmount,
    string? PurchaseDescription = null,
    string? MerchantProductReference = null,
    int? ExpectedVersion = null);

internal sealed record VersionedActionRequest(int? ExpectedVersion = null);

internal sealed record DecisionRequest(string? Note = null, int? ExpectedVersion = null);

internal sealed record CreateOfferRequest(
    Guid? OfferId = null,
    DateTimeOffset? ExpiresAtUtc = null,
    int? ExpectedVersion = null);

internal sealed record AcceptOfferRequest(Guid OfferId, int? ExpectedVersion = null);

internal sealed record PutInstallmentPlanRequest(
    Guid PlanId,
    IReadOnlyList<InstallmentPlanItemRequest> Items,
    int? ExpectedVersion = null);

internal sealed record InstallmentPlanItemRequest(
    Guid ItemId,
    int SequenceNumber,
    decimal PrincipalAmount,
    DateOnly DueDate);

internal sealed record FinancingApplicationDto(
    Guid ApplicationId,
    Guid OrganizationId,
    Guid BranchId,
    Guid CustomerId,
    string Status,
    decimal PurchaseAmount,
    decimal DownPaymentAmount,
    decimal RequestedFinanceAmount,
    string? PurchaseDescription,
    string? MerchantProductReference,
    int AggregateVersion,
    bool EligibilityApproved,
    Guid? CurrentOfferId,
    Guid? AcceptedOfferId,
    Guid? AcceptedPlanId,
    bool HasOutstandingDebt,
    bool HasInstallments,
    bool HasPlannedInstallmentSchedule,
    bool AreRepaymentsAllowed,
    IReadOnlyList<FinancingOfferDto> Offers,
    IReadOnlyList<FinancingDecisionDto> Decisions,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

internal sealed record FinancingOfferDto(
    Guid OfferId,
    int Version,
    decimal PurchaseAmount,
    decimal DownPaymentAmount,
    decimal FinancedPrincipal,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    bool IsSuperseded,
    bool IsAccepted,
    DateTimeOffset? AcceptedAtUtc,
    InstallmentPlanSummaryDto? InstallmentPlan);

internal sealed record FinancingDecisionDto(
    Guid DecisionId,
    string Stage,
    string Outcome,
    Guid ActorId,
    DateTimeOffset DecidedAtUtc,
    Guid? OfferId);

internal sealed record InstallmentPlanDto(
    Guid PlanId,
    Guid OfferId,
    Guid ApplicationId,
    int Version,
    decimal TotalScheduledPrincipal,
    bool IsLocked,
    bool IsSuperseded,
    bool IsAcceptedTerms,
    DateTimeOffset? OfferAcceptedAtUtc,
    IReadOnlyList<InstallmentPlanItemDto> Items);

internal sealed record InstallmentPlanSummaryDto(
    Guid PlanId,
    int Version,
    decimal TotalScheduledPrincipal,
    bool IsLocked,
    bool IsSuperseded,
    IReadOnlyList<InstallmentPlanItemDto> Items);

internal sealed record InstallmentPlanItemDto(
    Guid ItemId,
    int SequenceNumber,
    decimal PrincipalAmount,
    DateOnly DueDate);

internal sealed record FinancingApplicationSearchResponse(
    IReadOnlyList<FinancingApplicationDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
