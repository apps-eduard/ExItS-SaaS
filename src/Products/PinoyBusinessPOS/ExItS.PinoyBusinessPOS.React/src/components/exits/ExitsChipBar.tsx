import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import { cn } from "@/lib/cn";
import { CountBadge } from "@/components/exits/CountChip";

export type ExitsChipItem = {
  key: string;
  label: ReactNode;
  icon?: ReactNode;
  /**
   * Authoritative contextual count. `0` renders; `undefined`/`null` omits the badge
   * (unknown / unavailable — never fake zero).
   */
  count?: number | null;
  /** Visual state for flow/status chips. */
  state?: "idle" | "active" | "done";
  /** Accent for primary actions (e.g. Add supplier) — density-aware like filters. */
  emphasis?: "default" | "primary";
  testId?: string;
  disabled?: boolean;
  onSelect?: () => void;
  /** When set, renders a density-aware chip link instead of a button. */
  href?: string;
};

type ExitsChipBarProps = {
  items: ReadonlyArray<ExitsChipItem>;
  ariaLabel: string;
  testId?: string;
  className?: string;
  /** `steps` = numbered flow; `filter` = status chips; `actions` = nav/action chips. */
  variant?: "steps" | "filter" | "actions";
};

function chipClassName(item: ExitsChipItem): string {
  const state = item.state ?? "idle";
  return cn(
    "exits-chip",
    state === "active" && "exits-chip--active",
    state === "done" && "exits-chip--done",
    item.emphasis === "primary" && "exits-chip--primary",
    item.disabled && "exits-chip--disabled",
  );
}

function chipAccessibleName(item: ExitsChipItem): string | undefined {
  if (typeof item.label !== "string") {
    return undefined;
  }
  if (item.count == null) {
    return undefined;
  }
  return `${item.label}, ${item.count}`;
}

function ChipContent({
  item,
  index,
  showStepIndex,
}: {
  item: ExitsChipItem;
  index: number;
  showStepIndex: boolean;
}) {
  const active = (item.state ?? "idle") === "active";
  return (
    <>
      {showStepIndex ? (
        <span className="exits-chip__index" aria-hidden>
          {index + 1}
        </span>
      ) : null}
      {item.icon ? (
        <span className="exits-chip__icon" aria-hidden>
          {item.icon}
        </span>
      ) : null}
      <span className="exits-chip__label">{item.label}</span>
      {item.count != null ? (
        <CountBadge count={item.count} tone={active ? "primary" : "neutral"} />
      ) : null}
    </>
  );
}

/**
 * Shared chip bar for flow steps, filters, and action links.
 * Size comes from density tokens (`--exits-chip-*`); default density is balance.
 * Filter chips are the global tab selector (Your stores, notifications, auth, reports).
 */
export function ExitsChipBar({
  items,
  ariaLabel,
  testId,
  className,
  variant = "filter",
}: ExitsChipBarProps) {
  const role = variant === "steps" ? "list" : variant === "actions" ? "toolbar" : "tablist";
  const showStepIndex = variant === "steps";

  return (
    <div
      role={role}
      aria-label={ariaLabel}
      data-testid={testId}
      className={cn("exits-chip-bar", `exits-chip-bar--${variant}`, "exits-animate-toolbar", className)}
    >
      {items.map((item, index) => {
        const classNameChip = chipClassName(item);
        const content = (
          <ChipContent item={item} index={index} showStepIndex={showStepIndex} />
        );
        const accessibleName = chipAccessibleName(item);

        if (item.href && !item.disabled) {
          return (
            <Link
              key={item.key}
              to={item.href}
              role={variant === "filter" ? "tab" : undefined}
              aria-selected={
                variant === "filter" ? (item.state ?? "idle") === "active" : undefined
              }
              aria-label={accessibleName}
              data-testid={item.testId}
              className={classNameChip}
            >
              {content}
            </Link>
          );
        }

        if (typeof item.onSelect === "function") {
          return (
            <button
              key={item.key}
              type="button"
              role={variant === "steps" ? "listitem" : variant === "filter" ? "tab" : undefined}
              aria-selected={variant === "filter" ? (item.state ?? "idle") === "active" : undefined}
              aria-current={
                variant === "steps" && (item.state ?? "idle") === "active" ? "step" : undefined
              }
              aria-label={accessibleName}
              disabled={item.disabled}
              data-testid={item.testId}
              className={classNameChip}
              onClick={item.onSelect}
            >
              {content}
            </button>
          );
        }

        return (
          <span
            key={item.key}
            role={variant === "steps" ? "listitem" : undefined}
            aria-current={
              variant === "steps" && (item.state ?? "idle") === "active" ? "step" : undefined
            }
            aria-label={accessibleName}
            data-testid={item.testId}
            className={classNameChip}
          >
            {content}
          </span>
        );
      })}
    </div>
  );
}
