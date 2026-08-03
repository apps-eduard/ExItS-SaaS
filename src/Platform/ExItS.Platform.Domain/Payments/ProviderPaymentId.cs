using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Payments;

public sealed class ProviderPaymentId : IEquatable<ProviderPaymentId>
{
    public Guid Value { get; }

    private ProviderPaymentId(Guid value) => Value = value;

    public static ProviderPaymentId New() => new(Guid.NewGuid());

    public static ProviderPaymentId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidSaaSPaymentId, "ProviderPaymentId cannot be an empty GUID.");
        }

        return new ProviderPaymentId(value);
    }

    public bool Equals(ProviderPaymentId? other) => other is not null && Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is ProviderPaymentId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");
    public static bool operator ==(ProviderPaymentId? left, ProviderPaymentId? right) => Equals(left, right);
    public static bool operator !=(ProviderPaymentId? left, ProviderPaymentId? right) => !Equals(left, right);
}
