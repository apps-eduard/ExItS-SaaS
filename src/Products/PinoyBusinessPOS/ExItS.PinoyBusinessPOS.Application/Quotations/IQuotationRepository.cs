using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Quotations;

namespace ExItS.PinoyBusinessPOS.Application.Quotations;

public sealed record QuotationFilter(
    QuotationStatus? Status = null,
    Guid? CustomerId = null,
    Guid? BranchId = null,
    string? QuotationNumber = null,
    DateOnly? FromIssuedDate = null,
    DateOnly? ToIssuedDate = null);

public interface IQuotationRepository
{
    Task<Quotation?> GetByIdAsync(
        PosOrganizationId organizationId,
        QuotationId quotationId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Quotation> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        QuotationFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(Quotation quotation, CancellationToken cancellationToken = default);

    Task UpdateAsync(Quotation quotation, CancellationToken cancellationToken = default);

    Task<Quotation> IssueAsync(
        PosOrganizationId organizationId,
        QuotationId quotationId,
        DateOnly businessDateUtc,
        Func<string, Quotation> applyIssue,
        CancellationToken cancellationToken = default);
}
