using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Allows connected PO fulfillment stock movements (supplier tracked deduction).
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260904120000_AddConnectedPurchaseFulfillmentStockMovement")]
public partial class AddConnectedPurchaseFulfillmentStockMovement : Migration
{
    private const string MovementTypeSql =
        "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration', 'PurchaseReceipt', 'StockCountVarianceIncrease', 'StockCountVarianceDecrease', 'SaleReturnRestock', 'TransferOut', 'TransferIn', 'TransferCancelRestore', 'DirectPurchaseReceipt', 'ExpirationInitialization', 'StockUse', 'StockUseVoidRestoration', 'ProductionMaterialConsumption', 'ProductionMaterialRestoration', 'ProductionOutput', 'ProductionOutputReversal', 'WasteLoss', 'WasteLossVoidRestoration', 'PurchaseReceiptReversal', 'DirectPurchaseReceiptReversal', 'ConnectedPurchaseFulfillment')";

    private const string SourceTypeSql =
        "source_type IN ('None', 'Sale', 'Manual', 'Opening', 'PurchaseReceipt', 'StockCount', 'SaleReturn', 'InventoryTransfer', 'CustomerOrder', 'DirectPurchase', 'StockUse', 'Production', 'WasteLoss', 'ConnectedPurchaseOrder')";

    private const string PreviousMovementTypeSql =
        "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration', 'PurchaseReceipt', 'StockCountVarianceIncrease', 'StockCountVarianceDecrease', 'SaleReturnRestock', 'TransferOut', 'TransferIn', 'TransferCancelRestore', 'DirectPurchaseReceipt', 'ExpirationInitialization', 'StockUse', 'StockUseVoidRestoration', 'ProductionMaterialConsumption', 'ProductionMaterialRestoration', 'ProductionOutput', 'ProductionOutputReversal', 'WasteLoss', 'WasteLossVoidRestoration', 'PurchaseReceiptReversal', 'DirectPurchaseReceiptReversal')";

    private const string PreviousSourceTypeSql =
        "source_type IN ('None', 'Sale', 'Manual', 'Opening', 'PurchaseReceipt', 'StockCount', 'SaleReturn', 'InventoryTransfer', 'CustomerOrder', 'DirectPurchase', 'StockUse', 'Production', 'WasteLoss')";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_stock_movements_movement_type",
            schema: "pos",
            table: "stock_movements");

        migrationBuilder.DropCheckConstraint(
            name: "ck_stock_movements_source_type",
            schema: "pos",
            table: "stock_movements");

        migrationBuilder.CreateIndex(
            name: "ux_stock_movements_connected_po_fulfill_source",
            schema: "pos",
            table: "stock_movements",
            columns: new[] { "organization_id", "source_id", "product_id", "movement_type" },
            unique: true,
            filter: "source_type = 'ConnectedPurchaseOrder' AND source_id IS NOT NULL");

        migrationBuilder.AddCheckConstraint(
            name: "ck_stock_movements_movement_type",
            schema: "pos",
            table: "stock_movements",
            sql: MovementTypeSql);

        migrationBuilder.AddCheckConstraint(
            name: "ck_stock_movements_source_type",
            schema: "pos",
            table: "stock_movements",
            sql: SourceTypeSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_stock_movements_movement_type",
            schema: "pos",
            table: "stock_movements");

        migrationBuilder.DropCheckConstraint(
            name: "ck_stock_movements_source_type",
            schema: "pos",
            table: "stock_movements");

        migrationBuilder.DropIndex(
            name: "ux_stock_movements_connected_po_fulfill_source",
            schema: "pos",
            table: "stock_movements");

        migrationBuilder.AddCheckConstraint(
            name: "ck_stock_movements_movement_type",
            schema: "pos",
            table: "stock_movements",
            sql: PreviousMovementTypeSql);

        migrationBuilder.AddCheckConstraint(
            name: "ck_stock_movements_source_type",
            schema: "pos",
            table: "stock_movements",
            sql: PreviousSourceTypeSql);
    }
}
