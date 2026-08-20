import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { listInventory } from "@/api/pos/pos-inventory-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function InventoryListPage() {
  const { t } = useI18n();
  const { boundWorkspace } = useWorkspace();
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
    queryKey: ["inventory", workspace?.organizationId, workspace?.branchId, debounced],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) =>
      listInventory(workspace!, { search: debounced || undefined, pageSize: 50 }, signal),
  });

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="inventory-list-page">
      <PageHeader title={t("inventory.title")} description={t("inventory.lede")} />
      <Button asChild variant="ghost" className="min-h-11 w-fit" data-testid="open-expiring-stock">
        <Link to="/inventory/expiration">{t("inventory.openExpiring")}</Link>
      </Button>
      <SearchField
        label={t("inventory.search")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("inventory.search")}
      />
      {query.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {query.isError ? (
        <ErrorState title={t("error.title")} detail={(query.error as Error).message} />
      ) : null}
      {query.isSuccess && query.data.items.length === 0 ? (
        <EmptyState title={t("inventory.empty")} detail={t("inventory.emptyDetail")} />
      ) : null}
      <ul className="m-0 flex list-none flex-col gap-2 p-0">
        {query.data?.items.map((item) => (
          <li key={item.productId}>
            <Card className="p-3">
              <Link
                className="block min-w-0 text-foreground no-underline"
                to={`/inventory/${item.productId}`}
                data-testid={`inventory-row-${item.productId}`}
              >
                <span className="block truncate font-semibold">{item.name}</span>
                <span className="block truncate text-[length:var(--exits-text-sm)] text-muted">
                  {item.isTracked
                    ? `${t("inventory.onHand")}: ${item.onHandQuantity} ${item.unitOfMeasure}`
                    : t("inventory.notTracked")}
                </span>
              </Link>
            </Card>
          </li>
        ))}
      </ul>
      <Button asChild variant="ghost" className="min-h-11 w-fit">
        <Link to="/role/manager">{t("inventory.backOps")}</Link>
      </Button>
    </div>
  );
}
