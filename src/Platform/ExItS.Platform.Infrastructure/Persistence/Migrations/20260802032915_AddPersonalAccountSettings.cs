using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalAccountSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "personal_account_settings",
                schema: "platform",
                columns: table => new
                {
                    user_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_notifications_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    push_notifications_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    in_app_notifications_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    reminder_notifications_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal_account_settings", x => x.user_identity_id);
                    table.ForeignKey(
                        name: "FK_personal_account_settings_platform_users_user_identity_id",
                        column: x => x.user_identity_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "personal_account_settings",
                schema: "platform");
        }
    }
}
