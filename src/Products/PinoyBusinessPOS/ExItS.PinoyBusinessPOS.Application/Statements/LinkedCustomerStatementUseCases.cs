using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Statements;

public static class LinkedCustomerStatementLimits
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 20;

    public static int NormalizePageSize(int? pageSize) =>
        Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);

    public static int NormalizePage(int? page) => Math.Max(page ?? 1, 1);
}

/// <summary>
/// Lightweight Personal-facing linked Business Utang statement summary (no activity lines).
/// </summary>
public sealed record LinkedCustomerStatementSummaryDto(
    Guid OrganizationId,
    Guid PlatformBusinessCustomerId,
    Guid PosCustomerId,
    Guid LinkedCustomerAppUserId,
    string? MerchantDisplayName,
    string CustomerDisplayName,
    decimal OutstandingBalance,
    string Currency,
    DateTimeOffset AsOfUtc);

public sealed record LinkedCustomerActivityItemDto(
    Guid ActivityId,
    DateTimeOffset OccurredAtUtc,
    string Type,
    string ReferenceNumber,
    decimal? ChargeAmount,
    decimal? PaymentAmount,
    decimal? AdjustmentAmount,
    decimal? BalanceAfter,
    string Status,
    bool HasDetails,
    /// <summary>
    /// Present when the activity row is backed by a customer-owned sale. Clients use this
    /// for one-shot lazy receipt detail; never expand lines into the activity payload.
    /// </summary>
    Guid? SourceSaleId);

public sealed record LinkedCustomerRecentActivityPageDto(
    Guid OrganizationId,
    Guid PlatformBusinessCustomerId,
    Guid PosCustomerId,
    IReadOnlyList<LinkedCustomerActivityItemDto> Items,
    int Page,
    int PageSize,
    bool HasMore);

/// <summary>
/// Server-side limited recent ledger rows for linked-customer activity (credits ∪ repayments).
/// Must apply ORDER BY + LIMIT/OFFSET in the database — never load full history then trim.
/// </summary>
public interface ILinkedCustomerRecentActivityQuery
{
    Task<IReadOnlyList<LinkedCustomerActivityRawRow>> ListRecentDescendingAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}

public sealed record LinkedCustomerActivityRawRow(
    Guid EntryId,
    string EntryType,
    decimal Amount,
    decimal SignedEffect,
    string Status,
    DateTimeOffset RecordedAtUtc,
    Guid? SourceSaleId);

public sealed class GetLinkedCustomerStatementSummary
{
    private readonly AuthorizeLinkedCustomerStatementAccess _authorize;
    private readonly IPOSCustomerRepository _customers;
    private readonly IOutstandingBalanceService _outstanding;
    private readonly IClock _clock;

    public GetLinkedCustomerStatementSummary(
        AuthorizeLinkedCustomerStatementAccess authorize,
        IPOSCustomerRepository customers,
        IOutstandingBalanceService outstanding,
        IClock clock)
    {
        _authorize = authorize;
        _customers = customers;
        _outstanding = outstanding;
        _clock = clock;
    }

    public async Task<ApplicationResult<LinkedCustomerStatementSummaryDto>> ExecuteAsync(
        Guid organizationId,
        Guid platformBusinessCustomerId,
        string currencyCode = "PHP",
        CancellationToken cancellationToken = default)
    {
        var authz = await _authorize
            .ExecuteAsync(organizationId, platformBusinessCustomerId, posCustomerId: null, cancellationToken)
            .ConfigureAwait(false);
        if (!authz.IsSuccess)
        {
            return ApplicationResult<LinkedCustomerStatementSummaryDto>.Failure(
                authz.ErrorCode!,
                authz.ErrorMessage!);
        }

        var ctx = authz.Value!;
        var orgId = PosOrganizationId.From(ctx.OrganizationId);
        var posCustomerId = POSCustomerId.From(ctx.PosCustomerId);
        var customer = await _customers.GetByIdAsync(orgId, posCustomerId, cancellationToken).ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<LinkedCustomerStatementSummaryDto>.Failure(
                ApplicationErrorCodes.LinkedCustomerNotFound,
                "Linked customer was not found.");
        }

        var outstanding = await _outstanding
            .GetOutstandingAsync(orgId, posCustomerId, cancellationToken)
            .ConfigureAwait(false);

        return ApplicationResult<LinkedCustomerStatementSummaryDto>.Success(
            new LinkedCustomerStatementSummaryDto(
                ctx.OrganizationId,
                ctx.PlatformBusinessCustomerId,
                ctx.PosCustomerId,
                ctx.LinkedCustomerAppUserId,
                MerchantDisplayName: null,
                customer.DisplayName,
                outstanding,
                string.IsNullOrWhiteSpace(currencyCode) ? "PHP" : currencyCode.Trim().ToUpperInvariant(),
                _clock.UtcNow));
    }
}

public sealed class ListLinkedCustomerRecentActivity
{
    private readonly AuthorizeLinkedCustomerStatementAccess _authorize;
    private readonly ILinkedCustomerRecentActivityQuery _activity;
    private readonly IOutstandingBalanceService _outstanding;

    public ListLinkedCustomerRecentActivity(
        AuthorizeLinkedCustomerStatementAccess authorize,
        ILinkedCustomerRecentActivityQuery activity,
        IOutstandingBalanceService outstanding)
    {
        _authorize = authorize;
        _activity = activity;
        _outstanding = outstanding;
    }

    public async Task<ApplicationResult<LinkedCustomerRecentActivityPageDto>> ExecuteAsync(
        Guid organizationId,
        Guid platformBusinessCustomerId,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var authz = await _authorize
            .ExecuteAsync(organizationId, platformBusinessCustomerId, posCustomerId: null, cancellationToken)
            .ConfigureAwait(false);
        if (!authz.IsSuccess)
        {
            return ApplicationResult<LinkedCustomerRecentActivityPageDto>.Failure(
                authz.ErrorCode!,
                authz.ErrorMessage!);
        }

        var ctx = authz.Value!;
        var orgId = PosOrganizationId.From(ctx.OrganizationId);
        var posCustomerId = POSCustomerId.From(ctx.PosCustomerId);
        var normalizedPage = LinkedCustomerStatementLimits.NormalizePage(page);
        var normalizedPageSize = LinkedCustomerStatementLimits.NormalizePageSize(pageSize);
        var skip = (normalizedPage - 1) * normalizedPageSize;

        // Fetch one extra row to compute HasMore without a separate COUNT(*).
        var rows = await _activity
            .ListRecentDescendingAsync(orgId, posCustomerId, skip, normalizedPageSize + 1, cancellationToken)
            .ConfigureAwait(false);
        var hasMore = rows.Count > normalizedPageSize;
        var pageRows = hasMore ? rows.Take(normalizedPageSize).ToList() : rows.ToList();

        decimal? runningAfter = null;
        if (pageRows.Count > 0)
        {
            // Balance after the newest entry on this page equals current outstanding when page=1.
            // For later pages, reconstruct by subtracting signed effects of all newer rows
            // (skip count) would require more data; for WP04 we only attach BalanceAfter on page 1
            // (newest slice) where outstanding is the balance after the newest entry.
            if (normalizedPage == 1)
            {
                runningAfter = await _outstanding
                    .GetOutstandingAsync(orgId, posCustomerId, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        var items = new List<LinkedCustomerActivityItemDto>(pageRows.Count);
        foreach (var row in pageRows)
        {
            decimal? balanceAfter = null;
            if (runningAfter is decimal bal)
            {
                balanceAfter = bal;
                runningAfter = bal - row.SignedEffect;
            }

            items.Add(Map(row, balanceAfter));
        }

        return ApplicationResult<LinkedCustomerRecentActivityPageDto>.Success(
            new LinkedCustomerRecentActivityPageDto(
                ctx.OrganizationId,
                ctx.PlatformBusinessCustomerId,
                ctx.PosCustomerId,
                items,
                normalizedPage,
                normalizedPageSize,
                hasMore));
    }

    private static LinkedCustomerActivityItemDto Map(LinkedCustomerActivityRawRow row, decimal? balanceAfter)
    {
        var isCredit = string.Equals(row.EntryType, "Credit", StringComparison.OrdinalIgnoreCase);
        var isRepayment = string.Equals(row.EntryType, "Repayment", StringComparison.OrdinalIgnoreCase);
        var isReversed = string.Equals(row.Status, "Reversed", StringComparison.OrdinalIgnoreCase);

        string type;
        decimal? charge = null;
        decimal? payment = null;
        decimal? adjustment = null;

        if (isCredit)
        {
            charge = row.Amount;
            type = isReversed ? "UtangChargeReversal" : "UtangCharge";
            if (isReversed)
            {
                adjustment = -row.Amount;
            }
        }
        else if (isRepayment)
        {
            payment = row.Amount;
            if (isReversed)
            {
                type = "PaymentReversal";
                adjustment = row.Amount;
            }
            else if (balanceAfter is > 0m)
            {
                type = "PartialPayment";
            }
            else
            {
                type = "Payment";
            }
        }
        else
        {
            type = "Adjustment";
            adjustment = row.SignedEffect;
        }

        var reference = isRepayment
            ? BuildRepaymentReference(row.EntryId)
            : row.SourceSaleId is Guid saleId
                ? saleId.ToString("N")[..8].ToUpperInvariant()
                : row.EntryId.ToString("N")[..8].ToUpperInvariant();

        // HasDetails is true only when a sale receipt can be opened (WP05). Repayments stay
        // as activity rows; they do not preload or imply product-line receipt detail.
        var hasDetails = row.SourceSaleId is not null;

        return new LinkedCustomerActivityItemDto(
            row.EntryId,
            row.RecordedAtUtc,
            type,
            reference,
            charge,
            payment,
            adjustment,
            balanceAfter,
            row.Status,
            hasDetails,
            row.SourceSaleId);
    }

    private static string BuildRepaymentReference(Guid repaymentId) =>
        $"RCPT-{repaymentId.ToString("N")[..12].ToUpperInvariant()}";
}
