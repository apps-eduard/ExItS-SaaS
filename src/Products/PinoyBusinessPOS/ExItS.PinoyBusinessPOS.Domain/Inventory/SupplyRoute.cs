using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public sealed class SupplyRoute
{
    public const int NotesMaxLength = 512;

    public SupplyRouteId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public PosBranchId SourceLocationId { get; }
    public PosBranchId DestinationLocationId { get; }
    public bool IsPreferred { get; private set; }
    public bool IsActive { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private SupplyRoute(
        SupplyRouteId id,
        PosOrganizationId organizationId,
        PosBranchId sourceLocationId,
        PosBranchId destinationLocationId,
        bool isPreferred,
        bool isActive,
        string? notes,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        SourceLocationId = sourceLocationId;
        DestinationLocationId = destinationLocationId;
        IsPreferred = isPreferred;
        IsActive = isActive;
        Notes = notes;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static SupplyRoute Create(
        PosOrganizationId organizationId,
        PosBranchId sourceLocationId,
        PosBranchId destinationLocationId,
        DateTimeOffset utcNow,
        bool isPreferred = false,
        bool isActive = true,
        string? notes = null,
        SupplyRouteId? id = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureDistinctLocations(sourceLocationId, destinationLocationId);

        return new SupplyRoute(
            id ?? SupplyRouteId.New(),
            organizationId,
            sourceLocationId,
            destinationLocationId,
            isPreferred,
            isActive,
            NormalizeNotes(notes),
            utcNow,
            utcNow);
    }

    public void SetPreferred(bool preferred, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        IsPreferred = preferred;
        UpdatedAtUtc = utcNow;
    }

    public void Activate(DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        IsActive = true;
        UpdatedAtUtc = utcNow;
    }

    public void Deactivate(DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        IsActive = false;
        IsPreferred = false;
        UpdatedAtUtc = utcNow;
    }

    public void UpdateNotes(string? notes, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        Notes = NormalizeNotes(notes);
        UpdatedAtUtc = utcNow;
    }

    public static SupplyRoute Rehydrate(
        SupplyRouteId id,
        PosOrganizationId organizationId,
        PosBranchId sourceLocationId,
        PosBranchId destinationLocationId,
        bool isPreferred,
        bool isActive,
        string? notes,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            id,
            organizationId,
            sourceLocationId,
            destinationLocationId,
            isPreferred,
            isActive,
            notes,
            createdAtUtc,
            updatedAtUtc);

    private static void EnsureDistinctLocations(PosBranchId source, PosBranchId destination)
    {
        if (source == destination)
        {
            throw new DomainException(
                DomainErrorCodes.SupplyRouteSameLocation,
                "Supply route source and destination must be different.");
        }
    }

    private static string? NormalizeNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var trimmed = notes.Trim();
        if (trimmed.Length > NotesMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSupplyRouteNotes,
                $"Supply route notes must be at most {NotesMaxLength} characters.");
        }

        return trimmed;
    }
}
