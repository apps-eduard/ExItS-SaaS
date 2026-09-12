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
  /** Default = Classic gallery chrome. Compact = Simple catalog denser sample. */
  density?: "default" | "compact";
  standard?: UiStandardsStandardName | UiStandardsStandardName[];
  /** Explicit semantic Cursor shorthand for this sample. */
  command?: string;
  commandContext?: string;
  /** Pilot vs locked clipboard wording. Default locked. */
  standardStatus?: "locked" | "pilot";
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
  density = "default",
  standard,
  command,
  commandContext,
  standardStatus,
  explanatory = false,
}: UiStandardsSampleCardProps) {
  const hasCopy = Boolean(standard && command?.trim());
  const compact = density === "compact";
  return (
    <div
      className={cn(
        "flex min-w-0 flex-col",
        compact ? "gap-1" : "gap-1.5",
        bordered &&
          (compact
            ? "rounded-[var(--exits-radius-sm)] border border-border bg-[var(--exits-surface)] p-1.5"
            : "rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)]/40 p-2"),
        className,
      )}
      data-testid={testId}
      data-ui-standards-sample={explanatory ? "explanatory" : "implementable"}
      data-has-copy={hasCopy ? "true" : "false"}
      data-density={density}
    >
      <span className="text-[length:var(--exits-text-xs)] uppercase tracking-wide text-muted">{label}</span>
      <div className={cn("min-w-0", contentClassName)}>{children}</div>
      {hint ? <span className="text-[length:var(--exits-text-xs)] text-muted">{hint}</span> : null}
      <UiStandardsSampleCommandFooter
        standard={standard}
        command={command}
        commandContext={commandContext}
        standardStatus={standardStatus}
      />
    </div>
  );
}
