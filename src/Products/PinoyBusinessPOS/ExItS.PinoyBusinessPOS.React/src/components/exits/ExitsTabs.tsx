import {
  useCallback,
  useEffect,
  useId,
  useRef,
  type KeyboardEvent,
  type ReactNode,
} from "react";
import type { LucideIcon } from "lucide-react";
import { cn } from "@/lib/cn";
import { CountBadge } from "@/components/exits/CountChip";
import type { ChipTone } from "@/components/exits/chip-variants";
import {
  exitsTabTriggerVariants,
  exitsTabsListVariants,
  exitsTabsPanelVariants,
  type ExitsTabsActiveTreatment,
  type ExitsTabsLayout,
  type ExitsTabsVariant,
} from "@/components/exits/tab-variants";

export type { ExitsTabsVariant, ExitsTabsLayout, ExitsTabsActiveTreatment };

export type ExitsTabCountTone = Extract<ChipTone, "neutral" | "primary" | "danger" | "warning">;

export type ExitsTabItem = {
  key: string;
  label: ReactNode;
  icon?: LucideIcon;
  count?: ReactNode;
  /** Explicit count tone. Default: neutral (callers may pass primary when selected). */
  countTone?: ExitsTabCountTone;
  disabled?: boolean;
  /** Show a quiet count placeholder while “loading” (demo only). */
  loadingCount?: boolean;
  /** Tooltip / title — required for icon-only and useful when truncated. */
  title?: string;
  testId?: string;
};

export type ExitsTabsProps = {
  variant?: ExitsTabsVariant;
  items: ReadonlyArray<ExitsTabItem>;
  value: string;
  onValueChange: (key: string) => void;
  ariaLabel: string;
  /** Horizontal (or vertical) overflow scroll for many tabs. */
  scrollable?: boolean;
  /** Icon-only compact tool tabs — SPECIAL USE. */
  iconOnly?: boolean;
  /** Equal shares width vs content-sized (esp. pillBar). */
  layout?: ExitsTabsLayout;
  /** Pill-bar active pill treatment candidate — PILOT / NOT LOCKED. */
  activeTreatment?: ExitsTabsActiveTreatment;
  className?: string;
  listClassName?: string;
  testId?: string;
  /** Optional panel content keyed by tab key. */
  panels?: Readonly<Record<string, ReactNode>>;
  children?: ReactNode;
};

/**
 * ExItS Tabs visual pilot foundation (PILOT / NOT LOCKED).
 * Owns presentation + local keyboard/ARIA. Pages own routing and data.
 */
export function ExitsTabs({
  variant = "underline",
  items,
  value,
  onValueChange,
  ariaLabel,
  scrollable = false,
  iconOnly = false,
  layout = "content",
  activeTreatment = "solid",
  className,
  listClassName,
  testId,
  panels,
  children,
}: ExitsTabsProps) {
  const reactId = useId();
  const listRef = useRef<HTMLDivElement>(null);
  const orientation = variant === "vertical" ? "vertical" : "horizontal";

  const enabledKeys = items.filter((i) => !i.disabled).map((i) => i.key);

  const focusTab = useCallback((key: string) => {
    const root = listRef.current;
    if (!root) return;
    const el = root.querySelector<HTMLElement>(`[data-tab-key="${key}"]`);
    el?.focus();
  }, []);

  useEffect(() => {
    if (!scrollable || !listRef.current) return;
    const selected = listRef.current.querySelector<HTMLElement>('[data-selected="true"]');
    if (selected && typeof selected.scrollIntoView === "function") {
      selected.scrollIntoView({ inline: "nearest", block: "nearest", behavior: "smooth" });
    }
  }, [value, scrollable]);

  function onListKeyDown(e: KeyboardEvent<HTMLDivElement>) {
    if (enabledKeys.length === 0) return;
    const currentIndex = Math.max(0, enabledKeys.indexOf(value));
    const isRtl =
      typeof document !== "undefined" && document.documentElement.dir === "rtl";

    let nextIndex: number | null = null;
    if (orientation === "horizontal") {
      if (e.key === "ArrowRight") nextIndex = currentIndex + (isRtl ? -1 : 1);
      else if (e.key === "ArrowLeft") nextIndex = currentIndex + (isRtl ? 1 : -1);
    } else {
      if (e.key === "ArrowDown") nextIndex = currentIndex + 1;
      else if (e.key === "ArrowUp") nextIndex = currentIndex - 1;
    }
    if (e.key === "Home") nextIndex = 0;
    if (e.key === "End") nextIndex = enabledKeys.length - 1;

    if (nextIndex == null) return;
    e.preventDefault();
    const wrapped = (nextIndex + enabledKeys.length) % enabledKeys.length;
    const nextKey = enabledKeys[wrapped]!;
    onValueChange(nextKey);
    focusTab(nextKey);
  }

  const panelId = (key: string) => `${reactId}-panel-${key}`;
  const tabId = (key: string) => `${reactId}-tab-${key}`;

  const list = (
    <div
      ref={listRef}
      role="tablist"
      aria-label={ariaLabel}
      aria-orientation={orientation}
      data-variant={variant}
      data-layout={layout}
      data-active-treatment={variant === "pillBar" ? activeTreatment : undefined}
      data-testid={testId ? `${testId}-list` : undefined}
      className={cn(exitsTabsListVariants({ variant, scrollable, layout }), listClassName)}
      onKeyDown={onListKeyDown}
    >
      {items.map((item) => {
        const selected = value === item.key;
        const Icon = item.icon;
        const countTone = item.countTone ?? "neutral";
        const title =
          item.title ?? (iconOnly && typeof item.label === "string" ? item.label : undefined);
        const solidSelected = variant === "pillBar" && activeTreatment === "solid" && selected;

        return (
          <button
            key={item.key}
            type="button"
            role="tab"
            id={tabId(item.key)}
            data-tab-key={item.key}
            data-selected={selected ? "true" : "false"}
            data-testid={item.testId}
            aria-selected={selected}
            aria-controls={panelId(item.key)}
            aria-disabled={item.disabled || undefined}
            disabled={item.disabled}
            tabIndex={selected ? 0 : -1}
            title={title}
            aria-label={iconOnly && typeof item.label === "string" ? item.label : undefined}
            className={exitsTabTriggerVariants({
              variant,
              iconOnly,
              layout,
              activeTreatment: variant === "pillBar" ? activeTreatment : "solid",
            })}
            onClick={() => {
              if (!item.disabled) onValueChange(item.key);
            }}
          >
            {Icon ? (
              <Icon className="size-[0.9375rem] shrink-0 opacity-90" aria-hidden strokeWidth={2} />
            ) : null}
            {!iconOnly ? <span className="min-w-0 truncate">{item.label}</span> : null}
            {item.loadingCount ? (
              <span
                className="inline-flex h-[1.25rem] min-w-[1.25rem] items-center justify-center rounded-full bg-[var(--exits-surface-muted)] px-1.5 text-[0.6875rem] text-muted"
                aria-hidden
              >
                ···
              </span>
            ) : item.count != null ? (
              <CountBadge
                count={item.count}
                tone={countTone}
                className={
                  solidSelected
                    ? "border-[color-mix(in_srgb,var(--exits-primary-contrast)_35%,transparent)] bg-[color-mix(in_srgb,var(--exits-primary-contrast)_18%,transparent)] text-[var(--exits-primary-contrast)]"
                    : undefined
                }
              />
            ) : null}
          </button>
        );
      })}
    </div>
  );

  const showPanel = panels != null || children != null;

  return (
    <div
      className={cn(
        "exits-tabs min-w-0",
        variant === "vertical" && "flex items-stretch gap-0",
        className,
      )}
      data-testid={testId}
      data-variant={variant}
    >
      {list}

      {showPanel
        ? items.map((item) => {
            const selected = value === item.key;
            const content = panels?.[item.key] ?? (selected ? children : null);
            if (!selected) {
              return (
                <div
                  key={item.key}
                  role="tabpanel"
                  id={panelId(item.key)}
                  aria-labelledby={tabId(item.key)}
                  hidden
                  className="hidden"
                />
              );
            }
            return (
              <div
                key={item.key}
                role="tabpanel"
                id={panelId(item.key)}
                aria-labelledby={tabId(item.key)}
                className={exitsTabsPanelVariants({ variant })}
                data-testid={testId ? `${testId}-panel-${item.key}` : undefined}
              >
                {content}
              </div>
            );
          })
        : null}
    </div>
  );
}
