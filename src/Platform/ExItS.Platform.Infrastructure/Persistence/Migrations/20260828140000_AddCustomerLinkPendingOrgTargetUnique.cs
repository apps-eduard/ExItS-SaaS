using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(PlatformDbContext))]
[Migration("20260828140000_AddCustomerLinkPendingOrgTargetUnique")]
public partial class AddCustomerLinkPendingOrgTargetUnique : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Replaced by the filtered unique (organization_id, target_user_identity_id) index below.
        migrationBuilder.DropIndex(
            name: "IX_customer_link_requests_organization_id",
            schema: "platform",
            table: "customer_link_requests");

        migrationBuilder.CreateIndex(
            name: "ux_customer_link_requests_pending_org_target",
            schema: "platform",
            table: "customer_link_requests",
            columns: new[] { "organization_id", "target_user_identity_id" },
            unique: true,
            filter: "status = 'Pending' AND target_user_identity_id IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_customer_link_requests_pending_org_target",
            schema: "platform",
            table: "customer_link_requests");

        migrationBuilder.CreateIndex(
            name: "IX_customer_link_requests_organization_id",
            schema: "platform",
            table: "customer_link_requests",
            column: "organization_id");
    }
}
