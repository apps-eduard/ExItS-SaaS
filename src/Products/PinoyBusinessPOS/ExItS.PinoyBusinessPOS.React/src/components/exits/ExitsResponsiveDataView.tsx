import type { HTMLAttributes, ReactNode } from "react";
import { cn } from "@/lib/cn";
import type { ResponsiveDataLayout } from "@/components/exits/responsive-data-view";

export type ExitsResponsiveDataViewProps = HTMLAttributes<HTMLDivElement> & {
  /** Resolved layout (from `useResponsiveDataLayout` or forced). */
  layout: ResponsiveDataLayout;
  /**
   * Exception: table stays visible with overflow-x when narrow.
   * Not the global default — document as opt-in on the page.
   */
  allowHorizontalScroll?: boolean;
  /** Shared search / filter / selection / output toolbar. */
  toolbar?: ReactNode;
  /** Desktop (and wide tablet) ExitsTable tree. */
  table: ReactNode;
  /** Narrow list / record cards (ExitsTableMobile or ExitsDataRecordCard list). */
  list: ReactNode;
  pagination?: ReactNode;
  testId?: string;
};

/**
 * Coordinates TABLE vs LIST presentation without a second data model.
 * Keeps one toolbar + pagination; swaps body chrome by layout.
 */
export function ExitsResponsiveDataView({
  layout,
  allowHorizontalScroll = false,
  toolbar,
  table,
  list,
  pagination,
  className,
  testId = "exits-responsive-data",
  ...rest
}: ExitsResponsiveDataViewProps) {
  return (
    <div
      {...rest}
      className={cn(
        "exits-responsive-data",
        layout === "table" && "exits-responsive-data--table",
        layout === "list" && "exits-responsive-data--list",
        allowHorizontalScroll && "exits-responsive-data--h-scroll",
        className,
      )}
      data-layout={layout}
      data-h-scroll={allowHorizontalScroll ? "true" : undefined}
      data-testid={testId}
    >
      {toolbar ? <div className="exits-responsive-data__toolbar">{toolbar}</div> : null}
      <div className="exits-responsive-data__table" data-testid="exits-responsive-data-table">
        {table}
      </div>
      <div className="exits-responsive-data__list" data-testid="exits-responsive-data-list">
        {list}
      </div>
      {pagination ? (
        <div className="exits-responsive-data__pagination">{pagination}</div>
      ) : null}
    </div>
  );
}
