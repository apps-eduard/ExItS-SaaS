import { useState } from "react";
import { ShoppingCart, Trash2 } from "lucide-react";
import type { CustomerStorefrontProductDto } from "@/api/pos/pos-customer-orders-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import {
  formatQuantityDisplay,
  nextWeightCartQuantityKg,
} from "@/cart/sell-cart-helpers";
import { ConfirmationDialog } from "@/components/exits/SheetDialog";
import { MoneyDisplay, QuantityStepper } from "@/components/exits/MoneyQuantity";
import { Button } from "@/components/ui/button";
import { isKiloStorefrontProduct } from "@/features/customer-ordering/storefront-availability";
import type { PersonalMerchantCartLine } from "@/features/customer-ordering/personal-merchant-cart";
import { useStorefrontProductImageUrl } from "@/features/customer-ordering/use-storefront-product-image";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { formatUnitOfMeasureSymbol } from "@/lib/unit-of-measure";

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
  /** Shop cart shows Continue to payment. Checkout reuses the cart without that button. */
  showPayButton?: boolean;
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

function ShopCartLineThumb({
  workspace,
  sellerOrganizationId,
  productId,
  hasImage,
  imageVersion,
  name,
}: {
  workspace: PosWorkspaceScope | null;
  sellerOrganizationId: string;
  productId: string;
  hasImage: boolean;
  imageVersion?: number | null;
  name: string;
}) {
  const imageUrl = useStorefrontProductImageUrl(
    workspace,
    sellerOrganizationId,
    productId,
    hasImage,
    imageVersion,
  );
  return (
    <div className="sell-cart-line__media" aria-hidden>
      {imageUrl ? (
        <img src={imageUrl} alt="" className="sell-cart-line__image" />
      ) : (
        <span className="sell-cart-line__initial">{cartLineInitial(name)}</span>
      )}
    </div>
  );
}

export function ShopOrderCart({
  lines,
  productsById,
  workspace,
  sellerOrganizationId,
  subtotal,
  showClose = false,
  onClose,
  onChangeQuantity,
  onEditWeight,
  onClear,
  onContinue,
  showPayButton = true,
}: ShopOrderCartProps) {
  const { t } = useI18n();
  const [clearConfirmOpen, setClearConfirmOpen] = useState(false);
  const lineCount = lines.length;
  const canContinue = lineCount > 0;

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
              variant="ghost"
              className="min-h-8 px-2 text-[length:var(--exits-text-xs)] text-muted"
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
            const unitLabel = kilo ? "kg" : formatUnitOfMeasureSymbol(line.unitOfMeasure);
            const amount = Math.round(line.unitPrice * line.quantity * 100) / 100;
            const atStockCap =
              product.tracksInventory &&
              product.availableQuantity != null &&
              line.quantity >= product.availableQuantity - 1e-9;

            return (
              <li key={line.productId} className="sell-cart-line sell-cart-line--enter">
                <ShopCartLineThumb
                  workspace={workspace}
                  sellerOrganizationId={sellerOrganizationId}
                  productId={line.productId}
                  hasImage={product.hasImage === true}
                  imageVersion={product.imageVersion}
                  name={line.name}
                />
                <div className="sell-cart-line__body">
                  <div className="sell-cart-line__top">
                    <p className="sell-cart-line__name">{line.name}</p>
                    <div className="sell-cart-line__price-actions">
                      <MoneyDisplay amount={amount} className="sell-cart-line__amount" />
                      <Button
                        type="button"
                        variant="ghost"
                        className="sell-cart-line__remove"
                        aria-label={t("sell.cartRemoveLine")}
                        onClick={() => onChangeQuantity(product, 0)}
                      >
                        <Trash2 className="size-4" aria-hidden strokeWidth={2} />
                      </Button>
                    </div>
                  </div>
                  <div className="sell-cart-line__bottom">
                    <span className="sell-cart-line__meta">
                      {kilo ? t("sell.tileByWeight") : product.unitOfMeasure}
                      <span className="sell-cart-line__unit-price">
                        {" "}
                        · {qtyLabel}×₱{line.unitPrice.toFixed(2)}
                      </span>
                    </span>
                    <div className="sell-cart-line__qty">
                      {kilo ? (
                        <QuantityStepper
                          compact
                          variant="auto"
                          value={`${qtyLabel}kg`}
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
                          variant="auto"
                          editOnClick
                          value={qtyLabel}
                          unit={unitLabel}
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
            className="sell-cart-footer__total-amount text-[length:var(--exits-text-md)] font-bold"
          />
        </div>
        {showPayButton ? (
          <>
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
              {canContinue ? t("sell.continueToPayment") : t("sell.pay")}
            </Button>
            {canContinue ? null : (
              <p className="sell-cart-footer__hint m-0 text-center text-[length:var(--exits-text-xs)] text-muted">
                {t("sell.payAddItems")}
              </p>
            )}
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
