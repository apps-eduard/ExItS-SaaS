import { Link } from "react-router-dom";
import {
  buildAdminNavGroups,
  type AdminNavGroupId,
} from "@/features/admin/admin-nav-config";
import { useI18n } from "@/i18n/I18nProvider";
import { PageHeader } from "@/components/exits/PageHeader";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const MANAGE_GROUPS: AdminNavGroupId[] = ["organization"];

/**
 * Mobile Manage hub — Areas, Branches & Warehouses, Staff, Roles, Devices.
 */
export function AdminManageHubPage() {
  const { t } = useI18n();
  const { sessionGrant } = useWorkspace();
  const groups = buildAdminNavGroups(sessionGrant).filter((g) =>
    MANAGE_GROUPS.includes(g.id),
  );

  return (
    <div className="admin-hub-page exits-page" data-testid="admin-manage-hub">
      <PageHeader
        title={t("admin.manageHub.title")}
        description={t("admin.manageHub.lede")}
        backTo="/org"
        backLabel={t("admin.mobile.home")}
        backTestId="page-header-back-admin-manage"
      />
      <div className="admin-hub-grid">
        {groups.flatMap((group) =>
          group.items.map((item) => {
            const Icon = item.icon;
            return (
              <Link
                key={item.id}
                to={item.to}
                data-testid={`admin-manage-hub-${item.id}`}
                className="admin-hub-tile"
              >
                <Icon className="size-5 shrink-0" aria-hidden />
                <span className="min-w-0">
                  <span className="block font-semibold">{t(item.labelKey)}</span>
                  {item.locked && item.lockedReasonKey ? (
                    <span className="block text-[length:var(--exits-text-xs)] text-muted">
                      {t(item.lockedReasonKey)}
                    </span>
                  ) : null}
                </span>
              </Link>
            );
          }),
        )}
      </div>
    </div>
  );
}
