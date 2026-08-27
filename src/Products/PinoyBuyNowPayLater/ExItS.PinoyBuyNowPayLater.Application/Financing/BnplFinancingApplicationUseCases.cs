using ExItS.PinoyBuyNowPayLater.Application.Common;
using ExItS.PinoyBuyNowPayLater.Application.Customers;
using ExItS.PinoyBuyNowPayLater.Domain.Customers;
using ExItS.PinoyBuyNowPayLater.Domain.Financing;

namespace ExItS.PinoyBuyNowPayLater.Application.Financing;

internal static class BnplFinancingResults
{
    public static BnplApplicationResult<T> FromDomain<T>(BnplFinancingDomainException ex) =>
        BnplApplicationResult<T>.Failure(
            ex.ErrorCode,
            ex.Message,
            suggestedHttpStatus: ex.ErrorCode is BnplFinancingErrorCodes.ConcurrencyConflict
                or BnplFinancingErrorCodes.IdempotencyConflict
                or BnplFinancingErrorCodes.OfferSuperseded
                or BnplFinancingErrorCodes.PlanRequired
                or BnplFinancingErrorCodes.PlanImmutable
                ? 409
                : ex.ErrorCode is BnplFinancingErrorCodes.NotFound
                    ? 404
                    : 400);

    public static BnplApplicationResult<T> FromPersistence<T>(BnplPersistenceConflictException ex) =>
        BnplApplicationResult<T>.Failure(ex.ErrorCode, ex.Message, 409);
}

public sealed class CreateBnplFinancingApplication
{
    private readonly IBnplFinancingApplicationRepository _applications;
    private readonly IBnplCustomerRepository _customers;
    private readonly IBnplUnitOfWork _unitOfWork;
    private readonly IBnplClock _clock;

    public CreateBnplFinancingApplication(
        IBnplFinancingApplicationRepository applications,
        IBnplCustomerRepository customers,
        IBnplUnitOfWork unitOfWork,
        IBnplClock clock)
    {
        _applications = applications;
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<BnplApplicationResult<BnplFinancingApplication>> ExecuteAsync(
        Guid organizationId,
        Guid branchId,
        Guid customerId,
        Guid actorId,
        decimal purchaseAmount,
        decimal downPaymentAmount,
        Guid? applicationId = null,
        string? purchaseDescription = null,
        string? merchantProductReference = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (applicationId is Guid suppliedId)
            {
                var existing = await _applications
                    .GetByIdAsync(organizationId, BnplFinancingApplicationId.From(suppliedId), cancellationToken)
                    .ConfigureAwait(false);
                if (existing is not null)
                {
                    if (!existing.IsCompatibleCreatePayload(
                            organizationId,
                            branchId,
                            customerId,
                            purchaseAmount,
                            downPaymentAmount,
                            purchaseDescription,
                            merchantProductReference))
                    {
                        return BnplApplicationResult<BnplFinancingApplication>.Failure(
                            BnplFinancingErrorCodes.IdempotencyConflict,
                            "ApplicationId already exists with a conflicting payload.",
                            409);
                    }

                    return BnplApplicationResult<BnplFinancingApplication>.Success(existing);
                }
            }

            var customer = await _customers
                .GetByIdAsync(organizationId, BnplCustomerId.From(customerId), cancellationToken)
                .ConfigureAwait(false);
            if (customer is null)
            {
                return BnplApplicationResult<BnplFinancingApplication>.Failure(
                    BnplFinancingErrorCodes.NotFound,
                    "BNPL customer was not found in this organization.",
                    404);
            }

            if (customer.OrganizationId != organizationId)
            {
                return BnplApplicationResult<BnplFinancingApplication>.Failure(
                    BnplFinancingErrorCodes.CustomerOrgMismatch,
                    "Customer does not belong to this organization.",
                    409);
            }

            var application = BnplFinancingApplication.Create(
                organizationId,
                branchId,
                customerId,
                actorId,
                purchaseAmount,
                downPaymentAmount,
                _clock.UtcNow,
                applicationId is null ? null : BnplFinancingApplicationId.From(applicationId.Value),
                purchaseDescription,
                merchantProductReference);

            await _applications.AddAsync(application, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return BnplApplicationResult<BnplFinancingApplication>.Success(application);
        }
        catch (BnplFinancingDomainException ex)
        {
            return BnplFinancingResults.FromDomain<BnplFinancingApplication>(ex);
        }
        catch (BnplDomainException ex)
        {
            return BnplApplicationResult<BnplFinancingApplication>.Failure(ex.ErrorCode, ex.Message, 400);
        }
        catch (BnplPersistenceConflictException ex)
        {
            return BnplFinancingResults.FromPersistence<BnplFinancingApplication>(ex);
        }
    }
}

public sealed class GetBnplFinancingApplication
{
    private readonly IBnplFinancingApplicationRepository _applications;

    public GetBnplFinancingApplication(IBnplFinancingApplicationRepository applications) =>
        _applications = applications;

    public async Task<BnplApplicationResult<BnplFinancingApplication>> ExecuteAsync(
        Guid organizationId,
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var application = await _applications
                .GetByIdAsync(organizationId, BnplFinancingApplicationId.From(applicationId), cancellationToken)
                .ConfigureAwait(false);
            if (application is null)
            {
                return BnplApplicationResult<BnplFinancingApplication>.Failure(
                    BnplFinancingErrorCodes.NotFound,
                    "Financing application was not found in this organization.",
                    404);
            }

            return BnplApplicationResult<BnplFinancingApplication>.Success(application);
        }
        catch (BnplFinancingDomainException ex)
        {
            return BnplFinancingResults.FromDomain<BnplFinancingApplication>(ex);
        }
    }
}

public sealed class SearchBnplFinancingApplications
{
    private readonly IBnplFinancingApplicationRepository _applications;

    public SearchBnplFinancingApplications(IBnplFinancingApplicationRepository applications) =>
        _applications = applications;

    public async Task<BnplApplicationResult<BnplFinancingApplicationSearchPage>> ExecuteAsync(
        Guid organizationId,
        Guid? branchId,
        Guid? customerId,
        BnplFinancingApplicationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safeSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);
        var skip = (safePage - 1) * safeSize;
        var (items, total) = await _applications
            .SearchAsync(organizationId, branchId, customerId, status, skip, safeSize, cancellationToken)
            .ConfigureAwait(false);
        return BnplApplicationResult<BnplFinancingApplicationSearchPage>.Success(
            new BnplFinancingApplicationSearchPage(items, total, safePage, safeSize));
    }
}

public sealed record BnplFinancingApplicationSearchPage(
    IReadOnlyList<BnplFinancingApplication> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed class UpdateBnplFinancingApplicationDraft
{
    private readonly IBnplFinancingApplicationRepository _applications;
    private readonly IBnplUnitOfWork _unitOfWork;
    private readonly IBnplClock _clock;

    public UpdateBnplFinancingApplicationDraft(
        IBnplFinancingApplicationRepository applications,
        IBnplUnitOfWork unitOfWork,
        IBnplClock clock)
    {
        _applications = applications;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<BnplApplicationResult<BnplFinancingApplication>> ExecuteAsync(
        Guid organizationId,
        Guid applicationId,
        decimal purchaseAmount,
        decimal downPaymentAmount,
        string? purchaseDescription,
        string? merchantProductReference,
        int? expectedVersion = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var application = await LoadAsync(organizationId, applicationId, cancellationToken).ConfigureAwait(false);
            if (application is null)
            {
                return NotFound();
            }

            application.UpdateDraft(
                purchaseAmount,
                downPaymentAmount,
                purchaseDescription,
                merchantProductReference,
                _clock.UtcNow,
                expectedVersion);
            await PersistAsync(application, cancellationToken).ConfigureAwait(false);
            return BnplApplicationResult<BnplFinancingApplication>.Success(application);
        }
        catch (BnplFinancingDomainException ex)
        {
            return BnplFinancingResults.FromDomain<BnplFinancingApplication>(ex);
        }
        catch (BnplPersistenceConflictException ex)
        {
            return BnplFinancingResults.FromPersistence<BnplFinancingApplication>(ex);
        }
    }

    private Task<BnplFinancingApplication?> LoadAsync(
        Guid organizationId,
        Guid applicationId,
        CancellationToken cancellationToken) =>
        _applications.GetByIdAsync(organizationId, BnplFinancingApplicationId.From(applicationId), cancellationToken);

    private async Task PersistAsync(BnplFinancingApplication application, CancellationToken cancellationToken)
    {
        await _applications.UpdateAsync(application, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static BnplApplicationResult<BnplFinancingApplication> NotFound() =>
        BnplApplicationResult<BnplFinancingApplication>.Failure(
            BnplFinancingErrorCodes.NotFound,
            "Financing application was not found in this organization.",
            404);
}

public sealed class SubmitBnplFinancingApplication
{
    private readonly IBnplFinancingApplicationRepository _applications;
    private readonly IBnplUnitOfWork _unitOfWork;
    private readonly IBnplClock _clock;

    public SubmitBnplFinancingApplication(
        IBnplFinancingApplicationRepository applications,
        IBnplUnitOfWork unitOfWork,
        IBnplClock clock)
    {
        _applications = applications;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public Task<BnplApplicationResult<BnplFinancingApplication>> ExecuteAsync(
        Guid organizationId,
        Guid applicationId,
        int? expectedVersion = null,
        CancellationToken cancellationToken = default) =>
        MutateAsync(organizationId, applicationId, a => a.Submit(_clock.UtcNow, expectedVersion), cancellationToken);

    private async Task<BnplApplicationResult<BnplFinancingApplication>> MutateAsync(
        Guid organizationId,
        Guid applicationId,
        Action<BnplFinancingApplication> mutate,
        CancellationToken cancellationToken)
    {
        try
        {
            var application = await _applications
                .GetByIdAsync(organizationId, BnplFinancingApplicationId.From(applicationId), cancellationToken)
                .ConfigureAwait(false);
            if (application is null)
            {
                return BnplApplicationResult<BnplFinancingApplication>.Failure(
                    BnplFinancingErrorCodes.NotFound,
                    "Financing application was not found in this organization.",
                    404);
            }

            mutate(application);
            await _applications.UpdateAsync(application, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return BnplApplicationResult<BnplFinancingApplication>.Success(application);
        }
        catch (BnplFinancingDomainException ex)
        {
            return BnplFinancingResults.FromDomain<BnplFinancingApplication>(ex);
        }
        catch (BnplPersistenceConflictException ex)
        {
            return BnplFinancingResults.FromPersistence<BnplFinancingApplication>(ex);
        }
    }
}

public sealed class ApproveBnplFinancingEligibility
{
    private readonly IBnplFinancingApplicationRepository _applications;
    private readonly IBnplUnitOfWork _unitOfWork;
    private readonly IBnplClock _clock;

    public ApproveBnplFinancingEligibility(
        IBnplFinancingApplicationRepository applications,
        IBnplUnitOfWork unitOfWork,
        IBnplClock clock)
    {
        _applications = applications;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<BnplApplicationResult<BnplFinancingApplication>> ExecuteAsync(
        Guid organizationId,
        Guid applicationId,
        Guid actorId,
        string? note = null,
        int? expectedVersion = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var application = await _applications
                .GetByIdAsync(organizationId, BnplFinancingApplicationId.From(applicationId), cancellationToken)
                .ConfigureAwait(false);
            if (application is null)
            {
                return BnplApplicationResult<BnplFinancingApplication>.Failure(
                    BnplFinancingErrorCodes.NotFound,
                    "Financing application was not found in this organization.",
                    404);
            }

            application.ApproveEligibility(actorId, _clock.UtcNow, note, expectedVersion);
            await _applications.UpdateAsync(application, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return BnplApplicationResult<BnplFinancingApplication>.Success(application);
        }
        catch (BnplFinancingDomainException ex)
        {
            return BnplFinancingResults.FromDomain<BnplFinancingApplication>(ex);
        }
        catch (BnplPersistenceConflictException ex)
        {
            return BnplFinancingResults.FromPersistence<BnplFinancingApplication>(ex);
        }
    }
}

public sealed class DeclineBnplFinancingEligibility
{
    private readonly IBnplFinancingApplicationRepository _applications;
    private readonly IBnplUnitOfWork _unitOfWork;
    private readonly IBnplClock _clock;

    public DeclineBnplFinancingEligibility(
        IBnplFinancingApplicationRepository applications,
        IBnplUnitOfWork unitOfWork,
        IBnplClock clock)
    {
        _applications = applications;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<BnplApplicationResult<BnplFinancingApplication>> ExecuteAsync(
        Guid organizationId,
        Guid applicationId,
        Guid actorId,
        string? note = null,
        int? expectedVersion = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var application = await _applications
                .GetByIdAsync(organizationId, BnplFinancingApplicationId.From(applicationId), cancellationToken)
                .ConfigureAwait(false);
            if (application is null)
            {
                return BnplApplicationResult<BnplFinancingApplication>.Failure(
                    BnplFinancingErrorCodes.NotFound,
                    "Financing application was not found in this organization.",
                    404);
            }

            application.DeclineEligibility(actorId, _clock.UtcNow, note, expectedVersion);
            await _applications.UpdateAsync(application, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return BnplApplicationResult<BnplFinancingApplication>.Success(application);
        }
        catch (BnplFinancingDomainException ex)
        {
            return BnplFinancingResults.FromDomain<BnplFinancingApplication>(ex);
        }
        catch (BnplPersistenceConflictException ex)
        {
            return BnplFinancingResults.FromPersistence<BnplFinancingApplication>(ex);
        }
    }
}

public sealed class CreateBnplFinancingOffer
{
    private readonly IBnplFinancingApplicationRepository _applications;
    private readonly IBnplUnitOfWork _unitOfWork;
    private readonly IBnplClock _clock;

    public CreateBnplFinancingOffer(
        IBnplFinancingApplicationRepository applications,
        IBnplUnitOfWork unitOfWork,
        IBnplClock clock)
    {
        _applications = applications;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<BnplApplicationResult<BnplFinancingApplication>> ExecuteAsync(
        Guid organizationId,
        Guid applicationId,
        Guid actorId,
        Guid? offerId = null,
        DateTimeOffset? expiresAtUtc = null,
        int? expectedVersion = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var application = await _applications
                .GetByIdAsync(organizationId, BnplFinancingApplicationId.From(applicationId), cancellationToken)
                .ConfigureAwait(false);
            if (application is null)
            {
                return BnplApplicationResult<BnplFinancingApplication>.Failure(
                    BnplFinancingErrorCodes.NotFound,
                    "Financing application was not found in this organization.",
                    404);
            }

            application.CreateOffer(
                actorId,
                _clock.UtcNow,
                offerId is null ? null : BnplFinancingOfferId.From(offerId.Value),
                expiresAtUtc,
                expectedVersion);
            await _applications.UpdateAsync(application, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return BnplApplicationResult<BnplFinancingApplication>.Success(application);
        }
        catch (BnplFinancingDomainException ex)
        {
            return BnplFinancingResults.FromDomain<BnplFinancingApplication>(ex);
        }
        catch (BnplPersistenceConflictException ex)
        {
            return BnplFinancingResults.FromPersistence<BnplFinancingApplication>(ex);
        }
    }
}

public sealed class AcceptBnplFinancingOffer
{
    private readonly IBnplFinancingApplicationRepository _applications;
    private readonly IBnplUnitOfWork _unitOfWork;
    private readonly IBnplClock _clock;

    public AcceptBnplFinancingOffer(
        IBnplFinancingApplicationRepository applications,
        IBnplUnitOfWork unitOfWork,
        IBnplClock clock)
    {
        _applications = applications;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<BnplApplicationResult<BnplFinancingApplication>> ExecuteAsync(
        Guid organizationId,
        Guid applicationId,
        Guid offerId,
        Guid actorId,
        int? expectedVersion = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var application = await _applications
                .GetByIdAsync(organizationId, BnplFinancingApplicationId.From(applicationId), cancellationToken)
                .ConfigureAwait(false);
            if (application is null)
            {
                return BnplApplicationResult<BnplFinancingApplication>.Failure(
                    BnplFinancingErrorCodes.NotFound,
                    "Financing application was not found in this organization.",
                    404);
            }

            application.AcceptOffer(offerId, actorId, _clock.UtcNow, expectedVersion);
            await _applications.UpdateAsync(application, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return BnplApplicationResult<BnplFinancingApplication>.Success(application);
        }
        catch (BnplFinancingDomainException ex)
        {
            return BnplFinancingResults.FromDomain<BnplFinancingApplication>(ex);
        }
        catch (BnplPersistenceConflictException ex)
        {
            return BnplFinancingResults.FromPersistence<BnplFinancingApplication>(ex);
        }
    }
}

public sealed class ApproveBnplFinancingApplication
{
    private readonly IBnplFinancingApplicationRepository _applications;
    private readonly IBnplUnitOfWork _unitOfWork;
    private readonly IBnplClock _clock;

    public ApproveBnplFinancingApplication(
        IBnplFinancingApplicationRepository applications,
        IBnplUnitOfWork unitOfWork,
        IBnplClock clock)
    {
        _applications = applications;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<BnplApplicationResult<BnplFinancingApplication>> ExecuteAsync(
        Guid organizationId,
        Guid applicationId,
        Guid actorId,
        string? note = null,
        int? expectedVersion = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var application = await _applications
                .GetByIdAsync(organizationId, BnplFinancingApplicationId.From(applicationId), cancellationToken)
                .ConfigureAwait(false);
            if (application is null)
            {
                return BnplApplicationResult<BnplFinancingApplication>.Failure(
                    BnplFinancingErrorCodes.NotFound,
                    "Financing application was not found in this organization.",
                    404);
            }

            application.Approve(actorId, _clock.UtcNow, note, expectedVersion);
            await _applications.UpdateAsync(application, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return BnplApplicationResult<BnplFinancingApplication>.Success(application);
        }
        catch (BnplFinancingDomainException ex)
        {
            return BnplFinancingResults.FromDomain<BnplFinancingApplication>(ex);
        }
        catch (BnplPersistenceConflictException ex)
        {
            return BnplFinancingResults.FromPersistence<BnplFinancingApplication>(ex);
        }
    }
}

public sealed class DeclineBnplFinancingApplication
{
    private readonly IBnplFinancingApplicationRepository _applications;
    private readonly IBnplUnitOfWork _unitOfWork;
    private readonly IBnplClock _clock;

    public DeclineBnplFinancingApplication(
        IBnplFinancingApplicationRepository applications,
        IBnplUnitOfWork unitOfWork,
        IBnplClock clock)
    {
        _applications = applications;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<BnplApplicationResult<BnplFinancingApplication>> ExecuteAsync(
        Guid organizationId,
        Guid applicationId,
        Guid actorId,
        string? note = null,
        int? expectedVersion = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var application = await _applications
                .GetByIdAsync(organizationId, BnplFinancingApplicationId.From(applicationId), cancellationToken)
                .ConfigureAwait(false);
            if (application is null)
            {
                return BnplApplicationResult<BnplFinancingApplication>.Failure(
                    BnplFinancingErrorCodes.NotFound,
                    "Financing application was not found in this organization.",
                    404);
            }

            application.DeclineApproval(actorId, _clock.UtcNow, note, expectedVersion);
            await _applications.UpdateAsync(application, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return BnplApplicationResult<BnplFinancingApplication>.Success(application);
        }
        catch (BnplFinancingDomainException ex)
        {
            return BnplFinancingResults.FromDomain<BnplFinancingApplication>(ex);
        }
        catch (BnplPersistenceConflictException ex)
        {
            return BnplFinancingResults.FromPersistence<BnplFinancingApplication>(ex);
        }
    }
}

public sealed class CancelBnplFinancingApplication
{
    private readonly IBnplFinancingApplicationRepository _applications;
    private readonly IBnplUnitOfWork _unitOfWork;
    private readonly IBnplClock _clock;

    public CancelBnplFinancingApplication(
        IBnplFinancingApplicationRepository applications,
        IBnplUnitOfWork unitOfWork,
        IBnplClock clock)
    {
        _applications = applications;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<BnplApplicationResult<BnplFinancingApplication>> ExecuteAsync(
        Guid organizationId,
        Guid applicationId,
        Guid actorId,
        string? note = null,
        int? expectedVersion = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var application = await _applications
                .GetByIdAsync(organizationId, BnplFinancingApplicationId.From(applicationId), cancellationToken)
                .ConfigureAwait(false);
            if (application is null)
            {
                return BnplApplicationResult<BnplFinancingApplication>.Failure(
                    BnplFinancingErrorCodes.NotFound,
                    "Financing application was not found in this organization.",
                    404);
            }

            application.Cancel(actorId, _clock.UtcNow, note, expectedVersion);
            await _applications.UpdateAsync(application, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return BnplApplicationResult<BnplFinancingApplication>.Success(application);
        }
        catch (BnplFinancingDomainException ex)
        {
            return BnplFinancingResults.FromDomain<BnplFinancingApplication>(ex);
        }
        catch (BnplPersistenceConflictException ex)
        {
            return BnplFinancingResults.FromPersistence<BnplFinancingApplication>(ex);
        }
    }
}

public sealed class AttachBnplInstallmentPlan
{
    private readonly IBnplFinancingApplicationRepository _applications;
    private readonly IBnplUnitOfWork _unitOfWork;
    private readonly IBnplClock _clock;

    public AttachBnplInstallmentPlan(
        IBnplFinancingApplicationRepository applications,
        IBnplUnitOfWork unitOfWork,
        IBnplClock clock)
    {
        _applications = applications;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<BnplApplicationResult<BnplFinancingApplication>> ExecuteAsync(
        Guid organizationId,
        Guid applicationId,
        Guid offerId,
        Guid planId,
        IReadOnlyList<BnplInstallmentPlanItemDraft> items,
        Guid actorId,
        int? expectedVersion = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var application = await _applications
                .GetByIdAsync(organizationId, BnplFinancingApplicationId.From(applicationId), cancellationToken)
                .ConfigureAwait(false);
            if (application is null)
            {
                return BnplApplicationResult<BnplFinancingApplication>.Failure(
                    BnplFinancingErrorCodes.NotFound,
                    "Financing application was not found in this organization.",
                    404);
            }

            application.AttachOrReplaceInstallmentPlan(
                offerId,
                BnplInstallmentPlanId.From(planId),
                items,
                actorId,
                _clock.UtcNow,
                expectedVersion);
            await _applications.UpdateAsync(application, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return BnplApplicationResult<BnplFinancingApplication>.Success(application);
        }
        catch (BnplFinancingDomainException ex)
        {
            return BnplFinancingResults.FromDomain<BnplFinancingApplication>(ex);
        }
        catch (BnplPersistenceConflictException ex)
        {
            return BnplFinancingResults.FromPersistence<BnplFinancingApplication>(ex);
        }
    }
}

public sealed class GetBnplInstallmentPlan
{
    private readonly IBnplFinancingApplicationRepository _applications;

    public GetBnplInstallmentPlan(IBnplFinancingApplicationRepository applications) =>
        _applications = applications;

    public async Task<BnplApplicationResult<(BnplFinancingApplication Application, BnplInstallmentPlan Plan)>> ExecuteAsync(
        Guid organizationId,
        Guid applicationId,
        Guid offerId,
        CancellationToken cancellationToken = default)
    {
        var application = await _applications
            .GetByIdAsync(organizationId, BnplFinancingApplicationId.From(applicationId), cancellationToken)
            .ConfigureAwait(false);
        if (application is null)
        {
            return BnplApplicationResult<(BnplFinancingApplication, BnplInstallmentPlan)>.Failure(
                BnplFinancingErrorCodes.NotFound,
                "Financing application was not found in this organization.",
                404);
        }

        if (application.Offers.All(o => o.Id.Value != offerId))
        {
            return BnplApplicationResult<(BnplFinancingApplication, BnplInstallmentPlan)>.Failure(
                BnplFinancingErrorCodes.NotFound,
                "Offer was not found on this application.",
                404);
        }

        var plan = application.GetInstallmentPlanForOffer(offerId);
        if (plan is null)
        {
            return BnplApplicationResult<(BnplFinancingApplication, BnplInstallmentPlan)>.Failure(
                BnplFinancingErrorCodes.NotFound,
                "Installment plan was not found for this offer.",
                404);
        }

        return BnplApplicationResult<(BnplFinancingApplication, BnplInstallmentPlan)>.Success((application, plan));
    }
}
