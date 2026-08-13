using System.Text.RegularExpressions;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.OperationalSetup;

/// <summary>
/// Organization-owned POS operational setup (P17-WP02). One row per organization.
/// </summary>
public sealed class PosOperationalSetup
{
    public const int StoreDisplayNameMaxLength = 128;
    public const int CurrencyCodeMaxLength = 3;
    public const int ReceiptHeaderMaxLength = 256;
    public const int ReceiptFooterMaxLength = 256;
    public const int BusinessAddressMaxLength = 256;
    public const int ContactPhoneMaxLength = 32;
    public const decimal MaxTaxRatePercent = 100m;

    private static readonly Regex StoreDisplayNamePattern = new(
        @"^[\p{L}\p{N}][\p{L}\p{N} .'\-&/]{0,126}[\p{L}\p{N}.]?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex CurrencyCodePattern = new(
        @"^[A-Z]{3}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public PosOrganizationId OrganizationId { get; }
    public string StoreDisplayName { get; private set; }
    public string CurrencyCode { get; private set; }
    public TaxPricingMode TaxPricingMode { get; private set; }
    public decimal TaxRatePercent { get; private set; }
    public string? ReceiptHeader { get; private set; }
    public string? ReceiptFooter { get; private set; }
    public string? BusinessAddress { get; private set; }
    public string? ContactPhone { get; private set; }
    public RegisterId? DefaultRegisterId { get; private set; }
    public CashCountMode CashCountMode { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public Guid CreatedBy { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid UpdatedBy { get; private set; }

    private PosOperationalSetup(
        PosOrganizationId organizationId,
        string storeDisplayName,
        string currencyCode,
        TaxPricingMode taxPricingMode,
        decimal taxRatePercent,
        string? receiptHeader,
        string? receiptFooter,
        string? businessAddress,
        string? contactPhone,
        RegisterId? defaultRegisterId,
        CashCountMode cashCountMode,
        bool isCompleted,
        DateTimeOffset? completedAtUtc,
        DateTimeOffset createdAtUtc,
        Guid createdBy,
        DateTimeOffset updatedAtUtc,
        Guid updatedBy)
    {
        OrganizationId = organizationId;
        StoreDisplayName = storeDisplayName;
        CurrencyCode = currencyCode;
        TaxPricingMode = taxPricingMode;
        TaxRatePercent = taxRatePercent;
        ReceiptHeader = receiptHeader;
        ReceiptFooter = receiptFooter;
        BusinessAddress = businessAddress;
        ContactPhone = contactPhone;
        DefaultRegisterId = defaultRegisterId;
        CashCountMode = cashCountMode;
        IsCompleted = isCompleted;
        CompletedAtUtc = completedAtUtc;
        CreatedAtUtc = createdAtUtc;
        CreatedBy = createdBy;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedBy = updatedBy;
    }

    public static PosOperationalSetup CreateIncomplete(
        PosOrganizationId organizationId,
        Guid actorId,
        DateTimeOffset utcNow) =>
        new(
            organizationId,
            string.Empty,
            "PHP",
            TaxPricingMode.TaxExclusive,
            0m,
            null,
            null,
            null,
            null,
            null,
            CashCountMode.Required,
            isCompleted: false,
            completedAtUtc: null,
            utcNow,
            actorId,
            utcNow,
            actorId);

    public static PosOperationalSetup Rehydrate(
        PosOrganizationId organizationId,
        string storeDisplayName,
        string currencyCode,
        TaxPricingMode taxPricingMode,
        decimal taxRatePercent,
        string? receiptHeader,
        string? receiptFooter,
        string? businessAddress,
        string? contactPhone,
        RegisterId? defaultRegisterId,
        bool isCompleted,
        DateTimeOffset? completedAtUtc,
        DateTimeOffset createdAtUtc,
        Guid createdBy,
        DateTimeOffset updatedAtUtc,
        Guid updatedBy,
        CashCountMode cashCountMode = CashCountMode.Required) =>
        new(
            organizationId,
            storeDisplayName,
            currencyCode,
            taxPricingMode,
            taxRatePercent,
            receiptHeader,
            receiptFooter,
            businessAddress,
            contactPhone,
            defaultRegisterId,
            cashCountMode,
            isCompleted,
            completedAtUtc,
            createdAtUtc,
            createdBy,
            updatedAtUtc,
            updatedBy);

    public void Complete(
        string storeDisplayName,
        string currencyCode,
        TaxPricingMode taxPricingMode,
        decimal taxRatePercent,
        string? receiptHeader,
        string? receiptFooter,
        string? businessAddress,
        string? contactPhone,
        RegisterId defaultRegisterId,
        Guid actorId,
        DateTimeOffset utcNow,
        CashCountMode cashCountMode = CashCountMode.Required)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(actorId);

        StoreDisplayName = NormalizeStoreDisplayName(storeDisplayName);
        CurrencyCode = NormalizeCurrencyCode(currencyCode);
        TaxPricingMode = taxPricingMode;
        TaxRatePercent = NormalizeTaxRatePercent(taxRatePercent);
        ReceiptHeader = NormalizeOptional(receiptHeader, ReceiptHeaderMaxLength, DomainErrorCodes.InvalidOperationalSetupReceiptHeader);
        ReceiptFooter = NormalizeOptional(receiptFooter, ReceiptFooterMaxLength, DomainErrorCodes.InvalidOperationalSetupReceiptFooter);
        BusinessAddress = NormalizeOptional(businessAddress, BusinessAddressMaxLength, DomainErrorCodes.InvalidOperationalSetupBusinessAddress);
        ContactPhone = NormalizeOptional(contactPhone, ContactPhoneMaxLength, DomainErrorCodes.InvalidOperationalSetupContactPhone);
        DefaultRegisterId = defaultRegisterId ?? throw new DomainException(
            DomainErrorCodes.OperationalSetupDefaultRegisterRequired,
            "A default register is required to complete operational setup.");
        CashCountMode = cashCountMode;
        IsCompleted = true;
        CompletedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
        UpdatedBy = actorId;
    }

    public void Update(
        string storeDisplayName,
        string currencyCode,
        TaxPricingMode taxPricingMode,
        decimal taxRatePercent,
        string? receiptHeader,
        string? receiptFooter,
        string? businessAddress,
        string? contactPhone,
        Guid actorId,
        DateTimeOffset utcNow,
        CashCountMode? cashCountMode = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(actorId);

        if (!IsCompleted)
        {
            throw new DomainException(
                DomainErrorCodes.OperationalSetupIncomplete,
                "Operational setup must be completed before it can be updated.");
        }

        StoreDisplayName = NormalizeStoreDisplayName(storeDisplayName);
        CurrencyCode = NormalizeCurrencyCode(currencyCode);
        TaxPricingMode = taxPricingMode;
        TaxRatePercent = NormalizeTaxRatePercent(taxRatePercent);
        ReceiptHeader = NormalizeOptional(receiptHeader, ReceiptHeaderMaxLength, DomainErrorCodes.InvalidOperationalSetupReceiptHeader);
        ReceiptFooter = NormalizeOptional(receiptFooter, ReceiptFooterMaxLength, DomainErrorCodes.InvalidOperationalSetupReceiptFooter);
        BusinessAddress = NormalizeOptional(businessAddress, BusinessAddressMaxLength, DomainErrorCodes.InvalidOperationalSetupBusinessAddress);
        ContactPhone = NormalizeOptional(contactPhone, ContactPhoneMaxLength, DomainErrorCodes.InvalidOperationalSetupContactPhone);
        if (cashCountMode is not null)
        {
            CashCountMode = cashCountMode.Value;
        }

        UpdatedAtUtc = utcNow;
        UpdatedBy = actorId;
    }

    public void SetDefaultRegister(RegisterId registerId, Guid actorId, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(actorId);
        DefaultRegisterId = registerId;
        UpdatedAtUtc = utcNow;
        UpdatedBy = actorId;
    }

    private static string NormalizeStoreDisplayName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOperationalSetupStoreDisplayName,
                "Store display name is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > StoreDisplayNameMaxLength || !StoreDisplayNamePattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOperationalSetupStoreDisplayName,
                "Store display name is invalid.");
        }

        return trimmed;
    }

    private static string NormalizeCurrencyCode(string? currencyCode)
    {
        var normalized = string.IsNullOrWhiteSpace(currencyCode)
            ? "PHP"
            : currencyCode.Trim().ToUpperInvariant();

        if (normalized.Length > CurrencyCodeMaxLength || !CurrencyCodePattern.IsMatch(normalized))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOperationalSetupCurrencyCode,
                "Currency code must be a three-letter ISO code.");
        }

        return normalized;
    }

    private static decimal NormalizeTaxRatePercent(decimal taxRatePercent)
    {
        if (taxRatePercent < 0 || taxRatePercent > MaxTaxRatePercent)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOperationalSetupTaxRate,
                $"Tax rate must be between 0 and {MaxTaxRatePercent}.");
        }

        return SaleMoney.RoundMoney(taxRatePercent);
    }

    private static string? NormalizeOptional(string? value, int maxLength, string errorCode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(errorCode, $"Value cannot exceed {maxLength} characters.");
        }

        return trimmed;
    }
}
