import type { ReactNode } from "react";
import { Loader2, ShoppingBag } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/cn";

export function storeDisplayInitial(name: string): string {
  const trimmed = name.trim();
  return trimmed ? trimmed.charAt(0).toUpperCase() : "?";
}

export function CommerceLoadMore({
  label,
  loadingLabel,
  busy,
  onClick,
  testId,
}: {
  label: string;
  loadingLabel: string;
  busy: boolean;
  onClick: () => void;
  testId: string;
}) {
  return (
    <Button
      type="button"
      variant="outline"
      className="pc-load-more"
      data-testid={testId}
      disabled={busy}
      onClick={onClick}
    >
      {busy ? loadingLabel : label}
    </Button>
  );
}

export function ShopCartBar({
  itemCount,
  subtotalLabel,
  subtotal,
  actionLabel,
  onReview,
  disabled,
}: {
  itemCount: number;
  subtotalLabel: string;
  subtotal: string;
  actionLabel: string;
  onReview: () => void;
  disabled?: boolean;
}) {
  if (itemCount <= 0) {
    return null;
  }

  return (
    <div className="pc-cart-dock" data-testid="shop-cart-bar">
      <div className="pc-cart-dock__summary">
        <span className="pc-cart-dock__label">{subtotalLabel}</span>
        <span className="pc-cart-dock__value" data-testid="shop-cart-summary">
          {itemCount} · {subtotal}
        </span>
      </div>
      <Button
        type="button"
        className="pc-cart-dock__action gap-2"
        data-testid="shop-review"
        disabled={disabled}
        onClick={onReview}
      >
        <ShoppingBag className="size-4 shrink-0" aria-hidden />
        {actionLabel}
      </Button>
    </div>
  );
}

export function SegmentedOption({
  pressed,
  onClick,
  children,
  testId,
  className,
}: {
  pressed: boolean;
  onClick: () => void;
  children: ReactNode;
  testId?: string;
  className?: string;
}) {
  return (
    <button
      type="button"
      role="radio"
      aria-checked={pressed}
      data-testid={testId}
      className={cn("pc-segmented__item", pressed && "pc-segmented__item--active", className)}
      onClick={onClick}
    >
      {children}
    </button>
  );
}

export function CheckoutPlaceButton({
  label,
  busyLabel,
  busy,
  disabled,
  onClick,
}: {
  label: string;
  busyLabel: string;
  busy: boolean;
  disabled?: boolean;
  onClick: () => void;
}) {
  return (
    <Button
      type="button"
      className="pc-checkout-place gap-2"
      data-testid="place-order"
      disabled={disabled || busy}
      onClick={onClick}
    >
      {busy ? <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden /> : null}
      {busy ? busyLabel : label}
    </Button>
  );
}
