using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStartBusinessAndUtangMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "destination_credit_customer_id",
                schema: "platform",
                table: "personal_debt_relationships",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "destination_organization_id",
                schema: "platform",
                table: "personal_debt_relationships",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "migration_batch_id",
                schema: "platform",
                table: "personal_debt_relationships",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "business_credit_opening_balances",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credit_customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    effective_date_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    migration_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    imported_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    imported_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    destination_product = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_business_credit_opening_balances", x => x.id);
                    table.ForeignKey(
                        name: "FK_business_credit_opening_balances_business_customers_busines~",
                        column: x => x.business_customer_id,
                        principalSchema: "platform",
                        principalTable: "business_customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_business_credit_opening_balances_credit_customers_credit_cu~",
                        column: x => x.credit_customer_id,
                        principalSchema: "platform",
                        principalTable: "credit_customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_business_credit_opening_balances_organizations_organization~",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "personal_utang_migration_batches",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_product_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    effective_migration_date_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    include_contact = table.Column<bool>(type: "boolean", nullable: false),
                    include_opening_balance = table.Column<bool>(type: "boolean", nullable: false),
                    include_selected_history = table.Column<bool>(type: "boolean", nullable: false),
                    include_due_dates_and_notes = table.Column<bool>(type: "boolean", nullable: false),
                    source_disposition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    linked_participant_consent_acknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    confirmation_token = table.Column<Guid>(type: "uuid", nullable: false),
                    previewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    executed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal_utang_migration_batches", x => x.id);
                    table.ForeignKey(
                        name: "FK_personal_utang_migration_batches_organizations_destination_~",
                        column: x => x.destination_organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_personal_utang_migration_batches_platform_users_owner_user_~",
                        column: x => x.owner_user_identity_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_local_role_grants",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    role_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    granted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    granted_by_user_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_local_role_grants", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_local_role_grants_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_local_role_grants_platform_users_user_identity_id",
                        column: x => x.user_identity_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "personal_utang_migration_items",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    destination_record_id = table.Column<Guid>(type: "uuid", nullable: true),
                    opening_balance_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    notes_snapshot = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    due_date_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    history_entry_ids_csv = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    blocked_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal_utang_migration_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_personal_utang_migration_items_personal_utang_migration_bat~",
                        column: x => x.batch_id,
                        principalSchema: "platform",
                        principalTable: "personal_utang_migration_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_business_credit_opening_balances_business_customer_id",
                schema: "platform",
                table: "business_credit_opening_balances",
                column: "business_customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_business_credit_opening_balances_credit_customer_id",
                schema: "platform",
                table: "business_credit_opening_balances",
                column: "credit_customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_business_credit_opening_balances_org",
                schema: "platform",
                table: "business_credit_opening_balances",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ux_business_credit_opening_balances_org_source",
                schema: "platform",
                table: "business_credit_opening_balances",
                columns: new[] { "organization_id", "source_type", "source_record_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_personal_utang_migration_batches_destination_org",
                schema: "platform",
                table: "personal_utang_migration_batches",
                column: "destination_organization_id");

            migrationBuilder.CreateIndex(
                name: "ux_personal_utang_migration_batches_owner_idempotency",
                schema: "platform",
                table: "personal_utang_migration_batches",
                columns: new[] { "owner_user_identity_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_personal_utang_migration_items_batch_id",
                schema: "platform",
                table: "personal_utang_migration_items",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_personal_utang_migration_items_source_status",
                schema: "platform",
                table: "personal_utang_migration_items",
                columns: new[] { "source_type", "source_record_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_product_local_role_grants_user_identity_id",
                schema: "platform",
                table: "product_local_role_grants",
                column: "user_identity_id");

            migrationBuilder.CreateIndex(
                name: "ux_product_local_role_grants_org_user_product_role",
                schema: "platform",
                table: "product_local_role_grants",
                columns: new[] { "organization_id", "user_identity_id", "product_code", "role_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "business_credit_opening_balances",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "personal_utang_migration_items",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "product_local_role_grants",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "personal_utang_migration_batches",
                schema: "platform");

            migrationBuilder.DropColumn(
                name: "destination_credit_customer_id",
                schema: "platform",
                table: "personal_debt_relationships");

            migrationBuilder.DropColumn(
                name: "destination_organization_id",
                schema: "platform",
                table: "personal_debt_relationships");

            migrationBuilder.DropColumn(
                name: "migration_batch_id",
                schema: "platform",
                table: "personal_debt_relationships");
        }
    }
}
