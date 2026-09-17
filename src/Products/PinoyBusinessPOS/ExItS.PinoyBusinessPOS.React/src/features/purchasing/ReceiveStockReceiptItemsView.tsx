import { AlertTriangle, Trash2 } from "lucide-react";
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
import type { ResponsiveDataLayout } from "@/components/exits/responsive-data-view";
import { QuantityStepper } from "@/components/exits/MoneyQuantity";
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
      unit={line.uom}
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
    <input
      className="exits-input receive-cost-input tabular-nums"
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
  const sellingDisplay =
    line.effectiveSellingPrice > 0
      ? formatMoneyInput(line.effectiveSellingPrice)
      : "0.00";
  return (
    <div className="receive-stock-selling-cell">
      <span
        className="receive-stock-selling-readonly tabular-nums"
        aria-label={t("purchasing.sellingPriceShort")}
        data-testid={`direct-line-selling-${line.productId}`}
      >
        {sellingDisplay}
      </span>
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
  t,
}: {
  line: ReceiveStockReceiptLine;
  onPatchLine: ReceiveStockReceiptItemsViewProps["onPatchLine"];
  t: Translate;
}) {
  if (!line.tracksExpiration) {
    return null;
  }
  const expiryInvalid = !line.expiryDate.trim();
  return (
    <div className="receive-stock-receipt-card__details">
      <label className="receive-stock-field">
        <span className="receive-stock-field__label">{t("purchasing.expiryDate")}</span>
        <input
          type="date"
          className="exits-input receive-stock-expiry-input"
          value={line.expiryDate}
          onChange={(e) =>
            onPatchLine(line.productId, {
              expiryDate: e.target.value,
            })
          }
          aria-invalid={expiryInvalid || undefined}
          aria-required
          aria-label={t("purchasing.expiryDate")}
          data-testid={`direct-line-expiry-${line.productId}`}
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
  onRemoveLine,
  t,
}: ReceiveStockReceiptItemsViewProps) {
  const tableBody = (
        <ExitsTableContainer className="receive-stock-receipt-table">
          <ExitsTable>
            <ExitsTableHeader>
              <ExitsTableRow>
                <ExitsTableHead cellAlign="text">
                  {t("purchasing.receiveProduct")}
                </ExitsTableHead>
                <ExitsTableHead cellAlign="text">{t("purchasing.qtyShort")}</ExitsTableHead>
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
                const expiryInvalid = line.tracksExpiration && !line.expiryDate.trim();
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
                        <div className="text-[length:var(--exits-text-xs)] text-muted">
                          {line.sku}
                        </div>
                      ) : null}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="text">
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
                      {line.tracksExpiration ? (
                        <input
                          type="date"
                          className="exits-input receive-stock-expiry-input"
                          value={line.expiryDate}
                          onChange={(e) =>
                            onPatchLine(line.productId, {
                              expiryDate: e.target.value,
                            })
                          }
                          aria-invalid={expiryInvalid || undefined}
                          aria-required
                          aria-label={t("purchasing.expiryDate")}
                          data-testid={`direct-line-expiry-${line.productId}`}
                        />
                      ) : (
                        <span className="text-muted">—</span>
                      )}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="text">
                      {line.tracksExpiration ? (
                        <input
                          className="exits-input receive-stock-lot-input"
                          value={line.lotNumber}
                          onChange={(e) =>
                            onPatchLine(line.productId, {
                              lotNumber: e.target.value,
                            })
                          }
                          aria-label={t("purchasing.lotNumber")}
                          data-testid={`direct-line-lot-${line.productId}`}
                        />
                      ) : (
                        <span className="text-muted">—</span>
                      )}
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
                  <ReceiptExpiryLotDetails line={line} onPatchLine={onPatchLine} t={t} />
                }
              />
            );
          })}
        </ul>
  );

  return (
    <ExitsResponsiveDataView
      layout={layout}
      testId="direct-receipt-table"
      className="receive-stock-receipt-responsive"
      table={layout === "table" ? tableBody : null}
      list={layout === "list" ? listBody : null}
    />
  );
}
