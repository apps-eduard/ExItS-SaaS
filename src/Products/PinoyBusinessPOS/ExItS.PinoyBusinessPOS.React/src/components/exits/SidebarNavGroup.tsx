import type { ReactNode } from "react";
import type { LucideIcon } from "lucide-react";
import { ChevronDown } from "lucide-react";
import { cn } from "@/lib/cn";

/**
 * Collapsible sidenav section — full-width labels retained; only children hide.
 */
export function SidebarNavGroup({
  id,
  title,
  icon: Icon,
  expanded,
  active = false,
  onToggle,
  children,
  testId,
}: {
  id: string;
  title: string;
  icon: LucideIcon;
  expanded: boolean;
  /** True when this group owns the current route. */
  active?: boolean;
  onToggle: () => void;
  children: ReactNode;
  testId?: string;
}) {
  const panelId = `sidebar-nav-group-panel-${id}`;
  const headerId = `sidebar-nav-group-header-${id}`;

  return (
    <div
      className={cn(
        "admin-sidebar__group",
        !expanded && "admin-sidebar__group--collapsed",
        active && "admin-sidebar__group--active",
      )}
      data-testid={testId ?? `sidebar-nav-group-${id}`}
      data-expanded={expanded ? "true" : "false"}
      data-active={active ? "true" : "false"}
    >
      <button
        type="button"
        id={headerId}
        className={cn(
          "admin-sidebar__group-toggle",
          expanded && "admin-sidebar__group-toggle--open",
          active && "admin-sidebar__group-toggle--active",
        )}
        aria-expanded={expanded}
        aria-controls={panelId}
        data-testid={`sidebar-nav-group-toggle-${id}`}
        onClick={onToggle}
      >
        <span className="admin-sidebar__group-leading">
          <span className="admin-sidebar__group-icon-wrap" aria-hidden="true">
            <Icon className="admin-sidebar__group-icon" strokeWidth={2} />
          </span>
          <span className="admin-sidebar__group-title m-0">{title}</span>
        </span>
        <span
          className={cn(
            "admin-sidebar__group-chevron-wrap",
            expanded && "admin-sidebar__group-chevron-wrap--open",
          )}
          aria-hidden="true"
        >
          <ChevronDown
            className={cn(
              "admin-sidebar__group-chevron size-3.5 shrink-0",
              expanded && "admin-sidebar__group-chevron--open",
            )}
            strokeWidth={2.25}
          />
        </span>
      </button>
      <div
        id={panelId}
        role="region"
        aria-labelledby={headerId}
        aria-hidden={expanded ? undefined : true}
        {...(!expanded ? ({ inert: true } as Record<string, boolean>) : {})}
        className={cn(
          "admin-sidebar__group-panel",
          expanded && "admin-sidebar__group-panel--open",
        )}
      >
        <div className="admin-sidebar__group-panel-inner">{children}</div>
      </div>
    </div>
  );
}
