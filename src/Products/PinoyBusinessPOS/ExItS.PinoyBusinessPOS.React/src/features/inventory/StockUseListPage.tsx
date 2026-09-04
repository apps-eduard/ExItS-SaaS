import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, PackageMinus } from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import { listStockUses } from "@/api/pos/pos-stock-use-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  formatStockUseOccurredDate,
  stockUseReasonLabelKey,
  stockUseStatusLabelKey,
} from "@/features/inventory/stock-use-labels";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PAGE_SIZE = 20;

export function StockUseListPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [page, setPage] = useState(1);
  const allowManage = canManageInventory(sessionGrant);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: ["stock-uses", workspace?.organizationId, workspace?.branchId, page],
    enabled: Boolean(workspace) && online,
    queryFn: ({ signal }) =>
      listStockUses(workspace!, { page, pageSize: PAGE_SIZE }, signal),
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
      className="stock-use-list-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="stock-use-list-page"
    >
      <PageHeader
        title={t("stockUse.title")}
        description={t("stockUse.lede")}
        backTo={pageBackNav.inventory.to}
        backLabel={t(pageBackNav.inventory.labelKey)}
        backTestId="page-header-back-inventory"
      />

      {!online ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("stockUse.offline")}</p>
      ) : null}

      {allowManage ? (
        <ExitsChipBar
          variant="actions"
          ariaLabel={t("stockUse.title")}
          testId="stock-use-toolbar"
          className="exits-animate-toolbar"
          items={[
            {
              key: "record",
              label: t("stockUse.recordUse"),
              icon: <PackageMinus />,
              href: online ? "/inventory/stock-use/new" : undefined,
              disabled: !online,
              testId: "stock-use-new",
              emphasis: "primary",
            },
          ]}
        />
      ) : null}

      {query.isLoading ? <LoadingState label={t("stockUse.loading")} /> : null}
      {query.isError ? (
        <ErrorState title={t("stockUse.errorTitle")} detail={t("stockUse.loadFailed")} />
      ) : null}
      {query.isSuccess && items.length === 0 ? (
        <EmptyState title={t("stockUse.empty")} detail={t("stockUse.emptyDetail")} />
      ) : null}

      <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="stock-use-list">
        {items.map((item) => {
          const isVoided = item.status === "Voided";
          return (
            <li key={item.stockUseId}>
              <Link
                to={`/inventory/stock-use/${item.stockUseId}`}
                className="exits-list__card stock-use-row block min-w-0 text-foreground no-underline"
                data-testid={`stock-use-row-${item.stockUseId}`}
              >
                <span className="stock-use-row__main min-w-0">
                  <span className="exits-list__name block truncate font-semibold">
                    {item.stockUseNumber}
                  </span>
                  <span className="mt-1 block truncate text-[length:var(--exits-text-sm)] text-muted">
                    {[
                      formatStockUseOccurredDate(item.occurredAtUtc),
                      t(stockUseReasonLabelKey(item.reason)),
                      t("stockUse.linesCount").replace("{count}", String(item.lineCount)),
                    ].join(" · ")}
                  </span>
                </span>
                <span className="stock-use-row__aside flex shrink-0 items-center gap-2">
                  <StatusChip tone={isVoided ? "danger" : "success"}>
                    {t(stockUseStatusLabelKey(item.status))}
                  </StatusChip>
                  <ChevronRight
                    className="size-4 shrink-0 text-muted"
                    aria-hidden
                  />
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
            disabled={!canPrev || query.isFetching}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
          >
            {t("stockUse.prevPage")}
          </Button>
          <span className="text-[length:var(--exits-text-sm)] text-muted">
            {t("stockUse.pageOf")
              .replace("{page}", String(page))
              .replace("{pages}", String(totalPages))}
          </span>
          <Button
            type="button"
            variant="outline"
            disabled={!canNext || query.isFetching}
            onClick={() => setPage((p) => p + 1)}
          >
            {t("stockUse.nextPage")}
          </Button>
        </div>
      ) : null}
    </div>
  );
}
