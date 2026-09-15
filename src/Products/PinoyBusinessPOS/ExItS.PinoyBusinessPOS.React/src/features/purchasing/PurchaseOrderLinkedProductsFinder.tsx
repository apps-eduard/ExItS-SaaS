import { Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import type { ResponsiveDataLayout } from "@/components/exits/responsive-data-view";
import {
  ProductSelectionView,
  type ProductSelectionColumn,
  type ProductSelectionRow,
} from "@/components/exits/ProductSelectionView";
import {
  formatSupplierAvailabilityLabel,
  formatUnitOfMeasureLabel,
  formatUnitPriceLabel,
  type ConnectedPoReadyProduct,
} from "@/features/purchasing/purchase-order-create-connected";

type Translate = (key: string) => string;

type PurchaseOrderLinkedProductsFinderProps = {
  layout: ResponsiveDataLayout;
  /** Products not yet on the PO (selected lines are excluded by the page). */
  products: readonly ConnectedPoReadyProduct[];
  allowManage: boolean;
  online: boolean;
  saving: boolean;
  onAddProduct: (product: ConnectedPoReadyProduct) => void;
  t: Translate;
};

/**
 * Create PO (Linked tab) adapter — supplier stock/price/add → ProductSelectionView.
 * Selected products are omitted by the parent so Add always means “add to order”.
 * Stock is informational; Add is never blocked by availability.
 */
export function PurchaseOrderLinkedProductsFinder({
  layout,
  products,
  allowManage,
  online,
  saving,
  onAddProduct,
  t,
}: PurchaseOrderLinkedProductsFinderProps) {
  const columns: ProductSelectionColumn[] = [
    { id: "product", header: t("purchasing.colProduct") },
    { id: "category", header: t("purchasing.category") },
    { id: "sku", header: t("purchasing.colSku") },
    { id: "unit", header: t("purchasing.colUnit") },
    { id: "stock", header: t("purchasing.colStock") },
    { id: "price", header: t("purchasing.supplierPrice"), cellAlign: "numeric" },
  ];

  const rows: ProductSelectionRow[] = products.map((product) => {
    const stockLabel = formatSupplierAvailabilityLabel(product, t);

    const unitLabel =
      product.packageLabel || product.unitOfMeasure
        ? formatUnitOfMeasureLabel(
            product.packageLabel || product.unitOfMeasure || "",
          )
        : "—";

    const priceLabel = formatUnitPriceLabel(
      product.unitPurchaseCost,
      product.unitOfMeasure,
    );

    const skuLabel = product.supplierSku ?? t("connected.noSku");
    const categoryLabel = product.categoryName?.trim() || "—";
    const categoryCell = (
      <span data-testid={`po-category-${product.buyerProductId}`}>{categoryLabel}</span>
    );

    return {
      id: product.buyerProductId,
      testId: `po-connected-product-${product.buyerProductId}`,
      title: product.productName,
      subtitle: skuLabel,
      status: (
        <span data-testid={`po-stock-${product.buyerProductId}`}>{stockLabel}</span>
      ),
      cells: [
        <span key="name" className="font-medium leading-snug">
          {product.productName}
        </span>,
        categoryCell,
        skuLabel,
        unitLabel,
        <span key="stock" data-testid={`po-stock-${product.buyerProductId}`}>
          {stockLabel}
        </span>,
        <span key="price" className="tabular-nums">
          {priceLabel}
        </span>,
      ],
      fields: [
        { label: t("purchasing.category"), value: categoryCell },
        { label: t("purchasing.colUnit"), value: unitLabel },
        { label: t("purchasing.colStock"), value: stockLabel },
        { label: t("purchasing.supplierPrice"), value: priceLabel, emphasize: true },
      ],
      primaryAction: (
        <Button
          type="button"
          variant="default"
          size="icon"
          shape="round"
          className="po-linked-add-btn"
          data-testid={`po-add-${product.buyerProductId}`}
          disabled={!allowManage || !online || saving}
          aria-label={t("purchasing.addProduct")}
          onClick={() => onAddProduct(product)}
        >
          <Plus className="size-4" aria-hidden />
        </Button>
      ),
    };
  });

  return (
    <ProductSelectionView
      layout={layout}
      columns={columns}
      rows={rows}
      emptyMessage={t("purchasing.noReadyProducts")}
      testId="po-linked-products"
    />
  );
}
