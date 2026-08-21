import { useState } from "react";
import { Button } from "@/components/ui/button";
import { lineAmount, type SessionCartLine } from "@/cart/SessionCartProvider";
import { formatQuantityDisplay, isByWeightSellingMode } from "@/cart/sell-cart-helpers";
import { ConfirmationDialog } from "@/components/exits/SheetDialog";
import { MoneyDisplay, QuantityStepper } from "@/components/exits/MoneyQuantity";
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
  onClear: () => void;
  showClose?: boolean;
  onClose?: () => void;
  /** Disambiguates duplicate landscape + sheet markup (ids / optional test prefix). */
  panelId?: string;
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
  onClear,
  showClose = false,
  onClose,
  panelId = "cart",
}: SellCartPanelProps) {
  const { t } = useI18n();
  const [clearConfirmOpen, setClearConfirmOpen] = useState(false);
  const summary = formatCartSummary(lineCount, subtotal);

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
                        inputMode="decimal"
                        min={0.001}
                        step={line.multiplierToBase !== 1 ? 1 : 1}
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
        <Button
          data-testid="sell-pay"
          type="button"
          disabled
          title={t("sell.payDisabledTitle")}
          className="w-full"
        >
          {lineCount > 0 ? `${t("sell.payWithItems")} (${lineCount})` : t("sell.pay")}
        </Button>
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t("sell.payNotReady")}</p>
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
