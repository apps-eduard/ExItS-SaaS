using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.LocalValidation;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.LocalValidation;

public sealed class EnsurePspLocalValidationCatalogTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Reference_registration_adds_pinoy_service_pro_once()
    {
        var clock = new FixedClock(T0);
        var products = new InMemoryProductRepository();
        var pos = Product.Create(ProductCode.Create(ProductCode.PinoyBusinessPos), "Pinoy Business POS", T0);
        await products.AddAsync(pos);
        var ensure = new EnsurePspLocalValidationCatalog(
            new CreateProduct(products, new NoOpUnitOfWork(), clock),
            products,
            new NoOpUnitOfWork(),
            clock);

        await ensure.EnsureReferenceAsync();
        await ensure.EnsureReferenceAsync();

        var psp = await products.GetByCodeAsync(ProductCode.Create(ProductCode.PinoyServicePro));
        var posAgain = await products.GetByCodeAsync(ProductCode.Create(ProductCode.PinoyBusinessPos));
        Assert.NotNull(psp);
        Assert.Equal(EnsurePspLocalValidationCatalog.ProductDisplayName, psp!.DisplayName);
        Assert.Equal(ProductStatus.Active, psp.Status);
        Assert.NotNull(posAgain);
        Assert.NotEqual(psp.Id, posAgain!.Id);
        Assert.Equal(2, products.AddCount);
    }
}
