import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canCreateCustomer } from "@/access/pos-capabilities";
import { listCustomers, type PosCustomerListItem } from "@/api/pos/pos-customers-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import { SearchField } from "@/components/exits/SearchField";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import {
  cacheCustomers,
  filterCachedCustomers,
  listCachedCustomers,
} from "@/offline/customer-cache";
import { useOrganizationOfflineContext } from "@/offline/organization-offline-context";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function CustomersListPage() {
  const { t } = useI18n();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const online = useBrowserOnline();
  const offlineContext = useOrganizationOfflineContext();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [status, setStatus] = useState<"Active" | "Inactive" | "">("Active");
  const [cached, setCached] = useState<PosCustomerListItem[] | null>(null);

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

  const allowCreate = canCreateCustomer(sessionGrant);

  const query = useQuery({
    queryKey: [
      "customers",
      "list",
      workspace?.organizationId,
      workspace?.branchId,
      debounced,
      status,
    ],
    // Offline reads come from the cache below instead of burning retries on a dead network.
    enabled: Boolean(workspace) && online,
    queryFn: ({ signal }) =>
      listCustomers(
        workspace!,
        {
          search: debounced || undefined,
          status: status || undefined,
          pageSize: 50,
        },
        signal,
      ),
  });

  // Write-through only from a successful online read, so the cache can never invent a customer.
  useEffect(() => {
    if (!offlineContext || !query.isSuccess || !online) {
      return;
    }
    void cacheCustomers(offlineContext.db, offlineContext.scopeBinding, query.data.items).catch(
      () => {
        // A cache write failure must never break the customer list.
      },
    );
  }, [offlineContext, online, query.data, query.isSuccess]);

  const showCachedFallback = !online || query.isError;

  useEffect(() => {
    if (!offlineContext || !showCachedFallback) {
      setCached(null);
      return;
    }
    let cancelled = false;
    void listCachedCustomers(offlineContext.db, offlineContext.scopeBinding).then((customers) => {
      if (!cancelled) {
        setCached(customers);
      }
    });
    return () => {
      cancelled = true;
    };
  }, [offlineContext, showCachedFallback]);

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  const usingCache = showCachedFallback && cached !== null;
  const items = usingCache
    ? filterCachedCustomers(cached, { search: debounced, status })
    : (query.data?.items ?? []);

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="customers-list-page">
      <PageHeader title={t("customers.title")} description={t("customers.lede")} />
      <div className="flex flex-wrap gap-2">
        {allowCreate ? (
          <Button asChild className="min-h-11" data-testid="customers-new">
            <Link to="/customers/new">{t("customers.add")}</Link>
          </Button>
        ) : null}
      </div>
      <SearchField
        label={t("customers.search")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("customers.search")}
      />
      <UnderlineTabBar
        items={(
          [
            ["Active", "customers.statusActive"],
            ["Inactive", "customers.statusInactive"],
            ["", "customers.statusAll"],
          ] as const
        ).map(([value, labelKey]) => ({
          key: value || "all",
          label: t(labelKey),
          testId: `customers-status-${value || "all"}`,
        }))}
        activeKey={status || "all"}
        onChange={(key) =>
          setStatus(key === "all" ? "" : (key as "Active" | "Inactive"))
        }
        ariaLabel={t("customers.statusFilter")}
      />
      {usingCache ? (
        <Card data-testid="customers-cached-notice">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("offline.cachedCustomersNotice")}
          </p>
        </Card>
      ) : null}
      {query.isLoading && !usingCache ? <LoadingState label={t("loading.label")} /> : null}
      {query.isError && !usingCache ? (
        <ErrorState title={t("error.title")} detail={(query.error as Error).message} />
      ) : null}
      {(query.isSuccess || usingCache) && items.length === 0 ? (
        <EmptyState title={t("customers.empty")} detail={t("customers.emptyDetail")} />
      ) : null}
      <ul className="m-0 flex list-none flex-col gap-2 p-0" data-testid="customers-list">
        {items.map((customer) => (
          <li key={customer.customerId}>
            <Card className="p-3">
              <Link
                className="block min-w-0 text-foreground no-underline"
                to={`/customers/${customer.customerId}`}
                data-testid={`customer-row-${customer.customerId}`}
              >
                <span className="block truncate font-semibold">{customer.displayName}</span>
                <span className="block truncate text-[length:var(--exits-text-sm)] text-muted">
                  {[customer.mobileNumber, customer.status].filter(Boolean).join(" · ")}
                </span>
              </Link>
            </Card>
          </li>
        ))}
      </ul>
    </div>
  );
}
