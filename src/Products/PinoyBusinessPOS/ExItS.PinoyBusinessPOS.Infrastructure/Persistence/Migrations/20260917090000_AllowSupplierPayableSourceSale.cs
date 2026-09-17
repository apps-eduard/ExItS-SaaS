using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(PosDbContext))]
[Migration("20260917090000_AllowSupplierPayableSourceSale")]
public partial class AllowSupplierPayableSourceSale : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE pos.supplier_payables
                DROP CONSTRAINT IF EXISTS ck_supplier_payables_source_type;

            ALTER TABLE pos.supplier_payables
                ADD CONSTRAINT ck_supplier_payables_source_type
                CHECK (source_type IN ('GoodsReceipt', 'DirectPurchaseReceipt', 'Sale'));
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE pos.supplier_payables
                DROP CONSTRAINT IF EXISTS ck_supplier_payables_source_type;

            ALTER TABLE pos.supplier_payables
                ADD CONSTRAINT ck_supplier_payables_source_type
                CHECK (source_type IN ('GoodsReceipt', 'DirectPurchaseReceipt'));
            """);
    }
}
