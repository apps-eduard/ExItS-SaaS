using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipBranchAccessScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Persist BranchAccessScope on organization_memberships.
            // Backfill ALL existing rows as Explicit (defaultValue). Do NOT infer AllActive
            // from assignment IDs equaling current active branch IDs — existing data used
            // snapshot semantics; switching to dynamic would broaden authorization.
            migrationBuilder.AddColumn<string>(
                name: "branch_access_scope",
                schema: "platform",
                table: "organization_memberships",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Explicit");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "branch_access_scope",
                schema: "platform",
                table: "organization_memberships");
        }
    }
}
