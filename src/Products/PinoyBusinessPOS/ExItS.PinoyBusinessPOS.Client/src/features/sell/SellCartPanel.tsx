import { useState } from "react";
import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { lineAmount, type SessionCartLine } from "@/cart/SessionCartProvider";
import { formatQuantityDisplay, isByWeightSellingMode } from "@/cart/sell-cart-helpers";
import { ConfirmationDialog } from "@/components/exits/SheetDialog";
import { MoneyDisplay, QuantityStepper } from "@/components/exits/MoneyQuantity";
import type { CheckoutShiftReadiness } from "@/features/shifts/checkout-readiness";
import { useI18n } from "@/i18n/I18nProvider";
import { formatCartSummary } from "@/lib/format-money";

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
  onClear: () => void;
  showClose?: boolean;
  onClose?: () => void;
  /** Disambiguates duplicate landscape + sheet markup (ids / optional test prefix). */
  panelId?: string;
  checkoutReadiness?: CheckoutShiftReadiness;
};

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
}: SellCartPanelProps) {
  const { t } = useI18n();
  const [clearConfirmOpen, setClearConfirmOpen] = useState(false);
  const summary = formatCartSummary(lineCount, subtotal);
  const shiftGateReady = checkoutReadiness?.shiftGateReady === true;
  const readinessStatus = checkoutReadiness?.status ?? "loading";

  return (
    <>
      <div className="flex items-center justify-between gap-3">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
          {t("sell.cartLabel")}
        </h2>
        <div className="flex shrink-0 items-center gap-1">
          {lines.length > 0 ? (
            <Button
              type="button"
              variant="ghost"
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
        <ul className="m-0 flex min-h-0 flex-1 list-none flex-col gap-2 overflow-y-auto p-0">
          {lines.map((line) => {
            const byWeight = isByWeightSellingMode(line.sellingMode);
            const customMeasured = line.allowsCustomQuantity && !byWeight;
            const wholeOnly = !line.allowsCustomQuantity && !byWeight;
            const amount = lineAmount(line);
            return (
              <li
                key={line.lineKey}
                data-testid={`sell-cart-line-${line.lineKey}`}
                className="rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)] p-3"
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0">
                    <p className="m-0 truncate text-[length:var(--exits-text-sm)] font-semibold">
                      {line.name}
                    </p>
                    {line.sku ? (
                      <p className="m-0 truncate text-[length:var(--exits-text-xs)] text-muted">
                        {line.sku}
                      </p>
                    ) : null}
                    <p className="m-0 break-words text-[length:var(--exits-text-xs)] text-muted">
                      {t("sell.linePreview")
                        .replace("{qty}", formatQuantityDisplay(line.quantity))
                        .replace("{unit}", line.unitLabel)
                        .replace("{price}", line.unitPrice.toFixed(2))
                        .replace("{amount}", amount.toFixed(2))}
                    </p>
                  </div>
                  <Button
                    type="button"
                    variant="ghost"
                    aria-label={t("sell.cartRemoveLine")}
                    data-testid={`sell-cart-remove-${line.lineKey}`}
                    onClick={() => onRemove(line.lineKey)}
                  >
                    {t("sell.cartRemove")}
                  </Button>
                </div>
                <div className="mt-2 flex flex-wrap items-center justify-between gap-2">
                  {byWeight ? (
                    <Button
                      type="button"
                      variant="ghost"
                      className="border border-border"
                      data-testid={`sell-cart-edit-weight-${line.lineKey}`}
                      onClick={() => onEditWeight(line)}
                    >
                      {formatQuantityDisplay(line.quantity)} {line.unitLabel} ·{" "}
                      {t("sell.editWeight")}
                    </Button>
                  ) : customMeasured && onEditCustomQuantity ? (
                    <Button
                      type="button"
                      variant="ghost"
                      className="border border-border"
                      data-testid={`sell-cart-edit-custom-${line.lineKey}`}
                      onClick={() => onEditCustomQuantity(line)}
                    >
                      {formatQuantityDisplay(line.quantity)} {line.unitLabel} ·{" "}
                      {t("sell.editCustomQty")}
                    </Button>
                  ) : (
                    <div className="flex flex-wrap items-center gap-2">
                      <QuantityStepper
                        value={formatQuantityDisplay(line.quantity)}
                        valueTestId={`sell-cart-qty-${line.lineKey}`}
                        decreaseLabel={t("sell.cartDecrease")}
                        increaseLabel={t("sell.cartIncrease")}
                        onDecrement={() => onDecrement(line.lineKey)}
                        onIncrement={() => onIncrement(line.lineKey)}
                      />
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
                        inputMode={wholeOnly ? "numeric" : "decimal"}
                        min={wholeOnly ? 1 : 0.001}
                        step={wholeOnly ? 1 : 0.001}
                        value={line.quantity}
                        className="min-h-11 w-20 rounded-[var(--exits-radius-md)] border border-border bg-surface px-2 tabular-nums"
                        onChange={(event) => {
                          const next = Number(event.target.value);
                          if (!Number.isFinite(next)) {
                            return;
                          }
                          onSetQuantity(line.lineKey, next);
                        }}
                      />
                    </div>
                  )}
                  <MoneyDisplay
                    amount={amount}
                    className="max-w-[10rem] truncate"
                    testId={`sell-cart-amount-${line.lineKey}`}
                  />
                </div>
              </li>
            );
          })}
        </ul>
      )}

      <div className="mt-auto flex flex-col gap-2">
        {lines.length > 0 ? (
          <p
            data-testid="sell-cart-subtotal"
            className="m-0 break-words text-[length:var(--exits-text-sm)] font-semibold"
          >
            {t("sell.cartSubtotalLabel")}: <MoneyDisplay amount={subtotal} />
          </p>
        ) : null}
        <div
          data-testid="checkout-readiness"
          data-readiness={readinessStatus}
          className="rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)] p-3"
        >
          <p className="m-0 text-[length:var(--exits-text-xs)] font-semibold">
            {t("sell.checkoutReadinessLabel")}
          </p>
          <p
            className="mb-0 mt-1 text-[length:var(--exits-text-xs)] text-muted"
            data-testid="checkout-readiness-detail"
          >
            {shiftGateReady
              ? t("sell.checkoutReadinessReady")
              : readinessStatus === "blocked_denied"
                ? t("sell.checkoutReadinessDenied")
                : readinessStatus === "blocked_closed"
                  ? t("sell.checkoutReadinessClosed")
                  : readinessStatus === "blocked_no_register"
                    ? t("sell.checkoutReadinessNoRegister")
                    : readinessStatus === "loading"
                      ? t("loading.label")
                      : t("sell.checkoutReadinessBlocked")}
          </p>
          {!shiftGateReady && readinessStatus !== "loading" && readinessStatus !== "blocked_denied" ? (
            <Button asChild variant="ghost" className="mt-2 min-h-11 w-full" data-testid="sell-open-shift-cta">
              <Link to="/shifts/open">{t("shift.openTitle")}</Link>
            </Button>
          ) : null}
          {shiftGateReady ? (
            <Button asChild variant="ghost" className="mt-2 min-h-11 w-full" data-testid="sell-view-shift-cta">
              <Link to="/shifts">{t("shift.hubTitle")}</Link>
            </Button>
          ) : null}
        </div>
        <Button
          data-testid="sell-pay"
          type="button"
          disabled
          title={
            shiftGateReady ? t("sell.payAwaitingSalePost") : t("sell.payDisabledNeedsShift")
          }
          className="w-full"
        >
          {lineCount > 0 ? `${t("sell.payWithItems")} (${lineCount})` : t("sell.pay")}
        </Button>
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
          {shiftGateReady ? t("sell.payShiftReadyNotPosted") : t("sell.payNotReady")}
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
    </>
  );
}
