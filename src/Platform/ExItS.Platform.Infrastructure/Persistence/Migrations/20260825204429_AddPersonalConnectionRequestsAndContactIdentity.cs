using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalConnectionRequestsAndContactIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "blocked_at_utc",
                schema: "platform",
                table: "personal_contacts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "connected_at_utc",
                schema: "platform",
                table: "personal_contacts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "resolved_public_user_id",
                schema: "platform",
                table: "personal_contacts",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "resolved_user_identity_id",
                schema: "platform",
                table: "personal_contacts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "personal_connection_requests",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    requester_user_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_user_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requester_contact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    accepted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    declined_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    responded_by_user_identity_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal_connection_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_personal_connection_requests_personal_contacts_requester_co~",
                        column: x => x.requester_contact_id,
                        principalSchema: "platform",
                        principalTable: "personal_contacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_personal_connection_requests_platform_users_requester_user_~",
                        column: x => x.requester_user_identity_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_personal_connection_requests_platform_users_target_user_ide~",
                        column: x => x.target_user_identity_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_personal_contacts_resolved_user_identity_id",
                schema: "platform",
                table: "personal_contacts",
                column: "resolved_user_identity_id");

            migrationBuilder.CreateIndex(
                name: "IX_personal_connection_requests_requester_contact_id",
                schema: "platform",
                table: "personal_connection_requests",
                column: "requester_contact_id");

            migrationBuilder.CreateIndex(
                name: "ix_personal_connection_requests_requester_target_status",
                schema: "platform",
                table: "personal_connection_requests",
                columns: new[] { "requester_user_identity_id", "target_user_identity_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_personal_connection_requests_target_user_identity_id",
                schema: "platform",
                table: "personal_connection_requests",
                column: "target_user_identity_id");

            migrationBuilder.AddForeignKey(
                name: "FK_personal_contacts_platform_users_resolved_user_identity_id",
                schema: "platform",
                table: "personal_contacts",
                column: "resolved_user_identity_id",
                principalSchema: "platform",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_personal_contacts_platform_users_resolved_user_identity_id",
                schema: "platform",
                table: "personal_contacts");

            migrationBuilder.DropTable(
                name: "personal_connection_requests",
                schema: "platform");

            migrationBuilder.DropIndex(
                name: "IX_personal_contacts_resolved_user_identity_id",
                schema: "platform",
                table: "personal_contacts");

            migrationBuilder.DropColumn(
                name: "blocked_at_utc",
                schema: "platform",
                table: "personal_contacts");

            migrationBuilder.DropColumn(
                name: "connected_at_utc",
                schema: "platform",
                table: "personal_contacts");

            migrationBuilder.DropColumn(
                name: "resolved_public_user_id",
                schema: "platform",
                table: "personal_contacts");

            migrationBuilder.DropColumn(
                name: "resolved_user_identity_id",
                schema: "platform",
                table: "personal_contacts");
        }
    }
}
