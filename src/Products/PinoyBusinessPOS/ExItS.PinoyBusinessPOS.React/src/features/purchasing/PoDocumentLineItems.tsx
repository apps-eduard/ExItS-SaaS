import { CountBadge } from "@/components/exits/CountChip";
import { EmptyState } from "@/components/exits/EmptyState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { cn } from "@/lib/cn";
import type { PoDocumentLine } from "@/features/purchasing/po-document-types";
import { ClipboardList } from "lucide-react";
import type { ReactNode } from "react";

export type PoDocumentLineItemsProps = {
  title: string;
  emptyTitle: string;
  emptyDetail?: string;
  lines: PoDocumentLine[];
  /** When true, show buyer receive progress columns. */
  showReceiveProgress?: boolean;
  productColLabel: string;
  skuColLabel: string;
  qtyColLabel: string;
  unitCostColLabel: string;
  lineTotalColLabel: string;
  receivedColLabel?: string;
  outstandingColLabel?: string;
  /** Optional trailing slot in the section header (e.g. none). */
  headerEnd?: ReactNode;
  className?: string;
  testId?: string;
  lineTestIdPrefix?: string;
};

/**
 * Document-style PO line items — compact desktop table, mobile item rows.
 * No search / sort / pagination chrome (single-document review).
 * Desktop/mobile both render; CSS chooses visibility (read-only rows — no duplicate controls).
 */
export function PoDocumentLineItems({
  title,
  emptyTitle,
  emptyDetail,
  lines,
  showReceiveProgress = false,
  productColLabel,
  skuColLabel,
  qtyColLabel,
  unitCostColLabel,
  lineTotalColLabel,
  receivedColLabel,
  outstandingColLabel,
  headerEnd,
  className,
  testId = "po-document-lines",
  lineTestIdPrefix = "po-document-line",
}: PoDocumentLineItemsProps) {
  return (
    <section
      className={cn("po-document-lines flex flex-col gap-2", className)}
      aria-labelledby={`${testId}-heading`}
      data-testid={testId}
    >
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2
          id={`${testId}-heading`}
          className="po-document-lines__title m-0 flex items-center gap-2 text-[length:var(--exits-text-md)] font-medium text-[var(--exits-primary)]"
        >
          <span>{title}</span>
          <CountBadge count={lines.length} tone="primary" />
        </h2>
        {headerEnd}
      </div>

      {lines.length === 0 ? (
        <EmptyState
          variant="setup"
          align="center"
          size="compact"
          icon={<ClipboardList className="size-5" strokeWidth={1.75} />}
          title={emptyTitle}
          detail={emptyDetail}
        />
      ) : (
        <>
          <div className="po-document-lines__table-wrap" data-testid={`${testId}-table`}>
            <table className="po-document-lines__table">
              <thead>
                <tr>
                  <th scope="col">{productColLabel}</th>
                  <th scope="col">{skuColLabel}</th>
                  <th scope="col" className="po-document-lines__num">
                    {qtyColLabel}
                  </th>
                  <th scope="col" className="po-document-lines__num">
                    {unitCostColLabel}
                  </th>
                  <th scope="col" className="po-document-lines__num">
                    {lineTotalColLabel}
                  </th>
                  {showReceiveProgress ? (
                    <>
                      <th scope="col" className="po-document-lines__num">
                        {receivedColLabel}
                      </th>
                      <th scope="col" className="po-document-lines__num">
                        {outstandingColLabel}
                      </th>
                    </>
                  ) : null}
                </tr>
              </thead>
              <tbody>
                {lines.map((line) => (
                  <tr key={line.id} data-testid={`${lineTestIdPrefix}-${line.id}`}>
                    <td className="po-document-lines__product font-medium">{line.productName}</td>
                    <td className="text-muted">{line.sku?.trim() || "—"}</td>
                    <td className="po-document-lines__num tabular-nums">{line.quantityLabel}</td>
                    <td className="po-document-lines__num tabular-nums">
                      <MoneyDisplay amount={line.unitCost} />
                    </td>
                    <td className="po-document-lines__num tabular-nums font-semibold">
                      <MoneyDisplay
                        amount={line.lineTotal}
                        testId={`${lineTestIdPrefix}-total-${line.id}`}
                      />
                    </td>
                    {showReceiveProgress ? (
                      <>
                        <td className="po-document-lines__num tabular-nums">
                          {line.receivedLabel ?? "—"}
                        </td>
                        <td className="po-document-lines__num tabular-nums">
                          {line.outstandingLabel ?? "—"}
                        </td>
                      </>
                    ) : null}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <ul
            className="po-document-lines__mobile m-0 list-none flex-col gap-2 p-0"
            data-testid={`${testId}-mobile`}
          >
            {lines.map((line) => (
              <li
                key={line.id}
                className="po-document-lines__mobile-row"
                data-testid={`${lineTestIdPrefix}-mobile-${line.id}`}
              >
                <div className="po-document-lines__mobile-title-row">
                  <p className="m-0 font-medium">{line.productName}</p>
                  <p className="m-0 tabular-nums font-semibold">
                    <MoneyDisplay amount={line.lineTotal} />
                  </p>
                </div>
                {line.sku?.trim() ? (
                  <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{line.sku.trim()}</p>
                ) : null}
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted tabular-nums">
                  {line.quantityLabel} × <MoneyDisplay amount={line.unitCost} />
                </p>
                {showReceiveProgress ? (
                  <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                    {receivedColLabel}: {line.receivedLabel ?? "—"}
                    {" · "}
                    {outstandingColLabel}: {line.outstandingLabel ?? "—"}
                  </p>
                ) : null}
              </li>
            ))}
          </ul>
        </>
      )}
    </section>
  );
}
