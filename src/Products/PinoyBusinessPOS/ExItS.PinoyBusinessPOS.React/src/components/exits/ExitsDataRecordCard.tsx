import type { HTMLAttributes, ReactNode } from "react";
import { cn } from "@/lib/cn";

export type ExitsDataRecordField = {
  label: string;
  value: ReactNode;
  /** Emphasize money / primary totals. */
  emphasize?: boolean;
};

export type ExitsDataRecordCardProps = HTMLAttributes<HTMLElement> & {
  title: ReactNode;
  subtitle?: ReactNode;
  status?: ReactNode;
  fields?: readonly ExitsDataRecordField[];
  /** Up to one primary quick action (Edit / View). */
  primaryAction?: ReactNode;
  /** Overflow / More cluster. */
  moreActions?: ReactNode;
  selected?: boolean;
  /** Optional collapsed secondary details. */
  details?: ReactNode;
  as?: "article" | "li" | "div";
};

/**
 * Structured list/card record for Responsive Data View LIST mode.
 * Priority: title → status/actions → key fields → overflow actions.
 */
export function ExitsDataRecordCard({
  title,
  subtitle,
  status,
  fields,
  primaryAction,
  moreActions,
  selected = false,
  details,
  as: Comp = "article",
  className,
  ...rest
}: ExitsDataRecordCardProps) {
  return (
    <Comp
      {...rest}
      className={cn(
        "exits-data-record",
        selected && "exits-data-record--selected",
        className,
      )}
      data-selected={selected ? "true" : undefined}
    >
      <div className="exits-data-record__top">
        <div className="exits-data-record__identity">
          <div className="exits-data-record__title">{title}</div>
          {subtitle ? <div className="exits-data-record__subtitle">{subtitle}</div> : null}
        </div>
        <div className="exits-data-record__trailing">
          {status}
          {primaryAction || moreActions ? (
            <div className="exits-data-record__actions">
              {primaryAction}
              {moreActions}
            </div>
          ) : null}
        </div>
      </div>
      {fields && fields.length > 0 ? (
        <dl className="exits-data-record__fields">
          {fields.map((field) => (
            <div key={field.label} className="exits-data-record__field">
              <dt>{field.label}</dt>
              <dd className={field.emphasize ? "exits-data-record__value--emphasize" : undefined}>
                {field.value}
              </dd>
            </div>
          ))}
        </dl>
      ) : null}
      {details ? <div className="exits-data-record__details">{details}</div> : null}
    </Comp>
  );
}
