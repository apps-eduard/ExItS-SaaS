using System.Text.Json;
using System.Text.Json.Serialization;
using ExItS.Platform.Application.Geography;

namespace ExItS.Platform.Infrastructure.Geography;

/// <summary>
/// Barangays from the versioned PSGC snapshot, grouped by city or municipality code.
/// Does not call psa.gov.ph at runtime.
/// </summary>
public sealed class PhilippineBarangayDirectory
{
    public const string EmbeddedResourceName =
        "ExItS.Platform.Infrastructure.ReferenceData.Philippines.psgc-barangays-2026-06-30.json";

    private readonly IReadOnlyDictionary<string, IReadOnlyList<GeographyBarangayDto>> _byCity;

    public PhilippineBarangayDirectory()
        : this(LoadEmbeddedSnapshot())
    {
    }

    internal PhilippineBarangayDirectory(BarangaySnapshotDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(document.ByCity);
        var byCity = new Dictionary<string, IReadOnlyList<GeographyBarangayDto>>(StringComparer.Ordinal);
        foreach (var (cityId, rows) in document.ByCity)
        {
            if (string.IsNullOrWhiteSpace(cityId) || rows is null)
            {
                continue;
            }

            var barangays = new List<GeographyBarangayDto>(rows.Count);
            foreach (var row in rows)
            {
                if (row is not { Count: >= 2 } || string.IsNullOrWhiteSpace(row[0]) || string.IsNullOrWhiteSpace(row[1]))
                {
                    continue;
                }

                var code = row[0].Trim();
                barangays.Add(new GeographyBarangayDto(code, cityId.Trim(), code, row[1].Trim()));
            }

            barangays.Sort(static (left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
            byCity[cityId.Trim()] = barangays;
        }

        _byCity = byCity;
    }

    public IReadOnlyList<GeographyBarangayDto> ListByCity(string? cityId)
    {
        if (string.IsNullOrWhiteSpace(cityId))
        {
            return [];
        }

        return _byCity.TryGetValue(cityId.Trim(), out var rows) ? rows : [];
    }

    private static BarangaySnapshotDocument LoadEmbeddedSnapshot()
    {
        var assembly = typeof(PhilippineBarangayDirectory).Assembly;
        using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName)
            ?? throw new InvalidOperationException($"Embedded PSGC barangay snapshot '{EmbeddedResourceName}' was not found.");
        return JsonSerializer.Deserialize<BarangaySnapshotDocument>(stream, BarangaySnapshotDocument.JsonOptions)
            ?? throw new InvalidOperationException("PSGC barangay snapshot is empty.");
    }
}

public sealed class BarangaySnapshotDocument
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public Dictionary<string, List<List<string>>>? ByCity { get; set; }
}
