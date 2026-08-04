using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.Application.GlobalCatalog;

public sealed class CatalogTemplateQueryService
{
    private readonly ICatalogTemplateRepository _templates;

    public CatalogTemplateQueryService(ICatalogTemplateRepository templates) => _templates = templates;

    public async Task<CatalogTemplateDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await _templates.GetByIdAsync(CatalogTemplateId.From(id), cancellationToken)
            .ConfigureAwait(false);
        return template is null ? null : GlobalCatalogDtoMaps.Map(template);
    }

    public async Task<PagedResult<CatalogTemplateSummaryDto>> ListAsync(
        CatalogTemplateStatus? status,
        BusinessType? primaryBusinessType,
        string? search,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, total) = await _templates
            .ListAsync(status, primaryBusinessType, search, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<CatalogTemplateSummaryDto>(
            items.Select(GlobalCatalogDtoMaps.MapSummary).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public async Task<PagedResult<CatalogTemplateSummaryDto>> ListPublishedForMerchantsAsync(
        BusinessType? primaryBusinessType,
        string? search,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default) =>
        await ListAsync(
                CatalogTemplateStatus.Published,
                primaryBusinessType,
                search,
                page,
                pageSize,
                cancellationToken)
            .ConfigureAwait(false);

    public async Task<CatalogTemplateDto?> GetPublishedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var template = await _templates.GetByIdAsync(CatalogTemplateId.From(id), cancellationToken)
            .ConfigureAwait(false);
        if (template is null || template.Status != CatalogTemplateStatus.Published)
        {
            return null;
        }

        return GlobalCatalogDtoMaps.Map(template);
    }
}

public sealed class CreateCatalogTemplate
{
    private readonly ICatalogTemplateRepository _templates;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateCatalogTemplate(
        ICatalogTemplateRepository templates,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _templates = templates;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<CatalogTemplateDto>> ExecuteAsync(
        CreateCatalogTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var businessType = CatalogTemplateUseCaseHelpers.ParseBusinessType(request.PrimaryBusinessType);
            var selectionMode = CatalogTemplateUseCaseHelpers.ParseSelectionMode(
                request.SelectionMode,
                SelectionMode.Curated);
            var slug = GlobalCatalogRules.NormalizeSlug(request.Slug ?? request.Name);

            if (await _templates.ExistsWithSlugAsync(slug, excludingId: null, cancellationToken)
                    .ConfigureAwait(false))
            {
                return ApplicationResult<CatalogTemplateDto>.Failure(
                    ApplicationErrorCodes.DuplicateCatalogTemplateSlug,
                    "A template with this slug already exists.");
            }

            var template = CatalogTemplate.Create(
                request.Name,
                businessType,
                _clock.UtcNow,
                slug,
                request.Description,
                request.IconReference,
                request.DefaultBatchSize,
                selectionMode);

            await _templates.AddAsync(template, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<CatalogTemplateDto>.Success(GlobalCatalogDtoMaps.Map(template));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UpdateCatalogTemplate
{
    private readonly ICatalogTemplateRepository _templates;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateCatalogTemplate(
        ICatalogTemplateRepository templates,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _templates = templates;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<CatalogTemplateDto>> ExecuteAsync(
        Guid id,
        UpdateCatalogTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var template = await _templates.GetByIdAsync(CatalogTemplateId.From(id), cancellationToken)
            .ConfigureAwait(false);
        if (template is null)
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(
                ApplicationErrorCodes.CatalogTemplateNotFound,
                "Template was not found.");
        }

        if (IsConcurrencyMismatch(template.UpdatedAtUtc, request.ExpectedUpdatedAtUtc))
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The template was modified by another request. Refresh and try again.");
        }

        try
        {
            var businessType = CatalogTemplateUseCaseHelpers.ParseBusinessType(request.PrimaryBusinessType);
            SelectionMode? selectionMode = null;
            if (!string.IsNullOrWhiteSpace(request.SelectionMode))
            {
                selectionMode = CatalogTemplateUseCaseHelpers.ParseSelectionMode(
                    request.SelectionMode,
                    template.SelectionMode);
            }

            var slug = GlobalCatalogRules.NormalizeSlug(request.Slug ?? request.Name);
            if (await _templates
                    .ExistsWithSlugAsync(slug, excludingId: template.Id, cancellationToken)
                    .ConfigureAwait(false))
            {
                return ApplicationResult<CatalogTemplateDto>.Failure(
                    ApplicationErrorCodes.DuplicateCatalogTemplateSlug,
                    "A template with this slug already exists.");
            }

            template.Update(
                request.Name,
                businessType,
                _clock.UtcNow,
                slug,
                request.Description,
                request.IconReference,
                request.DefaultBatchSize,
                selectionMode);

            await _templates.UpdateAsync(template, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<CatalogTemplateDto>.Success(GlobalCatalogDtoMaps.Map(template));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    internal static bool IsConcurrencyMismatch(DateTimeOffset current, DateTimeOffset? expected) =>
        expected is not null
        && current.ToUnixTimeMilliseconds() != expected.Value.ToUnixTimeMilliseconds();
}

public sealed class PublishCatalogTemplate
{
    private readonly ICatalogTemplateRepository _templates;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public PublishCatalogTemplate(
        ICatalogTemplateRepository templates,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _templates = templates;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public Task<ApplicationResult<CatalogTemplateDto>> ExecuteAsync(
        Guid id,
        CatalogTemplateLifecycleRequest? request = null,
        CancellationToken cancellationToken = default) =>
        CatalogTemplateLifecycleHelper.ExecuteAsync(
            _templates,
            _unitOfWork,
            id,
            request?.ExpectedUpdatedAtUtc,
            (template, now) => template.Publish(now),
            _clock,
            cancellationToken);
}

public sealed class UnpublishCatalogTemplate
{
    private readonly ICatalogTemplateRepository _templates;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UnpublishCatalogTemplate(
        ICatalogTemplateRepository templates,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _templates = templates;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public Task<ApplicationResult<CatalogTemplateDto>> ExecuteAsync(
        Guid id,
        CatalogTemplateLifecycleRequest? request = null,
        CancellationToken cancellationToken = default) =>
        CatalogTemplateLifecycleHelper.ExecuteAsync(
            _templates,
            _unitOfWork,
            id,
            request?.ExpectedUpdatedAtUtc,
            (template, now) => template.Unpublish(now),
            _clock,
            cancellationToken);
}

public sealed class ArchiveCatalogTemplate
{
    private readonly ICatalogTemplateRepository _templates;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ArchiveCatalogTemplate(
        ICatalogTemplateRepository templates,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _templates = templates;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public Task<ApplicationResult<CatalogTemplateDto>> ExecuteAsync(
        Guid id,
        CatalogTemplateLifecycleRequest? request = null,
        CancellationToken cancellationToken = default) =>
        CatalogTemplateLifecycleHelper.ExecuteAsync(
            _templates,
            _unitOfWork,
            id,
            request?.ExpectedUpdatedAtUtc,
            (template, now) => template.Archive(now),
            _clock,
            cancellationToken);
}

public sealed class AssignCatalogTemplateProduct
{
    private readonly ICatalogTemplateRepository _templates;
    private readonly IGlobalProductRepository _products;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AssignCatalogTemplateProduct(
        ICatalogTemplateRepository templates,
        IGlobalProductRepository products,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _templates = templates;
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<CatalogTemplateDto>> ExecuteAsync(
        Guid templateId,
        AssignCatalogTemplateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var template = await _templates.GetByIdAsync(CatalogTemplateId.From(templateId), cancellationToken)
            .ConfigureAwait(false);
        if (template is null)
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(
                ApplicationErrorCodes.CatalogTemplateNotFound,
                "Template was not found.");
        }

        if (UpdateCatalogTemplate.IsConcurrencyMismatch(template.UpdatedAtUtc, request.ExpectedUpdatedAtUtc))
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The template was modified by another request. Refresh and try again.");
        }

        var productId = GlobalProductId.From(request.GlobalProductId);
        var product = await _products.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(
                ApplicationErrorCodes.GlobalProductNotFound,
                "Global product was not found.");
        }

        try
        {
            template.AssignProduct(
                productId,
                _clock.UtcNow,
                request.IsFeatured,
                request.IsFirstBatch,
                request.SortOrder);

            await _templates.UpdateAsync(template, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<CatalogTemplateDto>.Success(GlobalCatalogDtoMaps.Map(template));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class RemoveCatalogTemplateProduct
{
    private readonly ICatalogTemplateRepository _templates;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RemoveCatalogTemplateProduct(
        ICatalogTemplateRepository templates,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _templates = templates;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<CatalogTemplateDto>> ExecuteAsync(
        Guid templateId,
        Guid globalProductId,
        DateTimeOffset? expectedUpdatedAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        var template = await _templates.GetByIdAsync(CatalogTemplateId.From(templateId), cancellationToken)
            .ConfigureAwait(false);
        if (template is null)
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(
                ApplicationErrorCodes.CatalogTemplateNotFound,
                "Template was not found.");
        }

        if (UpdateCatalogTemplate.IsConcurrencyMismatch(template.UpdatedAtUtc, expectedUpdatedAtUtc))
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The template was modified by another request. Refresh and try again.");
        }

        try
        {
            template.RemoveProduct(GlobalProductId.From(globalProductId), _clock.UtcNow);
            await _templates.UpdateAsync(template, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<CatalogTemplateDto>.Success(GlobalCatalogDtoMaps.Map(template));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ReorderCatalogTemplateProducts
{
    private readonly ICatalogTemplateRepository _templates;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReorderCatalogTemplateProducts(
        ICatalogTemplateRepository templates,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _templates = templates;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<CatalogTemplateDto>> ExecuteAsync(
        Guid templateId,
        ReorderCatalogTemplateProductsRequest request,
        CancellationToken cancellationToken = default)
    {
        var template = await _templates.GetByIdAsync(CatalogTemplateId.From(templateId), cancellationToken)
            .ConfigureAwait(false);
        if (template is null)
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(
                ApplicationErrorCodes.CatalogTemplateNotFound,
                "Template was not found.");
        }

        if (UpdateCatalogTemplate.IsConcurrencyMismatch(template.UpdatedAtUtc, request.ExpectedUpdatedAtUtc))
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The template was modified by another request. Refresh and try again.");
        }

        try
        {
            var ordered = request.OrderedGlobalProductIds
                .Select(GlobalProductId.From)
                .ToList();
            template.ReorderProducts(ordered, _clock.UtcNow);
            await _templates.UpdateAsync(template, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<CatalogTemplateDto>.Success(GlobalCatalogDtoMaps.Map(template));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UpdateCatalogTemplateProductFlags
{
    private readonly ICatalogTemplateRepository _templates;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateCatalogTemplateProductFlags(
        ICatalogTemplateRepository templates,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _templates = templates;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<CatalogTemplateDto>> ExecuteAsync(
        Guid templateId,
        Guid globalProductId,
        UpdateCatalogTemplateProductFlagsRequest request,
        CancellationToken cancellationToken = default)
    {
        var template = await _templates.GetByIdAsync(CatalogTemplateId.From(templateId), cancellationToken)
            .ConfigureAwait(false);
        if (template is null)
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(
                ApplicationErrorCodes.CatalogTemplateNotFound,
                "Template was not found.");
        }

        if (UpdateCatalogTemplate.IsConcurrencyMismatch(template.UpdatedAtUtc, request.ExpectedUpdatedAtUtc))
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The template was modified by another request. Refresh and try again.");
        }

        try
        {
            template.SetProductFlags(
                GlobalProductId.From(globalProductId),
                _clock.UtcNow,
                request.IsFeatured,
                request.IsFirstBatch);
            await _templates.UpdateAsync(template, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<CatalogTemplateDto>.Success(GlobalCatalogDtoMaps.Map(template));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

internal static class CatalogTemplateLifecycleHelper
{
    public static async Task<ApplicationResult<CatalogTemplateDto>> ExecuteAsync(
        ICatalogTemplateRepository templates,
        IPlatformUnitOfWork unitOfWork,
        Guid id,
        DateTimeOffset? expectedUpdatedAtUtc,
        Action<CatalogTemplate, DateTimeOffset> mutate,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var template = await templates.GetByIdAsync(CatalogTemplateId.From(id), cancellationToken)
            .ConfigureAwait(false);
        if (template is null)
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(
                ApplicationErrorCodes.CatalogTemplateNotFound,
                "Template was not found.");
        }

        if (UpdateCatalogTemplate.IsConcurrencyMismatch(template.UpdatedAtUtc, expectedUpdatedAtUtc))
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The template was modified by another request. Refresh and try again.");
        }

        try
        {
            mutate(template, clock.UtcNow);
            await templates.UpdateAsync(template, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<CatalogTemplateDto>.Success(GlobalCatalogDtoMaps.Map(template));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CatalogTemplateDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

internal static class CatalogTemplateUseCaseHelpers
{
    public static BusinessType ParseBusinessType(string value)
    {
        if (!Enum.TryParse<BusinessType>(value, ignoreCase: true, out var type))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalCatalogBusinessType,
                $"Unrecognized business type '{value}'.");
        }

        return GlobalCatalogRules.NormalizePrimaryBusinessType(type);
    }

    public static SelectionMode ParseSelectionMode(string? value, SelectionMode fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        if (!Enum.TryParse<SelectionMode>(value, ignoreCase: true, out var mode))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogTemplateSelectionMode,
                $"Unrecognized selection mode '{value}'.");
        }

        return GlobalCatalogRules.NormalizeSelectionMode(mode);
    }
}
