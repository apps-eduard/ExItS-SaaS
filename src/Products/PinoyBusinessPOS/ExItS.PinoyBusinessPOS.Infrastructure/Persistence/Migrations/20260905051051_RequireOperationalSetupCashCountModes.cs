using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RequireOperationalSetupCashCountModes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "opening_cash_count_mode",
                schema: "pos",
                table: "operational_setups",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Required",
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16,
                oldDefaultValue: "Optional");

            migrationBuilder.AlterColumn<string>(
                name: "closing_cash_count_mode",
                schema: "pos",
                table: "operational_setups",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Required",
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16,
                oldDefaultValue: "Optional");

            migrationBuilder.AlterColumn<string>(
                name: "cash_count_mode",
                schema: "pos",
                table: "operational_setups",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Required",
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16,
                oldDefaultValue: "Optional");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "opening_cash_count_mode",
                schema: "pos",
                table: "operational_setups",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Optional",
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16,
                oldDefaultValue: "Required");

            migrationBuilder.AlterColumn<string>(
                name: "closing_cash_count_mode",
                schema: "pos",
                table: "operational_setups",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Optional",
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16,
                oldDefaultValue: "Required");

            migrationBuilder.AlterColumn<string>(
                name: "cash_count_mode",
                schema: "pos",
                table: "operational_setups",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Optional",
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16,
                oldDefaultValue: "Required");
        }
    }
}
