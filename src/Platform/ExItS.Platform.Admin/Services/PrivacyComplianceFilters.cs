using ExItS.Platform.Admin.Models;

namespace ExItS.Platform.Admin.Services;

public static class PrivacyComplianceFilters
{
    public static readonly IReadOnlySet<string> DocumentCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "CustomerFacing",
        "Internal",
        "RegulatoryReadiness"
    };

    public static bool MatchesDocuments(ComplianceRequirementDto requirement, string? categoryFilter)
    {
        if (!string.IsNullOrWhiteSpace(categoryFilter))
        {
            return string.Equals(requirement.Category, categoryFilter, StringComparison.OrdinalIgnoreCase);
        }

        return DocumentCategories.Contains(requirement.Category);
    }

    public static bool MatchesPias(ComplianceRequirementDto requirement) =>
        string.Equals(requirement.Category, "PrivacyImpactAssessment", StringComparison.OrdinalIgnoreCase)
        || requirement.Code.Contains("PIA", StringComparison.OrdinalIgnoreCase);

    public static bool MatchesDataInventory(ComplianceRequirementDto requirement) =>
        string.Equals(requirement.Category, "DataInventory", StringComparison.OrdinalIgnoreCase);

    public static bool MatchesRetention(ComplianceRequirementDto requirement) =>
        string.Equals(requirement.Category, "Retention", StringComparison.OrdinalIgnoreCase);

    public static bool MatchesIncidents(ComplianceRequirementDto requirement) =>
        string.Equals(requirement.Category, "IncidentBreach", StringComparison.OrdinalIgnoreCase);

    public static bool MatchesVendors(ComplianceRequirementDto requirement) =>
        string.Equals(requirement.Category, "VendorProcessor", StringComparison.OrdinalIgnoreCase);

    public static bool MatchesDpoNpc(ComplianceRequirementDto requirement) =>
        string.Equals(requirement.Category, "DpoNpc", StringComparison.OrdinalIgnoreCase)
        || string.Equals(requirement.Category, "RegulatoryReadiness", StringComparison.OrdinalIgnoreCase);

    public static bool IsImportantGap(ComplianceRequirementDto requirement) =>
        string.Equals(requirement.RequirementLevel, "Required", StringComparison.OrdinalIgnoreCase)
        && (string.Equals(requirement.Status, "NotStarted", StringComparison.OrdinalIgnoreCase)
            || string.Equals(requirement.Status, "NeedsUpdate", StringComparison.OrdinalIgnoreCase));
}
