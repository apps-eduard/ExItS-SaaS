using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>Where an effective stop-selling rule came from.</summary>
public enum ExpirySalePolicySource
{
    OrganizationDefault = 0,
    OrganizationCategory = 1,
    Branch = 2,
    BranchCategory = 3,
}

/// <summary>Organization-default stop-selling days before expiry for normal POS sales.</summary>
public sealed class OrganizationExpirySalePolicySetting
{
    public PosOrganizationId OrganizationId { get; }
    public int StopSellingDaysBeforeExpiry { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid UpdatedBy { get; private set; }

    private OrganizationExpirySalePolicySetting(
        PosOrganizationId organizationId,
        int stopSellingDaysBeforeExpiry,
        DateTimeOffset updatedAtUtc,
        Guid updatedBy)
    {
        OrganizationId = organizationId;
        StopSellingDaysBeforeExpiry = stopSellingDaysBeforeExpiry;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedBy = updatedBy;
    }

    public static OrganizationExpirySalePolicySetting CreateDefault(
        PosOrganizationId organizationId,
        Guid actorId,
        DateTimeOffset utcNow) =>
        Create(organizationId, InventoryLotSaleEligibility.DefaultStopSellingDays, actorId, utcNow);

    public static OrganizationExpirySalePolicySetting Create(
        PosOrganizationId organizationId,
        int stopSellingDaysBeforeExpiry,
        Guid actorId,
        DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        return new OrganizationExpirySalePolicySetting(
            organizationId,
            InventoryLotSaleEligibility.NormalizeStopSellingDays(stopSellingDaysBeforeExpiry),
            utcNow,
            actorId);
    }

    public static OrganizationExpirySalePolicySetting Rehydrate(
        PosOrganizationId organizationId,
        int stopSellingDaysBeforeExpiry,
        DateTimeOffset updatedAtUtc,
        Guid updatedBy) =>
        new(organizationId, stopSellingDaysBeforeExpiry, updatedAtUtc, updatedBy);

    public void SetStopSellingDays(int stopSellingDaysBeforeExpiry, Guid actorId, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        StopSellingDaysBeforeExpiry = InventoryLotSaleEligibility.NormalizeStopSellingDays(stopSellingDaysBeforeExpiry);
        UpdatedAtUtc = utcNow;
        UpdatedBy = actorId;
    }

    private static void EnsureActor(Guid actorId)
    {
        if (actorId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpirySalePolicyActor,
                "Actor id must be a non-empty GUID.");
        }
    }

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }
    }
}

/// <summary>Organization category override for stop-selling days.</summary>
public sealed class OrganizationCategoryExpirySalePolicy
{
    public PosOrganizationId OrganizationId { get; }
    public Guid CategoryId { get; }
    public int StopSellingDaysBeforeExpiry { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid UpdatedBy { get; private set; }

    private OrganizationCategoryExpirySalePolicy(
        PosOrganizationId organizationId,
        Guid categoryId,
        int stopSellingDaysBeforeExpiry,
        DateTimeOffset updatedAtUtc,
        Guid updatedBy)
    {
        OrganizationId = organizationId;
        CategoryId = categoryId;
        StopSellingDaysBeforeExpiry = stopSellingDaysBeforeExpiry;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedBy = updatedBy;
    }

    public static OrganizationCategoryExpirySalePolicy Create(
        PosOrganizationId organizationId,
        Guid categoryId,
        int stopSellingDaysBeforeExpiry,
        Guid actorId,
        DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        EnsureCategory(categoryId);
        return new OrganizationCategoryExpirySalePolicy(
            organizationId,
            categoryId,
            InventoryLotSaleEligibility.NormalizeStopSellingDays(stopSellingDaysBeforeExpiry),
            utcNow,
            actorId);
    }

    public static OrganizationCategoryExpirySalePolicy Rehydrate(
        PosOrganizationId organizationId,
        Guid categoryId,
        int stopSellingDaysBeforeExpiry,
        DateTimeOffset updatedAtUtc,
        Guid updatedBy) =>
        new(organizationId, categoryId, stopSellingDaysBeforeExpiry, updatedAtUtc, updatedBy);

    public void SetStopSellingDays(int stopSellingDaysBeforeExpiry, Guid actorId, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        StopSellingDaysBeforeExpiry = InventoryLotSaleEligibility.NormalizeStopSellingDays(stopSellingDaysBeforeExpiry);
        UpdatedAtUtc = utcNow;
        UpdatedBy = actorId;
    }

    private static void EnsureCategory(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpirySalePolicyCategory,
                "Category id must be a non-empty GUID.");
        }
    }

    private static void EnsureActor(Guid actorId)
    {
        if (actorId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpirySalePolicyActor,
                "Actor id must be a non-empty GUID.");
        }
    }

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }
    }
}

/// <summary>Branch-default override for stop-selling days (beats organization category).</summary>
public sealed class BranchExpirySalePolicySetting
{
    public PosOrganizationId OrganizationId { get; }
    public PosBranchId BranchId { get; }
    public int StopSellingDaysBeforeExpiry { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid UpdatedBy { get; private set; }

    private BranchExpirySalePolicySetting(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        int stopSellingDaysBeforeExpiry,
        DateTimeOffset updatedAtUtc,
        Guid updatedBy)
    {
        OrganizationId = organizationId;
        BranchId = branchId;
        StopSellingDaysBeforeExpiry = stopSellingDaysBeforeExpiry;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedBy = updatedBy;
    }

    public static BranchExpirySalePolicySetting Create(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        int stopSellingDaysBeforeExpiry,
        Guid actorId,
        DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        return new BranchExpirySalePolicySetting(
            organizationId,
            branchId,
            InventoryLotSaleEligibility.NormalizeStopSellingDays(stopSellingDaysBeforeExpiry),
            utcNow,
            actorId);
    }

    public static BranchExpirySalePolicySetting Rehydrate(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        int stopSellingDaysBeforeExpiry,
        DateTimeOffset updatedAtUtc,
        Guid updatedBy) =>
        new(organizationId, branchId, stopSellingDaysBeforeExpiry, updatedAtUtc, updatedBy);

    public void SetStopSellingDays(int stopSellingDaysBeforeExpiry, Guid actorId, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        StopSellingDaysBeforeExpiry = InventoryLotSaleEligibility.NormalizeStopSellingDays(stopSellingDaysBeforeExpiry);
        UpdatedAtUtc = utcNow;
        UpdatedBy = actorId;
    }

    private static void EnsureActor(Guid actorId)
    {
        if (actorId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpirySalePolicyActor,
                "Actor id must be a non-empty GUID.");
        }
    }

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }
    }
}

/// <summary>Most-specific override: branch + category stop-selling days.</summary>
public sealed class BranchCategoryExpirySalePolicy
{
    public PosOrganizationId OrganizationId { get; }
    public PosBranchId BranchId { get; }
    public Guid CategoryId { get; }
    public int StopSellingDaysBeforeExpiry { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid UpdatedBy { get; private set; }

    private BranchCategoryExpirySalePolicy(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        Guid categoryId,
        int stopSellingDaysBeforeExpiry,
        DateTimeOffset updatedAtUtc,
        Guid updatedBy)
    {
        OrganizationId = organizationId;
        BranchId = branchId;
        CategoryId = categoryId;
        StopSellingDaysBeforeExpiry = stopSellingDaysBeforeExpiry;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedBy = updatedBy;
    }

    public static BranchCategoryExpirySalePolicy Create(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        Guid categoryId,
        int stopSellingDaysBeforeExpiry,
        Guid actorId,
        DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        EnsureCategory(categoryId);
        return new BranchCategoryExpirySalePolicy(
            organizationId,
            branchId,
            categoryId,
            InventoryLotSaleEligibility.NormalizeStopSellingDays(stopSellingDaysBeforeExpiry),
            utcNow,
            actorId);
    }

    public static BranchCategoryExpirySalePolicy Rehydrate(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        Guid categoryId,
        int stopSellingDaysBeforeExpiry,
        DateTimeOffset updatedAtUtc,
        Guid updatedBy) =>
        new(organizationId, branchId, categoryId, stopSellingDaysBeforeExpiry, updatedAtUtc, updatedBy);

    public void SetStopSellingDays(int stopSellingDaysBeforeExpiry, Guid actorId, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        StopSellingDaysBeforeExpiry = InventoryLotSaleEligibility.NormalizeStopSellingDays(stopSellingDaysBeforeExpiry);
        UpdatedAtUtc = utcNow;
        UpdatedBy = actorId;
    }

    private static void EnsureCategory(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpirySalePolicyCategory,
                "Category id must be a non-empty GUID.");
        }
    }

    private static void EnsureActor(Guid actorId)
    {
        if (actorId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpirySalePolicyActor,
                "Actor id must be a non-empty GUID.");
        }
    }

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }
    }
}
