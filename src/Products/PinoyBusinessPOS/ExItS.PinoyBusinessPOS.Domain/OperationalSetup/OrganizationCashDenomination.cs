using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.OperationalSetup;

/// <summary>
/// Organization-owned cash denomination used as a counting helper. Cashiers cannot invent values;
/// historical shift breakdowns snapshot the value used at count time.
/// PinoyBusinessPOS is currently PHP-authoritative; defaults are Philippine denominations.
/// </summary>
public sealed class OrganizationCashDenomination
{
    public const int DisplayLabelMaxLength = 32;

    public OrganizationCashDenominationId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public decimal Value { get; }
    public string? DisplayLabel { get; private set; }
    public bool IsEnabled { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private OrganizationCashDenomination(
        OrganizationCashDenominationId id,
        PosOrganizationId organizationId,
        decimal value,
        string? displayLabel,
        bool isEnabled,
        int sortOrder,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        Value = value;
        DisplayLabel = displayLabel;
        IsEnabled = isEnabled;
        SortOrder = sortOrder;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static OrganizationCashDenomination Create(
        PosOrganizationId organizationId,
        decimal value,
        int sortOrder,
        DateTimeOffset utcNow,
        bool isEnabled = true,
        string? displayLabel = null,
        OrganizationCashDenominationId? id = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        return new OrganizationCashDenomination(
            id ?? OrganizationCashDenominationId.New(),
            organizationId,
            NormalizeValue(value),
            NormalizeLabel(displayLabel),
            isEnabled,
            NormalizeSortOrder(sortOrder),
            utcNow,
            utcNow);
    }

    public static OrganizationCashDenomination Rehydrate(
        OrganizationCashDenominationId id,
        PosOrganizationId organizationId,
        decimal value,
        string? displayLabel,
        bool isEnabled,
        int sortOrder,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(id, organizationId, value, displayLabel, isEnabled, sortOrder, createdAtUtc, updatedAtUtc);

    public void SetEnabled(bool isEnabled, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        IsEnabled = isEnabled;
        UpdatedAtUtc = utcNow;
    }

    public void Reorder(int sortOrder, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        SortOrder = NormalizeSortOrder(sortOrder);
        UpdatedAtUtc = utcNow;
    }

    public void SetDisplayLabel(string? displayLabel, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        DisplayLabel = NormalizeLabel(displayLabel);
        UpdatedAtUtc = utcNow;
    }

    public static decimal NormalizeValue(decimal value)
    {
        if (value <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCashDenominationValue,
                "Denomination value must be greater than zero.");
        }

        if (!SaleMoney.HasAtMostDecimals(value, SaleMoney.MonetaryDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCashDenominationValue,
                "Denomination value must have at most 2 decimal places.");
        }

        return SaleMoney.RoundMoney(value);
    }

    private static int NormalizeSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCashDenominationSortOrder,
                "Denomination sort order cannot be negative.");
        }

        return sortOrder;
    }

    private static string? NormalizeLabel(string? displayLabel)
    {
        if (string.IsNullOrWhiteSpace(displayLabel))
        {
            return null;
        }

        var trimmed = displayLabel.Trim();
        if (trimmed.Length > DisplayLabelMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCashDenominationLabel,
                $"Denomination label must be at most {DisplayLabelMaxLength} characters.");
        }

        return trimmed;
    }
}

/// <summary>
/// Default PHP bill/coin values seeded for new PinoyBusinessPOS organizations.
/// Practical current set: 1000, 500, 200, 100, 50, 20, 10, 5, 1, 0.25, 0.05, 0.01.
/// PHP 0.50 and 0.10 are not seeded as current defaults; owners may still add them
/// (or 5000) without a code deployment. Existing organization rows are not rewritten.
/// </summary>
public static class PhilippineCashDenominationDefaults
{
    public static readonly decimal[] Values =
    [
        1000.00m,
        500.00m,
        200.00m,
        100.00m,
        50.00m,
        20.00m,
        10.00m,
        5.00m,
        1.00m,
        0.25m,
        0.05m,
        0.01m
    ];
}
