using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "platform_settings",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    platform_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    support_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    branding_logo_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    branding_primary_color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    branding_accent_color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    email_provider_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    smtp_host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    smtp_port = table.Column<int>(type: "integer", nullable: true),
                    smtp_username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    protected_smtp_password = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    smtp_password_configured = table.Column<bool>(type: "boolean", nullable: false),
                    from_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    from_address = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    smtp_security_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    admin_public_base_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    default_time_zone_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    default_locale = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    default_currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    default_country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    date_format = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    time_format = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_actor_id = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_settings", x => x.id);
                    table.CheckConstraint("ck_platform_settings_singleton", "id = 1");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "platform_settings",
                schema: "platform");
        }
    }
}
