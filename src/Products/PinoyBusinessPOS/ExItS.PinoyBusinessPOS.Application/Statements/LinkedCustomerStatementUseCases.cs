using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using Microsoft.Extensions.Options;

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
    bool HasMore,
    bool CanAccessExtendedHistory,
    DateTimeOffset FreeHistoryStartsAtUtc);

/// <summary>
/// Active credits + active repayments that explain current outstanding (open-debt exception path).
/// Separate from chronological free/entitled recent activity to avoid premium paging leaks.
/// </summary>
public sealed record LinkedCustomerOpenDebtActivityPageDto(
    Guid OrganizationId,
    Guid PlatformBusinessCustomerId,
    Guid PosCustomerId,
    decimal OutstandingBalance,
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
    /// <param name="notBeforeUtc">Inclusive lower bound (free-window floor).</param>
    /// <param name="beforeUtc">Exclusive upper bound (older-than-free-window settled history).</param>
    Task<IReadOnlyList<LinkedCustomerActivityRawRow>> ListRecentDescendingAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        int skip,
        int take,
        DateTimeOffset? notBeforeUtc = null,
        DateTimeOffset? beforeUtc = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Active ledger rows only (Status = Active), newest first — open-debt explanation.
    /// Callers must gate on outstanding &gt; 0; never use this to unlock settled-old history.
    /// </summary>
    Task<IReadOnlyList<LinkedCustomerActivityRawRow>> ListActiveDescendingAsync(
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
    private readonly IPersonalFeatureEntitlementClient _entitlements;
    private readonly IOptions<PersonalStatementsOptions> _options;
    private readonly IClock _clock;

    public ListLinkedCustomerRecentActivity(
        AuthorizeLinkedCustomerStatementAccess authorize,
        ILinkedCustomerRecentActivityQuery activity,
        IOutstandingBalanceService outstanding,
        IPersonalFeatureEntitlementClient entitlements,
        IOptions<PersonalStatementsOptions> options,
        IClock clock)
    {
        _authorize = authorize;
        _activity = activity;
        _outstanding = outstanding;
        _entitlements = entitlements;
        _options = options;
        _clock = clock;
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

        var asOfUtc = _clock.UtcNow;
        var freeStart = PersonalHistoryWindows.ComputeFreeWindowStart(
            asOfUtc,
            _options.Value.FreeRecentMonths);
        var canAccessExtended = await _entitlements
            .HasActiveEntitlementAsync(PersonalSettledHistoryPolicy.ExtendedFeatureCode, cancellationToken)
            .ConfigureAwait(false);

        // Free users: server-side date filter. Entitled users: no notBefore (still page-sized).
        // Older-than-window settled history for entitled clients also has an explicit /older-activity route (WP10).
        DateTimeOffset? notBefore = canAccessExtended ? null : freeStart;

        var rows = await _activity
            .ListRecentDescendingAsync(
                orgId,
                posCustomerId,
                skip,
                normalizedPageSize + 1,
                notBefore,
                beforeUtc: null,
                cancellationToken)
            .ConfigureAwait(false);
        var hasMore = rows.Count > normalizedPageSize;
        var pageRows = hasMore ? rows.Take(normalizedPageSize).ToList() : rows.ToList();

        decimal? runningAfter = null;
        if (pageRows.Count > 0 && normalizedPage == 1)
        {
            runningAfter = await _outstanding
                .GetOutstandingAsync(orgId, posCustomerId, cancellationToken)
                .ConfigureAwait(false);
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

            items.Add(LinkedCustomerActivityMapper.Map(row, balanceAfter));
        }

        return ApplicationResult<LinkedCustomerRecentActivityPageDto>.Success(
            new LinkedCustomerRecentActivityPageDto(
                ctx.OrganizationId,
                ctx.PlatformBusinessCustomerId,
                ctx.PosCustomerId,
                items,
                normalizedPage,
                normalizedPageSize,
                hasMore,
                canAccessExtended,
                freeStart));
    }
}

/// <summary>
/// Explicit older/settled history (WP10): rows strictly before the free-history window.
/// Requires active <c>personal-digital-records-extended</c>; does not replace open-debt.
/// </summary>
public sealed class ListLinkedCustomerOlderSettledActivity
{
    private const string ExtendedRequiredMessage =
        "Extended digital records entitlement is required to view older settled history.";

    private readonly AuthorizeLinkedCustomerStatementAccess _authorize;
    private readonly ILinkedCustomerRecentActivityQuery _activity;
    private readonly IPersonalFeatureEntitlementClient _entitlements;
    private readonly IOptions<PersonalStatementsOptions> _options;
    private readonly IClock _clock;

    public ListLinkedCustomerOlderSettledActivity(
        AuthorizeLinkedCustomerStatementAccess authorize,
        ILinkedCustomerRecentActivityQuery activity,
        IPersonalFeatureEntitlementClient entitlements,
        IOptions<PersonalStatementsOptions> options,
        IClock clock)
    {
        _authorize = authorize;
        _activity = activity;
        _entitlements = entitlements;
        _options = options;
        _clock = clock;
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

        var canAccessExtended = await _entitlements
            .HasActiveEntitlementAsync(PersonalSettledHistoryPolicy.ExtendedFeatureCode, cancellationToken)
            .ConfigureAwait(false);
        if (!canAccessExtended)
        {
            return ApplicationResult<LinkedCustomerRecentActivityPageDto>.Failure(
                ApplicationErrorCodes.ExtendedHistoryRequired,
                ExtendedRequiredMessage);
        }

        var ctx = authz.Value!;
        var orgId = PosOrganizationId.From(ctx.OrganizationId);
        var posCustomerId = POSCustomerId.From(ctx.PosCustomerId);
        var normalizedPage = LinkedCustomerStatementLimits.NormalizePage(page);
        var normalizedPageSize = LinkedCustomerStatementLimits.NormalizePageSize(pageSize);
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var asOfUtc = _clock.UtcNow;
        var freeStart = PersonalHistoryWindows.ComputeFreeWindowStart(
            asOfUtc,
            _options.Value.FreeRecentMonths);

        // Exclusive upper bound = free window start → older settled only; SQL OFFSET/LIMIT.
        var rows = await _activity
            .ListRecentDescendingAsync(
                orgId,
                posCustomerId,
                skip,
                normalizedPageSize + 1,
                notBeforeUtc: null,
                beforeUtc: freeStart,
                cancellationToken)
            .ConfigureAwait(false);
        var hasMore = rows.Count > normalizedPageSize;
        var pageRows = hasMore ? rows.Take(normalizedPageSize).ToList() : rows.ToList();

        var items = pageRows
            .Select(row => LinkedCustomerActivityMapper.Map(row, balanceAfter: null))
            .ToList();

        return ApplicationResult<LinkedCustomerRecentActivityPageDto>.Success(
            new LinkedCustomerRecentActivityPageDto(
                ctx.OrganizationId,
                ctx.PlatformBusinessCustomerId,
                ctx.PosCustomerId,
                items,
                normalizedPage,
                normalizedPageSize,
                hasMore,
                CanAccessExtendedHistory: true,
                freeStart));
    }
}

/// <summary>
/// Open-debt explanation: Active credits + Active repayments only while outstanding &gt; 0.
/// Never unlocks settled-old history when balance is zero; does not unlock unrelated reversed rows.
/// </summary>
public sealed class ListLinkedCustomerOpenDebtActivity
{
    private readonly AuthorizeLinkedCustomerStatementAccess _authorize;
    private readonly ILinkedCustomerRecentActivityQuery _activity;
    private readonly IOutstandingBalanceService _outstanding;

    public ListLinkedCustomerOpenDebtActivity(
        AuthorizeLinkedCustomerStatementAccess authorize,
        ILinkedCustomerRecentActivityQuery activity,
        IOutstandingBalanceService outstanding)
    {
        _authorize = authorize;
        _activity = activity;
        _outstanding = outstanding;
    }

    public async Task<ApplicationResult<LinkedCustomerOpenDebtActivityPageDto>> ExecuteAsync(
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
            return ApplicationResult<LinkedCustomerOpenDebtActivityPageDto>.Failure(
                authz.ErrorCode!,
                authz.ErrorMessage!);
        }

        var ctx = authz.Value!;
        var orgId = PosOrganizationId.From(ctx.OrganizationId);
        var posCustomerId = POSCustomerId.From(ctx.PosCustomerId);
        var outstanding = await _outstanding
            .GetOutstandingAsync(orgId, posCustomerId, cancellationToken)
            .ConfigureAwait(false);

        var normalizedPage = LinkedCustomerStatementLimits.NormalizePage(page);
        var normalizedPageSize = LinkedCustomerStatementLimits.NormalizePageSize(pageSize);

        if (outstanding <= 0m)
        {
            // Zero outstanding must not unlock arbitrary old active-history dumps.
            return ApplicationResult<LinkedCustomerOpenDebtActivityPageDto>.Success(
                new LinkedCustomerOpenDebtActivityPageDto(
                    ctx.OrganizationId,
                    ctx.PlatformBusinessCustomerId,
                    ctx.PosCustomerId,
                    0m,
                    [],
                    normalizedPage,
                    normalizedPageSize,
                    HasMore: false));
        }

        var skip = (normalizedPage - 1) * normalizedPageSize;
        var rows = await _activity
            .ListActiveDescendingAsync(orgId, posCustomerId, skip, normalizedPageSize + 1, cancellationToken)
            .ConfigureAwait(false);
        var hasMore = rows.Count > normalizedPageSize;
        var pageRows = hasMore ? rows.Take(normalizedPageSize).ToList() : rows.ToList();

        decimal? runningAfter = normalizedPage == 1 ? outstanding : null;
        var items = new List<LinkedCustomerActivityItemDto>(pageRows.Count);
        foreach (var row in pageRows)
        {
            decimal? balanceAfter = null;
            if (runningAfter is decimal bal)
            {
                balanceAfter = bal;
                runningAfter = bal - row.SignedEffect;
            }

            items.Add(LinkedCustomerActivityMapper.Map(row, balanceAfter));
        }

        return ApplicationResult<LinkedCustomerOpenDebtActivityPageDto>.Success(
            new LinkedCustomerOpenDebtActivityPageDto(
                ctx.OrganizationId,
                ctx.PlatformBusinessCustomerId,
                ctx.PosCustomerId,
                outstanding,
                items,
                normalizedPage,
                normalizedPageSize,
                hasMore));
    }
}

internal static class LinkedCustomerActivityMapper
{
    public static LinkedCustomerActivityItemDto Map(LinkedCustomerActivityRawRow row, decimal? balanceAfter)
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
            ? $"RCPT-{row.EntryId.ToString("N")[..12].ToUpperInvariant()}"
            : row.SourceSaleId is Guid saleId
                ? saleId.ToString("N")[..8].ToUpperInvariant()
                : row.EntryId.ToString("N")[..8].ToUpperInvariant();

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
}
