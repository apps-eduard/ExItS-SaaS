using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Application.Statements;

/// <summary>
/// Privacy-safe Personal receipt for one linked-customer sale (lazy-loaded on explicit open).
/// Built only from sale-time snapshots — never live catalog price/name.
/// </summary>
public sealed record LinkedCustomerSaleReceiptLineDto(
    int LineNumber,
    string ProductNameSnapshot,
    decimal Quantity,
    string UnitOfMeasure,
    string SellingMode,
    decimal UnitPriceSnapshot,
    decimal LineTotal);

public sealed record LinkedCustomerSaleReceiptDto(
    Guid OrganizationId,
    Guid PlatformBusinessCustomerId,
    Guid PosCustomerId,
    Guid SaleId,
    string ReceiptNumber,
    DateTimeOffset OccurredAtUtc,
    string Status,
    string PaymentMethod,
    string Currency,
    string? MerchantDisplayName,
    string? BranchDisplayName,
    decimal Subtotal,
    decimal? DiscountAmount,
    decimal TaxAmount,
    decimal Total,
    decimal? UtangAmount,
    decimal? PaidAmount,
    decimal? OutstandingEffect,
    IReadOnlyList<LinkedCustomerSaleReceiptLineDto> Lines);

/// <summary>
/// Lazy receipt detail: WP03 authorization → ownership → free-window / open-debt / entitlement.
/// </summary>
public sealed class GetLinkedCustomerSaleReceipt
{
    private const string NotFoundMessage = "Receipt was not found.";
    private const string ExtendedRequiredMessage =
        "Extended digital records entitlement is required to open this settled historical receipt.";

    private readonly AuthorizeLinkedCustomerStatementAccess _authorize;
    private readonly ISaleRepository _sales;
    private readonly ICreditEntryRepository _credits;
    private readonly IOutstandingBalanceService _outstanding;
    private readonly IPersonalFeatureEntitlementClient _entitlements;
    private readonly IOptions<PersonalStatementsOptions> _options;
    private readonly IClock _clock;

    public GetLinkedCustomerSaleReceipt(
        AuthorizeLinkedCustomerStatementAccess authorize,
        ISaleRepository sales,
        ICreditEntryRepository credits,
        IOutstandingBalanceService outstanding,
        IPersonalFeatureEntitlementClient entitlements,
        IOptions<PersonalStatementsOptions> options,
        IClock clock)
    {
        _authorize = authorize;
        _sales = sales;
        _credits = credits;
        _outstanding = outstanding;
        _entitlements = entitlements;
        _options = options;
        _clock = clock;
    }

    public async Task<ApplicationResult<LinkedCustomerSaleReceiptDto>> ExecuteAsync(
        Guid organizationId,
        Guid platformBusinessCustomerId,
        Guid saleId,
        string currencyCode = "PHP",
        CancellationToken cancellationToken = default)
    {
        if (saleId == Guid.Empty)
        {
            return NotFound();
        }

        var auth = await _authorize
            .ExecuteAsync(organizationId, platformBusinessCustomerId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (!auth.IsSuccess)
        {
            return ApplicationResult<LinkedCustomerSaleReceiptDto>.Failure(auth.ErrorCode!, auth.ErrorMessage!);
        }

        var ctx = auth.Value!;
        var orgId = PosOrganizationId.From(ctx.OrganizationId);
        var sale = await _sales
            .GetByIdAsync(orgId, SaleId.From(saleId), cancellationToken)
            .ConfigureAwait(false);

        // Fail closed before revealing entitlement: missing / wrong customer → same 404.
        if (sale is null
            || sale.CustomerId is null
            || sale.CustomerId.Value != ctx.PosCustomerId)
        {
            return NotFound();
        }

        var asOfUtc = _clock.UtcNow;
        var freeStart = PersonalHistoryWindows.ComputeFreeWindowStart(
            asOfUtc,
            _options.Value.FreeRecentMonths);

        var openDebtAllows = false;
        if (sale.RecordedAtUtc < freeStart)
        {
            openDebtAllows = await IsOpenDebtEvidenceAsync(orgId, sale, cancellationToken)
                .ConfigureAwait(false);
        }

        var entitled = false;
        if (sale.RecordedAtUtc < freeStart && !openDebtAllows)
        {
            entitled = await _entitlements
                .HasActiveEntitlementAsync(PersonalSettledHistoryPolicy.ExtendedFeatureCode, cancellationToken)
                .ConfigureAwait(false);
        }

        var decision = PersonalSettledHistoryPolicy.EvaluateDetailAccess(
            sale.RecordedAtUtc,
            freeStart,
            openDebtAllows,
            entitled);
        if (decision == PersonalHistoryDetailAccessDecision.ExtendedHistoryRequired)
        {
            return ApplicationResult<LinkedCustomerSaleReceiptDto>.Failure(
                ApplicationErrorCodes.ExtendedHistoryRequired,
                ExtendedRequiredMessage);
        }

        return ApplicationResult<LinkedCustomerSaleReceiptDto>.Success(Map(ctx, sale, currencyCode));
    }

    private async Task<bool> IsOpenDebtEvidenceAsync(
        PosOrganizationId orgId,
        Sale sale,
        CancellationToken cancellationToken)
    {
        var outstanding = await _outstanding
            .GetOutstandingAsync(orgId, sale.CustomerId!, cancellationToken)
            .ConfigureAwait(false);

        if (sale.PaymentMethod != SalePaymentMethod.Utang || sale.LinkedCreditEntryId is null)
        {
            return PersonalSettledHistoryPolicy.OpenDebtReceiptExceptionApplies(
                outstanding,
                isUtangSale: false,
                hasLinkedCredit: false,
                linkedCreditIsActive: false);
        }

        var credit = await _credits
            .GetByIdAsync(orgId, sale.CustomerId!, sale.LinkedCreditEntryId, cancellationToken)
            .ConfigureAwait(false);

        return PersonalSettledHistoryPolicy.OpenDebtReceiptExceptionApplies(
            outstanding,
            isUtangSale: true,
            hasLinkedCredit: true,
            linkedCreditIsActive: credit is not null && credit.Status == CreditEntryStatus.Active);
    }

    private static LinkedCustomerSaleReceiptDto Map(
        AuthorizedLinkedCustomerContext ctx,
        Sale sale,
        string currencyCode)
    {
        var isUtang = sale.PaymentMethod == SalePaymentMethod.Utang;
        var isCompleted = sale.Status == SaleStatus.Completed;

        decimal? utangAmount = isUtang ? sale.Total : null;
        decimal? paidAmount = isUtang ? 0m : sale.Total;
        decimal? outstandingEffect = isUtang && isCompleted ? sale.Total : 0m;

        var lines = sale.Lines
            .OrderBy(l => l.LineNumber)
            .Select(l => new LinkedCustomerSaleReceiptLineDto(
                l.LineNumber,
                l.NameSnapshot,
                l.Quantity,
                UnitOfMeasures.ToCode(l.UnitOfMeasureSnapshot),
                SellingModes.ToCode(l.SellingModeSnapshot),
                l.UnitPrice,
                l.LineTotal))
            .ToList();

        return new LinkedCustomerSaleReceiptDto(
            ctx.OrganizationId,
            ctx.PlatformBusinessCustomerId,
            ctx.PosCustomerId,
            sale.Id.Value,
            sale.SaleNumber,
            sale.RecordedAtUtc,
            sale.Status.ToString(),
            SalePaymentMethods.ToCode(sale.PaymentMethod),
            string.IsNullOrWhiteSpace(currencyCode) ? "PHP" : currencyCode.Trim().ToUpperInvariant(),
            MerchantDisplayName: null,
            BranchDisplayName: null,
            sale.Subtotal,
            DiscountAmount: null,
            sale.TaxAmount,
            sale.Total,
            utangAmount,
            paidAmount,
            outstandingEffect,
            lines);
    }

    private static ApplicationResult<LinkedCustomerSaleReceiptDto> NotFound() =>
        ApplicationResult<LinkedCustomerSaleReceiptDto>.Failure(
            ApplicationErrorCodes.ReceiptNotFound,
            NotFoundMessage);
}
