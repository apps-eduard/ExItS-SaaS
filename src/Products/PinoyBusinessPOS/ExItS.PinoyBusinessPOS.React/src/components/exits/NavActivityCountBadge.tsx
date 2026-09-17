import { CountBadge } from "@/components/exits/CountChip";
import { cn } from "@/lib/cn";

type NavActivityCountBadgeProps = {
  /** Already gated: only pass when count > 0. */
  display: string;
  /** Selected/active nav row — keep Primary readable on primary-soft. */
  selected?: boolean;
  className?: string;
  testId?: string;
};

/**
 * Generic nav presentation for optional module activity counts.
 * Domain layers supply `display`; this only places CountBadge (PRIMARY).
 */
export function NavActivityCountBadge({
  display,
  selected = false,
  className,
  testId,
}: NavActivityCountBadgeProps) {
  return (
    <span className={cn("inline-flex shrink-0", className)} data-testid={testId} aria-hidden>
      <CountBadge
        count={display}
        tone="primary"
        className={
          selected
            ? "border-[color-mix(in_srgb,var(--exits-primary)_45%,transparent)] bg-[color-mix(in_srgb,var(--exits-primary)_22%,transparent)] text-[var(--exits-primary)]"
            : undefined
        }
      />
    </span>
  );
}
