using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Suppliers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Application.SupplierPayables;

public sealed class SupplierPayableQueryService
{
    private readonly ISupplierPayableRepository _payables;
    private readonly ISupplierRepository _suppliers;
    private readonly IClock _clock;

    public SupplierPayableQueryService(
        ISupplierPayableRepository payables,
        ISupplierRepository suppliers,
        IClock clock)
    {
        _payables = payables;
        _suppliers = suppliers;
        _clock = clock;
    }

    public async Task<PosSupplierPayableDto?> GetByIdAsync(
        Guid organizationId,
        Guid payableId,
        CancellationToken cancellationToken = default)
    {
        var org = PosOrganizationId.From(organizationId);
        var payable = await _payables
            .GetByIdAsync(org, SupplierPayableId.From(payableId), cancellationToken)
            .ConfigureAwait(false);
        if (payable is null)
        {
            return null;
        }

        var supplier = await _suppliers
            .GetByIdAsync(org, payable.SupplierId, cancellationToken)
            .ConfigureAwait(false);
        return SupplierPayableMapper.Map(payable, supplier?.Name, AsOfDate());
    }

    public async Task<PagedResult<PosSupplierPayableDto>> ListAsync(
        Guid organizationId,
        Guid? supplierId,
        string? status,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        SupplierPayableStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<SupplierPayableStatus>(status, ignoreCase: true, out var value))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidSupplierPayableStatusTransition,
                    $"Unrecognized payable status '{status}'.");
            }

            parsedStatus = value;
        }

        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var org = PosOrganizationId.From(organizationId);
        var filter = new SupplierPayableFilter(
            supplierId is Guid sid ? SupplierId.From(sid) : null,
            parsedStatus);

        var (items, total) = await _payables
            .ListAsync(org, filter, skip, take, cancellationToken)
            .ConfigureAwait(false);

        var names = await LoadSupplierNamesAsync(org, items.Select(i => i.SupplierId).Distinct(), cancellationToken)
            .ConfigureAwait(false);
        var asOf = AsOfDate();

        return new PagedResult<PosSupplierPayableDto>(
            items.Select(p => SupplierPayableMapper.Map(p, names.GetValueOrDefault(p.SupplierId.Value), asOf)).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public async Task<IReadOnlyList<PosSupplierPayablePaymentDto>> ListPaymentsAsync(
        Guid organizationId,
        Guid payableId,
        CancellationToken cancellationToken = default)
    {
        var org = PosOrganizationId.From(organizationId);
        var payable = await _payables
            .GetByIdAsync(org, SupplierPayableId.From(payableId), cancellationToken)
            .ConfigureAwait(false);
        if (payable is null)
        {
            return Array.Empty<PosSupplierPayablePaymentDto>();
        }

        var payments = await _payables
            .ListPaymentsAsync(org, payable.Id, cancellationToken)
            .ConfigureAwait(false);
        return payments.Select(SupplierPayableMapper.MapPayment).ToList();
    }

    public async Task<PosSupplierPayableSummaryDto> GetSupplierSummaryAsync(
        Guid organizationId,
        Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        var org = PosOrganizationId.From(organizationId);
        var sid = SupplierId.From(supplierId);
        var totals = await _payables
            .GetSupplierSummaryAsync(org, sid, AsOfDate(), cancellationToken)
            .ConfigureAwait(false);
        return new PosSupplierPayableSummaryDto(
            sid.Value,
            totals.OutstandingTotal,
            totals.OverdueTotal,
            totals.OpenCount);
    }

    public async Task<IReadOnlyList<PosSupplierPayableReportRowDto>> ListForReportAsync(
        Guid organizationId,
        Guid? supplierId,
        string? status,
        bool outstandingOnly,
        CancellationToken cancellationToken = default)
    {
        SupplierPayableStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<SupplierPayableStatus>(status, ignoreCase: true, out var value))
        {
            parsedStatus = value;
        }

        var org = PosOrganizationId.From(organizationId);
        var asOf = AsOfDate();
        var filter = new SupplierPayableFilter(
            supplierId is Guid sid ? SupplierId.From(sid) : null,
            parsedStatus,
            OutstandingOnly: outstandingOnly,
            AsOfDate: asOf);

        var (items, _) = await _payables
            .ListAsync(org, filter, skip: 0, take: 10_000, cancellationToken)
            .ConfigureAwait(false);

        var names = await LoadSupplierNamesAsync(org, items.Select(i => i.SupplierId).Distinct(), cancellationToken)
            .ConfigureAwait(false);

        return items
            .Select(p => SupplierPayableMapper.MapReportRow(p, names.GetValueOrDefault(p.SupplierId.Value), asOf))
            .ToList();
    }

    private DateOnly AsOfDate() => DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);

    private async Task<Dictionary<Guid, string>> LoadSupplierNamesAsync(
        PosOrganizationId organizationId,
        IEnumerable<SupplierId> supplierIds,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, string>();
        foreach (var supplierId in supplierIds)
        {
            var supplier = await _suppliers
                .GetByIdAsync(organizationId, supplierId, cancellationToken)
                .ConfigureAwait(false);
            if (supplier is not null)
            {
                result[supplierId.Value] = supplier.Name;
            }
        }

        return result;
    }
}

public sealed class RecordSupplierPayablePayment
{
    private readonly ISupplierPayableRepository _payables;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly IClock _clock;

    public RecordSupplierPayablePayment(
        ISupplierPayableRepository payables,
        IPosCommercialAccessAccessor access,
        IClock clock)
    {
        _payables = payables;
        _access = access;
        _clock = clock;
    }

    public async Task<ApplicationResult<PosSupplierPayablePaymentDto>> ExecuteAsync(
        Guid organizationId,
        Guid payableId,
        RecordSupplierPayablePaymentRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.ManagePurchasing);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<PosSupplierPayablePaymentDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        if (actorId == Guid.Empty)
        {
            return ApplicationResult<PosSupplierPayablePaymentDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to record a supplier payment.");
        }

        try
        {
            if (!SupplierPayablePaymentMethods.TryParse(request.PaymentMethod, out var method))
            {
                return ApplicationResult<PosSupplierPayablePaymentDto>.Failure(
                    DomainErrorCodes.InvalidSupplierPayablePaymentMethod,
                    $"Payment method must be one of: {string.Join(", ", SupplierPayablePaymentMethods.Codes)}.");
            }

            var org = PosOrganizationId.From(organizationId);
            var id = SupplierPayableId.From(payableId);
            var payable = await _payables.GetByIdAsync(org, id, cancellationToken).ConfigureAwait(false);
            if (payable is null)
            {
                return ApplicationResult<PosSupplierPayablePaymentDto>.Failure(
                    ApplicationErrorCodes.SupplierPayableNotFound,
                    "Supplier payable was not found.");
            }

            var utcNow = _clock.UtcNow;
            var payment = payable.ApplyPayment(
                request.Amount,
                method,
                actorId,
                utcNow,
                request.PaidAtUtc,
                request.Reference,
                request.Notes);

            await _payables.UpdateAsync(payable, cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PosSupplierPayablePaymentDto>.Success(
                SupplierPayableMapper.MapPayment(payment));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosSupplierPayablePaymentDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
