using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformUserPublicUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "public_user_id",
                schema: "platform",
                table: "platform_users",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_platform_users_public_user_id",
                schema: "platform",
                table: "platform_users",
                column: "public_user_id",
                unique: true,
                filter: "public_user_id IS NOT NULL");

            // Backfill immutable public IDs for existing users (collision-safe loop).
            migrationBuilder.Sql(
                """
                DO $$
                DECLARE r RECORD;
                DECLARE candidate text;
                BEGIN
                  FOR r IN SELECT id FROM platform.platform_users WHERE public_user_id IS NULL LOOP
                    LOOP
                      candidate := 'EX-'
                        || lpad((floor(random() * 10000))::int::text, 4, '0')
                        || '-'
                        || lpad((floor(random() * 10000))::int::text, 4, '0');
                      EXIT WHEN NOT EXISTS (
                        SELECT 1 FROM platform.platform_users u WHERE u.public_user_id = candidate);
                    END LOOP;
                    UPDATE platform.platform_users
                    SET public_user_id = candidate
                    WHERE id = r.id;
                  END LOOP;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_platform_users_public_user_id",
                schema: "platform",
                table: "platform_users");

            migrationBuilder.DropColumn(
                name: "public_user_id",
                schema: "platform",
                table: "platform_users");
        }
    }
}
