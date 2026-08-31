import { useEffect, useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, Plus } from "lucide-react";
import { canCreateCustomer, canViewSuppliers } from "@/access/pos-capabilities";
import {
  listBusinessCustomers,
  type BusinessCustomer,
} from "@/api/pos/pos-connected-suppliers-client";
import { listCustomers, type PosCustomerListItem } from "@/api/pos/pos-customers-client";
import { listOrganizationBusinessCustomers } from "@/api/platform/business-customer-delivery-client";
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
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";
import { CustomerListConnectionBadges } from "@/features/customers/CustomerListConnectionBadges";
import { parseKindForTest, type KindFilter } from "@/features/customers/customers-kind";
import { useOrganizationCustomerLinkOverlay } from "@/features/customers/use-organization-customer-link-overlay";

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

const KIND_FILTERS: Array<{
  value: KindFilter;
  labelKey:
    | "customers.kindAll"
    | "customers.kindPeople"
    | "customers.kindBusinesses";
}> = [
  { value: "all", labelKey: "customers.kindAll" },
  { value: "people", labelKey: "customers.kindPeople" },
  { value: "businesses", labelKey: "customers.kindBusinesses" },
];

function customerStatusTone(status: string): "success" | "warning" {
  return status.toLowerCase() === "active" ? "success" : "warning";
}

function discountLabel(
  customer: BusinessCustomer,
  discountTemplate: string,
): string | null {
  if (customer.customerDiscountPercent != null && customer.customerDiscountPercent > 0) {
    return discountTemplate.replace("{percent}", String(customer.customerDiscountPercent));
  }
  return null;
}

export function CustomersListPage() {
  const { t } = useI18n();
  const [searchParams, setSearchParams] = useSearchParams();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const workspace = usePosWorkspaceScope();
  const online = useBrowserOnline();
  const customerLinkOverlay = useOrganizationCustomerLinkOverlay(boundWorkspace?.organizationId);
  const offlineContext = useOrganizationOfflineContext();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [status, setStatus] = useState<StatusFilter>("Active");
  const [cached, setCached] = useState<PosCustomerListItem[] | null>(null);
  const kind = parseKindForTest(searchParams.get("kind"));
  const allowCreate = canCreateCustomer(sessionGrant);
  const allowBusiness = canViewSuppliers(sessionGrant);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  const setKind = (next: KindFilter) => {
    const params = new URLSearchParams(searchParams);
    if (next === "all") params.delete("kind");
    else params.set("kind", next);
    setSearchParams(params, { replace: true });
  };

  const showPeople = kind === "all" || kind === "people";
  const showBusinesses = allowBusiness && (kind === "all" || kind === "businesses");
  /** Status chips only on People tab — avoids two competing “All” filters on All. */
  const showStatusFilter = kind === "people";

  const peopleQuery = useQuery({
    queryKey: [
      "customers",
      "list",
      workspace?.organizationId,
      workspace?.branchId,
      debounced,
      status,
    ],
    enabled: Boolean(workspace) && online && showPeople,
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

  const businessQuery = useQuery({
    queryKey: [
      "business-customers",
      "list",
      workspace?.organizationId,
      debounced,
    ],
    enabled: Boolean(workspace) && online && showBusinesses,
    queryFn: ({ signal }) =>
      listBusinessCustomers(workspace!, { search: debounced || undefined }, signal),
  });

  const deliveryExceptionQuery = useQuery({
    queryKey: ["customers", "delivery-exception-ids", workspace?.organizationId],
    enabled: Boolean(workspace) && online && showPeople,
    queryFn: async ({ signal }) => {
      const page = await listOrganizationBusinessCustomers(workspace!.organizationId, {
        page: 1,
        pageSize: 100,
        signal,
      });
      return new Set(
        page.items
          .filter((c) => c.allowDeliveryBeyondNormalDistance)
          .map((c) => c.id),
      );
    },
  });

  const distanceExceptionIds = deliveryExceptionQuery.data;

  useEffect(() => {
    if (!offlineContext || !peopleQuery.isSuccess || !online) {
      return;
    }
    void cacheCustomers(offlineContext.db, offlineContext.scopeBinding, peopleQuery.data.items).catch(
      () => {
        // A cache write failure must never break the customer list.
      },
    );
  }, [offlineContext, online, peopleQuery.data, peopleQuery.isSuccess]);

  const showCachedFallback = showPeople && (!online || peopleQuery.isError);

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

  const usingCache = showCachedFallback && cached !== null;
  const peopleItems = usingCache
    ? filterCachedCustomers(cached, { search: debounced, status })
    : (peopleQuery.data?.items ?? []);

  const businessItems = useMemo(() => businessQuery.data ?? [], [businessQuery.data]);
  const peopleReady = peopleQuery.isSuccess || usingCache;
  const businessesReady = businessQuery.isSuccess;

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

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
        trailing={
          allowCreate && showPeople ? (
            <Link
              to="/customers/new"
              className="customers-page__add"
              data-testid="customers-new"
              aria-label={t("customers.add")}
            >
              <Plus className="size-4 shrink-0" aria-hidden />
              <span className="customers-page__add-label">{t("customers.add")}</span>
            </Link>
          ) : null
        }
      />

      <SearchField
        label={t("customers.search")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={
          kind === "businesses"
            ? t("customers.business.search")
            : t("customers.search")
        }
        data-testid="customers-search"
        containerClassName="customers-page__search exits-page__search"
      />

      {allowBusiness ? (
        <ExitsChipBar
          variant="filter"
          ariaLabel={t("customers.kindFilter")}
          testId="customers-kind-filters"
          items={KIND_FILTERS.map((filter) => ({
            key: filter.value,
            label: t(filter.labelKey),
            state: kind === filter.value ? "active" : "idle",
            testId: `customers-kind-${filter.value}`,
            onSelect: () => setKind(filter.value),
          }))}
        />
      ) : null}

      {showStatusFilter ? (
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
      ) : null}

      {usingCache ? (
        <div className="exits-alert" data-testid="customers-cached-notice" role="status">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("offline.cachedCustomersNotice")}
          </p>
        </div>
      ) : null}

      {showPeople ? (
        <section className="customers-section" data-testid="customers-people-section">
          {kind === "all" ? (
            <div className="customers-section__head">
              <h2 className="customers-section__title">{t("customers.kindPeople")}</h2>
              {peopleReady ? (
                <span className="customers-section__count">{peopleItems.length}</span>
              ) : null}
            </div>
          ) : null}
          {peopleQuery.isLoading && !usingCache ? <LoadingState label={t("loading.label")} /> : null}
          {peopleQuery.isError && !usingCache ? (
            <ErrorState title={t("error.title")} detail={(peopleQuery.error as Error).message} />
          ) : null}
          {peopleReady && peopleItems.length === 0 ? (
            kind === "all" ? (
              <p className="customers-section__empty-inline" data-testid="customers-people-empty">
                {t("customers.peopleEmptyCompact")}
              </p>
            ) : (
              <EmptyState title={t("customers.empty")} detail={t("customers.emptyDetail")} />
            )
          ) : null}
          <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="customers-list">
            {peopleItems.map((customer) => {
              const phone = customer.mobileNumber?.trim() || "";
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
                      {phone ? (
                        <span className="customer-row__meta mt-1 block truncate text-[length:var(--exits-text-sm)] text-muted">
                          {phone}
                        </span>
                      ) : null}
                      <CustomerListConnectionBadges
                        customer={customer}
                        overlay={customerLinkOverlay}
                        className="customer-row__badges"
                      />
                      {customer.platformBusinessCustomerId &&
                      distanceExceptionIds?.has(customer.platformBusinessCustomerId) ? (
                        <StatusChip tone="info">
                          <span data-testid={`customer-distance-exception-badge-${customer.customerId}`}>
                            {t("customers.delivery.distanceExceptionBadge")}
                          </span>
                        </StatusChip>
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
        </section>
      ) : null}

      {showBusinesses ? (
        <section className="customers-section" data-testid="customers-business-section">
          {kind === "all" ? (
            <div className="customers-section__head">
              <h2 className="customers-section__title">{t("customers.kindBusinesses")}</h2>
              {businessesReady ? (
                <span className="customers-section__count">{businessItems.length}</span>
              ) : null}
            </div>
          ) : null}
          {businessQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
          {businessQuery.isError ? (
            <div className="flex flex-col gap-2" data-testid="business-customers-error">
              <ErrorState
                title={t("customers.business.loadFailed")}
                detail={t("customers.business.loadFailedHelp")}
                error={businessQuery.error}
                operation="listBusinessCustomers"
              />
              <button
                type="button"
                className="exits-btn exits-btn--secondary self-start"
                data-testid="business-customers-retry"
                onClick={() => void businessQuery.refetch()}
              >
                {t("customers.business.retry")}
              </button>
            </div>
          ) : null}
          {businessesReady && businessItems.length === 0 ? (
            <EmptyState
              title={t("customers.business.empty")}
              detail={t("customers.business.emptyHelp")}
            />
          ) : null}
          <ul
            className="exits-list customers-business-list m-0 grid list-none gap-2 p-0"
            data-testid="business-customers-list"
          >
            {businessItems.map((customer) => {
              const name =
                customer.organizationDisplayName.trim() || t("customers.business.unknown");
              const pricing = discountLabel(
                customer,
                t("customers.business.discountShort"),
              );
              return (
                <li key={customer.connectionId}>
                  <Link
                    className="exits-list__card business-customer-row customer-row block min-w-0 text-foreground no-underline"
                    to={`/customers/business/${customer.connectionId}`}
                    data-testid={`business-customer-row-${customer.connectionId}`}
                  >
                    <span className="customer-row__main min-w-0">
                      <span className="business-customer-row__title">
                        <span
                          className="exits-list__name truncate font-semibold"
                          data-testid={`business-customer-name-${customer.connectionId}`}
                        >
                          {name}
                        </span>
                        <span className="business-customer-row__badge">
                          {t("customers.business.badgeShort")}
                        </span>
                      </span>
                      <span className="business-customer-row__facts">
                        <span>
                          {t("customers.business.sharedCountShort").replace(
                            "{count}",
                            String(customer.sharedCount),
                          )}
                        </span>
                        {pricing ? <span>{pricing}</span> : null}
                      </span>
                    </span>
                    <span className="customer-row__aside">
                      <StatusChip
                        tone={
                          customer.relationshipStatus.toLowerCase() === "active"
                            ? "success"
                            : "warning"
                        }
                      >
                        {customer.relationshipStatus}
                      </StatusChip>
                      <ChevronRight
                        className="customer-row__chevron size-4 shrink-0 text-muted"
                        aria-hidden
                      />
                    </span>
                  </Link>
                </li>
              );
            })}
          </ul>
        </section>
      ) : null}
    </div>
  );
}
