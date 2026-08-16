using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StrengthenBranchDeliveryPolicyTenantIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_branch_delivery_policies_organization_branches_branch_id",
                schema: "platform",
                table: "branch_delivery_policies");

            migrationBuilder.DropForeignKey(
                name: "FK_branch_delivery_policies_organizations_organization_id",
                schema: "platform",
                table: "branch_delivery_policies");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_organization_branches_id_organization_id",
                schema: "platform",
                table: "organization_branches",
                columns: new[] { "id", "organization_id" });

            migrationBuilder.CreateIndex(
                name: "ux_organization_branches_id_organization_id",
                schema: "platform",
                table: "organization_branches",
                columns: new[] { "id", "organization_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_organization_branches_lat_long_pair",
                schema: "platform",
                table: "organization_branches",
                sql: "(latitude IS NULL AND longitude IS NULL) OR (latitude IS NOT NULL AND longitude IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_branch_delivery_policies_branch_id_organization_id",
                schema: "platform",
                table: "branch_delivery_policies",
                columns: new[] { "branch_id", "organization_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_branch_delivery_policies_organization_branches_branch_id_or~",
                schema: "platform",
                table: "branch_delivery_policies",
                columns: new[] { "branch_id", "organization_id" },
                principalSchema: "platform",
                principalTable: "organization_branches",
                principalColumns: new[] { "id", "organization_id" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_branch_delivery_policies_organization_branches_branch_id_or~",
                schema: "platform",
                table: "branch_delivery_policies");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_organization_branches_id_organization_id",
                schema: "platform",
                table: "organization_branches");

            migrationBuilder.DropIndex(
                name: "ux_organization_branches_id_organization_id",
                schema: "platform",
                table: "organization_branches");

            migrationBuilder.DropCheckConstraint(
                name: "ck_organization_branches_lat_long_pair",
                schema: "platform",
                table: "organization_branches");

            migrationBuilder.DropIndex(
                name: "IX_branch_delivery_policies_branch_id_organization_id",
                schema: "platform",
                table: "branch_delivery_policies");

            migrationBuilder.AddForeignKey(
                name: "FK_branch_delivery_policies_organization_branches_branch_id",
                schema: "platform",
                table: "branch_delivery_policies",
                column: "branch_id",
                principalSchema: "platform",
                principalTable: "organization_branches",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_branch_delivery_policies_organizations_organization_id",
                schema: "platform",
                table: "branch_delivery_policies",
                column: "organization_id",
                principalSchema: "platform",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
