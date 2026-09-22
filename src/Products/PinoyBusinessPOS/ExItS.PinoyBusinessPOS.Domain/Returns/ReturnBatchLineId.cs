using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Returns;

/// <summary>Strongly typed identifier for one return batch line.</summary>
public sealed class ReturnBatchLineId : IEquatable<ReturnBatchLineId>
{
    public Guid Value { get; }

    private ReturnBatchLineId(Guid value) => Value = value;

    public static ReturnBatchLineId New() => new(Guid.NewGuid());

    public static ReturnBatchLineId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchLineId,
                "ReturnBatchLineId cannot be an empty GUID.");
        }

        return new ReturnBatchLineId(value);
    }

    public bool Equals(ReturnBatchLineId? other) => other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) => obj is ReturnBatchLineId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(ReturnBatchLineId? left, ReturnBatchLineId? right) => Equals(left, right);

    public static bool operator !=(ReturnBatchLineId? left, ReturnBatchLineId? right) => !Equals(left, right);
}
