using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(PlatformDbContext))]
[Migration("20260803030000_AddOrganizationInvitationInviteFields")]
public partial class AddOrganizationInvitationInviteFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "branch",
            schema: "platform",
            table: "organization_invitations",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "first_name",
            schema: "platform",
            table: "organization_invitations",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "invitee_display_name",
            schema: "platform",
            table: "organization_invitations",
            type: "character varying(256)",
            maxLength: 256,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "last_name",
            schema: "platform",
            table: "organization_invitations",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "product_role",
            schema: "platform",
            table: "organization_invitations",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "branch",
            schema: "platform",
            table: "organization_invitations");

        migrationBuilder.DropColumn(
            name: "first_name",
            schema: "platform",
            table: "organization_invitations");

        migrationBuilder.DropColumn(
            name: "invitee_display_name",
            schema: "platform",
            table: "organization_invitations");

        migrationBuilder.DropColumn(
            name: "last_name",
            schema: "platform",
            table: "organization_invitations");

        migrationBuilder.DropColumn(
            name: "product_role",
            schema: "platform",
            table: "organization_invitations");
    }
}
