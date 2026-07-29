using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Subscriptions;

public sealed class SubscriptionId : IEquatable<SubscriptionId>
{
    public Guid Value { get; }

    private SubscriptionId(Guid value) => Value = value;

    public static SubscriptionId New() => new(Guid.NewGuid());

    public static SubscriptionId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidSubscriptionId, "SubscriptionId cannot be an empty GUID.");
        }

        return new SubscriptionId(value);
    }

    public bool Equals(SubscriptionId? other) => other is not null && Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is SubscriptionId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");
    public static bool operator ==(SubscriptionId? left, SubscriptionId? right) => Equals(left, right);
    public static bool operator !=(SubscriptionId? left, SubscriptionId? right) => !Equals(left, right);
}
