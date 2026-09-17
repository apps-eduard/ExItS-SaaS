using System.Globalization;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

namespace ExItS.PinoyBusinessPOS.Application.Purchasing;

/// <summary>
/// Per-line physical stock shortage detected before a connected-PO fulfillment wave mutates inventory.
/// </summary>
public sealed record ConnectedPoFulfillmentShortageLine(
    Guid ProductId,
    string ProductName,
    string UnitOfMeasureCode,
    decimal RequiredQty,
    decimal PhysicalOnHandQty,
    decimal ReservedForPoQty,
    decimal ShortageQty);

/// <summary>
/// Thrown when authoritative physical stock cannot cover the fulfillment wave.
/// No stock movement, reservation consume, or PO status change may occur after this is thrown.
/// </summary>
public sealed class ConnectedPoFulfillmentShortageException : Exception
{
    public const string DefaultMessage = "Insufficient stock to fulfill remaining order.";

    public string ErrorCode { get; } = ConnectedSupplierErrorCodes.InsufficientSupplierStock;
    public IReadOnlyList<ConnectedPoFulfillmentShortageLine> Lines { get; }

    public ConnectedPoFulfillmentShortageException(IReadOnlyList<ConnectedPoFulfillmentShortageLine> lines)
        : base(DefaultMessage)
    {
        ArgumentNullException.ThrowIfNull(lines);
        if (lines.Count == 0)
        {
            throw new ArgumentException("At least one shortage line is required.", nameof(lines));
        }

        Lines = lines;
    }

    public IReadOnlyDictionary<string, string> ToErrorDetails()
    {
        var payload = Lines.Select(l => new
        {
            productId = l.ProductId.ToString("D"),
            productName = l.ProductName,
            unitOfMeasureCode = l.UnitOfMeasureCode,
            requiredQty = FormatQty(l.RequiredQty),
            physicalOnHandQty = FormatQty(l.PhysicalOnHandQty),
            reservedForPoQty = FormatQty(l.ReservedForPoQty),
            shortageQty = FormatQty(l.ShortageQty),
        });

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["shortages"] = JsonSerializer.Serialize(payload),
        };
    }

    private static string FormatQty(decimal value) =>
        value == decimal.Truncate(value)
            ? decimal.Truncate(value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.####", CultureInfo.InvariantCulture);
}
