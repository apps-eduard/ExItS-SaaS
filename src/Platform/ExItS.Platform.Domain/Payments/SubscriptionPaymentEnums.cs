namespace ExItS.Platform.Domain.Payments;

/// <summary>How the customer pays the ExItS subscription (not merchant POS tender).</summary>
public enum SubscriptionPaymentChannel
{
    GCash = 0,
    Maya = 1,
    Card = 2
}

public enum SubscriptionPaymentProvider
{
    Simulator = 0
}

public enum SubscriptionPaymentEnvironment
{
    Test = 0
}

/// <summary>Authoritative SaaS subscription checkout lifecycle.</summary>
public enum SubscriptionPaymentStatus
{
    Pending = 0,
    Processing = 1,
    Paid = 2,
    Failed = 3,
    Cancelled = 4,
    Expired = 5
}

public static class SubscriptionPaymentStatuses
{
    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(SubscriptionPaymentStatus.Pending),
        nameof(SubscriptionPaymentStatus.Processing),
        nameof(SubscriptionPaymentStatus.Paid),
        nameof(SubscriptionPaymentStatus.Failed),
        nameof(SubscriptionPaymentStatus.Cancelled),
        nameof(SubscriptionPaymentStatus.Expired)
    ];
}

public static class SubscriptionPaymentChannels
{
    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(SubscriptionPaymentChannel.GCash),
        nameof(SubscriptionPaymentChannel.Maya),
        nameof(SubscriptionPaymentChannel.Card)
    ];

    public static bool TryParse(string? value, out SubscriptionPaymentChannel channel)
    {
        channel = SubscriptionPaymentChannel.GCash;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Enum.TryParse(value.Trim(), ignoreCase: true, out channel);
    }
}
