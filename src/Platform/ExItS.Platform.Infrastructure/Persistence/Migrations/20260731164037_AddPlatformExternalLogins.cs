using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformExternalLogins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "platform_external_logins",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    provider_subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    provider_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_external_logins", x => x.id);
                    table.ForeignKey(
                        name: "FK_platform_external_logins_platform_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_platform_external_logins_provider_provider_subject",
                schema: "platform",
                table: "platform_external_logins",
                columns: new[] { "provider", "provider_subject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_external_logins_user_id",
                schema: "platform",
                table: "platform_external_logins",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "platform_external_logins",
                schema: "platform");
        }
    }
}
