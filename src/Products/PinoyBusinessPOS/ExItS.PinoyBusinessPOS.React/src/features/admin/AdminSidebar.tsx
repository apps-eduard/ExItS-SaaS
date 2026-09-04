import { NavLink, useLocation } from "react-router-dom";
import {
  buildAdminNavGroups,
  flattenAdminNavItems,
  matchAdminNavItem,
} from "@/features/admin/admin-nav-config";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type AdminSidebarProps = {
  /** Collapsed rail (tablet). */
  rail?: boolean;
};

export function AdminSidebar({ rail = false }: AdminSidebarProps) {
  const { t } = useI18n();
  const location = useLocation();
  const { sessionGrant, boundWorkspace } = useWorkspace();
  const groups = buildAdminNavGroups(sessionGrant);
  const items = flattenAdminNavItems(groups);
  const activeId = matchAdminNavItem(location.pathname, items);

  if (groups.length === 0) {
    return null;
  }

  return (
    <aside
      className={cn(
        "admin-sidebar",
        rail ? "admin-sidebar--rail" : "admin-sidebar--expanded",
      )}
      data-testid="admin-sidebar"
      aria-label={t("admin.nav.aria")}
    >
      <div className="admin-sidebar__brand">
        {!rail ? (
          <>
            <p className="admin-sidebar__product m-0">{t("admin.shell.productName")}</p>
            <p className="admin-sidebar__experience m-0">{t("admin.shell.manageBusiness")}</p>
            {boundWorkspace?.organizationDisplayName ? (
              <p className="admin-sidebar__org m-0 truncate" title={boundWorkspace.organizationDisplayName}>
                {boundWorkspace.organizationDisplayName}
              </p>
            ) : null}
          </>
        ) : (
          <p className="admin-sidebar__rail-mark m-0" aria-hidden>
            {t("admin.shell.railMark")}
          </p>
        )}
      </div>

      <nav className="admin-sidebar__nav">
        {groups.map((group) => (
          <div key={group.id} className="admin-sidebar__group">
            {!rail ? (
              <p className="admin-sidebar__group-title m-0">{t(group.titleKey)}</p>
            ) : null}
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
                      title={rail ? t(item.labelKey) : undefined}
                      aria-current={isActive ? "page" : undefined}
                      className={cn(
                        "admin-sidebar__link",
                        isActive && "admin-sidebar__link--active",
                        item.locked && "admin-sidebar__link--locked",
                      )}
                    >
                      <Icon className="admin-sidebar__icon size-5 shrink-0" aria-hidden />
                      {!rail ? (
                        <span className="min-w-0 flex-1 truncate">
                          {t(item.labelKey)}
                          {item.locked && item.lockedReasonKey ? (
                            <span className="admin-sidebar__lock-hint">
                              {" "}
                              · {t(item.lockedReasonKey)}
                            </span>
                          ) : null}
                        </span>
                      ) : null}
                    </NavLink>
                  </li>
                );
              })}
            </ul>
          </div>
        ))}
      </nav>
    </aside>
  );
}
