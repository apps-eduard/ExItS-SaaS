import { useMemo } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canManageRegisters, canViewRegisters } from "@/access/pos-capabilities";
import { listRegisters } from "@/api/pos/pos-registers-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function RegistersListPage() {
  const { t } = useI18n();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const canView = canViewRegisters(sessionGrant);
  const canManage = canManageRegisters(sessionGrant);

  const workspaceScope = useMemo(() => {
    if (!boundWorkspace?.branchId) {
      return null;
    }
    return {
      organizationId: boundWorkspace.organizationId,
      branchId: boundWorkspace.branchId,
    };
  }, [boundWorkspace]);

  const registersQuery = useQuery({
    queryKey: ["pos-registers-list", workspaceScope?.organizationId, workspaceScope?.branchId],
    enabled: workspaceScope !== null && canView,
    queryFn: ({ signal }) => listRegisters(workspaceScope!, { page: 1, pageSize: 50 }, signal),
  });

  if (!canView) {
    return (
      <div data-testid="registers-denied" className="flex flex-col gap-3">
        <PageHeader
          title={t("register.listTitle")}
          description={t("register.deniedDetail")}
          backTo={pageBackNav.managerHome.to}
          backLabel={t(pageBackNav.managerHome.labelKey)}
          backTestId="page-header-back-registers"
        />
      </div>
    );
  }

  return (
    <div
      data-testid="registers-list-page"
      className="registers-page exits-page flex min-w-0 flex-col gap-3"
    >
      <PageHeader
        title={t("register.listTitle")}
        description={t("register.listLede")}
        backTo={pageBackNav.managerHome.to}
        backLabel={t(pageBackNav.managerHome.labelKey)}
        backTestId="page-header-back-registers"
      />
      {!canManage ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="register-view-only"
        >
          {t("register.viewOnly")}
        </p>
      ) : null}

      {registersQuery.isLoading ? <LoadingSkeleton label={t("loading.label")} /> : null}
      {registersQuery.isError ? (
        <Card>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {t("register.loadError")}
          </p>
        </Card>
      ) : null}

      <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="registers-list">
        {(registersQuery.data?.items ?? []).map((register) => (
          <li key={register.registerId}>
            <div
              data-testid={`register-row-${register.registerId}`}
              className="exits-list__card catalog-product-row min-w-0"
            >
              <span className="min-w-0">
                <span className="exits-list__name block truncate font-semibold">
                  {register.registerCode} — {register.name}
                </span>
                <span className="mt-1 block truncate text-[length:var(--exits-text-sm)] text-muted">
                  {register.hasOpenShift ? t("register.hasOpenShift") : t("register.noOpenShift")}
                </span>
              </span>

              <span className="catalog-product-row__aside">
                <span className="catalog-product-row__status">
                  <StatusChip tone={register.status === "Active" ? "success" : "info"}>
                    {register.status}
                  </StatusChip>
                </span>
              </span>
            </div>
          </li>
        ))}
      </ul>

      {(registersQuery.data?.items.length ?? 0) === 0 && !registersQuery.isLoading ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="registers-empty"
        >
          {t("register.empty")}
        </p>
      ) : null}

      <Button asChild variant="ghost" className="min-h-11 w-fit">
        <Link to="/shifts">{t("shift.hubTitle")}</Link>
      </Button>
    </div>
  );
}
