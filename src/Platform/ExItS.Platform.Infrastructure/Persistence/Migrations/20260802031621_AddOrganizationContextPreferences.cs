using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationContextPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organization_context_preferences",
                schema: "platform",
                columns: table => new
                {
                    user_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_active_organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_context_preferences", x => x.user_identity_id);
                    table.ForeignKey(
                        name: "FK_organization_context_preferences_organizations_last_active_~",
                        column: x => x.last_active_organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_organization_context_preferences_platform_users_user_identi~",
                        column: x => x.user_identity_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_organization_context_preferences_last_active_organization_id",
                schema: "platform",
                table: "organization_context_preferences",
                column: "last_active_organization_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organization_context_preferences",
                schema: "platform");
        }
    }
}
