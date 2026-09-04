import { Link, useParams } from "react-router-dom";
import { Check, Minus, Users } from "lucide-react";
import { listProductLocalRoleDefinitions } from "@/api/platform/product-local-role-definitions-client";
import { useQuery } from "@tanstack/react-query";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function OrgRoleDetailPage() {
  const { t } = useI18n();
  const { roleCode = "" } = useParams();
  const { boundWorkspace } = useWorkspace();
  const organizationId = boundWorkspace?.organizationId ?? null;

  const detailQuery = useQuery({
    queryKey: ["product-local-role-definition", organizationId, roleCode],
    enabled: Boolean(organizationId && roleCode),
    queryFn: async () => {
      const result = await listProductLocalRoleDefinitions(organizationId!);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("orgRoles.loadError"));
      }
      const role = result.roles.find((item) => item.code === roleCode) ?? null;
      if (!role) {
        throw new Error(t("orgRoles.notFound"));
      }
      return role;
    },
  });

  return (
    <div
      className="org-role-detail-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="org-role-detail-page"
    >
      <PageHeader
        title={detailQuery.data?.displayName ?? t("orgRoles.detailTitle")}
        description={detailQuery.data?.description ?? t("orgRoles.detailLede")}
        backTo="/org/roles"
        backLabel={t("orgRoles.backList")}
        backTestId="page-header-back-roles"
        trailing={
          <Button asChild variant="outline" data-testid="org-role-manage-staff">
            <Link to={pageBackNav.orgStaff.to}>
              <Users className="size-4" aria-hidden />
              {t("staffManage.title")}
            </Link>
          </Button>
        }
      />

      {detailQuery.isLoading ? <LoadingSkeleton count={4} label={t("loading.label")} /> : null}

      {detailQuery.isError ? (
        <ErrorState
          title={t("error.title")}
          detail={
            detailQuery.error instanceof Error ? detailQuery.error.message : t("orgRoles.loadError")
          }
        />
      ) : null}

      {detailQuery.isSuccess ? (
        <>
          <section className="catalog-form-section exits-animate-panel gap-2" data-testid="org-role-meta">
            <p className="m-0 text-[length:var(--exits-text-sm)]">
              {t("orgRoles.systemRole")} ·{" "}
              {t("orgRoles.staffUsingRole").replace(
                "{count}",
                String(detailQuery.data.activeStaffCount ?? 0),
              )}
            </p>
            {detailQuery.data.code === "Owner" ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("orgRoles.posOwnerOwnershipNote")}
              </p>
            ) : null}
          </section>

          <div className="org-role-permissions" data-testid="org-role-permissions">
            {detailQuery.data.permissionGroups.map((group) => (
              <section
                key={group.code}
                className="catalog-form-section exits-animate-panel gap-2"
                data-testid={`org-role-group-${group.code}`}
              >
                <h2 className="catalog-form-section__title m-0">{group.displayName}</h2>
                <ul className="org-role-permission-list m-0 list-none p-0">
                  {group.items.map((item) => (
                    <li key={item.code} className="org-role-permission-item">
                      {item.allowed ? (
                        <Check
                          className="org-role-permission-item__icon org-role-permission-item__icon--allowed"
                          aria-hidden
                        />
                      ) : (
                        <Minus className="org-role-permission-item__icon" aria-hidden />
                      )}
                      <span>{item.displayName}</span>
                      <span className="sr-only">
                        {item.allowed ? t("orgRoles.allowed") : t("orgRoles.notIncluded")}
                      </span>
                    </li>
                  ))}
                </ul>
              </section>
            ))}
          </div>
        </>
      ) : null}
    </div>
  );
}
