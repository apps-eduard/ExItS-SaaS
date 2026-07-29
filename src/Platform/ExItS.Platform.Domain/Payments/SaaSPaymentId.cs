using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Payments;

/// <summary>
/// Strongly typed identifier for a manually reported SaaS subscription payment. Not a POS/retail
/// sale, Utang credit payment, gateway transaction, or invoice identifier.
/// </summary>
public sealed class SaaSPaymentId : IEquatable<SaaSPaymentId>
{
    public Guid Value { get; }

    private SaaSPaymentId(Guid value) => Value = value;

    public static SaaSPaymentId New() => new(Guid.NewGuid());

    public static SaaSPaymentId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidSaaSPaymentId, "SaaSPaymentId cannot be an empty GUID.");
        }

        return new SaaSPaymentId(value);
    }

    public bool Equals(SaaSPaymentId? other) => other is not null && Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is SaaSPaymentId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");
    public static bool operator ==(SaaSPaymentId? left, SaaSPaymentId? right) => Equals(left, right);
    public static bool operator !=(SaaSPaymentId? left, SaaSPaymentId? right) => !Equals(left, right);
}
