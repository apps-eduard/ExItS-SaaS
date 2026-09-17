import { CountBadge } from "@/components/exits/CountChip";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import type { ConnectedPurchaseOrderLine } from "@/api/pos/pos-connected-suppliers-client";
import { cn } from "@/lib/cn";
import { formatStockQtyLabel } from "@/features/purchasing/incoming-order-stock-review";

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

function qty(value: number | null | undefined, uom: string): string {
  return formatStockQtyLabel(value ?? 0, uom);
}

/**
 * Seller fulfillment-progress table after buyer partial/full receipt.
 * Compact desktop table + mobile stacked rows (CSS visibility).
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
  const showMissing = lines.some((line) => (line.missingQty ?? 0) > 0);

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
              <th scope="col" className="po-document-lines__num">
                {orderedLabel}
              </th>
              <th scope="col" className="po-document-lines__num">
                {goodLabel}
              </th>
              <th scope="col" className="po-document-lines__num">
                {damagedLabel}
              </th>
              {showMissing ? (
                <th scope="col" className="po-document-lines__num">
                  {missingLabel}
                </th>
              ) : null}
              <th scope="col" className="po-document-lines__num">
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
              const outstanding = line.outstandingQty ?? 0;
              const uom = line.unitOfMeasureCode;
              return (
                <tr
                  key={line.productId}
                  data-testid={`${testId}-row-${line.productId}`}
                  className={cn(outstanding > 0 && "incoming-order-fulfillment-row--outstanding")}
                >
                  <td className="po-document-lines__product font-medium">{line.nameSnapshot}</td>
                  <td className="po-document-lines__num tabular-nums">
                    {qty(line.orderedQty ?? line.confirmedQty ?? line.qty, uom)}
                  </td>
                  <td className="po-document-lines__num tabular-nums">
                    {qty(line.goodReceivedQty, uom)}
                  </td>
                  <td className="po-document-lines__num tabular-nums">{qty(line.damagedQty, uom)}</td>
                  {showMissing ? (
                    <td className="po-document-lines__num tabular-nums">{qty(line.missingQty, uom)}</td>
                  ) : null}
                  <td
                    className={cn(
                      "po-document-lines__num tabular-nums",
                      outstanding > 0 && "font-semibold text-[var(--exits-warning, #b45309)]",
                    )}
                    data-testid={`${testId}-outstanding-${line.productId}`}
                  >
                    {qty(outstanding, uom)}
                  </td>
                  <td className="po-document-lines__num tabular-nums">
                    <MoneyDisplay amount={line.unitPriceSnapshot} />
                  </td>
                  <td className="po-document-lines__num tabular-nums font-semibold">
                    <MoneyDisplay amount={line.remainingValue ?? 0} />
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
          const outstanding = line.outstandingQty ?? 0;
          const uom = line.unitOfMeasureCode;
          return (
            <li
              key={line.productId}
              className={cn(
                "po-document-lines__mobile-row",
                outstanding > 0 && "incoming-order-fulfillment-row--outstanding",
              )}
              data-testid={`${testId}-mobile-${line.productId}`}
            >
              <p className="m-0 font-medium">{line.nameSnapshot}</p>
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted tabular-nums">
                {orderedLabel}: {qty(line.orderedQty ?? line.confirmedQty ?? line.qty, uom)}
                {" · "}
                {goodLabel}: {qty(line.goodReceivedQty, uom)}
                {" · "}
                {damagedLabel}: {qty(line.damagedQty, uom)}
                {showMissing ? ` · ${missingLabel}: ${qty(line.missingQty, uom)}` : ""}
              </p>
              <p
                className={cn(
                  "m-0 text-[length:var(--exits-text-sm)] tabular-nums",
                  outstanding > 0 && "font-semibold text-[var(--exits-warning, #b45309)]",
                )}
              >
                {outstandingLabel}: {qty(outstanding, uom)}
                {" · "}
                {remainingValueLabel}: <MoneyDisplay amount={line.remainingValue ?? 0} />
              </p>
            </li>
          );
        })}
      </ul>
    </section>
  );
}
