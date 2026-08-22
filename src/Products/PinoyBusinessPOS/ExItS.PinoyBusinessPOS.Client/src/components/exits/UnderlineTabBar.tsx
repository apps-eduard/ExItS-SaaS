import type { LucideIcon } from "lucide-react";
import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

export type UnderlineTabItem = {
  key: string;
  label: ReactNode;
  icon?: LucideIcon;
  testId?: string;
  disabled?: boolean;
};

type UnderlineTabBarProps = {
  items: ReadonlyArray<UnderlineTabItem>;
  activeKey: string;
  onChange: (key: string) => void;
  ariaLabel: string;
  testId?: string;
  className?: string;
};

/**
 * Horizontal tabs with icon+label and primary underline on the active item.
 * Matches auth/settings-style navigation (inactive muted text, active primary + border).
 */
export function UnderlineTabBar({
  items,
  activeKey,
  onChange,
  ariaLabel,
  testId,
  className,
}: UnderlineTabBarProps) {
  return (
    <div
      className={cn("flex min-w-0 flex-wrap gap-x-1 border-b border-border", className)}
      role="tablist"
      aria-label={ariaLabel}
      data-testid={testId}
    >
      {items.map((item) => {
        const active = activeKey === item.key;
        const Icon = item.icon;
        return (
          <button
            key={item.key}
            type="button"
            role="tab"
            aria-selected={active}
            disabled={item.disabled}
            data-testid={item.testId}
            className={cn(
              "inline-flex min-h-11 items-center gap-2 border-b-[3px] px-3 pb-2.5 pt-1 text-[length:var(--exits-text-sm)] font-semibold transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:opacity-50",
              active
                ? "border-primary text-primary"
                : "border-transparent text-muted hover:text-foreground",
            )}
            onClick={() => onChange(item.key)}
          >
            {Icon ? <Icon className="size-5 shrink-0" aria-hidden /> : null}
            <span>{item.label}</span>
          </button>
        );
      })}
    </div>
  );
}
