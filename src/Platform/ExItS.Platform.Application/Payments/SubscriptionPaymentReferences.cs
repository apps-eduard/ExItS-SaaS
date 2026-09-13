using System.Security.Cryptography;
using System.Text;
using ExItS.Platform.Domain.Payments;

namespace ExItS.Platform.Application.Payments;

public static class SubscriptionPaymentReferences
{
    public static string FormatInternalReference(DateTimeOffset utcNow, long sequence) =>
        $"PAY-{utcNow:yyyyMMdd}-{sequence:D6}";

    public static string FormatProviderReference(SubscriptionPaymentChannel channel, DateTimeOffset utcNow)
    {
        var prefix = channel switch
        {
            SubscriptionPaymentChannel.GCash => "SIM-GC",
            SubscriptionPaymentChannel.Maya => "SIM-MY",
            SubscriptionPaymentChannel.Card => "SIM-CC",
            _ => "SIM-XX"
        };
        return $"{prefix}-{utcNow:yyMMdd}-{RandomToken(6)}";
    }

    private static string RandomToken(int length)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(length);
        var sb = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            sb.Append(alphabet[bytes[i] % alphabet.Length]);
        }

        return sb.ToString();
    }
}

/// <summary>Internal card simulator rules (test cards only — never real PAN validation).</summary>
public static class SubscriptionCardSimulator
{
    public const string SuccessPan = "4242424242424242";
    public const string DeclinePan = "4000000000000002";
    public const string PendingPan = "4000000000009995";

    public static string NormalizePan(string? pan) =>
        string.IsNullOrWhiteSpace(pan)
            ? string.Empty
            : new string(pan.Where(char.IsDigit).ToArray());

    public static (bool Success, bool LeaveProcessing, string? FailureCode, string? FailureReason) Evaluate(
        string? pan)
    {
        var digits = NormalizePan(pan);
        return digits switch
        {
            DeclinePan => (false, false, "card_declined", "Simulated card decline"),
            PendingPan => (false, true, null, null),
            _ => (true, false, null, null)
        };
    }

    public static string DetectBrand(string? pan)
    {
        var digits = NormalizePan(pan);
        if (digits.StartsWith('4'))
        {
            return "Visa";
        }

        if (digits.StartsWith('5'))
        {
            return "Mastercard";
        }

        return "Card";
    }

    public static string? Last4(string? pan)
    {
        var digits = NormalizePan(pan);
        return digits.Length >= 4 ? digits[^4..] : null;
    }
}
