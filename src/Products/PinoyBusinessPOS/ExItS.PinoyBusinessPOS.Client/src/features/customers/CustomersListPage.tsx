import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, Plus } from "lucide-react";
import { canCreateCustomer } from "@/access/pos-capabilities";
import { listCustomers, type PosCustomerListItem } from "@/api/pos/pos-customers-client";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import {
  cacheCustomers,
  filterCachedCustomers,
  listCachedCustomers,
} from "@/offline/customer-cache";
import { useOrganizationOfflineContext } from "@/offline/organization-offline-context";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type StatusFilter = "Active" | "Inactive" | "";

const STATUS_FILTERS: Array<{
  value: StatusFilter;
  key: string;
  labelKey: "customers.statusActive" | "customers.statusInactive" | "customers.statusAll";
}> = [
  { value: "Active", key: "Active", labelKey: "customers.statusActive" },
  { value: "Inactive", key: "Inactive", labelKey: "customers.statusInactive" },
  { value: "", key: "all", labelKey: "customers.statusAll" },
];

function customerStatusTone(status: string): "success" | "warning" {
  return status.toLowerCase() === "active" ? "success" : "warning";
}

function customerMeta(
  customer: PosCustomerListItem,
  exItsIdHint: string,
): string {
  const parts = [customer.mobileNumber].filter(Boolean);
  // POS-local ExItS ID / buyer org fields prove identity correlation only —
  // not Platform CustomerLink / LinkedCustomerAppUser Active status.
  if (customer.linkedPersonalPublicUserId || customer.linkedBuyerPublicOrganizationId) {
    parts.push(exItsIdHint);
  }
  return parts.join(" · ");
}

export function CustomersListPage() {
  const { t } = useI18n();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const online = useBrowserOnline();
  const offlineContext = useOrganizationOfflineContext();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [status, setStatus] = useState<StatusFilter>("Active");
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
    <div
      className="customers-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="customers-list-page"
    >
      <PageHeader
        title={t("customers.title")}
        description={t("customers.lede")}
        backTo={pageBackNav.managerHome.to}
        backLabel={t(pageBackNav.managerHome.labelKey)}
        backTestId="page-header-back-customers"
      />

      {allowCreate ? (
        <ExitsChipBar
          variant="actions"
          ariaLabel={t("customers.title")}
          testId="customers-toolbar"
          className="exits-animate-toolbar"
          items={[
            {
              key: "new",
              label: t("customers.add"),
              icon: <Plus />,
              href: "/customers/new",
              testId: "customers-new",
              emphasis: "primary",
            },
          ]}
        />
      ) : null}

      <SearchField
        label={t("customers.search")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("customers.search")}
        data-testid="customers-search"
        containerClassName="customers-page__search exits-page__search"
      />

      <ExitsChipBar
        variant="filter"
        ariaLabel={t("customers.statusFilter")}
        testId="customers-status-filters"
        items={STATUS_FILTERS.map((filter) => ({
          key: filter.key,
          label: t(filter.labelKey),
          state: (status || "all") === filter.key ? "active" : "idle",
          testId: `customers-status-${filter.key === "all" ? "all" : filter.key}`,
          onSelect: () => setStatus(filter.value),
        }))}
      />

      {usingCache ? (
        <div className="exits-alert" data-testid="customers-cached-notice" role="status">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("offline.cachedCustomersNotice")}
          </p>
        </div>
      ) : null}

      {query.isLoading && !usingCache ? <LoadingState label={t("loading.label")} /> : null}
      {query.isError && !usingCache ? (
        <ErrorState title={t("error.title")} detail={(query.error as Error).message} />
      ) : null}
      {(query.isSuccess || usingCache) && items.length === 0 ? (
        <EmptyState title={t("customers.empty")} detail={t("customers.emptyDetail")} />
      ) : null}

      <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="customers-list">
        {items.map((customer) => {
          const meta = customerMeta(customer, t("customers.listExItsIdHint"));
          return (
            <li key={customer.customerId}>
              <Link
                className="exits-list__card customer-row block min-w-0 text-foreground no-underline"
                to={`/customers/${customer.customerId}`}
                data-testid={`customer-row-${customer.customerId}`}
              >
                <span className="customer-row__main min-w-0">
                  <span className="exits-list__name block truncate font-semibold">
                    {customer.displayName}
                  </span>
                  {meta ? (
                    <span className="customer-row__meta mt-1 block truncate text-[length:var(--exits-text-sm)] text-muted">
                      {meta}
                    </span>
                  ) : null}
                </span>
                <span className="customer-row__aside">
                  <StatusChip tone={customerStatusTone(customer.status)}>{customer.status}</StatusChip>
                  <ChevronRight className="customer-row__chevron size-4 shrink-0 text-muted" aria-hidden />
                </span>
              </Link>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
