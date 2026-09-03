import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canManageSuppliers, canViewSuppliers } from "@/access/pos-capabilities";
import { listRelationships } from "@/api/pos/pos-connected-suppliers-client";
import { listSuppliers, resolveSupplierSearchParams } from "@/api/pos/pos-suppliers-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { BackgroundRefreshIndicator } from "@/components/exits/loading/BackgroundRefreshIndicator";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { SearchField } from "@/components/exits/SearchField";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { BranchRequiredPanel } from "@/features/workspace/BranchRequiredPanel";

const PAGE_SIZE = 20;

type StatusFilter = "Active" | "Inactive" | "";

const STATUS_FILTERS: Array<{ value: StatusFilter; key: string; labelKey: "suppliers.statusActive" | "suppliers.statusInactive" | "suppliers.statusAll" }> = [
  { value: "Active", key: "Active", labelKey: "suppliers.statusActive" },
  { value: "Inactive", key: "Inactive", labelKey: "suppliers.statusInactive" },
  { value: "", key: "all", labelKey: "suppliers.statusAll" },
];

export function SuppliersListPage() {
  const { t } = useI18n();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [status, setStatus] = useState<StatusFilter>("Active");
  const [page, setPage] = useState(1);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  useEffect(() => {
    setPage(1);
  }, [debounced, status]);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const allowManage = canManageSuppliers(sessionGrant);
  const allowView = canViewSuppliers(sessionGrant);
  const searchParams = resolveSupplierSearchParams(debounced);

  const query = useQuery({
    queryKey: [
      "suppliers",
      "list",
      workspace?.organizationId,
      workspace?.branchId,
      debounced,
      status,
      page,
    ],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) =>
      listSuppliers(
        workspace!,
        {
          ...searchParams,
          status: status || undefined,
          page,
          pageSize: PAGE_SIZE,
        },
        signal,
      ),
  });

  const incomingQuery = useQuery({
    queryKey: ["connected-suppliers", "incoming-count", workspace?.organizationId],
    enabled: Boolean(workspace) && allowView,
    queryFn: async ({ signal }) => {
      const rows = await listRelationships(workspace!, "supplier", signal);
      return rows.filter((row) => row.status.toLowerCase() === "pending").length;
    },
  });

  if (!workspace) {
    return <BranchRequiredPanel title={t("suppliers.title")} />;
  }

  const totalCount = query.data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const canPrev = page > 1;
  const canNext = page < totalPages && totalCount > 0;
  const incomingCount = incomingQuery.data ?? 0;

  return (
    <div className="suppliers-page exits-page flex min-w-0 flex-col gap-3" data-testid="suppliers-list-page">
      <PageHeader
        title={t("suppliers.title")}
        description={t("suppliers.lede")}
        backTo={pageBackNav.managerHome.to}
        backLabel={t(pageBackNav.managerHome.labelKey)}
        backTestId="page-header-back-suppliers"
      />

      <ExitsChipBar
        variant="actions"
        ariaLabel={t("suppliers.title")}
        testId="suppliers-toolbar"
        className="exits-animate-toolbar"
        items={[
          ...(allowManage
            ? [
                {
                  key: "add",
                  label: t("suppliers.add"),
                  href: "/suppliers/new",
                  testId: "suppliers-new",
                  emphasis: "primary" as const,
                },
                {
                  key: "connect",
                  label: t("connected.requestConnection"),
                  href: "/suppliers/connected/request",
                  testId: "suppliers-connect",
                },
              ]
            : []),
          ...(allowView
            ? [
                {
                  key: "incoming",
                  label:
                    incomingCount > 0
                      ? t("connected.incomingCompact").replace("{count}", String(incomingCount))
                      : t("connected.incomingRequests"),
                  href: "/suppliers/connected/requests",
                  testId: "suppliers-incoming",
                },
                {
                  key: "buyers",
                  label: t("connected.buyersTitle"),
                  href: "/customers?kind=businesses",
                  testId: "suppliers-connected-buyers",
                },
              ]
            : []),
        ]}
      />

      <SearchField
        label={t("suppliers.search")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("suppliers.search")}
        data-testid="suppliers-search"
        containerClassName="suppliers-page__search exits-page__search"
      />

      <ExitsChipBar
        variant="filter"
        ariaLabel={t("suppliers.statusFilter")}
        testId="suppliers-status-filters"
        items={STATUS_FILTERS.map((filter) => ({
          key: filter.key,
          label: t(filter.labelKey),
          state: (status || "all") === filter.key ? "active" : "idle",
          testId: `suppliers-status-${filter.key === "all" ? "all" : filter.key}`,
          onSelect: () => setStatus(filter.value),
        }))}
      />

      {query.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {query.isFetching && !query.isLoading && query.data ? (
        <BackgroundRefreshIndicator active label={t("loading.updating")} />
      ) : null}
      {query.isError ? (
        <ErrorState title={t("error.title")} detail={(query.error as Error).message} />
      ) : null}
      {query.isSuccess && query.data.items.length === 0 ? (
        <EmptyState title={t("suppliers.empty")} detail={t("suppliers.emptyDetail")} />
      ) : null}

      <ul className="exits-list suppliers-list m-0 grid list-none gap-2 p-0" data-testid="suppliers-list">
        {query.data?.items.map((supplier) => (
          <li key={supplier.supplierId}>
            <Link
              className="exits-list__card suppliers-list__card block min-w-0 text-foreground no-underline"
              to={`/suppliers/${supplier.supplierId}`}
              data-testid={`supplier-row-${supplier.supplierId}`}
            >
              <span className="suppliers-list__name block truncate font-semibold">
                {supplier.name}
              </span>
              <span className="mt-1 flex min-w-0 flex-wrap items-center gap-2 text-[length:var(--exits-text-sm)] text-muted">
                <span className="min-w-0 truncate">{supplier.supplierCode}</span>
                <StatusChip
                  tone={supplier.status.toLowerCase() === "active" ? "success" : "warning"}
                >
                  {supplier.status}
                </StatusChip>
              </span>
              {supplier.connectedBusinessPublicId || supplier.supplierBranchName ? (
                <span
                  className="mt-1 flex min-w-0 flex-col gap-0.5 text-[length:var(--exits-text-sm)] text-muted"
                  data-testid={`supplier-connected-meta-${supplier.supplierId}`}
                >
                  {supplier.connectedBusinessPublicId ? (
                    <span className="min-w-0 truncate">
                      {t("suppliers.listOrgId").replace(
                        "{id}",
                        supplier.connectedBusinessPublicId,
                      )}
                    </span>
                  ) : null}
                  {supplier.supplierBranchName ? (
                    <span className="min-w-0 truncate">
                      {t("suppliers.listBranch").replace(
                        "{name}",
                        supplier.supplierBranchName,
                      )}
                    </span>
                  ) : null}
                </span>
              ) : null}
            </Link>
          </li>
        ))}
      </ul>

      {query.isSuccess && totalCount > 0 ? (
        <div
          className="exits-pagination suppliers-pagination"
          data-testid="suppliers-pagination"
        >
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("suppliers.pageLabel")
              .replace("{page}", String(page))
              .replace("{totalPages}", String(totalPages))}
          </p>
          <div className="exits-pagination__actions suppliers-pagination__actions flex flex-wrap gap-2">
            <Button
              type="button"
              variant="ghost"
              className="min-h-9"
              data-testid="suppliers-prev"
              disabled={!canPrev}
              onClick={() => setPage((current) => Math.max(1, current - 1))}
            >
              {t("suppliers.prevPage")}
            </Button>
            <Button
              type="button"
              variant="ghost"
              className="min-h-9"
              data-testid="suppliers-next"
              disabled={!canNext}
              onClick={() => setPage((current) => current + 1)}
            >
              {t("suppliers.nextPage")}
            </Button>
          </div>
        </div>
      ) : null}
    </div>
  );
}
