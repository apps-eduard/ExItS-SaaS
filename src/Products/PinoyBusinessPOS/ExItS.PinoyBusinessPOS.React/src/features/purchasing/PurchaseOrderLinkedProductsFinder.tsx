import { Plus, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import type { ResponsiveDataLayout } from "@/components/exits/responsive-data-view";
import {
  ProductSelectionView,
  type ProductSelectionColumn,
  type ProductSelectionRow,
} from "@/components/exits/ProductSelectionView";
import {
  formatSupplierAvailabilityLabel,
  formatUnitPriceLabel,
  type ConnectedPoReadyProduct,
} from "@/features/purchasing/purchase-order-create-connected";

type Translate = (key: string) => string;

type PurchaseOrderLinkedProductsFinderProps = {
  layout: ResponsiveDataLayout;
  products: readonly ConnectedPoReadyProduct[];
  allowManage: boolean;
  online: boolean;
  saving: boolean;
  /** `add` = Plus action; `added` = remove-from-order (Added filter). */
  actionMode?: "add" | "added";
  onAddProduct: (product: ConnectedPoReadyProduct) => void;
  onRemoveProduct?: (product: ConnectedPoReadyProduct) => void;
  t: Translate;
};

/**
 * Create PO (Linked tab) adapter — supplier stock/price/add → ProductSelectionView.
 * Parent excludes selected lines unless `actionMode="added"` (Added filter).
 * Stock is informational; Add is never blocked by availability.
 * SKU sits under the product name (no separate SKU column).
 */
export function PurchaseOrderLinkedProductsFinder({
  layout,
  products,
  allowManage,
  online,
  saving,
  actionMode = "add",
  onAddProduct,
  onRemoveProduct,
  t,
}: PurchaseOrderLinkedProductsFinderProps) {
  const columns: ProductSelectionColumn[] = [
    { id: "product", header: t("purchasing.colProduct"), colSize: "flex" },
    {
      id: "category",
      header: t("purchasing.category"),
      className: "po-linked-col--category",
    },
    {
      id: "stock",
      header: t("purchasing.colStock"),
      cellAlign: "numeric",
      colSize: "numeric",
      className: "po-linked-col--stock",
    },
    {
      id: "price",
      header: t("purchasing.supplierPrice"),
      cellAlign: "numeric",
      colSize: "money",
      className: "po-linked-col--price",
    },
  ];

  const rows: ProductSelectionRow[] = products.map((product) => {
    const stockLabel = formatSupplierAvailabilityLabel(product, t);
    const priceLabel = formatUnitPriceLabel(product.unitPurchaseCost);
    const skuLabel = product.supplierSku ?? t("connected.noSku");
    const skuCell = (
      <span className="po-linked-sku" data-testid={`po-sku-${product.buyerProductId}`}>
        {skuLabel}
      </span>
    );
    const categoryLabel = product.categoryName?.trim() || "—";
    const categoryCell = (
      <span data-testid={`po-category-${product.buyerProductId}`}>{categoryLabel}</span>
    );
    const productCell = (
      <span className="po-linked-product-cell">
        <span className="po-linked-product-name font-medium leading-snug">{product.productName}</span>
        {skuCell}
      </span>
    );

    return {
      id: product.buyerProductId,
      testId: `po-connected-product-${product.buyerProductId}`,
      title: product.productName,
      subtitle: skuCell,
      status: (
        <span data-testid={`po-stock-${product.buyerProductId}`}>{stockLabel}</span>
      ),
      cells: [
        productCell,
        categoryCell,
        <span key="stock" data-testid={`po-stock-${product.buyerProductId}`}>
          {stockLabel}
        </span>,
        <span key="price" className="tabular-nums">
          {priceLabel}
        </span>,
      ],
      fields: [
        { label: t("purchasing.category"), value: categoryCell },
        { label: t("purchasing.colStock"), value: stockLabel },
        { label: t("purchasing.supplierPrice"), value: priceLabel, emphasize: true },
      ],
      primaryAction:
        actionMode === "added" ? (
          <Button
            type="button"
            intent="danger"
            appearance="outline"
            size="icon"
            shape="round"
            className="po-linked-remove-btn"
            data-testid={`po-remove-${product.buyerProductId}`}
            disabled={!allowManage || !online || saving || !onRemoveProduct}
            aria-label={t("purchasing.removeLine")}
            onClick={() => onRemoveProduct?.(product)}
          >
            <Trash2 className="size-4" aria-hidden />
          </Button>
        ) : (
          <Button
            type="button"
            intent="primary"
            appearance="outline"
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
      actionHeader={actionMode === "added" ? t("purchasing.removeLine") : t("purchasing.addProduct")}
      actionColClassName="po-linked-action-col"
      testId="po-linked-products"
    />
  );
}
