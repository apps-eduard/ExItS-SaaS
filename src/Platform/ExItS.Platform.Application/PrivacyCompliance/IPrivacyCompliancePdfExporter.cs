using ExItS.Platform.Domain.PrivacyCompliance;

namespace ExItS.Platform.Application.PrivacyCompliance;

public interface IPrivacyCompliancePdfExporter
{
    byte[] ExportRequirement(
        ComplianceRequirement requirement,
        string? companyName,
        DateTimeOffset generatedAtUtc);
}
