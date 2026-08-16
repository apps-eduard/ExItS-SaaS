using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerOrdersAndInventoryReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_source_type",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.AddColumn<decimal>(
                name: "reserved_quantity",
                schema: "pos",
                table: "inventory_accounts",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "customer_order_number_sequences",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_order_number_sequences", x => x.organization_id);
                    table.CheckConstraint("ck_customer_order_number_sequences_last_value_positive", "last_value > 0");
                });

            migrationBuilder.CreateTable(
                name: "customer_orders",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_number = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    fulfillment_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    payment_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    fulfillment_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    fulfillment_branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_name_snapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    customer_party_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    customer_display_name_snapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    customer_platform_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_buyer_organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_buyer_public_organization_id = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    merchandise_subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    delivery_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    delivery_recipient_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    delivery_recipient_phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    delivery_address_line1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    delivery_address_line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    delivery_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    delivery_notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    delivery_destination_latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    delivery_destination_longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    delivery_branch_latitude_snapshot = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    delivery_branch_longitude_snapshot = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    delivery_distance_km = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    delivery_minimum_order_amount_snapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    delivery_base_fee_snapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    delivery_included_distance_km_snapshot = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    delivery_additional_fee_per_km_snapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    delivery_maximum_distance_km_snapshot = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    delivery_free_threshold_snapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    delivery_distance_charge = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    delivery_final_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    delivery_free_applied = table.Column<bool>(type: "boolean", nullable: true),
                    stock_reservation_state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reject_reason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    reject_notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    submitted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    submitted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    accepted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    accepted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    rejected_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejected_by = table.Column<Guid>(type: "uuid", nullable: true),
                    cancelled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_by = table.Column<Guid>(type: "uuid", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_orders", x => x.id);
                    table.CheckConstraint("ck_customer_orders_fulfillment_status", "fulfillment_status IN ('Pending', 'Preparing', 'Ready', 'OutForDelivery', 'Delivered', 'ReadyForPickup', 'Collected')");
                    table.CheckConstraint("ck_customer_orders_fulfillment_type", "fulfillment_type IN ('Pickup', 'Delivery')");
                    table.CheckConstraint("ck_customer_orders_party_type", "customer_party_type IN ('Personal', 'Organization')");
                    table.CheckConstraint("ck_customer_orders_payment_status", "payment_status IN ('Unpaid', 'Pending', 'Paid')");
                    table.CheckConstraint("ck_customer_orders_status", "status IN ('Draft', 'Submitted', 'Accepted', 'Rejected', 'Cancelled', 'Completed')");
                    table.CheckConstraint("ck_customer_orders_stock_reservation", "stock_reservation_state IN ('None', 'Reserved', 'Released', 'Consumed')");
                    table.CheckConstraint("ck_customer_orders_totals_non_negative", "merchandise_subtotal >= 0 AND delivery_fee >= 0 AND total >= 0");
                });

            migrationBuilder.CreateTable(
                name: "customer_order_lines",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sku_snapshot = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    unit_snapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_order_lines", x => x.id);
                    table.CheckConstraint("ck_customer_order_lines_amounts_non_negative", "unit_price >= 0 AND discount >= 0 AND line_total >= 0");
                    table.CheckConstraint("ck_customer_order_lines_line_number_positive", "line_number > 0");
                    table.CheckConstraint("ck_customer_order_lines_quantity_positive", "quantity > 0");
                    table.CheckConstraint("ck_customer_order_lines_unit", "unit_snapshot IN ('Piece', 'Pack', 'Box', 'Bottle', 'Can', 'Sachet', 'Kilogram', 'Gram', 'Liter', 'Milliliter', 'Meter')");
                    table.ForeignKey(
                        name: "fk_customer_order_lines_orders",
                        column: x => x.order_id,
                        principalSchema: "pos",
                        principalTable: "customer_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_customer_order_lines_products",
                        column: x => x.product_id,
                        principalSchema: "pos",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_source_type",
                schema: "pos",
                table: "stock_movements",
                sql: "source_type IN ('None', 'Sale', 'Manual', 'Opening', 'PurchaseReceipt', 'StockCount', 'SaleReturn', 'InventoryTransfer', 'CustomerOrder')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_accounts_reserved_non_negative",
                schema: "pos",
                table: "inventory_accounts",
                sql: "reserved_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_accounts_reserved_not_over_on_hand",
                schema: "pos",
                table: "inventory_accounts",
                sql: "reserved_quantity <= on_hand_quantity");

            migrationBuilder.CreateIndex(
                name: "ix_customer_order_lines_org_product",
                schema: "pos",
                table: "customer_order_lines",
                columns: new[] { "seller_organization_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_order_lines_product_id",
                schema: "pos",
                table: "customer_order_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ux_customer_order_lines_order_line_number",
                schema: "pos",
                table: "customer_order_lines",
                columns: new[] { "order_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customer_orders_org_created_at",
                schema: "pos",
                table: "customer_orders",
                columns: new[] { "seller_organization_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_orders_org_customer_buyer_org",
                schema: "pos",
                table: "customer_orders",
                columns: new[] { "seller_organization_id", "customer_buyer_organization_id" },
                filter: "customer_buyer_organization_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_customer_orders_org_customer_user",
                schema: "pos",
                table: "customer_orders",
                columns: new[] { "seller_organization_id", "customer_platform_user_id" },
                filter: "customer_platform_user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_customer_orders_org_status",
                schema: "pos",
                table: "customer_orders",
                columns: new[] { "seller_organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_customer_orders_org_idempotency",
                schema: "pos",
                table: "customer_orders",
                columns: new[] { "seller_organization_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_customer_orders_org_order_number",
                schema: "pos",
                table: "customer_orders",
                columns: new[] { "seller_organization_id", "order_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_order_lines",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "customer_order_number_sequences",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "customer_orders",
                schema: "pos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_source_type",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_accounts_reserved_non_negative",
                schema: "pos",
                table: "inventory_accounts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_accounts_reserved_not_over_on_hand",
                schema: "pos",
                table: "inventory_accounts");

            migrationBuilder.DropColumn(
                name: "reserved_quantity",
                schema: "pos",
                table: "inventory_accounts");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_source_type",
                schema: "pos",
                table: "stock_movements",
                sql: "source_type IN ('None', 'Sale', 'Manual', 'Opening', 'PurchaseReceipt', 'StockCount', 'SaleReturn', 'InventoryTransfer')");
        }
    }
}
