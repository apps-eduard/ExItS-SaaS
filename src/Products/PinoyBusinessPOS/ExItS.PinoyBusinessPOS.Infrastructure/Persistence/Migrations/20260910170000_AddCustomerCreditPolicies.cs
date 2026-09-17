using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerCreditPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_credit_policies",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    credit_limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    default_term_days = table.Column<int>(type: "integer", nullable: false),
                    configured_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    configured_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_credit_policies", x => x.id);
                    table.CheckConstraint("ck_customer_credit_policies_credit_limit_non_negative", "credit_limit >= 0");
                    table.CheckConstraint("ck_customer_credit_policies_status", "status BETWEEN 1 AND 3");
                    table.CheckConstraint("ck_customer_credit_policies_term_days", "default_term_days BETWEEN 1 AND 365");
                    table.ForeignKey(
                        name: "fk_customer_credit_policies_customers",
                        column: x => x.customer_id,
                        principalSchema: "pos",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_credit_policy_changes",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_credit_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    previous_status = table.Column<int>(type: "integer", nullable: true),
                    new_status = table.Column<int>(type: "integer", nullable: false),
                    previous_credit_limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    new_credit_limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    previous_term_days = table.Column<int>(type: "integer", nullable: true),
                    new_term_days = table.Column<int>(type: "integer", nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    changed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_credit_policy_changes", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_credit_policy_changes_customers",
                        column: x => x.customer_id,
                        principalSchema: "pos",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_customer_credit_policy_changes_policies",
                        column: x => x.customer_credit_policy_id,
                        principalSchema: "pos",
                        principalTable: "customer_credit_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_credit_policies_customer_id",
                schema: "pos",
                table: "customer_credit_policies",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ux_customer_credit_policies_org_customer",
                schema: "pos",
                table: "customer_credit_policies",
                columns: new[] { "organization_id", "customer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_credit_policy_changes_customer_credit_policy_id",
                schema: "pos",
                table: "customer_credit_policy_changes",
                column: "customer_credit_policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_credit_policy_changes_customer_id",
                schema: "pos",
                table: "customer_credit_policy_changes",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_credit_policy_changes_org_customer_changed",
                schema: "pos",
                table: "customer_credit_policy_changes",
                columns: new[] { "organization_id", "customer_id", "changed_at_utc" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_credit_policy_changes",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "customer_credit_policies",
                schema: "pos");
        }
    }
}
