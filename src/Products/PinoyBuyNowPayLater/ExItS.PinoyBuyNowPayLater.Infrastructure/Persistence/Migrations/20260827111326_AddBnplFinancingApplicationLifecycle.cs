using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBuyNowPayLater.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBnplFinancingApplicationLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "financing_applications",
                schema: "bnpl",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    purchase_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    down_payment_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    requested_finance_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    purchase_description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    merchant_product_reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    aggregate_version = table.Column<int>(type: "integer", nullable: false),
                    eligibility_approved = table.Column<bool>(type: "boolean", nullable: false),
                    eligibility_decided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    eligibility_decided_by_actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    eligibility_note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    current_offer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accepted_offer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financing_applications", x => x.id);
                    table.CheckConstraint("ck_bnpl_financing_applications_status", "status IN ('Draft', 'PendingEligibility', 'Offered', 'CustomerAccepted', 'ApprovedPendingSale', 'Declined', 'Cancelled')");
                    table.ForeignKey(
                        name: "fk_bnpl_financing_applications_customer",
                        column: x => x.customer_id,
                        principalSchema: "bnpl",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "financing_decisions",
                schema: "bnpl",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stage = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    offer_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financing_decisions", x => x.id);
                    table.ForeignKey(
                        name: "FK_financing_decisions_financing_applications_application_id",
                        column: x => x.application_id,
                        principalSchema: "bnpl",
                        principalTable: "financing_applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "financing_offers",
                schema: "bnpl",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    purchase_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    down_payment_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    financed_principal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_by_actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_superseded = table.Column<bool>(type: "boolean", nullable: false),
                    accepted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    accepted_by_actor_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financing_offers", x => x.id);
                    table.ForeignKey(
                        name: "FK_financing_offers_financing_applications_application_id",
                        column: x => x.application_id,
                        principalSchema: "bnpl",
                        principalTable: "financing_applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bnpl_financing_applications_org_branch",
                schema: "bnpl",
                table: "financing_applications",
                columns: new[] { "organization_id", "branch_id" });

            migrationBuilder.CreateIndex(
                name: "ix_bnpl_financing_applications_org_customer",
                schema: "bnpl",
                table: "financing_applications",
                columns: new[] { "organization_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_bnpl_financing_applications_org_status",
                schema: "bnpl",
                table: "financing_applications",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_financing_applications_customer_id",
                schema: "bnpl",
                table: "financing_applications",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_bnpl_financing_decisions_application_time",
                schema: "bnpl",
                table: "financing_decisions",
                columns: new[] { "application_id", "decided_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_bnpl_financing_offers_application_version",
                schema: "bnpl",
                table: "financing_offers",
                columns: new[] { "application_id", "version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "financing_decisions",
                schema: "bnpl");

            migrationBuilder.DropTable(
                name: "financing_offers",
                schema: "bnpl");

            migrationBuilder.DropTable(
                name: "financing_applications",
                schema: "bnpl");
        }
    }
}
