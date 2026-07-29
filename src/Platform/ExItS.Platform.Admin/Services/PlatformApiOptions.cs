namespace ExItS.Platform.Admin.Services;

public sealed class PlatformApiOptions
{
    public const string SectionName = "PlatformApi";
    public string BaseUrl { get; init; } = "http://localhost:5288";
    public int TimeoutSeconds { get; init; } = 30;
}

public sealed class DevelopmentOperatorOptions
{
    public string DisplayName { get; set; } = "Dev Operator";
}
