import { useState } from "react";
import { ShoppingCart, Trash2 } from "lucide-react";
import type { CustomerStorefrontProductDto } from "@/api/pos/pos-customer-orders-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import {
  formatQuantityDisplay,
  nextWeightCartQuantityKg,
} from "@/cart/sell-cart-helpers";
import { ConfirmationDialog } from "@/components/exits/SheetDialog";
import { useToast } from "@/components/exits/ToastProvider";
import { MoneyDisplay, QuantityStepper } from "@/components/exits/MoneyQuantity";
import { Button } from "@/components/ui/button";
import type { ShopCheckoutUiMode } from "@/features/customer-ordering/checkout-place-readiness";
import { isKiloStorefrontProduct } from "@/features/customer-ordering/storefront-availability";
import type { PersonalMerchantCartLine } from "@/features/customer-ordering/personal-merchant-cart";
import { formatShopCartQtyPriceMeta } from "@/features/customer-ordering/shop-cart-line-meta";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

type ShopOrderCartProps = {
  lines: PersonalMerchantCartLine[];
  productsById: ReadonlyMap<string, CustomerStorefrontProductDto>;
  workspace: PosWorkspaceScope | null;
  sellerOrganizationId: string;
  subtotal: number;
  showClose?: boolean;
  onClose?: () => void;
  onChangeQuantity: (product: CustomerStorefrontProductDto, quantity: number) => void;
  onEditWeight: (product: CustomerStorefrontProductDto) => void;
  onClear: () => void;
  onContinue?: () => void;
  onScheduleOrder?: () => void;
  /** Shop cart shows Continue to payment. Checkout reuses the cart without that button. */
  showPayButton?: boolean;
  continueBlockedDetail?: string | null;
  checkoutMode?: ShopCheckoutUiMode;
  storeStatusMessage?: string | null;
};

function lineProduct(
  line: PersonalMerchantCartLine,
  loaded: CustomerStorefrontProductDto | undefined,
): CustomerStorefrontProductDto {
  if (loaded) {
    return loaded;
  }
  return {
    productId: line.productId,
    name: line.name,
    sku: line.sku,
    unitOfMeasure: line.unitOfMeasure,
    unitPrice: line.unitPrice,
    isAvailable: true,
    tracksInventory: false,
    availableQuantity: null,
    availabilityStatus: "Untracked",
    hasImage: false,
    imageVersion: null,
    imageSource: "None",
    categoryId: null,
  };
}

function cartLineInitial(name: string): string {
  const trimmed = name.trim();
  return trimmed ? trimmed.charAt(0).toUpperCase() : "?";
}

function ShopCartLineThumb({ name }: { name: string }) {
  return (
    <div className="sell-cart-line__media sell-cart-line__media--fallback" aria-hidden>
      <span className="sell-cart-line__initial">{cartLineInitial(name)}</span>
    </div>
  );
}

export function ShopOrderCart({
  lines,
  productsById,
  workspace: _workspace,
  sellerOrganizationId: _sellerOrganizationId,
  subtotal,
  showClose = false,
  onClose,
  onChangeQuantity,
  onEditWeight,
  onClear,
  onContinue,
  onScheduleOrder,
  showPayButton = true,
  continueBlockedDetail = null,
  checkoutMode = "open",
  storeStatusMessage = null,
}: ShopOrderCartProps) {
  const { t } = useI18n();
  const toast = useToast();
  const [clearConfirmOpen, setClearConfirmOpen] = useState(false);
  const lineCount = lines.length;
  const canContinue = lineCount > 0 && checkoutMode === "open" && !continueBlockedDetail;
  const statusLine = storeStatusMessage?.trim() || null;

  return (
    <div className="sell-cart-panel flex min-h-0 flex-1 flex-col overflow-hidden">
      <div className="sell-cart-panel__header flex shrink-0 items-start justify-between gap-2">
        <div className="sell-cart-panel__title min-w-0">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">{t("sell.cartLabel")}</h2>
          {lineCount > 0 ? (
            <p className="sell-cart-panel__meta m-0 text-[length:var(--exits-text-xs)] text-muted">
              {lineCount} {lineCount === 1 ? t("sell.cartItemSingular") : t("sell.cartItemPlural")}
            </p>
          ) : null}
        </div>
        <div className="flex shrink-0 items-center gap-0.5">
          {lineCount > 0 ? (
            <Button
              type="button"
              intent="danger"
              appearance="ghost"
              className="sell-cart-clear min-h-9 px-2.5 text-[length:var(--exits-text-md)] font-bold"
              onClick={() => setClearConfirmOpen(true)}
            >
              {t("sell.cartClear")}
            </Button>
          ) : null}
          {showClose && onClose ? (
            <Button
              type="button"
              variant="ghost"
              className="min-h-8 px-2 text-[length:var(--exits-text-xs)]"
              aria-label={t("sell.cartSheetClose")}
              onClick={onClose}
            >
              {t("sell.cartSheetClose")}
            </Button>
          ) : null}
        </div>
      </div>

      {lineCount === 0 ? (
        <div className="sell-cart-empty flex flex-1 flex-col items-center justify-center gap-1 px-2 py-6 text-center">
          <ShoppingCart className="size-5 text-muted" strokeWidth={1.75} aria-hidden />
          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t("sell.payAddItems")}</p>
        </div>
      ) : (
        <ul className="sell-cart-lines m-0 min-h-0 flex-1 list-none overflow-y-auto p-0">
          {lines.map((line) => {
            const product = lineProduct(line, productsById.get(line.productId));
            const kilo = isKiloStorefrontProduct(product);
            const qtyLabel = formatQuantityDisplay(line.quantity);
            const amount = Math.round(line.unitPrice * line.quantity * 100) / 100;
            const atStockCap =
              product.tracksInventory &&
              product.availableQuantity != null &&
              line.quantity >= product.availableQuantity - 1e-9;

            return (
              <li key={line.productId} className="sell-cart-line sell-cart-line--enter">
                <ShopCartLineThumb name={line.name} />
                <div className="sell-cart-line__body sell-cart-line__body--shop">
                  <div className="sell-cart-line__copy">
                    <p className="sell-cart-line__name">{line.name}</p>
                    <MoneyDisplay amount={amount} className="sell-cart-line__amount" />
                    <span className="sell-cart-line__meta">
                      <span className="sell-cart-line__unit-price">
                        {formatShopCartQtyPriceMeta(
                          line.unitOfMeasure,
                          line.quantity,
                          line.unitPrice,
                        )}
                      </span>
                    </span>
                  </div>
                  <div className="sell-cart-line__side">
                    <Button
                      type="button"
                      variant="ghost"
                      className="sell-cart-line__remove"
                      aria-label={t("sell.cartRemoveLine")}
                      onClick={() => onChangeQuantity(product, 0)}
                    >
                      <Trash2 className="size-4" aria-hidden strokeWidth={2} />
                    </Button>
                    <div className="sell-cart-line__qty">
                      {kilo ? (
                        <QuantityStepper
                          compact
                          variant="pill"
                          className="quantity-stepper--soft-glass quantity-stepper--shop-pill"
                          value={qtyLabel}
                          decreaseLabel={t("sell.cartDecrease")}
                          increaseLabel={t("sell.cartIncrease")}
                          valueClickLabel={t("sell.weightEditTitle")}
                          incrementDisabled={atStockCap}
                          onDecrement={() =>
                            onChangeQuantity(product, nextWeightCartQuantityKg(line.quantity, -1))
                          }
                          onIncrement={() =>
                            onChangeQuantity(product, nextWeightCartQuantityKg(line.quantity, 1))
                          }
                          onValueClick={() => onEditWeight(product)}
                        />
                      ) : (
                        <QuantityStepper
                          compact
                          variant="pill"
                          className="quantity-stepper--soft-glass quantity-stepper--shop-pill"
                          editOnClick
                          value={qtyLabel}
                          decreaseLabel={t("sell.cartDecrease")}
                          increaseLabel={t("sell.cartIncrease")}
                          valueClickLabel={t("sell.quantityDirect")}
                          precision={0}
                          min={1}
                          max={
                            product.tracksInventory && product.availableQuantity != null
                              ? product.availableQuantity
                              : undefined
                          }
                          incrementDisabled={atStockCap}
                          onDecrement={() => onChangeQuantity(product, line.quantity - 1)}
                          onIncrement={() => onChangeQuantity(product, line.quantity + 1)}
                          onChange={(next) => onChangeQuantity(product, next)}
                        />
                      )}
                    </div>
                  </div>
                </div>
              </li>
            );
          })}
        </ul>
      )}

      <div className="sell-cart-footer mt-auto flex shrink-0 flex-col gap-2">
        <div className="sell-cart-footer__total-row flex items-baseline justify-between gap-3">
          <span className="sell-cart-footer__total-label text-[length:var(--exits-text-sm)] font-semibold">
            {t("sell.cartTotalLabel")}
          </span>
          <MoneyDisplay
            amount={subtotal}
            className="sell-cart-footer__total-amount text-[length:var(--exits-text-lg)] font-bold"
          />
        </div>
        {showPayButton ? (
          <>
            {checkoutMode === "paused" && lineCount > 0 ? (
              <div
                className="pc-shop-cart-notice personal-qa-glass personal-qa-glass--info flex flex-col gap-1 p-3 text-[length:var(--exits-text-sm)]"
                data-testid="shop-checkout-paused"
                data-shell="soft-glass"
              >
                <p className="m-0 font-semibold">{t("orders.shopPausedTitle")}</p>
                <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                  {t("orders.placePaused")}
                </p>
              </div>
            ) : null}
            {(checkoutMode === "schedule" || checkoutMode === "closed") && lineCount > 0 ? (
              <div
                className="pc-shop-cart-notice personal-qa-glass personal-qa-glass--info flex flex-col gap-1 p-3"
                data-testid="shop-checkout-closed"
                data-shell="soft-glass"
              >
                <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                  {t("orders.storeCurrentlyClosed")}
                </p>
                {statusLine ? (
                  <p className="m-0 text-[length:var(--exits-text-xs)] text-muted" data-testid="shop-store-status">
                    {statusLine}
                  </p>
                ) : null}
              </div>
            ) : null}
            {checkoutMode === "open" ? (
              <Button
                type="button"
                data-testid="shop-review"
                disabled={!canContinue}
                className={cn(
                  "sell-cart-pay w-full",
                  canContinue ? "sell-cart-pay--ready" : "sell-cart-pay--disabled",
                )}
                onClick={() => {
                  if (canContinue) {
                    onContinue?.();
                  }
                }}
              >
                {lineCount > 0 ? t("sell.continueToPayment") : t("sell.pay")}
              </Button>
            ) : null}
            {checkoutMode === "schedule" && lineCount > 0 ? (
              <Button
                type="button"
                intent="primary"
                appearance="solid"
                className="sell-cart-pay sell-cart-pay--ready w-full"
                data-testid="shop-schedule-order"
                onClick={() => onScheduleOrder?.()}
              >
                {t("orders.scheduleOrder")}
              </Button>
            ) : null}
            {(checkoutMode === "schedule" || checkoutMode === "closed" || checkoutMode === "paused")
            && lineCount > 0 ? (
              <Button
                type="button"
                intent="neutral"
                appearance="outline"
                className="w-full"
                data-testid="shop-save-cart"
                onClick={() => toast.success(t("orders.saveCartForLaterConfirm"))}
              >
                {t("orders.saveCartForLater")}
              </Button>
            ) : null}
            {checkoutMode === "open" && !canContinue ? (
              <p
                className="sell-cart-footer__hint m-0 text-center text-[length:var(--exits-text-xs)] text-muted"
                data-testid="shop-checkout-blocked"
              >
                {lineCount > 0 && continueBlockedDetail
                  ? continueBlockedDetail
                  : t("sell.payAddItems")}
              </p>
            ) : null}
            {checkoutMode === "blocked" && lineCount > 0 && continueBlockedDetail ? (
              <p
                className="sell-cart-footer__hint m-0 text-center text-[length:var(--exits-text-xs)] text-muted"
                data-testid="shop-checkout-blocked"
              >
                {continueBlockedDetail}
              </p>
            ) : null}
          </>
        ) : null}
      </div>

      <ConfirmationDialog
        open={clearConfirmOpen}
        title={t("sell.cartClearTitle")}
        detail={t("sell.cartClearDetail")}
        confirmLabel={t("sell.cartClearConfirm")}
        cancelLabel={t("sell.cancel")}
        onCancel={() => setClearConfirmOpen(false)}
        onConfirm={() => {
          onClear();
          setClearConfirmOpen(false);
        }}
      />
    </div>
  );
}
