using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Domain.Organizations;

public sealed class BusinessCreditOpeningBalanceId : IEquatable<BusinessCreditOpeningBalanceId>
{
    public Guid Value { get; }

    private BusinessCreditOpeningBalanceId(Guid value) => Value = value;

    public static BusinessCreditOpeningBalanceId New() => new(Guid.NewGuid());

    public static BusinessCreditOpeningBalanceId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBusinessCreditOpeningBalanceId,
                "Business credit opening balance id is required.");
        }

        return new BusinessCreditOpeningBalanceId(value);
    }

    public bool Equals(BusinessCreditOpeningBalanceId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is BusinessCreditOpeningBalanceId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");
}

/// <summary>
/// Organization-owned opening Business Credit balance imported from Personal Utang (Platform enrollment layer).
/// Independent of POS product ledger entries; carries ADR-020 provenance.
/// </summary>
public sealed class BusinessCreditOpeningBalance
{
    public BusinessCreditOpeningBalanceId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public CreditCustomerId CreditCustomerId { get; }
    public BusinessCustomerId BusinessCustomerId { get; }
    public decimal Amount { get; }
    public string CurrencyCode { get; }
    public DateTimeOffset EffectiveDateUtc { get; }
    public PersonalUtangMigrationSourceType SourceType { get; }
    public Guid SourceRecordId { get; }
    public PersonalUtangMigrationBatchId MigrationBatchId { get; }
    public PlatformUserId ImportedByUserId { get; }
    public DateTimeOffset ImportedAtUtc { get; }
    public string DestinationProduct { get; }

    private BusinessCreditOpeningBalance(
        BusinessCreditOpeningBalanceId id,
        PlatformOrganizationId organizationId,
        CreditCustomerId creditCustomerId,
        BusinessCustomerId businessCustomerId,
        decimal amount,
        string currencyCode,
        DateTimeOffset effectiveDateUtc,
        PersonalUtangMigrationSourceType sourceType,
        Guid sourceRecordId,
        PersonalUtangMigrationBatchId migrationBatchId,
        PlatformUserId importedByUserId,
        DateTimeOffset importedAtUtc,
        string destinationProduct)
    {
        Id = id;
        OrganizationId = organizationId;
        CreditCustomerId = creditCustomerId;
        BusinessCustomerId = businessCustomerId;
        Amount = amount;
        CurrencyCode = currencyCode;
        EffectiveDateUtc = effectiveDateUtc;
        SourceType = sourceType;
        SourceRecordId = sourceRecordId;
        MigrationBatchId = migrationBatchId;
        ImportedByUserId = importedByUserId;
        ImportedAtUtc = importedAtUtc;
        DestinationProduct = destinationProduct;
    }

    public static BusinessCreditOpeningBalance Create(
        PlatformOrganizationId organizationId,
        CreditCustomerId creditCustomerId,
        BusinessCustomerId businessCustomerId,
        decimal amount,
        string currencyCode,
        DateTimeOffset effectiveDateUtc,
        PersonalUtangMigrationSourceType sourceType,
        Guid sourceRecordId,
        PersonalUtangMigrationBatchId migrationBatchId,
        PlatformUserId importedByUserId,
        DateTimeOffset importedAtUtc,
        string destinationProduct,
        BusinessCreditOpeningBalanceId? id = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(creditCustomerId);
        ArgumentNullException.ThrowIfNull(businessCustomerId);
        ArgumentNullException.ThrowIfNull(migrationBatchId);
        ArgumentNullException.ThrowIfNull(importedByUserId);
        EnsureUtc(effectiveDateUtc);
        EnsureUtc(importedAtUtc);

        if (amount < 0)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalUtangAmountInvalid,
                "Opening balance amount cannot be negative.");
        }

        if (sourceRecordId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalUtangMigrationSelectionRequired,
                "Source record id is required for provenance.");
        }

        return new BusinessCreditOpeningBalance(
            id ?? BusinessCreditOpeningBalanceId.New(),
            organizationId,
            creditCustomerId,
            businessCustomerId,
            amount,
            NormalizeCurrency(currencyCode),
            effectiveDateUtc,
            sourceType,
            sourceRecordId,
            migrationBatchId,
            importedByUserId,
            importedAtUtc,
            NormalizeProduct(destinationProduct));
    }

    public static BusinessCreditOpeningBalance Rehydrate(
        BusinessCreditOpeningBalanceId id,
        PlatformOrganizationId organizationId,
        CreditCustomerId creditCustomerId,
        BusinessCustomerId businessCustomerId,
        decimal amount,
        string currencyCode,
        DateTimeOffset effectiveDateUtc,
        PersonalUtangMigrationSourceType sourceType,
        Guid sourceRecordId,
        PersonalUtangMigrationBatchId migrationBatchId,
        PlatformUserId importedByUserId,
        DateTimeOffset importedAtUtc,
        string destinationProduct) =>
        new(
            id,
            organizationId,
            creditCustomerId,
            businessCustomerId,
            amount,
            currencyCode,
            effectiveDateUtc,
            sourceType,
            sourceRecordId,
            migrationBatchId,
            importedByUserId,
            importedAtUtc,
            destinationProduct);

    private static string NormalizeCurrency(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Trim().Length != 3)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCreditCustomerCurrency,
                "Currency code must be a 3-letter ISO code.");
        }

        return currencyCode.Trim().ToUpperInvariant();
    }

    private static string NormalizeProduct(string productCode)
    {
        if (string.IsNullOrWhiteSpace(productCode))
        {
            throw new DomainException(DomainErrorCodes.InvalidProductCode, "Destination product is required.");
        }

        return productCode.Trim().ToLowerInvariant();
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamps must be UTC.");
        }
    }
}
