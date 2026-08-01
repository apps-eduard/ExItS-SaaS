using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountProfilesAndSessionScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_profiles",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_class = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_account_profiles_platform_users_user_identity_id",
                        column: x => x.user_identity_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_profiles_user_identity_id",
                schema: "platform",
                table: "account_profiles",
                column: "user_identity_id");

            migrationBuilder.CreateIndex(
                name: "IX_account_profiles_user_identity_id_account_class",
                schema: "platform",
                table: "account_profiles",
                columns: new[] { "user_identity_id", "account_class" },
                unique: true);

            // Backfill Personal profiles for every existing user (session rebind target).
            migrationBuilder.Sql(
                """
                INSERT INTO platform.account_profiles (id, user_identity_id, account_class, status, created_at_utc, updated_at_utc)
                SELECT gen_random_uuid(), u.id, 'Personal', 'Active', NOW(), NOW()
                FROM platform.platform_users u
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM platform.account_profiles p
                    WHERE p.user_identity_id = u.id AND p.account_class = 'Personal');
                """);

            migrationBuilder.AddColumn<string>(
                name: "account_class",
                schema: "platform",
                table: "platform_auth_sessions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "account_profile_id",
                schema: "platform",
                table: "platform_auth_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE platform.platform_auth_sessions s
                SET account_class = 'Personal',
                    account_profile_id = p.id
                FROM platform.account_profiles p
                WHERE p.user_identity_id = s.user_id
                  AND p.account_class = 'Personal'
                  AND s.account_profile_id IS NULL;
                """);

            // Orphan sessions (user deleted) cannot be rebound — revoke and remove.
            migrationBuilder.Sql(
                """
                DELETE FROM platform.platform_auth_sessions
                WHERE account_profile_id IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "account_class",
                schema: "platform",
                table: "platform_auth_sessions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "account_profile_id",
                schema: "platform",
                table: "platform_auth_sessions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_auth_sessions_account_profile_id",
                schema: "platform",
                table: "platform_auth_sessions",
                column: "account_profile_id");

            migrationBuilder.AddForeignKey(
                name: "FK_platform_auth_sessions_account_profiles_account_profile_id",
                schema: "platform",
                table: "platform_auth_sessions",
                column: "account_profile_id",
                principalSchema: "platform",
                principalTable: "account_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_platform_auth_sessions_account_profiles_account_profile_id",
                schema: "platform",
                table: "platform_auth_sessions");

            migrationBuilder.DropTable(
                name: "account_profiles",
                schema: "platform");

            migrationBuilder.DropIndex(
                name: "IX_platform_auth_sessions_account_profile_id",
                schema: "platform",
                table: "platform_auth_sessions");

            migrationBuilder.DropColumn(
                name: "account_class",
                schema: "platform",
                table: "platform_auth_sessions");

            migrationBuilder.DropColumn(
                name: "account_profile_id",
                schema: "platform",
                table: "platform_auth_sessions");
        }
    }
}
