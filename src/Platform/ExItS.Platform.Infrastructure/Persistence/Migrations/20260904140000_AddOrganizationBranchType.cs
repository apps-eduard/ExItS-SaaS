using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds branch_type (Retail default / Warehouse) for warehouse branch experience.
/// Existing rows remain Retail.
/// </summary>
[DbContext(typeof(PlatformDbContext))]
[Migration("20260904140000_AddOrganizationBranchType")]
public partial class AddOrganizationBranchType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "branch_type",
            schema: "platform",
            table: "organization_branches",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "Retail");

        migrationBuilder.AddCheckConstraint(
            name: "ck_organization_branches_branch_type",
            schema: "platform",
            table: "organization_branches",
            sql: "branch_type IN ('Retail', 'Warehouse')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_organization_branches_branch_type",
            schema: "platform",
            table: "organization_branches");

        migrationBuilder.DropColumn(
            name: "branch_type",
            schema: "platform",
            table: "organization_branches");
    }
}
