import { type ReactNode } from "react";
import { Link } from "react-router-dom";
import { ChevronDown, Loader2, type LucideIcon } from "lucide-react";
import { CountBadge } from "@/components/exits/CountChip";
import {
  actionChipGroupVariants,
  actionChipItemVariants,
  type ActionChipGroupLayout,
  type ActionChipShape,
  type ActionChipTone,
  type ActionChipVisual,
} from "@/components/exits/action-chip-variants";
import {
  DropdownMenu,
  MenuItem,
  useDismissibleOpen,
} from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/cn";

export type ActionChipCountTone = Extract<ActionChipTone, "neutral" | "primary" | "danger" | "warning">;

export type ActionChipItem = {
  key: string;
  label: ReactNode;
  /** Leading icon (recommended for workflow shortcuts). */
  icon?: LucideIcon;
  /** Trailing icon (directional / forward actions). */
  trailingIcon?: LucideIcon;
  /** Icon-only utility — requires ariaLabel. */
  iconOnly?: boolean;
  ariaLabel?: string;
  title?: string;
  /** Navigation Action Chip — React Router Link. */
  href?: string;
  /** Action Chip — native button. */
  onSelect?: () => void;
  /**
   * Lightweight primary emphasis among peers.
   * Maps to tintedPrimary (default) or solidPrimary when bar primaryTreatment is solid.
   */
  emphasis?: "default" | "primary";
  /** Per-item visual override. */
  visual?: ActionChipVisual;
  /** Semantic tint — not Status Chip; use sparingly. */
  tone?: ActionChipTone;
  count?: ReactNode;
  countTone?: ActionChipCountTone;
  /** Default hide for nav/workflow shortcuts; show for explicit comparison. */
  countZeroMode?: "show" | "hide";
  disabled?: boolean;
  loading?: boolean;
  testId?: string;
};

export type ActionChipSection = {
  id: string;
  label: string;
  itemKeys: ReadonlyArray<string>;
};

export type ActionChipBarProps = {
  items: ReadonlyArray<ActionChipItem>;
  ariaLabel: string;
  /** Default visual for items without override. Production baseline ≈ outline/soft surface. */
  variant?: ActionChipVisual;
  shape?: ActionChipShape;
  layout?: ActionChipGroupLayout;
  /** How emphasis="primary" renders. Default tinted (matches production ExitsChipBar). */
  primaryTreatment?: "tinted" | "solid";
  fullWidthMobile?: boolean;
  /**
   * toolbar = action toolbar (default for actionable groups).
   * group = generic grouping without toolbar keyboard expectations.
   * none = no role (visual-only wrapper; children keep button/link semantics).
   */
  groupRole?: "toolbar" | "group" | "none";
  /** Sectioned layout candidate — only when enough actions justify categories. */
  sections?: ReadonlyArray<ActionChipSection>;
  /**
   * Overflow / More candidate: keep first N visible, rest in dropdown.
   * Never hide primary/critical actions — caller responsibility.
   */
  overflowAfter?: number;
  /** Trailing utility cluster rendered at inline-end (Refresh, More). */
  trailingItems?: ReadonlyArray<ActionChipItem>;
  className?: string;
  listClassName?: string;
  testId?: string;
};

function resolveVisual(
  item: ActionChipItem,
  barVariant: ActionChipVisual,
  primaryTreatment: "tinted" | "solid",
): ActionChipVisual {
  if (item.visual) return item.visual;
  if (item.emphasis === "primary") {
    return primaryTreatment === "solid" ? "solidPrimary" : "tintedPrimary";
  }
  return barVariant;
}

function shouldShowCount(item: ActionChipItem): boolean {
  if (item.count == null) return false;
  const mode = item.countZeroMode ?? "hide";
  if (mode === "show") return true;
  if (typeof item.count === "number") return item.count !== 0;
  if (typeof item.count === "string") {
    const trimmed = item.count.trim();
    return trimmed !== "" && trimmed !== "0";
  }
  return true;
}

function renderIcon(Icon: LucideIcon | undefined, className?: string) {
  if (!Icon) return null;
  return <Icon className={cn("size-[0.9375rem] shrink-0 opacity-90", className)} aria-hidden strokeWidth={2} />;
}

function ActionChipControl({
  item,
  visual,
  shape,
  fullWidthMobile,
}: {
  item: ActionChipItem;
  visual: ActionChipVisual;
  shape: ActionChipShape;
  fullWidthMobile?: boolean;
}) {
  const tone = item.tone ?? "neutral";
  const showCount = shouldShowCount(item);
  const iconOnly = Boolean(item.iconOnly);
  const ariaLabel =
    item.ariaLabel ??
    (iconOnly && typeof item.label === "string" ? item.label : undefined);
  const className = cn(
    actionChipItemVariants({
      visual,
      shape,
      tone: visual === "solidPrimary" || visual === "tintedPrimary" || visual === "gradient" ? "neutral" : tone,
      iconOnly,
      fullWidthMobile: Boolean(fullWidthMobile),
    }),
  );

  const body = (
    <>
      {item.loading ? (
        <Loader2 className="size-[0.9375rem] shrink-0 animate-spin" aria-hidden strokeWidth={2} />
      ) : (
        renderIcon(item.icon)
      )}
      {iconOnly ? null : <span className="min-w-0 truncate">{item.label}</span>}
      {showCount ? (
        <CountBadge count={item.count} tone={item.countTone ?? "neutral"} />
      ) : null}
      {renderIcon(item.trailingIcon)}
    </>
  );

  if (item.href && !item.disabled && !item.loading) {
    return (
      <Link
        to={item.href}
        title={item.title}
        aria-label={ariaLabel}
        data-testid={item.testId}
        data-emphasis={item.emphasis ?? "default"}
        className={className}
      >
        {body}
      </Link>
    );
  }

  if (typeof item.onSelect === "function" || item.loading || item.disabled || !item.href) {
    return (
      <button
        type="button"
        title={item.title}
        aria-label={ariaLabel}
        aria-busy={item.loading || undefined}
        disabled={item.disabled || item.loading}
        data-testid={item.testId}
        data-emphasis={item.emphasis ?? "default"}
        className={className}
        onClick={item.onSelect}
      >
        {body}
      </button>
    );
  }

  return (
    <span data-testid={item.testId} className={className} aria-disabled="true">
      {body}
    </span>
  );
}

function OverflowMore({
  items,
  shape,
  variant,
}: {
  items: ReadonlyArray<ActionChipItem>;
  shape: ActionChipShape;
  variant: ActionChipVisual;
}) {
  const { open, setOpen } = useDismissibleOpen(false);
  if (items.length === 0) return null;

  return (
    <DropdownMenu
      open={open}
      onOpenChange={setOpen}
      menuLabel="More actions"
      trigger={({ id, expanded, controls, onClick, onKeyDown }) => (
        <button
          type="button"
          id={id}
          aria-haspopup="menu"
          aria-expanded={expanded}
          aria-controls={controls}
          aria-label="More actions"
          title="More"
          data-testid="action-chip-overflow-more"
          className={actionChipItemVariants({ visual: variant, shape })}
          onClick={onClick}
          onKeyDown={onKeyDown}
        >
          <span>More</span>
          <ChevronDown className="size-[0.875rem] shrink-0 opacity-80" aria-hidden strokeWidth={2} />
        </button>
      )}
    >
      {items.map((item) => (
        <MenuItem
          key={item.key}
          disabled={item.disabled}
          onSelect={() => {
            if (item.href) {
              window.location.assign(item.href);
              return;
            }
            item.onSelect?.();
          }}
        >
          {item.icon ? renderIcon(item.icon, "me-2 inline") : null}
          {item.label}
        </MenuItem>
      ))}
    </DropdownMenu>
  );
}

/**
 * ExItS Action Chip bar (PILOT).
 * Lightweight quick actions + navigation shortcuts — not Tabs, Filters, or Module Subnav.
 * Production pages continue to use ExitsChipBar variant="actions"; this component powers
 * the UI Standards gallery and future opt-in migrations after lock.
 */
export function ActionChipBar({
  items,
  ariaLabel,
  variant = "soft",
  shape = "auto",
  layout = "wrap",
  primaryTreatment = "tinted",
  fullWidthMobile = false,
  groupRole = "toolbar",
  sections,
  overflowAfter,
  trailingItems,
  className,
  listClassName,
  testId,
}: ActionChipBarProps) {
  const visible =
    overflowAfter != null && overflowAfter >= 0 ? items.slice(0, overflowAfter) : items;
  const overflow =
    overflowAfter != null && overflowAfter >= 0 ? items.slice(overflowAfter) : [];

  const role = groupRole === "none" ? undefined : groupRole;

  function renderItems(list: ReadonlyArray<ActionChipItem>) {
    return list.map((item) => (
      <ActionChipControl
        key={item.key}
        item={item}
        visual={resolveVisual(item, variant, primaryTreatment)}
        shape={shape}
        fullWidthMobile={fullWidthMobile}
      />
    ));
  }

  if (sections && sections.length > 0) {
    const byKey = new Map(items.map((item) => [item.key, item]));
    return (
      <div
        role={role}
        aria-label={ariaLabel}
        data-testid={testId}
        data-layout="sectioned"
        data-variant={variant}
        className={cn("exits-action-chip-bar flex min-w-0 flex-col gap-3", className)}
      >
        {sections.map((section) => {
          const sectionItems = section.itemKeys
            .map((key) => byKey.get(key))
            .filter((item): item is ActionChipItem => Boolean(item));
          if (sectionItems.length === 0) return null;
          return (
            <div key={section.id} className="grid gap-1.5" data-testid={`action-chip-section-${section.id}`}>
              <div className="text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-muted">
                {section.label}
              </div>
              <div className={cn(actionChipGroupVariants({ layout: "wrap" }), listClassName)}>
                {renderItems(sectionItems)}
              </div>
            </div>
          );
        })}
      </div>
    );
  }

  const hasTrailing = Boolean(trailingItems?.length);

  return (
    <div
      role={role}
      aria-label={ariaLabel}
      data-testid={testId}
      data-layout={layout}
      data-variant={variant}
      data-shape={shape}
      className={cn(
        hasTrailing ? "flex min-w-0 flex-wrap items-start justify-between gap-[var(--exits-chip-gap)]" : null,
        className,
      )}
    >
      <div className={cn(actionChipGroupVariants({ layout }), hasTrailing && "min-w-0 flex-1", listClassName)}>
        {renderItems(visible)}
        {overflow.length > 0 ? (
          <OverflowMore items={overflow} shape={shape} variant={variant} />
        ) : null}
      </div>
      {hasTrailing ? (
        <div
          className={cn(actionChipGroupVariants({ layout: "inline" }), "ms-auto shrink-0")}
          data-testid={testId ? `${testId}-trailing` : undefined}
        >
          {renderItems(trailingItems!)}
        </div>
      ) : null}
    </div>
  );
}

export type {
  ActionChipVisual,
  ActionChipShape,
  ActionChipGroupLayout,
  ActionChipTone,
};
