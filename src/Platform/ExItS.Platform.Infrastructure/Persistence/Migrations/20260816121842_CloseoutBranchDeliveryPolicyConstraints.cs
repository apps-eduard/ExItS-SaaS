using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CloseoutBranchDeliveryPolicyConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_organization_branches_id_organization_id",
                schema: "platform",
                table: "organization_branches");

            migrationBuilder.AddCheckConstraint(
                name: "ck_branch_delivery_policies_free_threshold_nonneg",
                schema: "platform",
                table: "branch_delivery_policies",
                sql: "free_delivery_threshold IS NULL OR free_delivery_threshold >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_branch_delivery_policies_free_threshold_nonneg",
                schema: "platform",
                table: "branch_delivery_policies");

            migrationBuilder.CreateIndex(
                name: "ux_organization_branches_id_organization_id",
                schema: "platform",
                table: "organization_branches",
                columns: new[] { "id", "organization_id" },
                unique: true);
        }
    }
}
