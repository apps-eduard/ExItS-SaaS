namespace ExItS.Platform.Application.Reference;

public enum PhilippineLocalityType
{
    City = 1,
    Municipality = 2
}

public sealed record PhilippineLocality(
    string PsgcCode,
    string Name,
    PhilippineLocalityType Type,
    string RegionCode,
    string RegionName,
    string? ProvinceCode,
    string? ProvinceName)
{
    public string DisplayLabel
    {
        get
        {
            var friendly = FriendlyName(Name);
            if (!string.IsNullOrWhiteSpace(ProvinceName))
            {
                return $"{friendly} · {ProvinceName}";
            }

            return $"{friendly} · {RegionName}";
        }
    }

    public static string FriendlyName(string canonicalName)
    {
        if (string.IsNullOrWhiteSpace(canonicalName))
        {
            return canonicalName;
        }

        var name = canonicalName.Trim();
        // "City of Bacolod" → "Bacolod City" for merchant-friendly chips when safe.
        if (name.StartsWith("City of ", StringComparison.OrdinalIgnoreCase))
        {
            var rest = name["City of ".Length..].Trim();
            if (rest.Length > 0 && !rest.EndsWith(" City", StringComparison.OrdinalIgnoreCase))
            {
                return $"{rest} City";
            }
        }

        return name;
    }
}

public sealed record PhilippineLocalityDirectoryMetadata(
    string Source,
    string Dataset,
    string AsOf,
    string Release,
    string Country,
    string DatasetVersion,
    int RecordCount);
