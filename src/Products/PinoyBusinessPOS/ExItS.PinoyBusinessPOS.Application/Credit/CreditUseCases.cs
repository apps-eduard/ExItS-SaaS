using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Credit;

public sealed record CreditEntryDto(
    Guid CreditEntryId,
    Guid OrganizationId,
    Guid CustomerId,
    decimal Amount,
    string Remarks,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReversedAtUtc,
    string? ReversalReason,
    DateOnly? CurrentDueDate);

public sealed record CreditSyncPageDto(
    List<CreditEntryDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    DateTimeOffset? NextCheckpointUtc);

public sealed record CustomerCreditSummaryDto(
    Guid CustomerId,
    Guid OrganizationId,
    decimal OutstandingAmount,
    int ActiveEntryCount,
    int TotalEntryCount);

public sealed class CreditEntryQueryService
{
    private readonly ICreditEntryRepository _entries;
    private readonly IOutstandingBalanceService _outstanding;

    public CreditEntryQueryService(ICreditEntryRepository entries, IOutstandingBalanceService outstanding)
    {
        _entries = entries;
        _outstanding = outstanding;
    }

    public async Task<CreditEntryDto?> GetByIdAsync(
        Guid organizationId,
        Guid customerId,
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        var entry = await _entries
            .GetByIdAsync(
                PosOrganizationId.From(organizationId),
                POSCustomerId.From(customerId),
                CreditEntryId.From(entryId),
                cancellationToken)
            .ConfigureAwait(false);
        return entry is null ? null : Map(entry);
    }

    public async Task<PagedResult<CreditEntryDto>> ListByCustomerAsync(
        Guid organizationId,
        Guid customerId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _entries
            .ListByCustomerAsync(
                PosOrganizationId.From(organizationId),
                POSCustomerId.From(customerId),
                skip,
                take,
                cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<CreditEntryDto>(
            items.Select(Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public async Task<CreditSyncPageDto> ListForSyncAsync(
        Guid organizationId,
        DateTimeOffset? sinceUtc,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _entries
            .ListCreatedSinceAsync(PosOrganizationId.From(organizationId), sinceUtc, skip, take, cancellationToken)
            .ConfigureAwait(false);

        var mapped = items.Select(Map).ToList();
        DateTimeOffset? nextCheckpoint = null;
        foreach (var entry in mapped)
        {
            var candidate = entry.ReversedAtUtc ?? entry.CreatedAtUtc;
            if (nextCheckpoint is null || candidate > nextCheckpoint)
            {
                nextCheckpoint = candidate;
            }
        }

        return new CreditSyncPageDto(mapped, total, Math.Max(page ?? 1, 1), take, nextCheckpoint);
    }

    public async Task<CustomerCreditSummaryDto> GetSummaryAsync(
        Guid organizationId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var custId = POSCustomerId.From(customerId);
        var outstanding = await _outstanding.GetOutstandingAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        var activeCount = await _entries.CountActiveAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        var (_, total) = await _entries.ListByCustomerAsync(orgId, custId, 0, 1, cancellationToken).ConfigureAwait(false);

        return new CustomerCreditSummaryDto(customerId, organizationId, outstanding, activeCount, total);
    }

    public static CreditEntryDto Map(CreditEntry entry) =>
        new(
            entry.Id.Value,
            entry.OrganizationId.Value,
            entry.CustomerId.Value,
            entry.Amount,
            entry.Remarks,
            entry.Status.ToString(),
            entry.CreatedAtUtc,
            entry.ReversedAtUtc,
            entry.ReversalReason,
            entry.CurrentDueDate);
}

public sealed class CreateCreditEntry
{
    private readonly IPOSCustomerRepository _customers;
    private readonly ICreditEntryRepository _entries;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateCreditEntry(
        IPOSCustomerRepository customers,
        ICreditEntryRepository entries,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _customers = customers;
        _entries = entries;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<CreditEntry>> ExecuteAsync(
        Guid organizationId,
        Guid customerId,
        decimal amount,
        string remarks,
        Guid? clientCreditEntryId = null,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var custId = POSCustomerId.From(customerId);
        var customer = await _customers.GetByIdAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<CreditEntry>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        if (customer.Status != CustomerStatus.Active)
        {
            return ApplicationResult<CreditEntry>.Failure(
                DomainErrorCodes.CustomerNotActive,
                "Credit can only be recorded for an active customer.");
        }

        if (clientCreditEntryId is not null)
        {
            var existing = await _entries
                .GetByIdAsync(orgId, custId, CreditEntryId.From(clientCreditEntryId.Value), cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                return ApplicationResult<CreditEntry>.Success(existing);
            }
        }

        try
        {
            var entry = clientCreditEntryId is null
                ? CreditEntry.Create(orgId, custId, amount, remarks, _clock.UtcNow)
                : CreditEntry.Create(
                    orgId,
                    custId,
                    amount,
                    remarks,
                    _clock.UtcNow,
                    id: CreditEntryId.From(clientCreditEntryId.Value));
            await _entries.AddAsync(entry, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<CreditEntry>.Success(entry);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CreditEntry>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CreditEntry>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ReverseCreditEntry
{
    private readonly IPOSCustomerRepository _customers;
    private readonly ICreditEntryRepository _entries;
    private readonly IOutstandingBalanceService _outstanding;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReverseCreditEntry(
        IPOSCustomerRepository customers,
        ICreditEntryRepository entries,
        IOutstandingBalanceService outstanding,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _customers = customers;
        _entries = entries;
        _outstanding = outstanding;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<CreditEntry>> ExecuteAsync(
        Guid organizationId,
        Guid customerId,
        Guid entryId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var custId = POSCustomerId.From(customerId);

        var customer = await _customers.GetByIdAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<CreditEntry>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        var entry = await _entries
            .GetByIdAsync(orgId, custId, CreditEntryId.From(entryId), cancellationToken)
            .ConfigureAwait(false);
        if (entry is null)
        {
            return ApplicationResult<CreditEntry>.Failure(
                ApplicationErrorCodes.CreditEntryNotFound,
                "Credit entry was not found.");
        }

        try
        {
            return await _unitOfWork
                .ExecuteInSerializableTransactionAsync(async ct =>
                {
                    // Re-load inside the transaction for a consistent outstanding check.
                    var current = await _entries
                        .GetByIdAsync(orgId, custId, CreditEntryId.From(entryId), ct)
                        .ConfigureAwait(false);
                    if (current is null)
                    {
                        return ApplicationResult<CreditEntry>.Failure(
                            ApplicationErrorCodes.CreditEntryNotFound,
                            "Credit entry was not found.");
                    }

                    if (current.Status == CreditEntryStatus.Active)
                    {
                        var outstanding = await _outstanding.GetOutstandingAsync(orgId, custId, ct).ConfigureAwait(false);
                        if (outstanding - current.Amount < 0m)
                        {
                            return ApplicationResult<CreditEntry>.Failure(
                                DomainErrorCodes.CreditReversalWouldMakeOutstandingNegative,
                                "Reversing this credit would make outstanding negative because active repayments exceed remaining credits.");
                        }
                    }

                    current.Reverse(reason, _clock.UtcNow);
                    await _entries.UpdateAsync(current, ct).ConfigureAwait(false);
                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                    return ApplicationResult<CreditEntry>.Success(current);
                }, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CreditEntry>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CreditEntry>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
