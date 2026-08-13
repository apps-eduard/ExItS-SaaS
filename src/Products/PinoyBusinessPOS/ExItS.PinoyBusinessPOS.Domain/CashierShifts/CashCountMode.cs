using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.CashierShifts;

/// <summary>
/// Organization cash-count policy snapshotted onto each cashier shift at open.
/// Off/Optional never fabricate counted cash; Required is server-enforced.
/// </summary>
public enum CashCountMode
{
    Off = 0,
    Optional = 1,
    Required = 2
}

public static class CashCountModes
{
    public const string Off = nameof(CashCountMode.Off);
    public const string Optional = nameof(CashCountMode.Optional);
    public const string Required = nameof(CashCountMode.Required);

    public static CashCountMode Parse(string? value, CashCountMode whenMissing = CashCountMode.Optional)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return whenMissing;
        }

        if (!Enum.TryParse<CashCountMode>(value.Trim(), ignoreCase: true, out var parsed)
            || !Enum.IsDefined(parsed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCashCountMode,
                "Cash count mode must be Off, Optional, or Required.");
        }

        return parsed;
    }

    public static bool RequiresPhysicalCount(CashCountMode mode) => mode == CashCountMode.Required;

    public static string ClosingState(CashCountMode mode, decimal? closingCashAmount)
    {
        if (closingCashAmount is not null)
        {
            return CashCountStates.Counted;
        }

        return mode == CashCountMode.Off
            ? CashCountStates.NotRequired
            : CashCountStates.NotPerformed;
    }

    public static string OpeningState(CashCountMode mode, bool openingCashCounted)
    {
        if (openingCashCounted)
        {
            return CashCountStates.Counted;
        }

        return mode == CashCountMode.Off
            ? CashCountStates.NotRequired
            : CashCountStates.NotPerformed;
    }
}

public static class CashCountStates
{
    public const string NotRequired = "NotRequired";
    public const string NotPerformed = "NotPerformed";
    public const string Counted = "Counted";
}

public static class CashVarianceKinds
{
    public const string Balanced = "Balanced";
    public const string Over = "Over";
    public const string Short = "Short";

    public static string Classify(decimal variance) =>
        variance == 0m ? Balanced : variance > 0m ? Over : Short;
}
