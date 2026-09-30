import { useMemo } from "react";
import { Link, NavLink, useLocation } from "react-router-dom";
import { ArrowLeftRight } from "lucide-react";
import { ExitsTooltip } from "@/components/exits/ExitsTooltip";
import { SidebarBrandHeader } from "@/components/exits/SidebarBrandHeader";
import { SidebarNavGroup } from "@/components/exits/SidebarNavGroup";
import {
  buildPersonalSidebarGroups,
  flattenPersonalNavItems,
  matchPersonalNavItem,
  type PersonalNavItem,
} from "@/features/personal/personal-nav-config";
import {
  capturePreferencesReturnFrom,
  isPreferencesDestination,
  preferencesNavigationState,
} from "@/features/preferences/preferences-return";
import { usePreferencesDestinationClick } from "@/features/preferences/usePreferencesDestinationClick";
import { useNotificationsDestinationClick } from "@/features/personal/useNotificationsDestinationClick";
import { findActiveGroupId } from "@/features/shell/sidebar-nav-group-accordion";
import { isAccordionNavGroup } from "@/features/shell/sidebar-nav-group-helpers";
import { useSidebarNavGroupAccordion } from "@/features/shell/useSidebarNavGroupAccordion";
import { useSidebarNavTooltipEnabled } from "@/features/shell/useSidebarNavTooltipEnabled";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

/** Desktop (lg+) Personal sidebar — primary tabs + More destinations as groups. */
export function PersonalSidebar() {
  const { t } = useI18n();
  const location = useLocation();
  const { workspaces } = useWorkspace();
  const openPreferencesDestination = usePreferencesDestinationClick();
  const openNotificationsDestination = useNotificationsDestinationClick();
  const groups = buildPersonalSidebarGroups();
  const items = flattenPersonalNavItems(groups);
  const activeId = matchPersonalNavItem(location.pathname, items);
  const accordionGroups = useMemo(
    () => groups.filter((g) => g.id !== "primary" && isAccordionNavGroup(g)),
    [groups],
  );
  const activeGroupId = useMemo(
    () => findActiveGroupId(accordionGroups, activeId),
    [accordionGroups, activeId],
  );
  const groupIds = useMemo(() => accordionGroups.map((g) => g.id), [accordionGroups]);
  const accordion = useSidebarNavGroupAccordion({
    scope: "personal",
    groupIds,
    activeGroupId,
  });
  const tooltipEnabled = useSidebarNavTooltipEnabled();
  const switchLabel = t("workspace.switch");
  const showWorkspaceFooter = workspaces.length > 0;

  const renderItem = (item: PersonalNavItem) => {
    const Icon = item.icon;
    const isActive = activeId === item.id;
    const preferencesState = isPreferencesDestination(item.to)
      ? preferencesNavigationState(location.pathname, location.search)
      : undefined;
    const label = t(item.labelKey);
    const sidebarTestId = item.testId.startsWith("personal-nav-")
      ? item.testId.replace("personal-nav-", "personal-sidebar-")
      : item.testId;
    const link = (
      <NavLink
        to={item.to}
        end={item.end}
        state={preferencesState}
        onClick={(event) => {
          if (openNotificationsDestination(item.to, event)) {
            return;
          }
          if (openPreferencesDestination(item.to, event)) {
            return;
          }
          if (preferencesState) {
            capturePreferencesReturnFrom(location.pathname, location.search);
          }
        }}
        data-testid={sidebarTestId}
        aria-label={label}
        aria-current={isActive ? "page" : undefined}
        className={cn("admin-sidebar__link", isActive && "admin-sidebar__link--active")}
      >
        <Icon className="admin-sidebar__icon size-4 shrink-0" aria-hidden />
        <span className="admin-sidebar__label min-w-0 flex-1 truncate">{label}</span>
      </NavLink>
    );
    return (
      <li key={item.id}>
        <ExitsTooltip content={label} disabled={!tooltipEnabled}>
          {link}
        </ExitsTooltip>
      </li>
    );
  };

  return (
    <aside
      className={cn("admin-sidebar", "admin-sidebar--expanded", "personal-sidebar")}
      data-testid="personal-sidebar"
      aria-label={t("personal.nav.aria")}
    >
      <SidebarBrandHeader
        testId="personal-sidebar-brand"
        allGroupsExpanded={accordion.allExpanded}
        onToggleAllGroups={accordion.toggleAll}
      />

      <nav className="admin-sidebar__nav">
        {groups.map((group, index) => {
          if (group.id === "primary") {
            return (
              <ul
                key={group.id}
                className="admin-sidebar__solo m-0 list-none p-0"
                data-testid="sidebar-nav-solo-primary"
              >
                {group.items.map(renderItem)}
              </ul>
            );
          }

          if (!isAccordionNavGroup(group)) {
            return null;
          }

          const prev = groups[index - 1];
          const showRule = prev != null;

          return (
            <div key={group.id} className="admin-sidebar__group-block">
              {showRule ? <div className="admin-sidebar__nav-rule" aria-hidden="true" /> : null}
              <SidebarNavGroup
                id={group.id}
                title={t(group.titleKey)}
                icon={group.icon}
                expanded={accordion.isGroupExpanded(group.id)}
                active={activeGroupId === group.id}
                onToggle={() => accordion.toggleGroup(group.id)}
              >
                <ul className="m-0 list-none space-y-0.5 p-0">{group.items.map(renderItem)}</ul>
              </SidebarNavGroup>
            </div>
          );
        })}
      </nav>

      {showWorkspaceFooter ? (
        <div className="admin-sidebar__footer">
          <ExitsTooltip content={switchLabel} disabled={!tooltipEnabled}>
            <Link
              to="/workspace"
              className="admin-sidebar__switch"
              data-testid="personal-sidebar-switch-workspace"
              aria-label={switchLabel}
            >
              <ArrowLeftRight className="size-4 shrink-0" aria-hidden />
              <span className="admin-sidebar__label">{switchLabel}</span>
            </Link>
          </ExitsTooltip>
        </div>
      ) : null}
    </aside>
  );
}
