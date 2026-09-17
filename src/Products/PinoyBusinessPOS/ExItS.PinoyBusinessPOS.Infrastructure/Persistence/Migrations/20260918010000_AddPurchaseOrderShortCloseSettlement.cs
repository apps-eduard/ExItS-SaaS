using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(PosDbContext))]
[Migration("20260918010000_AddPurchaseOrderShortCloseSettlement")]
public partial class AddPurchaseOrderShortCloseSettlement : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "remaining_closed_at_utc",
            schema: "pos",
            table: "purchase_orders",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "remaining_closed_by_user_id",
            schema: "pos",
            table: "purchase_orders",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "remaining_closed_reason",
            schema: "pos",
            table: "purchase_orders",
            type: "character varying(512)",
            maxLength: 512,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "final_accepted_value",
            schema: "pos",
            table: "purchase_orders",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "cancelled_remaining_value",
            schema: "pos",
            table: "purchase_orders",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "refund_due_amount",
            schema: "pos",
            table: "purchase_orders",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "amount_paid_snapshot",
            schema: "pos",
            table: "purchase_orders",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "remaining_closed_at_utc",
            schema: "pos",
            table: "purchase_orders");

        migrationBuilder.DropColumn(
            name: "remaining_closed_by_user_id",
            schema: "pos",
            table: "purchase_orders");

        migrationBuilder.DropColumn(
            name: "remaining_closed_reason",
            schema: "pos",
            table: "purchase_orders");

        migrationBuilder.DropColumn(
            name: "final_accepted_value",
            schema: "pos",
            table: "purchase_orders");

        migrationBuilder.DropColumn(
            name: "cancelled_remaining_value",
            schema: "pos",
            table: "purchase_orders");

        migrationBuilder.DropColumn(
            name: "refund_due_amount",
            schema: "pos",
            table: "purchase_orders");

        migrationBuilder.DropColumn(
            name: "amount_paid_snapshot",
            schema: "pos",
            table: "purchase_orders");
    }
}
