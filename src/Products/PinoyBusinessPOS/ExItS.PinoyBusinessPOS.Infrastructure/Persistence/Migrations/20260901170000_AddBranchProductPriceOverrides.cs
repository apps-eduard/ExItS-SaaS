using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>MB2-03 sparse branch price overrides (base + sell units).</summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260901170000_AddBranchProductPriceOverrides")]
public partial class AddBranchProductPriceOverrides : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "branch_product_price_overrides",
            schema: "pos",
            columns: table => new
            {
                organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                product_id = table.Column<Guid>(type: "uuid", nullable: false),
                product_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                selling_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_by_actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey(
                    "pk_branch_product_price_overrides",
                    x => new { x.organization_id, x.branch_id, x.product_id, x.product_unit_id });
                table.CheckConstraint(
                    "ck_branch_product_price_overrides_selling_price_non_negative",
                    "selling_price >= 0");
                table.ForeignKey(
                    name: "fk_branch_product_price_overrides_products",
                    columns: x => new { x.product_id, x.organization_id },
                    principalSchema: "pos",
                    principalTable: "products",
                    principalColumns: new[] { "id", "organization_id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_branch_product_price_overrides_org_branch_product",
            schema: "pos",
            table: "branch_product_price_overrides",
            columns: new[] { "organization_id", "branch_id", "product_id" });

        migrationBuilder.CreateIndex(
            name: "IX_branch_product_price_overrides_product_id_organization_id",
            schema: "pos",
            table: "branch_product_price_overrides",
            columns: new[] { "product_id", "organization_id" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "branch_product_price_overrides",
            schema: "pos");
    }
}
