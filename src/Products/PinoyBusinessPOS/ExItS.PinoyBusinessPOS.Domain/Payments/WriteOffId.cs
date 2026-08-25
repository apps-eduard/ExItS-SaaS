using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Payments;

public readonly record struct WriteOffId
{
    public Guid Value { get; }

    private WriteOffId(Guid value) => Value = value;

    public static WriteOffId New() => new(Guid.NewGuid());

    public static WriteOffId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidWriteOffId, "Write-off id must be a non-empty GUID.");
        }

        return new WriteOffId(value);
    }

    public override string ToString() => Value.ToString("D");
}
