using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PosDbContext))]
[Migration("20260814200000_AddPosProductUnitsAndBehavior")]
public partial class AddPosProductUnitsAndBehavior : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "can_be_purchased",
            schema: "pos",
            table: "products",
            type: "boolean",
            nullable: false,
            defaultValue: true);
        migrationBuilder.AddColumn<bool>(
            name: "can_be_sold",
            schema: "pos",
            table: "products",
            type: "boolean",
            nullable: false,
            defaultValue: true);
        migrationBuilder.AddColumn<bool>(
            name: "can_be_used_as_ingredient",
            schema: "pos",
            table: "products",
            type: "boolean",
            nullable: false,
            defaultValue: false);
        migrationBuilder.AddColumn<bool>(
            name: "is_produced",
            schema: "pos",
            table: "products",
            type: "boolean",
            nullable: false,
            defaultValue: false);
        migrationBuilder.AddColumn<string>(
            name: "usage_preset",
            schema: "pos",
            table: "products",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true,
            defaultValue: "BuyAndSell");

        migrationBuilder.Sql("""
            CREATE TABLE pos.product_units (
                id uuid NOT NULL PRIMARY KEY,
                organization_id uuid NOT NULL,
                product_id uuid NOT NULL,
                kind integer NOT NULL,
                display_name varchar(64) NOT NULL,
                short_label varchar(16) NOT NULL,
                multiplier_to_base numeric(18,3) NOT NULL,
                selling_price numeric(18,2) NULL,
                allows_custom_quantity boolean NOT NULL,
                is_active boolean NOT NULL,
                sort_order integer NOT NULL,
                created_at_utc timestamp with time zone NOT NULL,
                updated_at_utc timestamp with time zone NOT NULL,
                CONSTRAINT ck_product_units_kind CHECK (kind IN (0, 1)),
                CONSTRAINT ck_product_units_multiplier_positive CHECK (multiplier_to_base > 0),
                CONSTRAINT ck_product_units_selling_price_non_negative CHECK (selling_price IS NULL OR selling_price >= 0),
                CONSTRAINT ck_product_units_sort_order_non_negative CHECK (sort_order >= 0),
                CONSTRAINT fk_product_units_products FOREIGN KEY (product_id)
                    REFERENCES pos.products(id) ON DELETE CASCADE
            );
            CREATE INDEX ix_product_units_org_product
                ON pos.product_units (organization_id, product_id);
            CREATE INDEX ix_product_units_org_product_kind_active
                ON pos.product_units (organization_id, product_id, kind)
                WHERE is_active;

            -- Seed 1:1 purchase + sell units for every existing product.
            INSERT INTO pos.product_units (
                id, organization_id, product_id, kind, display_name, short_label,
                multiplier_to_base, selling_price, allows_custom_quantity, is_active, sort_order,
                created_at_utc, updated_at_utc)
            SELECT
                gen_random_uuid(),
                p.organization_id,
                p.id,
                0,
                p.unit_of_measure,
                LEFT(p.unit_of_measure, 16),
                1,
                NULL,
                false,
                true,
                0,
                COALESCE(p.created_at_utc, NOW()),
                COALESCE(p.updated_at_utc, NOW())
            FROM pos.products p;

            INSERT INTO pos.product_units (
                id, organization_id, product_id, kind, display_name, short_label,
                multiplier_to_base, selling_price, allows_custom_quantity, is_active, sort_order,
                created_at_utc, updated_at_utc)
            SELECT
                gen_random_uuid(),
                p.organization_id,
                p.id,
                1,
                p.unit_of_measure,
                LEFT(p.unit_of_measure, 16),
                1,
                p.selling_price,
                (p.selling_mode = 'ByWeight'),
                true,
                0,
                COALESCE(p.created_at_utc, NOW()),
                COALESCE(p.updated_at_utc, NOW())
            FROM pos.products p;
            """);

        migrationBuilder.AddColumn<Guid>(
            name: "selling_unit_id",
            schema: "pos",
            table: "sale_lines",
            type: "uuid",
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "selling_unit_name_snapshot",
            schema: "pos",
            table: "sale_lines",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);
        migrationBuilder.AddColumn<decimal>(
            name: "entered_quantity",
            schema: "pos",
            table: "sale_lines",
            type: "numeric(18,3)",
            nullable: true);
        migrationBuilder.AddColumn<decimal>(
            name: "multiplier_to_base_snapshot",
            schema: "pos",
            table: "sale_lines",
            type: "numeric(18,3)",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "purchase_unit_id",
            schema: "pos",
            table: "purchase_order_lines",
            type: "uuid",
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "purchase_unit_name_snapshot",
            schema: "pos",
            table: "purchase_order_lines",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);
        migrationBuilder.AddColumn<decimal>(
            name: "multiplier_to_base_snapshot",
            schema: "pos",
            table: "purchase_order_lines",
            type: "numeric(18,3)",
            nullable: false,
            defaultValue: 1m);

        migrationBuilder.AddColumn<Guid>(
            name: "purchase_unit_id",
            schema: "pos",
            table: "goods_receipt_lines",
            type: "uuid",
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "purchase_unit_name_snapshot",
            schema: "pos",
            table: "goods_receipt_lines",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);
        migrationBuilder.AddColumn<decimal>(
            name: "multiplier_to_base_snapshot",
            schema: "pos",
            table: "goods_receipt_lines",
            type: "numeric(18,3)",
            nullable: false,
            defaultValue: 1m);

        migrationBuilder.AddColumn<Guid>(
            name: "buyer_purchase_unit_id",
            schema: "pos",
            table: "buyer_supplier_product_links",
            type: "uuid",
            nullable: true);
        migrationBuilder.AddColumn<decimal>(
            name: "multiplier_to_base",
            schema: "pos",
            table: "buyer_supplier_product_links",
            type: "numeric(18,3)",
            nullable: false,
            defaultValue: 1m);
        migrationBuilder.AddColumn<string>(
            name: "package_label",
            schema: "pos",
            table: "buyer_supplier_product_links",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "buyer_purchase_unit_id", schema: "pos", table: "buyer_supplier_product_links");
        migrationBuilder.DropColumn(name: "multiplier_to_base", schema: "pos", table: "buyer_supplier_product_links");
        migrationBuilder.DropColumn(name: "package_label", schema: "pos", table: "buyer_supplier_product_links");

        migrationBuilder.DropColumn(name: "purchase_unit_id", schema: "pos", table: "goods_receipt_lines");
        migrationBuilder.DropColumn(name: "purchase_unit_name_snapshot", schema: "pos", table: "goods_receipt_lines");
        migrationBuilder.DropColumn(name: "multiplier_to_base_snapshot", schema: "pos", table: "goods_receipt_lines");

        migrationBuilder.DropColumn(name: "purchase_unit_id", schema: "pos", table: "purchase_order_lines");
        migrationBuilder.DropColumn(name: "purchase_unit_name_snapshot", schema: "pos", table: "purchase_order_lines");
        migrationBuilder.DropColumn(name: "multiplier_to_base_snapshot", schema: "pos", table: "purchase_order_lines");

        migrationBuilder.DropColumn(name: "selling_unit_id", schema: "pos", table: "sale_lines");
        migrationBuilder.DropColumn(name: "selling_unit_name_snapshot", schema: "pos", table: "sale_lines");
        migrationBuilder.DropColumn(name: "entered_quantity", schema: "pos", table: "sale_lines");
        migrationBuilder.DropColumn(name: "multiplier_to_base_snapshot", schema: "pos", table: "sale_lines");

        migrationBuilder.Sql("DROP TABLE IF EXISTS pos.product_units;");

        migrationBuilder.DropColumn(name: "can_be_purchased", schema: "pos", table: "products");
        migrationBuilder.DropColumn(name: "can_be_sold", schema: "pos", table: "products");
        migrationBuilder.DropColumn(name: "can_be_used_as_ingredient", schema: "pos", table: "products");
        migrationBuilder.DropColumn(name: "is_produced", schema: "pos", table: "products");
        migrationBuilder.DropColumn(name: "usage_preset", schema: "pos", table: "products");
    }
}
