import { useId, type ReactNode } from "react";
import { ChevronDown, type LucideIcon } from "lucide-react";
import { cn } from "@/lib/cn";

type CheckoutCollapsibleSectionProps = {
  title: string;
  /** Short line shown in the header when collapsed (e.g. selected method). */
  summary?: ReactNode;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  testId: string;
  children: ReactNode;
  /** Label for the collapsed primary CTA when there is no summary yet. */
  expandLabel: string;
  icon?: LucideIcon;
  className?: string;
  disabled?: boolean;
  /** Optional trailing control in the header (e.g. clear). */
  trailing?: ReactNode;
  /** Hide the expand chevron (useful when the whole header is a CTA). */
  hideChevron?: boolean;
};

/**
 * Checkout progressive disclosure: content stays collapsed until the cashier opens it.
 * Uses the same grid-rows expand pattern as PageHeader for accessible height animation.
 */
export function CheckoutCollapsibleSection({
  title,
  summary,
  open,
  onOpenChange,
  testId,
  children,
  expandLabel,
  icon: Icon,
  className,
  disabled,
  trailing,
  hideChevron,
}: CheckoutCollapsibleSectionProps) {
  const panelId = useId();
  const hasSummary = summary != null && summary !== false && summary !== "";

  return (
    <div
      className={cn("checkout-collapsible", className)}
      data-testid={testId}
      data-open={open ? "true" : "false"}
    >
      <div className="checkout-collapsible__header">
        <button
          type="button"
          className={cn(
            "checkout-collapsible__toggle",
            open && "checkout-collapsible__toggle--open",
            !hasSummary && !open && "checkout-collapsible__toggle--cta",
          )}
          data-testid={`${testId}-toggle`}
          aria-expanded={open}
          aria-controls={panelId}
          disabled={disabled}
          onClick={() => onOpenChange(!open)}
        >
          {Icon ? (
            <span className="checkout-collapsible__icon" aria-hidden>
              <Icon className="size-4" strokeWidth={2} />
            </span>
          ) : null}
          <span className="checkout-collapsible__copy">
            <span className="checkout-collapsible__title">{open || hasSummary ? title : expandLabel}</span>
            {!open && hasSummary ? (
              <span className="checkout-collapsible__summary">{summary}</span>
            ) : null}
          </span>
          {!hideChevron ? (
            <ChevronDown
              className={cn(
                "checkout-collapsible__chevron size-4 shrink-0",
                open && "checkout-collapsible__chevron--open",
              )}
              aria-hidden
            />
          ) : null}
        </button>
        {trailing ? <div className="checkout-collapsible__trailing">{trailing}</div> : null}
      </div>

      <div
        id={panelId}
        className={cn(
          "checkout-collapsible__shell",
          open && "checkout-collapsible__shell--open",
        )}
        data-testid={`${testId}-panel`}
        aria-hidden={!open}
      >
        <div className="checkout-collapsible__clip">
          <div className="checkout-collapsible__body">{children}</div>
        </div>
      </div>
    </div>
  );
}
