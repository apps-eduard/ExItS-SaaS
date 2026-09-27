import { Plus } from "lucide-react";
import type { ReplenishmentCatalogItemDto } from "@/api/pos/pos-stock-requests-client";
import { Button } from "@/components/ui/button";
import type { ResponsiveDataLayout } from "@/components/exits/responsive-data-view";
import {
  ProductSelectionView,
  type ProductSelectionColumn,
  type ProductSelectionRow,
} from "@/components/exits/ProductSelectionView";
import { isByWeightSellingMode } from "@/cart/sell-cart-helpers";
import { requestStockDisplayUom } from "@/features/warehouse/retail-warehouse-request-math";
import { cn } from "@/lib/cn";

type Translate = (key: string) => string;

export type RequestStockProductSelectionProps = {
  layout: ResponsiveDataLayout;
  products: readonly ReplenishmentCatalogItemDto[];
  formatAvailable: (qty: number, uom: string) => string;
  onAddProduct: (row: ReplenishmentCatalogItemDto) => void;
  t: Translate;
};

/**
 * Find-products adapter for Request stock — mirrors transfer picker chrome,
 * columns show warehouse available (source of supply).
 */
export function RequestStockProductSelection({
  layout,
  products,
  formatAvailable,
  onAddProduct,
  t,
}: RequestStockProductSelectionProps) {
  const columns: ProductSelectionColumn[] = [
    { id: "product", header: t("transfer.product") },
    { id: "category", header: t("purchasing.category") },
    { id: "available", header: t("retailWarehouse.request.colWarehouseAvailable") },
  ];

  const rows: ProductSelectionRow[] = products.map((row) => {
    const uom = requestStockDisplayUom(row.sellingMode || "PerItem", row.unitOfMeasure);
    const available = Math.max(0, row.warehouseAvailableQuantity);
    const outOfStock = available <= 0;
    const sku = row.sku?.trim() || "";
    const category =
      row.categoryName?.trim() ||
      (row.categoryId?.trim() ? row.categoryId : "—");
    const availableLabel = outOfStock
      ? t("retailWarehouse.request.warehouseOutOfStock")
      : formatAvailable(available, uom);
    const byWeight = isByWeightSellingMode(row.sellingMode);

    const productCell = (
      <>
        <div className="font-medium leading-snug">{row.name}</div>
        {sku ? (
          <div className="text-[length:var(--exits-text-xs)] text-muted">{sku}</div>
        ) : null}
        {byWeight ? (
          <div className="text-[length:var(--exits-text-xs)] text-muted">
            {t("sell.tileByWeight")}
          </div>
        ) : null}
      </>
    );

    const primaryAction = outOfStock ? (
      <span
        className="shrink-0 text-[length:var(--exits-text-xs)] text-muted"
        data-testid={`request-picker-unavailable-${row.productId}`}
      >
        {t("transfer.unavailable")}
      </span>
    ) : (
      <Button
        type="button"
        size="icon"
        className="shrink-0"
        aria-label={t("retailWarehouse.request.add")}
        onClick={() => onAddProduct(row)}
        data-testid={`request-add-${row.productId}`}
      >
        <Plus className="size-4" aria-hidden />
      </Button>
    );

    return {
      id: row.productId,
      testId: `request-picker-row-${row.productId}`,
      title: row.name,
      subtitle: sku || undefined,
      status: (
        <span
          className={cn(
            "text-[length:var(--exits-text-xs)]",
            outOfStock ? "text-danger" : "text-muted",
          )}
          data-testid={`request-picker-available-${row.productId}`}
        >
          {availableLabel}
        </span>
      ),
      cells: [
        productCell,
        category,
        <span
          key="avail"
          className={cn(outOfStock && "text-danger")}
          data-testid={`request-picker-available-${row.productId}`}
        >
          {availableLabel}
        </span>,
      ],
      fields: [
        { label: t("purchasing.category"), value: category },
        { label: t("retailWarehouse.request.colWarehouseAvailable"), value: availableLabel },
        {
          label: t("retailWarehouse.request.cardBranch"),
          value: formatAvailable(Math.max(0, row.branchOnHandQuantity), uom),
        },
      ],
      primaryAction,
    };
  });

  return (
    <ProductSelectionView
      layout={layout}
      columns={columns}
      rows={rows}
      actionHeader={t("purchasing.action")}
      actionColClassName="transfer-product-selection__action-col"
      testId="request-product-picker"
      className="transfer-product-selection"
      tableClassName="transfer-product-selection__table"
      listClassName="transfer-product-selection__list"
    />
  );
}
