using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.UnitTests.Offline;

public sealed class CatalogProductOfflineCreateDeniedTests
{
    [Fact]
    public async Task PNAME_OFF_01_02_QueueCreate_never_enqueues_canonical_product()
    {
        var service = new CatalogProductOfflineSyncService(
            queue: null!,
            pendingImages: null!,
            currentUser: null!);

        var result = await service.QueueCreateAsync(
            new CreatePosCatalogProductRequest("Coke 1L", "Piece", 50m),
            pendingImage: null);

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogProductOfflineSyncService.OnlineRequiredErrorCode, result.ErrorCode);
        Assert.Contains("internet", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }
}
