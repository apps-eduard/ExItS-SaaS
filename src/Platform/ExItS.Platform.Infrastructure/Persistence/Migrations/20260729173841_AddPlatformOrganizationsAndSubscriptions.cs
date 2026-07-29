using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformOrganizationsAndSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organizations",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trial_definition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    trial_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    trial_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    paid_period_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    paid_period_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    grace_period_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    suspended_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    past_due_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expired_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    aggregate_version = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.id);
                    table.CheckConstraint("ck_subscriptions_paid_range", "paid_period_start_utc IS NULL OR paid_period_end_utc IS NULL OR paid_period_end_utc > paid_period_start_utc");
                    table.CheckConstraint("ck_subscriptions_trial_range", "trial_start_utc IS NULL OR trial_end_utc IS NULL OR trial_end_utc > trial_start_utc");
                    table.ForeignKey(
                        name: "FK_subscriptions_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subscriptions_plan_versions_plan_version_id",
                        column: x => x.plan_version_id,
                        principalSchema: "platform",
                        principalTable: "plan_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subscriptions_plans_plan_id",
                        column: x => x.plan_id,
                        principalSchema: "platform",
                        principalTable: "plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subscriptions_trial_definitions_trial_definition_id",
                        column: x => x.trial_definition_id,
                        principalSchema: "platform",
                        principalTable: "trial_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_organizations_slug",
                schema: "platform",
                table: "organizations",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_plan_id",
                schema: "platform",
                table: "subscriptions",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_plan_version_id",
                schema: "platform",
                table: "subscriptions",
                column: "plan_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_trial_definition_id",
                schema: "platform",
                table: "subscriptions",
                column: "trial_definition_id");

            migrationBuilder.CreateIndex(
                name: "ux_subscriptions_one_active_like",
                schema: "platform",
                table: "subscriptions",
                columns: new[] { "organization_id", "product_code" },
                unique: true,
                filter: "status IN ('Trialing', 'Active', 'GracePeriod', 'PastDue', 'Suspended')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "subscriptions",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "organizations",
                schema: "platform");
        }
    }
}
