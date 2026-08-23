import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { effectiveUnitPrice, lineAmount, type SessionCartLine } from "@/cart/SessionCartProvider";
import { formatQuantityDisplay, isByWeightSellingMode } from "@/cart/sell-cart-helpers";
import { ConfirmationDialog } from "@/components/exits/SheetDialog";
import { MoneyDisplay, QuantityStepper } from "@/components/exits/MoneyQuantity";
import type { CheckoutShiftReadiness } from "@/features/shifts/checkout-readiness";
import type { MidSessionSellBlock } from "@/features/sell/sell-readiness";
import { useI18n } from "@/i18n/I18nProvider";
import { formatCartSummary } from "@/lib/format-money";

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
  /** Disambiguates duplicate landscape + sheet markup (ids / optional test prefix). */
  panelId?: string;
  checkoutReadiness?: CheckoutShiftReadiness;
  /** CreateSale capability — required with moneyPostReady to enable Pay. */
  canCreateSale?: boolean;
  canOverrideSalePrice?: boolean;
  /**
   * Compact mid-session warning after Sell opened.
   * Prefer explicit value from evaluateMidSessionSellBlock; falls back from readiness.
   */
  midSessionBlock?: MidSessionBlockProp;
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

function CartLineThumbnail({ name }: { name: string }) {
  return (
    <div
      className="sell-cart-line__thumb"
      aria-hidden
    >
      <span className="sell-cart-line__thumb-letter">{cartLineInitial(name)}</span>
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
}: SellCartPanelProps) {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [clearConfirmOpen, setClearConfirmOpen] = useState(false);
  const summary = formatCartSummary(lineCount, subtotal);
  const shiftGateReady = checkoutReadiness?.shiftGateReady === true;
  const moneyPostReady = checkoutReadiness?.moneyPostReady === true;
  const midSessionBlock = deriveMidSessionBlock(midSessionBlockProp, checkoutReadiness);
  const showMidSessionWarning = midSessionBlock !== "none" && !moneyPostReady;
  const payEnabled = lines.length > 0 && moneyPostReady && canCreateSale;

  return (
    <div className="sell-cart-panel flex min-h-0 flex-1 flex-col gap-3">
      <div className="flex items-center justify-between gap-2">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
          {t("sell.cartLabel")}
        </h2>
        <div className="flex shrink-0 items-center gap-1">
          {lines.length > 0 ? (
            <Button
              type="button"
              variant="ghost"
              className="min-h-9 px-2 text-[length:var(--exits-text-xs)] text-muted"
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
              className="min-h-9 px-2"
              aria-label={t("sell.cartSheetClose")}
              onClick={onClose}
            >
              {t("sell.cartSheetClose")}
            </Button>
          ) : null}
        </div>
      </div>

      {lines.length === 0 ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{summary}</p>
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

            return (
              <li
                key={line.lineKey}
                data-testid={`sell-cart-line-${line.lineKey}`}
                className="sell-cart-line"
              >
                <CartLineThumbnail name={line.name} />

                <div className="sell-cart-line__body">
                  <div className="sell-cart-line__head">
                    <div className="min-w-0 flex-1">
                      <p className="sell-cart-line__name">{line.name}</p>
                      <p className="sell-cart-line__meta">
                        {line.unitLabel}
                        {line.sku ? ` · ${line.sku}` : ""}
                      </p>
                      {hasOverride ? (
                        <p
                          data-testid={`sell-cart-price-changed-${line.lineKey}`}
                          className="sell-cart-line__override"
                        >
                          {t("sell.priceChanged")}
                        </p>
                      ) : null}
                    </div>
                    <Button
                      type="button"
                      variant="ghost"
                      className="sell-cart-line__remove"
                      aria-label={t("sell.cartRemoveLine")}
                      data-testid={`sell-cart-remove-${line.lineKey}`}
                      onClick={() => onRemove(line.lineKey)}
                    >
                      <X className="size-4" aria-hidden />
                    </Button>
                  </div>

                  <div className="sell-cart-line__controls">
                    {byWeight ? (
                      <Button
                        type="button"
                        variant="ghost"
                        className="sell-cart-line__edit"
                        data-testid={`sell-cart-edit-weight-${line.lineKey}`}
                        onClick={() => onEditWeight(line)}
                      >
                        {qtyLabel} {line.unitLabel} · {t("sell.editWeight")}
                      </Button>
                    ) : customMeasured && onEditCustomQuantity ? (
                      <Button
                        type="button"
                        variant="ghost"
                        className="sell-cart-line__edit"
                        data-testid={`sell-cart-edit-custom-${line.lineKey}`}
                        onClick={() => onEditCustomQuantity(line)}
                      >
                        {qtyLabel} {line.unitLabel} · {t("sell.editCustomQty")}
                      </Button>
                    ) : (
                      <div className="flex min-w-0 flex-wrap items-center gap-1.5">
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

                    <div className="sell-cart-line__price">
                      {hasOverride ? (
                        <span
                          data-testid={`sell-cart-regular-price-${line.lineKey}`}
                          className="sell-cart-line__price-was"
                        >
                          {qtyLabel} × ₱{line.unitPrice.toFixed(2)}
                        </span>
                      ) : null}
                      <MoneyDisplay
                        amount={amount}
                        className={hasOverride ? "sell-cart-line__price-now" : undefined}
                        testId={`sell-cart-amount-${line.lineKey}`}
                      />
                      <span className="sell-cart-line__price-unit">
                        {qtyLabel} × ₱{sellingPrice.toFixed(2)}
                      </span>
                    </div>
                  </div>
                </div>
              </li>
            );
          })}
        </ul>
      )}

      <div className="sell-cart-footer mt-auto flex flex-col gap-3">
        {lines.length > 0 ? (
          <dl className="sell-cart-summary m-0">
            <div className="sell-cart-summary__row">
              <dt>{t("sell.cartItemsLabel")}</dt>
              <dd data-testid="sell-cart-item-count">
                {lineCount} {lineCount === 1 ? t("sell.cartItemSingular") : t("sell.cartItemPlural")}
              </dd>
            </div>
            <div className="sell-cart-summary__row">
              <dt>{t("sell.cartSubtotalLabel")}</dt>
              <dd data-testid="sell-cart-subtotal">
                <MoneyDisplay amount={subtotal} />
              </dd>
            </div>
            <div className="sell-cart-summary__row sell-cart-summary__row--total">
              <dt>{t("sell.cartTotalLabel")}</dt>
              <dd>
                <MoneyDisplay amount={subtotal} />
              </dd>
            </div>
          </dl>
        ) : null}

        {showMidSessionWarning ? (
          <div
            data-testid="sell-mid-session-warning"
            data-block={midSessionBlock}
            className="rounded-[var(--exits-radius-md)] border border-[var(--exits-danger)]/40 bg-[var(--exits-surface-muted)] p-2"
            role="alert"
          >
            <p className="m-0 text-[length:var(--exits-text-xs)] font-semibold leading-snug">
              {midSessionBlock === "device_lost"
                ? t("sell.midSession.deviceLost")
                : t("sell.midSession.shiftLost")}
            </p>
            {midSessionBlock === "device_lost" ? (
              <Button
                asChild
                variant="ghost"
                className="mt-1.5 min-h-9 w-full text-[length:var(--exits-text-xs)]"
                data-testid="sell-mid-session-register"
              >
                <Link to="/devices/register?from=sell">{t("sell.midSession.fixDevice")}</Link>
              </Button>
            ) : (
              <Button
                asChild
                variant="ghost"
                className="mt-1.5 min-h-9 w-full text-[length:var(--exits-text-xs)]"
                data-testid="sell-mid-session-open-shift"
              >
                <Link to="/shifts/open?from=sell">{t("sell.midSession.openShift")}</Link>
              </Button>
            )}
          </div>
        ) : null}

        <Button
          data-testid="sell-pay"
          type="button"
          disabled={!payEnabled}
          title={
            payEnabled
              ? t("sell.payReadyTitle")
              : !canCreateSale
                ? t("sell.payDisabledTitle")
                : !moneyPostReady
                  ? shiftGateReady
                    ? t("sell.payDisabledNeedsDevice")
                    : t("sell.payDisabledNeedsShift")
                  : t("sell.payDisabledEmpty")
          }
          className="sell-cart-pay w-full"
          onClick={() => {
            if (payEnabled) {
              navigate("/sell/checkout");
            }
          }}
        >
          {lineCount > 0 ? `${t("sell.payWithItems")} (${lineCount})` : t("sell.pay")}
        </Button>
        <p className="m-0 text-center text-[length:var(--exits-text-xs)] text-muted">
          {payEnabled
            ? t("sell.payReadyHint")
            : moneyPostReady
              ? t("sell.payAddItems")
              : shiftGateReady
                ? t("sell.payNeedsDevice")
                : t("sell.payNotReady")}
        </p>
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
