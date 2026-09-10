namespace ExItS.PinoyBusinessPOS.Application.Purchasing;

/// <summary>
/// Unified Direct Purchases history sources.
/// Local = buyer-owned <c>DirectPurchaseReceipt</c>; B2B = seller-owned <c>Sale</c> projected read-only.
/// </summary>
public static class DirectPurchaseHistorySourceTypes
{
    public const string Local = "Local";
    public const string B2B = "B2B";

    public static bool TryNormalize(string? value, out string? normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (string.Equals(value.Trim(), Local, StringComparison.OrdinalIgnoreCase))
        {
            normalized = Local;
            return true;
        }

        if (string.Equals(value.Trim(), "B2B", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value.Trim(), "ExItsB2B", StringComparison.OrdinalIgnoreCase))
        {
            normalized = B2B;
            return true;
        }

        return false;
    }
}

public static class DirectPurchaseHistoryStatuses
{
    public const string Completed = "Completed";
    public const string Voided = "Voided";
    public const string AwaitingPayment = "AwaitingPayment";

    public static bool TryNormalize(string? value, out string? normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var trimmed = value.Trim();
        if (string.Equals(trimmed, Completed, StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "Posted", StringComparison.OrdinalIgnoreCase))
        {
            normalized = Completed;
            return true;
        }

        if (string.Equals(trimmed, Voided, StringComparison.OrdinalIgnoreCase))
        {
            normalized = Voided;
            return true;
        }

        if (string.Equals(trimmed, AwaitingPayment, StringComparison.OrdinalIgnoreCase))
        {
            normalized = AwaitingPayment;
            return true;
        }

        return false;
    }
}

public sealed record DirectPurchaseHistoryFilter(
    string? SourceType = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    string? Search = null,
    string? Status = null);

/// <summary>Buyer-safe unified list row for Direct Purchases.</summary>
public sealed record DirectPurchaseHistoryItemDto(
    Guid SourceId,
    string SourceType,
    DateTimeOffset OccurredAtUtc,
    string? PurchaseDate,
    string SellerDisplayName,
    Guid? SellerOrganizationId,
    string? SellerPublicOrganizationId,
    string ReferenceNumber,
    int LineCount,
    decimal TotalAmount,
    string Status,
    string? PaymentMethod = null,
    string? SellerStoreDisplayName = null);

public sealed record DirectPurchaseB2bLineDto(
    int LineNumber,
    string ProductNameSnapshot,
    string? SkuSnapshot,
    string? BarcodeSnapshot,
    decimal Quantity,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal LineDiscountAmount,
    decimal LineTotal);

/// <summary>Buyer-safe Transaction Summary projection of a seller Sale (Organization buyer).</summary>
public sealed record DirectPurchaseB2bDetailDto(
    Guid SaleId,
    Guid SellerOrganizationId,
    string? SellerPublicOrganizationId,
    string SellerDisplayName,
    string? SellerStoreDisplayName,
    string SaleNumber,
    DateTimeOffset OccurredAtUtc,
    string Status,
    string PaymentMethod,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxAmount,
    decimal TotalAmount,
    IReadOnlyList<DirectPurchaseB2bLineDto> Lines);

/// <summary>Raw unified history row from the read query (Infrastructure → Application).</summary>
public sealed record DirectPurchaseHistoryRawRow(
    Guid SourceId,
    string SourceType,
    DateTimeOffset OccurredAtUtc,
    DateOnly? PurchaseDate,
    string SellerDisplayName,
    Guid? SellerOrganizationId,
    string? SellerPublicOrganizationId,
    string ReferenceNumber,
    int LineCount,
    decimal TotalAmount,
    string Status,
    string? PaymentMethod,
    string? SellerStoreDisplayName);
