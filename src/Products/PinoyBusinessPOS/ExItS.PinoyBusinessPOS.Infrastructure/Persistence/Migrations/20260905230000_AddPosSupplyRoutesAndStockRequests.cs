using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Dynamic replenishment: supply routes + stock requests linked to inventory transfers.
/// No Area ownership; location-to-location routes only.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260905230000_AddPosSupplyRoutesAndStockRequests")]
public partial class AddPosSupplyRoutesAndStockRequests : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "supply_routes",
            schema: "pos",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                source_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                destination_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                is_preferred = table.Column<bool>(type: "boolean", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_supply_routes", x => x.id);
                table.CheckConstraint(
                    "ck_supply_routes_distinct_locations",
                    "source_location_id <> destination_location_id");
            });

        migrationBuilder.CreateTable(
            name: "stock_requests",
            schema: "pos",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                destination_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                requested_source_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                request_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                requested_by = table.Column<Guid>(type: "uuid", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                rejected_by = table.Column<Guid>(type: "uuid", nullable: true),
                rejected_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                rejection_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                cancelled_by = table.Column<Guid>(type: "uuid", nullable: true),
                cancelled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_stock_requests", x => x.id);
                table.CheckConstraint(
                    "ck_stock_requests_status",
                    "status IN ('Pending', 'InProgress', 'PartiallyFulfilled', 'Fulfilled', 'Rejected', 'Cancelled')");
                table.CheckConstraint(
                    "ck_stock_requests_distinct_locations",
                    "requested_source_location_id <> destination_location_id");
            });

        migrationBuilder.CreateTable(
            name: "stock_request_number_sequences",
            schema: "pos",
            columns: table => new
            {
                organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                business_date = table.Column<DateOnly>(type: "date", nullable: false),
                last_value = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_stock_request_number_sequences", x => new { x.organization_id, x.business_date });
                table.CheckConstraint(
                    "ck_stock_request_number_sequences_last_value_positive",
                    "last_value > 0");
            });

        migrationBuilder.CreateTable(
            name: "stock_request_lines",
            schema: "pos",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                stock_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                product_id = table.Column<Guid>(type: "uuid", nullable: false),
                line_number = table.Column<int>(type: "integer", nullable: false),
                requested_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                unit_of_measure = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_stock_request_lines", x => x.id);
                table.CheckConstraint(
                    "ck_stock_request_lines_requested_positive",
                    "requested_quantity > 0");
                table.ForeignKey(
                    name: "fk_stock_request_lines_requests",
                    column: x => x.stock_request_id,
                    principalSchema: "pos",
                    principalTable: "stock_requests",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_stock_request_lines_products",
                    column: x => x.product_id,
                    principalSchema: "pos",
                    principalTable: "catalog_products",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.AddColumn<Guid>(
            name: "stock_request_id",
            schema: "pos",
            table: "inventory_transfers",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ux_supply_routes_org_source_destination",
            schema: "pos",
            table: "supply_routes",
            columns: new[] { "organization_id", "source_location_id", "destination_location_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_supply_routes_org_destination_preferred",
            schema: "pos",
            table: "supply_routes",
            columns: new[] { "organization_id", "destination_location_id" },
            unique: true,
            filter: "is_preferred = TRUE AND is_active = TRUE");

        migrationBuilder.CreateIndex(
            name: "ix_supply_routes_org_destination_active",
            schema: "pos",
            table: "supply_routes",
            columns: new[] { "organization_id", "destination_location_id", "is_active" });

        migrationBuilder.CreateIndex(
            name: "ux_stock_requests_org_request_number",
            schema: "pos",
            table: "stock_requests",
            columns: new[] { "organization_id", "request_number" },
            unique: true,
            filter: "request_number IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_stock_requests_org_destination",
            schema: "pos",
            table: "stock_requests",
            columns: new[] { "organization_id", "destination_location_id" });

        migrationBuilder.CreateIndex(
            name: "ix_stock_requests_org_source",
            schema: "pos",
            table: "stock_requests",
            columns: new[] { "organization_id", "requested_source_location_id" });

        migrationBuilder.CreateIndex(
            name: "ix_stock_requests_org_status",
            schema: "pos",
            table: "stock_requests",
            columns: new[] { "organization_id", "status" });

        migrationBuilder.CreateIndex(
            name: "ux_stock_request_lines_request_line_number",
            schema: "pos",
            table: "stock_request_lines",
            columns: new[] { "stock_request_id", "line_number" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_stock_request_lines_product_id",
            schema: "pos",
            table: "stock_request_lines",
            column: "product_id");

        migrationBuilder.CreateIndex(
            name: "ix_inventory_transfers_org_stock_request",
            schema: "pos",
            table: "inventory_transfers",
            columns: new[] { "organization_id", "stock_request_id" });

        migrationBuilder.AddForeignKey(
            name: "fk_inventory_transfers_stock_requests",
            schema: "pos",
            table: "inventory_transfers",
            column: "stock_request_id",
            principalSchema: "pos",
            principalTable: "stock_requests",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_inventory_transfers_stock_requests",
            schema: "pos",
            table: "inventory_transfers");

        migrationBuilder.DropIndex(
            name: "ix_inventory_transfers_org_stock_request",
            schema: "pos",
            table: "inventory_transfers");

        migrationBuilder.DropColumn(
            name: "stock_request_id",
            schema: "pos",
            table: "inventory_transfers");

        migrationBuilder.DropTable(
            name: "stock_request_lines",
            schema: "pos");

        migrationBuilder.DropTable(
            name: "stock_request_number_sequences",
            schema: "pos");

        migrationBuilder.DropTable(
            name: "supply_routes",
            schema: "pos");

        migrationBuilder.DropTable(
            name: "stock_requests",
            schema: "pos");
    }
}
