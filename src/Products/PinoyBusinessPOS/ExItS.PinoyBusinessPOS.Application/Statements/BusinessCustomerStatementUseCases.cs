using System.Globalization;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Statements;

/// <summary>
/// Seller-facing statement over the B2B business credit ledger (Organization buyer).
/// Outstanding = active credits − settled repayments (pending checks do not settle).
/// </summary>
public sealed record BusinessCustomerStatementLineDto(
    Guid EntryId,
    string EntryType,
    DateTimeOffset RecordedAtUtc,
    decimal Amount,
    decimal SignedEffect,
    string Status,
    string? Remarks,
    DateOnly? DueDate,
    string? DueStatus,
    bool IsOverdue,
    bool IsReversed,
    decimal RunningBalance,
    Guid? SourceSaleId = null);

public sealed record BusinessCustomerStatementDto(
    Guid OrganizationId,
    string? OrganizationDisplayName,
    Guid ConnectionId,
    Guid BuyerOrganizationId,
    string CustomerDisplayName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal OpeningBalance,
    decimal ClosingBalance,
    decimal PeriodCreditTotal,
    decimal PeriodRepaymentTotal,
    decimal PeriodReversalCreditTotal,
    decimal PeriodReversalRepaymentTotal,
    decimal OutstandingBalance,
    decimal OverdueAmount,
    int OverdueCreditCount,
    DateTimeOffset GeneratedAtUtc,
    string CurrencyCode,
    string CultureName,
    IReadOnlyList<BusinessCustomerStatementLineDto> Lines);

public sealed class GetBusinessCustomerStatement
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IBusinessCreditEntryRepository _businessCredits;
    private readonly IBusinessRepaymentRepository _businessRepayments;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly IClock _clock;

    public GetBusinessCustomerStatement(
        IConnectedSupplierRelationshipRepository relationships,
        IBusinessCreditEntryRepository businessCredits,
        IBusinessRepaymentRepository businessRepayments,
        IPosCommercialAccessAccessor access,
        IClock clock)
    {
        _relationships = relationships;
        _businessCredits = businessCredits;
        _businessRepayments = businessRepayments;
        _access = access;
        _clock = clock;
    }

    public async Task<ApplicationResult<BusinessCustomerStatementDto>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid connectionId,
        DateOnly periodStart,
        DateOnly periodEnd,
        string? organizationDisplayName,
        string currencyCode,
        string cultureName,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.ViewGenerateStatement);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<BusinessCustomerStatementDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        if (periodEnd < periodStart)
        {
            return ApplicationResult<BusinessCustomerStatementDto>.Failure(
                ApplicationErrorCodes.StatementInvalidPeriod,
                "Statement period end must be on or after period start.");
        }

        var resolved = await BusinessCustomerCreditPolicyRelationshipGuard
            .ResolveForSellerAsync(_relationships, sellerOrganizationId, connectionId, cancellationToken)
            .ConfigureAwait(false);
        if (!resolved.IsSuccess)
        {
            return ApplicationResult<BusinessCustomerStatementDto>.Failure(
                resolved.ErrorCode!,
                resolved.ErrorMessage!);
        }

        var relationship = resolved.Value!;
        var seller = relationship.SupplierOrganizationId;
        var buyer = relationship.BuyerOrganizationId;

        var entries = await _businessCredits
            .ListChronologicalForBuyerAsync(seller, buyer, cancellationToken)
            .ConfigureAwait(false);

        var periodStartUtc = new DateTimeOffset(periodStart.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var periodEndExclusiveUtc = new DateTimeOffset(periodEnd.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var today = CreditFifoAging.EffectiveBusinessDateUtc(_clock.UtcNow);

        decimal opening = 0m;
        foreach (var entry in entries.Where(e => e.CreatedAtUtc < periodStartUtc))
        {
            opening += SignedEffect(entry);
        }

        var periodEntries = entries
            .Where(e => e.CreatedAtUtc >= periodStartUtc && e.CreatedAtUtc < periodEndExclusiveUtc)
            .ToList();

        decimal running = opening;
        decimal periodCredit = 0m;
        decimal periodRevCredit = 0m;
        var lines = new List<BusinessCustomerStatementLineDto>(periodEntries.Count);

        foreach (var entry in periodEntries)
        {
            var signed = SignedEffect(entry);
            running += signed;
            var isReversed = entry.Status == CreditEntryStatus.Reversed;
            if (isReversed)
            {
                periodRevCredit += entry.Amount;
            }
            else
            {
                periodCredit += entry.Amount;
            }

            var dueDate = entry.CurrentDueDate;
            var isOverdue = !isReversed
                && dueDate is DateOnly due
                && due < today;
            lines.Add(new BusinessCustomerStatementLineDto(
                entry.Id.Value,
                "Credit",
                entry.CreatedAtUtc,
                entry.Amount,
                signed,
                entry.Status.ToString(),
                string.IsNullOrWhiteSpace(entry.Remarks) ? null : entry.Remarks,
                dueDate,
                isOverdue ? "Overdue" : dueDate is null ? null : "Current",
                isOverdue,
                isReversed,
                running,
                entry.SourceSaleId?.Value));
        }

        var creditTotal = await _businessCredits
            .SumActiveAmountAsync(seller, buyer, cancellationToken)
            .ConfigureAwait(false);
        var settledRepayments = await _businessRepayments
            .SumSettledAmountAsync(seller, buyer, cancellationToken)
            .ConfigureAwait(false);
        var outstanding = creditTotal - settledRepayments;

        var closing = opening + periodEntries.Sum(SignedEffect);

        var overdueEntries = entries
            .Where(e => e.Status == CreditEntryStatus.Active
                        && e.CurrentDueDate is DateOnly due
                        && due < today)
            .ToList();
        var overdueAmount = overdueEntries.Sum(e => e.Amount);
        var overdueCount = overdueEntries.Count;

        var displayName = string.IsNullOrWhiteSpace(relationship.BuyerDisplayNameSnapshot)
            ? (relationship.BuyerPublicOrganizationIdSnapshot ?? "Business customer")
            : relationship.BuyerDisplayNameSnapshot!;

        return ApplicationResult<BusinessCustomerStatementDto>.Success(
            new BusinessCustomerStatementDto(
                seller.Value,
                organizationDisplayName,
                connectionId,
                buyer.Value,
                displayName,
                periodStart,
                periodEnd,
                opening,
                closing,
                periodCredit,
                0m,
                periodRevCredit,
                0m,
                outstanding,
                overdueAmount,
                overdueCount,
                _clock.UtcNow,
                string.IsNullOrWhiteSpace(currencyCode) ? "PHP" : currencyCode,
                string.IsNullOrWhiteSpace(cultureName) ? CultureInfo.CurrentCulture.Name : cultureName,
                lines));
    }

    private static decimal SignedEffect(BusinessCreditEntry entry) =>
        entry.Status == CreditEntryStatus.Active ? entry.Amount : 0m;
}
