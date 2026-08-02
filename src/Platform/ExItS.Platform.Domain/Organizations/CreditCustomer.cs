using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Organization-owned credit relationship for one Business Customer. Not staff; not a Platform User by default.
/// </summary>
public sealed class CreditCustomer
{
    public CreditCustomerId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public BusinessCustomerId BusinessCustomerId { get; }
    public string CurrencyCode { get; private set; }
    public CreditCustomerStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private CreditCustomer(
        CreditCustomerId id,
        PlatformOrganizationId organizationId,
        BusinessCustomerId businessCustomerId,
        string currencyCode,
        CreditCustomerStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        BusinessCustomerId = businessCustomerId;
        CurrencyCode = currencyCode;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static CreditCustomer Create(
        PlatformOrganizationId organizationId,
        BusinessCustomerId businessCustomerId,
        DateTimeOffset utcNow,
        string currencyCode = "PHP",
        CreditCustomerId? id = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(businessCustomerId);
        EnsureUtc(utcNow);

        return new CreditCustomer(
            id ?? CreditCustomerId.New(),
            organizationId,
            businessCustomerId,
            NormalizeCurrency(currencyCode),
            CreditCustomerStatus.Active,
            utcNow,
            utcNow);
    }

    public static CreditCustomer Rehydrate(
        CreditCustomerId id,
        PlatformOrganizationId organizationId,
        BusinessCustomerId businessCustomerId,
        string currencyCode,
        CreditCustomerStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(id, organizationId, businessCustomerId, currencyCode, status, createdAtUtc, updatedAtUtc);

    public void Close(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == CreditCustomerStatus.Closed)
        {
            return;
        }

        Status = CreditCustomerStatus.Closed;
        UpdatedAtUtc = utcNow;
    }

    public bool IsOrganizationStaff => false;

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

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamps must be UTC.");
        }
    }
}
