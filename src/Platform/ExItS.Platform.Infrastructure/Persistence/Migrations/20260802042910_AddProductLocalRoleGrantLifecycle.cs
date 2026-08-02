using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductLocalRoleGrantLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_product_local_role_grants_org_user_product_role",
                schema: "platform",
                table: "product_local_role_grants");

            migrationBuilder.AddColumn<string>(
                name: "reason",
                schema: "platform",
                table: "product_local_role_grants",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at_utc",
                schema: "platform",
                table: "product_local_role_grants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "revoked_by_user_identity_id",
                schema: "platform",
                table: "product_local_role_grants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "platform",
                table: "product_local_role_grants",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.Sql(
                """
                UPDATE platform.product_local_role_grants
                SET status = 'Active'
                WHERE status IS NULL OR status = '';
                """);

            migrationBuilder.CreateIndex(
                name: "ux_product_local_role_grants_active_org_user_product",
                schema: "platform",
                table: "product_local_role_grants",
                columns: new[] { "organization_id", "user_identity_id", "product_code" },
                unique: true,
                filter: "status = 'Active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_product_local_role_grants_active_org_user_product",
                schema: "platform",
                table: "product_local_role_grants");

            migrationBuilder.DropColumn(
                name: "reason",
                schema: "platform",
                table: "product_local_role_grants");

            migrationBuilder.DropColumn(
                name: "revoked_at_utc",
                schema: "platform",
                table: "product_local_role_grants");

            migrationBuilder.DropColumn(
                name: "revoked_by_user_identity_id",
                schema: "platform",
                table: "product_local_role_grants");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "platform",
                table: "product_local_role_grants");

            migrationBuilder.CreateIndex(
                name: "ux_product_local_role_grants_org_user_product_role",
                schema: "platform",
                table: "product_local_role_grants",
                columns: new[] { "organization_id", "user_identity_id", "product_code", "role_code" },
                unique: true);
        }
    }
}
