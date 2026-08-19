using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationMembershipBranchAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organization_membership_branch_assignments",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_membership_branch_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_organization_membership_branch_assignments_organization_bra~",
                        column: x => x.branch_id,
                        principalSchema: "platform",
                        principalTable: "organization_branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_organization_membership_branch_assignments_organization_mem~",
                        column: x => x.membership_id,
                        principalSchema: "platform",
                        principalTable: "organization_memberships",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_organization_membership_branch_assignments_organizations_or~",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_org_membership_branch_assignments_organization_id",
                schema: "platform",
                table: "organization_membership_branch_assignments",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_organization_membership_branch_assignments_branch_id",
                schema: "platform",
                table: "organization_membership_branch_assignments",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ux_org_membership_branch_assignments_membership_branch",
                schema: "platform",
                table: "organization_membership_branch_assignments",
                columns: new[] { "membership_id", "branch_id" },
                unique: true);

            // P28-WP15C compatibility: existing staff receive assignments to current Active branches only.
            migrationBuilder.Sql(
                """
                INSERT INTO platform.organization_membership_branch_assignments (
                    id, organization_id, membership_id, branch_id, created_at_utc, actor_reference)
                SELECT gen_random_uuid(), m.organization_id, m.id, b.id, NOW(), 'migration:p28-wp15c-backfill'
                FROM platform.organization_memberships m
                INNER JOIN platform.organization_branches b
                    ON b.organization_id = m.organization_id
                   AND b.status = 'Active'
                WHERE m.status = 'Active'
                  AND m.role = 'OrganizationMember'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM platform.organization_membership_branch_assignments a
                      WHERE a.membership_id = m.id AND a.branch_id = b.id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organization_membership_branch_assignments",
                schema: "platform");
        }
    }
}
