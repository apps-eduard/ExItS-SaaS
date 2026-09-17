namespace ExItS.PinoyBusinessPOS.Application.Purchasing;

public interface IDirectPurchaseHistoryQuery
{
    Task<(IReadOnlyList<DirectPurchaseHistoryRawRow> Items, int TotalCount)> ListAsync(
        Guid buyerOrganizationId,
        DirectPurchaseHistoryFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<DirectPurchaseB2bDetailDto?> GetB2bDetailAsync(
        Guid buyerOrganizationId,
        Guid saleId,
        CancellationToken cancellationToken = default);
}
