using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnrichPosStockCountDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "count_date",
                schema: "pos",
                table: "stock_counts",
                type: "date",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE pos.stock_counts
                SET count_date = (created_at_utc AT TIME ZONE 'UTC')::date
                WHERE count_date IS NULL;
                """);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "count_date",
                schema: "pos",
                table: "stock_counts",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "count_date",
                schema: "pos",
                table: "stock_counts");
        }
    }
}
