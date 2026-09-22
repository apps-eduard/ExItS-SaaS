using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds waived quantity on transfer lines and receive follow-up classification on receipt lines.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260922120000_AddInventoryTransferWaivedAndFollowUp")]
public partial class AddInventoryTransferWaivedAndFollowUp : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "waived_qty",
            schema: "pos",
            table: "inventory_transfer_lines",
            type: "numeric(18,3)",
            precision: 18,
            scale: 3,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "damaged_follow_up",
            schema: "pos",
            table: "inventory_transfer_receipt_lines",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "other_follow_up",
            schema: "pos",
            table: "inventory_transfer_receipt_lines",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "quantity_waived",
            schema: "pos",
            table: "inventory_transfer_receipt_lines",
            type: "numeric(18,3)",
            precision: 18,
            scale: 3,
            nullable: false,
            defaultValue: 0m);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "waived_qty",
            schema: "pos",
            table: "inventory_transfer_lines");

        migrationBuilder.DropColumn(
            name: "damaged_follow_up",
            schema: "pos",
            table: "inventory_transfer_receipt_lines");

        migrationBuilder.DropColumn(
            name: "other_follow_up",
            schema: "pos",
            table: "inventory_transfer_receipt_lines");

        migrationBuilder.DropColumn(
            name: "quantity_waived",
            schema: "pos",
            table: "inventory_transfer_receipt_lines");
    }
}
