using System.Text.Json;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.CustomerOrdering;

internal static class CustomerOrderActivityJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private sealed record Row(
        Guid EventId,
        DateTimeOffset AtUtc,
        Guid? ActorId,
        string Action,
        Guid? LineId,
        string? ProductName,
        decimal? PreviousQuantity,
        decimal? NewQuantity,
        string? ReasonCode,
        string? Note,
        decimal? PreviousTotal,
        decimal? NewTotal,
        string? Summary);

    public static string? Serialize(IReadOnlyList<CustomerOrderActivityEvent> events)
    {
        if (events.Count == 0)
        {
            return null;
        }

        var rows = events.Select(e => new Row(
            e.EventId,
            e.AtUtc,
            e.ActorId,
            e.Action,
            e.LineId,
            e.ProductName,
            e.PreviousQuantity,
            e.NewQuantity,
            e.ReasonCode,
            e.Note,
            e.PreviousTotal,
            e.NewTotal,
            e.Summary)).ToList();
        return JsonSerializer.Serialize(rows, Options);
    }

    public static IReadOnlyList<CustomerOrderActivityEvent> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var rows = JsonSerializer.Deserialize<List<Row>>(json, Options);
            if (rows is null || rows.Count == 0)
            {
                return [];
            }

            return rows.Select(r => new CustomerOrderActivityEvent(
                r.EventId,
                r.AtUtc,
                r.ActorId,
                r.Action,
                r.LineId,
                r.ProductName,
                r.PreviousQuantity,
                r.NewQuantity,
                r.ReasonCode,
                r.Note,
                r.PreviousTotal,
                r.NewTotal,
                r.Summary)).ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
