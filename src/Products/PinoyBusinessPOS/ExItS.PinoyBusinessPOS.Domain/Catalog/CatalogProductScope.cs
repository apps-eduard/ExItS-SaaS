using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

/// <summary>
/// Organization product governance scope. Distinct from Platform Global Catalog (import source).
/// </summary>
public enum CatalogProductScope
{
    OrganizationStandard = 0,
    BranchLocal = 1
}

public static class CatalogProductScopes
{
    public static readonly IReadOnlyList<string> Codes =
    [
        nameof(CatalogProductScope.OrganizationStandard),
        nameof(CatalogProductScope.BranchLocal)
    ];

    public const int CodeMaxLength = 32;

    public static string ToCode(CatalogProductScope scope) => scope.ToString();

    public static bool TryParse(string? text, out CatalogProductScope scope)
    {
        scope = CatalogProductScope.OrganizationStandard;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return Enum.TryParse(text.Trim(), ignoreCase: true, out scope) && Enum.IsDefined(scope);
    }

    public static CatalogProductScope Parse(string? text)
    {
        if (!TryParse(text, out var scope))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogProductScope,
                $"CatalogProductScope must be one of: {string.Join(", ", Codes)}.");
        }

        return scope;
    }

    /// <summary>
    /// BranchLocal requires a non-null origin branch. OrganizationStandard may retain origin for audit.
    /// </summary>
    public static void EnsureOriginValid(CatalogProductScope scope, Inventory.PosBranchId? originBranchId)
    {
        if (!Enum.IsDefined(scope))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogProductScope,
                $"Unrecognized catalog product scope '{scope}'.");
        }

        if (scope == CatalogProductScope.BranchLocal && originBranchId is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogProductOriginBranch,
                "BranchLocal products require OriginBranchId.");
        }
    }
}
