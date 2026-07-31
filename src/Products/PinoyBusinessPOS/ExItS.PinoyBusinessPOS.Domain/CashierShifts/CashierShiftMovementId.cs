using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.CashierShifts;

public readonly record struct CashierShiftMovementId(Guid Value)
{
    public static CashierShiftMovementId New() => new(Guid.NewGuid());

    public static CashierShiftMovementId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCashierShiftMovementId,
                "Cashier shift movement id cannot be an empty GUID.");
        }

        return new CashierShiftMovementId(value);
    }
}
