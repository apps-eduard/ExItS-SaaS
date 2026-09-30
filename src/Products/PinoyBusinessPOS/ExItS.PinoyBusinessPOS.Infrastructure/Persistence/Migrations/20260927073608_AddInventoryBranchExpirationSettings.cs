using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryBranchExpirationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_branch_expiration_settings",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tracks_expiration = table.Column<bool>(type: "boolean", nullable: false),
                    expiration_warning_days = table.Column<int>(type: "integer", nullable: true),
                    enabled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    enabled_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_branch_expiration_settings", x => new { x.organization_id, x.branch_id, x.product_id });
                    table.CheckConstraint("ck_inventory_branch_expiration_settings_warning_days", "expiration_warning_days IS NULL OR (expiration_warning_days >= 1 AND expiration_warning_days <= 365)");
                    table.ForeignKey(
                        name: "fk_inventory_branch_expiration_settings_products",
                        column: x => x.product_id,
                        principalSchema: "pos",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_branch_expiration_settings_org_branch_enabled",
                schema: "pos",
                table: "inventory_branch_expiration_settings",
                columns: new[] { "organization_id", "branch_id", "tracks_expiration" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_branch_expiration_settings_product_id",
                schema: "pos",
                table: "inventory_branch_expiration_settings",
                column: "product_id");

            // Infer branch enablement from existing branch-scoped lots only (not every branch).
            // Legacy BranchId=NULL lots are not duplicated and do not enable all branches.
            migrationBuilder.Sql(
                """
                INSERT INTO pos.inventory_branch_expiration_settings (
                    organization_id,
                    branch_id,
                    product_id,
                    tracks_expiration,
                    expiration_warning_days,
                    enabled_at_utc,
                    enabled_by,
                    updated_at_utc,
                    updated_by)
                SELECT DISTINCT
                    l.organization_id,
                    l.branch_id,
                    l.product_id,
                    TRUE,
                    CASE
                        WHEN p.expiration_warning_days IS NULL THEN 7
                        WHEN p.expiration_warning_days < 1 THEN 7
                        WHEN p.expiration_warning_days > 365 THEN 365
                        ELSE p.expiration_warning_days
                    END,
                    NOW() AT TIME ZONE 'UTC',
                    '00000000-0000-0000-0000-000000000000'::uuid,
                    NOW() AT TIME ZONE 'UTC',
                    '00000000-0000-0000-0000-000000000000'::uuid
                FROM pos.inventory_lots AS l
                INNER JOIN pos.products AS p ON p.id = l.product_id
                WHERE p.tracks_expiration = TRUE
                  AND l.branch_id IS NOT NULL
                  AND l.quantity_on_hand > 0
                ON CONFLICT (organization_id, branch_id, product_id) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_branch_expiration_settings",
                schema: "pos");
        }
    }
}
