import type { ReactNode } from "react";
import { Card } from "@/components/ui/card";
import { StatusChip } from "@/components/exits/StatusChip";
import { cn } from "@/lib/cn";
import type { PoDocumentMetaField, PoDocumentStatus } from "@/features/purchasing/po-document-types";

export type PoDocumentSummaryProps = {
  /** Optional section title above the meta grid / counterparty. */
  title?: string;
  /** Buyer or Seller counterparty label (perspective-aware). Omit with `counterpartyName` for field-only layouts. */
  counterpartyLabel?: string;
  /** Optional leading icon beside the counterparty name (store/branch line). */
  counterpartyIcon?: ReactNode;
  /** Counterparty display name. When omitted (with label), the header block is skipped. */
  counterpartyName?: string;
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
  title,
  counterpartyLabel,
  counterpartyIcon,
  counterpartyName,
  status,
  fields,
  footer,
  className,
  testId = "po-document-summary",
}: PoDocumentSummaryProps) {
  const showCounterparty = Boolean(counterpartyLabel && counterpartyName != null);

  return (
    <Card className={cn("po-document-summary grid gap-3 p-3", className)} data-testid={testId}>
      {title ? (
        <h2 className="po-document-summary__title m-0" data-testid={`${testId}-title`}>
          {title}
        </h2>
      ) : null}

      {showCounterparty || status ? (
        <div className="flex flex-wrap items-start justify-between gap-2">
          {showCounterparty ? (
            <div className="min-w-0">
              {!title ? <h2 className="po-document-summary__title m-0">{counterpartyLabel}</h2> : (
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{counterpartyLabel}</p>
              )}
              <p
                className="m-0 mt-1 flex items-center gap-2 font-semibold"
                data-testid={`${testId}-counterparty`}
              >
                {counterpartyIcon ? (
                  <span className="inline-flex shrink-0 text-primary" aria-hidden>
                    {counterpartyIcon}
                  </span>
                ) : null}
                <span className="min-w-0">{counterpartyName}</span>
              </p>
            </div>
          ) : (
            <span />
          )}
          {status ? (
            <span data-testid={`${testId}-status`}>
              <StatusChip tone={status.tone}>{status.label}</StatusChip>
            </span>
          ) : null}
        </div>
      ) : null}

      {fields.length > 0 ? (
        <dl className="po-document-summary__meta m-0">
          {fields.map((field) => (
            <div
              key={field.key}
              className={cn("po-document-summary__field", `po-document-summary__field--${field.key}`)}
              data-field={field.key}
            >
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
