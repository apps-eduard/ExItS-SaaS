using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalUserProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "personal_user_profiles",
                schema: "platform",
                columns: table => new
                {
                    user_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    middle_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    gender = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    nationality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    profile_photo_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    alternative_mobile = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    address_line_1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address_line_2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    barangay = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    city_municipality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    province = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    city_psgc_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    is_primary_address = table.Column<bool>(type: "boolean", nullable: false),
                    show_profile_photo = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    show_display_name = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    show_city = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    show_mobile = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    show_email = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal_user_profiles", x => x.user_identity_id);
                    table.ForeignKey(
                        name: "FK_personal_user_profiles_platform_users_user_identity_id",
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
                name: "personal_user_profiles",
                schema: "platform");
        }
    }
}
