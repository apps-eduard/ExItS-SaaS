using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConnectedPoReturnPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "return_policy_mode",
                schema: "pos",
                table: "products",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<bool>(
                name: "return_policy_returns_allowed",
                schema: "pos",
                table: "products",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "return_policy_window_days",
                schema: "pos",
                table: "products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "receiving_issue_window_days",
                schema: "pos",
                table: "organization_connected_commerce_settings",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<bool>(
                name: "require_return_approval",
                schema: "pos",
                table: "organization_connected_commerce_settings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "return_window_days",
                schema: "pos",
                table: "organization_connected_commerce_settings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "returns_allowed",
                schema: "pos",
                table: "organization_connected_commerce_settings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "connected_po_return_allocations",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_batch_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    eligibility_bucket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_connected_po_return_allocations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "connected_po_return_eligibility_buckets",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goods_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goods_receipt_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    supplier_product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity_received = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    quantity_allocated = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    policy_returns_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    policy_return_window_days = table.Column<int>(type: "integer", nullable: true),
                    policy_receiving_issue_window_days = table.Column<int>(type: "integer", nullable: false),
                    policy_require_return_approval = table.Column<bool>(type: "boolean", nullable: false),
                    policy_source = table.Column<short>(type: "smallint", nullable: false),
                    return_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_connected_po_return_eligibility_buckets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organization_connected_commerce_category_return_rules",
                schema: "pos",
                columns: table => new
                {
                    organization_connected_commerce_settings_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    returns_allowed = table.Column<bool>(type: "boolean", nullable: true),
                    return_window_days = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_connected_commerce_category_return_rules", x => new { x.organization_connected_commerce_settings_id, x.category_id });
                    table.ForeignKey(
                        name: "FK_organization_connected_commerce_category_return_rules_organ~",
                        column: x => x.organization_connected_commerce_settings_id,
                        principalSchema: "pos",
                        principalTable: "organization_connected_commerce_settings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_connected_po_return_allocations_bucket",
                schema: "pos",
                table: "connected_po_return_allocations",
                column: "eligibility_bucket_id");

            migrationBuilder.CreateIndex(
                name: "ux_connected_po_return_allocations_line_bucket",
                schema: "pos",
                table: "connected_po_return_allocations",
                columns: new[] { "return_batch_line_id", "eligibility_bucket_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_connected_po_return_eligibility_buckets_buyer_po",
                schema: "pos",
                table: "connected_po_return_eligibility_buckets",
                columns: new[] { "buyer_organization_id", "purchase_order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_connected_po_return_eligibility_buckets_po_line",
                schema: "pos",
                table: "connected_po_return_eligibility_buckets",
                columns: new[] { "buyer_organization_id", "purchase_order_line_id", "received_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_connected_po_return_eligibility_buckets_gr_line",
                schema: "pos",
                table: "connected_po_return_eligibility_buckets",
                column: "goods_receipt_line_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_org_connected_commerce_category_return_rules_category",
                schema: "pos",
                table: "organization_connected_commerce_category_return_rules",
                column: "category_id");

            // Backfill: existing connected GRN good lines → unlimited org-default voluntary return policy.
            // Prior ReturnBatch quantities remain a secondary over-return guard via SumReturnedByPurchaseOrderLine.
            migrationBuilder.Sql("""
                INSERT INTO pos.connected_po_return_eligibility_buckets (
                    id,
                    buyer_organization_id,
                    seller_organization_id,
                    purchase_order_id,
                    purchase_order_line_id,
                    goods_receipt_id,
                    goods_receipt_line_id,
                    buyer_product_id,
                    supplier_product_id,
                    quantity_received,
                    quantity_allocated,
                    received_at_utc,
                    policy_returns_allowed,
                    policy_return_window_days,
                    policy_receiving_issue_window_days,
                    policy_require_return_approval,
                    policy_source,
                    return_expires_at_utc,
                    created_at_utc
                )
                SELECT
                    gen_random_uuid(),
                    gr.organization_id,
                    cpo.supplier_organization_id,
                    gr.purchase_order_id,
                    grl.purchase_order_line_id,
                    gr.id,
                    grl.id,
                    grl.product_id,
                    pol.supplier_product_id,
                    grl.received_qty,
                    0,
                    gr.received_at_utc,
                    TRUE,
                    NULL,
                    2,
                    TRUE,
                    0,
                    NULL,
                    NOW()
                FROM pos.goods_receipt_lines grl
                INNER JOIN pos.goods_receipts gr ON gr.id = grl.goods_receipt_id
                INNER JOIN pos.connected_purchase_orders cpo ON cpo.buyer_purchase_order_id = gr.purchase_order_id
                LEFT JOIN pos.purchase_order_lines pol ON pol.id = grl.purchase_order_line_id
                WHERE gr.status = 'Posted'
                  AND grl.received_qty > 0
                  AND NOT EXISTS (
                      SELECT 1
                      FROM pos.connected_po_return_eligibility_buckets b
                      WHERE b.goods_receipt_line_id = grl.id
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "connected_po_return_allocations",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "connected_po_return_eligibility_buckets",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "organization_connected_commerce_category_return_rules",
                schema: "pos");

            migrationBuilder.DropColumn(
                name: "return_policy_mode",
                schema: "pos",
                table: "products");

            migrationBuilder.DropColumn(
                name: "return_policy_returns_allowed",
                schema: "pos",
                table: "products");

            migrationBuilder.DropColumn(
                name: "return_policy_window_days",
                schema: "pos",
                table: "products");

            migrationBuilder.DropColumn(
                name: "receiving_issue_window_days",
                schema: "pos",
                table: "organization_connected_commerce_settings");

            migrationBuilder.DropColumn(
                name: "require_return_approval",
                schema: "pos",
                table: "organization_connected_commerce_settings");

            migrationBuilder.DropColumn(
                name: "return_window_days",
                schema: "pos",
                table: "organization_connected_commerce_settings");

            migrationBuilder.DropColumn(
                name: "returns_allowed",
                schema: "pos",
                table: "organization_connected_commerce_settings");
        }
    }
}
