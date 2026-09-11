import type { ReactNode } from "react";
import { cn } from "@/lib/cn";
import {
  UiStandardsSampleCommandFooter,
  type UiStandardsStandardName,
} from "@/features/ui-standards/UiStandardsCopyCommand";

export type UiStandardsSampleCardProps = {
  label: string;
  children: ReactNode;
  hint?: string;
  testId?: string;
  className?: string;
  /** Content wrapper class (e.g. flex justify-start). */
  contentClassName?: string;
  /** When false, omit the bordered muted sample chrome (tables/cards frames). */
  bordered?: boolean;
  standard?: UiStandardsStandardName | UiStandardsStandardName[];
  /** Explicit semantic Cursor shorthand for this sample. */
  command?: string;
  commandContext?: string;
  /**
   * Explanatory / non-implementable chrome (notes, legends).
   * Excluded from coverage audits when true.
   */
  explanatory?: boolean;
};

/**
 * UI Standards sample shell with optional Cursor command footer.
 */
export function UiStandardsSampleCard({
  label,
  children,
  hint,
  testId,
  className,
  contentClassName,
  bordered = true,
  standard,
  command,
  commandContext,
  explanatory = false,
}: UiStandardsSampleCardProps) {
  const hasCopy = Boolean(standard && command?.trim());
  return (
    <div
      className={cn(
        "flex min-w-0 flex-col gap-1.5",
        bordered &&
          "rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)]/40 p-2",
        className,
      )}
      data-testid={testId}
      data-ui-standards-sample={explanatory ? "explanatory" : "implementable"}
      data-has-copy={hasCopy ? "true" : "false"}
    >
      <span className="text-[length:var(--exits-text-xs)] uppercase tracking-wide text-muted">{label}</span>
      <div className={cn("min-w-0", contentClassName)}>{children}</div>
      {hint ? <span className="text-[length:var(--exits-text-xs)] text-muted">{hint}</span> : null}
      <UiStandardsSampleCommandFooter
        standard={standard}
        command={command}
        commandContext={commandContext}
      />
    </div>
  );
}
