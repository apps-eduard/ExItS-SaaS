using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Organization-owned physical stock count. Draft lines are editable; start snapshots on-hand
/// and allocates a count number; complete posts immutable variance movements.
/// </summary>
public sealed class StockCount
{
    /// <summary>Safe display/persistence value for counts created before titles existed.</summary>
    public const string HistoricalTitle = "Stock count";

    public const int TitleMaxLength = 80;
    public const int NotesMaxLength = 512;
    public const int MaxLineCount = 500;

    private readonly List<StockCountLine> _lines;

    public StockCountId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public string? CountNumber { get; private set; }
    public StockCountStatus Status { get; private set; }
    public DateOnly CountDate { get; private set; }
    public string Title { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public Guid? StartedBy { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public Guid? CompletedBy { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public Guid? CancelledBy { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyList<StockCountLine> Lines => _lines;

    private StockCount(
        StockCountId id,
        PosOrganizationId organizationId,
        string? countNumber,
        StockCountStatus status,
        DateOnly countDate,
        string title,
        string? notes,
        DateTimeOffset? startedAtUtc,
        Guid? startedBy,
        DateTimeOffset? completedAtUtc,
        Guid? completedBy,
        DateTimeOffset? cancelledAtUtc,
        Guid? cancelledBy,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        List<StockCountLine> lines)
    {
        Id = id;
        OrganizationId = organizationId;
        CountNumber = countNumber;
        Status = status;
        CountDate = countDate;
        Title = title;
        Notes = notes;
        StartedAtUtc = startedAtUtc;
        StartedBy = startedBy;
        CompletedAtUtc = completedAtUtc;
        CompletedBy = completedBy;
        CancelledAtUtc = cancelledAtUtc;
        CancelledBy = cancelledBy;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        _lines = lines;
    }

    public static StockCount CreateDraft(
        PosOrganizationId organizationId,
        IReadOnlyList<StockCountLineDraft> lines,
        DateTimeOffset utcNow,
        string title,
        DateOnly? countDate = null,
        string? notes = null,
        StockCountId? id = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureLines(lines);

        var countId = id ?? StockCountId.New();
        var countLines = BuildDraftLines(countId, organizationId, lines);

        return new StockCount(
            countId,
            organizationId,
            countNumber: null,
            StockCountStatus.Draft,
            countDate ?? DateOnly.FromDateTime(utcNow.UtcDateTime),
            NormalizeTitle(title, allowHistoricalFallback: false),
            NormalizeNotes(notes),
            startedAtUtc: null,
            startedBy: null,
            completedAtUtc: null,
            completedBy: null,
            cancelledAtUtc: null,
            cancelledBy: null,
            utcNow,
            utcNow,
            countLines);
    }

    public void UpdateDraft(
        IReadOnlyList<StockCountLineDraft> lines,
        DateTimeOffset utcNow,
        DateOnly? countDate = null,
        string? notes = null,
        string? title = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureDraft();
        EnsureLines(lines);
        if (countDate is not null)
        {
            CountDate = countDate.Value;
        }

        if (title is not null)
        {
            Title = NormalizeTitle(title, allowHistoricalFallback: false);
        }

        Notes = NormalizeNotes(notes);
        ReplaceDraftLines(lines);
        UpdatedAtUtc = utcNow;
    }

    public void Start(
        string countNumber,
        IReadOnlyDictionary<Guid, decimal> onHandByProductId,
        Guid startedBy,
        DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(startedBy);
        EnsureDraft();
        EnsureLines(_lines);

        foreach (var line in _lines.OrderBy(l => l.LineNumber))
        {
            if (!onHandByProductId.TryGetValue(line.ProductId.Value, out var onHand))
            {
                throw new DomainException(
                    DomainErrorCodes.StockCountProductNotTracked,
                    "All count lines must reference tracked products with inventory accounts.");
            }

            line.ApplySnapshot(onHand);
        }

        CountNumber = StockCountNumbers.Normalize(countNumber);
        Status = StockCountStatus.InProgress;
        StartedAtUtc = utcNow;
        StartedBy = startedBy;
        UpdatedAtUtc = utcNow;
    }

    public void UpdateInProgressLines(
        IReadOnlyList<StockCountLineDraft> lines,
        IReadOnlyDictionary<Guid, UnitOfMeasure> unitByProductId,
        DateTimeOffset utcNow,
        IReadOnlyDictionary<Guid, SellingMode>? sellingModeByProductId = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureInProgress();
        EnsureLines(lines);

        var byProduct = _lines.ToDictionary(l => l.ProductId.Value);
        foreach (var draft in lines)
        {
            if (!byProduct.TryGetValue(draft.ProductId.Value, out var line))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidStockCountLine,
                    "Count line product is not on this stock count.");
            }

            if (draft.CountedQuantity is null)
            {
                continue;
            }

            if (!unitByProductId.TryGetValue(draft.ProductId.Value, out var unit))
            {
                throw new DomainException(
                    DomainErrorCodes.StockCountProductNotFound,
                    "Count line product was not found.");
            }

            var sellingMode = sellingModeByProductId is not null
                && sellingModeByProductId.TryGetValue(draft.ProductId.Value, out var mode)
                    ? mode
                    : SellingMode.PerItem;
            line.SetCountedQuantity(draft.CountedQuantity.Value, unit, sellingMode);
        }

        UpdatedAtUtc = utcNow;
    }

    public void MarkCompleted(Guid completedBy, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(completedBy);

        if (Status == StockCountStatus.Completed)
        {
            return;
        }

        EnsureInProgress();

        foreach (var line in _lines)
        {
            if (line.CountedQuantity is null)
            {
                throw new DomainException(
                    DomainErrorCodes.StockCountCountedQuantityRequired,
                    "All count lines must have a counted quantity before completion.");
            }
        }

        Status = StockCountStatus.Completed;
        CompletedAtUtc = utcNow;
        CompletedBy = completedBy;
        UpdatedAtUtc = utcNow;
    }

    public void Cancel(Guid cancelledBy, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(cancelledBy);

        if (Status is StockCountStatus.Completed or StockCountStatus.Cancelled)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockCountStatusTransition,
                "Completed stock counts cannot be cancelled.");
        }

        Status = StockCountStatus.Cancelled;
        CancelledAtUtc = utcNow;
        CancelledBy = cancelledBy;
        UpdatedAtUtc = utcNow;
    }

    private static void EnsureLines(IReadOnlyList<StockCountLineDraft> lines)
    {
        if (lines is null || lines.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.StockCountRequiresLines,
                "At least one count line is required.");
        }

        if (lines.Count > MaxLineCount)
        {
            throw new DomainException(
                DomainErrorCodes.StockCountRequiresLines,
                $"Stock count cannot exceed {MaxLineCount} lines.");
        }

        var seen = new HashSet<Guid>();
        foreach (var line in lines)
        {
            if (!seen.Add(line.ProductId.Value))
            {
                throw new DomainException(
                    DomainErrorCodes.StockCountDuplicateProduct,
                    "Duplicate products are not allowed on a stock count.");
            }
        }
    }

    private static void EnsureLines(IReadOnlyList<StockCountLine> lines)
    {
        if (lines.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.StockCountRequiresLines,
                "At least one count line is required.");
        }
    }

    private void EnsureDraft()
    {
        if (Status != StockCountStatus.Draft)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockCountStatusTransition,
                "Stock count is not in Draft status.");
        }
    }

    private void EnsureInProgress()
    {
        if (Status != StockCountStatus.InProgress)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockCountStatusTransition,
                "Stock count is not in progress.");
        }
    }

    private void ReplaceDraftLines(IReadOnlyList<StockCountLineDraft> lines)
    {
        _lines.Clear();
        _lines.AddRange(BuildDraftLines(Id, OrganizationId, lines));
    }

    private static List<StockCountLine> BuildDraftLines(
        StockCountId countId,
        PosOrganizationId organizationId,
        IReadOnlyList<StockCountLineDraft> lines)
    {
        var result = new List<StockCountLine>(lines.Count);
        var lineNumber = 1;
        foreach (var draft in lines.OrderBy(l => l.ProductId.Value))
        {
            result.Add(StockCountLine.CreateDraft(countId, organizationId, lineNumber++, draft.ProductId));
        }

        return result;
    }

    private static string NormalizeTitle(string? title, bool allowHistoricalFallback)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            if (allowHistoricalFallback)
            {
                return HistoricalTitle;
            }

            throw new DomainException(
                DomainErrorCodes.InvalidStockCountTitle,
                "Stock count title is required.");
        }

        var trimmed = title.Trim();
        if (trimmed.Length > TitleMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockCountTitle,
                $"Stock count title must be at most {TitleMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? NormalizeNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var trimmed = notes.Trim();
        if (trimmed.Length > NotesMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockCountNotes,
                $"Stock count notes must be at most {NotesMaxLength} characters.");
        }

        return trimmed;
    }

    public static StockCount Rehydrate(
        StockCountId id,
        PosOrganizationId organizationId,
        string? countNumber,
        StockCountStatus status,
        DateOnly countDate,
        string? title,
        string? notes,
        DateTimeOffset? startedAtUtc,
        Guid? startedBy,
        DateTimeOffset? completedAtUtc,
        Guid? completedBy,
        DateTimeOffset? cancelledAtUtc,
        Guid? cancelledBy,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        IReadOnlyList<StockCountLine> lines) =>
        new(
            id,
            organizationId,
            countNumber,
            status,
            countDate,
            NormalizeTitle(title, allowHistoricalFallback: true),
            notes,
            startedAtUtc,
            startedBy,
            completedAtUtc,
            completedBy,
            cancelledAtUtc,
            cancelledBy,
            createdAtUtc,
            updatedAtUtc,
            lines.ToList());
}
