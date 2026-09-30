using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public sealed record UpsertExpirySalePolicyRequest(int StopSellingDaysBeforeExpiry);

public sealed record ExpirySalePolicySettingDto(
    int StopSellingDaysBeforeExpiry,
    DateTimeOffset? UpdatedAtUtc,
    Guid? UpdatedBy,
    bool IsExplicit);

public sealed record ExpirySalePolicyCategoryOverrideDto(
    Guid CategoryId,
    int StopSellingDaysBeforeExpiry,
    DateTimeOffset UpdatedAtUtc,
    Guid UpdatedBy);

public sealed record EffectiveExpirySalePolicyDto(
    int StopSellingDaysBeforeExpiry,
    string Source,
    int OrganizationDefaultDays,
    int? OrganizationCategoryDays,
    int? BranchDefaultDays,
    int? BranchCategoryDays);

public sealed class GetOrganizationExpirySalePolicy
{
    private readonly IOrganizationExpirySalePolicyRepository _repository;

    public GetOrganizationExpirySalePolicy(IOrganizationExpirySalePolicyRepository repository) =>
        _repository = repository;

    public async Task<ExpirySalePolicySettingDto> ExecuteAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var setting = await _repository
            .GetAsync(PosOrganizationId.From(organizationId), cancellationToken)
            .ConfigureAwait(false);
        if (setting is null)
        {
            return new ExpirySalePolicySettingDto(
                InventoryLotSaleEligibility.DefaultStopSellingDays,
                null,
                null,
                IsExplicit: false);
        }

        return new ExpirySalePolicySettingDto(
            setting.StopSellingDaysBeforeExpiry,
            setting.UpdatedAtUtc,
            setting.UpdatedBy,
            IsExplicit: true);
    }
}

public sealed class PutOrganizationExpirySalePolicy
{
    private readonly IOrganizationExpirySalePolicyRepository _repository;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public PutOrganizationExpirySalePolicy(
        IOrganizationExpirySalePolicyRepository repository,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ExpirySalePolicySettingDto>> ExecuteAsync(
        Guid organizationId,
        UpsertExpirySalePolicyRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<ExpirySalePolicySettingDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to update the organization expiry sale policy.");
        }

        try
        {
            var orgId = PosOrganizationId.From(organizationId);
            var utcNow = _clock.UtcNow;
            var existing = await _repository.GetAsync(orgId, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                existing = OrganizationExpirySalePolicySetting.Create(
                    orgId,
                    request.StopSellingDaysBeforeExpiry,
                    actorId,
                    utcNow);
            }
            else
            {
                existing.SetStopSellingDays(request.StopSellingDaysBeforeExpiry, actorId, utcNow);
            }

            await _repository.UpsertAsync(existing, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<ExpirySalePolicySettingDto>.Success(
                new ExpirySalePolicySettingDto(
                    existing.StopSellingDaysBeforeExpiry,
                    existing.UpdatedAtUtc,
                    existing.UpdatedBy,
                    IsExplicit: true));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ExpirySalePolicySettingDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ListOrganizationCategoryExpirySalePolicies
{
    private readonly IOrganizationCategoryExpirySalePolicyRepository _repository;

    public ListOrganizationCategoryExpirySalePolicies(IOrganizationCategoryExpirySalePolicyRepository repository) =>
        _repository = repository;

    public async Task<IReadOnlyList<ExpirySalePolicyCategoryOverrideDto>> ExecuteAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var items = await _repository
            .ListByOrganizationAsync(PosOrganizationId.From(organizationId), cancellationToken)
            .ConfigureAwait(false);
        return items
            .Select(p => new ExpirySalePolicyCategoryOverrideDto(
                p.CategoryId,
                p.StopSellingDaysBeforeExpiry,
                p.UpdatedAtUtc,
                p.UpdatedBy))
            .ToList();
    }
}

public sealed class PutOrganizationCategoryExpirySalePolicy
{
    private readonly IOrganizationCategoryExpirySalePolicyRepository _repository;
    private readonly IProductCategoryRepository _categories;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public PutOrganizationCategoryExpirySalePolicy(
        IOrganizationCategoryExpirySalePolicyRepository repository,
        IProductCategoryRepository categories,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _repository = repository;
        _categories = categories;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ExpirySalePolicyCategoryOverrideDto>> ExecuteAsync(
        Guid organizationId,
        Guid categoryId,
        UpsertExpirySalePolicyRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<ExpirySalePolicyCategoryOverrideDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to update a category expiry sale policy.");
        }

        try
        {
            var orgId = PosOrganizationId.From(organizationId);
            var category = await _categories
                .GetByIdAsync(orgId, ProductCategoryId.From(categoryId), cancellationToken)
                .ConfigureAwait(false);
            if (category is null)
            {
                return ApplicationResult<ExpirySalePolicyCategoryOverrideDto>.Failure(
                    ApplicationErrorCodes.CategoryNotFound,
                    "Product category was not found.");
            }

            var utcNow = _clock.UtcNow;
            var existing = await _repository.GetAsync(orgId, categoryId, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                existing = OrganizationCategoryExpirySalePolicy.Create(
                    orgId,
                    categoryId,
                    request.StopSellingDaysBeforeExpiry,
                    actorId,
                    utcNow);
            }
            else
            {
                existing.SetStopSellingDays(request.StopSellingDaysBeforeExpiry, actorId, utcNow);
            }

            await _repository.UpsertAsync(existing, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<ExpirySalePolicyCategoryOverrideDto>.Success(
                new ExpirySalePolicyCategoryOverrideDto(
                    existing.CategoryId,
                    existing.StopSellingDaysBeforeExpiry,
                    existing.UpdatedAtUtc,
                    existing.UpdatedBy));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ExpirySalePolicyCategoryOverrideDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class DeleteOrganizationCategoryExpirySalePolicy
{
    private readonly IOrganizationCategoryExpirySalePolicyRepository _repository;
    private readonly IPosUnitOfWork _unitOfWork;

    public DeleteOrganizationCategoryExpirySalePolicy(
        IOrganizationCategoryExpirySalePolicyRepository repository,
        IPosUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApplicationResult> ExecuteAsync(
        Guid organizationId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        if (categoryId == Guid.Empty)
        {
            return ApplicationResult.Failure(
                DomainErrorCodes.InvalidExpirySalePolicyCategory,
                "Category id must be a non-empty GUID.");
        }

        await _repository
            .DeleteAsync(PosOrganizationId.From(organizationId), categoryId, cancellationToken)
            .ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult.Success();
    }
}

public sealed class GetBranchExpirySalePolicy
{
    private readonly IBranchExpirySalePolicyRepository _repository;

    public GetBranchExpirySalePolicy(IBranchExpirySalePolicyRepository repository) =>
        _repository = repository;

    public async Task<ExpirySalePolicySettingDto> ExecuteAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        var setting = await _repository
            .GetAsync(PosOrganizationId.From(organizationId), PosBranchId.From(branchId), cancellationToken)
            .ConfigureAwait(false);
        if (setting is null)
        {
            return new ExpirySalePolicySettingDto(
                InventoryLotSaleEligibility.DefaultStopSellingDays,
                null,
                null,
                IsExplicit: false);
        }

        return new ExpirySalePolicySettingDto(
            setting.StopSellingDaysBeforeExpiry,
            setting.UpdatedAtUtc,
            setting.UpdatedBy,
            IsExplicit: true);
    }
}

public sealed class PutBranchExpirySalePolicy
{
    private readonly IBranchExpirySalePolicyRepository _repository;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public PutBranchExpirySalePolicy(
        IBranchExpirySalePolicyRepository repository,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ExpirySalePolicySettingDto>> ExecuteAsync(
        Guid organizationId,
        Guid branchId,
        UpsertExpirySalePolicyRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<ExpirySalePolicySettingDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to update the branch expiry sale policy.");
        }

        if (branchId == Guid.Empty)
        {
            return ApplicationResult<ExpirySalePolicySettingDto>.Failure(
                ApplicationErrorCodes.InventoryBranchRequired,
                "A selected branch is required to update the branch expiry sale policy.");
        }

        try
        {
            var orgId = PosOrganizationId.From(organizationId);
            var branch = PosBranchId.From(branchId);
            var utcNow = _clock.UtcNow;
            var existing = await _repository.GetAsync(orgId, branch, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                existing = BranchExpirySalePolicySetting.Create(
                    orgId,
                    branch,
                    request.StopSellingDaysBeforeExpiry,
                    actorId,
                    utcNow);
            }
            else
            {
                existing.SetStopSellingDays(request.StopSellingDaysBeforeExpiry, actorId, utcNow);
            }

            await _repository.UpsertAsync(existing, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<ExpirySalePolicySettingDto>.Success(
                new ExpirySalePolicySettingDto(
                    existing.StopSellingDaysBeforeExpiry,
                    existing.UpdatedAtUtc,
                    existing.UpdatedBy,
                    IsExplicit: true));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ExpirySalePolicySettingDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class DeleteBranchExpirySalePolicy
{
    private readonly IBranchExpirySalePolicyRepository _repository;
    private readonly IPosUnitOfWork _unitOfWork;

    public DeleteBranchExpirySalePolicy(
        IBranchExpirySalePolicyRepository repository,
        IPosUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApplicationResult> ExecuteAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        if (branchId == Guid.Empty)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.InventoryBranchRequired,
                "A selected branch is required to clear the branch expiry sale policy.");
        }

        await _repository
            .DeleteAsync(PosOrganizationId.From(organizationId), PosBranchId.From(branchId), cancellationToken)
            .ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult.Success();
    }
}

public sealed class ListBranchCategoryExpirySalePolicies
{
    private readonly IBranchCategoryExpirySalePolicyRepository _repository;

    public ListBranchCategoryExpirySalePolicies(IBranchCategoryExpirySalePolicyRepository repository) =>
        _repository = repository;

    public async Task<IReadOnlyList<ExpirySalePolicyCategoryOverrideDto>> ExecuteAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        var items = await _repository
            .ListByBranchAsync(
                PosOrganizationId.From(organizationId),
                PosBranchId.From(branchId),
                cancellationToken)
            .ConfigureAwait(false);
        return items
            .Select(p => new ExpirySalePolicyCategoryOverrideDto(
                p.CategoryId,
                p.StopSellingDaysBeforeExpiry,
                p.UpdatedAtUtc,
                p.UpdatedBy))
            .ToList();
    }
}

public sealed class PutBranchCategoryExpirySalePolicy
{
    private readonly IBranchCategoryExpirySalePolicyRepository _repository;
    private readonly IProductCategoryRepository _categories;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public PutBranchCategoryExpirySalePolicy(
        IBranchCategoryExpirySalePolicyRepository repository,
        IProductCategoryRepository categories,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _repository = repository;
        _categories = categories;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ExpirySalePolicyCategoryOverrideDto>> ExecuteAsync(
        Guid organizationId,
        Guid branchId,
        Guid categoryId,
        UpsertExpirySalePolicyRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<ExpirySalePolicyCategoryOverrideDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to update a branch category expiry sale policy.");
        }

        if (branchId == Guid.Empty)
        {
            return ApplicationResult<ExpirySalePolicyCategoryOverrideDto>.Failure(
                ApplicationErrorCodes.InventoryBranchRequired,
                "A selected branch is required to update a branch category expiry sale policy.");
        }

        try
        {
            var orgId = PosOrganizationId.From(organizationId);
            var branch = PosBranchId.From(branchId);
            var category = await _categories
                .GetByIdAsync(orgId, ProductCategoryId.From(categoryId), cancellationToken)
                .ConfigureAwait(false);
            if (category is null)
            {
                return ApplicationResult<ExpirySalePolicyCategoryOverrideDto>.Failure(
                    ApplicationErrorCodes.CategoryNotFound,
                    "Product category was not found.");
            }

            var utcNow = _clock.UtcNow;
            var existing = await _repository
                .GetAsync(orgId, branch, categoryId, cancellationToken)
                .ConfigureAwait(false);
            if (existing is null)
            {
                existing = BranchCategoryExpirySalePolicy.Create(
                    orgId,
                    branch,
                    categoryId,
                    request.StopSellingDaysBeforeExpiry,
                    actorId,
                    utcNow);
            }
            else
            {
                existing.SetStopSellingDays(request.StopSellingDaysBeforeExpiry, actorId, utcNow);
            }

            await _repository.UpsertAsync(existing, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<ExpirySalePolicyCategoryOverrideDto>.Success(
                new ExpirySalePolicyCategoryOverrideDto(
                    existing.CategoryId,
                    existing.StopSellingDaysBeforeExpiry,
                    existing.UpdatedAtUtc,
                    existing.UpdatedBy));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ExpirySalePolicyCategoryOverrideDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class DeleteBranchCategoryExpirySalePolicy
{
    private readonly IBranchCategoryExpirySalePolicyRepository _repository;
    private readonly IPosUnitOfWork _unitOfWork;

    public DeleteBranchCategoryExpirySalePolicy(
        IBranchCategoryExpirySalePolicyRepository repository,
        IPosUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApplicationResult> ExecuteAsync(
        Guid organizationId,
        Guid branchId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        if (branchId == Guid.Empty)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.InventoryBranchRequired,
                "A selected branch is required to clear a branch category expiry sale policy.");
        }

        if (categoryId == Guid.Empty)
        {
            return ApplicationResult.Failure(
                DomainErrorCodes.InvalidExpirySalePolicyCategory,
                "Category id must be a non-empty GUID.");
        }

        await _repository
            .DeleteAsync(
                PosOrganizationId.From(organizationId),
                PosBranchId.From(branchId),
                categoryId,
                cancellationToken)
            .ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult.Success();
    }
}

public sealed class GetEffectiveExpirySalePolicy
{
    private readonly ExpirySalePolicyResolver _resolver;

    public GetEffectiveExpirySalePolicy(ExpirySalePolicyResolver resolver) =>
        _resolver = resolver;

    public async Task<EffectiveExpirySalePolicyDto> ExecuteAsync(
        Guid organizationId,
        Guid branchId,
        Guid? categoryId,
        CancellationToken cancellationToken = default)
    {
        var effective = await _resolver
            .ResolveAsync(
                PosOrganizationId.From(organizationId),
                PosBranchId.From(branchId),
                categoryId,
                cancellationToken)
            .ConfigureAwait(false);
        return new EffectiveExpirySalePolicyDto(
            effective.StopSellingDaysBeforeExpiry,
            effective.Source.ToString(),
            effective.OrganizationDefaultDays,
            effective.OrganizationCategoryDays,
            effective.BranchDefaultDays,
            effective.BranchCategoryDays);
    }
}
