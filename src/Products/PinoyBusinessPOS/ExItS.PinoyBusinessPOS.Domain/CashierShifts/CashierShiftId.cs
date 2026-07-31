using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.CashierShifts;

public readonly record struct CashierShiftId(Guid Value)
{
    public static CashierShiftId New() => new(Guid.NewGuid());

    public static CashierShiftId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCashierShiftId,
                "Cashier shift id cannot be an empty GUID.");
        }

        return new CashierShiftId(value);
    }
}
