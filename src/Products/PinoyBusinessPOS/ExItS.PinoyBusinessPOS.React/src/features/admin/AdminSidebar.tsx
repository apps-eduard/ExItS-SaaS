import { Link, NavLink, useLocation } from "react-router-dom";
import { ArrowLeftRight } from "lucide-react";
import { ExitsTooltip } from "@/components/exits/ExitsTooltip";
import { SidebarBrandHeader } from "@/components/exits/SidebarBrandHeader";
import {
  buildAdminNavGroups,
  flattenAdminNavItems,
  matchAdminNavItem,
} from "@/features/admin/admin-nav-config";
import {
  capturePreferencesReturnFrom,
  isPreferencesDestination,
  preferencesNavigationState,
} from "@/features/preferences/preferences-return";
import { useSidebarNavTooltipEnabled } from "@/features/shell/useSidebarNavTooltipEnabled";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

/** Desktop (lg+) Manage Business sidebar — Standard / Compact / Reveal via data-navigation-mode. */
export function AdminSidebar() {
  const { t } = useI18n();
  const location = useLocation();
  const { sessionGrant } = useWorkspace();
  const groups = buildAdminNavGroups(sessionGrant);
  const items = flattenAdminNavItems(groups);
  const activeId = matchAdminNavItem(location.pathname, items);
  const switchLabel = t("workspace.switch");
  const tooltipEnabled = useSidebarNavTooltipEnabled();

  if (groups.length === 0) {
    return null;
  }

  return (
    <aside
      className={cn("admin-sidebar", "admin-sidebar--expanded")}
      data-testid="admin-sidebar"
      aria-label={t("admin.nav.aria")}
    >
      <SidebarBrandHeader testId="admin-sidebar-brand" />

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
                const label = t(item.labelKey);
                const accessibleLabel =
                  item.locked && item.lockedReasonKey
                    ? `${label} · ${t(item.lockedReasonKey)}`
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
                    aria-label={accessibleLabel}
                    aria-current={isActive ? "page" : undefined}
                    className={cn(
                      "admin-sidebar__link",
                      isActive && "admin-sidebar__link--active",
                      item.locked && "admin-sidebar__link--locked",
                    )}
                  >
                    <Icon className="admin-sidebar__icon size-5 shrink-0" aria-hidden />
                    <span className="admin-sidebar__label min-w-0 flex-1 truncate">
                      {label}
                      {item.locked && item.lockedReasonKey ? (
                        <span className="admin-sidebar__lock-hint">
                          {" "}
                          · {t(item.lockedReasonKey)}
                        </span>
                      ) : null}
                    </span>
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
            data-testid="admin-sidebar-switch-workspace"
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
