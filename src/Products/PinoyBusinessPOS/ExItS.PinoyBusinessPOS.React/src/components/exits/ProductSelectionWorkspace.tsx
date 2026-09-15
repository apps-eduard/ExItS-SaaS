import type { ReactNode } from "react";
import { Plus, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { CountBadge } from "@/components/exits/CountChip";
import { EmptyState } from "@/components/exits/EmptyState";
import { cn } from "@/lib/cn";
import { PackagePlus } from "lucide-react";

export type SelectedItemsPanelProps = {
  title: string;
  count: number;
  headingId: string;
  addLabel: string;
  onAddClick: () => void;
  finderOpen: boolean;
  finderPanelId: string;
  emptyTitle: string;
  emptyDetail: string;
  emptyTestId?: string;
  addTestId?: string;
  testId?: string;
  /** Feature-specific selected lines (table/cards). */
  children?: ReactNode;
  /** Totals / summary under lines. */
  summary?: ReactNode;
  className?: string;
};

/**
 * Shared “selected items first + Add products” chrome.
 * Feature pages own line renderers and domain fields.
 */
export function SelectedItemsPanel({
  title,
  count,
  headingId,
  addLabel,
  onAddClick,
  finderOpen,
  finderPanelId,
  emptyTitle,
  emptyDetail,
  emptyTestId,
  addTestId = "product-selection-add-trigger",
  testId = "product-selection-selected-items",
  children,
  summary,
  className,
}: SelectedItemsPanelProps) {
  return (
    <Card
      as="section"
      padding="compact"
      className={cn(
        "product-selection-workspace__selected receive-stock-section",
        className,
      )}
      data-testid={testId}
      aria-labelledby={headingId}
    >
      <div className="receive-stock-section__header product-selection-workspace__header">
        <h2
          id={headingId}
          className="receive-stock-section__title m-0 flex items-center gap-2"
        >
          <span>{title}</span>
          <CountBadge count={count} tone="primary" />
        </h2>
        <Button
          type="button"
          onClick={onAddClick}
          aria-expanded={finderOpen}
          aria-controls={finderPanelId}
          data-testid={addTestId}
        >
          <Plus className="size-4" aria-hidden />
          {addLabel}
        </Button>
      </div>

      {count === 0 ? (
        <EmptyState
          align="center"
          size="compact"
          icon={<PackagePlus className="size-5" strokeWidth={1.75} />}
          title={emptyTitle}
          detail={emptyDetail}
          testId={emptyTestId ?? `${testId}-empty`}
        />
      ) : (
        <>
          {children}
          {summary}
        </>
      )}
    </Card>
  );
}

export type ProductFinderPanelProps = {
  title: string;
  /** Optional leading icon beside the title (e.g. supplier store). */
  titleIcon?: ReactNode;
  headingId: string;
  panelId: string;
  closeLabel: string;
  onClose: () => void;
  closeTestId?: string;
  testId?: string;
  children: ReactNode;
  className?: string;
};

/** Shared finder chrome — Close hides the panel; body is feature-owned. */
export function ProductFinderPanel({
  title,
  titleIcon,
  headingId,
  panelId,
  closeLabel,
  onClose,
  closeTestId = "product-selection-close-finder",
  testId = "product-selection-finder",
  children,
  className,
}: ProductFinderPanelProps) {
  return (
    <Card
      as="section"
      padding="compact"
      id={panelId}
      className={cn(
        "product-selection-workspace__finder receive-stock-section receive-stock-add receive-stock-finder",
        className,
      )}
      data-testid={testId}
      aria-labelledby={headingId}
    >
      <div className="receive-stock-section__header product-selection-workspace__header">
        <h2
          id={headingId}
          className="receive-stock-section__title m-0 flex items-center gap-2"
        >
          {titleIcon ? (
            <span className="inline-flex shrink-0 text-primary" aria-hidden>
              {titleIcon}
            </span>
          ) : null}
          <span>{title}</span>
        </h2>
        <Button
          type="button"
          variant="ghost"
          onClick={onClose}
          data-testid={closeTestId}
          aria-label={closeLabel}
        >
          <X className="size-4" aria-hidden />
          {closeLabel}
        </Button>
      </div>
      {children}
    </Card>
  );
}

export type ProductSelectionWorkspaceProps = {
  className?: string;
  testId?: string;
  selected: ReactNode;
  finder?: ReactNode;
  footer?: ReactNode;
};

/** Selected items + optional finder + sticky footer actions. */
export function ProductSelectionWorkspace({
  className,
  testId = "product-selection-workspace",
  selected,
  finder,
  footer,
}: ProductSelectionWorkspaceProps) {
  return (
    <div
      className={cn("product-selection-workspace receive-stock-workspace", className)}
      data-testid={testId}
    >
      {selected}
      {finder}
      {footer ? (
        <div className="product-selection-workspace__actions receive-stock-actions">
          {footer}
        </div>
      ) : null}
    </div>
  );
}
