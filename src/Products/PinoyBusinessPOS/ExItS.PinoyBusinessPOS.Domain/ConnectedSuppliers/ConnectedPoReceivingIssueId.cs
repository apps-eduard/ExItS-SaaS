using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

/// <summary>Strongly typed identifier for a connected-PO receiving-issue case.</summary>
public sealed class ConnectedPoReceivingIssueId : IEquatable<ConnectedPoReceivingIssueId>
{
    public Guid Value { get; }

    private ConnectedPoReceivingIssueId(Guid value) => Value = value;

    public static ConnectedPoReceivingIssueId New() => new(Guid.NewGuid());

    public static ConnectedPoReceivingIssueId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReceivingIssueId,
                "ConnectedPoReceivingIssueId cannot be an empty GUID.");
        }

        return new ConnectedPoReceivingIssueId(value);
    }

    public bool Equals(ConnectedPoReceivingIssueId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is ConnectedPoReceivingIssueId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(ConnectedPoReceivingIssueId? left, ConnectedPoReceivingIssueId? right) =>
        Equals(left, right);

    public static bool operator !=(ConnectedPoReceivingIssueId? left, ConnectedPoReceivingIssueId? right) =>
        !Equals(left, right);
}
