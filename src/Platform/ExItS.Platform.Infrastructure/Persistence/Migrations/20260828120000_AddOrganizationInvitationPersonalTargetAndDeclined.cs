using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddOrganizationInvitationPersonalTargetAndDeclined : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "declined_at_utc",
            schema: "platform",
            table: "organization_invitations",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "target_personal_user_id",
            schema: "platform",
            table: "organization_invitations",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "target_public_user_id",
            schema: "platform",
            table: "organization_invitations",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_organization_invitations_target_personal_user_id",
            schema: "platform",
            table: "organization_invitations",
            column: "target_personal_user_id");

        migrationBuilder.CreateIndex(
            name: "ux_organization_invitations_pending_target_user",
            schema: "platform",
            table: "organization_invitations",
            columns: new[] { "organization_id", "target_personal_user_id" },
            unique: true,
            filter: "status = 'Pending' AND target_personal_user_id IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_organization_invitations_pending_target_user",
            schema: "platform",
            table: "organization_invitations");

        migrationBuilder.DropIndex(
            name: "ix_organization_invitations_target_personal_user_id",
            schema: "platform",
            table: "organization_invitations");

        migrationBuilder.DropColumn(
            name: "declined_at_utc",
            schema: "platform",
            table: "organization_invitations");

        migrationBuilder.DropColumn(
            name: "target_personal_user_id",
            schema: "platform",
            table: "organization_invitations");

        migrationBuilder.DropColumn(
            name: "target_public_user_id",
            schema: "platform",
            table: "organization_invitations");
    }
}
