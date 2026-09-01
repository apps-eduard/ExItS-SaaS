using System;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(PosDbContext))]
    [Migration("20260901120000_AddBranchInventoryReservations")]
    public partial class AddBranchInventoryReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "reserved_quantity",
                schema: "pos",
                table: "inventory_branch_balances",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_branch_balances_reserved_non_negative",
                schema: "pos",
                table: "inventory_branch_balances",
                sql: "reserved_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_branch_balances_reserved_not_over_on_hand",
                schema: "pos",
                table: "inventory_branch_balances",
                sql: "reserved_quantity <= on_hand_quantity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_branch_balances_reserved_not_over_on_hand",
                schema: "pos",
                table: "inventory_branch_balances");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_branch_balances_reserved_non_negative",
                schema: "pos",
                table: "inventory_branch_balances");

            migrationBuilder.DropColumn(
                name: "reserved_quantity",
                schema: "pos",
                table: "inventory_branch_balances");
        }
    }
}
