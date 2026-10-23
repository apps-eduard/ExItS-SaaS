import type { ButtonHTMLAttributes, ReactNode } from "react";
import { ChevronRight, type LucideIcon } from "lucide-react";
import {
  softGlassQaTrailing,
  type SoftGlassIdentity,
  type SoftGlassQaTrailing,
} from "@/features/ui-standards/soft-glass-card-global-default";
import { cn } from "@/lib/cn";

export type SoftGlassQuickActionProps = Omit<
  ButtonHTMLAttributes<HTMLButtonElement>,
  "children"
> & {
  identity: SoftGlassIdentity;
  label: ReactNode;
  icon: LucideIcon;
  /**
   * When set (including `0`), trailing is the Soft Glass count badge.
   * Otherwise use `trailing` or default chevron.
   */
  count?: number | null;
  /** Force trailing when not using count. Default: chevron. */
  trailing?: SoftGlassQaTrailing;
  testId?: string;
  className?: string;
};

/**
 * Locked Soft Glass Quick Action row:
 * icon chip · label · trailing (count badge | chevron | add).
 */
export function SoftGlassQuickAction({
  identity,
  label,
  icon: Icon,
  count = null,
  trailing,
  testId,
  className,
  type = "button",
  ...props
}: SoftGlassQuickActionProps) {
  const end = softGlassQaTrailing({ count, trailing });

  return (
    <button
      type={type}
      className={cn(
        "personal-qa-glass",
        `personal-qa-glass--${identity}`,
        end === "count" && "personal-qa-glass--with-count",
        className,
      )}
      data-shell="soft-glass"
      data-identity={identity}
      data-trailing={end}
      data-testid={testId}
      {...props}
    >
      <span className="personal-qa-glass__icon" aria-hidden="true">
        <Icon className="size-[0.95rem] shrink-0" aria-hidden />
      </span>
      <span className="personal-qa-glass__label">{label}</span>
      {end === "count" ? (
        <strong className="personal-qa-glass__badge" aria-hidden="true">
          {count ?? 0}
        </strong>
      ) : end === "add" ? (
        <span
          className={cn("personal-qa-glass__plus", `personal-qa-glass__plus--${identity}`)}
          aria-hidden="true"
        >
          +
        </span>
      ) : (
        <ChevronRight className="personal-qa-glass__chevron" aria-hidden />
      )}
    </button>
  );
}
