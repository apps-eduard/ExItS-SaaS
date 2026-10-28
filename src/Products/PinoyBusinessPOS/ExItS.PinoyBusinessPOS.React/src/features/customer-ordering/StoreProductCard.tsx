import { Package } from "lucide-react";
import type { CustomerStorefrontProductDto } from "@/api/pos/pos-customer-orders-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { formatQuantityDisplay, resolveSellCardStock } from "@/cart/sell-cart-helpers";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { sellAvailableCaption } from "@/features/catalog/catalog-stock-caption";
import { formatShopCartUnitLabel } from "@/features/customer-ordering/shop-cart-line-meta";
import { isKiloStorefrontProduct } from "@/features/customer-ordering/storefront-availability";
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
  workspace: _workspace,
  sellerOrganizationId: _sellerOrganizationId,
  quantity,
  canAdd,
  onAdd,
  t,
}: StoreProductCardProps) {
  // Soft Glass shop tiles: letter + package fallback only (no product photos for now).
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
      <span className="sell-product-card__name">{product.name}</span>
      <div className="sell-product-card__media">
        <span className="sell-product-card__fallback" aria-hidden>
          <Package className="sell-product-card__fallback-icon" strokeWidth={1.75} />
          <span className="sell-product-card__initial">{productInitial(product.name)}</span>
        </span>
      </div>
      <div className="sell-product-card__body">
        <div className="sell-product-card__price-row">
          <MoneyDisplay amount={product.unitPrice} className="sell-product-card__price" />
          {kilo ? (
            <span className="sell-product-card__hint">{t("sell.tileByWeight")}</span>
          ) : null}
        </div>
        {!kilo ? (
          <span className="sell-product-card__hint sell-product-card__hint--muted">
            {formatShopCartUnitLabel(product.unitOfMeasure)}
          </span>
        ) : null}
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
