import type { ReactNode } from "react";
import { AlertTriangle, CircleAlert, CircleCheck, Info } from "lucide-react";
import { cn } from "@/lib/cn";

export type NoticeTone = "info" | "warning" | "danger" | "success";

export type NoticeProps = {
  /** Body copy. Optional when `title` alone is enough. */
  children?: ReactNode;
  tone?: NoticeTone;
  /** Optional short heading above the body. */
  title?: string;
  /** Override the default tone icon. Pass `null` to hide. */
  icon?: ReactNode | null;
  /** Optional trailing / stacked action (permission-gated by caller). */
  action?: ReactNode;
  className?: string;
  testId?: string;
};

const TONE_ICON: Record<NoticeTone, typeof Info> = {
  info: Info,
  warning: AlertTriangle,
  danger: CircleAlert,
  success: CircleCheck,
};

/**
 * Compact contextual notice (APPROVED / LOCKED).
 * INFO / WARNING / DANGER / SUCCESS — not an EmptyState and not a page ErrorState.
 */
export function Notice({
  children,
  tone = "info",
  title,
  icon,
  action,
  className,
  testId,
}: NoticeProps) {
  const DefaultIcon = TONE_ICON[tone];
  const showIcon = icon !== null;
  const role = tone === "danger" ? "alert" : "status";
  const live = tone === "danger" ? "assertive" : "polite";

  return (
    <div
      role={role}
      aria-live={live}
      data-tone={tone}
      data-testid={testId ?? "exits-notice"}
      className={cn(
        "exits-notice flex min-w-0 items-start gap-2 px-3 py-2.5",
        "rounded-[var(--exits-radius-md)] border border-solid text-[length:var(--exits-text-sm)]",
        tone === "info" &&
          "border-[color-mix(in_srgb,var(--exits-info)_40%,var(--exits-border))] bg-[color-mix(in_srgb,var(--exits-info)_10%,var(--exits-surface))] text-foreground",
        tone === "warning" &&
          "border-[color-mix(in_srgb,var(--exits-warning)_50%,var(--exits-border))] bg-[color-mix(in_srgb,var(--exits-warning)_14%,var(--exits-surface))] text-foreground",
        tone === "danger" &&
          "border-[color-mix(in_srgb,var(--exits-danger)_45%,var(--exits-border))] bg-[color-mix(in_srgb,var(--exits-danger)_10%,var(--exits-surface))] text-[var(--exits-danger)]",
        tone === "success" &&
          "border-[color-mix(in_srgb,var(--exits-success)_45%,var(--exits-border))] bg-[color-mix(in_srgb,var(--exits-success)_10%,var(--exits-surface))] text-[var(--exits-success)]",
        className,
      )}
    >
      {showIcon ? (
        <span
          className={cn(
            "mt-0.5 inline-flex size-4 shrink-0 items-center justify-center [&_svg]:size-full",
            tone === "info" && "text-[var(--exits-info)]",
            tone === "warning" && "text-[var(--exits-warning)]",
            tone === "danger" && "text-[var(--exits-danger)]",
            tone === "success" && "text-[var(--exits-success)]",
          )}
          aria-hidden
        >
          {icon ?? <DefaultIcon strokeWidth={2} />}
        </span>
      ) : null}
      <div className="flex min-w-0 flex-1 flex-col gap-1.5">
        {title ? (
          <p className="m-0 font-semibold leading-snug text-inherit">{title}</p>
        ) : null}
        {children != null && children !== "" ? (
          <div className="m-0 leading-snug text-inherit [&_p]:m-0">{children}</div>
        ) : null}
        {action ? <div className="mt-0.5">{action}</div> : null}
      </div>
    </div>
  );
}
