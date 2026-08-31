using System.Text.Json;
using System.Text.Json.Serialization;
using ExItS.Platform.Application.Reference;

namespace ExItS.Platform.Infrastructure.Reference;

/// <summary>
/// Immutable in-memory PSGC City/Municipality directory loaded once from the versioned snapshot.
/// Does not call psa.gov.ph at runtime.
/// </summary>
public sealed class PhilippineLocalityDirectory : IPhilippineLocalityDirectory
{
    public const string EmbeddedResourceName =
        "ExItS.Platform.Infrastructure.ReferenceData.Philippines.psgc-localities-2026-06-30.json";

    public const int DefaultSearchLimit = 20;
    public const int MaxSearchLimit = 50;
    public const int MinQueryLength = 2;

    private readonly IReadOnlyDictionary<string, PhilippineLocality> _byCode;
    private readonly IReadOnlyList<PhilippineLocality> _all;

    public PhilippineLocalityDirectoryMetadata Metadata { get; }

    public PhilippineLocalityDirectory()
        : this(LoadEmbeddedSnapshot())
    {
    }

    internal PhilippineLocalityDirectory(PsgcSnapshotDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(document.Metadata);
        ArgumentNullException.ThrowIfNull(document.Localities);

        var localities = new List<PhilippineLocality>(document.Localities.Count);
        var byCode = new Dictionary<string, PhilippineLocality>(StringComparer.Ordinal);

        foreach (var row in document.Localities)
        {
            if (string.IsNullOrWhiteSpace(row.PsgcCode) || string.IsNullOrWhiteSpace(row.Name))
            {
                throw new InvalidOperationException("PSGC snapshot contains a blank code or name.");
            }

            if (!TryParseType(row.LocalityType, out var type))
            {
                throw new InvalidOperationException($"PSGC snapshot has unsupported locality type '{row.LocalityType}'.");
            }

            if (string.IsNullOrWhiteSpace(row.RegionCode) || string.IsNullOrWhiteSpace(row.RegionName))
            {
                throw new InvalidOperationException($"PSGC locality {row.PsgcCode} is missing region.");
            }

            var locality = new PhilippineLocality(
                row.PsgcCode.Trim(),
                row.Name.Trim(),
                type,
                row.RegionCode.Trim(),
                row.RegionName.Trim(),
                string.IsNullOrWhiteSpace(row.ProvinceCode) ? null : row.ProvinceCode.Trim(),
                string.IsNullOrWhiteSpace(row.ProvinceName) ? null : row.ProvinceName.Trim());

            if (!byCode.TryAdd(locality.PsgcCode, locality))
            {
                throw new InvalidOperationException($"Duplicate PSGC code in snapshot: {locality.PsgcCode}");
            }

            localities.Add(locality);
        }

        localities.Sort(static (a, b) => string.CompareOrdinal(a.PsgcCode, b.PsgcCode));
        _all = localities;
        _byCode = byCode;
        Metadata = new PhilippineLocalityDirectoryMetadata(
            document.Metadata.Source,
            document.Metadata.Dataset,
            document.Metadata.AsOf,
            document.Metadata.Release,
            document.Metadata.Country,
            document.Metadata.DatasetVersion,
            localities.Count);
    }

    public PhilippineLocality? GetByPsgcCode(string psgcCode)
    {
        if (string.IsNullOrWhiteSpace(psgcCode))
        {
            return null;
        }

        return _byCode.TryGetValue(psgcCode.Trim(), out var locality) ? locality : null;
    }

    public bool Contains(string psgcCode) => GetByPsgcCode(psgcCode) is not null;

    public IReadOnlyList<PhilippineLocality> Search(string query, int limit = DefaultSearchLimit)
    {
        var capped = Math.Clamp(limit <= 0 ? DefaultSearchLimit : limit, 1, MaxSearchLimit);
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<PhilippineLocality>();
        }

        var normalized = Collapse(query);
        if (normalized.Length < MinQueryLength)
        {
            return Array.Empty<PhilippineLocality>();
        }

        var exact = new List<PhilippineLocality>();
        var prefix = new List<PhilippineLocality>();
        var nameContains = new List<PhilippineLocality>();
        var geoContains = new List<PhilippineLocality>();

        foreach (var locality in _all)
        {
            var name = Collapse(locality.Name);
            var friendly = Collapse(PhilippineLocality.FriendlyName(locality.Name));
            var province = Collapse(locality.ProvinceName ?? string.Empty);
            var region = Collapse(locality.RegionName);

            if (name == normalized || friendly == normalized)
            {
                exact.Add(locality);
                continue;
            }

            if (name.StartsWith(normalized, StringComparison.Ordinal) ||
                friendly.StartsWith(normalized, StringComparison.Ordinal))
            {
                prefix.Add(locality);
                continue;
            }

            if (name.Contains(normalized, StringComparison.Ordinal) ||
                friendly.Contains(normalized, StringComparison.Ordinal))
            {
                nameContains.Add(locality);
                continue;
            }

            if (province.Contains(normalized, StringComparison.Ordinal) ||
                region.Contains(normalized, StringComparison.Ordinal))
            {
                geoContains.Add(locality);
            }
        }

        return exact
            .Concat(prefix)
            .Concat(nameContains)
            .Concat(geoContains)
            .Take(capped)
            .ToList();
    }

    private static string Collapse(string value)
    {
        var chars = value.Trim().ToLowerInvariant().ToCharArray();
        var buffer = new char[chars.Length];
        var w = 0;
        var prevSpace = false;
        foreach (var c in chars)
        {
            if (char.IsWhiteSpace(c))
            {
                if (w == 0 || prevSpace)
                {
                    continue;
                }

                buffer[w++] = ' ';
                prevSpace = true;
                continue;
            }

            buffer[w++] = c;
            prevSpace = false;
        }

        return new string(buffer, 0, w);
    }

    private static bool TryParseType(string? raw, out PhilippineLocalityType type)
    {
        if (string.Equals(raw, "City", StringComparison.OrdinalIgnoreCase))
        {
            type = PhilippineLocalityType.City;
            return true;
        }

        if (string.Equals(raw, "Municipality", StringComparison.OrdinalIgnoreCase))
        {
            type = PhilippineLocalityType.Municipality;
            return true;
        }

        type = default;
        return false;
    }

    private static PsgcSnapshotDocument LoadEmbeddedSnapshot()
    {
        var assembly = typeof(PhilippineLocalityDirectory).Assembly;
        using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded PSGC snapshot '{EmbeddedResourceName}' was not found.");
        var document = JsonSerializer.Deserialize<PsgcSnapshotDocument>(stream, SnapshotJsonOptions)
            ?? throw new InvalidOperationException("PSGC snapshot JSON deserialized to null.");
        return document;
    }

    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
}

internal sealed class PsgcSnapshotDocument
{
    [JsonPropertyName("metadata")]
    public PsgcSnapshotMetadata Metadata { get; set; } = null!;

    [JsonPropertyName("localities")]
    public List<PsgcSnapshotLocality> Localities { get; set; } = null!;
}

internal sealed class PsgcSnapshotMetadata
{
    [JsonPropertyName("source")]
    public string Source { get; set; } = "";

    [JsonPropertyName("dataset")]
    public string Dataset { get; set; } = "";

    [JsonPropertyName("asOf")]
    public string AsOf { get; set; } = "";

    [JsonPropertyName("release")]
    public string Release { get; set; } = "";

    [JsonPropertyName("country")]
    public string Country { get; set; } = "";

    [JsonPropertyName("datasetVersion")]
    public string DatasetVersion { get; set; } = "";

    [JsonPropertyName("recordCount")]
    public int RecordCount { get; set; }
}

internal sealed class PsgcSnapshotLocality
{
    [JsonPropertyName("psgcCode")]
    public string PsgcCode { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("localityType")]
    public string LocalityType { get; set; } = "";

    [JsonPropertyName("regionCode")]
    public string RegionCode { get; set; } = "";

    [JsonPropertyName("regionName")]
    public string RegionName { get; set; } = "";

    [JsonPropertyName("provinceCode")]
    public string? ProvinceCode { get; set; }

    [JsonPropertyName("provinceName")]
    public string? ProvinceName { get; set; }
}
