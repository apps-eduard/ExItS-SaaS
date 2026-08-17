using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.UnitTests.GlobalCatalog;

internal sealed class EmptyGlobalProductImageRepository : IGlobalProductImageRepository
{
    public Task<GlobalProductImage?> GetByProductIdAsync(
        GlobalProductId productId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<GlobalProductImage?>(null);

    public Task<IReadOnlyList<GlobalProductImage>> ListByProductIdsAsync(
        IReadOnlyList<GlobalProductId> productIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<GlobalProductImage>>([]);

    public Task AddAsync(GlobalProductImage image, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task UpdateAsync(GlobalProductImage image, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task DeleteAsync(GlobalProductImage image, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
