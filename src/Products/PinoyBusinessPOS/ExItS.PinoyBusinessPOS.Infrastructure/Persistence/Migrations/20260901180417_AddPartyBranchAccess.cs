using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPartyBranchAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_suppliers_id_organization_id",
                schema: "pos",
                table: "suppliers",
                columns: new[] { "id", "organization_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_customers_id_organization_id",
                schema: "pos",
                table: "customers",
                columns: new[] { "id", "organization_id" });

            migrationBuilder.CreateTable(
                name: "customer_branch_access",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grant_source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    granted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    granted_by_actor_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_branch_access", x => new { x.organization_id, x.branch_id, x.customer_id });
                    table.CheckConstraint("ck_customer_branch_access_grant_source", "grant_source IN ('ExplicitAssign', 'CreateAtBranch', 'Transaction', 'SetupCopy', 'MigrationBackfill')");
                    table.ForeignKey(
                        name: "fk_customer_branch_access_customers",
                        columns: x => new { x.customer_id, x.organization_id },
                        principalSchema: "pos",
                        principalTable: "customers",
                        principalColumns: new[] { "id", "organization_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_branch_access",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grant_source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    granted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    granted_by_actor_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_branch_access", x => new { x.organization_id, x.branch_id, x.supplier_id });
                    table.CheckConstraint("ck_supplier_branch_access_grant_source", "grant_source IN ('ExplicitAssign', 'CreateAtBranch', 'Transaction', 'SetupCopy', 'MigrationBackfill')");
                    table.ForeignKey(
                        name: "fk_supplier_branch_access_suppliers",
                        columns: x => new { x.supplier_id, x.organization_id },
                        principalSchema: "pos",
                        principalTable: "suppliers",
                        principalColumns: new[] { "id", "organization_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_branch_access_customer_id_organization_id",
                schema: "pos",
                table: "customer_branch_access",
                columns: new[] { "customer_id", "organization_id" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_branch_access_org_branch",
                schema: "pos",
                table: "customer_branch_access",
                columns: new[] { "organization_id", "branch_id" });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_branch_access_org_branch",
                schema: "pos",
                table: "supplier_branch_access",
                columns: new[] { "organization_id", "branch_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_branch_access_supplier_id_organization_id",
                schema: "pos",
                table: "supplier_branch_access",
                columns: new[] { "supplier_id", "organization_id" });

            migrationBuilder.Sql(
                """
                INSERT INTO pos.customer_branch_access (organization_id, branch_id, customer_id, grant_source, granted_at_utc)
                SELECT DISTINCT s.organization_id, s.branch_id, s.customer_id, 'MigrationBackfill', NOW() AT TIME ZONE 'UTC'
                FROM pos.sales s
                WHERE s.branch_id IS NOT NULL
                  AND s.customer_id IS NOT NULL
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO pos.customer_branch_access (organization_id, branch_id, customer_id, grant_source, granted_at_utc)
                SELECT DISTINCT co.seller_organization_id, co.fulfillment_branch_id, c.id, 'MigrationBackfill', NOW() AT TIME ZONE 'UTC'
                FROM pos.customer_orders co
                INNER JOIN pos.customers c ON c.organization_id = co.seller_organization_id
                WHERE (
                    (co.platform_business_customer_id IS NOT NULL AND c.platform_business_customer_id = co.platform_business_customer_id)
                    OR (co.customer_buyer_organization_id IS NOT NULL AND c.linked_buyer_organization_id = co.customer_buyer_organization_id)
                )
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO pos.supplier_branch_access (organization_id, branch_id, supplier_id, grant_source, granted_at_utc)
                SELECT DISTINCT gr.organization_id, gr.receiving_branch_id, gr.supplier_id, 'MigrationBackfill', NOW() AT TIME ZONE 'UTC'
                FROM pos.goods_receipts gr
                WHERE gr.receiving_branch_id IS NOT NULL
                  AND gr.supplier_id IS NOT NULL
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO pos.supplier_branch_access (organization_id, branch_id, supplier_id, grant_source, granted_at_utc)
                SELECT DISTINCT dpr.organization_id, dpr.receiving_branch_id, dpr.supplier_id, 'MigrationBackfill', NOW() AT TIME ZONE 'UTC'
                FROM pos.direct_purchase_receipts dpr
                WHERE dpr.receiving_branch_id IS NOT NULL
                  AND dpr.supplier_id IS NOT NULL
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                WITH org_primary AS (
                    SELECT DISTINCT ON (organization_id) organization_id, branch_id AS primary_branch_id
                    FROM pos.sales
                    WHERE branch_id IS NOT NULL
                    ORDER BY organization_id, branch_id
                )
                INSERT INTO pos.customer_branch_access (organization_id, branch_id, customer_id, grant_source, granted_at_utc)
                SELECT c.organization_id, op.primary_branch_id, c.id, 'MigrationBackfill', NOW() AT TIME ZONE 'UTC'
                FROM pos.customers c
                INNER JOIN org_primary op ON op.organization_id = c.organization_id
                WHERE NOT EXISTS (
                    SELECT 1 FROM pos.customer_branch_access a
                    WHERE a.organization_id = c.organization_id AND a.customer_id = c.id
                )
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                WITH org_primary AS (
                    SELECT DISTINCT ON (organization_id) organization_id, branch_id AS primary_branch_id
                    FROM pos.sales
                    WHERE branch_id IS NOT NULL
                    ORDER BY organization_id, branch_id
                )
                INSERT INTO pos.supplier_branch_access (organization_id, branch_id, supplier_id, grant_source, granted_at_utc)
                SELECT s.organization_id, op.primary_branch_id, s.id, 'MigrationBackfill', NOW() AT TIME ZONE 'UTC'
                FROM pos.suppliers s
                INNER JOIN org_primary op ON op.organization_id = s.organization_id
                WHERE NOT EXISTS (
                    SELECT 1 FROM pos.supplier_branch_access a
                    WHERE a.organization_id = s.organization_id AND a.supplier_id = s.id
                )
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_branch_access",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "supplier_branch_access",
                schema: "pos");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_suppliers_id_organization_id",
                schema: "pos",
                table: "suppliers");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_customers_id_organization_id",
                schema: "pos",
                table: "customers");
        }
    }
}
