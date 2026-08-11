using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleLineSellingModeSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "selling_mode_snapshot",
                schema: "pos",
                table: "sale_lines",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "PerItem");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_lines_selling_mode",
                schema: "pos",
                table: "sale_lines",
                sql: "selling_mode_snapshot IN ('PerItem','ByWeight')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_lines_selling_mode",
                schema: "pos",
                table: "sale_lines");

            migrationBuilder.DropColumn(
                name: "selling_mode_snapshot",
                schema: "pos",
                table: "sale_lines");
        }
    }
}
