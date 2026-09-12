import type { ReactNode } from "react";
import { Card } from "@/components/ui/card";
import { StatusChip } from "@/components/exits/StatusChip";
import { cn } from "@/lib/cn";
import type { PoDocumentMetaField, PoDocumentStatus } from "@/features/purchasing/po-document-types";

export type PoDocumentSummaryProps = {
  /** Buyer or Seller counterparty label (perspective-aware). */
  counterpartyLabel: string;
  counterpartyName: string;
  status?: PoDocumentStatus;
  fields: PoDocumentMetaField[];
  /** Optional decline reason / note block. */
  footer?: ReactNode;
  className?: string;
  testId?: string;
};

/**
 * Compact PO document summary card — counterparty + status + metadata grid.
 * Not a nested card stack; single bordered summary surface.
 */
export function PoDocumentSummary({
  counterpartyLabel,
  counterpartyName,
  status,
  fields,
  footer,
  className,
  testId = "po-document-summary",
}: PoDocumentSummaryProps) {
  return (
    <Card className={cn("po-document-summary grid gap-3 p-3", className)} data-testid={testId}>
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div className="min-w-0">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{counterpartyLabel}</p>
          <p className="m-0 font-semibold" data-testid={`${testId}-counterparty`}>
            {counterpartyName}
          </p>
        </div>
        {status ? (
          <span data-testid={`${testId}-status`}>
            <StatusChip tone={status.tone}>{status.label}</StatusChip>
          </span>
        ) : null}
      </div>

      {fields.length > 0 ? (
        <dl className="po-document-summary__meta m-0">
          {fields.map((field) => (
            <div key={field.key} className="po-document-summary__field">
              <dt className="m-0 text-[length:var(--exits-text-sm)] text-muted">{field.label}</dt>
              <dd className="m-0 text-[length:var(--exits-text-sm)] font-medium" data-testid={`${testId}-${field.key}`}>
                {field.value}
              </dd>
            </div>
          ))}
        </dl>
      ) : null}

      {footer ? <div className="po-document-summary__footer">{footer}</div> : null}
    </Card>
  );
}
