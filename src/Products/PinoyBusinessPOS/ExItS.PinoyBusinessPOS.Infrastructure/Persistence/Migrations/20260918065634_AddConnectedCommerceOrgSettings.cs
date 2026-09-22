using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConnectedCommerceOrgSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "allow_pay_before_fulfillment",
                schema: "pos",
                table: "connected_supplier_relationships",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "allow_pay_on_delivery_or_receipt",
                schema: "pos",
                table: "connected_supplier_relationships",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "allow_supplier_credit",
                schema: "pos",
                table: "connected_supplier_relationships",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "customer_default_payment_timing",
                schema: "pos",
                table: "connected_supplier_relationships",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "use_org_payment_timing_defaults",
                schema: "pos",
                table: "connected_supplier_relationships",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "confirmed_payment_timing",
                schema: "pos",
                table: "connected_purchase_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "payment_timing",
                schema: "pos",
                table: "connected_purchase_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "proposed_payment_timing",
                schema: "pos",
                table: "connected_purchase_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "payment_timing",
                schema: "pos",
                table: "purchase_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Historical rows: preserve pre-timing commercial behavior.
            // Prior connected POs had no PayBefore fulfillment gate — do NOT leave default 0 (PayBefore).
            // Utang (payment_term=2) → SupplierCredit (2); all other terms → PayOnDeliveryOrReceipt (1).
            // New orgs / new POs still default to PayBefore via application CreateDefault / create paths.
            // Snapshot confirmed_payment_timing for already-accepted+ fulfillment statuses so Effective*
            // stays stable if payment_timing is later revised in application code.
            migrationBuilder.Sql(
                """
                UPDATE pos.connected_purchase_orders
                SET payment_timing = CASE payment_term WHEN 2 THEN 2 ELSE 1 END;

                UPDATE pos.purchase_orders
                SET payment_timing = CASE payment_term WHEN 2 THEN 2 ELSE 1 END;

                UPDATE pos.connected_purchase_orders
                SET confirmed_payment_timing = payment_timing
                WHERE confirmed_payment_timing IS NULL
                  AND status IN (1, 3, 4);
                """);

            migrationBuilder.CreateTable(
                name: "connected_supplier_relationship_category_discount_overrides",
                schema: "pos",
                columns: table => new
                {
                    relationship_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    discount_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_connected_supplier_relationship_category_discount_overrides", x => new { x.relationship_id, x.category_id });
                    table.ForeignKey(
                        name: "FK_connected_supplier_relationship_category_discount_overrides~",
                        column: x => x.relationship_id,
                        principalSchema: "pos",
                        principalTable: "connected_supplier_relationships",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organization_connected_commerce_settings",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allow_pay_before_fulfillment = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    allow_pay_on_delivery_or_receipt = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    allow_supplier_credit = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    default_payment_timing = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    default_b2b_discount_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    proposal_reservation_hold_hours = table.Column<int>(type: "integer", nullable: false, defaultValue: 24),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_connected_commerce_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organization_connected_commerce_category_discount_rules",
                schema: "pos",
                columns: table => new
                {
                    organization_connected_commerce_settings_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    discount_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_connected_commerce_category_discount_rules", x => new { x.organization_connected_commerce_settings_id, x.category_id });
                    table.ForeignKey(
                        name: "FK_organization_connected_commerce_category_discount_rules_org~",
                        column: x => x.organization_connected_commerce_settings_id,
                        principalSchema: "pos",
                        principalTable: "organization_connected_commerce_settings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_connected_purchase_orders_payment_timing",
                schema: "pos",
                table: "connected_purchase_orders",
                sql: "payment_timing BETWEEN 0 AND 2");

            migrationBuilder.CreateIndex(
                name: "ix_org_connected_commerce_category_rules_category",
                schema: "pos",
                table: "organization_connected_commerce_category_discount_rules",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ux_org_connected_commerce_settings_org",
                schema: "pos",
                table: "organization_connected_commerce_settings",
                column: "organization_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "connected_supplier_relationship_category_discount_overrides",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "organization_connected_commerce_category_discount_rules",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "organization_connected_commerce_settings",
                schema: "pos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_connected_purchase_orders_payment_timing",
                schema: "pos",
                table: "connected_purchase_orders");

            migrationBuilder.DropColumn(
                name: "allow_pay_before_fulfillment",
                schema: "pos",
                table: "connected_supplier_relationships");

            migrationBuilder.DropColumn(
                name: "allow_pay_on_delivery_or_receipt",
                schema: "pos",
                table: "connected_supplier_relationships");

            migrationBuilder.DropColumn(
                name: "allow_supplier_credit",
                schema: "pos",
                table: "connected_supplier_relationships");

            migrationBuilder.DropColumn(
                name: "customer_default_payment_timing",
                schema: "pos",
                table: "connected_supplier_relationships");

            migrationBuilder.DropColumn(
                name: "use_org_payment_timing_defaults",
                schema: "pos",
                table: "connected_supplier_relationships");

            migrationBuilder.DropColumn(
                name: "confirmed_payment_timing",
                schema: "pos",
                table: "connected_purchase_orders");

            migrationBuilder.DropColumn(
                name: "payment_timing",
                schema: "pos",
                table: "connected_purchase_orders");

            migrationBuilder.DropColumn(
                name: "proposed_payment_timing",
                schema: "pos",
                table: "connected_purchase_orders");

            migrationBuilder.DropColumn(
                name: "payment_timing",
                schema: "pos",
                table: "purchase_orders");
        }
    }
}
