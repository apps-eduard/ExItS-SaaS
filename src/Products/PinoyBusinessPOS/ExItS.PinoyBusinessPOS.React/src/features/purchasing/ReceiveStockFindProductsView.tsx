import { Plus } from "lucide-react";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import { Button } from "@/components/ui/button";
import { StatusChip } from "@/components/exits/StatusChip";
import { ExitsTablePagination } from "@/components/exits/ExitsTable";
import type { ResponsiveDataLayout } from "@/components/exits/responsive-data-view";
import {
  ProductSelectionView,
  type ProductSelectionColumn,
  type ProductSelectionRow,
} from "@/components/exits/ProductSelectionView";

type Translate = (key: string) => string;

type ReceiveStockFindProductsViewProps = {
  layout: ResponsiveDataLayout;
  products: readonly PosCatalogProductDto[];
  categoryNameById: ReadonlyMap<string, string>;
  page: number;
  pageSize: number;
  total: number;
  pageSizeOptions: readonly number[];
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
  onAddProduct: (product: PosCatalogProductDto) => void;
  t: Translate;
};

function TrackingChip({
  notTracked,
  t,
}: {
  notTracked: boolean;
  t: Translate;
}) {
  return (
    <StatusChip tone={notTracked ? "neutral" : "primary"}>
      {notTracked ? t("inventory.notTracked") : t("inventory.tracked")}
    </StatusChip>
  );
}

function AddProductButton({
  product,
  notTracked,
  onAddProduct,
  t,
}: {
  product: PosCatalogProductDto;
  notTracked: boolean;
  onAddProduct: (product: PosCatalogProductDto) => void;
  t: Translate;
}) {
  return (
    <Button
      type="button"
      size="icon"
      shape="round"
      variant={notTracked ? "secondary" : "default"}
      onClick={() => onAddProduct(product)}
      data-testid={`direct-add-${product.productId}`}
      aria-label={
        notTracked
          ? t("purchasing.inventoryTrackingRequired")
          : t("purchasing.addProduct")
      }
      title={
        notTracked
          ? t("purchasing.inventoryTrackingRequired")
          : t("purchasing.addProduct")
      }
    >
      <Plus className="size-4" aria-hidden />
    </Button>
  );
}

/**
 * Receive Stock adapter — maps org catalog products into ProductSelectionView.
 * Tracked-only / search / categories stay in the page filter toolbar.
 */
export function ReceiveStockFindProductsView({
  layout,
  products,
  categoryNameById,
  page,
  pageSize,
  total,
  pageSizeOptions,
  onPageChange,
  onPageSizeChange,
  onAddProduct,
  t,
}: ReceiveStockFindProductsViewProps) {
  const columns: ProductSelectionColumn[] = [
    { id: "product", header: t("purchasing.receiveProduct") },
    { id: "category", header: t("purchasing.category") },
    { id: "tracking", header: t("purchasing.inventoryTracking") },
  ];

  const rows: ProductSelectionRow[] = products.map((product) => {
    const notTracked = product.isTracked === false;
    const categoryName =
      product.categoryId != null
        ? (categoryNameById.get(product.categoryId) ?? "—")
        : "—";
    const tracking = <TrackingChip notTracked={notTracked} t={t} />;
    const productCell = (
      <>
        <div className="font-medium leading-snug">{product.name}</div>
        {product.sku ? (
          <div className="text-[length:var(--exits-text-xs)] text-muted">{product.sku}</div>
        ) : null}
      </>
    );
    return {
      id: product.productId,
      testId: `direct-product-${product.productId}`,
      title: product.name,
      subtitle: product.sku || undefined,
      status: tracking,
      cells: [productCell, categoryName, tracking],
      fields: [{ label: t("purchasing.category"), value: categoryName }],
      primaryAction: (
        <AddProductButton
          product={product}
          notTracked={notTracked}
          onAddProduct={onAddProduct}
          t={t}
        />
      ),
    };
  });

  return (
    <ProductSelectionView
      layout={layout}
      columns={columns}
      rows={rows}
      actionHeader={t("purchasing.action")}
      actionColClassName="receive-stock-product-table__action-col"
      testId="direct-product-results"
      className="receive-stock-find-products-responsive"
      tableClassName="receive-stock-product-table"
      listClassName="receive-stock-product-list"
      pagination={
        <ExitsTablePagination
          page={page}
          pageSize={pageSize}
          total={total}
          pageSizeOptions={pageSizeOptions}
          onPageChange={onPageChange}
          onPageSizeChange={onPageSizeChange}
          rowsPerPageLabel={t("exitsTable.rowsPerPage")}
          previousLabel={t("exitsTable.previous")}
          nextLabel={t("exitsTable.next")}
          rangeLabel={t("exitsTable.range")}
        />
      }
    />
  );
}
