using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryExpirySalePolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "branch_category_expiry_sale_policies",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stop_selling_days_before_expiry = table.Column<int>(type: "integer", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_branch_category_expiry_sale_policies", x => new { x.organization_id, x.branch_id, x.category_id });
                    table.CheckConstraint("ck_branch_category_expiry_sale_policies_days", "stop_selling_days_before_expiry >= 0 AND stop_selling_days_before_expiry <= 365");
                    table.ForeignKey(
                        name: "fk_branch_category_expiry_sale_policies_categories",
                        column: x => x.category_id,
                        principalSchema: "pos",
                        principalTable: "product_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "branch_expiry_sale_policy_settings",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stop_selling_days_before_expiry = table.Column<int>(type: "integer", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_branch_expiry_sale_policy_settings", x => new { x.organization_id, x.branch_id });
                    table.CheckConstraint("ck_branch_expiry_sale_policy_settings_days", "stop_selling_days_before_expiry >= 0 AND stop_selling_days_before_expiry <= 365");
                });

            migrationBuilder.CreateTable(
                name: "organization_category_expiry_sale_policies",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stop_selling_days_before_expiry = table.Column<int>(type: "integer", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_category_expiry_sale_policies", x => new { x.organization_id, x.category_id });
                    table.CheckConstraint("ck_organization_category_expiry_sale_policies_days", "stop_selling_days_before_expiry >= 0 AND stop_selling_days_before_expiry <= 365");
                    table.ForeignKey(
                        name: "fk_organization_category_expiry_sale_policies_categories",
                        column: x => x.category_id,
                        principalSchema: "pos",
                        principalTable: "product_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "organization_expiry_sale_policy_settings",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stop_selling_days_before_expiry = table.Column<int>(type: "integer", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_expiry_sale_policy_settings", x => x.organization_id);
                    table.CheckConstraint("ck_organization_expiry_sale_policy_settings_days", "stop_selling_days_before_expiry >= 0 AND stop_selling_days_before_expiry <= 365");
                });

            migrationBuilder.CreateIndex(
                name: "IX_branch_category_expiry_sale_policies_category_id",
                schema: "pos",
                table: "branch_category_expiry_sale_policies",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_branch_category_expiry_sale_policies_org_branch",
                schema: "pos",
                table: "branch_category_expiry_sale_policies",
                columns: new[] { "organization_id", "branch_id" });

            migrationBuilder.CreateIndex(
                name: "IX_organization_category_expiry_sale_policies_category_id",
                schema: "pos",
                table: "organization_category_expiry_sale_policies",
                column: "category_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_category_expiry_sale_policies",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "branch_expiry_sale_policy_settings",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "organization_category_expiry_sale_policies",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "organization_expiry_sale_policy_settings",
                schema: "pos");
        }
    }
}
