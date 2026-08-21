import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canManageSuppliers } from "@/access/pos-capabilities";
import { listSuppliers, resolveSupplierSearchParams } from "@/api/pos/pos-suppliers-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PAGE_SIZE = 20;

export function SuppliersListPage() {
  const { t } = useI18n();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [status, setStatus] = useState<"Active" | "Inactive" | "">("Active");
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

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  const totalCount = query.data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const canPrev = page > 1;
  const canNext = page < totalPages && totalCount > 0;

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="suppliers-list-page">
      <PageHeader title={t("suppliers.title")} description={t("suppliers.lede")} />
      <div className="flex flex-wrap gap-2">
        {allowManage ? (
          <Button asChild className="min-h-11" data-testid="suppliers-new">
            <Link to="/suppliers/new">{t("suppliers.add")}</Link>
          </Button>
        ) : null}
      </div>
      <SearchField
        label={t("suppliers.search")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("suppliers.search")}
        data-testid="suppliers-search"
      />
      <div className="flex flex-wrap gap-2" role="group" aria-label={t("suppliers.statusFilter")}>
        {(
          [
            ["Active", "suppliers.statusActive"],
            ["Inactive", "suppliers.statusInactive"],
            ["", "suppliers.statusAll"],
          ] as const
        ).map(([value, labelKey]) => (
          <Button
            key={value || "all"}
            type="button"
            variant={status === value ? "default" : "ghost"}
            className="min-h-11"
            data-testid={`suppliers-status-${value || "all"}`}
            onClick={() => setStatus(value)}
          >
            {t(labelKey)}
          </Button>
        ))}
      </div>
      {query.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {query.isError ? (
        <ErrorState title={t("error.title")} detail={(query.error as Error).message} />
      ) : null}
      {query.isSuccess && query.data.items.length === 0 ? (
        <EmptyState title={t("suppliers.empty")} detail={t("suppliers.emptyDetail")} />
      ) : null}
      <ul
        className="m-0 grid list-none grid-cols-1 gap-2 p-0 sm:grid-cols-2 lg:grid-cols-1"
        data-testid="suppliers-list"
      >
        {query.data?.items.map((supplier) => (
          <li key={supplier.supplierId}>
            <Card className="p-3">
              <Link
                className="block min-w-0 text-foreground no-underline"
                to={`/suppliers/${supplier.supplierId}`}
                data-testid={`supplier-row-${supplier.supplierId}`}
              >
                <span className="block truncate font-semibold">{supplier.name}</span>
                <span className="mt-1 flex flex-wrap items-center gap-2 text-[length:var(--exits-text-sm)] text-muted">
                  <span className="truncate">{supplier.supplierCode}</span>
                  <StatusChip
                    tone={supplier.status.toLowerCase() === "active" ? "success" : "warning"}
                  >
                    {supplier.status}
                  </StatusChip>
                </span>
              </Link>
            </Card>
          </li>
        ))}
      </ul>
      {query.isSuccess && totalCount > 0 ? (
        <div
          className="flex flex-wrap items-center justify-between gap-2"
          data-testid="suppliers-pagination"
        >
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("suppliers.pageLabel")
              .replace("{page}", String(page))
              .replace("{totalPages}", String(totalPages))}
          </p>
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="ghost"
              className="min-h-11"
              data-testid="suppliers-prev"
              disabled={!canPrev}
              onClick={() => setPage((current) => Math.max(1, current - 1))}
            >
              {t("suppliers.prevPage")}
            </Button>
            <Button
              type="button"
              variant="ghost"
              className="min-h-11"
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
