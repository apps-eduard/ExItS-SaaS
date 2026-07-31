using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosOperationalRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pos_role_assignments",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    assigned_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assigned_by = table.Column<Guid>(type: "uuid", nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by = table.Column<Guid>(type: "uuid", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pos_role_assignments", x => x.id);
                    table.CheckConstraint("ck_pos_role_assignments_role", "role IN ('Owner', 'Admin', 'StoreManager', 'Cashier', 'InventoryStaff', 'ReportingUser')");
                    table.CheckConstraint("ck_pos_role_assignments_status", "status IN ('Active', 'Revoked')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_pos_role_assignments_org_actor_status",
                schema: "pos",
                table: "pos_role_assignments",
                columns: new[] { "organization_id", "actor_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_pos_role_assignments_org_revoked",
                schema: "pos",
                table: "pos_role_assignments",
                columns: new[] { "organization_id", "revoked_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_pos_role_assignments_org_role_status",
                schema: "pos",
                table: "pos_role_assignments",
                columns: new[] { "organization_id", "role", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_pos_role_assignments_org_status_assigned",
                schema: "pos",
                table: "pos_role_assignments",
                columns: new[] { "organization_id", "status", "assigned_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_pos_role_assignments_org_actor_active",
                schema: "pos",
                table: "pos_role_assignments",
                columns: new[] { "organization_id", "actor_id" },
                unique: true,
                filter: "status = 'Active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pos_role_assignments",
                schema: "pos");
        }
    }
}
