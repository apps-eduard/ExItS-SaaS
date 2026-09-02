import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { ShoppingCart, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { effectiveUnitPrice, lineAmount, type SessionCartLine } from "@/cart/SessionCartProvider";
import { formatQuantityDisplay, isByWeightSellingMode } from "@/cart/sell-cart-helpers";
import type { CartLineStockIssue } from "@/cart/sell-cart-helpers";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { ConfirmationDialog } from "@/components/exits/SheetDialog";
import { MoneyDisplay, QuantityStepper } from "@/components/exits/MoneyQuantity";
import type { CheckoutShiftReadiness } from "@/features/shifts/checkout-readiness";
import { useCatalogProductImageUrl } from "@/features/sell/use-catalog-product-image";
import type { MidSessionSellBlock } from "@/features/sell/sell-readiness";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

export type MidSessionBlockProp = MidSessionSellBlock["kind"];

type SellCartPanelProps = {
  lines: SessionCartLine[];
  lineCount: number;
  subtotal: number;
  onIncrement: (lineKey: string) => void;
  onDecrement: (lineKey: string) => void;
  onRemove: (lineKey: string) => void;
  onSetQuantity: (lineKey: string, quantity: number) => void;
  onEditWeight: (line: SessionCartLine) => void;
  onEditCustomQuantity?: (line: SessionCartLine) => void;
  onChangePrice?: (line: SessionCartLine) => void;
  onClear: () => void;
  showClose?: boolean;
  onClose?: () => void;
  panelId?: string;
  checkoutReadiness?: CheckoutShiftReadiness;
  canCreateSale?: boolean;
  canOverrideSalePrice?: boolean;
  midSessionBlock?: MidSessionBlockProp;
  stockIssues?: CartLineStockIssue[];
  stockBanner?: string | null;
  suppressMidSessionWarning?: boolean;
  workspace?: PosWorkspaceScope | null;
};

function deriveMidSessionBlock(
  explicit: MidSessionBlockProp | undefined,
  checkoutReadiness: CheckoutShiftReadiness | undefined,
): MidSessionBlockProp {
  if (explicit !== undefined) {
    return explicit;
  }
  if (!checkoutReadiness || checkoutReadiness.status === "loading") {
    return "none";
  }
  if (checkoutReadiness.moneyPostReady) {
    return "none";
  }
  if (!checkoutReadiness.shiftGateReady) {
    return "shift_lost";
  }
  return "device_lost";
}

function cartLineInitial(name: string): string {
  const trimmed = name.trim();
  return trimmed ? trimmed.charAt(0).toUpperCase() : "?";
}

function SellCartLineThumb({
  workspace,
  productId,
  hasImage,
  imageVersion,
  name,
}: {
  workspace: PosWorkspaceScope | null;
  productId: string;
  hasImage: boolean;
  imageVersion?: number | null;
  name: string;
}) {
  const imageUrl = useCatalogProductImageUrl(workspace, productId, hasImage, imageVersion);
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

export function SellCartPanel({
  lines,
  lineCount,
  subtotal,
  onIncrement,
  onDecrement,
  onRemove,
  onSetQuantity,
  onEditWeight,
  onEditCustomQuantity,
  onClear,
  showClose = false,
  onClose,
  panelId = "cart",
  checkoutReadiness,
  canCreateSale = false,
  midSessionBlock: midSessionBlockProp,
  stockIssues = [],
  stockBanner = null,
  suppressMidSessionWarning = false,
  workspace = null,
}: SellCartPanelProps) {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [clearConfirmOpen, setClearConfirmOpen] = useState(false);
  const shiftGateReady = checkoutReadiness?.shiftGateReady === true;
  const moneyPostReady = checkoutReadiness?.moneyPostReady === true;
  const midSessionBlock = deriveMidSessionBlock(midSessionBlockProp, checkoutReadiness);
  const showMidSessionWarning =
    !suppressMidSessionWarning && midSessionBlock !== "none" && !moneyPostReady;
  const hasStockIssues = stockIssues.length > 0;
  const stockIssueByLine = new Map(stockIssues.map((issue) => [issue.lineKey, issue]));
  const payEnabled = lines.length > 0 && moneyPostReady && canCreateSale && !hasStockIssues;

  return (
    <div className="sell-cart-panel flex min-h-0 flex-1 flex-col overflow-hidden">
      <div className="sell-cart-panel__header flex shrink-0 items-start justify-between gap-2">
        <div className="sell-cart-panel__title min-w-0">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">{t("sell.cartLabel")}</h2>
          {lines.length > 0 ? (
            <p
              className="sell-cart-panel__meta m-0 text-[length:var(--exits-text-xs)] text-muted"
              data-testid="sell-cart-header-count"
            >
              {lineCount} {lineCount === 1 ? t("sell.cartItemSingular") : t("sell.cartItemPlural")}
            </p>
          ) : null}
        </div>
        <div className="flex shrink-0 items-center gap-0.5">
          {lines.length > 0 ? (
            <Button
              type="button"
              variant="ghost"
              className="min-h-8 px-2 text-[length:var(--exits-text-xs)] text-muted"
              data-testid="sell-cart-clear"
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
              data-testid="sell-cart-sheet-close"
              onClick={onClose}
            >
              {t("sell.cartSheetClose")}
            </Button>
          ) : null}
        </div>
      </div>

      {lines.length === 0 ? (
        <div className="sell-cart-empty flex flex-1 flex-col items-center justify-center gap-1 px-2 py-6 text-center">
          <ShoppingCart className="size-5 text-muted" strokeWidth={1.75} aria-hidden />
          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t("sell.payAddItems")}</p>
        </div>
      ) : (
        <ul className="sell-cart-lines m-0 min-h-0 flex-1 list-none overflow-y-auto p-0">
          {lines.map((line) => {
            const byWeight = isByWeightSellingMode(line.sellingMode);
            const customMeasured = line.allowsCustomQuantity && !byWeight;
            const wholeOnly = !line.allowsCustomQuantity && !byWeight;
            const sellingPrice = effectiveUnitPrice(line);
            const amount = lineAmount(line);
            const hasOverride = Boolean(line.priceOverride);
            const qtyLabel = formatQuantityDisplay(line.quantity);
            const stockIssue = stockIssueByLine.get(line.lineKey);

            return (
              <li
                key={line.lineKey}
                data-testid={`sell-cart-line-${line.lineKey}`}
                data-stock-invalid={stockIssue ? "true" : undefined}
                className="sell-cart-line sell-cart-line--enter"
              >
                <SellCartLineThumb
                  workspace={workspace}
                  productId={line.productId}
                  hasImage={line.hasImage === true}
                  imageVersion={line.imageVersion}
                  name={line.name}
                />
                <div className="sell-cart-line__body">
                <div className="sell-cart-line__top">
                  <p className="sell-cart-line__name">{line.name}</p>
                  <div className="sell-cart-line__price-actions">
                    <MoneyDisplay
                      amount={amount}
                      className={cn("sell-cart-line__amount", hasOverride && "sell-cart-line__price-now")}
                      testId={`sell-cart-amount-${line.lineKey}`}
                    />
                    <Button
                      type="button"
                      variant="ghost"
                      className="sell-cart-line__remove"
                      aria-label={t("sell.cartRemoveLine")}
                      data-testid={`sell-cart-remove-${line.lineKey}`}
                      onClick={() => onRemove(line.lineKey)}
                    >
                      <Trash2 className="size-4" aria-hidden strokeWidth={2} />
                    </Button>
                  </div>
                </div>

                <div className="sell-cart-line__bottom">
                  <span className="sell-cart-line__meta">
                    {line.unitLabel}
                    <span className="sell-cart-line__unit-price">
                      {" "}
                      · {qtyLabel}×₱{sellingPrice.toFixed(2)}
                    </span>
                  </span>

                  {byWeight ? (
                    <Button
                      type="button"
                      variant="ghost"
                      className="sell-cart-line__edit"
                      data-testid={`sell-cart-edit-weight-${line.lineKey}`}
                      onClick={() => onEditWeight(line)}
                    >
                      {qtyLabel} {line.unitLabel}
                    </Button>
                  ) : customMeasured && onEditCustomQuantity ? (
                    <Button
                      type="button"
                      variant="ghost"
                      className="sell-cart-line__edit"
                      data-testid={`sell-cart-edit-custom-${line.lineKey}`}
                      onClick={() => onEditCustomQuantity(line)}
                    >
                      {qtyLabel} {line.unitLabel}
                    </Button>
                  ) : (
                    <div className="sell-cart-line__qty">
                      <QuantityStepper
                        compact
                        value={qtyLabel}
                        valueTestId={`sell-cart-qty-${line.lineKey}`}
                        decreaseLabel={t("sell.cartDecrease")}
                        increaseLabel={t("sell.cartIncrease")}
                        onDecrement={() => onDecrement(line.lineKey)}
                        onIncrement={() => onIncrement(line.lineKey)}
                      />
                      {!wholeOnly ? (
                        <>
                          <label
                            className="sr-only"
                            htmlFor={`${panelId}-sell-qty-input-${line.lineKey}`}
                          >
                            {t("sell.quantityDirect")}
                          </label>
                          <input
                            id={`${panelId}-sell-qty-input-${line.lineKey}`}
                            data-testid={`sell-cart-qty-input-${line.lineKey}`}
                            type="number"
                            inputMode="decimal"
                            min={0.001}
                            step={0.001}
                            value={line.quantity}
                            className="sell-cart-line__qty-input"
                            onChange={(event) => {
                              const next = Number(event.target.value);
                              if (!Number.isFinite(next)) {
                                return;
                              }
                              onSetQuantity(line.lineKey, next);
                            }}
                          />
                        </>
                      ) : null}
                    </div>
                  )}
                </div>

                {hasOverride ? (
                  <p
                    data-testid={`sell-cart-price-changed-${line.lineKey}`}
                    className="sell-cart-line__override"
                  >
                    {t("sell.priceChanged")}
                    {hasOverride ? (
                      <span
                        data-testid={`sell-cart-regular-price-${line.lineKey}`}
                        className="sell-cart-line__price-was"
                      >
                        {" "}
                        (₱{line.unitPrice.toFixed(2)})
                      </span>
                    ) : null}
                  </p>
                ) : null}
                {stockIssue ? (
                  <p
                    role="alert"
                    data-testid={`sell-cart-stock-issue-${line.lineKey}`}
                    className="sell-cart-line__stock"
                  >
                    {stockIssue.message}
                  </p>
                ) : null}
                </div>
              </li>
            );
          })}
        </ul>
      )}

      <div className="sell-cart-footer mt-auto flex shrink-0 flex-col gap-2">
        {showMidSessionWarning ? (
          <div
            data-testid="sell-mid-session-warning"
            data-block={midSessionBlock}
            className="rounded-[var(--exits-radius-md)] border border-[var(--exits-danger)]/40 bg-[var(--exits-surface-muted)] px-2 py-1.5"
            role="alert"
          >
            <p className="m-0 text-[length:var(--exits-text-xs)] font-medium leading-snug">
              {midSessionBlock === "device_lost"
                ? t("sell.midSession.deviceLost")
                : t("sell.midSession.shiftLost")}
            </p>
          </div>
        ) : null}

        {stockBanner || hasStockIssues ? (
          <p
            role="alert"
            data-testid="sell-cart-stock-alert"
            className="m-0 text-[length:var(--exits-text-xs)] text-[var(--exits-danger)]"
          >
            {stockBanner ?? t("sell.payDisabledStock")}
          </p>
        ) : null}

        <div
          className="sell-cart-footer__total-row flex items-baseline justify-between gap-3"
          data-testid="sell-cart-footer-total"
        >
          <span className="sell-cart-footer__total-label text-[length:var(--exits-text-sm)] font-semibold">
            {t("sell.cartTotalLabel")}
          </span>
          <MoneyDisplay
            amount={subtotal}
            className="sell-cart-footer__total-amount text-[length:var(--exits-text-md)] font-bold"
            testId="sell-cart-header-subtotal"
          />
        </div>

        <Button
          data-testid="sell-pay"
          type="button"
          disabled={!payEnabled}
          title={
            payEnabled
              ? t("sell.payReadyTitle")
              : hasStockIssues
                ? t("sell.payDisabledStock")
                : !canCreateSale
                  ? t("sell.payDisabledTitle")
                  : !moneyPostReady
                    ? shiftGateReady
                      ? t("sell.payDisabledNeedsDevice")
                      : t("sell.payDisabledNeedsShift")
                    : t("sell.payDisabledEmpty")
          }
          className={cn(
            "sell-cart-pay w-full",
            payEnabled ? "sell-cart-pay--ready" : "sell-cart-pay--disabled",
          )}
          onClick={() => {
            if (payEnabled) {
              navigate("/sell/checkout");
            }
          }}
        >
          {payEnabled
            ? t("sell.continueToPayment")
            : lineCount > 0
              ? t("sell.payWithItems")
              : t("sell.pay")}
        </Button>
        {!payEnabled ? (
          <p className="sell-cart-footer__hint m-0 text-center text-[length:var(--exits-text-xs)] text-muted">
            {hasStockIssues
              ? t("sell.payDisabledStock")
              : moneyPostReady
                ? t("sell.payAddItems")
                : shiftGateReady
                  ? t("sell.payNeedsDevice")
                  : t("sell.payNotReady")}
          </p>
        ) : null}
        <span className="sr-only" data-testid="sell-cart-subtotal">
          <MoneyDisplay amount={subtotal} />
        </span>
      </div>

      <ConfirmationDialog
        open={clearConfirmOpen}
        title={t("sell.cartClearTitle")}
        detail={t("sell.cartClearDetail")}
        confirmLabel={t("sell.cartClearConfirm")}
        cancelLabel={t("sell.cancel")}
        testId="sell-cart-clear-confirm"
        onCancel={() => setClearConfirmOpen(false)}
        onConfirm={() => {
          onClear();
          setClearConfirmOpen(false);
        }}
      />
    </div>
  );
}
