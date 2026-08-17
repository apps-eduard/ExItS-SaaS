using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class PersonalMerchantCartTests
{
    private static readonly Guid OrgA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OrgB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Product1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Product2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void First_plus_creates_qty_one_and_totals_update()
    {
        var cart = new PersonalMerchantCart();
        cart.EnsureMerchant(OrgA, "Corner Store");
        cart.Increment(Product(Product1, "Rice", 50m));

        Assert.Equal(1m, cart.GetQuantity(Product1));
        Assert.Equal(1, cart.LineCount);
        Assert.Equal(50m, cart.MerchandiseSubtotal);
    }

    [Fact]
    public void Plus_minus_remove_and_zero_clears_line()
    {
        var cart = new PersonalMerchantCart();
        cart.EnsureMerchant(OrgA, "Corner Store");
        cart.Increment(Product(Product1, "Rice", 50m));
        cart.Increment(Product(Product1, "Rice", 50m));
        Assert.Equal(2m, cart.GetQuantity(Product1));
        Assert.Equal(100m, cart.MerchandiseSubtotal);

        cart.Decrement(Product1);
        Assert.Equal(1m, cart.GetQuantity(Product1));

        cart.Decrement(Product1);
        Assert.Equal(0m, cart.GetQuantity(Product1));
        Assert.Equal(0, cart.LineCount);

        cart.Increment(Product(Product2, "Oil", 20m));
        cart.Remove(Product2);
        Assert.Equal(0, cart.LineCount);
    }

    [Fact]
    public void Unavailable_product_cannot_be_added()
    {
        var cart = new PersonalMerchantCart();
        cart.EnsureMerchant(OrgA, "Corner Store");
        cart.Increment(Product(Product1, "Rice", 50m, available: false));
        Assert.Equal(0, cart.LineCount);
    }

    [Fact]
    public void Switching_merchant_clears_cart()
    {
        var cart = new PersonalMerchantCart();
        cart.EnsureMerchant(OrgA, "A");
        cart.Increment(Product(Product1, "Rice", 50m));
        cart.EnsureMerchant(OrgB, "B");
        Assert.Equal(0, cart.LineCount);
        Assert.Equal(OrgB, cart.SellerOrganizationId);
    }

    [Fact]
    public void Tracked_cart_stops_at_available_qty_untracked_has_no_max()
    {
        var cart = new PersonalMerchantCart();
        cart.EnsureMerchant(OrgA, "Corner Store");
        var tracked = new CustomerStorefrontProductDto(
            Product1, "Soft Drink", "SKU", "Bottle", null, 55m, true, true, 2m, CustomerStorefrontAvailability.LowStock);
        cart.Increment(tracked);
        cart.Increment(tracked);
        cart.Increment(tracked);
        Assert.Equal(2m, cart.GetQuantity(Product1));
        Assert.False(cart.CanIncrement(tracked));

        var untracked = new CustomerStorefrontProductDto(
            Product2, "Bread", "B", "Piece", null, 25m, true, false, null, CustomerStorefrontAvailability.Untracked);
        for (var i = 0; i < 6; i++)
        {
            cart.Increment(untracked);
        }

        Assert.Equal(6m, cart.GetQuantity(Product2));
        Assert.True(cart.CanIncrement(untracked));

        var oos = new CustomerStorefrontProductDto(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            "Biscuit",
            "C",
            "Pack",
            null,
            12m,
            false,
            true,
            0m,
            CustomerStorefrontAvailability.OutOfStock);
        cart.Increment(oos);
        Assert.Equal(0m, cart.GetQuantity(oos.ProductId));
        Assert.False(cart.CanIncrement(oos));
    }

    private static CustomerStorefrontProductDto Product(
        Guid id,
        string name,
        decimal price,
        bool available = true) =>
        new(id, name, "SKU", "Piece", null, price, available);
}
