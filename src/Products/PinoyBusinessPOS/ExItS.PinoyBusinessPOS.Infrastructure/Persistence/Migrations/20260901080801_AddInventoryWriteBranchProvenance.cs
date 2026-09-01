using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryWriteBranchProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                schema: "pos",
                table: "stock_counts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "receiving_branch_id",
                schema: "pos",
                table: "goods_receipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "receiving_branch_id",
                schema: "pos",
                table: "direct_purchase_receipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_counts_org_branch",
                schema: "pos",
                table: "stock_counts",
                columns: new[] { "organization_id", "branch_id" });

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipts_org_receiving_branch",
                schema: "pos",
                table: "goods_receipts",
                columns: new[] { "organization_id", "receiving_branch_id" });

            migrationBuilder.CreateIndex(
                name: "ix_direct_purchase_receipts_org_receiving_branch",
                schema: "pos",
                table: "direct_purchase_receipts",
                columns: new[] { "organization_id", "receiving_branch_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_stock_counts_org_branch",
                schema: "pos",
                table: "stock_counts");

            migrationBuilder.DropIndex(
                name: "ix_goods_receipts_org_receiving_branch",
                schema: "pos",
                table: "goods_receipts");

            migrationBuilder.DropIndex(
                name: "ix_direct_purchase_receipts_org_receiving_branch",
                schema: "pos",
                table: "direct_purchase_receipts");

            migrationBuilder.DropColumn(
                name: "branch_id",
                schema: "pos",
                table: "stock_counts");

            migrationBuilder.DropColumn(
                name: "receiving_branch_id",
                schema: "pos",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "receiving_branch_id",
                schema: "pos",
                table: "direct_purchase_receipts");
        }
    }
}
