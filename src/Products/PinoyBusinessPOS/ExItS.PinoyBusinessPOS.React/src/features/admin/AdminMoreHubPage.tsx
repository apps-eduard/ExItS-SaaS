import { Link } from "react-router-dom";
import {
  buildAdminNavGroups,
  type AdminNavGroupId,
} from "@/features/admin/admin-nav-config";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const MORE_GROUPS: AdminNavGroupId[] = ["business", "security", "settings"];

/**
 * Mobile More hub — business configuration, security, settings.
 * No Sell/Catalog primary destinations.
 */
export function AdminMoreHubPage() {
  const { t } = useI18n();
  const { sessionGrant } = useWorkspace();
  const groups = buildAdminNavGroups(sessionGrant).filter((g) => MORE_GROUPS.includes(g.id));

  return (
    <div className="admin-hub-page exits-page" data-testid="admin-more-hub">
      <PageHeader
        title={t("admin.moreHub.title")}
        description={t("admin.moreHub.lede")}
        backTo="/org"
        backLabel={t("admin.mobile.home")}
        backTestId="page-header-back-admin-more"
      />
      {groups.map((group) => (
        <section key={group.id} className="catalog-form-section exits-animate-panel gap-2">
          <h2 className="catalog-form-section__title text-muted">{t(group.titleKey)}</h2>
          <div className="admin-hub-grid">
            {group.items.map((item) => {
              const Icon = item.icon;
              return (
                <Link
                  key={item.id}
                  to={item.to}
                  data-testid={`admin-more-hub-${item.id}`}
                  className="admin-hub-tile"
                >
                  <Icon className="size-5 shrink-0" aria-hidden />
                  <span className="font-semibold">{t(item.labelKey)}</span>
                </Link>
              );
            })}
          </div>
        </section>
      ))}
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        <Link to="/workspace" data-testid="admin-more-switch-workspace">
          {t("workspace.switch")}
        </Link>
      </p>
    </div>
  );
}
