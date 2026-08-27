using ExItS.PinoyBuyNowPayLater.Application.Common;
using ExItS.PinoyBuyNowPayLater.Domain.Customers;

namespace ExItS.PinoyBuyNowPayLater.Application.Customers;

public sealed class CreateBnplCustomer
{
    private readonly IBnplCustomerRepository _customers;
    private readonly IBnplUnitOfWork _unitOfWork;
    private readonly IBnplClock _clock;

    public CreateBnplCustomer(
        IBnplCustomerRepository customers,
        IBnplUnitOfWork unitOfWork,
        IBnplClock clock)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<BnplApplicationResult<BnplCustomer>> ExecuteAsync(
        Guid organizationId,
        string displayName,
        Guid? customerId = null,
        string? mobile = null,
        string? email = null,
        string? linkedPersonalPublicUserId = null,
        Guid? linkedCommerceCustomerId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (customerId is Guid suppliedId)
            {
                var existing = await _customers
                    .GetByIdAsync(organizationId, BnplCustomerId.From(suppliedId), cancellationToken)
                    .ConfigureAwait(false);
                if (existing is not null)
                {
                    if (!existing.IsCompatibleCreatePayload(
                            displayName,
                            mobile,
                            email,
                            linkedPersonalPublicUserId,
                            linkedCommerceCustomerId))
                    {
                        return BnplApplicationResult<BnplCustomer>.Failure(
                            BnplCustomerErrorCodes.IdempotencyConflict,
                            "CustomerId already exists with a conflicting profile payload.",
                            suggestedHttpStatus: 409);
                    }

                    return BnplApplicationResult<BnplCustomer>.Success(existing);
                }
            }

            var customer = BnplCustomer.Create(
                organizationId,
                displayName,
                _clock.UtcNow,
                customerId is null ? null : BnplCustomerId.From(customerId.Value),
                mobile,
                email,
                linkedPersonalPublicUserId,
                linkedCommerceCustomerId);

            var linkConflict = await DetectLinkConflictsAsync(
                    organizationId,
                    customer,
                    excludingCustomerId: null,
                    cancellationToken)
                .ConfigureAwait(false);
            if (linkConflict is not null)
            {
                return linkConflict;
            }

            await _customers.AddAsync(customer, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return BnplApplicationResult<BnplCustomer>.Success(customer);
        }
        catch (BnplDomainException ex)
        {
            return BnplApplicationResult<BnplCustomer>.Failure(ex.ErrorCode, ex.Message, 400);
        }
        catch (BnplPersistenceConflictException ex)
        {
            return BnplApplicationResult<BnplCustomer>.Failure(ex.ErrorCode, ex.Message, 409);
        }
    }

    internal static async Task<BnplApplicationResult<BnplCustomer>?> DetectLinkConflictsAsync(
        Guid organizationId,
        BnplCustomer candidate,
        BnplCustomerId? excludingCustomerId,
        CancellationToken cancellationToken,
        IBnplCustomerRepository customers)
    {
        if (candidate.LinkedPersonalPublicUserId is not null)
        {
            var existing = await customers
                .FindByLinkedPersonalPublicUserIdAsync(
                    organizationId,
                    candidate.LinkedPersonalPublicUserId,
                    cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null && (excludingCustomerId is null || existing.Id != excludingCustomerId.Value))
            {
                return BnplApplicationResult<BnplCustomer>.Failure(
                    BnplCustomerErrorCodes.PersonalLinkConflict,
                    "Another BNPL customer in this organization already links that Platform Personal identity.",
                    409);
            }
        }

        if (candidate.LinkedCommerceCustomerId is Guid commerceId)
        {
            var existing = await customers
                .FindByLinkedCommerceCustomerIdAsync(organizationId, commerceId, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null && (excludingCustomerId is null || existing.Id != excludingCustomerId.Value))
            {
                return BnplApplicationResult<BnplCustomer>.Failure(
                    BnplCustomerErrorCodes.CommerceLinkConflict,
                    "Another BNPL customer in this organization already links that Commerce customer id.",
                    409);
            }
        }

        return null;
    }

    private Task<BnplApplicationResult<BnplCustomer>?> DetectLinkConflictsAsync(
        Guid organizationId,
        BnplCustomer candidate,
        BnplCustomerId? excludingCustomerId,
        CancellationToken cancellationToken) =>
        DetectLinkConflictsAsync(organizationId, candidate, excludingCustomerId, cancellationToken, _customers);
}

public sealed class GetBnplCustomer
{
    private readonly IBnplCustomerRepository _customers;

    public GetBnplCustomer(IBnplCustomerRepository customers) => _customers = customers;

    public async Task<BnplApplicationResult<BnplCustomer>> ExecuteAsync(
        Guid organizationId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var customer = await _customers
                .GetByIdAsync(organizationId, BnplCustomerId.From(customerId), cancellationToken)
                .ConfigureAwait(false);
            if (customer is null)
            {
                return BnplApplicationResult<BnplCustomer>.Failure(
                    BnplCustomerErrorCodes.NotFound,
                    "Customer was not found in this organization.",
                    404);
            }

            return BnplApplicationResult<BnplCustomer>.Success(customer);
        }
        catch (BnplDomainException ex)
        {
            return BnplApplicationResult<BnplCustomer>.Failure(ex.ErrorCode, ex.Message, 400);
        }
    }
}

public sealed class SearchBnplCustomers
{
    private readonly IBnplCustomerRepository _customers;

    public SearchBnplCustomers(IBnplCustomerRepository customers) => _customers = customers;

    public async Task<BnplApplicationResult<BnplCustomerSearchPage>> ExecuteAsync(
        Guid organizationId,
        string? search,
        BnplCustomerStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            return BnplApplicationResult<BnplCustomerSearchPage>.Failure(
                BnplCustomerErrorCodes.InvalidOrganizationId,
                "OrganizationId must be a non-empty Guid.",
                400);
        }

        var safePage = page < 1 ? 1 : page;
        var safeSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);
        var skip = (safePage - 1) * safeSize;

        var (items, total) = await _customers
            .SearchAsync(organizationId, search, status, skip, safeSize, cancellationToken)
            .ConfigureAwait(false);

        return BnplApplicationResult<BnplCustomerSearchPage>.Success(
            new BnplCustomerSearchPage(items, total, safePage, safeSize));
    }
}

public sealed record BnplCustomerSearchPage(
    IReadOnlyList<BnplCustomer> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed class UpdateBnplCustomerProfile
{
    private readonly IBnplCustomerRepository _customers;
    private readonly IBnplUnitOfWork _unitOfWork;
    private readonly IBnplClock _clock;

    public UpdateBnplCustomerProfile(
        IBnplCustomerRepository customers,
        IBnplUnitOfWork unitOfWork,
        IBnplClock clock)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<BnplApplicationResult<BnplCustomer>> ExecuteAsync(
        Guid organizationId,
        Guid customerId,
        string displayName,
        string? mobile,
        string? email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var customer = await _customers
                .GetByIdAsync(organizationId, BnplCustomerId.From(customerId), cancellationToken)
                .ConfigureAwait(false);
            if (customer is null)
            {
                return BnplApplicationResult<BnplCustomer>.Failure(
                    BnplCustomerErrorCodes.NotFound,
                    "Customer was not found in this organization.",
                    404);
            }

            customer.UpdateProfile(displayName, mobile, email, _clock.UtcNow);
            await _customers.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return BnplApplicationResult<BnplCustomer>.Success(customer);
        }
        catch (BnplDomainException ex)
        {
            return BnplApplicationResult<BnplCustomer>.Failure(ex.ErrorCode, ex.Message, 400);
        }
    }
}

public sealed class LinkBnplCustomerPersonalIdentity
{
    private readonly IBnplCustomerRepository _customers;
    private readonly IBnplUnitOfWork _unitOfWork;
    private readonly IBnplClock _clock;

    public LinkBnplCustomerPersonalIdentity(
        IBnplCustomerRepository customers,
        IBnplUnitOfWork unitOfWork,
        IBnplClock clock)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<BnplApplicationResult<BnplCustomer>> ExecuteAsync(
        Guid organizationId,
        Guid customerId,
        string personalPublicUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var id = BnplCustomerId.From(customerId);
            var customer = await _customers
                .GetByIdAsync(organizationId, id, cancellationToken)
                .ConfigureAwait(false);
            if (customer is null)
            {
                return BnplApplicationResult<BnplCustomer>.Failure(
                    BnplCustomerErrorCodes.NotFound,
                    "Customer was not found in this organization.",
                    404);
            }

            customer.LinkPersonalPublicUserId(personalPublicUserId, _clock.UtcNow);
            var conflict = await CreateBnplCustomer.DetectLinkConflictsAsync(
                    organizationId,
                    customer,
                    excludingCustomerId: id,
                    cancellationToken,
                    _customers)
                .ConfigureAwait(false);
            if (conflict is not null)
            {
                return conflict;
            }

            await _customers.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return BnplApplicationResult<BnplCustomer>.Success(customer);
        }
        catch (BnplDomainException ex)
        {
            return BnplApplicationResult<BnplCustomer>.Failure(ex.ErrorCode, ex.Message, 400);
        }
        catch (BnplPersistenceConflictException ex)
        {
            return BnplApplicationResult<BnplCustomer>.Failure(ex.ErrorCode, ex.Message, 409);
        }
    }
}

public sealed class LinkBnplCustomerCommerceReference
{
    private readonly IBnplCustomerRepository _customers;
    private readonly IBnplUnitOfWork _unitOfWork;
    private readonly IBnplClock _clock;

    public LinkBnplCustomerCommerceReference(
        IBnplCustomerRepository customers,
        IBnplUnitOfWork unitOfWork,
        IBnplClock clock)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<BnplApplicationResult<BnplCustomer>> ExecuteAsync(
        Guid organizationId,
        Guid customerId,
        Guid commerceCustomerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var id = BnplCustomerId.From(customerId);
            var customer = await _customers
                .GetByIdAsync(organizationId, id, cancellationToken)
                .ConfigureAwait(false);
            if (customer is null)
            {
                return BnplApplicationResult<BnplCustomer>.Failure(
                    BnplCustomerErrorCodes.NotFound,
                    "Customer was not found in this organization.",
                    404);
            }

            customer.LinkCommerceCustomerId(commerceCustomerId, _clock.UtcNow);
            var conflict = await CreateBnplCustomer.DetectLinkConflictsAsync(
                    organizationId,
                    customer,
                    excludingCustomerId: id,
                    cancellationToken,
                    _customers)
                .ConfigureAwait(false);
            if (conflict is not null)
            {
                return conflict;
            }

            await _customers.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return BnplApplicationResult<BnplCustomer>.Success(customer);
        }
        catch (BnplDomainException ex)
        {
            return BnplApplicationResult<BnplCustomer>.Failure(ex.ErrorCode, ex.Message, 400);
        }
        catch (BnplPersistenceConflictException ex)
        {
            return BnplApplicationResult<BnplCustomer>.Failure(ex.ErrorCode, ex.Message, 409);
        }
    }
}

public sealed class BnplPersistenceConflictException : Exception
{
    public BnplPersistenceConflictException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
