using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalUtang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "personal_contacts",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    linked_user_identity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal_contacts", x => x.id);
                    table.ForeignKey(
                        name: "FK_personal_contacts_platform_users_linked_user_identity_id",
                        column: x => x.linked_user_identity_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_personal_contacts_platform_users_owner_user_identity_id",
                        column: x => x.owner_user_identity_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "personal_debt_relationships",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    creditor_user_identity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    creditor_contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    debtor_user_identity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    debtor_contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    currency_code = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    current_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    due_date_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    aggregate_version = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal_debt_relationships", x => x.id);
                    table.CheckConstraint("ck_personal_debt_relationships_creditor_side", "(creditor_user_identity_id IS NOT NULL AND creditor_contact_id IS NULL) OR (creditor_user_identity_id IS NULL AND creditor_contact_id IS NOT NULL)");
                    table.CheckConstraint("ck_personal_debt_relationships_debtor_side", "(debtor_user_identity_id IS NOT NULL AND debtor_contact_id IS NULL) OR (debtor_user_identity_id IS NULL AND debtor_contact_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_personal_debt_relationships_personal_contacts_creditor_cont~",
                        column: x => x.creditor_contact_id,
                        principalSchema: "platform",
                        principalTable: "personal_contacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_personal_debt_relationships_personal_contacts_debtor_contac~",
                        column: x => x.debtor_contact_id,
                        principalSchema: "platform",
                        principalTable: "personal_contacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_personal_debt_relationships_platform_users_creditor_user_id~",
                        column: x => x.creditor_user_identity_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_personal_debt_relationships_platform_users_debtor_user_iden~",
                        column: x => x.debtor_user_identity_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "personal_utang_entries",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    relationship_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    signed_delta = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    balance_after = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    due_date_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_user_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal_utang_entries", x => x.id);
                    table.CheckConstraint("ck_personal_utang_entries_positive_amount", "amount > 0");
                    table.ForeignKey(
                        name: "FK_personal_utang_entries_personal_debt_relationships_relation~",
                        column: x => x.relationship_id,
                        principalSchema: "platform",
                        principalTable: "personal_debt_relationships",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_personal_utang_entries_platform_users_created_by_user_ident~",
                        column: x => x.created_by_user_identity_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_personal_contacts_linked_user_identity_id",
                schema: "platform",
                table: "personal_contacts",
                column: "linked_user_identity_id");

            migrationBuilder.CreateIndex(
                name: "IX_personal_contacts_owner_user_identity_id",
                schema: "platform",
                table: "personal_contacts",
                column: "owner_user_identity_id");

            migrationBuilder.CreateIndex(
                name: "IX_personal_debt_relationships_creditor_contact_id",
                schema: "platform",
                table: "personal_debt_relationships",
                column: "creditor_contact_id");

            migrationBuilder.CreateIndex(
                name: "IX_personal_debt_relationships_creditor_user_identity_id",
                schema: "platform",
                table: "personal_debt_relationships",
                column: "creditor_user_identity_id");

            migrationBuilder.CreateIndex(
                name: "IX_personal_debt_relationships_debtor_contact_id",
                schema: "platform",
                table: "personal_debt_relationships",
                column: "debtor_contact_id");

            migrationBuilder.CreateIndex(
                name: "IX_personal_debt_relationships_debtor_user_identity_id",
                schema: "platform",
                table: "personal_debt_relationships",
                column: "debtor_user_identity_id");

            migrationBuilder.CreateIndex(
                name: "IX_personal_utang_entries_created_by_user_identity_id",
                schema: "platform",
                table: "personal_utang_entries",
                column: "created_by_user_identity_id");

            migrationBuilder.CreateIndex(
                name: "IX_personal_utang_entries_relationship_id",
                schema: "platform",
                table: "personal_utang_entries",
                column: "relationship_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "personal_utang_entries",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "personal_debt_relationships",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "personal_contacts",
                schema: "platform");
        }
    }
}
