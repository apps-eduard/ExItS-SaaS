import { CountBadge } from "@/components/exits/CountChip";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import type { ConnectedPurchaseOrderLine } from "@/api/pos/pos-connected-suppliers-client";
import { cn } from "@/lib/cn";

export type IncomingOrderFulfillmentProgressProps = {
  lines: readonly ConnectedPurchaseOrderLine[];
  title: string;
  productLabel: string;
  orderedLabel: string;
  goodLabel: string;
  damagedLabel: string;
  missingLabel: string;
  unitLabel: string;
  unitCostLabel: string;
  remainingValueLabel: string;
  testId?: string;
};

function qty(value: number | null | undefined): string {
  const n = value ?? 0;
  return Number.isInteger(n) ? String(n) : String(n);
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
  unitLabel,
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
              <th scope="col">{unitLabel}</th>
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
              const uom = line.unitOfMeasureCode?.trim() || "—";
              return (
                <tr key={line.productId} data-testid={`${testId}-row-${line.productId}`}>
                  <td className="po-document-lines__product font-medium">{line.nameSnapshot}</td>
                  <td className="po-document-lines__num tabular-nums">
                    {qty(line.orderedQty ?? line.confirmedQty ?? line.qty)}
                  </td>
                  <td className="po-document-lines__num tabular-nums">
                    {qty(line.goodReceivedQty)}
                  </td>
                  <td className="po-document-lines__num tabular-nums">{qty(line.damagedQty)}</td>
                  {showMissing ? (
                    <td className="po-document-lines__num tabular-nums">{qty(line.missingQty)}</td>
                  ) : null}
                  <td className="text-muted">{uom}</td>
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
          const uom = line.unitOfMeasureCode?.trim() || "";
          return (
            <li
              key={line.productId}
              className="po-document-lines__mobile-row"
              data-testid={`${testId}-mobile-${line.productId}`}
            >
              <p className="m-0 font-medium">{line.nameSnapshot}</p>
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted tabular-nums">
                {orderedLabel}: {qty(line.orderedQty ?? line.confirmedQty ?? line.qty)}
                {" · "}
                {goodLabel}: {qty(line.goodReceivedQty)}
                {" · "}
                {damagedLabel}: {qty(line.damagedQty)}
                {showMissing ? ` · ${missingLabel}: ${qty(line.missingQty)}` : ""}
                {uom ? ` · ${unitLabel}: ${uom}` : ""}
              </p>
              <p className={cn("m-0 text-[length:var(--exits-text-sm)] tabular-nums")}>
                {remainingValueLabel}: <MoneyDisplay amount={line.remainingValue ?? 0} />
              </p>
            </li>
          );
        })}
      </ul>
    </section>
  );
}
