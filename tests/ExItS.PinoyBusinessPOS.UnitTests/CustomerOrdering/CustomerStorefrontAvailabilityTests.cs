using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.CustomerOrdering;

public sealed class CustomerStorefrontAvailabilityTests
{
    [Fact]
    public void Tracked_availability_excludes_reserved_and_maps_display_status()
    {
        var org = Domain.Customers.PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var product = Domain.Catalog.CatalogProductId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var now = DateTimeOffset.Parse("2026-08-17T00:00:00Z");
        var account = InventoryAccount.CreateUntracked(org, product, now);
        account.Enable(24m, Domain.Catalog.UnitOfMeasure.Piece, Guid.NewGuid(), now, false);
        var comfortable = CustomerStorefrontAvailability.FromAccount(account);
        Assert.Equal(CustomerStorefrontAvailability.InStock, comfortable.Status);
        Assert.Equal(24m, comfortable.AvailableQuantity);

        account.Reserve(21m);
        var low = CustomerStorefrontAvailability.FromAccount(account);
        Assert.Equal(CustomerStorefrontAvailability.LowStock, low.Status);
        Assert.Equal(3m, low.AvailableQuantity);

        account.Reserve(3m);
        var empty = CustomerStorefrontAvailability.FromAccount(account);
        Assert.Equal(CustomerStorefrontAvailability.OutOfStock, empty.Status);
        Assert.False(empty.IsAvailable);
    }

    [Fact]
    public void Untracked_and_missing_are_available_without_quantity()
    {
        var missing = CustomerStorefrontAvailability.FromAccount(null);
        Assert.Equal(CustomerStorefrontAvailability.Untracked, missing.Status);
        Assert.True(missing.IsAvailable);
        Assert.Null(missing.AvailableQuantity);

        var org = Domain.Customers.PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var product = Domain.Catalog.CatalogProductId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var untracked = InventoryAccount.CreateUntracked(org, product, DateTimeOffset.Parse("2026-08-17T00:00:00Z"));
        var snapshot = CustomerStorefrontAvailability.FromAccount(untracked);
        Assert.Equal(CustomerStorefrontAvailability.Untracked, snapshot.Status);
        Assert.True(snapshot.IsAvailable);
        Assert.Null(snapshot.AvailableQuantity);
    }
}
