import { Plus } from "lucide-react";
import type { PosInventoryAccountDto, PosInventoryLotDto } from "@/api/pos/pos-inventory-client";
import { Button } from "@/components/ui/button";
import type { ResponsiveDataLayout } from "@/components/exits/responsive-data-view";
import {
  ProductSelectionView,
  type ProductSelectionColumn,
  type ProductSelectionRow,
} from "@/components/exits/ProductSelectionView";
import { cn } from "@/lib/cn";

type Translate = (key: string) => string;

const qtyFieldClassName = cn(
  "exits-input box-border h-[var(--exits-control-height)] min-h-[var(--exits-control-height)]",
  "min-w-0 rounded-[var(--exits-field-radius)] border border-border bg-surface",
  "px-[var(--exits-control-padding-x)] text-[length:var(--exits-text-md)] font-normal text-foreground",
  "outline-none transition-[border-color,box-shadow] duration-[var(--exits-motion-fast)]",
  "placeholder:text-[var(--exits-text-subtle)] hover:border-[var(--exits-field-border-hover)]",
  "exits-input--no-spin",
);

export type InventoryTransferProductSelectionProps = {
  layout: ResponsiveDataLayout;
  products: readonly PosInventoryAccountDto[];
  qtyByProduct: Readonly<Record<string, string>>;
  lotByProduct: Readonly<Record<string, string>>;
  lotsCache: Readonly<Record<string, PosInventoryLotDto[]>>;
  online: boolean;
  formatAvailable: (qty: number, uom: string) => string;
  onQtyChange: (productId: string, value: string) => void;
  onLotChange: (productId: string, lotId: string) => void;
  onLotFocus: (productId: string) => void;
  onAddProduct: (row: PosInventoryAccountDto) => void;
  t: Translate;
};

/**
 * Branch Transfer adapter — source availability / qty / lot → ProductSelectionView.
 * Destination, draft lines, and inventory movement stay on the page.
 */
export function InventoryTransferProductSelection({
  layout,
  products,
  qtyByProduct,
  lotByProduct,
  lotsCache,
  online,
  formatAvailable,
  onQtyChange,
  onLotChange,
  onLotFocus,
  onAddProduct,
  t,
}: InventoryTransferProductSelectionProps) {
  const columns: ProductSelectionColumn[] = [
    { id: "product", header: t("transfer.product") },
    { id: "sku", header: t("purchasing.colSku") },
    { id: "category", header: t("purchasing.category") },
    { id: "available", header: t("transfer.colAvailable") },
    { id: "unit", header: t("purchasing.colUnit") },
  ];

  const rows: ProductSelectionRow[] = products.map((row) => {
    const tracksExpiration = row.tracksExpiration === true;
    const lots = lotsCache[row.productId] ?? [];
    const available = Math.max(0, row.onHandQuantity);
    const outOfStock = available <= 0;
    const selectedLotId = lotByProduct[row.productId] ?? "";
    const selectedLot = lots.find((l) => l.lotId === selectedLotId);
    const lotOut =
      tracksExpiration && selectedLot != null && selectedLot.quantityOnHand <= 0;
    const addDisabled = !online || outOfStock || lotOut;
    const sku = row.sku?.trim() || "—";
    const category =
      row.categoryName?.trim() ||
      (row.categoryId?.trim() ? row.categoryId : "—");
    const unit = row.unitOfMeasure || "—";
    const availableLabel = outOfStock
      ? t("transfer.outOfStock")
      : formatAvailable(available, row.unitOfMeasure);
    const availableWithExpiry = tracksExpiration
      ? `${availableLabel} · ${t("transfer.tracksExpiry")}`
      : availableLabel;

    const primaryAction = outOfStock ? (
      <span
        className="shrink-0 text-[length:var(--exits-text-xs)] text-muted"
        data-testid={`transfer-picker-unavailable-${row.productId}`}
      >
        {t("transfer.unavailable")}
      </span>
    ) : (
      <div className="product-selection__inline-action flex flex-wrap items-center justify-end gap-1.5">
        <label className="sr-only" htmlFor={`transfer-qty-${row.productId}`}>
          {t("transfer.quantity")}
        </label>
        <input
          id={`transfer-qty-${row.productId}`}
          type="number"
          inputMode="decimal"
          step="any"
          min={0}
          className={cn(qtyFieldClassName, "w-[5.5rem] shrink-0")}
          placeholder={t("transfer.quantity")}
          value={qtyByProduct[row.productId] ?? ""}
          onChange={(e) => onQtyChange(row.productId, e.target.value)}
          data-testid={`transfer-picker-qty-${row.productId}`}
        />
        <Button
          type="button"
          size="icon"
          className="shrink-0 rounded-full"
          disabled={addDisabled}
          aria-label={t("transfer.addProduct")}
          onClick={() => onAddProduct(row)}
          data-testid={`transfer-add-${row.productId}`}
        >
          <Plus className="size-4" aria-hidden />
        </Button>
      </div>
    );

    const details =
      tracksExpiration && !outOfStock ? (
        <select
          className="exits-select w-full max-w-md"
          value={lotByProduct[row.productId] ?? ""}
          onFocus={() => onLotFocus(row.productId)}
          onChange={(e) => onLotChange(row.productId, e.target.value)}
          data-testid={`transfer-lot-${row.productId}`}
        >
          <option value="">{t("transfer.selectLot")}</option>
          {lots.map((lot) => (
            <option key={lot.lotId} value={lot.lotId} disabled={lot.quantityOnHand <= 0}>
              {(lot.lotNumber ?? t("transfer.lot")) +
                ` · ${lot.expirationDate ?? "—"} · ${lot.quantityOnHand}`}
              {lot.quantityOnHand <= 0 ? ` (${t("transfer.outOfStock")})` : ""}
            </option>
          ))}
        </select>
      ) : undefined;

    return {
      id: row.productId,
      testId: `transfer-picker-row-${row.productId}`,
      title: row.name,
      subtitle: sku !== "—" ? sku : undefined,
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
        <span key="name" className="font-medium leading-snug">
          {row.name}
        </span>,
        sku,
        category,
        <span
          key="avail"
          className={cn(outOfStock && "text-danger")}
          data-testid={`transfer-picker-available-${row.productId}`}
        >
          {availableWithExpiry}
        </span>,
        unit,
      ],
      fields: [
        { label: t("purchasing.category"), value: category },
        { label: t("transfer.colAvailable"), value: availableWithExpiry },
        { label: t("purchasing.colUnit"), value: unit },
      ],
      primaryAction,
      details,
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
