using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddCustomerLinkPendingOrgTargetUnique : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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
    }
}
