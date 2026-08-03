namespace ExItS.Platform.Application.Payments;

public sealed class PaymentProviderOptions
{
    public const string SectionName = "Payments";

    public string Provider { get; set; } = "None";
}

public static class PaymentProviderNames
{
    public const string None = "None";
    public const string LocalValidation = "LocalValidation";
    public const string Manual = "Manual";
}
