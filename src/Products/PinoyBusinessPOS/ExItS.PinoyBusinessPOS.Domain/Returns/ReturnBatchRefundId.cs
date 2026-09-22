using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Returns;

public sealed class ReturnBatchRefundId : IEquatable<ReturnBatchRefundId>
{
    public Guid Value { get; }

    private ReturnBatchRefundId(Guid value) => Value = value;

    public static ReturnBatchRefundId New() => new(Guid.NewGuid());

    public static ReturnBatchRefundId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchRefundId,
                "Return batch refund id cannot be empty.");
        }

        return new ReturnBatchRefundId(value);
    }

    public bool Equals(ReturnBatchRefundId? other) => other is not null && Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is ReturnBatchRefundId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public static bool operator ==(ReturnBatchRefundId? left, ReturnBatchRefundId? right) => Equals(left, right);
    public static bool operator !=(ReturnBatchRefundId? left, ReturnBatchRefundId? right) => !Equals(left, right);
}
