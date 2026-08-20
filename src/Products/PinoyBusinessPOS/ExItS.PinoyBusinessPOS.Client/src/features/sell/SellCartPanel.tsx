import { Button } from "@/components/ui/button";
import type { SessionCartLine } from "@/cart/SessionCartProvider";
import { useI18n } from "@/i18n/I18nProvider";
import { formatCartSummary, formatPeso } from "@/lib/format-money";

type SellCartPanelProps = {
  lines: SessionCartLine[];
  lineCount: number;
  subtotal: number;
  onIncrement: (productId: string) => void;
  onDecrement: (productId: string) => void;
  onRemove: (productId: string) => void;
  showClose?: boolean;
  onClose?: () => void;
};

export function SellCartPanel({
  lines,
  lineCount,
  subtotal,
  onIncrement,
  onDecrement,
  onRemove,
  showClose = false,
  onClose,
}: SellCartPanelProps) {
  const { t } = useI18n();
  const summary = formatCartSummary(lineCount, subtotal);

  return (
    <>
      <div className="flex items-center justify-between gap-3">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
          {t("sell.cartLabel")}
        </h2>
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

      {lines.length === 0 ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{summary}</p>
      ) : (
        <ul className="m-0 flex min-h-0 flex-1 list-none flex-col gap-2 overflow-y-auto p-0">
          {lines.map((line) => (
            <li
              key={line.productId}
              data-testid={`sell-cart-line-${line.productId}`}
              className="rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)] p-3"
            >
              <div className="flex items-start justify-between gap-2">
                <div className="min-w-0">
                  <p className="m-0 truncate text-[length:var(--exits-text-sm)] font-semibold">
                    {line.name}
                  </p>
                  {line.sku ? (
                    <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{line.sku}</p>
                  ) : null}
                  <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                    {formatPeso(line.unitPrice)}
                  </p>
                </div>
                <Button
                  type="button"
                  variant="ghost"
                  aria-label={t("sell.cartRemoveLine")}
                  onClick={() => onRemove(line.productId)}
                >
                  {t("sell.cartRemove")}
                </Button>
              </div>
              <div className="mt-2 flex items-center gap-2">
                <Button
                  type="button"
                  variant="ghost"
                  className="border border-border"
                  aria-label={t("sell.cartDecrease")}
                  onClick={() => onDecrement(line.productId)}
                >
                  −
                </Button>
                <span
                  data-testid={`sell-cart-qty-${line.productId}`}
                  className="min-w-[2rem] text-center text-[length:var(--exits-text-sm)] font-semibold"
                >
                  {line.quantity}
                </span>
                <Button
                  type="button"
                  variant="ghost"
                  className="border border-border"
                  aria-label={t("sell.cartIncrease")}
                  onClick={() => onIncrement(line.productId)}
                >
                  +
                </Button>
                <span className="ml-auto text-[length:var(--exits-text-sm)] font-semibold">
                  {formatPeso(line.unitPrice * line.quantity)}
                </span>
              </div>
            </li>
          ))}
        </ul>
      )}

      <div className="mt-auto flex flex-col gap-2">
        {lines.length > 0 ? (
          <p
            data-testid="sell-cart-subtotal"
            className="m-0 text-[length:var(--exits-text-sm)] font-semibold"
          >
            {t("sell.cartSubtotalLabel")}: {formatPeso(subtotal)}
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
      </div>
    </>
  );
}
