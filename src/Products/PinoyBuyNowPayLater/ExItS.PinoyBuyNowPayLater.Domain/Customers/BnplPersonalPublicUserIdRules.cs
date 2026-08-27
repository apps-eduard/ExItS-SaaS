using System.Text.RegularExpressions;

namespace ExItS.PinoyBuyNowPayLater.Domain.Customers;

/// <summary>
/// BNPL-local validation of Platform Personal public user id shape (EX-####-####).
/// Does not reference Platform assemblies — format contract only.
/// </summary>
public static partial class BnplPersonalPublicUserIdRules
{
    public const int CanonicalLength = 12;

    [GeneratedRegex(@"^EX-\d{4}-\d{4}$", RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex CanonicalPattern();

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BnplDomainException(
                BnplCustomerErrorCodes.InvalidPersonalPublicUserId,
                "Platform Personal public user id is required when linking.");
        }

        var trimmed = value.Trim().ToUpperInvariant();
        var compact = trimmed.Replace("-", string.Empty, StringComparison.Ordinal);
        if (compact.Length == 10 && compact.StartsWith("EX", StringComparison.Ordinal))
        {
            trimmed = $"EX-{compact[2..6]}-{compact[6..10]}";
        }

        if (!CanonicalPattern().IsMatch(trimmed))
        {
            throw new BnplDomainException(
                BnplCustomerErrorCodes.InvalidPersonalPublicUserId,
                "Platform Personal public user id must match EX-####-####.");
        }

        return trimmed;
    }

    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : Normalize(value);
}
