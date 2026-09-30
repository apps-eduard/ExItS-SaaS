using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalSharedUtangPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "confirmation_source",
                schema: "platform",
                table: "personal_utang_entries",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.Sql(
                """
                UPDATE platform.personal_utang_entries
                SET confirmation_source = CASE
                    WHEN status = 'Confirmed' AND resolved_by_user_identity_id IS NOT NULL THEN 'Manual'
                    ELSE 'None'
                END;
                """);

            migrationBuilder.CreateTable(
                name: "personal_shared_utang_preferences",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    counterparty_user_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receive_shared_utang = table.Column<bool>(type: "boolean", nullable: false),
                    auto_accept_shared_utang = table.Column<bool>(type: "boolean", nullable: false),
                    shared_utang_notifications = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal_shared_utang_preferences", x => x.id);
                    table.ForeignKey(
                        name: "FK_personal_shared_utang_preferences_platform_users_counterpar~",
                        column: x => x.counterparty_user_identity_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_personal_shared_utang_preferences_platform_users_owner_user~",
                        column: x => x.owner_user_identity_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_personal_shared_utang_preferences_counterparty_user_identit~",
                schema: "platform",
                table: "personal_shared_utang_preferences",
                column: "counterparty_user_identity_id");

            migrationBuilder.CreateIndex(
                name: "ix_personal_shared_utang_preferences_owner_counterparty",
                schema: "platform",
                table: "personal_shared_utang_preferences",
                columns: new[] { "owner_user_identity_id", "counterparty_user_identity_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "personal_shared_utang_preferences",
                schema: "platform");

            migrationBuilder.DropColumn(
                name: "confirmation_source",
                schema: "platform",
                table: "personal_utang_entries");
        }
    }
}
