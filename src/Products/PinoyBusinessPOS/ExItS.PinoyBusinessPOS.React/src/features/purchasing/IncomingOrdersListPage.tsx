import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ClipboardList } from "lucide-react";
import { canViewPurchasing } from "@/access/pos-capabilities";
import { listIncomingOrders, type ConnectedPurchaseOrder } from "@/api/pos/pos-connected-suppliers-client";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import {
  ExitsTable,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableMobile,
  ExitsTableMobileRow,
  ExitsTableOutputActions,
  ExitsTablePagination,
  ExitsTableRow,
  ExitsTableToolbar,
} from "@/components/exits/ExitsTable";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { useToast } from "@/components/exits/ToastProvider";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  countIncomingLines,
  countIncomingUnits,
  filterIncomingOrdersBySearch,
  incomingOrderStatusTone,
  uiFilterToApiStatus,
  type IncomingOrdersUiFilter,
} from "@/features/purchasing/incoming-orders-helpers";
import {
  buildIncomingOrderListExportModel,
  downloadIncomingOrderListCsv,
  downloadIncomingOrderListPdf,
  downloadIncomingOrderListXlsx,
  printIncomingOrderListDocument,
} from "@/features/purchasing/incoming-orders-list-output";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PAGE_SIZE_OPTIONS = [10, 25, 50, 100] as const;

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
  const { showToast } = useToast();
  const navigate = useNavigate();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [filter, setFilter] = useState<IncomingOrdersUiFilter>("pending");
  const [searchInput, setSearchInput] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const allowView = canViewPurchasing(sessionGrant);
  const apiStatus = uiFilterToApiStatus(filter);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedSearch(searchInput.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [searchInput]);

  useEffect(() => {
    setPage(1);
  }, [filter, debouncedSearch]);

  const query = useQuery({
    queryKey: ["connected-suppliers", "incoming-orders", workspace?.organizationId, apiStatus ?? "All"],
    enabled: Boolean(workspace) && online && allowView,
    queryFn: ({ signal }) => listIncomingOrders(workspace!, { status: apiStatus }, signal),
  });

  const filtered = useMemo(
    () => filterIncomingOrdersBySearch(query.data ?? [], debouncedSearch),
    [query.data, debouncedSearch],
  );

  const paged = useMemo(() => {
    const start = (page - 1) * pageSize;
    return filtered.slice(start, start + pageSize);
  }, [filtered, page, pageSize]);

  const statusFilterLabel =
    FILTERS.find((item) => item.value === filter)?.labelKey ?? "incomingOrders.filterAll";

  const resolveRowStatus = (order: ConnectedPurchaseOrder) =>
    statusLabel(t, order.status, order.displayStatus);

  function runOutput(kind: "csv" | "xlsx" | "pdf" | "print") {
    try {
      if (kind === "print") {
        printIncomingOrderListDocument();
        return;
      }
      const model = buildIncomingOrderListExportModel(
        filtered,
        t(statusFilterLabel),
        resolveRowStatus,
        t("incomingOrders.buyer"),
      );
      if (kind === "csv") downloadIncomingOrderListCsv(model);
      if (kind === "xlsx") downloadIncomingOrderListXlsx(model);
      if (kind === "pdf") downloadIncomingOrderListPdf(model);
    } catch {
      showToast({ tone: "danger", title: t("error.title"), detail: t("incomingOrders.loadFailed") });
    }
  }

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  const printModel = buildIncomingOrderListExportModel(
    filtered,
    t(statusFilterLabel),
    resolveRowStatus,
    t("incomingOrders.buyer"),
  );

  return (
    <div
      className="incoming-orders-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="incoming-orders-list-page"
    >
      <div className="incoming-orders-print-root incoming-order-print-root" aria-hidden>
        <h1>{t("incomingOrders.title")}</h1>
        <p>{t(statusFilterLabel)}</p>
        <table>
          <thead>
            <tr>
              <th>{t("purchasing.poNumber")}</th>
              <th>{t("incomingOrders.buyer")}</th>
              <th>{t("incomingOrders.deliverTo")}</th>
              <th>{t("incomingOrders.orderDate")}</th>
              <th>{t("purchasing.lines")}</th>
              <th>{t("incomingOrders.total")}</th>
              <th>{t("purchasing.fieldStatus")}</th>
            </tr>
          </thead>
          <tbody>
            {printModel.rows.map((row) => (
              <tr key={`${row.poNumber}-${row.orderDate}-${row.status}-${row.buyer}`}>
                <td>{row.poNumber}</td>
                <td>{row.buyer}</td>
                <td>{row.branch}</td>
                <td>{row.orderDate}</td>
                <td>
                  {row.products} / {row.units}
                </td>
                <td>{row.total}</td>
                <td>{row.status}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <PageHeader
        title={t("incomingOrders.title")}
        description={t("incomingOrders.lede")}
        backTo={pageBackNav.purchasing.to}
        backLabel={t(pageBackNav.purchasing.labelKey)}
        backTestId="page-header-back-incoming-orders"
      />

      {!online ? (
        <Notice tone="warning" testId="incoming-orders-offline">
          {t("purchasing.offline")}
        </Notice>
      ) : null}

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

      {!query.isLoading && !query.isError && filtered.length === 0 ? (
        <EmptyState
          align="center"
          variant={debouncedSearch ? "filtered" : "default"}
          icon={<ClipboardList className="size-5" strokeWidth={1.75} />}
          title={debouncedSearch ? t("incomingOrders.noMatch") : t("incomingOrders.empty")}
          detail={debouncedSearch ? t("incomingOrders.noMatchHelp") : t("incomingOrders.emptyHelp")}
        />
      ) : null}

      {!query.isLoading && !query.isError && filtered.length > 0 ? (
        <ExitsTableContainer data-testid="incoming-orders-table">
          <ExitsTableToolbar
            search={
              <SearchField
                label={t("incomingOrders.search")}
                value={searchInput}
                placeholder={t("incomingOrders.search")}
                onChange={(e) => setSearchInput(e.target.value)}
                onClear={() => setSearchInput("")}
                data-testid="incoming-orders-search"
              />
            }
            output={
              <ExitsTableOutputActions
                csvLabel={t("exitsTable.exportCsv")}
                xlsxLabel={t("exitsTable.exportExcel")}
                pdfLabel={t("exitsTable.exportPdf")}
                printLabel={t("exitsTable.print")}
                menuLabel={t("exitsTable.exportPrintMenu")}
                onCsv={() => runOutput("csv")}
                onXlsx={() => runOutput("xlsx")}
                onPdf={() => runOutput("pdf")}
                onPrint={() => runOutput("print")}
              />
            }
          />

          <ExitsTable>
            <ExitsTableHeader>
              <ExitsTableRow>
                <ExitsTableHead cellAlign="text">{t("purchasing.poNumber")}</ExitsTableHead>
                <ExitsTableHead cellAlign="text">{t("incomingOrders.buyer")}</ExitsTableHead>
                <ExitsTableHead cellAlign="text">{t("incomingOrders.deliverTo")}</ExitsTableHead>
                <ExitsTableHead cellAlign="text">{t("incomingOrders.orderDate")}</ExitsTableHead>
                <ExitsTableHead cellAlign="text">{t("purchasing.lines")}</ExitsTableHead>
                <ExitsTableHead cellAlign="numeric">{t("incomingOrders.total")}</ExitsTableHead>
                <ExitsTableHead cellAlign="text">{t("purchasing.fieldStatus")}</ExitsTableHead>
              </ExitsTableRow>
            </ExitsTableHeader>
            <ExitsTableBody>
              {paged.map((order) => {
                const products = countIncomingLines(order);
                const units = countIncomingUnits(order);
                const buyer = order.buyerDisplayName?.trim() || t("incomingOrders.buyer");
                const branch = order.supplierBranchName?.trim() || "—";
                const label = resolveRowStatus(order);
                return (
                  <ExitsTableRow
                    key={order.connectedPurchaseOrderId}
                    interactive
                    data-testid={`incoming-order-row-${order.connectedPurchaseOrderId}`}
                    onClick={() =>
                      navigate(`/purchasing/incoming-orders/${order.connectedPurchaseOrderId}`)
                    }
                  >
                    <ExitsTableCell cellAlign="text" className="font-medium">
                      {order.buyerPoNumber ?? t("incomingOrders.unnamedPo")}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="text">{buyer}</ExitsTableCell>
                    <ExitsTableCell cellAlign="text">{branch}</ExitsTableCell>
                    <ExitsTableCell cellAlign="text">{order.orderDate}</ExitsTableCell>
                    <ExitsTableCell cellAlign="text" className="tabular-nums">
                      {t("incomingOrders.summary")
                        .replace("{products}", String(products))
                        .replace("{units}", String(units))}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="numeric" className="font-semibold tabular-nums">
                      <MoneyDisplay amount={order.totalAmount} />
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="text">
                      <StatusChip tone={incomingOrderStatusTone(order.status)}>{label}</StatusChip>
                    </ExitsTableCell>
                  </ExitsTableRow>
                );
              })}
            </ExitsTableBody>
          </ExitsTable>

          <ExitsTableMobile data-testid="incoming-orders-mobile">
            {paged.map((order) => {
              const products = countIncomingLines(order);
              const units = countIncomingUnits(order);
              const buyer = order.buyerDisplayName?.trim() || t("incomingOrders.buyer");
              const branch = order.supplierBranchName?.trim();
              const label = resolveRowStatus(order);
              return (
                <ExitsTableMobileRow
                  key={order.connectedPurchaseOrderId}
                  data-testid={`incoming-order-row-mobile-${order.connectedPurchaseOrderId}`}
                  onClick={() =>
                    navigate(`/purchasing/incoming-orders/${order.connectedPurchaseOrderId}`)
                  }
                >
                  <div className="exits-table-mobile__title-row">
                    <p className="exits-table-mobile__title">
                      {order.buyerPoNumber ?? t("incomingOrders.unnamedPo")}
                    </p>
                    <StatusChip tone={incomingOrderStatusTone(order.status)}>{label}</StatusChip>
                  </div>
                  <p className="exits-table-mobile__meta">
                    {buyer}
                    {branch ? ` · ${branch}` : ""}
                  </p>
                  <p className="exits-table-mobile__math">
                    {t("incomingOrders.summary")
                      .replace("{products}", String(products))
                      .replace("{units}", String(units))}
                    {" · "}
                    {order.orderDate}
                    {" · "}
                    <MoneyDisplay amount={order.totalAmount} />
                  </p>
                </ExitsTableMobileRow>
              );
            })}
          </ExitsTableMobile>

          <ExitsTablePagination
            page={page}
            pageSize={pageSize}
            total={filtered.length}
            pageSizeOptions={PAGE_SIZE_OPTIONS}
            onPageChange={setPage}
            onPageSizeChange={(size) => {
              setPageSize(size);
              setPage(1);
            }}
            rowsPerPageLabel={t("exitsTable.rowsPerPage")}
            previousLabel={t("exitsTable.previous")}
            nextLabel={t("exitsTable.next")}
            rangeLabel={t("exitsTable.range")}
          />
        </ExitsTableContainer>
      ) : null}
    </div>
  );
}
