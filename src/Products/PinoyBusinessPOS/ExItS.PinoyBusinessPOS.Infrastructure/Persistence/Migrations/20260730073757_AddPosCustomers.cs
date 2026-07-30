using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosCustomers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "pos");

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    mobile_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    normalized_mobile = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    address = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.id);
                    table.CheckConstraint("ck_customers_status", "status IN ('Active', 'Inactive')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_customers_org_display_name",
                schema: "pos",
                table: "customers",
                columns: new[] { "organization_id", "display_name" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_organization_id",
                schema: "pos",
                table: "customers",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ux_customers_org_active_mobile",
                schema: "pos",
                table: "customers",
                columns: new[] { "organization_id", "normalized_mobile" },
                unique: true,
                filter: "status = 'Active' AND normalized_mobile IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customers",
                schema: "pos");
        }
    }
}
