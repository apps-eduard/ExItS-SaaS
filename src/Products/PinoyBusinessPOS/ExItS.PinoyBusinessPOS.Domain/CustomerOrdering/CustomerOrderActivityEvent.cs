namespace ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

/// <summary>Immutable audit entry for customer-order lifecycle and line adjustments.</summary>
public sealed class CustomerOrderActivityEvent
{
    public const int ActionMaxLength = 64;
    public const int ProductNameMaxLength = 200;
    public const int ReasonMaxLength = 64;
    public const int NoteMaxLength = 512;
    public const int SummaryMaxLength = 512;

    public Guid EventId { get; }
    public DateTimeOffset AtUtc { get; }
    public Guid? ActorId { get; }
    public string Action { get; }
    public Guid? LineId { get; }
    public string? ProductName { get; }
    public decimal? PreviousQuantity { get; }
    public decimal? NewQuantity { get; }
    public string? ReasonCode { get; }
    public string? Note { get; }
    public decimal? PreviousTotal { get; }
    public decimal? NewTotal { get; }
    public string? Summary { get; }

    public CustomerOrderActivityEvent(
        Guid eventId,
        DateTimeOffset atUtc,
        Guid? actorId,
        string action,
        Guid? lineId = null,
        string? productName = null,
        decimal? previousQuantity = null,
        decimal? newQuantity = null,
        string? reasonCode = null,
        string? note = null,
        decimal? previousTotal = null,
        decimal? newTotal = null,
        string? summary = null)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Activity event id is required.", nameof(eventId));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Activity action is required.", nameof(action));
        }

        EventId = eventId;
        AtUtc = atUtc;
        ActorId = actorId;
        Action = Trim(action, ActionMaxLength)!;
        LineId = lineId;
        ProductName = Trim(productName, ProductNameMaxLength);
        PreviousQuantity = previousQuantity;
        NewQuantity = newQuantity;
        ReasonCode = Trim(reasonCode, ReasonMaxLength);
        Note = Trim(note, NoteMaxLength);
        PreviousTotal = previousTotal;
        NewTotal = newTotal;
        Summary = Trim(summary, SummaryMaxLength);
    }

    public static class Actions
    {
        public const string QuantityReduced = "QuantityReduced";
        public const string ItemCancelled = "ItemCancelled";
        public const string OrderTotalAdjusted = "OrderTotalAdjusted";
        public const string OrderCancelled = "OrderCancelled";
        public const string PaymentConfirmed = "PaymentConfirmed";
        public const string CustomerRefused = "CustomerRefused";
    }

    private static string? Trim(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > max ? trimmed[..max] : trimmed;
    }
}
