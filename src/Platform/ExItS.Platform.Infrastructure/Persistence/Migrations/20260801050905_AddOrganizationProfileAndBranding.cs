using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationProfileAndBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "accent_color",
                schema: "platform",
                table: "organizations",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address_line1",
                schema: "platform",
                table: "organizations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address_line2",
                schema: "platform",
                table: "organizations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "brand_display_name",
                schema: "platform",
                table: "organizations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city",
                schema: "platform",
                table: "organizations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contact_email",
                schema: "platform",
                table: "organizations",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contact_phone",
                schema: "platform",
                table: "organizations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "country_code",
                schema: "platform",
                table: "organizations",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                schema: "platform",
                table: "organizations",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "legal_name",
                schema: "platform",
                table: "organizations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "locale",
                schema: "platform",
                table: "organizations",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "logo_url",
                schema: "platform",
                table: "organizations",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                schema: "platform",
                table: "organizations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "primary_color",
                schema: "platform",
                table: "organizations",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "region",
                schema: "platform",
                table: "organizations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "time_zone_id",
                schema: "platform",
                table: "organizations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "accent_color",
                schema: "platform",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "address_line1",
                schema: "platform",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "address_line2",
                schema: "platform",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "brand_display_name",
                schema: "platform",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "city",
                schema: "platform",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "contact_email",
                schema: "platform",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "contact_phone",
                schema: "platform",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "country_code",
                schema: "platform",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "currency_code",
                schema: "platform",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "legal_name",
                schema: "platform",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "locale",
                schema: "platform",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "logo_url",
                schema: "platform",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "postal_code",
                schema: "platform",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "primary_color",
                schema: "platform",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "region",
                schema: "platform",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "time_zone_id",
                schema: "platform",
                table: "organizations");
        }
    }
}
