import type { ReactNode } from "react";
import { cn } from "@/lib/cn";
import { UiStandardsSampleCard } from "@/features/ui-standards/UiStandardsSampleCard";
import type { UiStandardsStandardName } from "@/features/ui-standards/UiStandardsCopyCommand";

export type SimpleV2SampleProps = {
  /** Row label under the Cursor command (e.g. Default, Pills, Icons). */
  label: string;
  command: string;
  standard: UiStandardsStandardName | UiStandardsStandardName[];
  standardStatus?: "locked" | "pilot";
  children: ReactNode;
  testId?: string;
  commandContext?: string;
  contentClassName?: string;
};

/**
 * Simple V2 sample: Cursor command first, visual row below (matches Tag reference layout).
 */
export function SimpleV2Sample({
  label,
  command,
  standard,
  standardStatus = "locked",
  children,
  testId,
  commandContext,
  contentClassName = "flex min-w-0 flex-wrap items-center gap-2",
}: SimpleV2SampleProps) {
  return (
    <UiStandardsSampleCard
      label={label}
      density="compact"
      commandPlacement="above"
      bordered={false}
      standard={standard}
      standardStatus={standardStatus}
      command={command}
      commandContext={commandContext}
      testId={testId}
      contentClassName={contentClassName}
      labelClassName="text-[length:var(--exits-text-sm)] font-medium normal-case tracking-normal text-foreground"
      className="gap-2 border-0 bg-transparent p-0"
    >
      {children}
    </UiStandardsSampleCard>
  );
}

/** Compact category heading inside a showcase (e.g. DEFAULT, SHAPES). */
export function SimpleV2Group({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="grid min-w-0 gap-3">
      <h4 className="m-0 text-[length:var(--exits-text-xs)] font-medium uppercase tracking-wide text-muted">
        {title}
      </h4>
      <div className="grid min-w-0 gap-4">{children}</div>
    </div>
  );
}

/** Component showcase: title + LOCKED/PILOT + visual rows (command sits above each row). */
export function SimpleV2Showcase({
  title,
  status,
  children,
  testId,
  id,
  note,
}: {
  title: string;
  status?: "LOCKED" | "PILOT";
  children: ReactNode;
  testId?: string;
  id?: string;
  note?: string;
}) {
  return (
    <section
      id={id}
      data-testid={testId}
      className="min-w-0 rounded-[var(--exits-radius-md)] border border-border bg-surface p-3 sm:p-4"
    >
      <div className="mb-3 flex min-w-0 flex-wrap items-center gap-2">
        <h3 className="m-0 text-[length:var(--exits-type-section-title)] font-semibold text-foreground">
          {title}
        </h3>
        {status ? (
          <span
            className={cn(
              "rounded-full px-1.5 py-0.5 text-[0.625rem] font-semibold uppercase tracking-wide",
              status === "PILOT"
                ? "bg-[color-mix(in_srgb,var(--exits-warning)_16%,transparent)] text-[var(--exits-warning)]"
                : "bg-[color-mix(in_srgb,var(--exits-success)_14%,transparent)] text-[var(--exits-success)]",
            )}
          >
            {status}
          </span>
        ) : null}
      </div>
      {note ? <p className="m-0 mb-3 text-[length:var(--exits-text-xs)] text-muted">{note}</p> : null}
      <div className="grid min-w-0 gap-5">{children}</div>
    </section>
  );
}
