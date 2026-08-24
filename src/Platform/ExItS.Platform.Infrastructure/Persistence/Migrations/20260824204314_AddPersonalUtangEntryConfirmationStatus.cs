using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalUtangEntryConfirmationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "dispute_reason",
                schema: "platform",
                table: "personal_utang_entries",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "resolved_at_utc",
                schema: "platform",
                table: "personal_utang_entries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "resolved_by_user_identity_id",
                schema: "platform",
                table: "personal_utang_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "platform",
                table: "personal_utang_entries",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Confirmed");

            // Existing rows predate confirmation workflow; preserve balances and treat as Confirmed.
            migrationBuilder.Sql(
                """
                UPDATE platform.personal_utang_entries
                SET status = 'Confirmed'
                WHERE status IS NULL OR status = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_personal_utang_entries_relationship_id_status",
                schema: "platform",
                table: "personal_utang_entries",
                columns: new[] { "relationship_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_personal_utang_entries_resolved_by_user_identity_id",
                schema: "platform",
                table: "personal_utang_entries",
                column: "resolved_by_user_identity_id");

            migrationBuilder.AddForeignKey(
                name: "FK_personal_utang_entries_platform_users_resolved_by_user_iden~",
                schema: "platform",
                table: "personal_utang_entries",
                column: "resolved_by_user_identity_id",
                principalSchema: "platform",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_personal_utang_entries_platform_users_resolved_by_user_iden~",
                schema: "platform",
                table: "personal_utang_entries");

            migrationBuilder.DropIndex(
                name: "IX_personal_utang_entries_relationship_id_status",
                schema: "platform",
                table: "personal_utang_entries");

            migrationBuilder.DropIndex(
                name: "IX_personal_utang_entries_resolved_by_user_identity_id",
                schema: "platform",
                table: "personal_utang_entries");

            migrationBuilder.DropColumn(
                name: "dispute_reason",
                schema: "platform",
                table: "personal_utang_entries");

            migrationBuilder.DropColumn(
                name: "resolved_at_utc",
                schema: "platform",
                table: "personal_utang_entries");

            migrationBuilder.DropColumn(
                name: "resolved_by_user_identity_id",
                schema: "platform",
                table: "personal_utang_entries");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "platform",
                table: "personal_utang_entries");
        }
    }
}
