using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerLinkTargetAndOrgNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "target_public_user_id",
                schema: "platform",
                table: "customer_link_requests",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "target_user_identity_id",
                schema: "platform",
                table: "customer_link_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "organization_in_app_notifications",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_user_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    preview = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    related_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    related_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    read_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_in_app_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_organization_in_app_notifications_organizations_organizatio~",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_organization_in_app_notifications_platform_users_recipient_~",
                        column: x => x.recipient_user_identity_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customer_link_requests_target_user_identity_id",
                schema: "platform",
                table: "customer_link_requests",
                column: "target_user_identity_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_in_app_notifications_org_recipient",
                schema: "platform",
                table: "organization_in_app_notifications",
                columns: new[] { "organization_id", "recipient_user_identity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_organization_in_app_notifications_recipient_related",
                schema: "platform",
                table: "organization_in_app_notifications",
                columns: new[] { "recipient_user_identity_id", "related_type", "related_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organization_in_app_notifications",
                schema: "platform");

            migrationBuilder.DropIndex(
                name: "ix_customer_link_requests_target_user_identity_id",
                schema: "platform",
                table: "customer_link_requests");

            migrationBuilder.DropColumn(
                name: "target_public_user_id",
                schema: "platform",
                table: "customer_link_requests");

            migrationBuilder.DropColumn(
                name: "target_user_identity_id",
                schema: "platform",
                table: "customer_link_requests");
        }
    }
}
