using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogProductGovernanceFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Add nullable scope + origin; backfill all existing products to OrganizationStandard.
            migrationBuilder.AddColumn<string>(
                name: "scope",
                schema: "pos",
                table: "products",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "origin_branch_id",
                schema: "pos",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE pos.products
                SET scope = 'OrganizationStandard'
                WHERE scope IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "scope",
                schema: "pos",
                table: "products",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_scope",
                schema: "pos",
                table: "products",
                sql: "scope IN ('OrganizationStandard', 'BranchLocal')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_branch_local_origin",
                schema: "pos",
                table: "products",
                sql: "scope <> 'BranchLocal' OR origin_branch_id IS NOT NULL");

            // Sparse overrides only — no product×branch fan-out backfill.
            migrationBuilder.CreateTable(
                name: "branch_product_availabilities",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_offered = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_branch_product_availabilities", x => new { x.organization_id, x.branch_id, x.product_id });
                    table.ForeignKey(
                        name: "fk_branch_product_availabilities_products",
                        columns: x => new { x.product_id, x.organization_id },
                        principalSchema: "pos",
                        principalTable: "products",
                        principalColumns: new[] { "id", "organization_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_branch_product_availabilities_org_branch_offered",
                schema: "pos",
                table: "branch_product_availabilities",
                columns: new[] { "organization_id", "branch_id", "is_offered" });

            migrationBuilder.CreateIndex(
                name: "IX_branch_product_availabilities_product_id_organization_id",
                schema: "pos",
                table: "branch_product_availabilities",
                columns: new[] { "product_id", "organization_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_product_availabilities",
                schema: "pos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_branch_local_origin",
                schema: "pos",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_scope",
                schema: "pos",
                table: "products");

            migrationBuilder.DropColumn(
                name: "origin_branch_id",
                schema: "pos",
                table: "products");

            migrationBuilder.DropColumn(
                name: "scope",
                schema: "pos",
                table: "products");
        }
    }
}
