using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Returns;

/// <summary>Strongly typed identifier for a return inspection batch.</summary>
public sealed class ReturnBatchId : IEquatable<ReturnBatchId>
{
    public Guid Value { get; }

    private ReturnBatchId(Guid value) => Value = value;

    public static ReturnBatchId New() => new(Guid.NewGuid());

    public static ReturnBatchId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchId,
                "ReturnBatchId cannot be an empty GUID.");
        }

        return new ReturnBatchId(value);
    }

    public bool Equals(ReturnBatchId? other) => other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) => obj is ReturnBatchId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(ReturnBatchId? left, ReturnBatchId? right) => Equals(left, right);

    public static bool operator !=(ReturnBatchId? left, ReturnBatchId? right) => !Equals(left, right);
}
