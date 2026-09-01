using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncMb2PartyAccessAndSetupProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_supplier_branch_access",
                schema: "pos",
                table: "supplier_branch_access");

            migrationBuilder.DropPrimaryKey(
                name: "pk_customer_branch_access",
                schema: "pos",
                table: "customer_branch_access");

            migrationBuilder.AddPrimaryKey(
                name: "pk_supplier_branch_access",
                schema: "pos",
                table: "supplier_branch_access",
                columns: new[] { "organization_id", "branch_id", "supplier_id", "grant_source" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_customer_branch_access",
                schema: "pos",
                table: "customer_branch_access",
                columns: new[] { "organization_id", "branch_id", "customer_id", "grant_source" });

            migrationBuilder.CreateTable(
                name: "branch_setup_progress",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_visited_step = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_visited_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_branch_setup_progress", x => new { x.organization_id, x.branch_id });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_setup_progress",
                schema: "pos");

            migrationBuilder.DropPrimaryKey(
                name: "pk_supplier_branch_access",
                schema: "pos",
                table: "supplier_branch_access");

            migrationBuilder.DropPrimaryKey(
                name: "pk_customer_branch_access",
                schema: "pos",
                table: "customer_branch_access");

            migrationBuilder.AddPrimaryKey(
                name: "pk_supplier_branch_access",
                schema: "pos",
                table: "supplier_branch_access",
                columns: new[] { "organization_id", "branch_id", "supplier_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_customer_branch_access",
                schema: "pos",
                table: "customer_branch_access",
                columns: new[] { "organization_id", "branch_id", "customer_id" });
        }
    }
}
