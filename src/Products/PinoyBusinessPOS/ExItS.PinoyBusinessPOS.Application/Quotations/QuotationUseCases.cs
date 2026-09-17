using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Quotations;

namespace ExItS.PinoyBusinessPOS.Application.Quotations;

public sealed record PosQuotationLineDto(
    Guid LineId,
    Guid ProductId,
    int LineNumber,
    string? NameSnapshot,
    string? SkuSnapshot,
    string? UomSnapshot,
    decimal Quantity,
    decimal UnitPrice,
    decimal? DiscountAmount,
    decimal LineTotal);

public sealed record PosQuotationDto(
    Guid QuotationId,
    Guid OrganizationId,
    string? QuotationNumber,
    string Status,
    Guid CustomerId,
    string CustomerDisplayName,
    string? CustomerMobileNumber,
    string? CustomerAddress,
    string? CustomerNotes,
    Guid BranchId,
    Guid PreparedBy,
    DateOnly? ValidUntil,
    string? Reference,
    string? Notes,
    string? Terms,
    Guid? ConvertedSaleId,
    bool IsCustomerVisible,
    DateTimeOffset? IssuedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    decimal Subtotal,
    IReadOnlyList<PosQuotationLineDto> Lines);

public sealed record CreateQuotationLineRequest(
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal? DiscountAmount = null);

public sealed record CreateQuotationRequest(
    Guid CustomerId,
    Guid BranchId,
    IReadOnlyList<CreateQuotationLineRequest> Lines,
    DateOnly? ValidUntil = null,
    string? Reference = null,
    string? Notes = null,
    string? Terms = null,
    Guid? QuotationId = null,
    bool IsCustomerVisible = false);

public sealed record UpdateQuotationRequest(
    Guid CustomerId,
    Guid BranchId,
    IReadOnlyList<CreateQuotationLineRequest> Lines,
    DateTimeOffset ExpectedUpdatedAtUtc,
    DateOnly? ValidUntil = null,
    string? Reference = null,
    string? Notes = null,
    string? Terms = null,
    bool? IsCustomerVisible = null);

public static class QuotationMapper
{
    public static PosQuotationDto Map(Quotation quotation) =>
        new(
            quotation.Id.Value,
            quotation.OrganizationId.Value,
            quotation.QuotationNumber,
            quotation.Status.ToString(),
            quotation.CustomerId.Value,
            quotation.CustomerDisplayNameSnapshot,
            quotation.CustomerMobileNumberSnapshot,
            quotation.CustomerAddressSnapshot,
            quotation.CustomerNotesSnapshot,
            quotation.BranchId.Value,
            quotation.PreparedBy,
            quotation.ValidUntil,
            quotation.Reference,
            quotation.Notes,
            quotation.Terms,
            quotation.ConvertedSaleId,
            quotation.IsCustomerVisible,
            quotation.IssuedAtUtc,
            quotation.CreatedAtUtc,
            quotation.UpdatedAtUtc,
            quotation.Subtotal,
            quotation.Lines.Select(MapLine).ToList());

    public static PosQuotationLineDto MapLine(QuotationLine line) =>
        new(
            line.Id.Value,
            line.ProductId.Value,
            line.LineNumber,
            line.NameSnapshot,
            line.SkuSnapshot,
            line.UomSnapshot is null ? null : UnitOfMeasures.ToCode(line.UomSnapshot.Value),
            line.Quantity,
            line.UnitPrice,
            line.DiscountAmount,
            line.LineTotal);
}

internal static class QuotationGuards
{
    public static async Task<(ApplicationResult? Error, POSCustomer? Customer)> LoadActiveCustomerAsync(
        IPOSCustomerRepository customers,
        PosOrganizationId organizationId,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        if (customerId == Guid.Empty)
        {
            return (ApplicationResult.Failure(
                DomainErrorCodes.InvalidCustomerId,
                "Customer id is required."), null);
        }

        var customer = await customers
            .GetByIdAsync(organizationId, POSCustomerId.From(customerId), cancellationToken)
            .ConfigureAwait(false);
        if (customer is null)
        {
            return (ApplicationResult.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found in this organization."), null);
        }

        if (customer.Status != CustomerStatus.Active)
        {
            return (ApplicationResult.Failure(
                DomainErrorCodes.CustomerNotActive,
                "Only active customers can receive quotations."), null);
        }

        return (null, customer);
    }

    public static async Task<(ApplicationResult? Error, IReadOnlyDictionary<Guid, CatalogProduct>? Products)>
        ResolveActiveProductsAsync(
            ICatalogProductRepository products,
            PosOrganizationId organizationId,
            IReadOnlyList<Guid> productIds,
            CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return (ApplicationResult.Failure(
                DomainErrorCodes.QuotationRequiresLines,
                "A quotation must contain at least one line."), null);
        }

        var ids = productIds.Select(CatalogProductId.From).ToList();
        var found = await products.ListByIdsAsync(organizationId, ids, cancellationToken).ConfigureAwait(false);
        var byId = found.ToDictionary(p => p.Id.Value);
        foreach (var id in productIds)
        {
            if (!byId.ContainsKey(id))
            {
                return (ApplicationResult.Failure(
                    ApplicationErrorCodes.QuotationProductNotFound,
                    "One or more products were not found in this organization."), null);
            }

            if (byId[id].Status != CatalogProductStatus.Active)
            {
                return (ApplicationResult.Failure(
                    ApplicationErrorCodes.QuotationProductNotActive,
                    "Only active catalog products can be added to a quotation."), null);
            }
        }

        return (null, byId);
    }

    public static List<QuotationLineDraft> BuildDraftLines(
        IReadOnlyList<CreateQuotationLineRequest> lines,
        IReadOnlyDictionary<Guid, CatalogProduct> products) =>
        lines.Select(l =>
        {
            var product = products[l.ProductId];
            return new QuotationLineDraft(
                CatalogProductId.From(l.ProductId),
                l.Quantity,
                l.UnitPrice,
                l.DiscountAmount,
                product.Name,
                product.Sku,
                product.UnitOfMeasure);
        }).ToList();

    public static async Task<ApplicationResult<PosQuotationDto>> MutateStatusAsync(
        IQuotationRepository quotations,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider clock,
        Guid organizationId,
        Guid quotationId,
        Action<Quotation> mutate,
        CancellationToken cancellationToken)
    {
        var gate = CommercialAccessGuard.Require(access, UtangCapability.CreateSale);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<PosQuotationDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        try
        {
            var org = PosOrganizationId.From(organizationId);
            var existing = await quotations
                .GetByIdAsync(org, QuotationId.From(quotationId), cancellationToken)
                .ConfigureAwait(false);
            if (existing is null)
            {
                return ApplicationResult<PosQuotationDto>.Failure(
                    ApplicationErrorCodes.QuotationNotFound,
                    "Quotation was not found in this organization.");
            }

            mutate(existing);
            await quotations.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PosQuotationDto>.Success(QuotationMapper.Map(existing));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosQuotationDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PosQuotationDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class QuotationQueryService
{
    private readonly IQuotationRepository _quotations;

    public QuotationQueryService(IQuotationRepository quotations) => _quotations = quotations;

    public async Task<PosQuotationDto?> GetByIdAsync(
        Guid organizationId,
        Guid quotationId,
        CancellationToken cancellationToken = default)
    {
        var quotation = await _quotations
            .GetByIdAsync(PosOrganizationId.From(organizationId), QuotationId.From(quotationId), cancellationToken)
            .ConfigureAwait(false);
        return quotation is null ? null : QuotationMapper.Map(quotation);
    }

    public async Task<PagedResult<PosQuotationDto>> ListAsync(
        Guid organizationId,
        QuotationFilter filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(pageSize ?? 25, 1, 100);
        var currentPage = Math.Max(page ?? 1, 1);
        var skip = (currentPage - 1) * take;
        var (items, total) = await _quotations
            .ListAsync(PosOrganizationId.From(organizationId), filter, skip, take, cancellationToken)
            .ConfigureAwait(false);
        return new PagedResult<PosQuotationDto>(
            items.Select(QuotationMapper.Map).ToList(),
            total,
            currentPage,
            take);
    }
}

public sealed class CreateQuotationDraft
{
    private readonly IQuotationRepository _quotations;
    private readonly IPOSCustomerRepository _customers;
    private readonly ICatalogProductRepository _products;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public CreateQuotationDraft(
        IQuotationRepository quotations,
        IPOSCustomerRepository customers,
        ICatalogProductRepository products,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _quotations = quotations;
        _customers = customers;
        _products = products;
        _unitOfWork = unitOfWork;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PosQuotationDto>> ExecuteAsync(
        Guid organizationId,
        CreateQuotationRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.CreateSale);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<PosQuotationDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        if (actorId == Guid.Empty)
        {
            return ApplicationResult<PosQuotationDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to create a quotation.");
        }

        try
        {
            var org = PosOrganizationId.From(organizationId);
            if (request.QuotationId is Guid clientId && clientId != Guid.Empty)
            {
                var existing = await _quotations
                    .GetByIdAsync(org, QuotationId.From(clientId), cancellationToken)
                    .ConfigureAwait(false);
                if (existing is not null)
                {
                    return ApplicationResult<PosQuotationDto>.Success(QuotationMapper.Map(existing));
                }
            }

            var (customerError, customer) = await QuotationGuards
                .LoadActiveCustomerAsync(_customers, org, request.CustomerId, cancellationToken)
                .ConfigureAwait(false);
            if (customerError is not null)
            {
                return ApplicationResult<PosQuotationDto>.Failure(
                    customerError.ErrorCode!,
                    customerError.ErrorMessage!);
            }

            var (productError, products) = await QuotationGuards
                .ResolveActiveProductsAsync(
                    _products,
                    org,
                    request.Lines.Select(l => l.ProductId).ToList(),
                    cancellationToken)
                .ConfigureAwait(false);
            if (productError is not null)
            {
                return ApplicationResult<PosQuotationDto>.Failure(
                    productError.ErrorCode!,
                    productError.ErrorMessage!);
            }

            var drafts = QuotationGuards.BuildDraftLines(request.Lines, products!);
            var utcNow = _clock.GetUtcNow();
            var quotation = Quotation.CreateDraft(
                org,
                customer!.Id,
                customer.DisplayName,
                PosBranchId.From(request.BranchId),
                actorId,
                drafts,
                utcNow,
                customer.MobileNumber,
                customer.Address,
                customer.Notes,
                request.ValidUntil,
                request.Reference,
                request.Notes,
                request.Terms,
                request.QuotationId is Guid qid && qid != Guid.Empty ? QuotationId.From(qid) : null,
                request.IsCustomerVisible);

            await _quotations.AddAsync(quotation, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PosQuotationDto>.Success(QuotationMapper.Map(quotation));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosQuotationDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UpdateQuotationDraft
{
    private readonly IQuotationRepository _quotations;
    private readonly IPOSCustomerRepository _customers;
    private readonly ICatalogProductRepository _products;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public UpdateQuotationDraft(
        IQuotationRepository quotations,
        IPOSCustomerRepository customers,
        ICatalogProductRepository products,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _quotations = quotations;
        _customers = customers;
        _products = products;
        _unitOfWork = unitOfWork;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PosQuotationDto>> ExecuteAsync(
        Guid organizationId,
        Guid quotationId,
        UpdateQuotationRequest request,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.CreateSale);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<PosQuotationDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        try
        {
            var org = PosOrganizationId.From(organizationId);
            var id = QuotationId.From(quotationId);
            var existing = await _quotations.GetByIdAsync(org, id, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                return ApplicationResult<PosQuotationDto>.Failure(
                    ApplicationErrorCodes.QuotationNotFound,
                    "Quotation was not found in this organization.");
            }

            if (existing.UpdatedAtUtc != request.ExpectedUpdatedAtUtc)
            {
                return ApplicationResult<PosQuotationDto>.Failure(
                    ApplicationErrorCodes.QuotationConcurrencyConflict,
                    "Quotation was modified by another request. Reload and retry.");
            }

            var (customerError, customer) = await QuotationGuards
                .LoadActiveCustomerAsync(_customers, org, request.CustomerId, cancellationToken)
                .ConfigureAwait(false);
            if (customerError is not null)
            {
                return ApplicationResult<PosQuotationDto>.Failure(
                    customerError.ErrorCode!,
                    customerError.ErrorMessage!);
            }

            var (productError, products) = await QuotationGuards
                .ResolveActiveProductsAsync(
                    _products,
                    org,
                    request.Lines.Select(l => l.ProductId).ToList(),
                    cancellationToken)
                .ConfigureAwait(false);
            if (productError is not null)
            {
                return ApplicationResult<PosQuotationDto>.Failure(
                    productError.ErrorCode!,
                    productError.ErrorMessage!);
            }

            existing.UpdateDraft(
                customer!.Id,
                customer.DisplayName,
                PosBranchId.From(request.BranchId),
                QuotationGuards.BuildDraftLines(request.Lines, products!),
                _clock.GetUtcNow(),
                customer.MobileNumber,
                customer.Address,
                customer.Notes,
                request.ValidUntil,
                request.Reference,
                request.Notes,
                request.Terms,
                request.IsCustomerVisible);

            await _quotations.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PosQuotationDto>.Success(QuotationMapper.Map(existing));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosQuotationDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PosQuotationDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class IssueQuotation
{
    private readonly IQuotationRepository _quotations;
    private readonly ICatalogProductRepository _products;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public IssueQuotation(
        IQuotationRepository quotations,
        ICatalogProductRepository products,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _quotations = quotations;
        _products = products;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PosQuotationDto>> ExecuteAsync(
        Guid organizationId,
        Guid quotationId,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.CreateSale);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<PosQuotationDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        try
        {
            var org = PosOrganizationId.From(organizationId);
            var id = QuotationId.From(quotationId);
            var existing = await _quotations.GetByIdAsync(org, id, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                return ApplicationResult<PosQuotationDto>.Failure(
                    ApplicationErrorCodes.QuotationNotFound,
                    "Quotation was not found in this organization.");
            }

            if (existing.Status == QuotationStatus.Sent && !string.IsNullOrWhiteSpace(existing.QuotationNumber))
            {
                return ApplicationResult<PosQuotationDto>.Success(QuotationMapper.Map(existing));
            }

            var (productError, products) = await QuotationGuards
                .ResolveActiveProductsAsync(
                    _products,
                    org,
                    existing.Lines.Select(l => l.ProductId.Value).ToList(),
                    cancellationToken)
                .ConfigureAwait(false);
            if (productError is not null)
            {
                return ApplicationResult<PosQuotationDto>.Failure(
                    productError.ErrorCode!,
                    productError.ErrorMessage!);
            }

            var utcNow = _clock.GetUtcNow();
            var snapshots = existing.Lines
                .OrderBy(l => l.LineNumber)
                .Select(l =>
                {
                    var product = products![l.ProductId.Value];
                    return new QuotationLineSnapshotInput(
                        l.ProductId,
                        product.Name,
                        product.UnitOfMeasure,
                        l.Quantity,
                        l.UnitPrice,
                        product.Sku,
                        l.DiscountAmount);
                })
                .ToList();

            var issued = await _quotations
                .IssueAsync(
                    org,
                    id,
                    QuotationNumbers.BusinessDateOf(utcNow),
                    number =>
                    {
                        existing.Issue(number, snapshots, utcNow);
                        return existing;
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            return ApplicationResult<PosQuotationDto>.Success(QuotationMapper.Map(issued));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosQuotationDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PosQuotationDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class CancelQuotation
{
    private readonly IQuotationRepository _quotations;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public CancelQuotation(
        IQuotationRepository quotations,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _quotations = quotations;
        _unitOfWork = unitOfWork;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public Task<ApplicationResult<PosQuotationDto>> ExecuteAsync(
        Guid organizationId,
        Guid quotationId,
        CancellationToken cancellationToken = default) =>
        QuotationGuards.MutateStatusAsync(
            _quotations,
            _unitOfWork,
            _access,
            _clock,
            organizationId,
            quotationId,
            q => q.Cancel(_clock.GetUtcNow()),
            cancellationToken);
}

public sealed class AcceptQuotation
{
    private readonly IQuotationRepository _quotations;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public AcceptQuotation(
        IQuotationRepository quotations,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _quotations = quotations;
        _unitOfWork = unitOfWork;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public Task<ApplicationResult<PosQuotationDto>> ExecuteAsync(
        Guid organizationId,
        Guid quotationId,
        CancellationToken cancellationToken = default) =>
        QuotationGuards.MutateStatusAsync(
            _quotations,
            _unitOfWork,
            _access,
            _clock,
            organizationId,
            quotationId,
            q => q.Accept(_clock.GetUtcNow()),
            cancellationToken);
}

public sealed class DeclineQuotation
{
    private readonly IQuotationRepository _quotations;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public DeclineQuotation(
        IQuotationRepository quotations,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _quotations = quotations;
        _unitOfWork = unitOfWork;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public Task<ApplicationResult<PosQuotationDto>> ExecuteAsync(
        Guid organizationId,
        Guid quotationId,
        CancellationToken cancellationToken = default) =>
        QuotationGuards.MutateStatusAsync(
            _quotations,
            _unitOfWork,
            _access,
            _clock,
            organizationId,
            quotationId,
            q => q.Decline(_clock.GetUtcNow()),
            cancellationToken);
}

public sealed class ExpireQuotation
{
    private readonly IQuotationRepository _quotations;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public ExpireQuotation(
        IQuotationRepository quotations,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _quotations = quotations;
        _unitOfWork = unitOfWork;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public Task<ApplicationResult<PosQuotationDto>> ExecuteAsync(
        Guid organizationId,
        Guid quotationId,
        CancellationToken cancellationToken = default) =>
        QuotationGuards.MutateStatusAsync(
            _quotations,
            _unitOfWork,
            _access,
            _clock,
            organizationId,
            quotationId,
            q => q.Expire(_clock.GetUtcNow()),
            cancellationToken);
}

public sealed class MarkQuotationConverted
{
    private readonly IQuotationRepository _quotations;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public MarkQuotationConverted(
        IQuotationRepository quotations,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _quotations = quotations;
        _unitOfWork = unitOfWork;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PosQuotationDto>> ExecuteAsync(
        Guid organizationId,
        Guid quotationId,
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.CreateSale);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<PosQuotationDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        try
        {
            var org = PosOrganizationId.From(organizationId);
            var existing = await _quotations
                .GetByIdAsync(org, QuotationId.From(quotationId), cancellationToken)
                .ConfigureAwait(false);
            if (existing is null)
            {
                return ApplicationResult<PosQuotationDto>.Failure(
                    ApplicationErrorCodes.QuotationNotFound,
                    "Quotation was not found in this organization.");
            }

            existing.MarkConverted(saleId, _clock.GetUtcNow());
            await _quotations.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PosQuotationDto>.Success(QuotationMapper.Map(existing));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosQuotationDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PosQuotationDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
