import type { CustomerStorefrontProductDto } from "@/api/pos/pos-customer-orders-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { formatQuantityDisplay, resolveSellCardStock } from "@/cart/sell-cart-helpers";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { sellAvailableCaption } from "@/features/catalog/catalog-stock-caption";
import { isKiloStorefrontProduct } from "@/features/customer-ordering/storefront-availability";
import { useStorefrontProductImageUrl } from "@/features/customer-ordering/use-storefront-product-image";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";

function productInitial(name: string): string {
  const trimmed = name.trim();
  return trimmed ? trimmed.charAt(0).toUpperCase() : "?";
}

type StoreProductCardProps = {
  product: CustomerStorefrontProductDto;
  workspace: PosWorkspaceScope | null;
  sellerOrganizationId: string;
  quantity: number;
  canAdd: boolean;
  onAdd: () => void;
  t: (key: MessageKey) => string;
};

export function StoreProductCard({
  product,
  workspace,
  sellerOrganizationId,
  quantity,
  canAdd,
  onAdd,
  t,
}: StoreProductCardProps) {
  const imageUrl = useStorefrontProductImageUrl(
    workspace,
    sellerOrganizationId,
    product.productId,
    product.hasImage,
    product.imageVersion,
  );
  const kilo = isKiloStorefrontProduct(product);
  const unavailable = !canAdd && quantity <= 0;
  const remaining =
    product.tracksInventory && product.availableQuantity != null
      ? Math.max(0, product.availableQuantity - quantity)
      : product.availableQuantity;
  const stock = resolveSellCardStock({
    isTracked: product.tracksInventory,
    onHandQuantity: remaining,
    unitOfMeasure: product.unitOfMeasure,
    stockStatus: product.availabilityStatus,
  });

  return (
    <button
      type="button"
      data-testid="cart-increment"
      data-storefront-product=""
      className={cn(
        "sell-product-card",
        quantity > 0 && "sell-product-card--added",
        unavailable && "sell-product-card--unavailable",
      )}
      disabled={unavailable}
      aria-disabled={unavailable}
      onClick={() => {
        if (!unavailable) {
          onAdd();
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
          <MoneyDisplay amount={product.unitPrice} className="sell-product-card__price" />
          {kilo ? (
            <span className="sell-product-card__hint">{t("sell.tileByWeight")}</span>
          ) : (
            <span className="sell-product-card__hint sell-product-card__hint--muted">
              {product.unitOfMeasure}
            </span>
          )}
        </div>
        <span
          className={cn(
            "sell-product-card__stock",
            stock.tone !== "ok" && stock.tone !== "untracked" && `sell-product-card__stock--${stock.tone}`,
          )}
        >
          {sellAvailableCaption(t, stock)}
        </span>
        {quantity > 0 ? (
          <span className="sr-only" data-testid="cart-qty">
            {formatQuantityDisplay(quantity)}
          </span>
        ) : null}
      </div>
    </button>
  );
}
