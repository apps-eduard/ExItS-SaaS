import { Link } from "react-router-dom";
import { ShieldCheck } from "lucide-react";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useProductLocalRoleCatalog } from "@/features/staff/useProductLocalRoleCatalog";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function staffCountLabel(count: number | null | undefined, t: (key: MessageKey) => string): string {
  const value = count ?? 0;
  if (value === 1) {
    return t("orgRoles.staffCountOne");
  }
  return t("orgRoles.staffCountMany").replace("{count}", String(value));
}

export function OrgRolesPage() {
  const { t } = useI18n();
  const { boundWorkspace } = useWorkspace();
  const organizationId = boundWorkspace?.organizationId ?? null;
  const catalogQuery = useProductLocalRoleCatalog(organizationId);

  return (
    <div className="org-roles-page exits-page flex min-w-0 flex-col gap-3" data-testid="org-roles-page">
      <PageHeader
        title={t("orgRoles.title")}
        description={t("orgRoles.lede")}
        backTo={pageBackNav.org.to}
        backLabel={t(pageBackNav.org.labelKey)}
        backTestId="page-header-back-org"
      />

      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="org-roles-custom-note">
        {t("orgRoles.customRolesDeferred")}
      </p>

      {catalogQuery.isLoading ? <LoadingSkeleton count={5} label={t("loading.label")} /> : null}

      {catalogQuery.isError ? (
        <ErrorState
          title={t("error.title")}
          detail={
            catalogQuery.error instanceof Error
              ? catalogQuery.error.message
              : t("orgRoles.loadError")
          }
        />
      ) : null}

      {catalogQuery.isSuccess ? (
        <ul className="org-roles-list m-0 grid list-none gap-2 p-0" data-testid="org-roles-list">
          {catalogQuery.data.map((role) => (
            <li key={role.code}>
              <article className="exits-list__card org-role-card min-w-0" data-testid={`org-role-card-${role.code}`}>
                <div className="org-role-card__header">
                  <div className="min-w-0">
                    <h2 className="org-role-card__title m-0 font-semibold">{role.displayName}</h2>
                    <p className="org-role-card__desc m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                      {role.description}
                    </p>
                  </div>
                  <span className="org-role-card__badge shrink-0">
                    <ShieldCheck className="size-3.5" aria-hidden />
                    {t("orgRoles.systemRole")}
                  </span>
                </div>
                <p className="org-role-card__count m-0 text-[length:var(--exits-text-sm)]">
                  {staffCountLabel(role.activeStaffCount, t)}
                </p>
                <Link
                  className="org-role-card__link"
                  to={`/org/roles/${encodeURIComponent(role.code)}`}
                  data-testid={`org-role-view-${role.code}`}
                >
                  {t("orgRoles.viewPermissions")}
                </Link>
              </article>
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}
