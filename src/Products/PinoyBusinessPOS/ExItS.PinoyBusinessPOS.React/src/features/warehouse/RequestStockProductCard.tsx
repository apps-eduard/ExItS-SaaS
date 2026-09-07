import type { ReplenishmentCatalogItemDto } from "@/api/pos/pos-stock-requests-client";
import {
  formatQuantityDisplay,
  isByWeightSellingMode,
} from "@/cart/sell-cart-helpers";
import { remainingWarehouseAvailable } from "@/features/warehouse/retail-warehouse-request-availability";
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
  /** Basket requested qty for this product (UI remaining projection only). */
  requestedQtyInBasket?: number;
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
  requestedQtyInBasket = 0,
  inBasket = false,
  addedFlash = false,
  onSelect,
}: RequestStockProductCardProps) {
  const { t } = useI18n();
  const uom = displayUom(product.sellingMode, product.unitOfMeasure);
  const byWeight = isByWeightSellingMode(product.sellingMode);
  const warehouseActual = product.warehouseAvailableQuantity;
  const warehouseOut = warehouseActual <= 0;
  const warehouseRemaining = remainingWarehouseAvailable(
    warehouseActual,
    requestedQtyInBasket,
  );

  return (
    <button
      type="button"
      data-testid={`retail-warehouse-product-${product.productId}`}
      className={cn(
        "sell-product-card sell-product-card--request",
        inBasket && "sell-product-card--selected",
        addedFlash && "sell-product-card--added",
        warehouseOut && "sell-product-card--unavailable",
      )}
      aria-pressed={inBasket}
      disabled={warehouseOut}
      aria-disabled={warehouseOut}
      onClick={() => {
        if (!warehouseOut) {
          onSelect(product);
        }
      }}
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
            <dt title={t("retailWarehouse.request.remainingTooltip")}>
              {t("retailWarehouse.request.cardWarehouse")}
            </dt>
            <dd
              data-testid={
                warehouseOut
                  ? `retail-warehouse-oos-${product.productId}`
                  : `retail-warehouse-wh-stock-${product.productId}`
              }
              data-warehouse-actual={warehouseActual}
              data-warehouse-remaining={warehouseRemaining}
              title={t("retailWarehouse.request.remainingTooltip")}
              className={warehouseOut ? "text-[var(--exits-danger)]" : undefined}
            >
              {warehouseOut
                ? t("retailWarehouse.request.warehouseOutOfStock")
                : `${formatQuantityDisplay(warehouseRemaining)} ${uom}`}
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
