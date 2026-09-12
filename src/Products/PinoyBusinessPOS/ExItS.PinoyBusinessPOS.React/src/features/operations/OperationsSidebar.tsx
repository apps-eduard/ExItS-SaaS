import { Link, NavLink, useLocation } from "react-router-dom";
import { ArrowLeftRight } from "lucide-react";
import { ExitsTooltip } from "@/components/exits/ExitsTooltip";
import { NavActivityCountBadge } from "@/components/exits/NavActivityCountBadge";
import { SidebarBrandHeader } from "@/components/exits/SidebarBrandHeader";
import {
  buildOperationsSidebarGroups,
  flattenOperationsSidebarItems,
  matchOperationsSidebarItem,
} from "@/features/operations/operations-nav-config";
import {
  capturePreferencesReturnFrom,
  isPreferencesDestination,
  preferencesNavigationState,
} from "@/features/preferences/preferences-return";
import { usePurchasingNavigationBadge } from "@/features/purchasing/usePurchasingNavigationBadge";
import { useSidebarNavTooltipEnabled } from "@/features/shell/useSidebarNavTooltipEnabled";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

/** Desktop (lg+) Operations sidebar — Standard / Compact / Reveal via data-navigation-mode. */
export function OperationsSidebar() {
  const { t } = useI18n();
  const location = useLocation();
  const { sessionGrant, boundWorkspace } = useWorkspace();
  const purchasingBadge = usePurchasingNavigationBadge();
  const tooltipEnabled = useSidebarNavTooltipEnabled();
  const groups = buildOperationsSidebarGroups({
    grant: sessionGrant,
    branchType: boundWorkspace?.branchType,
    experience: boundWorkspace?.experience ?? "operations",
  });
  const items = flattenOperationsSidebarItems(groups);
  const activeId = matchOperationsSidebarItem(location.pathname, items);
  const switchLabel = t("workspace.switch");

  if (groups.length === 0) {
    return null;
  }

  return (
    <aside
      className={cn("admin-sidebar", "admin-sidebar--expanded", "operations-sidebar")}
      data-testid="operations-sidebar"
      aria-label={t("operations.nav.aria")}
    >
      <SidebarBrandHeader testId="operations-sidebar-brand" />

      <nav className="admin-sidebar__nav">
        {groups.map((group) => (
          <div key={group.id} className="admin-sidebar__group">
            <p className="admin-sidebar__group-title m-0">{t(group.titleKey)}</p>
            <ul className="m-0 list-none space-y-0.5 p-0">
              {group.items.map((item) => {
                const Icon = item.icon;
                const isActive = activeId === item.id;
                const preferencesState = isPreferencesDestination(item.to)
                  ? preferencesNavigationState(location.pathname, location.search)
                  : undefined;
                const badgeDisplay =
                  item.id === "purchasing" ? purchasingBadge.display : null;
                const label = t(item.labelKey);
                const ariaLabel =
                  badgeDisplay != null
                    ? `${label}, ${purchasingBadge.count} items`
                    : label;
                const link = (
                  <NavLink
                    to={item.to}
                    end={item.end}
                    state={preferencesState}
                    onClick={
                      preferencesState
                        ? () =>
                            capturePreferencesReturnFrom(location.pathname, location.search)
                        : undefined
                    }
                    data-testid={item.testId}
                    aria-label={ariaLabel}
                    aria-current={isActive ? "page" : undefined}
                    className={cn(
                      "admin-sidebar__link",
                      isActive && "admin-sidebar__link--active",
                    )}
                  >
                    <Icon className="admin-sidebar__icon size-5 shrink-0" aria-hidden />
                    <span className="admin-sidebar__label min-w-0 flex-1 truncate">{label}</span>
                    {badgeDisplay != null ? (
                      <NavActivityCountBadge
                        display={badgeDisplay}
                        selected={isActive}
                        className="admin-sidebar__badge"
                        testId={`${item.testId}-badge`}
                      />
                    ) : null}
                  </NavLink>
                );
                return (
                  <li key={item.id}>
                    <ExitsTooltip content={label} disabled={!tooltipEnabled}>
                      {link}
                    </ExitsTooltip>
                  </li>
                );
              })}
            </ul>
          </div>
        ))}
      </nav>

      <div className="admin-sidebar__footer">
        <ExitsTooltip content={switchLabel} disabled={!tooltipEnabled}>
          <Link
            to="/workspace"
            className="admin-sidebar__switch"
            data-testid="operations-sidebar-switch-workspace"
            aria-label={switchLabel}
          >
            <ArrowLeftRight className="size-4 shrink-0" aria-hidden />
            <span className="admin-sidebar__label">{switchLabel}</span>
          </Link>
        </ExitsTooltip>
      </div>
    </aside>
  );
}
