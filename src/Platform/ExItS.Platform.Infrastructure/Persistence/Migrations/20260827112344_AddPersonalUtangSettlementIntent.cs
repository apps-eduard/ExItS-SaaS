using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalUtangSettlementIntent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "intent",
                schema: "platform",
                table: "personal_utang_entries",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Regular");

            migrationBuilder.AddColumn<decimal>(
                name: "settlement_balance_snapshot",
                schema: "platform",
                table: "personal_utang_entries",
                type: "numeric(18,4)",
                nullable: true);

            // Existing rows predate settlement intent; treat as Regular ledger entries.
            migrationBuilder.Sql(
                """
                UPDATE platform.personal_utang_entries
                SET intent = 'Regular'
                WHERE intent IS NULL OR intent = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "intent",
                schema: "platform",
                table: "personal_utang_entries");

            migrationBuilder.DropColumn(
                name: "settlement_balance_snapshot",
                schema: "platform",
                table: "personal_utang_entries");
        }
    }
}
