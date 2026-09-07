import type { ReplenishmentCatalogItemDto } from "@/api/pos/pos-stock-requests-client";
import {
  formatQuantityDisplay,
  isByWeightSellingMode,
} from "@/cart/sell-cart-helpers";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
import { cn } from "@/lib/cn";

function displayUom(sellingMode: string, unitOfMeasure: string): string {
  if (isByWeightSellingMode(sellingMode)) {
    return "kg";
  }
  const trimmed = unitOfMeasure.trim();
  if (trimmed.toLowerCase() === "kilogram") {
    return "kg";
  }
  return trimmed || "pc";
}

type RequestStockProductCardProps = {
  product: ReplenishmentCatalogItemDto;
  inBasket?: boolean;
  addedFlash?: boolean;
  onSelect: (product: ReplenishmentCatalogItemDto) => void;
};

/**
 * Sell-density product tile for Request stock — no image media; replenishment facts
 * occupy the space Sell uses for thumbnail + selling price.
 */
export function RequestStockProductCard({
  product,
  inBasket = false,
  addedFlash = false,
  onSelect,
}: RequestStockProductCardProps) {
  const { t } = useI18n();
  const uom = displayUom(product.sellingMode, product.unitOfMeasure);
  const byWeight = isByWeightSellingMode(product.sellingMode);
  const warehouseOut = product.warehouseAvailableQuantity <= 0;

  return (
    <button
      type="button"
      data-testid={`retail-warehouse-product-${product.productId}`}
      className={cn(
        "sell-product-card sell-product-card--request",
        inBasket && "sell-product-card--selected",
        addedFlash && "sell-product-card--added",
      )}
      aria-pressed={inBasket}
      onClick={() => onSelect(product)}
    >
      <div className="sell-product-card__body">
        <span className="sell-product-card__name">{product.name}</span>
        {product.sku ? (
          <span
            className="sell-product-card__sku"
            data-testid={`retail-warehouse-sku-${product.productId}`}
          >
            {product.sku}
          </span>
        ) : null}

        <dl className="sell-product-card__replenish m-0">
          <div className="sell-product-card__replenish-row">
            <dt>{t("retailWarehouse.request.cardBranch")}</dt>
            <dd data-testid={`retail-warehouse-branch-stock-${product.productId}`}>
              {formatQuantityDisplay(product.branchOnHandQuantity)} {uom}
            </dd>
          </div>
          <div className="sell-product-card__replenish-row">
            <dt>{t("retailWarehouse.request.cardWarehouse")}</dt>
            <dd
              data-testid={
                warehouseOut
                  ? `retail-warehouse-oos-${product.productId}`
                  : `retail-warehouse-wh-stock-${product.productId}`
              }
              className={warehouseOut ? "text-[var(--exits-danger)]" : undefined}
            >
              {warehouseOut
                ? t("retailWarehouse.request.warehouseOutOfStock")
                : `${formatQuantityDisplay(product.warehouseAvailableQuantity)} ${uom}`}
            </dd>
          </div>
          {product.warehouseUnitCost != null ? (
            <div className="sell-product-card__replenish-row">
              <dt>{t("retailWarehouse.request.cardCost")}</dt>
              <dd data-testid={`retail-warehouse-cost-${product.productId}`}>
                {formatPeso(product.warehouseUnitCost)}/{uom}
              </dd>
            </div>
          ) : null}
        </dl>

        {byWeight ? (
          <span className="sell-product-card__hint">{t("sell.tileByWeight")}</span>
        ) : (
          <span className="sell-product-card__hint sell-product-card__hint--muted">{uom}</span>
        )}
      </div>
    </button>
  );
}

export { displayUom as requestStockDisplayUom };
