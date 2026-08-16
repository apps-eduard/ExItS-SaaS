using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlatformDbContext))]
[Migration("20260816120000_AddOrganizationTaxConfigurationEnabled")]
public partial class AddOrganizationTaxConfigurationEnabled : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE platform.organization_sales_document_capabilities
            ADD COLUMN IF NOT EXISTS tax_configuration_enabled boolean NOT NULL DEFAULT false;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE platform.organization_sales_document_capabilities
            DROP COLUMN IF EXISTS tax_configuration_enabled;
            """);
    }
}
