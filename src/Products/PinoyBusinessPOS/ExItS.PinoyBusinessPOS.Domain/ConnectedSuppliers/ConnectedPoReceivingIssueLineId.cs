using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

/// <summary>Strongly typed identifier for one line on a connected-PO receiving-issue case.</summary>
public sealed class ConnectedPoReceivingIssueLineId : IEquatable<ConnectedPoReceivingIssueLineId>
{
    public Guid Value { get; }

    private ConnectedPoReceivingIssueLineId(Guid value) => Value = value;

    public static ConnectedPoReceivingIssueLineId New() => new(Guid.NewGuid());

    public static ConnectedPoReceivingIssueLineId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReceivingIssueLineId,
                "ConnectedPoReceivingIssueLineId cannot be an empty GUID.");
        }

        return new ConnectedPoReceivingIssueLineId(value);
    }

    public bool Equals(ConnectedPoReceivingIssueLineId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is ConnectedPoReceivingIssueLineId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(ConnectedPoReceivingIssueLineId? left, ConnectedPoReceivingIssueLineId? right) =>
        Equals(left, right);

    public static bool operator !=(ConnectedPoReceivingIssueLineId? left, ConnectedPoReceivingIssueLineId? right) =>
        !Equals(left, right);
}
