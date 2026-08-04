using ClosedXML.Excel;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.Infrastructure.GlobalCatalog;

/// <summary>
/// Safe catalog import parser: CSV via Application helper; XLSX via ClosedXML
/// (no VBA/macros; formulas are never evaluated — cached/display text only, then sanitized upstream).
/// </summary>
internal sealed class CatalogImportFileParser : ICatalogImportFileParser
{
    public Task<IReadOnlyList<CatalogImportRawRow>> ParseAsync(
        Stream content,
        CatalogImportFileFormat format,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return format switch
        {
            CatalogImportFileFormat.Csv => Task.FromResult(CatalogImportCsvParser.Parse(content)),
            CatalogImportFileFormat.Xlsx => Task.FromResult(ParseXlsx(content)),
            _ => throw new DomainException(
                DomainErrorCodes.CatalogImportFileInvalid,
                $"Unsupported import format '{format}'.")
        };
    }

    private static IReadOnlyList<CatalogImportRawRow> ParseXlsx(Stream content)
    {
        try
        {
            using var workbook = new XLWorkbook(content);
            var worksheet = workbook.Worksheets.FirstOrDefault()
                ?? throw new DomainException(
                    DomainErrorCodes.CatalogImportFileInvalid,
                    "XLSX workbook has no worksheets.");

            var used = worksheet.RangeUsed();
            if (used is null)
            {
                throw new DomainException(
                    DomainErrorCodes.CatalogImportFileInvalid,
                    "XLSX worksheet is empty.");
            }

            var firstRow = used.FirstRow().RowNumber();
            var lastRow = used.LastRow().RowNumber();
            var firstCol = used.FirstColumn().ColumnNumber();
            var lastCol = used.LastColumn().ColumnNumber();

            var headers = new List<string>();
            for (var col = firstCol; col <= lastCol; col++)
            {
                headers.Add(ReadCellText(worksheet.Cell(firstRow, col)));
            }

            if (headers.Count == 0 || headers.All(string.IsNullOrWhiteSpace))
            {
                throw new DomainException(
                    DomainErrorCodes.CatalogImportFileInvalid,
                    "XLSX header row is empty.");
            }

            var rows = new List<CatalogImportRawRow>();
            for (var row = firstRow + 1; row <= lastRow; row++)
            {
                var cells = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var anyValue = false;
                for (var i = 0; i < headers.Count; i++)
                {
                    var header = headers[i];
                    if (string.IsNullOrWhiteSpace(header))
                    {
                        continue;
                    }

                    var text = ReadCellText(worksheet.Cell(row, firstCol + i));
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        anyValue = true;
                    }

                    cells[header] = text;
                }

                if (!anyValue)
                {
                    continue;
                }

                rows.Add(new CatalogImportRawRow(row, cells));
                if (rows.Count > CatalogImportRules.MaxRows)
                {
                    throw new DomainException(
                        DomainErrorCodes.CatalogImportFileInvalid,
                        $"XLSX exceeds the maximum of {CatalogImportRules.MaxRows} data rows.");
                }
            }

            return rows;
        }
        catch (DomainException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportFileInvalid,
                $"Unable to read XLSX file: {ex.Message}");
        }
    }

    /// <summary>
    /// Reads cell text without evaluating formulas. If the cell has a formula, uses cached value
    /// when present; otherwise returns the formula text (which upstream formula-injection checks reject).
    /// </summary>
    private static string ReadCellText(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return string.Empty;
        }

        if (cell.HasFormula)
        {
            try
            {
                // Prefer cached calculated value; never call workbook calculation engines.
                var cached = cell.CachedValue;
                if (!cached.IsBlank)
                {
                    return cached.ToString()?.Trim() ?? string.Empty;
                }
            }
            catch
            {
                // Fall through to formula text — will be sanitized/rejected as injection.
            }

            return cell.FormulaA1 ?? string.Empty;
        }

        return cell.GetFormattedString()?.Trim() ?? string.Empty;
    }
}
