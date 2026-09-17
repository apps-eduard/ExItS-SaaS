using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Stock request warehouse workflow: approve / prepare / dispatch statuses and audit columns.
/// Inventory balances remain owned exclusively by InventoryTransfer movements.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260906200000_ExtendStockRequestWarehouseWorkflow")]
public partial class ExtendStockRequestWarehouseWorkflow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_stock_requests_status",
            schema: "pos",
            table: "stock_requests");

        migrationBuilder.Sql(
            """
            UPDATE pos.stock_requests
            SET status = 'Preparing'
            WHERE status = 'InProgress';
            """);

        migrationBuilder.AddColumn<Guid>(
            name: "approved_by",
            schema: "pos",
            table: "stock_requests",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "approved_at_utc",
            schema: "pos",
            table: "stock_requests",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "preparing_started_by",
            schema: "pos",
            table: "stock_requests",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "preparing_started_at_utc",
            schema: "pos",
            table: "stock_requests",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "dispatched_by",
            schema: "pos",
            table: "stock_requests",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "dispatched_at_utc",
            schema: "pos",
            table: "stock_requests",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "linked_inventory_transfer_id",
            schema: "pos",
            table: "stock_requests",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "approved_quantity",
            schema: "pos",
            table: "stock_request_lines",
            type: "numeric(18,3)",
            precision: 18,
            scale: 3,
            nullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "ck_stock_requests_status",
            schema: "pos",
            table: "stock_requests",
            sql: "status IN ('Pending', 'Approved', 'Preparing', 'InTransit', 'PartiallyFulfilled', 'Fulfilled', 'Rejected', 'Cancelled', 'InProgress')");

        migrationBuilder.AddCheckConstraint(
            name: "ck_stock_request_lines_approved_range",
            schema: "pos",
            table: "stock_request_lines",
            sql: "approved_quantity IS NULL OR (approved_quantity > 0 AND approved_quantity <= requested_quantity)");

        migrationBuilder.CreateIndex(
            name: "ix_stock_requests_linked_transfer",
            schema: "pos",
            table: "stock_requests",
            column: "linked_inventory_transfer_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_stock_requests_linked_transfer",
            schema: "pos",
            table: "stock_requests");

        migrationBuilder.DropCheckConstraint(
            name: "ck_stock_request_lines_approved_range",
            schema: "pos",
            table: "stock_request_lines");

        migrationBuilder.DropCheckConstraint(
            name: "ck_stock_requests_status",
            schema: "pos",
            table: "stock_requests");

        migrationBuilder.DropColumn(
            name: "approved_quantity",
            schema: "pos",
            table: "stock_request_lines");

        migrationBuilder.DropColumn(
            name: "linked_inventory_transfer_id",
            schema: "pos",
            table: "stock_requests");

        migrationBuilder.DropColumn(
            name: "dispatched_at_utc",
            schema: "pos",
            table: "stock_requests");

        migrationBuilder.DropColumn(
            name: "dispatched_by",
            schema: "pos",
            table: "stock_requests");

        migrationBuilder.DropColumn(
            name: "preparing_started_at_utc",
            schema: "pos",
            table: "stock_requests");

        migrationBuilder.DropColumn(
            name: "preparing_started_by",
            schema: "pos",
            table: "stock_requests");

        migrationBuilder.DropColumn(
            name: "approved_at_utc",
            schema: "pos",
            table: "stock_requests");

        migrationBuilder.DropColumn(
            name: "approved_by",
            schema: "pos",
            table: "stock_requests");

        migrationBuilder.Sql(
            """
            UPDATE pos.stock_requests
            SET status = 'InProgress'
            WHERE status IN ('Approved', 'Preparing', 'InTransit');
            """);

        migrationBuilder.AddCheckConstraint(
            name: "ck_stock_requests_status",
            schema: "pos",
            table: "stock_requests",
            sql: "status IN ('Pending', 'InProgress', 'PartiallyFulfilled', 'Fulfilled', 'Rejected', 'Cancelled')");
    }
}
