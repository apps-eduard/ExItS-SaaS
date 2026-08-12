using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalFeatureEntitlements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "personal_feature_definitions",
                schema: "platform",
                columns: table => new
                {
                    feature_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal_feature_definitions", x => x.feature_code);
                });

            migrationBuilder.CreateTable(
                name: "personal_feature_entitlements",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    personal_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    feature_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    starts_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    grant_source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal_feature_entitlements", x => x.id);
                    table.ForeignKey(
                        name: "FK_personal_feature_entitlements_platform_users_personal_user_~",
                        column: x => x.personal_user_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_personal_feature_definitions_is_active",
                schema: "platform",
                table: "personal_feature_definitions",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_personal_feature_entitlements_user_feature_status",
                schema: "platform",
                table: "personal_feature_entitlements",
                columns: new[] { "personal_user_id", "feature_code", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_personal_feature_entitlements_user_feature_window",
                schema: "platform",
                table: "personal_feature_entitlements",
                columns: new[] { "personal_user_id", "feature_code", "starts_at_utc", "ends_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "personal_feature_definitions",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "personal_feature_entitlements",
                schema: "platform");
        }
    }
}
