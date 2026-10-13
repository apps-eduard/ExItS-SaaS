using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "personal_addresses",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address_line_1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    address_line_2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    barangay = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    city_municipality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    province_state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    postal_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal_addresses", x => x.id);
                    table.ForeignKey(
                        name: "FK_personal_addresses_platform_users_user_identity_id",
                        column: x => x.user_identity_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO platform.personal_addresses (
                    id, user_identity_id, address_type, country, address_line_1, address_line_2,
                    barangay, city_municipality, province_state, postal_code, is_primary,
                    created_at_utc, updated_at_utc)
                SELECT gen_random_uuid(), user_identity_id, 'Home',
                    COALESCE(NULLIF(btrim(country), ''), 'Philippines'),
                    COALESCE(address_line_1, ''),
                    address_line_2,
                    barangay,
                    COALESCE(city_municipality, ''),
                    COALESCE(province, ''),
                    postal_code,
                    TRUE,
                    created_at_utc,
                    updated_at_utc
                FROM platform.personal_user_profiles
                WHERE NULLIF(btrim(country), '') IS NOT NULL
                   OR NULLIF(btrim(address_line_1), '') IS NOT NULL
                   OR NULLIF(btrim(address_line_2), '') IS NOT NULL
                   OR NULLIF(btrim(barangay), '') IS NOT NULL
                   OR NULLIF(btrim(city_municipality), '') IS NOT NULL
                   OR NULLIF(btrim(province), '') IS NOT NULL
                   OR NULLIF(btrim(postal_code), '') IS NOT NULL
                   OR NULLIF(btrim(region), '') IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "address_line_1",
                schema: "platform",
                table: "personal_user_profiles");

            migrationBuilder.DropColumn(
                name: "address_line_2",
                schema: "platform",
                table: "personal_user_profiles");

            migrationBuilder.DropColumn(
                name: "barangay",
                schema: "platform",
                table: "personal_user_profiles");

            migrationBuilder.DropColumn(
                name: "city_municipality",
                schema: "platform",
                table: "personal_user_profiles");

            migrationBuilder.DropColumn(
                name: "city_psgc_code",
                schema: "platform",
                table: "personal_user_profiles");

            migrationBuilder.DropColumn(
                name: "country",
                schema: "platform",
                table: "personal_user_profiles");

            migrationBuilder.DropColumn(
                name: "is_primary_address",
                schema: "platform",
                table: "personal_user_profiles");

            migrationBuilder.DropColumn(
                name: "postal_code",
                schema: "platform",
                table: "personal_user_profiles");

            migrationBuilder.DropColumn(
                name: "province",
                schema: "platform",
                table: "personal_user_profiles");

            migrationBuilder.DropColumn(
                name: "region",
                schema: "platform",
                table: "personal_user_profiles");

            migrationBuilder.CreateIndex(
                name: "ix_personal_addresses_user_identity_id",
                schema: "platform",
                table: "personal_addresses",
                column: "user_identity_id");

            migrationBuilder.CreateIndex(
                name: "ux_personal_addresses_one_primary",
                schema: "platform",
                table: "personal_addresses",
                columns: new[] { "user_identity_id", "is_primary" },
                unique: true,
                filter: "is_primary = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "address_line_1",
                schema: "platform",
                table: "personal_user_profiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address_line_2",
                schema: "platform",
                table: "personal_user_profiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "barangay",
                schema: "platform",
                table: "personal_user_profiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city_municipality",
                schema: "platform",
                table: "personal_user_profiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city_psgc_code",
                schema: "platform",
                table: "personal_user_profiles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "country",
                schema: "platform",
                table: "personal_user_profiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_primary_address",
                schema: "platform",
                table: "personal_user_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                schema: "platform",
                table: "personal_user_profiles",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "province",
                schema: "platform",
                table: "personal_user_profiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "region",
                schema: "platform",
                table: "personal_user_profiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE platform.personal_user_profiles AS profile
                SET country = address.country,
                    address_line_1 = NULLIF(address.address_line_1, ''),
                    address_line_2 = address.address_line_2,
                    barangay = address.barangay,
                    city_municipality = NULLIF(address.city_municipality, ''),
                    province = NULLIF(address.province_state, ''),
                    postal_code = address.postal_code,
                    is_primary_address = TRUE
                FROM platform.personal_addresses AS address
                WHERE address.user_identity_id = profile.user_identity_id
                  AND address.is_primary = TRUE;
                """);

            migrationBuilder.DropTable(
                name: "personal_addresses",
                schema: "platform");
        }
    }
}
