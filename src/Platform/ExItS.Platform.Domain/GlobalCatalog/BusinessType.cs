using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.GlobalCatalog;

public sealed class BusinessTypeId : IEquatable<BusinessTypeId>
{
    public Guid Value { get; }

    private BusinessTypeId(Guid value) => Value = value;

    public static BusinessTypeId New() => new(Guid.NewGuid());

    public static BusinessTypeId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalCatalogBusinessType,
                "BusinessTypeId cannot be an empty GUID.");
        }

        return new BusinessTypeId(value);
    }

    public bool Equals(BusinessTypeId? other) => other is not null && Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is BusinessTypeId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");
    public static bool operator ==(BusinessTypeId? left, BusinessTypeId? right) => Equals(left, right);
    public static bool operator !=(BusinessTypeId? left, BusinessTypeId? right) => !Equals(left, right);
}

public enum BusinessTypeStatus
{
    Active = 0,
    Inactive = 1,
    Archived = 2
}

/// <summary>
/// Platform-owned dynamic business-type classification (not a curated product template).
/// Soft lifecycle preferred — archive rather than hard-delete when referenced.
/// </summary>
public sealed class BusinessType
{
    public BusinessTypeId Id { get; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public BusinessTypeStatus Status { get; private set; }
    public int SortOrder { get; private set; }
    public string? IconReference { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private BusinessType(
        BusinessTypeId id,
        string code,
        string name,
        string? description,
        BusinessTypeStatus status,
        int sortOrder,
        string? iconReference,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        Code = code;
        Name = name;
        Description = description;
        Status = status;
        SortOrder = sortOrder;
        IconReference = iconReference;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static BusinessType Create(
        string code,
        string name,
        DateTimeOffset utcNow,
        string? description = null,
        int sortOrder = 0,
        string? iconReference = null,
        BusinessTypeId? id = null)
    {
        DomainTime.EnsureUtc(utcNow);
        return new BusinessType(
            id ?? BusinessTypeId.New(),
            GlobalCatalogRules.NormalizeBusinessTypeCode(code),
            GlobalCatalogRules.NormalizeName(name),
            GlobalCatalogRules.NormalizeOptionalText(
                description,
                GlobalCatalogRules.DescriptionMaxLength,
                DomainErrorCodes.InvalidGlobalProductDescription),
            BusinessTypeStatus.Active,
            sortOrder,
            GlobalCatalogRules.NormalizeOptionalText(
                iconReference,
                GlobalCatalogRules.IconReferenceMaxLength,
                DomainErrorCodes.InvalidGlobalCategoryIcon),
            utcNow,
            utcNow);
    }

    public static BusinessType Rehydrate(
        BusinessTypeId id,
        string code,
        string name,
        string? description,
        BusinessTypeStatus status,
        int sortOrder,
        string? iconReference,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(id, code, name, description, status, sortOrder, iconReference, createdAtUtc, updatedAtUtc);

    public void Rename(string name, DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);
        Name = GlobalCatalogRules.NormalizeName(name);
        UpdatedAtUtc = utcNow;
    }

    public void SetDescription(string? description, DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);
        Description = GlobalCatalogRules.NormalizeOptionalText(
            description,
            GlobalCatalogRules.DescriptionMaxLength,
            DomainErrorCodes.InvalidGlobalProductDescription);
        UpdatedAtUtc = utcNow;
    }

    public void SetSortOrder(int sortOrder, DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);
        SortOrder = sortOrder;
        UpdatedAtUtc = utcNow;
    }

    public void SetIcon(string? iconReference, DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);
        IconReference = GlobalCatalogRules.NormalizeOptionalText(
            iconReference,
            GlobalCatalogRules.IconReferenceMaxLength,
            DomainErrorCodes.InvalidGlobalCategoryIcon);
        UpdatedAtUtc = utcNow;
    }

    public void SetStatus(BusinessTypeStatus status, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == status)
        {
            return;
        }

        var allowed = Status switch
        {
            BusinessTypeStatus.Active => status is BusinessTypeStatus.Inactive or BusinessTypeStatus.Archived,
            BusinessTypeStatus.Inactive => status is BusinessTypeStatus.Active or BusinessTypeStatus.Archived,
            BusinessTypeStatus.Archived => status is BusinessTypeStatus.Active or BusinessTypeStatus.Inactive,
            _ => false
        };

        if (!allowed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalCatalogBusinessType,
                $"Cannot transition BusinessType from {Status} to {status}.");
        }

        Status = status;
        UpdatedAtUtc = utcNow;
    }

    private void EnsureMutable(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        // Archived types remain editable for reactivation/metadata; assignment uses status gates in application.
    }
}

/// <summary>Well-known legacy business-type seed ids/codes preserved from the former enum.</summary>
public static class LegacyBusinessTypeSeeds
{
    public static readonly Guid SariSariId = Guid.Parse("a1000001-0000-4000-8000-000000000001");
    public static readonly Guid MiniGroceryId = Guid.Parse("a1000001-0000-4000-8000-000000000002");
    public static readonly Guid BakeryId = Guid.Parse("a1000001-0000-4000-8000-000000000003");
    public static readonly Guid CafeId = Guid.Parse("a1000001-0000-4000-8000-000000000004");
    public static readonly Guid PharmacyId = Guid.Parse("a1000001-0000-4000-8000-000000000005");
    public static readonly Guid GeneralRetailId = Guid.Parse("a1000001-0000-4000-8000-000000000006");

    public const string SariSariCode = "SariSari";
    public const string MiniGroceryCode = "MiniGrocery";
    public const string BakeryCode = "Bakery";
    public const string CafeCode = "Cafe";
    public const string PharmacyCode = "Pharmacy";
    public const string GeneralRetailCode = "GeneralRetail";

    public static IReadOnlyList<(Guid Id, string Code, string Name, int SortOrder)> All { get; } =
    [
        (SariSariId, SariSariCode, "Sari-Sari Store", 10),
        (MiniGroceryId, MiniGroceryCode, "Mini Grocery", 20),
        (BakeryId, BakeryCode, "Bakery", 30),
        (CafeId, CafeCode, "Cafe / Coffee Shop", 40),
        (PharmacyId, PharmacyCode, "Pharmacy", 50),
        (GeneralRetailId, GeneralRetailCode, "General Retail / Other", 60)
    ];

    public static bool TryGetIdByCode(string code, out Guid id)
    {
        foreach (var row in All)
        {
            if (string.Equals(row.Code, code, StringComparison.OrdinalIgnoreCase))
            {
                id = row.Id;
                return true;
            }
        }

        id = Guid.Empty;
        return false;
    }
}
