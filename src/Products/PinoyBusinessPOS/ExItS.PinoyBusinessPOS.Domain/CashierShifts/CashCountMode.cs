using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.CashierShifts;

/// <summary>
/// Organization cash-count policy. Configurable policies are Required and Optional.
/// <see cref="Off"/> is retained only for historical shift snapshots; it is not selectable.
/// </summary>
public enum CashCountMode
{
    /// <summary>Legacy stored snapshot. Not configurable. New opens treat leftover org Off as Optional.</summary>
    Off = 0,
    Optional = 1,
    Required = 2
}

public static class CashCountModes
{
    public const string Off = nameof(CashCountMode.Off);
    public const string Optional = nameof(CashCountMode.Optional);
    public const string Required = nameof(CashCountMode.Required);

    public const CashCountMode Default = CashCountMode.Optional;

    public static CashCountMode Parse(string? value, CashCountMode whenMissing = Default)
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
                "Cash count mode must be Optional or Required.");
        }

        return parsed;
    }

    /// <summary>Parse a write/config value. Rejects Off so it cannot be newly selected.</summary>
    public static CashCountMode ParseConfigurable(string? value, CashCountMode whenMissing = Default)
    {
        var parsed = Parse(value, whenMissing);
        if (parsed == CashCountMode.Off)
        {
            throw new DomainException(
                DomainErrorCodes.CashCountModeOffRetired,
                "Off is no longer a configurable cash-count policy. Choose Required or Optional.");
        }

        return parsed;
    }

    /// <summary>Organization Off leftovers open new shifts as Optional. Historical Off snapshots stay Off.</summary>
    public static CashCountMode ForNewShift(CashCountMode organizationMode) =>
        organizationMode == CashCountMode.Off ? CashCountMode.Optional : organizationMode;

    public static bool RequiresPhysicalCount(CashCountMode mode) => mode == CashCountMode.Required;

    public static bool AllowsSkip(CashCountMode mode) => mode != CashCountMode.Required;

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

public static class CashCountKinds
{
    public const string Opening = "Opening";
    public const string Closing = "Closing";
}
