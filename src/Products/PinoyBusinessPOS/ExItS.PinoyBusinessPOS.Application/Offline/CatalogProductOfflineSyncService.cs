using System.Text.Json;
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

public sealed class CatalogProductOfflineSyncService(
    IOfflineOperationQueue queue,
    PendingProductImageStore pendingImages,
    ICurrentUserContext currentUser,
    IPosSyncStatusService? syncStatus = null) : ICatalogProductOfflineSyncService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public async Task<ApplicationResult<Guid>> QueueCreateAsync(
        CreatePosCatalogProductRequest request,
        byte[]? pendingImage,
        CancellationToken cancellationToken = default)
    {
        var organizationId = currentUser.Session?.OrganizationId;
        if (organizationId is null)
        {
            return ApplicationResult<Guid>.Failure(
                ApplicationErrorCodes.OrganizationRequired,
                "An organization is required.");
        }

        var productId = request.ProductId ?? Guid.NewGuid();
        var payload = request with { ProductId = productId };
        if (pendingImage is { Length: > 0 })
        {
            await pendingImages.SaveAsync(organizationId.Value, productId, pendingImage, cancellationToken)
                .ConfigureAwait(false);
        }

        var operationId = Guid.NewGuid();
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        try
        {
            await queue.EnqueueAsync(
                    new OfflineEnqueueRequest(
                        operationId,
                        OfflineOperationTypes.CatalogProductCreate,
                        PayloadVersion: 1,
                        IdempotencyKey: productId.ToString("N"),
                        plaintext,
                        EntityId: productId),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            pendingImages.Delete(organizationId.Value, productId);
            return ApplicationResult<Guid>.Failure(
                "offline_mutations_unavailable",
                "Reconnect to verify access.");
        }
        syncStatus?.Refresh();
        return ApplicationResult<Guid>.Success(productId);
    }
}
