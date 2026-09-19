import { useEffect, useRef, useState, type ReactNode } from "react";
import { Check, Pencil, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
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
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { Notice } from "@/components/exits/Notice";
import type { ResponsiveDataLayout } from "@/components/exits/responsive-data-view";
import {
  roundMoney,
  type SupplierAvailabilityState,
} from "@/features/purchasing/purchase-order-create-connected";
import {
  clampQuantityToPrecision,
  formatQuantityValue,
  maxQuantityDecimals,
  parseQuantityTyping,
  quantityInputMinimum,
  stripQuantityGrouping,
} from "@/lib/quantity-rules";
import { cn } from "@/lib/cn";

export type PurchaseOrderSelectedLine = {
  productId: string;
  name: string;
  sku?: string | null;
  orderedQty: number;
  /** Optional unit suffix shown beside qty (e.g. Kg). */
  unitLabel?: string | null;
  /** Catalog UOM for qty precision while editing. */
  unitOfMeasure?: string | null;
  /** Authoritative supplier/catalog unit price (read-only). */
  unitPurchaseCost: number;
  /**
   * Canonical availability label from formatSupplierAvailabilityLabel
   * (qty digits, out-of-stock copy, or not-tracked copy).
   */
  availabilityLabel?: string | null;
  /** Presentation kind from resolveSupplierAvailability — no recalculation. */
  availabilityKind?: SupplierAvailabilityState["kind"] | null;
  /** Non-blocking over-order warning copy when requested qty exceeds availability. */
  overOrderWarning?: string | null;
  onRemove: () => void;
  /** Commit edited qty (plain text input). Omit when qty is read-only. */
  onQtyChange?: (next: number) => void;
  canEditQty?: boolean;
};

type Translate = (key: string) => string;

type PurchaseOrderItemsViewProps = {
  layout: ResponsiveDataLayout;
  lines: readonly PurchaseOrderSelectedLine[];
  t: Translate;
  lineTestIdPrefix?: string;
};

function formatQtyDisplay(qty: number): string {
  return Math.abs(qty - Math.trunc(qty)) < 1e-9
    ? String(Math.trunc(qty))
    : String(Math.round(qty * 1_000_000) / 1_000_000);
}

function qtyPrecision(unitOfMeasure?: string | null): number {
  return maxQuantityDecimals(unitOfMeasure, "PerItem");
}

function qtyFloor(unitOfMeasure?: string | null): number {
  return quantityInputMinimum(unitOfMeasure, "PerItem");
}

function commitQtyText(
  raw: string,
  unitOfMeasure: string | null | undefined,
  onQtyChange: (next: number) => void,
): string {
  const precision = qtyPrecision(unitOfMeasure);
  const floor = qtyFloor(unitOfMeasure);
  const parsed = parseQuantityTyping(raw.trim(), precision);

  let next: number | null = null;
  if (parsed.kind === "value") {
    next = parsed.value;
  } else if (parsed.kind === "incomplete" || parsed.kind === "empty") {
    const cleaned = stripQuantityGrouping(raw).replace(/\.$/u, "");
    if (cleaned === "") {
      next = floor;
    } else {
      const asNumber = Number(cleaned);
      if (Number.isFinite(asNumber)) {
        next = asNumber;
      }
    }
  }

  if (next == null || !Number.isFinite(next)) {
    return formatQuantityValue(floor, precision);
  }

  let resolved = clampQuantityToPrecision(next, precision);
  if (resolved <= 0 || resolved < floor) {
    resolved = floor;
  }
  onQtyChange(resolved);
  return formatQuantityValue(resolved, precision);
}

/**
 * Create PO selected lines — Available column; qty + edit + delete as separate cols.
 * Always table presentation (no card conversion).
 */
export function PurchaseOrderItemsView({
  layout: _layout,
  lines,
  t,
  lineTestIdPrefix = "po-connected-selected",
}: PurchaseOrderItemsViewProps) {
  const [editingProductId, setEditingProductId] = useState<string | null>(null);
  const [qtyDraft, setQtyDraft] = useState("");
  const qtyInputRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    if (!editingProductId) {
      return;
    }
    if (!lines.some((line) => line.productId === editingProductId)) {
      setEditingProductId(null);
      setQtyDraft("");
    }
  }, [editingProductId, lines]);

  useEffect(() => {
    if (!editingProductId) {
      return;
    }
    const input = qtyInputRef.current;
    if (!input) {
      return;
    }
    input.focus();
    input.select();
  }, [editingProductId]);

  function startEdit(line: PurchaseOrderSelectedLine) {
    setEditingProductId(line.productId);
    setQtyDraft(formatQuantityValue(line.orderedQty, qtyPrecision(line.unitOfMeasure)));
  }

  function finishEdit(line: PurchaseOrderSelectedLine) {
    if (line.onQtyChange) {
      const nextText = commitQtyText(qtyDraft, line.unitOfMeasure, line.onQtyChange);
      setQtyDraft(nextText);
    }
    setEditingProductId(null);
    setQtyDraft("");
  }

  function availableCell(
    line: PurchaseOrderSelectedLine,
    options?: { testId?: string | null },
  ): ReactNode {
    const kind = line.availabilityKind;
    const testId =
      options && "testId" in options
        ? options.testId
        : `${lineTestIdPrefix}-availability-${line.productId}`;

    if (kind == null || kind === "unknown") {
      return (
        <span className="po-order-items__available-empty text-muted" data-testid={testId ?? undefined}>
          —
        </span>
      );
    }

    if (kind === "untracked") {
      return (
        <span className="po-order-items__available-empty text-muted" data-testid={testId ?? undefined}>
          {t("purchasing.stockNotTracked")}
        </span>
      );
    }

    if (kind === "out_of_stock") {
      return (
        <span
          className="po-order-items__available-readout po-order-items__available-readout--zero"
          data-testid={testId ?? undefined}
        >
          <span className="po-order-items__available-qty tabular-nums text-destructive">0</span>
          {line.unitLabel?.trim() ? (
            <span className="po-order-items__available-unit">{line.unitLabel.trim()}</span>
          ) : null}
        </span>
      );
    }

    return (
      <span className="po-order-items__available-readout" data-testid={testId ?? undefined}>
        <span className="po-order-items__available-qty tabular-nums">
          {line.availabilityLabel ?? "—"}
        </span>
        {line.unitLabel?.trim() ? (
          <span className="po-order-items__available-unit">{line.unitLabel.trim()}</span>
        ) : null}
      </span>
    );
  }

  function qtyCell(line: PurchaseOrderSelectedLine) {
    const editing = editingProductId === line.productId;
    if (editing) {
      return (
        <div className="po-order-items__qty-editor">
          <input
            ref={qtyInputRef}
            type="text"
            inputMode="decimal"
            className="exits-input exits-input--no-spin po-order-items__qty-input tabular-nums"
            value={qtyDraft}
            aria-label={t("purchasing.qtyShort")}
            data-testid={`po-qty-${line.productId}`}
            onChange={(e) => setQtyDraft(e.target.value)}
            onBlur={() => {
              if (!line.onQtyChange) {
                return;
              }
              setQtyDraft(commitQtyText(qtyDraft, line.unitOfMeasure, line.onQtyChange));
            }}
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                e.preventDefault();
                finishEdit(line);
              }
            }}
          />
          {line.unitLabel?.trim() ? (
            <span className="po-order-items__qty-unit">{line.unitLabel.trim()}</span>
          ) : null}
        </div>
      );
    }

    return (
      <span
        className="po-order-items__qty-readout"
        data-testid={`${lineTestIdPrefix}-qty-value-${line.productId}`}
      >
        <span className="po-order-items__qty-value tabular-nums">
          {formatQtyDisplay(line.orderedQty)}
        </span>
        {line.unitLabel?.trim() ? (
          <span className="po-order-items__qty-unit">{line.unitLabel.trim()}</span>
        ) : null}
      </span>
    );
  }

  function editAction(line: PurchaseOrderSelectedLine) {
    const editing = editingProductId === line.productId;
    const canEdit = line.canEditQty !== false && typeof line.onQtyChange === "function";

    if (editing) {
      return (
        <Button
          type="button"
          intent="success"
          appearance="outline"
          size="icon"
          aria-label={t("purchasing.doneEditingQty")}
          onClick={() => finishEdit(line)}
          data-testid={`${lineTestIdPrefix}-qty-done-${line.productId}`}
        >
          <Check className="size-4" aria-hidden />
        </Button>
      );
    }

    if (!canEdit) {
      return null;
    }

    return (
      <Button
        type="button"
        intent="primary"
        appearance="outline"
        size="icon"
        className="po-order-items__edit-btn"
        aria-label={t("purchasing.editQty").replace("{name}", line.name)}
        onClick={() => startEdit(line)}
        data-testid={`${lineTestIdPrefix}-qty-edit-${line.productId}`}
      >
        <Pencil className="size-4" aria-hidden />
      </Button>
    );
  }

  function deleteAction(line: PurchaseOrderSelectedLine) {
    return (
      <Button
        type="button"
        intent="danger"
        appearance="outline"
        size="icon"
        aria-label={t("purchasing.removeNamed").replace("{name}", line.name)}
        onClick={() => {
          if (editingProductId === line.productId) {
            setEditingProductId(null);
            setQtyDraft("");
          }
          line.onRemove();
        }}
        data-testid={`${lineTestIdPrefix}-remove-${line.productId}`}
      >
        <Trash2 className="size-4" aria-hidden />
      </Button>
    );
  }

  function actionCell(line: PurchaseOrderSelectedLine) {
    return (
      <ExitsTableActions className="po-order-items__row-actions justify-center">
        {editAction(line)}
        {deleteAction(line)}
      </ExitsTableActions>
    );
  }

  const tableBody = (
    <ExitsTableContainer className="po-order-items-table">
      <ExitsTable>
        <ExitsTableHeader>
          <ExitsTableRow>
            <ExitsTableHead
              cellAlign="text"
              colSize="flex"
              className="po-order-items-table__product-col"
            >
              {t("purchasing.colProduct")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="center" className="po-order-items-table__available-col">
              {t("purchasing.colAvailable")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="numeric" className="po-order-items-table__price-col">
              {t("purchasing.catalogPrice")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="numeric" className="po-order-items-table__total-col">
              {t("purchasing.lineTotal")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="center" className="po-order-items-table__qty-col">
              {t("purchasing.qty")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="center" className="po-order-items-table__action-col">
              {t("purchasing.colAction")}
            </ExitsTableHead>
          </ExitsTableRow>
        </ExitsTableHeader>
        <ExitsTableBody>
          {lines.map((line) => {
            const lineTotal = roundMoney(line.orderedQty * line.unitPurchaseCost);
            return (
              <ExitsTableRow
                key={line.productId}
                data-testid={`${lineTestIdPrefix}-${line.productId}`}
              >
                <ExitsTableCell
                  cellAlign="text"
                  colSize="flex"
                  className="po-order-items-table__product-col"
                >
                  <div className="font-medium leading-snug po-order-items-table__product-name">
                    {line.name}
                  </div>
                  {line.sku?.trim() ? (
                    <div className="text-[length:var(--exits-text-xs)] text-muted">
                      {line.sku.trim()}
                    </div>
                  ) : null}
                  {/* Mobile-only: Available tucked under product identity */}
                  {line.availabilityKind != null && line.availabilityKind !== "unknown" ? (
                    <div className="po-order-items-table__product-available-mobile">
                      {availableCell(line, { testId: null })}
                    </div>
                  ) : null}
                  {line.overOrderWarning ? (
                    <Notice
                      tone="warning"
                      className="mt-2"
                      testId={`${lineTestIdPrefix}-over-order-${line.productId}`}
                    >
                      {line.overOrderWarning}
                    </Notice>
                  ) : null}
                </ExitsTableCell>
                <ExitsTableCell cellAlign="center" className="po-order-items-table__available-col">
                  {availableCell(line)}
                </ExitsTableCell>
                <ExitsTableCell cellAlign="numeric" className="po-order-items-table__price-col">
                  <span
                    className="tabular-nums"
                    data-testid={`${lineTestIdPrefix}-price-${line.productId}`}
                  >
                    <MoneyDisplay amount={line.unitPurchaseCost} />
                  </span>
                </ExitsTableCell>
                <ExitsTableCell cellAlign="numeric" className="po-order-items-table__total-col">
                  <span
                    className="tabular-nums font-semibold"
                    data-testid={`${lineTestIdPrefix}-line-total-${line.productId}`}
                  >
                    <MoneyDisplay amount={lineTotal} />
                  </span>
                </ExitsTableCell>
                <ExitsTableCell cellAlign="center" className="po-order-items-table__qty-col">
                  <ExitsTableActions className="po-order-items__qty-actions justify-center">
                    {qtyCell(line)}
                  </ExitsTableActions>
                </ExitsTableCell>
                <ExitsTableCell cellAlign="center" className="po-order-items-table__action-col">
                  {actionCell(line)}
                </ExitsTableCell>
              </ExitsTableRow>
            );
          })}
        </ExitsTableBody>
      </ExitsTable>
    </ExitsTableContainer>
  );

  return (
    <ExitsResponsiveDataView
      layout="table"
      testId="po-order-items"
      className={cn("po-order-items-responsive")}
      table={tableBody}
      list={null}
    />
  );
}
