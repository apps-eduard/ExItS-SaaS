using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffProfileIdentitySnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "profile_date_of_birth",
                schema: "platform",
                table: "organization_memberships",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "profile_details_captured",
                schema: "platform",
                table: "organization_memberships",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "profile_email",
                schema: "platform",
                table: "organization_memberships",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "profile_first_name",
                schema: "platform",
                table: "organization_memberships",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "profile_gender",
                schema: "platform",
                table: "organization_memberships",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "profile_last_name",
                schema: "platform",
                table: "organization_memberships",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "profile_middle_name",
                schema: "platform",
                table: "organization_memberships",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "profile_mobile",
                schema: "platform",
                table: "organization_memberships",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "profile_nationality",
                schema: "platform",
                table: "organization_memberships",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "profile_photo_url",
                schema: "platform",
                table: "organization_memberships",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "profile_date_of_birth",
                schema: "platform",
                table: "organization_memberships");

            migrationBuilder.DropColumn(
                name: "profile_details_captured",
                schema: "platform",
                table: "organization_memberships");

            migrationBuilder.DropColumn(
                name: "profile_email",
                schema: "platform",
                table: "organization_memberships");

            migrationBuilder.DropColumn(
                name: "profile_first_name",
                schema: "platform",
                table: "organization_memberships");

            migrationBuilder.DropColumn(
                name: "profile_gender",
                schema: "platform",
                table: "organization_memberships");

            migrationBuilder.DropColumn(
                name: "profile_last_name",
                schema: "platform",
                table: "organization_memberships");

            migrationBuilder.DropColumn(
                name: "profile_middle_name",
                schema: "platform",
                table: "organization_memberships");

            migrationBuilder.DropColumn(
                name: "profile_mobile",
                schema: "platform",
                table: "organization_memberships");

            migrationBuilder.DropColumn(
                name: "profile_nationality",
                schema: "platform",
                table: "organization_memberships");

            migrationBuilder.DropColumn(
                name: "profile_photo_url",
                schema: "platform",
                table: "organization_memberships");
        }
    }
}
