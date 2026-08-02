using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalUtangInvitationsAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "personal_in_app_notifications",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_personal_in_app_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_personal_in_app_notifications_platform_users_recipient_user~",
                        column: x => x.recipient_user_identity_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "personal_reminders",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    debt_relationship_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    schedule_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    message = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: true),
                    scheduled_for_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    next_delivery_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    delivered_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    delivery_attempt_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal_reminders", x => x.id);
                    table.ForeignKey(
                        name: "FK_personal_reminders_personal_debt_relationships_debt_relatio~",
                        column: x => x.debt_relationship_id,
                        principalSchema: "platform",
                        principalTable: "personal_debt_relationships",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_personal_reminders_platform_users_created_by_user_identity_~",
                        column: x => x.created_by_user_identity_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "personal_utang_invitations",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    debt_relationship_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invitee_contact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invited_by_user_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invite_target_normalized_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    invite_target_phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    accepted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    declined_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    accepted_by_user_identity_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal_utang_invitations", x => x.id);
                    table.ForeignKey(
                        name: "FK_personal_utang_invitations_personal_contacts_invitee_contac~",
                        column: x => x.invitee_contact_id,
                        principalSchema: "platform",
                        principalTable: "personal_contacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_personal_utang_invitations_personal_debt_relationships_debt~",
                        column: x => x.debt_relationship_id,
                        principalSchema: "platform",
                        principalTable: "personal_debt_relationships",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_personal_utang_invitations_platform_users_accepted_by_user_~",
                        column: x => x.accepted_by_user_identity_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_personal_utang_invitations_platform_users_invited_by_user_i~",
                        column: x => x.invited_by_user_identity_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "personal_notification_deliveries",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reminder_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient_user_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    preview_text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    attempted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    delivered_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal_notification_deliveries", x => x.id);
                    table.ForeignKey(
                        name: "FK_personal_notification_deliveries_personal_in_app_notificati~",
                        column: x => x.notification_id,
                        principalSchema: "platform",
                        principalTable: "personal_in_app_notifications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_personal_notification_deliveries_personal_reminders_reminde~",
                        column: x => x.reminder_id,
                        principalSchema: "platform",
                        principalTable: "personal_reminders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_personal_notification_deliveries_platform_users_recipient_u~",
                        column: x => x.recipient_user_identity_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_personal_in_app_notifications_recipient_user_identity_id",
                schema: "platform",
                table: "personal_in_app_notifications",
                column: "recipient_user_identity_id");

            migrationBuilder.CreateIndex(
                name: "IX_personal_notification_deliveries_notification_id",
                schema: "platform",
                table: "personal_notification_deliveries",
                column: "notification_id");

            migrationBuilder.CreateIndex(
                name: "IX_personal_notification_deliveries_recipient_user_identity_id",
                schema: "platform",
                table: "personal_notification_deliveries",
                column: "recipient_user_identity_id");

            migrationBuilder.CreateIndex(
                name: "IX_personal_notification_deliveries_reminder_id",
                schema: "platform",
                table: "personal_notification_deliveries",
                column: "reminder_id");

            migrationBuilder.CreateIndex(
                name: "IX_personal_reminders_created_by_user_identity_id",
                schema: "platform",
                table: "personal_reminders",
                column: "created_by_user_identity_id");

            migrationBuilder.CreateIndex(
                name: "IX_personal_reminders_debt_relationship_id",
                schema: "platform",
                table: "personal_reminders",
                column: "debt_relationship_id");

            migrationBuilder.CreateIndex(
                name: "IX_personal_reminders_next_delivery_at_utc",
                schema: "platform",
                table: "personal_reminders",
                column: "next_delivery_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_personal_utang_invitations_accepted_by_user_identity_id",
                schema: "platform",
                table: "personal_utang_invitations",
                column: "accepted_by_user_identity_id");

            migrationBuilder.CreateIndex(
                name: "IX_personal_utang_invitations_invite_target_normalized_email",
                schema: "platform",
                table: "personal_utang_invitations",
                column: "invite_target_normalized_email");

            migrationBuilder.CreateIndex(
                name: "IX_personal_utang_invitations_invited_by_user_identity_id",
                schema: "platform",
                table: "personal_utang_invitations",
                column: "invited_by_user_identity_id");

            migrationBuilder.CreateIndex(
                name: "IX_personal_utang_invitations_invitee_contact_id",
                schema: "platform",
                table: "personal_utang_invitations",
                column: "invitee_contact_id");

            migrationBuilder.CreateIndex(
                name: "ix_personal_utang_invitations_relationship_contact_status",
                schema: "platform",
                table: "personal_utang_invitations",
                columns: new[] { "debt_relationship_id", "invitee_contact_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_personal_utang_invitations_token_hash",
                schema: "platform",
                table: "personal_utang_invitations",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "personal_notification_deliveries",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "personal_utang_invitations",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "personal_in_app_notifications",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "personal_reminders",
                schema: "platform");
        }
    }
}
