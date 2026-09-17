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
  /** Route path for real navigation (NavLink). Also used as href in controlled demos. */
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
  /**
   * Controlled current item key (UI Standards demos).
   * When set, renders plain links that call `onValueChange` — no nested Router.
   */
  value?: string;
  onValueChange?: (key: string) => void;
  className?: string;
  listClassName?: string;
  testId?: string;
};

function itemClassName(
  variant: ModuleSubnavVariant,
  layout: ModuleSubnavLayout,
  activeTreatment: ModuleSubnavActiveTreatment,
  isActive: boolean,
  solidCurrent: boolean,
) {
  return cn(
    moduleSubnavItemVariants({
      variant,
      layout,
      activeTreatment: variant === "pillBar" || variant === "pill" ? activeTreatment : "soft",
    }),
    isActive && solidCurrent ? "exits-module-subnav__item--solid-active" : null,
  );
}

function ItemBody({
  Icon,
  label,
  count,
  countTone,
  isActive,
  solidCurrent,
}: {
  Icon?: LucideIcon;
  label: ReactNode;
  count?: ReactNode;
  countTone: ModuleSubnavCountTone;
  isActive: boolean;
  solidCurrent: boolean;
}) {
  return (
    <>
      {Icon ? (
        <Icon className="size-[0.9375rem] shrink-0 opacity-90" aria-hidden strokeWidth={2} />
      ) : null}
      <span className="min-w-0 truncate">{label}</span>
      {count != null ? (
        <CountBadge
          count={count}
          tone={isActive && solidCurrent ? "neutral" : countTone}
          className={
            isActive && solidCurrent
              ? "border-[color-mix(in_srgb,var(--exits-primary-contrast)_35%,transparent)] bg-[color-mix(in_srgb,var(--exits-primary-contrast)_18%,transparent)] text-[var(--exits-primary-contrast)]"
              : undefined
          }
        />
      ) : null}
    </>
  );
}

/**
 * ExItS Module Subnav (PILOT).
 * Related-route navigation inside one module — nav/link semantics, NOT Tabs.
 * Does not fetch data or mount destination pages. Counts/icons are props only.
 *
 * Production: omit `value` → uses React Router `NavLink`.
 * UI Standards demos: pass `value` + `onValueChange` to avoid nesting Routers.
 */
export function ModuleSubnav({
  variant = "soft",
  items,
  ariaLabel,
  layout = "content",
  activeTreatment = "solid",
  scrollable = false,
  value,
  onValueChange,
  className,
  listClassName,
  testId,
}: ModuleSubnavProps) {
  const listRef = useRef<HTMLDivElement>(null);
  const controlled = value !== undefined;

  useEffect(() => {
    if (!scrollable || !listRef.current) return;
    const current = listRef.current.querySelector<HTMLElement>('[aria-current="page"]');
    if (!current || typeof current.scrollIntoView !== "function") return;
    current.scrollIntoView({
      inline: "nearest",
      block: "nearest",
      behavior: prefersReducedMotion() ? "auto" : "smooth",
    });
  }, [scrollable, items, value]);

  return (
    <nav
      aria-label={ariaLabel}
      data-testid={testId}
      data-variant={variant}
      data-controlled={controlled ? "true" : undefined}
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
            (variant === "pillBar" || variant === "pill") && activeTreatment === "solid";

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
                  activeTreatment:
                    variant === "pillBar" || variant === "pill" ? activeTreatment : "soft",
                })}
              >
                <ItemBody
                  Icon={Icon}
                  label={item.label}
                  count={item.count}
                  countTone={countTone}
                  isActive={false}
                  solidCurrent={false}
                />
              </span>
            );
          }

          if (controlled) {
            const isActive = value === item.key;
            return (
              <a
                key={item.key}
                href={item.to}
                title={item.title}
                data-testid={item.testId}
                aria-current={isActive ? "page" : undefined}
                className={itemClassName(variant, layout, activeTreatment, isActive, solidCurrent)}
                onClick={(event) => {
                  event.preventDefault();
                  onValueChange?.(item.key);
                }}
              >
                <ItemBody
                  Icon={Icon}
                  label={item.label}
                  count={item.count}
                  countTone={countTone}
                  isActive={isActive}
                  solidCurrent={solidCurrent}
                />
              </a>
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
                itemClassName(variant, layout, activeTreatment, isActive, solidCurrent)
              }
            >
              {({ isActive }) => (
                <ItemBody
                  Icon={Icon}
                  label={item.label}
                  count={item.count}
                  countTone={countTone}
                  isActive={isActive}
                  solidCurrent={solidCurrent}
                />
              )}
            </NavLink>
          );
        })}
      </div>
    </nav>
  );
}

export type { ModuleSubnavVariant, ModuleSubnavLayout, ModuleSubnavActiveTreatment };
