import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { cn } from "@/lib/cn";
import type { PoDocumentTotalRow } from "@/features/purchasing/po-document-types";

export type PoDocumentTotalsProps = {
  rows: PoDocumentTotalRow[];
  className?: string;
  testId?: string;
};

/**
 * Document-style totals — right-aligned, no KPI card.
 */
export function PoDocumentTotals({
  rows,
  className,
  testId = "po-document-totals",
}: PoDocumentTotalsProps) {
  if (rows.length === 0) {
    return null;
  }

  return (
    <div className={cn("po-document-totals", className)} data-testid={testId}>
      {rows.map((row) => (
        <div
          key={row.key}
          className={cn(
            "po-document-totals__row",
            row.emphasis === "strong" && "po-document-totals__row--strong",
          )}
        >
          <span className="po-document-totals__label">{row.label}</span>
          <span className="po-document-totals__value tabular-nums">
            <MoneyDisplay amount={row.amount} testId={row.testId} />
          </span>
        </div>
      ))}
    </div>
  );
}
