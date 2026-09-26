import { Plus } from "lucide-react";
import type { PosInventoryAccountDto, PosInventoryLotDto } from "@/api/pos/pos-inventory-client";
import { Button } from "@/components/ui/button";
import type { ResponsiveDataLayout } from "@/components/exits/responsive-data-view";
import {
  ProductSelectionView,
  type ProductSelectionColumn,
  type ProductSelectionRow,
} from "@/components/exits/ProductSelectionView";
import { selectTransferEligibleLots } from "@/features/inventory/inventory-transfer-fefo-allocate";
import { resolveAvailableQuantity } from "@/features/inventory/inventory-reservation-display";
import { cn } from "@/lib/cn";

type Translate = (key: string) => string;

export type InventoryTransferProductSelectionProps = {
  layout: ResponsiveDataLayout;
  products: readonly PosInventoryAccountDto[];
  lotsCache: Readonly<Record<string, PosInventoryLotDto[]>>;
  online: boolean;
  formatAvailable: (qty: number, uom: string) => string;
  onAddProduct: (row: PosInventoryAccountDto) => void;
  t: Translate;
};

/**
 * Branch Transfer find-products adapter — product-only add.
 * Lot allocation happens on the draft after add (FEFO / Change lots).
 */
export function InventoryTransferProductSelection({
  layout,
  products,
  lotsCache,
  online,
  formatAvailable,
  onAddProduct,
  t,
}: InventoryTransferProductSelectionProps) {
  const columns: ProductSelectionColumn[] = [
    { id: "product", header: t("transfer.product") },
    { id: "category", header: t("purchasing.category") },
    { id: "available", header: t("transfer.colAvailable") },
  ];

  const rows: ProductSelectionRow[] = products.map((row) => {
    const tracksExpiration = row.tracksExpiration === true;
    const lots = lotsCache[row.productId];
    const eligibleLotCount =
      lots != null ? selectTransferEligibleLots(lots).length : null;
    const available = Math.max(0, resolveAvailableQuantity(row));
    const outOfStock = available <= 0;
    const addDisabled = !online || outOfStock;
    const sku = row.sku?.trim() || "";
    const category =
      row.categoryName?.trim() ||
      (row.categoryId?.trim() ? row.categoryId : "—");
    const availableLabel = outOfStock
      ? t("transfer.outOfStock")
      : formatAvailable(available, row.unitOfMeasure);

    let availableWithExpiry = availableLabel;
    if (tracksExpiration && !outOfStock) {
      const parts = [availableLabel];
      if (eligibleLotCount != null) {
        parts.push(
          t("transfer.lotCount").replace("{count}", String(eligibleLotCount)),
        );
      }
      parts.push(t("transfer.tracksExpiry"));
      availableWithExpiry = parts.join(" · ");
    }

    const productCell = (
      <>
        <div className="font-medium leading-snug">{row.name}</div>
        {sku ? (
          <div className="text-[length:var(--exits-text-xs)] text-muted">{sku}</div>
        ) : null}
      </>
    );

    const primaryAction = outOfStock ? (
      <span
        className="shrink-0 text-[length:var(--exits-text-xs)] text-muted"
        data-testid={`transfer-picker-unavailable-${row.productId}`}
      >
        {t("transfer.unavailable")}
      </span>
    ) : (
      <Button
        type="button"
        size="icon"
        className="shrink-0"
        disabled={addDisabled}
        aria-label={t("transfer.addProduct")}
        onClick={() => onAddProduct(row)}
        data-testid={`transfer-add-${row.productId}`}
      >
        <Plus className="size-4" aria-hidden />
      </Button>
    );

    return {
      id: row.productId,
      testId: `transfer-picker-row-${row.productId}`,
      title: row.name,
      subtitle: sku || undefined,
      status: (
        <span
          className={cn(
            "text-[length:var(--exits-text-xs)]",
            outOfStock ? "text-danger" : "text-muted",
          )}
          data-testid={`transfer-picker-available-${row.productId}`}
        >
          {availableWithExpiry}
        </span>
      ),
      cells: [
        productCell,
        category,
        <span
          key="avail"
          className={cn(outOfStock && "text-danger")}
          data-testid={`transfer-picker-available-${row.productId}`}
        >
          {availableWithExpiry}
        </span>,
      ],
      fields: [
        { label: t("purchasing.category"), value: category },
        { label: t("transfer.colAvailable"), value: availableWithExpiry },
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
      testId="transfer-product-picker"
      className="transfer-product-selection"
      tableClassName="transfer-product-selection__table"
      listClassName="transfer-product-selection__list"
    />
  );
}
