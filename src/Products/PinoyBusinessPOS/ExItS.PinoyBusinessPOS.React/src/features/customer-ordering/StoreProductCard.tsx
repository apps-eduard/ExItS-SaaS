import type { CustomerStorefrontProductDto } from "@/api/pos/pos-customer-orders-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { StatusChip } from "@/components/exits/StatusChip";
import { StorefrontProductThumbnail } from "@/features/customer-ordering/StorefrontProductThumbnail";
import { cn } from "@/lib/cn";
import type { MessageKey } from "@/i18n/messages";

function money(n: number): string {
  return `₱${n.toFixed(2)}`;
}

function availabilityLabel(
  t: (key: MessageKey) => string,
  product: CustomerStorefrontProductDto,
): string {
  switch (product.availabilityStatus) {
    case "OutOfStock":
      return t("orders.availabilityOut");
    case "LowStock":
      return t("orders.availabilityLow");
    case "InStock":
      return t("orders.availabilityIn");
    default:
      return t("orders.availabilityUntracked");
  }
}

type StoreProductCardProps = {
  product: CustomerStorefrontProductDto;
  workspace: PosWorkspaceScope | null;
  sellerOrganizationId: string;
  quantity: number;
  canAdd: boolean;
  onIncrement: () => void;
  onDecrement: () => void;
  t: (key: MessageKey) => string;
};

export function StoreProductCard({
  product,
  workspace,
  sellerOrganizationId,
  quantity,
  canAdd,
  onIncrement,
  onDecrement,
  t,
}: StoreProductCardProps) {
  const outOfStock = !product.isAvailable || product.availabilityStatus === "OutOfStock";

  return (
    <article
      className={cn("pc-product-card", outOfStock && "pc-product-card--unavailable")}
      data-testid="storefront-product"
    >
      <div className="pc-product-card__media">
        <StorefrontProductThumbnail
          workspace={workspace}
          sellerOrganizationId={sellerOrganizationId}
          product={product}
        />
      </div>

      <div className="pc-product-card__body">
        <h3 className="pc-product-card__name">{product.name}</h3>
        <div className="pc-product-card__price-row">
          <span className="pc-product-card__price">{money(product.unitPrice)}</span>
          <span className="pc-product-card__unit">/ {product.unitOfMeasure}</span>
        </div>
        {product.tracksInventory && product.availableQuantity != null ? (
          <p className="pc-product-card__stock m-0">
            {product.availableQuantity} {t("orders.items")}
          </p>
        ) : null}
        <StatusChip tone={product.isAvailable ? "success" : "danger"}>
          {availabilityLabel(t, product)}
        </StatusChip>
      </div>

      <div className="pc-product-card__footer">
        {quantity > 0 ? (
          <div className="pc-qty-stepper" data-testid="cart-qty-controls">
            <button
              type="button"
              className="pc-qty-stepper__btn"
              disabled={quantity <= 0}
              data-testid="cart-decrement"
              aria-label={t("orders.cartEmptyTitle")}
              onClick={onDecrement}
            >
              −
            </button>
            <span className="pc-qty-stepper__value" data-testid="cart-qty">
              {quantity}
            </span>
            <button
              type="button"
              className="pc-qty-stepper__btn"
              disabled={!canAdd}
              data-testid="cart-increment"
              aria-label={t("orders.reviewOrder")}
              onClick={onIncrement}
            >
              +
            </button>
          </div>
        ) : (
          <Button
            type="button"
            className="pc-qty-stepper__add w-full"
            disabled={!canAdd}
            data-testid="cart-increment"
            aria-label={t("sell.addToCart")}
            onClick={onIncrement}
          >
            +
          </Button>
        )}
      </div>
    </article>
  );
}
