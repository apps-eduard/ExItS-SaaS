import type { ReactNode } from "react";
import type { LucideIcon } from "lucide-react";
import { ChevronRight } from "lucide-react";
import { Link } from "react-router-dom";
import { cn } from "@/lib/cn";

export type ReportHubCardProps = {
  to: string;
  title: string;
  description?: string;
  icon: LucideIcon;
  testId?: string;
  /** Subtle primary accent — not a solid green CTA. */
  featured?: boolean;
  className?: string;
};

/**
 * Compact report launcher card for /reports.
 * Separate from RoleActionTile so other hubs keep their existing tile look.
 */
export function ReportHubCard({
  to,
  title,
  description,
  icon: Icon,
  testId,
  featured = false,
  className,
}: ReportHubCardProps) {
  return (
    <Link
      to={to}
      data-testid={testId}
      className={cn(
        "reports-hub-card",
        featured && "reports-hub-card--featured",
        className,
      )}
    >
      <span className="reports-hub-card__icon" aria-hidden>
        <Icon className="size-[1.15rem]" />
      </span>
      <span className="reports-hub-card__copy">
        <span className="reports-hub-card__title">{title}</span>
        {description ? (
          <span className="reports-hub-card__description">{description}</span>
        ) : null}
      </span>
      <ChevronRight className="reports-hub-card__chevron size-4 shrink-0" aria-hidden />
    </Link>
  );
}

export function ReportHubCardGrid({
  children,
  className,
  testId,
}: {
  children: ReactNode;
  className?: string;
  testId?: string;
}) {
  return (
    <div
      className={cn("reports-hub-grid", className)}
      role="group"
      data-testid={testId}
    >
      {children}
    </div>
  );
}
