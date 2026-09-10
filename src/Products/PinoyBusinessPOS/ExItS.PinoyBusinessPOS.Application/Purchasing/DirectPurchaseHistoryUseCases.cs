using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Application.Purchasing;

public sealed class DirectPurchaseHistoryQueryService
{
    private readonly IDirectPurchaseHistoryQuery _query;

    public DirectPurchaseHistoryQueryService(IDirectPurchaseHistoryQuery query) => _query = query;

    public async Task<ApplicationResult<PagedResult<DirectPurchaseHistoryItemDto>>> ListAsync(
        Guid buyerOrganizationId,
        DirectPurchaseHistoryFilter filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        if (buyerOrganizationId == Guid.Empty)
        {
            return ApplicationResult<PagedResult<DirectPurchaseHistoryItemDto>>.Failure(
                ApplicationErrorCodes.OrganizationRequired,
                "Organization is required.");
        }

        if (!DirectPurchaseHistorySourceTypes.TryNormalize(filter.SourceType, out var sourceType))
        {
            return ApplicationResult<PagedResult<DirectPurchaseHistoryItemDto>>.Failure(
                ApplicationErrorCodes.DomainViolation,
                "Invalid sourceType. Use All, Local, or B2B.");
        }

        if (!DirectPurchaseHistoryStatuses.TryNormalize(filter.Status, out var status))
        {
            return ApplicationResult<PagedResult<DirectPurchaseHistoryItemDto>>.Failure(
                ApplicationErrorCodes.DomainViolation,
                "Invalid status. Use All, Completed, Voided, or AwaitingPayment.");
        }

        var normalized = filter with { SourceType = sourceType, Status = status };
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _query
            .ListAsync(buyerOrganizationId, normalized, skip, take, cancellationToken)
            .ConfigureAwait(false);

        var mapped = items
            .Select(r => new DirectPurchaseHistoryItemDto(
                r.SourceId,
                r.SourceType,
                r.OccurredAtUtc,
                r.PurchaseDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                string.IsNullOrWhiteSpace(r.SellerDisplayName) ? "Seller" : r.SellerDisplayName,
                r.SellerOrganizationId,
                r.SellerPublicOrganizationId,
                r.ReferenceNumber,
                r.LineCount,
                r.TotalAmount,
                r.Status,
                r.PaymentMethod,
                r.SellerStoreDisplayName))
            .ToList();

        return ApplicationResult<PagedResult<DirectPurchaseHistoryItemDto>>.Success(
            new PagedResult<DirectPurchaseHistoryItemDto>(
                mapped,
                total,
                Math.Max(page ?? 1, 1),
                take));
    }

    public async Task<ApplicationResult<DirectPurchaseB2bDetailDto>> GetB2bDetailAsync(
        Guid buyerOrganizationId,
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        if (buyerOrganizationId == Guid.Empty || saleId == Guid.Empty)
        {
            return NotFound();
        }

        var detail = await _query
            .GetB2bDetailAsync(buyerOrganizationId, saleId, cancellationToken)
            .ConfigureAwait(false);

        return detail is null
            ? NotFound()
            : ApplicationResult<DirectPurchaseB2bDetailDto>.Success(detail);
    }

    private static ApplicationResult<DirectPurchaseB2bDetailDto> NotFound() =>
        ApplicationResult<DirectPurchaseB2bDetailDto>.Failure(
            ApplicationErrorCodes.SaleNotFound,
            "Direct purchase was not found.");
}
