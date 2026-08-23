using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.Application.Operations;

public sealed class PlatformBackgroundJobsQueryService
{
    private readonly CatalogImportQueryService _imports;

    public PlatformBackgroundJobsQueryService(CatalogImportQueryService imports)
    {
        _imports = imports;
    }

    public async Task<PagedResult<PlatformBackgroundJobDto>> ListAsync(
        string? status,
        string? search,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        CatalogImportJobStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<CatalogImportJobStatus>(status, ignoreCase: true, out var statusValue))
        {
            parsedStatus = statusValue;
        }

        var pageResult = await _imports
            .ListAsync(parsedStatus, page, pageSize, cancellationToken)
            .ConfigureAwait(false);

        var items = pageResult.Items.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = search.Trim();
            items = items.Where(item =>
                item.FileName.Contains(needle, StringComparison.OrdinalIgnoreCase)
                || item.Id.ToString("D").Contains(needle, StringComparison.OrdinalIgnoreCase)
                || (item.CurrentStage?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var mapped = items.Select(MapSummary).ToList();
        return new PagedResult<PlatformBackgroundJobDto>(
            mapped,
            string.IsNullOrWhiteSpace(search) ? pageResult.TotalCount : mapped.Count,
            pageResult.Page,
            pageResult.PageSize);
    }

    public async Task<PlatformBackgroundJobDetailDto?> GetAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _imports.GetByIdAsync(jobId, includePreview: false, cancellationToken).ConfigureAwait(false);
        if (job is null)
        {
            return null;
        }

        var summary = MapSummary(job);
        return new PlatformBackgroundJobDetailDto(
            summary,
            job.RequestedBy,
            job.FileFormat,
            job.FileSizeBytes,
            job.IdempotencyKey,
            job.LastHeartbeatAtUtc,
            job.PreviewSummary);
    }

    private static PlatformBackgroundJobDto MapSummary(CatalogImportJobDto job) =>
        new(
            job.Id,
            PlatformBackgroundJobSources.CatalogImport,
            "Global catalog import",
            job.Status,
            job.TotalCount,
            job.ProcessedCount,
            job.ImportedCount,
            job.SkippedCount,
            job.FailedCount,
            job.CurrentStage,
            job.ErrorSummary,
            job.CreatedAtUtc,
            job.StartedAtUtc,
            job.CompletedAtUtc,
            AttemptCount: null,
            DisplayName: job.FileName);
}
