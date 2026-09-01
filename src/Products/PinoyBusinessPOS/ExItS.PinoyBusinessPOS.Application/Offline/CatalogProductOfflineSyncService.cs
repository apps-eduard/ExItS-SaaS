using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

public interface ICatalogProductOfflineSyncService
{
    Task<ApplicationResult<Guid>> QueueCreateAsync(
        CreatePosCatalogProductRequest request,
        byte[]? pendingImage,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// MB2-01C-H1: canonical product create is ONLINE_REQUIRED. Do not enqueue offline ProductIds.
/// Offline product drafts are deferred; a draft is not CatalogProduct authority.
/// Constructor dependencies retained for existing DI registrations.
/// </summary>
public sealed class CatalogProductOfflineSyncService : ICatalogProductOfflineSyncService
{
    public const string OnlineRequiredErrorCode = "pos.catalog.product.create.online_required";

    public CatalogProductOfflineSyncService(
        IOfflineOperationQueue queue,
        PendingProductImageStore pendingImages,
        ICurrentUserContext currentUser,
        IPosSyncStatusService? syncStatus = null)
    {
        _ = queue;
        _ = pendingImages;
        _ = currentUser;
        _ = syncStatus;
    }

    public Task<ApplicationResult<Guid>> QueueCreateAsync(
        CreatePosCatalogProductRequest request,
        byte[]? pendingImage,
        CancellationToken cancellationToken = default)
    {
        _ = request;
        _ = pendingImage;
        _ = cancellationToken;
        return Task.FromResult(ApplicationResult<Guid>.Failure(
            OnlineRequiredErrorCode,
            "Creating a product requires an internet connection so we can check for duplicates across your organization."));
    }
}
