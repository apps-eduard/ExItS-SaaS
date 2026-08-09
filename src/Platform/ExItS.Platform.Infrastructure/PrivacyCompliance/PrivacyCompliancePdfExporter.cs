using ExItS.Platform.Application.PrivacyCompliance;
using ExItS.Platform.Domain.PrivacyCompliance;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ExItS.Platform.Infrastructure.PrivacyCompliance;

internal sealed class PrivacyCompliancePdfExporter : IPrivacyCompliancePdfExporter
{
    static PrivacyCompliancePdfExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] ExportRequirement(
        ComplianceRequirement requirement,
        string? companyName,
        DateTimeOffset generatedAtUtc)
    {
        var displayCompany = string.IsNullOrWhiteSpace(companyName) ? "Not configured" : companyName.Trim();
        var showWatermark = ComplianceStatusRules.RequiresDraftWatermark(requirement.Status);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                if (showWatermark)
                {
                    page.Background()
                        .AlignCenter()
                        .AlignMiddle()
                        .Text("DRAFT / NOT APPROVED")
                        .FontSize(48)
                        .Bold()
                        .FontColor(Colors.Grey.Lighten2);
                }

                page.Header()
                    .Column(column =>
                    {
                        column.Item().Text("Privacy Compliance Requirement").FontSize(16).Bold();
                        column.Item().Text(displayCompany).FontSize(12).SemiBold();
                        if (showWatermark)
                        {
                            column.Item().PaddingTop(6)
                                .Text("DRAFT / NOT APPROVED")
                                .FontSize(18)
                                .Bold()
                                .FontColor(Colors.Red.Medium);
                        }

                        column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                page.Content()
                    .PaddingVertical(16)
                    .Column(column =>
                    {
                        column.Spacing(8);
                        AddRow(column, "Code", requirement.Code);
                        AddRow(column, "Title", requirement.Title);
                        AddRow(column, "Category", requirement.Category.ToString());
                        AddRow(column, "Status", requirement.Status.ToString());
                        AddRow(column, "Version", requirement.Version);
                        AddRow(column, "Requirement Level", requirement.RequirementLevel.ToString());
                        AddRow(column, "Owner / DPO Role", requirement.OwnerRole);
                        AddRow(column, "Effective Date", FormatDate(requirement.EffectiveDate));
                        AddRow(column, "Last Reviewed", FormatDate(requirement.LastReviewedDate));
                        AddRow(column, "Next Review", FormatDate(requirement.NextReviewDate));
                        AddRow(column, "Requires DPO/Legal Verification", requirement.RequiresDpoLegalVerification ? "Yes" : "No");
                        AddRow(column, "Generated (UTC)", generatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"));

                        column.Item().PaddingTop(12).Text("Description").Bold();
                        column.Item().Text(requirement.Description);

                        if (!string.IsNullOrWhiteSpace(requirement.Notes))
                        {
                            column.Item().PaddingTop(12).Text("Notes").Bold();
                            column.Item().Text(requirement.Notes!);
                        }

                        if (!string.IsNullOrWhiteSpace(requirement.SourceReference))
                        {
                            column.Item().PaddingTop(12).Text("Source Reference").Bold();
                            column.Item().Text(requirement.SourceReference!);
                        }

                        column.Item().PaddingTop(16).Text(
                            "This document reflects documentation readiness only. It is not legal certification, NPC approval, or a compliance claim.")
                            .Italic()
                            .FontSize(8)
                            .FontColor(Colors.Grey.Darken1);
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("ExItS Platform — Privacy Compliance Export — ");
                        text.Span(generatedAtUtc.ToString("yyyy-MM-dd")).SemiBold();
                    });
            });
        }).GeneratePdf();
    }

    private static void AddRow(ColumnDescriptor column, string label, string value)
    {
        column.Item().Row(row =>
        {
            row.ConstantItem(140).Text(label).SemiBold();
            row.RelativeItem().Text(value);
        });
    }

    private static string FormatDate(DateOnly? date) =>
        date?.ToString("yyyy-MM-dd") ?? "—";
}
