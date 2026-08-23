using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Payments;

public sealed record SaaSPaymentDto(
    Guid Id,
    Guid OrganizationId,
    string ProductCode,
    Guid? SubscriptionId,
    decimal Amount,
    string CurrencyCode,
    string Method,
    string ExternalReference,
    string Status,
    DateTimeOffset PaidAtUtc,
    DateTimeOffset? ConfirmedAtUtc,
    string? ConfirmedBy,
    DateTimeOffset? RejectedAtUtc,
    string? RejectedBy,
    string? RejectionReason,
    DateTimeOffset? VoidedAtUtc,
    string? VoidedBy,
    string? VoidReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int Version);

public sealed class SaaSPaymentQueryService
{
    private readonly ISaaSPaymentRepository _payments;

    public SaaSPaymentQueryService(ISaaSPaymentRepository payments)
    {
        _payments = payments;
    }

    public async Task<SaaSPaymentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var payment = await _payments
            .GetByIdAsync(SaaSPaymentId.From(id), cancellationToken)
            .ConfigureAwait(false);

        return payment is null ? null : Map(payment);
    }

    public async Task<SaaSPaymentDto?> SearchByReferenceAsync(
        SaaSPaymentMethod method,
        string reference,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var normalized = SaaSPayment.NormalizeReference(reference);
        var payment = await _payments
            .GetByNormalizedReferenceAsync(method, normalized, PlatformOrganizationId.From(organizationId), cancellationToken)
            .ConfigureAwait(false);

        return payment is null ? null : Map(payment);
    }

    public async Task<IReadOnlyList<SaaSPaymentDto>> FindByNormalizedReferenceAsync(
        string reference,
        SaaSPaymentMethod? method,
        CancellationToken cancellationToken = default)
    {
        var normalized = SaaSPayment.NormalizeReference(reference);
        var payments = await _payments
            .FindByNormalizedReferenceAsync(normalized, method, cancellationToken)
            .ConfigureAwait(false);
        return payments.Select(Map).ToList();
    }

    public async Task<PagedResult<SaaSPaymentDto>> ListByOrganizationAsync(
        Guid organizationId,
        SaaSPaymentStatus? status,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, totalCount) = await _payments
            .ListByOrganizationAsync(PlatformOrganizationId.From(organizationId), status, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return ToPagedResult(items, totalCount, page, take);
    }

    public async Task<PagedResult<SaaSPaymentDto>> ListByProductAsync(
        string productCode,
        SaaSPaymentStatus? status,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, totalCount) = await _payments
            .ListByProductAsync(ProductCode.Create(productCode), status, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return ToPagedResult(items, totalCount, page, take);
    }

    public async Task<PagedResult<SaaSPaymentDto>> ListByStatusAsync(
        SaaSPaymentStatus status,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, totalCount) = await _payments
            .ListByStatusAsync(status, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return ToPagedResult(items, totalCount, page, take);
    }

    public async Task<PagedResult<SaaSPaymentDto>> ListBySubscriptionAsync(
        Guid subscriptionId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, totalCount) = await _payments
            .ListBySubscriptionAsync(SubscriptionId.From(subscriptionId), skip, take, cancellationToken)
            .ConfigureAwait(false);

        return ToPagedResult(items, totalCount, page, take);
    }

    private static PagedResult<SaaSPaymentDto> ToPagedResult(
        IReadOnlyList<SaaSPayment> items,
        int totalCount,
        int? page,
        int take)
    {
        var pageNumber = Math.Max(page ?? 1, 1);
        return new PagedResult<SaaSPaymentDto>(items.Select(Map).ToList(), totalCount, pageNumber, take);
    }

    private static SaaSPaymentDto Map(SaaSPayment payment) =>
        new(
            payment.Id.Value,
            payment.OrganizationId.Value,
            payment.ProductCode.Value,
            payment.SubscriptionId?.Value,
            payment.Amount,
            payment.CurrencyCode.Value,
            payment.Method.ToString(),
            payment.ExternalReference,
            payment.Status.ToString(),
            payment.PaidAtUtc,
            payment.ConfirmedAtUtc,
            payment.ConfirmedBy,
            payment.RejectedAtUtc,
            payment.RejectedBy,
            payment.RejectionReason,
            payment.VoidedAtUtc,
            payment.VoidedBy,
            payment.VoidReason,
            payment.CreatedAtUtc,
            payment.UpdatedAtUtc,
            payment.Version);
}
