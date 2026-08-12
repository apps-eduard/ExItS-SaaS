using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

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
/// Lazy receipt detail: WP03 authorization → authorized PosCustomerId → sale ownership check.
/// Does not batch, does not accept activity EntryIds, does not load catalog.
/// </summary>
public sealed class GetLinkedCustomerSaleReceipt
{
    private const string NotFoundMessage = "Receipt was not found.";

    private readonly AuthorizeLinkedCustomerStatementAccess _authorize;
    private readonly ISaleRepository _sales;

    public GetLinkedCustomerSaleReceipt(
        AuthorizeLinkedCustomerStatementAccess authorize,
        ISaleRepository sales)
    {
        _authorize = authorize;
        _sales = sales;
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
        var sale = await _sales
            .GetByIdAsync(PosOrganizationId.From(ctx.OrganizationId), SaleId.From(saleId), cancellationToken)
            .ConfigureAwait(false);

        // Fail closed: missing, wrong org (repo scoped), or different POS customer → same 404.
        if (sale is null
            || sale.CustomerId is null
            || sale.CustomerId.Value != ctx.PosCustomerId)
        {
            return NotFound();
        }

        return ApplicationResult<LinkedCustomerSaleReceiptDto>.Success(Map(ctx, sale, currencyCode));
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
        // Outstanding effect of this sale on Business Utang (not a historical running balance).
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
