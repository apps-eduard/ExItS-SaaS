using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationComplianceEligibilityStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE platform.organization_sales_document_capabilities
                ADD COLUMN IF NOT EXISTS compliance_eligibility_status character varying(64)
                    NOT NULL DEFAULT 'NotRequested';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE platform.organization_sales_document_capabilities
                DROP COLUMN IF EXISTS compliance_eligibility_status;
                """);
        }
    }
}
