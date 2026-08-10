using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.Application.GlobalCatalog;

public sealed class BusinessTypeQueryService
{
    private readonly IBusinessTypeRepository _businessTypes;

    public BusinessTypeQueryService(IBusinessTypeRepository businessTypes) => _businessTypes = businessTypes;

    public async Task<BusinessTypeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _businessTypes.GetByIdAsync(BusinessTypeId.From(id), cancellationToken)
            .ConfigureAwait(false);
        return entity is null ? null : GlobalCatalogDtoMaps.Map(entity);
    }

    public async Task<PagedResult<BusinessTypeDto>> ListAsync(
        BusinessTypeStatus? status,
        string? search,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default,
        BusinessTypeListSortBy sortBy = BusinessTypeListSortBy.SortOrder,
        bool sortDescending = false)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, total) = await _businessTypes
            .ListAsync(status, search, skip, take, cancellationToken, sortBy, sortDescending)
            .ConfigureAwait(false);

        return new PagedResult<BusinessTypeDto>(
            items.Select(GlobalCatalogDtoMaps.Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public async Task<IReadOnlyList<BusinessTypeDto>> ListActiveForMerchantsAsync(
        CancellationToken cancellationToken = default)
    {
        var (items, _) = await _businessTypes
            .ListAsync(
                BusinessTypeStatus.Active,
                search: null,
                skip: 0,
                take: 500,
                cancellationToken,
                BusinessTypeListSortBy.SortOrder)
            .ConfigureAwait(false);
        return items.Select(GlobalCatalogDtoMaps.Map).ToList();
    }
}

public sealed class CreateBusinessType
{
    private readonly IBusinessTypeRepository _businessTypes;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateBusinessType(
        IBusinessTypeRepository businessTypes,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _businessTypes = businessTypes;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<BusinessTypeDto>> ExecuteAsync(
        CreateBusinessTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var code = GlobalCatalogRules.NormalizeBusinessTypeCode(request.Code);
            var name = GlobalCatalogRules.NormalizeName(request.Name);

            if (await _businessTypes.ExistsWithCodeAsync(code, excludingId: null, cancellationToken)
                    .ConfigureAwait(false))
            {
                return ApplicationResult<BusinessTypeDto>.Failure(
                    ApplicationErrorCodes.DuplicateBusinessTypeCode,
                    "A business type with this code already exists.");
            }

            if (await _businessTypes.ExistsWithNameAsync(name, excludingId: null, cancellationToken)
                    .ConfigureAwait(false))
            {
                return ApplicationResult<BusinessTypeDto>.Failure(
                    ApplicationErrorCodes.DuplicateBusinessTypeName,
                    "A business type with this name already exists.");
            }

            var entity = BusinessType.Create(
                code,
                name,
                _clock.UtcNow,
                request.Description,
                request.SortOrder,
                request.IconReference);

            await _businessTypes.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<BusinessTypeDto>.Success(GlobalCatalogDtoMaps.Map(entity));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<BusinessTypeDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<BusinessTypeDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UpdateBusinessType
{
    private readonly IBusinessTypeRepository _businessTypes;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateBusinessType(
        IBusinessTypeRepository businessTypes,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _businessTypes = businessTypes;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<BusinessTypeDto>> ExecuteAsync(
        Guid id,
        UpdateBusinessTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _businessTypes.GetByIdAsync(BusinessTypeId.From(id), cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            return ApplicationResult<BusinessTypeDto>.Failure(
                ApplicationErrorCodes.BusinessTypeNotFound,
                "Business type was not found.");
        }

        if (IsConcurrencyMismatch(entity.UpdatedAtUtc, request.ExpectedUpdatedAtUtc))
        {
            return ApplicationResult<BusinessTypeDto>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The business type was modified by another request. Refresh and try again.");
        }

        try
        {
            var now = _clock.UtcNow;
            var name = GlobalCatalogRules.NormalizeName(request.Name);
            if (await _businessTypes.ExistsWithNameAsync(name, entity.Id, cancellationToken)
                    .ConfigureAwait(false))
            {
                return ApplicationResult<BusinessTypeDto>.Failure(
                    ApplicationErrorCodes.DuplicateBusinessTypeName,
                    "A business type with this name already exists.");
            }

            entity.Rename(name, now);
            entity.SetDescription(request.Description, now);
            entity.SetSortOrder(request.SortOrder, now);
            entity.SetIcon(request.IconReference, now);

            await _businessTypes.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<BusinessTypeDto>.Success(GlobalCatalogDtoMaps.Map(entity));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<BusinessTypeDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<BusinessTypeDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    internal static bool IsConcurrencyMismatch(DateTimeOffset current, DateTimeOffset? expected) =>
        expected is not null
        && current.ToUnixTimeMilliseconds() != expected.Value.ToUnixTimeMilliseconds();
}

public sealed class SetBusinessTypeStatus
{
    private readonly IBusinessTypeRepository _businessTypes;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SetBusinessTypeStatus(
        IBusinessTypeRepository businessTypes,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _businessTypes = businessTypes;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<BusinessTypeDto>> ExecuteAsync(
        Guid id,
        SetBusinessTypeStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _businessTypes.GetByIdAsync(BusinessTypeId.From(id), cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            return ApplicationResult<BusinessTypeDto>.Failure(
                ApplicationErrorCodes.BusinessTypeNotFound,
                "Business type was not found.");
        }

        if (UpdateBusinessType.IsConcurrencyMismatch(entity.UpdatedAtUtc, request.ExpectedUpdatedAtUtc))
        {
            return ApplicationResult<BusinessTypeDto>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The business type was modified by another request. Refresh and try again.");
        }

        if (!Enum.TryParse<BusinessTypeStatus>(request.Status, ignoreCase: true, out var status))
        {
            return ApplicationResult<BusinessTypeDto>.Failure(
                DomainErrorCodes.InvalidGlobalCatalogBusinessType,
                $"Unrecognized business type status '{request.Status}'.");
        }

        try
        {
            entity.SetStatus(status, _clock.UtcNow);
            await _businessTypes.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<BusinessTypeDto>.Success(GlobalCatalogDtoMaps.Map(entity));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<BusinessTypeDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<BusinessTypeDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class BulkAssignCategoryBusinessTypes
{
    private readonly IGlobalCategoryRepository _categories;
    private readonly IBusinessTypeRepository _businessTypes;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public BulkAssignCategoryBusinessTypes(
        IGlobalCategoryRepository categories,
        IBusinessTypeRepository businessTypes,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _categories = categories;
        _businessTypes = businessTypes;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<GlobalCategoryDto>> ExecuteAsync(
        Guid categoryId,
        BulkAssignCategoryBusinessTypesRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await _categories.GetByIdAsync(GlobalCategoryId.From(categoryId), cancellationToken)
            .ConfigureAwait(false);
        if (category is null)
        {
            return ApplicationResult<GlobalCategoryDto>.Failure(
                ApplicationErrorCodes.GlobalCategoryNotFound,
                "Category was not found.");
        }

        if (UpdateGlobalCategory.IsConcurrencyMismatch(category.UpdatedAtUtc, request.ExpectedUpdatedAtUtc))
        {
            return ApplicationResult<GlobalCategoryDto>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The category was modified by another request. Refresh and try again.");
        }

        try
        {
            var mode = ParseMode(request.Mode);
            var ids = await BusinessTypeResolver
                .ResolveManyAsync(_businessTypes, request.BusinessTypes, request.BusinessTypeIds, cancellationToken)
                .ConfigureAwait(false);
            var now = _clock.UtcNow;

            switch (mode)
            {
                case BusinessTypeAssignmentMode.Add:
                    category.AddBusinessTypes(ids, now);
                    break;
                case BusinessTypeAssignmentMode.Remove:
                    category.RemoveBusinessTypes(ids, now);
                    break;
                default:
                    category.AssignBusinessTypes(ids, now);
                    break;
            }

            await _categories.UpdateAsync(category, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var codes = await BusinessTypeResolver
                .LoadCodeLookupAsync(_businessTypes, category.BusinessTypeIds, cancellationToken)
                .ConfigureAwait(false);
            return ApplicationResult<GlobalCategoryDto>.Success(GlobalCatalogDtoMaps.Map(category, codes));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<GlobalCategoryDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<GlobalCategoryDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private static BusinessTypeAssignmentMode ParseMode(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            return BusinessTypeAssignmentMode.Replace;
        }

        if (!Enum.TryParse<BusinessTypeAssignmentMode>(mode, ignoreCase: true, out var parsed)
            || !Enum.IsDefined(parsed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalCatalogBusinessType,
                $"Unrecognized assignment mode '{mode}'.");
        }

        return parsed;
    }
}
