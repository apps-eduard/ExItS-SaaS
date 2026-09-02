import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import {
  remainingQuantityAfterCart,
  resolveAddFlow,
  resolveSellCardStock,
  resolveSellUnitPrice,
} from "@/cart/sell-cart-helpers";
import { sellStockCaption } from "@/features/sell/sell-stock-caption";
import { useCatalogProductImageUrl } from "@/features/sell/use-catalog-product-image";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

function productInitial(name: string): string {
  const trimmed = name.trim();
  return trimmed ? trimmed.charAt(0).toUpperCase() : "?";
}

type SellProductCardProps = {
  product: PosCatalogProductDto;
  workspace: PosWorkspaceScope | null;
  onAdd: (product: PosCatalogProductDto) => void;
  /** Brief highlight after the product was added to cart. */
  addedFlash?: boolean;
  /** Base quantity already in this register's cart (not a stock reservation). */
  cartReservedBaseQty?: number;
  /** Committed out of stock — visible for review, not addable. */
  unavailable?: boolean;
};

export function SellProductCard({
  product,
  workspace,
  onAdd,
  addedFlash = false,
  cartReservedBaseQty = 0,
  unavailable = false,
}: SellProductCardProps) {
  const { t } = useI18n();
  const imageUrl = useCatalogProductImageUrl(
    workspace,
    product.productId,
    product.hasImage === true,
    product.imageVersion,
  );
  const remainingOnHand = remainingQuantityAfterCart(product.onHandQuantity, cartReservedBaseQty);
  const stock = resolveSellCardStock({
    isTracked: product.isTracked,
    onHandQuantity: remainingOnHand,
    unitOfMeasure: product.unitOfMeasure,
    tracksExpiration: product.tracksExpiration,
    stockStatus: product.stockStatus,
  });
  const flow = resolveAddFlow(product);

  return (
    <button
      type="button"
      data-testid={`sell-product-${product.productId}`}
      className={cn(
        "sell-product-card",
        addedFlash && "sell-product-card--added",
        unavailable && "sell-product-card--unavailable",
      )}
      disabled={unavailable}
      aria-disabled={unavailable}
      onClick={() => {
        if (!unavailable) {
          onAdd(product);
        }
      }}
    >
      <div className="sell-product-card__media">
        {imageUrl ? (
          <img
            src={imageUrl}
            alt=""
            className="sell-product-card__image"
            loading="lazy"
            decoding="async"
          />
        ) : (
          <span className="sell-product-card__initial" aria-hidden>
            {productInitial(product.name)}
          </span>
        )}
      </div>
      <div className="sell-product-card__body">
        <span className="sell-product-card__name">{product.name}</span>
        <div className="sell-product-card__price-row">
          <MoneyDisplay
            amount={resolveSellUnitPrice(product, null)}
            className="sell-product-card__price"
            testId={`sell-product-price-${product.productId}`}
          />
          {flow.kind === "weight" ||
          flow.kind === "customQuantity" ||
          flow.kind === "unitSelector" ? (
            <span className="sell-product-card__hint">
              {flow.kind === "weight"
                ? t("sell.tileByWeight")
                : flow.kind === "customQuantity"
                  ? t("sell.tileCustomQty")
                  : t("sell.tileChooseUnit")}
            </span>
          ) : (
            <span className="sell-product-card__hint sell-product-card__hint--muted">
              {product.unitOfMeasure}
            </span>
          )}
        </div>
        <span
          data-testid={`sell-product-stock-${product.productId}`}
          className={`sell-product-card__stock sell-product-card__stock--${stock.tone}`}
        >
          {sellStockCaption(t, stock)}
        </span>
      </div>
    </button>
  );
}
