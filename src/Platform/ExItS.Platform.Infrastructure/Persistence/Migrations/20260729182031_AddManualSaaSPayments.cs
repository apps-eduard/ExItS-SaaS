using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManualSaaSPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "saas_payments",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency_code = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    external_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    normalized_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    paid_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    confirmed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    confirmed_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    rejected_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejected_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    voided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    voided_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    void_reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    aggregate_version = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_payments", x => x.id);
                    table.CheckConstraint("ck_saas_payments_positive_amount", "amount > 0");
                    table.ForeignKey(
                        name: "FK_saas_payments_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_saas_payments_subscriptions_subscription_id",
                        column: x => x.subscription_id,
                        principalSchema: "platform",
                        principalTable: "subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_saas_payments_organization_id",
                schema: "platform",
                table: "saas_payments",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_saas_payments_subscription_id",
                schema: "platform",
                table: "saas_payments",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "ux_saas_payments_reference",
                schema: "platform",
                table: "saas_payments",
                columns: new[] { "method", "normalized_reference", "organization_id" },
                unique: true,
                filter: "status NOT IN ('Rejected', 'Voided')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "saas_payments",
                schema: "platform");
        }
    }
}
