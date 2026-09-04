import { Link, NavLink, useLocation } from "react-router-dom";
import { ArrowLeftRight } from "lucide-react";
import {
  buildOperationsSidebarGroups,
  flattenOperationsSidebarItems,
  matchOperationsSidebarItem,
} from "@/features/operations/operations-nav-config";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

/** Desktop (lg+) Operations sidebar — expanded only (no tablet rail). */
export function OperationsSidebar() {
  const { t } = useI18n();
  const location = useLocation();
  const { sessionGrant, boundWorkspace } = useWorkspace();
  const groups = buildOperationsSidebarGroups({
    grant: sessionGrant,
    branchType: boundWorkspace?.branchType,
    experience: boundWorkspace?.experience ?? "operations",
  });
  const items = flattenOperationsSidebarItems(groups);
  const activeId = matchOperationsSidebarItem(location.pathname, items);

  if (groups.length === 0) {
    return null;
  }

  return (
    <aside
      className={cn("admin-sidebar", "admin-sidebar--expanded", "operations-sidebar")}
      data-testid="operations-sidebar"
      aria-label={t("operations.nav.aria")}
    >
      <div className="admin-sidebar__brand">
        <p className="admin-sidebar__product m-0">{t("operations.shell.productName")}</p>
        <p className="admin-sidebar__experience m-0">{t("operations.shell.operations")}</p>
        {boundWorkspace?.organizationDisplayName ? (
          <p
            className="admin-sidebar__org m-0 truncate"
            title={
              boundWorkspace.branchName
                ? `${boundWorkspace.organizationDisplayName} · ${boundWorkspace.branchName}`
                : boundWorkspace.organizationDisplayName
            }
          >
            {boundWorkspace.organizationDisplayName}
            {boundWorkspace.branchName ? ` · ${boundWorkspace.branchName}` : ""}
          </p>
        ) : null}
      </div>

      <nav className="admin-sidebar__nav">
        {groups.map((group) => (
          <div key={group.id} className="admin-sidebar__group">
            <p className="admin-sidebar__group-title m-0">{t(group.titleKey)}</p>
            <ul className="m-0 list-none space-y-0.5 p-0">
              {group.items.map((item) => {
                const Icon = item.icon;
                const isActive = activeId === item.id;
                return (
                  <li key={item.id}>
                    <NavLink
                      to={item.to}
                      end={item.end}
                      data-testid={item.testId}
                      aria-current={isActive ? "page" : undefined}
                      className={cn(
                        "admin-sidebar__link",
                        isActive && "admin-sidebar__link--active",
                      )}
                    >
                      <Icon className="admin-sidebar__icon size-5 shrink-0" aria-hidden />
                      <span className="min-w-0 flex-1 truncate">{t(item.labelKey)}</span>
                    </NavLink>
                  </li>
                );
              })}
            </ul>
          </div>
        ))}
      </nav>

      <div className="admin-sidebar__footer">
        <Link
          to="/workspace"
          className="admin-sidebar__switch"
          data-testid="operations-sidebar-switch-workspace"
        >
          <ArrowLeftRight className="size-4 shrink-0" aria-hidden />
          <span>{t("workspace.switch")}</span>
        </Link>
      </div>
    </aside>
  );
}
