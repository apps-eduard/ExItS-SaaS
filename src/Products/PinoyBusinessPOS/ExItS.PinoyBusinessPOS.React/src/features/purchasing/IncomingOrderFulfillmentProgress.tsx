import { CountBadge } from "@/components/exits/CountChip";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import type { ConnectedPurchaseOrderLine } from "@/api/pos/pos-connected-suppliers-client";
import { formatUnitOfMeasureLabel } from "@/features/purchasing/purchase-order-create-connected";
import { cn } from "@/lib/cn";

export type IncomingOrderFulfillmentProgressProps = {
  lines: readonly ConnectedPurchaseOrderLine[];
  title: string;
  productLabel: string;
  orderedLabel: string;
  goodLabel: string;
  damagedLabel: string;
  missingLabel: string;
  outstandingLabel: string;
  unitCostLabel: string;
  remainingValueLabel: string;
  testId?: string;
};

function qty(value: number | null | undefined): string {
  const n = value ?? 0;
  return Number.isInteger(n) ? String(n) : String(n);
}

function lineUnit(line: ConnectedPurchaseOrderLine): string {
  const raw = line.unitOfMeasureCode?.trim();
  return raw ? formatUnitOfMeasureLabel(raw) : "";
}

function orderedQty(line: ConnectedPurchaseOrderLine): number {
  return line.orderedQty ?? line.confirmedQty ?? line.qty;
}

function goodQty(line: ConnectedPurchaseOrderLine): number {
  return line.goodReceivedQty ?? 0;
}

function damagedQty(line: ConnectedPurchaseOrderLine): number {
  return line.damagedQty ?? 0;
}

function missingQty(line: ConnectedPurchaseOrderLine): number {
  return line.missingQty ?? 0;
}

/** Outstanding = ordered − good − cancelled; prefer API when present. */
function outstandingQty(line: ConnectedPurchaseOrderLine): number {
  if (line.outstandingQty != null && Number.isFinite(line.outstandingQty)) {
    return Math.max(0, line.outstandingQty);
  }
  const cancelled = line.cancelledRemainingQty ?? 0;
  return Math.max(0, orderedQty(line) - goodQty(line) - cancelled);
}

function remainingValue(line: ConnectedPurchaseOrderLine): number {
  if (line.remainingValue != null && Number.isFinite(line.remainingValue)) {
    return line.remainingValue;
  }
  const unit = line.confirmedUnitPrice ?? line.unitPriceSnapshot;
  return outstandingQty(line) * unit;
}

function QtyWithUnit({
  value,
  unit,
}: {
  value: string;
  unit: string;
}) {
  return (
    <span className="inline-flex items-baseline justify-start gap-1 leading-tight">
      <span className="tabular-nums">{value}</span>
      {unit ? (
        <span className="text-[length:var(--exits-text-xs)] text-muted">{unit}</span>
      ) : null}
    </span>
  );
}

/**
 * Seller fulfillment-progress table after buyer partial/full receipt.
 * Cumulative: Ordered / Good received / Damaged / Missing? / Outstanding / Unit cost / Remaining value.
 * Unit sits beside Ordered (xs, start-aligned). Outstanding > 0 is emphasized for prepare-remaining.
 */
export function IncomingOrderFulfillmentProgress({
  lines,
  title,
  productLabel,
  orderedLabel,
  goodLabel,
  damagedLabel,
  missingLabel,
  outstandingLabel,
  unitCostLabel,
  remainingValueLabel,
  testId = "incoming-order-fulfillment-progress",
}: IncomingOrderFulfillmentProgressProps) {
  const showMissing = lines.some((line) => missingQty(line) > 0);

  return (
    <section className="po-document-lines flex flex-col gap-2" data-testid={testId}>
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 className="po-document-lines__title m-0 flex items-center gap-2 text-[length:var(--exits-text-md)] font-medium text-[var(--exits-primary)]">
          <span>{title}</span>
          <CountBadge count={lines.length} tone="primary" />
        </h2>
      </div>

      <div className="po-document-lines__table-wrap" data-testid={`${testId}-table`}>
        <table className="po-document-lines__table">
          <thead>
            <tr>
              <th scope="col">{productLabel}</th>
              <th scope="col" className="po-document-lines__num po-document-lines__num--start">
                {orderedLabel}
              </th>
              <th scope="col" className="po-document-lines__num po-document-lines__num--center">
                {goodLabel}
              </th>
              <th scope="col" className="po-document-lines__num po-document-lines__num--center">
                {damagedLabel}
              </th>
              {showMissing ? (
                <th scope="col" className="po-document-lines__num po-document-lines__num--center">
                  {missingLabel}
                </th>
              ) : null}
              <th scope="col" className="po-document-lines__num po-document-lines__num--center">
                {outstandingLabel}
              </th>
              <th scope="col" className="po-document-lines__num">
                {unitCostLabel}
              </th>
              <th scope="col" className="po-document-lines__num">
                {remainingValueLabel}
              </th>
            </tr>
          </thead>
          <tbody>
            {lines.map((line) => {
              const unit = lineUnit(line);
              const outstanding = outstandingQty(line);
              const hasOutstanding = outstanding > 0;
              return (
                <tr
                  key={line.productId}
                  className={cn(hasOutstanding && "incoming-order-fulfillment-progress__row--outstanding")}
                  data-testid={`${testId}-row-${line.productId}`}
                  data-outstanding={hasOutstanding ? "true" : "false"}
                >
                  <td className="po-document-lines__product font-medium">{line.nameSnapshot}</td>
                  <td className="po-document-lines__num po-document-lines__num--start">
                    <QtyWithUnit value={qty(orderedQty(line))} unit={unit} />
                  </td>
                  <td
                    className="po-document-lines__num po-document-lines__num--center tabular-nums"
                    data-testid={`${testId}-good-${line.productId}`}
                  >
                    {qty(goodQty(line))}
                  </td>
                  <td
                    className="po-document-lines__num po-document-lines__num--center tabular-nums"
                    data-testid={`${testId}-damaged-${line.productId}`}
                  >
                    {qty(damagedQty(line))}
                  </td>
                  {showMissing ? (
                    <td
                      className="po-document-lines__num po-document-lines__num--center tabular-nums"
                      data-testid={`${testId}-missing-${line.productId}`}
                    >
                      {qty(missingQty(line))}
                    </td>
                  ) : null}
                  <td
                    className={cn(
                      "po-document-lines__num po-document-lines__num--center tabular-nums",
                      hasOutstanding && "font-semibold text-[var(--exits-warning, #b45309)]",
                    )}
                    data-testid={`${testId}-outstanding-${line.productId}`}
                  >
                    {qty(outstanding)}
                  </td>
                  <td className="po-document-lines__num tabular-nums">
                    <MoneyDisplay amount={line.confirmedUnitPrice ?? line.unitPriceSnapshot} />
                  </td>
                  <td
                    className="po-document-lines__num tabular-nums font-semibold"
                    data-testid={`${testId}-remaining-value-${line.productId}`}
                  >
                    <MoneyDisplay amount={remainingValue(line)} />
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      <ul
        className="po-document-lines__mobile m-0 list-none flex-col gap-2 p-0"
        data-testid={`${testId}-mobile`}
      >
        {lines.map((line) => {
          const unit = lineUnit(line);
          const outstanding = outstandingQty(line);
          return (
            <li
              key={line.productId}
              className="po-document-lines__mobile-row"
              data-testid={`${testId}-mobile-${line.productId}`}
            >
              <p className="m-0 font-medium">{line.nameSnapshot}</p>
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted tabular-nums">
                {orderedLabel}: {qty(orderedQty(line))}
                {unit ? (
                  <span className="ml-1 text-[length:var(--exits-text-xs)]">{unit}</span>
                ) : null}
                {" · "}
                {goodLabel}: {qty(goodQty(line))}
                {" · "}
                {damagedLabel}: {qty(damagedQty(line))}
                {showMissing ? ` · ${missingLabel}: ${qty(missingQty(line))}` : ""}
                {" · "}
                {outstandingLabel}:{" "}
                <span className={cn(outstanding > 0 && "font-semibold text-[var(--exits-warning, #b45309)]")}>
                  {qty(outstanding)}
                </span>
              </p>
              <p className="m-0 text-[length:var(--exits-text-sm)] tabular-nums">
                {remainingValueLabel}: <MoneyDisplay amount={remainingValue(line)} />
              </p>
            </li>
          );
        })}
      </ul>
    </section>
  );
}
