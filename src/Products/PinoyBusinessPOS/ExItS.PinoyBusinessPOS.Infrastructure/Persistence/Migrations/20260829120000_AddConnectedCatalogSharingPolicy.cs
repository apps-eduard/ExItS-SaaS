using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds connection-level catalog sharing mode + customer discount.
/// Existing rows default to SelectedOnly (0) — legacy visibility is preserved.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260829120000_AddConnectedCatalogSharingPolicy")]
public partial class AddConnectedCatalogSharingPolicy : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "catalog_sharing_mode",
            schema: "pos",
            table: "connected_supplier_relationships",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<decimal>(
            name: "customer_discount_percent",
            schema: "pos",
            table: "connected_supplier_relationships",
            type: "numeric(5,2)",
            nullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "ck_connected_supplier_relationships_catalog_sharing_mode",
            schema: "pos",
            table: "connected_supplier_relationships",
            sql: "catalog_sharing_mode BETWEEN 0 AND 1");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_connected_supplier_relationships_catalog_sharing_mode",
            schema: "pos",
            table: "connected_supplier_relationships");

        migrationBuilder.DropColumn(
            name: "customer_discount_percent",
            schema: "pos",
            table: "connected_supplier_relationships");

        migrationBuilder.DropColumn(
            name: "catalog_sharing_mode",
            schema: "pos",
            table: "connected_supplier_relationships");
    }
}
