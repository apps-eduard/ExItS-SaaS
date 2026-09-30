import { useState } from "react";
import { AlertTriangle, Trash2, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { ExitsDataRecordCard } from "@/components/exits/ExitsDataRecordCard";
import { ExitsResponsiveDataView } from "@/components/exits/ExitsResponsiveDataView";
import {
  ExitsTable,
  ExitsTableActions,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableRow,
} from "@/components/exits/ExitsTable";
import { Notice } from "@/components/exits/Notice";
import type { ResponsiveDataLayout } from "@/components/exits/responsive-data-view";
import { MoneyDisplay, QuantityStepper } from "@/components/exits/MoneyQuantity";
import { receiveCostMarginKind } from "@/features/purchasing/receive-cost-margin";
import {
  formatMoneyInput,
  parseMoneyInput,
  roundMoney,
} from "@/features/purchasing/receive-payment";
import { cn } from "@/lib/cn";
import { formatPeso } from "@/lib/format-money";
import { normalizeMoneyAmountTyping } from "@/lib/money-input";

export type ReceiveStockReceiptLine = {
  productId: string;
  name: string;
  sku: string | null;
  uom: string;
  sellingMode: string;
  quantity: number;
  unitCost: number;
  costInput: string;
  effectiveSellingPrice: number;
  tracksExpiration: boolean;
  expiryDate: string;
  lotNumber: string;
};

type Translate = (key: string) => string;

type ReceiveStockReceiptItemsViewProps = {
  layout: ResponsiveDataLayout;
  lines: readonly ReceiveStockReceiptLine[];
  highlightProductId: string | null;
  onPatchLine: (
    productId: string,
    patch: Partial<
      Pick<
        ReceiveStockReceiptLine,
        "quantity" | "costInput" | "expiryDate" | "lotNumber"
      >
    >,
  ) => void;
  /**
   * When set, intercepts non-tracked expiry date selection so the parent can open
   * in-place existing-stock setup (onHand > 0) before applying the date.
   */
  onExpiryDateAttempt?: (productId: string, nextDate: string) => void;
  onRemoveLine: (productId: string) => void;
  t: Translate;
};

function ReceiptQtyStepper({
  line,
  onPatchLine,
  t,
}: {
  line: ReceiveStockReceiptLine;
  onPatchLine: ReceiveStockReceiptItemsViewProps["onPatchLine"];
  t: Translate;
}) {
  return (
    <QuantityStepper
      compact
      value={line.quantity}
      onChange={(next) => onPatchLine(line.productId, { quantity: next })}
      unitOfMeasure={line.uom}
      sellingMode={line.sellingMode}
      unit={line.uom?.trim() || undefined}
      invalid={!(line.quantity > 0)}
      decreaseLabel={t("purchasing.decreaseQty")}
      increaseLabel={t("purchasing.increaseQty")}
      ariaLabel={t("purchasing.qtyShort")}
      valueTestId={`direct-line-qty-${line.productId}`}
      className="receive-qty-stepper"
    />
  );
}

function ReceiptCostInput({
  line,
  onPatchLine,
  t,
}: {
  line: ReceiveStockReceiptLine;
  onPatchLine: ReceiveStockReceiptItemsViewProps["onPatchLine"];
  t: Translate;
}) {
  const costInvalid = !(line.unitCost > 0);
  return (
    <div className="exits-currency-field receive-stock-cost-field">
      <span className="exits-currency-field__prefix" aria-hidden>
        ₱
      </span>
      <input
        className="exits-currency-field__input receive-cost-input tabular-nums"
        value={line.costInput}
        onChange={(e) =>
          onPatchLine(line.productId, {
            costInput: normalizeMoneyAmountTyping(e.target.value),
          })
        }
        onBlur={(e) => {
          const parsed = parseMoneyInput(e.target.value);
          if (parsed !== null) {
            onPatchLine(line.productId, {
              costInput: formatMoneyInput(parsed),
            });
          }
        }}
        inputMode="decimal"
        placeholder="0.00"
        autoComplete="off"
        aria-invalid={costInvalid || undefined}
        aria-required
        aria-label={t("purchasing.costShort")}
        data-testid={`direct-line-cost-${line.productId}`}
      />
    </div>
  );
}

function ReceiptExpiryDateInput({
  line,
  onPatchLine,
  onExpiryDateAttempt,
  t,
}: {
  line: ReceiveStockReceiptLine;
  onPatchLine: ReceiveStockReceiptItemsViewProps["onPatchLine"];
  onExpiryDateAttempt?: ReceiveStockReceiptItemsViewProps["onExpiryDateAttempt"];
  t: Translate;
}) {
  const expiryRequired = line.tracksExpiration;
  const expiryInvalid = expiryRequired && !line.expiryDate.trim();
  const hasValue = Boolean(line.expiryDate.trim());

  function applyExpiry(nextDate: string) {
    const trimmed = nextDate.trim();
    const looksComplete = /^\d{4}-\d{2}-\d{2}$/.test(trimmed);
    if (onExpiryDateAttempt && !line.tracksExpiration && looksComplete) {
      onExpiryDateAttempt(line.productId, trimmed);
      return;
    }
    onPatchLine(line.productId, { expiryDate: nextDate });
  }

  return (
    <div className="receive-stock-expiry-field">
      <div className="receive-stock-expiry-field__control">
        <input
          type="date"
          className="exits-input receive-stock-expiry-input"
          value={line.expiryDate}
          onChange={(e) => applyExpiry(e.target.value)}
          aria-invalid={expiryInvalid || undefined}
          aria-required={expiryRequired || undefined}
          aria-label={t("purchasing.expiryDate")}
          data-testid={`direct-line-expiry-${line.productId}`}
        />
        {hasValue ? (
          <Button
            type="button"
            intent="danger"
            appearance="solid"
            emphasis="soft"
            shape="pill"
            size="icon"
            className="receive-stock-expiry-field__clear"
            aria-label={t("purchasing.clearExpiryDate")}
            data-testid={`direct-line-expiry-clear-${line.productId}`}
            onClick={() => onPatchLine(line.productId, { expiryDate: "" })}
          >
            <X className="size-3.5" aria-hidden strokeWidth={2.75} />
          </Button>
        ) : null}
      </div>
    </div>
  );
}

function ExpiryRequiredNotice({ t }: { t: Translate }) {
  const [dismissed, setDismissed] = useState(false);
  if (dismissed) return null;

  return (
    <Notice
      tone="info"
      testId="direct-receipt-expiry-required-notice"
      className="receive-stock-expiry-required-notice"
      action={
        <button
          type="button"
          className="receive-stock-expiry-required-notice__close"
          aria-label={t("shell.needsAttention.close")}
          data-testid="direct-receipt-expiry-required-notice-close"
          onClick={() => setDismissed(true)}
        >
          <X className="size-4" aria-hidden strokeWidth={2} />
        </button>
      }
    >
      {t("purchasing.expiryRequired")}
    </Notice>
  );
}

function ExpiryEnableTrackingNotice({
  t,
  onClearDates,
}: {
  t: Translate;
  onClearDates: () => void;
}) {
  const [dismissed, setDismissed] = useState(false);
  if (dismissed) return null;

  return (
    <Notice
      tone="info"
      testId="direct-receipt-expiry-hint"
      className="receive-stock-expiry-enable-notice"
      action={
        <div className="receive-stock-expiry-enable-notice__actions">
          <button
            type="button"
            className="receive-stock-expiry-enable-notice__clear-date"
            data-testid="direct-receipt-expiry-hint-clear"
            onClick={onClearDates}
          >
            {t("purchasing.clearExpiryDate")}
          </button>
          <button
            type="button"
            className="receive-stock-expiry-enable-notice__close"
            aria-label={t("shell.needsAttention.close")}
            data-testid="direct-receipt-expiry-hint-close"
            onClick={() => setDismissed(true)}
          >
            <X className="size-4" aria-hidden strokeWidth={2} />
          </button>
        </div>
      }
    >
      {t("purchasing.expiryEnableTrackingHint")}
    </Notice>
  );
}

function ReceiptSellingCell({
  line,
  t,
}: {
  line: ReceiveStockReceiptLine;
  t: Translate;
}) {
  const marginKind = receiveCostMarginKind(line.unitCost, line.effectiveSellingPrice);
  const marginLabel =
    marginKind === "zeroMargin"
      ? t("purchasing.costZeroMarginWarning")
      : marginKind === "negativeMargin"
        ? t("purchasing.costNegativeMarginWarning")
        : null;
  return (
    <div className="receive-stock-selling-cell">
      <MoneyDisplay
        amount={line.effectiveSellingPrice > 0 ? line.effectiveSellingPrice : 0}
        className="receive-stock-selling-readonly"
        testId={`direct-line-selling-${line.productId}`}
      />
      {marginLabel ? (
        <span
          className="receive-stock-margin-warning"
          title={marginLabel}
          role="img"
          aria-label={marginLabel}
          data-testid={`direct-line-margin-warning-${line.productId}`}
          data-margin={marginKind}
        >
          <AlertTriangle aria-hidden strokeWidth={2} />
        </span>
      ) : null}
    </div>
  );
}

function ReceiptExpiryLotDetails({
  line,
  onPatchLine,
  onExpiryDateAttempt,
  t,
}: {
  line: ReceiveStockReceiptLine;
  onPatchLine: ReceiveStockReceiptItemsViewProps["onPatchLine"];
  onExpiryDateAttempt?: ReceiveStockReceiptItemsViewProps["onExpiryDateAttempt"];
  t: Translate;
}) {
  return (
    <div className="receive-stock-receipt-card__details">
      <label className="receive-stock-field">
        <span className="receive-stock-field__label">{t("purchasing.expiryDate")}</span>
        <ReceiptExpiryDateInput
          line={line}
          onPatchLine={onPatchLine}
          onExpiryDateAttempt={onExpiryDateAttempt}
          t={t}
        />
      </label>
      <label className="receive-stock-field">
        <span className="receive-stock-field__label">{t("purchasing.lotNumber")}</span>
        <input
          className="exits-input receive-stock-lot-input"
          value={line.lotNumber}
          onChange={(e) =>
            onPatchLine(line.productId, {
              lotNumber: e.target.value,
            })
          }
          maxLength={64}
          placeholder={t("purchasing.lotNumberOptional")}
          aria-label={t("purchasing.lotNumber")}
          data-testid={`direct-line-lot-${line.productId}`}
        />
      </label>
    </div>
  );
}

function RemoveLineButton({
  line,
  onRemoveLine,
  t,
}: {
  line: ReceiveStockReceiptLine;
  onRemoveLine: (productId: string) => void;
  t: Translate;
}) {
  return (
    <Button
      type="button"
      variant="destructive"
      size="icon"
      aria-label={t("purchasing.removeNamed").replace("{name}", line.name)}
      onClick={() => onRemoveLine(line.productId)}
      data-testid={`direct-remove-${line.productId}`}
    >
      <Trash2 className="size-4" aria-hidden />
    </Button>
  );
}

/**
 * Receipt lines — one ExitsResponsiveDataView (TABLE desktop / LIST cards narrow).
 * Same draft-line model and controls in both modes.
 * Only the active layout body mounts (avoids duplicate test ids / double inputs).
 */
export function ReceiveStockReceiptItemsView({
  layout,
  lines,
  highlightProductId,
  onPatchLine,
  onExpiryDateAttempt,
  onRemoveLine,
  t,
}: ReceiveStockReceiptItemsViewProps) {
  const showExpiryEnableNotice = lines.some(
    (line) => !line.tracksExpiration && Boolean(line.expiryDate.trim()),
  );
  const showExpiryRequiredNotice = lines.some(
    (line) => line.tracksExpiration && !line.expiryDate.trim(),
  );

  const tableBody = (
        <ExitsTableContainer className="receive-stock-receipt-table">
          <ExitsTable>
            <ExitsTableHeader>
              <ExitsTableRow>
                <ExitsTableHead cellAlign="text">
                  {t("purchasing.receiveProduct")}
                </ExitsTableHead>
                <ExitsTableHead cellAlign="center">{t("purchasing.qtyShort")}</ExitsTableHead>
                <ExitsTableHead cellAlign="text">{t("purchasing.costShort")}</ExitsTableHead>
                <ExitsTableHead cellAlign="text">
                  {t("purchasing.sellingPriceShort")}
                </ExitsTableHead>
                <ExitsTableHead
                  cellAlign="text"
                  className="receive-stock-receipt-table__expiry-col"
                >
                  {t("purchasing.expiryDate")}
                </ExitsTableHead>
                <ExitsTableHead cellAlign="text">{t("purchasing.lotNumber")}</ExitsTableHead>
                <ExitsTableHead cellAlign="numeric">{t("purchasing.lineTotal")}</ExitsTableHead>
                <ExitsTableHead
                  cellAlign="center"
                  colSize="actions"
                  className="receive-stock-receipt-table__action-col"
                >
                  {t("purchasing.action")}
                </ExitsTableHead>
              </ExitsTableRow>
            </ExitsTableHeader>
            <ExitsTableBody>
              {lines.map((line) => {
                const lineTotal = roundMoney(line.quantity * line.unitCost);
                return (
                  <ExitsTableRow
                    key={line.productId}
                    className={cn(
                      highlightProductId === line.productId &&
                        "receive-stock-receipt-row--highlight",
                    )}
                    data-testid={`direct-receipt-line-${line.productId}`}
                  >
                    <ExitsTableCell cellAlign="text">
                      <div className="font-medium leading-snug">{line.name}</div>
                      {line.sku ? (
                        <div className="receive-stock-receipt-product__sku">{line.sku}</div>
                      ) : null}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="center">
                      <ReceiptQtyStepper line={line} onPatchLine={onPatchLine} t={t} />
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="text">
                      <ReceiptCostInput line={line} onPatchLine={onPatchLine} t={t} />
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="text">
                      <ReceiptSellingCell line={line} t={t} />
                    </ExitsTableCell>
                    <ExitsTableCell
                      cellAlign="text"
                      className="receive-stock-receipt-table__expiry-col"
                    >
                      <ReceiptExpiryDateInput
                        line={line}
                        onPatchLine={onPatchLine}
                        onExpiryDateAttempt={onExpiryDateAttempt}
                        t={t}
                      />
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="text">
                      <input
                        className="exits-input receive-stock-lot-input"
                        value={line.lotNumber}
                        onChange={(e) =>
                          onPatchLine(line.productId, {
                            lotNumber: e.target.value,
                          })
                        }
                        maxLength={64}
                        placeholder={t("purchasing.lotNumberOptional")}
                        aria-label={t("purchasing.lotNumber")}
                        data-testid={`direct-line-lot-${line.productId}`}
                      />
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="numeric">
                      <span className="font-semibold tabular-nums">{formatPeso(lineTotal)}</span>
                    </ExitsTableCell>
                    <ExitsTableCell
                      cellAlign="center"
                      colSize="actions"
                      className="receive-stock-receipt-table__action-col"
                    >
                      <ExitsTableActions className="justify-center">
                        <RemoveLineButton line={line} onRemoveLine={onRemoveLine} t={t} />
                      </ExitsTableActions>
                    </ExitsTableCell>
                  </ExitsTableRow>
                );
              })}
            </ExitsTableBody>
          </ExitsTable>
        </ExitsTableContainer>
  );

  const listBody = (
        <ul className="exits-data-record-list receive-stock-receipt-list">
          {lines.map((line) => {
            const lineTotal = roundMoney(line.quantity * line.unitCost);
            return (
              <ExitsDataRecordCard
                key={line.productId}
                as="li"
                className={cn(
                  highlightProductId === line.productId &&
                    "receive-stock-receipt-row--highlight",
                )}
                data-testid={`direct-receipt-line-${line.productId}`}
                title={line.name}
                subtitle={line.sku || undefined}
                primaryAction={
                  <RemoveLineButton line={line} onRemoveLine={onRemoveLine} t={t} />
                }
                fields={[
                  {
                    label: t("purchasing.qtyShort"),
                    value: <ReceiptQtyStepper line={line} onPatchLine={onPatchLine} t={t} />,
                  },
                  {
                    label: t("purchasing.costShort"),
                    value: <ReceiptCostInput line={line} onPatchLine={onPatchLine} t={t} />,
                  },
                  {
                    label: t("purchasing.sellingPriceShort"),
                    value: <ReceiptSellingCell line={line} t={t} />,
                  },
                  {
                    label: t("purchasing.lineTotal"),
                    value: (
                      <span className="font-semibold tabular-nums">{formatPeso(lineTotal)}</span>
                    ),
                    emphasize: true,
                  },
                ]}
                details={
                  <ReceiptExpiryLotDetails
                    line={line}
                    onPatchLine={onPatchLine}
                    onExpiryDateAttempt={onExpiryDateAttempt}
                    t={t}
                  />
                }
              />
            );
          })}
        </ul>
  );

  const clearEnableTrackingExpiryDates = () => {
    for (const line of lines) {
      if (!line.tracksExpiration && line.expiryDate.trim()) {
        onPatchLine(line.productId, { expiryDate: "" });
      }
    }
  };

  return (
    <div className="receive-stock-receipt-items">
      {showExpiryRequiredNotice ? <ExpiryRequiredNotice t={t} /> : null}
      {showExpiryEnableNotice ? (
        <ExpiryEnableTrackingNotice t={t} onClearDates={clearEnableTrackingExpiryDates} />
      ) : null}
      <ExitsResponsiveDataView
        layout={layout}
        testId="direct-receipt-table"
        className="receive-stock-receipt-responsive"
        table={layout === "table" ? tableBody : null}
        list={layout === "list" ? listBody : null}
      />
    </div>
  );
}
