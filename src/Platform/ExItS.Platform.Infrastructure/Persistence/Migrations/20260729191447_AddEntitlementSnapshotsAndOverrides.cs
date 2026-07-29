using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEntitlementSnapshotsAndOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "entitlement_snapshots",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    plan_version_number = table.Column<int>(type: "integer", nullable: false),
                    snapshot_version = table.Column<int>(type: "integer", nullable: false),
                    schema_version = table.Column<int>(type: "integer", nullable: false),
                    subscription_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    in_grace_period = table.Column<bool>(type: "boolean", nullable: false),
                    generated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    effective_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    refresh_by_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    source_aggregate_version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entitlement_snapshots", x => x.id);
                    table.CheckConstraint("ck_entitlement_snapshots_expiry_range", "expires_at_utc IS NULL OR expires_at_utc >= effective_at_utc");
                    table.CheckConstraint("ck_entitlement_snapshots_refresh_range", "refresh_by_utc >= generated_at_utc");
                    table.CheckConstraint("ck_entitlement_snapshots_schema_positive", "schema_version > 0");
                    table.CheckConstraint("ck_entitlement_snapshots_version_positive", "snapshot_version > 0");
                    table.ForeignKey(
                        name: "FK_entitlement_snapshots_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_entitlement_snapshots_subscriptions_subscription_id",
                        column: x => x.subscription_id,
                        principalSchema: "platform",
                        principalTable: "subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "feature_overrides",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    feature_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    numeric_limit = table.Column<int>(type: "integer", nullable: true),
                    reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    effective_from_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feature_overrides", x => x.id);
                    table.CheckConstraint("ck_feature_overrides_expiry_range", "expires_at_utc IS NULL OR expires_at_utc > effective_from_utc");
                    table.CheckConstraint("ck_feature_overrides_numeric_limit", "numeric_limit IS NULL OR numeric_limit >= 0");
                    table.ForeignKey(
                        name: "FK_feature_overrides_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "entitlement_snapshot_grants",
                schema: "platform",
                columns: table => new
                {
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    feature_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    numeric_limit = table.Column<int>(type: "integer", nullable: true),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    effective_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entitlement_snapshot_grants", x => new { x.snapshot_id, x.feature_code });
                    table.ForeignKey(
                        name: "FK_entitlement_snapshot_grants_entitlement_snapshots_snapshot_~",
                        column: x => x.snapshot_id,
                        principalSchema: "platform",
                        principalTable: "entitlement_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_entitlement_snapshots_subscription_id",
                schema: "platform",
                table: "entitlement_snapshots",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "ux_entitlement_snapshots_org_product_version",
                schema: "platform",
                table: "entitlement_snapshots",
                columns: new[] { "organization_id", "product_code", "snapshot_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_feature_overrides_organization_id_product_code_feature_code",
                schema: "platform",
                table: "feature_overrides",
                columns: new[] { "organization_id", "product_code", "feature_code" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "entitlement_snapshot_grants",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "feature_overrides",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "entitlement_snapshots",
                schema: "platform");
        }
    }
}
