using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerConnectionRemindersAndBlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_reminded_at_utc",
                schema: "platform",
                table: "customer_link_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reminder_count",
                schema: "platform",
                table: "customer_link_requests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "personal_organization_connection_blocks",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    personal_user_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    blocked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    unblocked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    source_customer_link_request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal_organization_connection_blocks", x => x.id);
                    table.ForeignKey(
                        name: "FK_personal_organization_connection_blocks_organizations_organ~",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_personal_organization_connection_blocks_platform_users_pers~",
                        column: x => x.personal_user_identity_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_personal_org_connection_blocks_personal_active",
                schema: "platform",
                table: "personal_organization_connection_blocks",
                column: "personal_user_identity_id",
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_personal_organization_connection_blocks_organization_id",
                schema: "platform",
                table: "personal_organization_connection_blocks",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ux_personal_org_connection_blocks_pair",
                schema: "platform",
                table: "personal_organization_connection_blocks",
                columns: new[] { "personal_user_identity_id", "organization_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "personal_organization_connection_blocks",
                schema: "platform");

            migrationBuilder.DropColumn(
                name: "last_reminded_at_utc",
                schema: "platform",
                table: "customer_link_requests");

            migrationBuilder.DropColumn(
                name: "reminder_count",
                schema: "platform",
                table: "customer_link_requests");
        }
    }
}
