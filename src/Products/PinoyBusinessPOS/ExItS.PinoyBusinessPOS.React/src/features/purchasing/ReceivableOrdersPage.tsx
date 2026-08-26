import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, ClipboardList } from "lucide-react";
import {
  isPurchaseOrderReceivable,
  listPurchaseOrders,
  type PosPurchaseOrderDto,
} from "@/api/pos/pos-purchase-orders-client";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { pageBackNav } from "@/navigation/page-back-nav";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type ReceiptStatusFilter = "all" | "Ordered" | "PartiallyReceived";

const RECEIPT_FILTERS: Array<{
  value: ReceiptStatusFilter;
  key: string;
  labelKey: "purchasing.statusAll" | "purchasing.statusOrdered" | "purchasing.statusPartial";
}> = [
  { value: "all", key: "all", labelKey: "purchasing.statusAll" },
  { value: "Ordered", key: "Ordered", labelKey: "purchasing.statusOrdered" },
  { value: "PartiallyReceived", key: "PartiallyReceived", labelKey: "purchasing.statusPartial" },
];

function statusTone(status: string): "success" | "warning" | "info" | "danger" {
  switch (status) {
    case "Ordered":
      return "success";
    case "PartiallyReceived":
      return "warning";
    default:
      return "info";
  }
}

function poOutstandingSummary(po: PosPurchaseOrderDto) {
  const outstandingLines = po.lines.filter((line) => line.outstandingQty > 0);
  const totalOutstanding = outstandingLines.reduce((sum, line) => sum + line.outstandingQty, 0);
  return { totalOutstanding, lineCount: outstandingLines.length };
}

function matchesReceiptSearch(po: PosPurchaseOrderDto, query: string): boolean {
  if (!query) return true;
  const haystack = [
    po.poNumber,
    po.supplierName,
    po.displayStatus,
    po.status,
    po.supplierReference,
  ]
    .filter(Boolean)
    .join(" ")
    .toLowerCase();
  return haystack.includes(query.toLowerCase());
}

export function ReceivableOrdersPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { boundWorkspace } = useWorkspace();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [statusFilter, setStatusFilter] = useState<ReceiptStatusFilter>("all");

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
    queryKey: ["purchase-orders", "receivable", workspace?.organizationId, workspace?.branchId],
    enabled: Boolean(workspace) && online,
    queryFn: async ({ signal }) => {
      const ordered = await listPurchaseOrders(
        workspace!,
        { status: "Ordered", pageSize: 50 },
        signal,
      );
      const partial = await listPurchaseOrders(
        workspace!,
        { status: "PartiallyReceived", pageSize: 50 },
        signal,
      );
      return [...ordered.items, ...partial.items].filter((po) => isPurchaseOrderReceivable(po));
    },
  });

  const items = useMemo(() => {
    const all = query.data ?? [];
    return all.filter((po) => {
      if (statusFilter !== "all" && po.status !== statusFilter) return false;
      return matchesReceiptSearch(po, debounced);
    });
  }, [query.data, statusFilter, debounced]);

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  const hasLoaded = query.isSuccess;
  const totalReceivable = query.data?.length ?? 0;
  const showFilteredEmpty = hasLoaded && totalReceivable > 0 && items.length === 0;
  const showTrueEmpty = hasLoaded && totalReceivable === 0;

  return (
    <div
      className="purchasing-receipts-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="receivable-orders-page"
    >
      <PageHeader
        title={t("purchasing.receipts")}
        description={t("purchasing.receiptsLede")}
        backTo={pageBackNav.purchasing.to}
        backLabel={t(pageBackNav.purchasing.labelKey)}
        backTestId="page-header-back-purchasing"
      />
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("purchasing.receiptsStockNote")}
      </p>
      {!online ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("purchasing.offline")}</p>
      ) : null}

      <ExitsChipBar
        variant="actions"
        ariaLabel={t("purchasing.receipts")}
        testId="receivable-toolbar"
        className="exits-animate-toolbar"
        items={[
          {
            key: "orders",
            label: t("purchasing.orders"),
            icon: <ClipboardList />,
            href: "/purchasing/orders",
            testId: "receivable-open-orders",
          },
        ]}
      />

      <SearchField
        label={t("purchasing.searchReceipts")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("purchasing.searchReceipts")}
        data-testid="receivable-search"
        containerClassName="purchasing-receipts-page__search exits-page__search"
      />

      <ExitsChipBar
        variant="filter"
        ariaLabel={t("purchasing.receiptsFilter")}
        testId="receivable-status-filter"
        items={RECEIPT_FILTERS.map((filter) => ({
          key: filter.key,
          label: t(filter.labelKey),
          state: statusFilter === filter.value ? "active" : "idle",
          testId: `receivable-status-${filter.key}`,
          onSelect: () => setStatusFilter(filter.value),
        }))}
      />

      {query.isLoading ? <LoadingState label={t("purchasing.loading")} /> : null}
      {query.isError ? (
        <ErrorState title={t("purchasing.errorTitle")} detail={t("purchasing.loadFailed")} />
      ) : null}
      {showTrueEmpty ? (
        <EmptyState
          title={t("purchasing.receiptsEmpty")}
          detail={t("purchasing.receiptsEmptyDetail")}
        />
      ) : null}
      {showFilteredEmpty ? (
        <EmptyState
          title={t("purchasing.receiptsNoMatch")}
          detail={t("purchasing.receiptsNoMatchDetail")}
        />
      ) : null}

      <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="receivable-orders-list">
        {items.map((po) => {
          const { totalOutstanding, lineCount } = poOutstandingSummary(po);
          return (
            <li key={po.purchaseOrderId}>
              <Link
                to={`/purchasing/${po.purchaseOrderId}/receive`}
                className="exits-list__card purchasing-row block min-w-0 text-foreground no-underline"
                data-testid={`receivable-row-${po.purchaseOrderId}`}
              >
                <span className="purchasing-row__main min-w-0">
                  <span className="exits-list__name block truncate font-semibold">
                    {po.poNumber ?? t("purchasing.unnamedPo")}
                  </span>
                  <span className="purchasing-row__meta mt-1 block truncate text-[length:var(--exits-text-sm)] text-muted">
                    {po.supplierName ?? t("purchasing.unknownSupplier")} · {po.orderDate} ·{" "}
                    {t("purchasing.outstandingSummary")
                      .replace("{qty}", String(totalOutstanding))
                      .replace("{count}", String(lineCount))}
                  </span>
                </span>
                <span className="purchasing-row__aside">
                  <span className="purchasing-row__qty">
                    {totalOutstanding}
                    <span className="purchasing-row__uom">{t("purchasing.outstanding")}</span>
                  </span>
                  <StatusChip tone={statusTone(po.status)}>{po.displayStatus || po.status}</StatusChip>
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
