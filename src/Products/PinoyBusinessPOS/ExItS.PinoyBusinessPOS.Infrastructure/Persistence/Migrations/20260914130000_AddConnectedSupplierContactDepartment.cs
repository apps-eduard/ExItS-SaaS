using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds contact_department for environments that already applied
/// 20260914120000 before Department was included in that migration.
/// Idempotent: safe when 14120000 (current source) already created the column.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260914130000_AddConnectedSupplierContactDepartment")]
public partial class AddConnectedSupplierContactDepartment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE pos.connected_supplier_relationships
            ADD COLUMN IF NOT EXISTS contact_department character varying(128) NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE pos.connected_supplier_relationships
            DROP COLUMN IF EXISTS contact_department;
            """);
    }
}
