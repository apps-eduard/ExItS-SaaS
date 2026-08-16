using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchFulfillmentAndDeliveryPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "delivery_enabled",
                schema: "platform",
                table: "organization_branches",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "latitude",
                schema: "platform",
                table: "organization_branches",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "longitude",
                schema: "platform",
                table: "organization_branches",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "pickup_enabled",
                schema: "platform",
                table: "organization_branches",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "branch_delivery_policies",
                schema: "platform",
                columns: table => new
                {
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    minimum_order_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    base_delivery_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    included_distance_km = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    additional_fee_per_km = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    maximum_delivery_distance_km = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    free_delivery_threshold = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_delivery_policies", x => x.branch_id);
                    table.CheckConstraint("ck_branch_delivery_policies_base_fee_nonneg", "base_delivery_fee >= 0");
                    table.CheckConstraint("ck_branch_delivery_policies_included_nonneg", "included_distance_km >= 0");
                    table.CheckConstraint("ck_branch_delivery_policies_max_positive", "maximum_delivery_distance_km > 0");
                    table.CheckConstraint("ck_branch_delivery_policies_min_order_nonneg", "minimum_order_amount >= 0");
                    table.CheckConstraint("ck_branch_delivery_policies_per_km_nonneg", "additional_fee_per_km >= 0");
                    table.ForeignKey(
                        name: "FK_branch_delivery_policies_organization_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "platform",
                        principalTable: "organization_branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_branch_delivery_policies_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_organization_branches_latitude",
                schema: "platform",
                table: "organization_branches",
                sql: "latitude IS NULL OR (latitude >= -90 AND latitude <= 90)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_organization_branches_longitude",
                schema: "platform",
                table: "organization_branches",
                sql: "longitude IS NULL OR (longitude >= -180 AND longitude <= 180)");

            migrationBuilder.CreateIndex(
                name: "IX_branch_delivery_policies_organization_id",
                schema: "platform",
                table: "branch_delivery_policies",
                column: "organization_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_delivery_policies",
                schema: "platform");

            migrationBuilder.DropCheckConstraint(
                name: "ck_organization_branches_latitude",
                schema: "platform",
                table: "organization_branches");

            migrationBuilder.DropCheckConstraint(
                name: "ck_organization_branches_longitude",
                schema: "platform",
                table: "organization_branches");

            migrationBuilder.DropColumn(
                name: "delivery_enabled",
                schema: "platform",
                table: "organization_branches");

            migrationBuilder.DropColumn(
                name: "latitude",
                schema: "platform",
                table: "organization_branches");

            migrationBuilder.DropColumn(
                name: "longitude",
                schema: "platform",
                table: "organization_branches");

            migrationBuilder.DropColumn(
                name: "pickup_enabled",
                schema: "platform",
                table: "organization_branches");
        }
    }
}
