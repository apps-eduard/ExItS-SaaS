using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationMembershipBusinessProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "department",
                schema: "platform",
                table: "organization_memberships",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "job_title",
                schema: "platform",
                table: "organization_memberships",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "work_phone",
                schema: "platform",
                table: "organization_memberships",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "work_email",
                schema: "platform",
                table: "organization_memberships",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            // Default false for all, then enable Owner rows (recommended Owner default).
            migrationBuilder.AddColumn<bool>(
                name: "is_business_contact",
                schema: "platform",
                table: "organization_memberships",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE platform.organization_memberships
                SET is_business_contact = TRUE
                WHERE role = 'OrganizationOwner';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_business_contact",
                schema: "platform",
                table: "organization_memberships");

            migrationBuilder.DropColumn(
                name: "work_email",
                schema: "platform",
                table: "organization_memberships");

            migrationBuilder.DropColumn(
                name: "work_phone",
                schema: "platform",
                table: "organization_memberships");

            migrationBuilder.DropColumn(
                name: "job_title",
                schema: "platform",
                table: "organization_memberships");

            migrationBuilder.DropColumn(
                name: "department",
                schema: "platform",
                table: "organization_memberships");
        }
    }
}
