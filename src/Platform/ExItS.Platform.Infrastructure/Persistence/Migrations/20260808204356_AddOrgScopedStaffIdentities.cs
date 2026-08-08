using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgScopedStaffIdentities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "home_organization_id",
                schema: "platform",
                table: "platform_users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "normalized_contact_email",
                schema: "platform",
                table: "platform_users",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "public_organization_id",
                schema: "platform",
                table: "organizations",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_platform_users_home_organization_id",
                schema: "platform",
                table: "platform_users",
                column: "home_organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_platform_users_normalized_contact_email",
                schema: "platform",
                table: "platform_users",
                column: "normalized_contact_email");

            migrationBuilder.CreateIndex(
                name: "ux_organizations_public_organization_id",
                schema: "platform",
                table: "organizations",
                column: "public_organization_id",
                unique: true,
                filter: "public_organization_id IS NOT NULL");

            // Deterministic backfill for existing orgs (legacy rows before PublicOrganizationId assignment).
            migrationBuilder.Sql(
                """
                WITH numbered AS (
                    SELECT id,
                           'ORG' || LPAD((ROW_NUMBER() OVER (ORDER BY created_at_utc, id))::text, 6, '0') AS public_id
                    FROM platform.organizations
                    WHERE public_organization_id IS NULL
                )
                UPDATE platform.organizations o
                SET public_organization_id = n.public_id
                FROM numbered n
                WHERE o.id = n.id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_platform_users_home_organization_id",
                schema: "platform",
                table: "platform_users");

            migrationBuilder.DropIndex(
                name: "ix_platform_users_normalized_contact_email",
                schema: "platform",
                table: "platform_users");

            migrationBuilder.DropIndex(
                name: "ux_organizations_public_organization_id",
                schema: "platform",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "home_organization_id",
                schema: "platform",
                table: "platform_users");

            migrationBuilder.DropColumn(
                name: "normalized_contact_email",
                schema: "platform",
                table: "platform_users");

            migrationBuilder.DropColumn(
                name: "public_organization_id",
                schema: "platform",
                table: "organizations");
        }
    }
}
