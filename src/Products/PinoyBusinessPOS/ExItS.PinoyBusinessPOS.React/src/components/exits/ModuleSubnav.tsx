import { useEffect, useRef, type ReactNode } from "react";
import { NavLink } from "react-router-dom";
import type { LucideIcon } from "lucide-react";
import { CountBadge } from "@/components/exits/CountChip";
import type { ChipTone } from "@/components/exits/chip-variants";
import {
  moduleSubnavItemVariants,
  moduleSubnavListVariants,
  type ModuleSubnavActiveTreatment,
  type ModuleSubnavLayout,
  type ModuleSubnavVariant,
} from "@/components/exits/module-subnav-variants";
import { cn } from "@/lib/cn";
import { prefersReducedMotion } from "@/lib/motion";

export type ModuleSubnavCountTone = Extract<ChipTone, "neutral" | "primary" | "danger" | "warning">;

export type ModuleSubnavItem = {
  key: string;
  label: ReactNode;
  /** Route path — required for real navigation. Demo may use absolute paths under MemoryRouter. */
  to: string;
  icon?: LucideIcon;
  count?: ReactNode;
  countTone?: ModuleSubnavCountTone;
  disabled?: boolean;
  end?: boolean;
  title?: string;
  testId?: string;
};

export type ModuleSubnavProps = {
  variant?: ModuleSubnavVariant;
  items: ReadonlyArray<ModuleSubnavItem>;
  ariaLabel: string;
  /** Equal shares vs content-sized (esp. pillBar / soft). */
  layout?: ModuleSubnavLayout;
  /** Pill / pillBar active treatment (default solid). */
  activeTreatment?: ModuleSubnavActiveTreatment;
  /** Horizontal (or vertical) overflow scroll. */
  scrollable?: boolean;
  className?: string;
  listClassName?: string;
  testId?: string;
};

/**
 * ExItS Module Subnav (PILOT).
 * Related-route navigation inside one module — nav/link semantics, NOT Tabs.
 * Does not fetch data or mount destination pages. Counts/icons are props only.
 */
export function ModuleSubnav({
  variant = "soft",
  items,
  ariaLabel,
  layout = "content",
  activeTreatment = "solid",
  scrollable = false,
  className,
  listClassName,
  testId,
}: ModuleSubnavProps) {
  const listRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!scrollable || !listRef.current) return;
    const current = listRef.current.querySelector<HTMLElement>('[aria-current="page"]');
    if (!current || typeof current.scrollIntoView !== "function") return;
    current.scrollIntoView({
      inline: "nearest",
      block: "nearest",
      behavior: prefersReducedMotion() ? "auto" : "smooth",
    });
  }, [scrollable, items]);

  return (
    <nav
      aria-label={ariaLabel}
      data-testid={testId}
      data-variant={variant}
      className={cn("exits-module-subnav min-w-0", className)}
    >
      <div
        ref={listRef}
        data-layout={layout}
        data-active-treatment={
          variant === "pillBar" || variant === "pill" ? activeTreatment : undefined
        }
        data-testid={testId ? `${testId}-list` : undefined}
        className={cn(moduleSubnavListVariants({ variant, scrollable, layout }), listClassName)}
      >
        {items.map((item) => {
          const Icon = item.icon;
          const countTone = item.countTone ?? "neutral";
          const solidCurrent =
            (variant === "pillBar" || variant === "pill") &&
            activeTreatment === "solid";

          if (item.disabled) {
            return (
              <span
                key={item.key}
                aria-disabled="true"
                data-testid={item.testId}
                title={item.title}
                className={moduleSubnavItemVariants({
                  variant,
                  layout,
                  activeTreatment: variant === "pillBar" || variant === "pill" ? activeTreatment : "soft",
                })}
              >
                {Icon ? (
                  <Icon className="size-[0.9375rem] shrink-0 opacity-90" aria-hidden strokeWidth={2} />
                ) : null}
                <span className="min-w-0 truncate">{item.label}</span>
                {item.count != null ? <CountBadge count={item.count} tone={countTone} /> : null}
              </span>
            );
          }

          return (
            <NavLink
              key={item.key}
              to={item.to}
              end={item.end}
              title={item.title}
              data-testid={item.testId}
              className={({ isActive }) =>
                cn(
                  moduleSubnavItemVariants({
                    variant,
                    layout,
                    activeTreatment:
                      variant === "pillBar" || variant === "pill" ? activeTreatment : "soft",
                  }),
                  isActive && solidCurrent ? "exits-module-subnav__item--solid-active" : null,
                )
              }
            >
              {({ isActive }) => (
                <>
                  {Icon ? (
                    <Icon
                      className="size-[0.9375rem] shrink-0 opacity-90"
                      aria-hidden
                      strokeWidth={2}
                    />
                  ) : null}
                  <span className="min-w-0 truncate">{item.label}</span>
                  {item.count != null ? (
                    <CountBadge
                      count={item.count}
                      tone={isActive && solidCurrent ? "neutral" : countTone}
                      className={
                        isActive && solidCurrent
                          ? "border-[color-mix(in_srgb,var(--exits-primary-contrast)_35%,transparent)] bg-[color-mix(in_srgb,var(--exits-primary-contrast)_18%,transparent)] text-[var(--exits-primary-contrast)]"
                          : undefined
                      }
                    />
                  ) : null}
                </>
              )}
            </NavLink>
          );
        })}
      </div>
    </nav>
  );
}

export type { ModuleSubnavVariant, ModuleSubnavLayout, ModuleSubnavActiveTreatment };
