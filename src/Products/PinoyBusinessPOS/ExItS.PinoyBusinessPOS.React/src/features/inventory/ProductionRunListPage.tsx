import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, Factory } from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import { listProductionRuns } from "@/api/pos/pos-production-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  formatProductionDate,
  productionRunStatusLabelKey,
} from "@/features/inventory/production-labels";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PAGE_SIZE = 20;

export function ProductionRunListPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);
  const [page, setPage] = useState(1);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: ["production-runs", workspace?.organizationId, workspace?.branchId, page],
    enabled: Boolean(workspace) && online,
    queryFn: ({ signal }) =>
      listProductionRuns(workspace!, { page, pageSize: PAGE_SIZE }, signal),
  });

  useEffect(() => {
    setPage(1);
  }, [workspace?.organizationId, workspace?.branchId]);

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  const items = query.data?.items ?? [];
  const totalCount = query.data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const canPrev = page > 1;
  const canNext = page < totalPages && totalCount > 0;

  return (
    <div
      className="production-run-list-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="production-run-list-page"
    >
      <PageHeader
        title={t("production.runs.title")}
        description={t("production.runs.lede")}
        backTo="/inventory/production"
        backLabel={t("production.backHome")}
        backTestId="page-header-back-production"
      />

      {!online ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("production.offline")}</p>
      ) : null}

      {allowManage ? (
        <ExitsChipBar
          variant="actions"
          ariaLabel={t("production.runs.title")}
          testId="production-runs-toolbar"
          className="exits-animate-toolbar"
          items={[
            {
              key: "produce",
              label: t("production.homeProduce"),
              icon: <Factory />,
              href: online ? "/inventory/production/produce" : undefined,
              disabled: !online,
              testId: "production-runs-produce",
              emphasis: "primary",
            },
          ]}
        />
      ) : null}

      {query.isLoading ? <LoadingState label={t("production.loading")} /> : null}
      {query.isError ? (
        <ErrorState title={t("production.errorTitle")} detail={t("production.runs.loadFailed")} />
      ) : null}
      {query.isSuccess && items.length === 0 ? (
        <EmptyState title={t("production.runs.empty")} detail={t("production.runs.emptyDetail")} />
      ) : null}

      <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="production-run-list">
        {items.map((item) => {
          const isVoided = item.status === "Voided";
          return (
            <li key={item.productionRunId}>
              <Link
                to={`/inventory/production/runs/${item.productionRunId}`}
                className="exits-list__card block min-w-0 text-foreground no-underline"
                data-testid={`production-run-row-${item.productionRunId}`}
              >
                <span className="min-w-0">
                  <span className="exits-list__name block truncate font-semibold">
                    {item.productionNumber}
                  </span>
                  <span className="mt-1 block truncate text-[length:var(--exits-text-sm)] text-muted">
                    {[
                      formatProductionDate(item.producedAtUtc),
                      item.outputNameSnapshot,
                      `${item.outputBaseQuantity}`,
                    ].join(" · ")}
                  </span>
                  {item.totalMaterialCost != null ? (
                    <span className="mt-1 block text-[length:var(--exits-text-sm)] text-muted">
                      <MoneyDisplay amount={item.totalMaterialCost} />
                    </span>
                  ) : null}
                </span>
                <span className="flex shrink-0 items-center gap-2">
                  <StatusChip tone={isVoided ? "danger" : "success"}>
                    {t(productionRunStatusLabelKey(item.status))}
                  </StatusChip>
                  <ChevronRight className="size-4 shrink-0 text-muted" aria-hidden />
                </span>
              </Link>
            </li>
          );
        })}
      </ul>

      {totalCount > PAGE_SIZE ? (
        <div className="flex flex-wrap items-center gap-2">
          <Button
            type="button"
            variant="outline"
            className="min-h-11"
            disabled={!canPrev || query.isFetching}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
          >
            {t("production.runs.prevPage")}
          </Button>
          <span className="text-[length:var(--exits-text-sm)] text-muted">
            {t("production.runs.pageOf")
              .replace("{page}", String(page))
              .replace("{pages}", String(totalPages))}
          </span>
          <Button
            type="button"
            variant="outline"
            className="min-h-11"
            disabled={!canNext || query.isFetching}
            onClick={() => setPage((p) => p + 1)}
          >
            {t("production.runs.nextPage")}
          </Button>
        </div>
      ) : null}
    </div>
  );
}
