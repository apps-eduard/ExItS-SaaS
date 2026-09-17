import type { ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { CountBadge } from "@/components/exits/CountChip";
import { EmptyState } from "@/components/exits/EmptyState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { useMediaMin } from "@/hooks/useMediaQuery";
import { cn } from "@/lib/cn";
import { ClipboardList } from "lucide-react";

export type PoDocumentSelectedLine = {
  id: string;
  productName: string;
  sku?: string | null;
  quantity: number;
  unitOfMeasure?: string;
  unitCost: number;
  lineTotal: number;
  /** Compact qty control (stepper / input). */
  quantityControl?: ReactNode;
  /** Compact unit-cost control when editable. */
  unitCostControl?: ReactNode;
  onRemove?: () => void;
};

export type PoDocumentSelectedItemsProps = {
  title: string;
  emptyTitle: string;
  emptyDetail?: string;
  lines: PoDocumentSelectedLine[];
  productColLabel: string;
  qtyColLabel: string;
  unitCostColLabel: string;
  lineTotalColLabel: string;
  actionsColLabel: string;
  removeLabel: string;
  className?: string;
  testId?: string;
  lineTestIdPrefix?: string;
};

/**
 * Create-PO selected lines — document-style with compact editable qty/cost.
 */
export function PoDocumentSelectedItems({
  title,
  emptyTitle,
  emptyDetail,
  lines,
  productColLabel,
  qtyColLabel,
  unitCostColLabel,
  lineTotalColLabel,
  actionsColLabel,
  removeLabel,
  className,
  testId = "po-document-selected-items",
  lineTestIdPrefix = "po-draft-line",
}: PoDocumentSelectedItemsProps) {
  const mediaDesktop = useMediaMin(768);
  const isDesktop =
    typeof window === "undefined" || typeof window.matchMedia !== "function"
      ? true
      : mediaDesktop;

  return (
    <section
      className={cn("po-document-selected flex flex-col gap-2", className)}
      aria-labelledby={`${testId}-heading`}
      data-testid={testId}
    >
      <h2
        id={`${testId}-heading`}
        className="po-document-selected__title m-0 flex items-center gap-2 text-[length:var(--exits-text-md)] font-medium text-[var(--exits-primary)]"
      >
        <span>{title}</span>
        <CountBadge count={lines.length} tone="primary" />
      </h2>

      {lines.length === 0 ? (
        <EmptyState
          variant="setup"
          align="center"
          size="compact"
          icon={<ClipboardList className="size-5" strokeWidth={1.75} />}
          title={emptyTitle}
          detail={emptyDetail}
          testId={`${testId}-empty`}
        />
      ) : isDesktop ? (
        <div className="po-document-selected__table-wrap">
          <table className="po-document-selected__table">
            <thead>
              <tr>
                <th scope="col">{productColLabel}</th>
                <th scope="col" className="po-document-lines__num">
                  {qtyColLabel}
                </th>
                <th scope="col" className="po-document-lines__num">
                  {unitCostColLabel}
                </th>
                <th scope="col" className="po-document-lines__num">
                  {lineTotalColLabel}
                </th>
                <th scope="col" className="po-document-lines__actions">
                  {actionsColLabel}
                </th>
              </tr>
            </thead>
            <tbody>
              {lines.map((line) => (
                <tr key={line.id} data-testid={`${lineTestIdPrefix}-${line.id}`}>
                  <td>
                    <p className="m-0 font-medium">{line.productName}</p>
                    {line.sku?.trim() ? (
                      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                        {line.sku.trim()}
                      </p>
                    ) : null}
                  </td>
                  <td className="po-document-lines__num">
                    <div className="po-document-selected__qty">
                      {line.quantityControl ?? (
                        <span className="tabular-nums">
                          {line.quantity}
                          {line.unitOfMeasure ? ` ${line.unitOfMeasure}` : ""}
                        </span>
                      )}
                    </div>
                  </td>
                  <td className="po-document-lines__num">
                    <div className="po-document-selected__cost">
                      {line.unitCostControl ?? (
                        <span className="tabular-nums">
                          <MoneyDisplay amount={line.unitCost} />
                        </span>
                      )}
                    </div>
                  </td>
                  <td className="po-document-lines__num tabular-nums font-semibold">
                    <MoneyDisplay amount={line.lineTotal} />
                  </td>
                  <td className="po-document-lines__actions">
                    {line.onRemove ? (
                      <Button
                        type="button"
                        variant="ghost"
                        size="default"
                        onClick={line.onRemove}
                        data-testid={`${lineTestIdPrefix}-remove-${line.id}`}
                      >
                        {removeLabel}
                      </Button>
                    ) : null}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        <ul className="po-document-selected__mobile m-0 flex list-none flex-col gap-2 p-0">
          {lines.map((line) => (
            <li
              key={line.id}
              className="po-document-selected__mobile-row"
              data-testid={`${lineTestIdPrefix}-mobile-${line.id}`}
            >
              <p className="m-0 font-medium">{line.productName}</p>
              {line.sku?.trim() ? (
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{line.sku.trim()}</p>
              ) : null}
              <label className="po-document-selected__mobile-field">
                <span className="text-muted">{qtyColLabel}</span>
                <div className="po-document-selected__qty">
                  {line.quantityControl ?? (
                    <span className="tabular-nums">
                      {line.quantity}
                      {line.unitOfMeasure ? ` ${line.unitOfMeasure}` : ""}
                    </span>
                  )}
                </div>
              </label>
              <label className="po-document-selected__mobile-field">
                <span className="text-muted">{unitCostColLabel}</span>
                <div className="po-document-selected__cost">
                  {line.unitCostControl ?? (
                    <span className="tabular-nums">
                      <MoneyDisplay amount={line.unitCost} />
                    </span>
                  )}
                </div>
              </label>
              <div className="po-document-selected__mobile-total">
                <span className="text-muted">{lineTotalColLabel}</span>
                <span className="tabular-nums font-semibold">
                  <MoneyDisplay amount={line.lineTotal} />
                </span>
              </div>
              {line.onRemove ? (
                <Button
                  type="button"
                  variant="ghost"
                  className="w-fit"
                  onClick={line.onRemove}
                  data-testid={`${lineTestIdPrefix}-remove-mobile-${line.id}`}
                >
                  {removeLabel}
                </Button>
              ) : null}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
