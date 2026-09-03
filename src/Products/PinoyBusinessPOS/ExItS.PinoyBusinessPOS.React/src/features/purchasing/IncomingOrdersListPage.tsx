import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight } from "lucide-react";
import { canViewPurchasing } from "@/access/pos-capabilities";
import { listIncomingOrders } from "@/api/pos/pos-connected-suppliers-client";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  countIncomingLines,
  countIncomingUnits,
  filterIncomingOrdersBySearch,
  incomingOrderStatusTone,
  uiFilterToApiStatus,
  type IncomingOrdersUiFilter,
} from "@/features/purchasing/incoming-orders-helpers";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const FILTERS: Array<{
  value: IncomingOrdersUiFilter;
  labelKey:
    | "incomingOrders.filterAll"
    | "incomingOrders.filterPending"
    | "incomingOrders.filterAccepted"
    | "incomingOrders.filterPreparing"
    | "incomingOrders.filterCompleted"
    | "incomingOrders.filterDeclined";
}> = [
  { value: "all", labelKey: "incomingOrders.filterAll" },
  { value: "pending", labelKey: "incomingOrders.filterPending" },
  { value: "accepted", labelKey: "incomingOrders.filterAccepted" },
  { value: "preparing", labelKey: "incomingOrders.filterPreparing" },
  { value: "completed", labelKey: "incomingOrders.filterCompleted" },
  { value: "declined", labelKey: "incomingOrders.filterDeclined" },
];

function statusLabel(t: (key: MessageKey) => string, status: string, displayStatus: string): string {
  switch (status) {
    case "New":
      return t("incomingOrders.statusPending");
    case "Accepted":
      return t("incomingOrders.statusAccepted");
    case "Preparing":
      return t("incomingOrders.statusPreparing");
    case "Fulfilled":
      return t("incomingOrders.statusCompleted");
    case "Declined":
      return t("incomingOrders.statusDeclined");
    case "Withdrawn":
      return t("incomingOrders.statusWithdrawn");
    case "ChangesProposed":
      return t("incomingOrders.statusChangesProposed");
    default:
      return displayStatus || status;
  }
}

export function IncomingOrdersListPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [filter, setFilter] = useState<IncomingOrdersUiFilter>("pending");
  const [search, setSearch] = useState("");

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const allowView = canViewPurchasing(sessionGrant);
  const apiStatus = uiFilterToApiStatus(filter);

  const query = useQuery({
    queryKey: ["connected-suppliers", "incoming-orders", workspace?.organizationId, apiStatus ?? "All"],
    enabled: Boolean(workspace) && online && allowView,
    queryFn: ({ signal }) => listIncomingOrders(workspace!, { status: apiStatus }, signal),
  });

  const filtered = useMemo(
    () => filterIncomingOrdersBySearch(query.data ?? [], search),
    [query.data, search],
  );

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  return (
    <div
      className="incoming-orders-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="incoming-orders-list-page"
    >
      <PageHeader
        title={t("incomingOrders.title")}
        description={t("incomingOrders.lede")}
        backTo={pageBackNav.purchasing.to}
        backLabel={t(pageBackNav.purchasing.labelKey)}
        backTestId="page-header-back-incoming-orders"
      />

      {!online ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="incoming-orders-offline">
          {t("purchasing.offline")}
        </p>
      ) : null}

      <SearchField
        label={t("incomingOrders.search")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("incomingOrders.search")}
        data-testid="incoming-orders-search"
      />

      <ExitsChipBar
        variant="filter"
        className="exits-chip-bar--scroll"
        ariaLabel={t("incomingOrders.statusFilter")}
        testId="incoming-orders-status-filter"
        items={FILTERS.map((item) => ({
          key: item.value,
          label: t(item.labelKey),
          state: filter === item.value ? "active" : "idle",
          testId: `incoming-orders-filter-${item.value}`,
          onSelect: () => setFilter(item.value),
        }))}
      />

      {query.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {query.isError ? (
        <ErrorState title={t("error.title")} detail={t("incomingOrders.loadFailed")} />
      ) : null}
      {query.isSuccess && (query.data?.length ?? 0) === 0 && !search.trim() ? (
        <EmptyState title={t("incomingOrders.empty")} detail={t("incomingOrders.emptyHelp")} />
      ) : null}
      {query.isSuccess && filtered.length === 0 && search.trim() ? (
        <EmptyState title={t("incomingOrders.noMatch")} detail={t("incomingOrders.noMatchHelp")} />
      ) : null}

      <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="incoming-orders-list">
        {filtered.map((order) => {
          const products = countIncomingLines(order);
          const units = countIncomingUnits(order);
          const buyer = order.buyerDisplayName?.trim() || t("incomingOrders.buyer");
          const branch = order.supplierBranchName?.trim();
          return (
            <li key={order.connectedPurchaseOrderId}>
              <Link
                to={`/purchasing/incoming-orders/${order.connectedPurchaseOrderId}`}
                className="exits-list__card purchasing-row block min-w-0 text-foreground no-underline"
                data-testid={`incoming-order-row-${order.connectedPurchaseOrderId}`}
              >
                <span className="purchasing-row__main min-w-0">
                  <span className="exits-list__name block truncate font-semibold">
                    {order.buyerPoNumber ?? t("incomingOrders.unnamedPo")}
                  </span>
                  <span className="purchasing-row__meta mt-1 block truncate text-[length:var(--exits-text-sm)] text-muted">
                    {buyer}
                    {branch ? ` · ${branch}` : ""}
                  </span>
                  <span className="purchasing-row__meta mt-1 block truncate text-[length:var(--exits-text-sm)] text-muted">
                    {t("incomingOrders.summary")
                      .replace("{products}", String(products))
                      .replace("{units}", String(units))}
                    {" · "}
                    {order.orderDate}
                  </span>
                  <span className="mt-1 block font-semibold tabular-nums">
                    <MoneyDisplay amount={order.totalAmount} />
                  </span>
                </span>
                <span className="purchasing-row__aside gap-2">
                  <StatusChip tone={incomingOrderStatusTone(order.status)}>
                    {statusLabel(t, order.status, order.displayStatus)}
                  </StatusChip>
                  <span className="text-[length:var(--exits-text-sm)] font-medium text-[var(--exits-primary)]">
                    {t("incomingOrders.review")}
                  </span>
                  <ChevronRight className="purchasing-row__chevron size-4 shrink-0 text-muted" aria-hidden />
                </span>
              </Link>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
