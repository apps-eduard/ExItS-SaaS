using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Domain.Purchasing;

/// <summary>Stable UI/API codes for "other" discrepancy classification (not damaged, not missing).</summary>
public static class ReceiveDiscrepancyOtherReason
{
    public const int CodeMaxLength = 32;
    public const int NoteMaxLength = 280;

    public const string WrongItem = "WrongItem";
    public const string WrongVariant = "WrongVariant";
    public const string Expired = "Expired";
    public const string PackagingIssue = "PackagingIssue";
    public const string QualityIssue = "QualityIssue";
    public const string Other = "Other";

    public static IReadOnlyList<string> Codes { get; } =
    [
        WrongItem,
        WrongVariant,
        Expired,
        PackagingIssue,
        QualityIssue,
        Other
    ];

    public static bool TryParse(string? code, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var trimmed = code.Trim();
        var match = Codes.FirstOrDefault(c => string.Equals(c, trimmed, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return false;
        }

        normalized = match;
        return true;
    }

    public static void EnsureValid(string? code, string? note, decimal otherQty)
    {
        if (otherQty <= 0m)
        {
            return;
        }

        if (!TryParse(code, out var normalized))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseReceiveQuantity,
                "Other discrepancy reason is required when other quantity is greater than zero.");
        }

        if (string.Equals(normalized, Other, StringComparison.Ordinal)
            && string.IsNullOrWhiteSpace(note))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseReceiveQuantity,
                "A short description is required when other reason is Other.");
        }
    }

    public static ConnectedPoReceivingDiscrepancyKind ResolvePoKind(
        decimal damagedQty,
        decimal notDeliveredQty,
        decimal otherQty,
        string? otherReasonCode)
    {
        if (otherQty > 0m && damagedQty <= 0m && notDeliveredQty <= 0m
            && TryParse(otherReasonCode, out var code))
        {
            return code switch
            {
                WrongItem => ConnectedPoReceivingDiscrepancyKind.WrongItem,
                Expired => ConnectedPoReceivingDiscrepancyKind.Expired,
                _ => ConnectedPoReceivingDiscrepancyKind.Other
            };
        }

        if (otherQty > 0m)
        {
            return ConnectedPoReceivingDiscrepancyKind.Other;
        }

        if (damagedQty > 0m && notDeliveredQty > 0m)
        {
            return ConnectedPoReceivingDiscrepancyKind.Other;
        }

        if (damagedQty > 0m)
        {
            return ConnectedPoReceivingDiscrepancyKind.Damaged;
        }

        if (notDeliveredQty > 0m)
        {
            return ConnectedPoReceivingDiscrepancyKind.Short;
        }

        return ConnectedPoReceivingDiscrepancyKind.None;
    }

    public static InventoryTransferDiscrepancyReason ResolveTransferReason(string? otherReasonCode)
    {
        if (TryParse(otherReasonCode, out var code)
            && string.Equals(code, WrongItem, StringComparison.Ordinal))
        {
            return InventoryTransferDiscrepancyReason.WrongItem;
        }

        return InventoryTransferDiscrepancyReason.Other;
    }

    public static string? NormalizeNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        var trimmed = note.Trim();
        if (trimmed.Length > NoteMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGoodsReceiptNotes,
                $"Other reason note must be at most {NoteMaxLength} characters.");
        }

        return trimmed;
    }
}
