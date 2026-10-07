using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffPasswordResetRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "staff_password_reset_requests",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    decided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decided_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_password_reset_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_staff_password_reset_requests_organization_memberships_memb~",
                        column: x => x.membership_id,
                        principalSchema: "platform",
                        principalTable: "organization_memberships",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_staff_password_reset_requests_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_staff_password_reset_requests_platform_users_decided_by_use~",
                        column: x => x.decided_by_user_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_staff_password_reset_requests_platform_users_requested_by_u~",
                        column: x => x.requested_by_user_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_staff_password_reset_requests_platform_users_staff_user_id",
                        column: x => x.staff_user_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_staff_password_reset_requests_decided_by_user_id",
                schema: "platform",
                table: "staff_password_reset_requests",
                column: "decided_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_password_reset_requests_membership_id",
                schema: "platform",
                table: "staff_password_reset_requests",
                column: "membership_id");

            migrationBuilder.CreateIndex(
                name: "ix_staff_password_reset_requests_org_status",
                schema: "platform",
                table: "staff_password_reset_requests",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_staff_password_reset_requests_requested_by_user_id",
                schema: "platform",
                table: "staff_password_reset_requests",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_staff_password_reset_requests_open_staff",
                schema: "platform",
                table: "staff_password_reset_requests",
                column: "staff_user_id",
                unique: true,
                filter: "status IN ('Pending', 'Approved')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staff_password_reset_requests",
                schema: "platform");
        }
    }
}
