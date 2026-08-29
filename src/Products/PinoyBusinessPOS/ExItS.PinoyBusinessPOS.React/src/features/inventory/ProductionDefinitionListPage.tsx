import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, Plus } from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import { listProductionDefinitions } from "@/api/pos/pos-production-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  formatProductionDate,
  productionDefinitionStatusLabelKey,
} from "@/features/inventory/production-labels";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PAGE_SIZE = 20;

export function ProductionDefinitionListPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: [
      "production-definitions",
      workspace?.organizationId,
      workspace?.branchId,
      page,
      debounced,
    ],
    enabled: Boolean(workspace) && online,
    queryFn: ({ signal }) =>
      listProductionDefinitions(
        workspace!,
        { page, pageSize: PAGE_SIZE, search: debounced || undefined },
        signal,
      ),
  });

  useEffect(() => {
    setPage(1);
  }, [workspace?.organizationId, workspace?.branchId, debounced]);

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
      className="production-definition-list-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="production-definition-list-page"
    >
      <PageHeader
        title={t("production.setups.title")}
        description={t("production.setups.lede")}
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
          ariaLabel={t("production.setups.title")}
          testId="production-setups-toolbar"
          className="exits-animate-toolbar"
          items={[
            {
              key: "new",
              label: t("production.setups.new"),
              icon: <Plus />,
              href: online ? "/inventory/production/setups/new" : undefined,
              disabled: !online,
              testId: "production-setup-new",
              emphasis: "primary",
            },
          ]}
        />
      ) : null}

      <SearchField
        label={t("production.setups.search")}
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("production.setups.search")}
        data-testid="production-setup-search"
      />

      {query.isLoading ? <LoadingState label={t("production.loading")} /> : null}
      {query.isError ? (
        <ErrorState
          title={t("production.errorTitle")}
          detail={t("production.setups.loadFailed")}
        />
      ) : null}
      {query.isSuccess && items.length === 0 ? (
        <EmptyState
          title={t("production.setups.empty")}
          detail={t("production.setups.emptyDetail")}
        />
      ) : null}

      <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="production-setup-list">
        {items.map((item) => (
          <li key={item.productionDefinitionId}>
            <Link
              to={`/inventory/production/setups/${item.productionDefinitionId}`}
              className="exits-list__card block min-w-0 text-foreground no-underline"
              data-testid={`production-setup-row-${item.productionDefinitionId}`}
            >
              <span className="min-w-0">
                <span className="exits-list__name block truncate font-semibold">{item.name}</span>
                <span className="mt-1 block truncate text-[length:var(--exits-text-sm)] text-muted">
                  {[
                    formatProductionDate(item.createdAtUtc),
                    t("production.setups.componentsCount").replace(
                      "{count}",
                      String(item.componentCount),
                    ),
                    t("production.setups.revision").replace("{revision}", String(item.revision)),
                  ].join(" · ")}
                </span>
              </span>
              <span className="flex shrink-0 items-center gap-2">
                <StatusChip tone={item.isActive ? "success" : "warning"}>
                  {t(productionDefinitionStatusLabelKey(item.status))}
                </StatusChip>
                <ChevronRight className="size-4 shrink-0 text-muted" aria-hidden />
              </span>
            </Link>
          </li>
        ))}
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
