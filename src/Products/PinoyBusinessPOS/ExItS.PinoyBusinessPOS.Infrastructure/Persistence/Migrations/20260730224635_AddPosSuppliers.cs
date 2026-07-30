using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosSuppliers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supplier_code_sequences",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    next_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_code_sequences", x => x.organization_id);
                    table.CheckConstraint("ck_supplier_code_sequences_next_value_positive", "next_value > 0");
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    contact_person = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    mobile_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    normalized_mobile = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    telephone_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    address_line1 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    address_line2 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    city_municipality = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    province = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    tax_or_registration_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    normalized_tax_or_registration_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suppliers", x => x.id);
                    table.CheckConstraint("ck_suppliers_status", "status IN ('Active', 'Inactive')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_org_normalized_email",
                schema: "pos",
                table: "suppliers",
                columns: new[] { "organization_id", "normalized_email" },
                filter: "normalized_email IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_org_normalized_mobile",
                schema: "pos",
                table: "suppliers",
                columns: new[] { "organization_id", "normalized_mobile" },
                filter: "normalized_mobile IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_org_normalized_name",
                schema: "pos",
                table: "suppliers",
                columns: new[] { "organization_id", "normalized_name" },
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_org_normalized_tax",
                schema: "pos",
                table: "suppliers",
                columns: new[] { "organization_id", "normalized_tax_or_registration_number" },
                filter: "normalized_tax_or_registration_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_org_status",
                schema: "pos",
                table: "suppliers",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_suppliers_org_supplier_code",
                schema: "pos",
                table: "suppliers",
                columns: new[] { "organization_id", "supplier_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supplier_code_sequences",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "suppliers",
                schema: "pos");
        }
    }
}
