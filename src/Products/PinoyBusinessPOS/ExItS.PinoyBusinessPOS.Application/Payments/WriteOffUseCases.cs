using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;

namespace ExItS.PinoyBusinessPOS.Application.Payments;

public sealed record WriteOffDto(
    Guid WriteOffId,
    Guid OrganizationId,
    Guid CustomerId,
    decimal Amount,
    string Reason,
    string Status,
    DateTimeOffset RecordedAtUtc,
    Guid RecordedBy,
    DateTimeOffset? ReversedAtUtc,
    string? ReversalReason,
    Guid? ReversedBy);

public sealed class WriteOffQueryService
{
    private readonly IWriteOffRepository _writeOffs;

    public WriteOffQueryService(IWriteOffRepository writeOffs) => _writeOffs = writeOffs;

    public async Task<WriteOffDto?> GetByIdAsync(
        Guid organizationId,
        Guid writeOffId,
        CancellationToken cancellationToken = default)
    {
        var writeOff = await _writeOffs
            .GetByIdAsync(PosOrganizationId.From(organizationId), WriteOffId.From(writeOffId), cancellationToken)
            .ConfigureAwait(false);
        return writeOff is null ? null : Map(writeOff);
    }

    public async Task<PagedResult<WriteOffDto>> ListByCustomerAsync(
        Guid organizationId,
        Guid customerId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _writeOffs
            .ListByCustomerAsync(
                PosOrganizationId.From(organizationId),
                POSCustomerId.From(customerId),
                skip,
                take,
                cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<WriteOffDto>(
            items.Select(Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public static WriteOffDto Map(WriteOff writeOff) =>
        new(
            writeOff.Id.Value,
            writeOff.OrganizationId.Value,
            writeOff.CustomerId.Value,
            writeOff.Amount,
            writeOff.Reason,
            writeOff.Status.ToString(),
            writeOff.RecordedAtUtc,
            writeOff.RecordedBy,
            writeOff.ReversedAtUtc,
            writeOff.ReversalReason,
            writeOff.ReversedBy);
}

public sealed class CreateWriteOff
{
    private readonly IPOSCustomerRepository _customers;
    private readonly IWriteOffRepository _writeOffs;
    private readonly IOutstandingBalanceService _outstanding;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateWriteOff(
        IPOSCustomerRepository customers,
        IWriteOffRepository writeOffs,
        IOutstandingBalanceService outstanding,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _customers = customers;
        _writeOffs = writeOffs;
        _outstanding = outstanding;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<WriteOff>> ExecuteAsync(
        Guid organizationId,
        Guid customerId,
        decimal amount,
        string reason,
        Guid recordedBy,
        Guid? clientWriteOffId = null,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var custId = POSCustomerId.From(customerId);
        var customer = await _customers.GetByIdAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<WriteOff>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        if (clientWriteOffId is not null)
        {
            var existing = await _writeOffs
                .GetByIdAsync(orgId, WriteOffId.From(clientWriteOffId.Value), cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                if (existing.CustomerId != custId)
                {
                    return ApplicationResult<WriteOff>.Failure(
                        ApplicationErrorCodes.ConcurrencyConflict,
                        "Write-off id is already assigned to another customer.");
                }

                return ApplicationResult<WriteOff>.Success(existing);
            }
        }

        try
        {
            return await _unitOfWork
                .ExecuteInSerializableTransactionAsync(async ct =>
                {
                    var outstanding = await _outstanding.GetOutstandingAsync(orgId, custId, ct).ConfigureAwait(false);
                    if (outstanding <= 0m)
                    {
                        return ApplicationResult<WriteOff>.Failure(
                            DomainErrorCodes.WriteOffOutstandingZero,
                            "Outstanding balance is zero; write-off is not allowed.");
                    }

                    var normalized = WriteOff.NormalizeAmount(amount);
                    if (normalized > outstanding)
                    {
                        return ApplicationResult<WriteOff>.Failure(
                            DomainErrorCodes.WriteOffExceedsOutstanding,
                            "Write-off amount exceeds the current outstanding balance.");
                    }

                    var writeOff = clientWriteOffId is null
                        ? WriteOff.Create(orgId, custId, normalized, reason, recordedBy, _clock.UtcNow)
                        : WriteOff.Create(
                            orgId,
                            custId,
                            normalized,
                            reason,
                            recordedBy,
                            _clock.UtcNow,
                            id: WriteOffId.From(clientWriteOffId.Value));
                    await _writeOffs.AddAsync(writeOff, ct).ConfigureAwait(false);
                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                    return ApplicationResult<WriteOff>.Success(writeOff);
                }, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<WriteOff>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<WriteOff>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ReverseWriteOff
{
    private readonly IPOSCustomerRepository _customers;
    private readonly IWriteOffRepository _writeOffs;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReverseWriteOff(
        IPOSCustomerRepository customers,
        IWriteOffRepository writeOffs,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _customers = customers;
        _writeOffs = writeOffs;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<WriteOff>> ExecuteAsync(
        Guid organizationId,
        Guid writeOffId,
        string reason,
        Guid reversedBy,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var writeOff = await _writeOffs
            .GetByIdAsync(orgId, WriteOffId.From(writeOffId), cancellationToken)
            .ConfigureAwait(false);
        if (writeOff is null)
        {
            return ApplicationResult<WriteOff>.Failure(
                ApplicationErrorCodes.WriteOffNotFound,
                "Write-off was not found.");
        }

        var customer = await _customers
            .GetByIdAsync(orgId, writeOff.CustomerId, cancellationToken)
            .ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<WriteOff>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        try
        {
            writeOff.Reverse(reason, reversedBy, _clock.UtcNow);
            await _writeOffs.UpdateAsync(writeOff, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<WriteOff>.Success(writeOff);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<WriteOff>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<WriteOff>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
