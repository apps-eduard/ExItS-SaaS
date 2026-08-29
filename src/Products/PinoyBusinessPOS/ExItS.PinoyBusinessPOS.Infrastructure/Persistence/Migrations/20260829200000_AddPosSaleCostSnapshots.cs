using System;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(PosDbContext))]
    [Migration("20260829200000_AddPosSaleCostSnapshots")]
    public partial class AddPosSaleCostSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cost_status",
                schema: "pos",
                table: "sales",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "total_cost_snapshot",
                schema: "pos",
                table: "sales",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_cost_snapshot",
                schema: "pos",
                table: "sale_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "line_cost_snapshot",
                schema: "pos",
                table: "sale_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_cost_status",
                schema: "pos",
                table: "sales",
                sql: "cost_status IS NULL OR cost_status IN ('Complete', 'Partial', 'Unavailable')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_cost_status",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropColumn(name: "line_cost_snapshot", schema: "pos", table: "sale_lines");
            migrationBuilder.DropColumn(name: "unit_cost_snapshot", schema: "pos", table: "sale_lines");
            migrationBuilder.DropColumn(name: "total_cost_snapshot", schema: "pos", table: "sales");
            migrationBuilder.DropColumn(name: "cost_status", schema: "pos", table: "sales");
        }
    }
}
