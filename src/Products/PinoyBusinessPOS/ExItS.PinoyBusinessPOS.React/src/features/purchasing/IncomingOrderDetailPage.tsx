import { ClipboardList } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { canManagePurchasing, canViewPurchasing } from "@/access/pos-capabilities";
import { describePosApiError } from "@/access/pos-commercial-errors";
import {
  acceptIncomingOrder,
  declineIncomingOrder,
  fulfillIncomingOrder,
  getIncomingOrder,
  prepareIncomingOrder,
  type ConnectedPurchaseOrderLine,
} from "@/api/pos/pos-connected-suppliers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import {
  cycleExitsTableSort,
  ExitsTable,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableFooter,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableMobile,
  ExitsTableMobileRow,
  ExitsTableOutputActions,
  ExitsTablePagination,
  ExitsTableRow,
  ExitsTableToolbar,
  type ExitsTableSortDirection,
} from "@/components/exits/ExitsTable";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { useToast } from "@/components/exits/ToastProvider";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { incomingOrderStatusTone } from "@/features/purchasing/incoming-orders-helpers";
import {
  buildIncomingOrderExportModel,
  downloadIncomingOrderCsv,
  downloadIncomingOrderPdf,
  downloadIncomingOrderXlsx,
  printIncomingOrderDocument,
} from "@/features/purchasing/incoming-order-table-output";
import { formatUnitOfMeasureLabel } from "@/features/purchasing/purchase-order-create-connected";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { formatPeso } from "@/lib/format-money";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type LineSkuFilter = "all" | "hasSku" | "noSku";
type LineSortKey = "product" | "sku" | "quantity" | "unitCost" | "lineTotal";

const PAGE_SIZE_OPTIONS = [10, 25, 50, 100] as const;

function lineQtyLabel(line: ConnectedPurchaseOrderLine): string {
  const uom = line.unitOfMeasureCode ? formatUnitOfMeasureLabel(line.unitOfMeasureCode) : "";
  return uom ? `${line.qty} ${uom}` : String(line.qty);
}

const DECLINE_REASONS = [
  "OutOfStock",
  "CannotFulfillQuantity",
  "PriceOrOrderIssue",
  "UnableToFulfill",
  "Other",
] as const;

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

function declineReasonLabel(t: (key: MessageKey) => string, reason: string): string {
  switch (reason) {
    case "OutOfStock":
      return t("incomingOrders.declineReason.outOfStock");
    case "CannotFulfillQuantity":
      return t("incomingOrders.declineReason.cannotFulfillQuantity");
    case "PriceOrOrderIssue":
      return t("incomingOrders.declineReason.priceOrOrderIssue");
    case "UnableToFulfill":
      return t("incomingOrders.declineReason.unableToFulfill");
    case "Other":
      return t("incomingOrders.declineReason.other");
    default:
      return reason;
  }
}

export function IncomingOrderDetailPage() {
  const { t } = useI18n();
  const { showToast } = useToast();
  const online = useBrowserOnline();
  const queryClient = useQueryClient();
  const { connectedPurchaseOrderId } = useParams<{ connectedPurchaseOrderId: string }>();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [showDecline, setShowDecline] = useState(false);
  const [declineReason, setDeclineReason] = useState("");
  const [declineNote, setDeclineNote] = useState("");
  const [actionError, setActionError] = useState<string | null>(null);
  const [searchInput, setSearchInput] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [skuFilter, setSkuFilter] = useState<LineSkuFilter>("all");
  const [sortKey, setSortKey] = useState<LineSortKey | null>(null);
  const [sortDirection, setSortDirection] = useState<ExitsTableSortDirection>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedSearch(searchInput.trim()), 200);
    return () => window.clearTimeout(handle);
  }, [searchInput]);

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, skuFilter, sortKey, sortDirection, pageSize]);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const allowView = canViewPurchasing(sessionGrant);
  const allowManage = canManagePurchasing(sessionGrant);

  const query = useQuery({
    queryKey: ["connected-suppliers", "incoming-order", connectedPurchaseOrderId],
    enabled: Boolean(workspace) && online && allowView && Boolean(connectedPurchaseOrderId),
    queryFn: ({ signal }) => getIncomingOrder(workspace!, connectedPurchaseOrderId!, signal),
  });

  async function refresh() {
    await queryClient.invalidateQueries({ queryKey: ["connected-suppliers", "incoming-orders"] });
    await queryClient.invalidateQueries({
      queryKey: ["connected-suppliers", "incoming-order", connectedPurchaseOrderId],
    });
    await queryClient.invalidateQueries({ queryKey: ["purchasing-hub"] });
  }

  const acceptMutation = useMutation({
    mutationFn: () => acceptIncomingOrder(workspace!, connectedPurchaseOrderId!),
    onSuccess: async () => {
      setActionError(null);
      setShowDecline(false);
      await refresh();
    },
    onError: (err) => {
      setActionError(describePosApiError(err, t, "incomingOrders.actionFailed"));
    },
  });

  const declineMutation = useMutation({
    mutationFn: () =>
      declineIncomingOrder(workspace!, connectedPurchaseOrderId!, {
        declineReason: declineReason || null,
        declineNote: declineNote.trim() || null,
      }),
    onSuccess: async () => {
      setActionError(null);
      setShowDecline(false);
      await refresh();
    },
    onError: (err) => {
      setActionError(describePosApiError(err, t, "incomingOrders.actionFailed"));
    },
  });

  const prepareMutation = useMutation({
    mutationFn: () => prepareIncomingOrder(workspace!, connectedPurchaseOrderId!),
    onSuccess: async () => {
      setActionError(null);
      await refresh();
    },
    onError: (err) => {
      setActionError(describePosApiError(err, t, "incomingOrders.actionFailed"));
    },
  });

  const fulfillMutation = useMutation({
    mutationFn: () => fulfillIncomingOrder(workspace!, connectedPurchaseOrderId!),
    onSuccess: async () => {
      setActionError(null);
      await refresh();
    },
    onError: (err) => {
      setActionError(describePosApiError(err, t, "incomingOrders.actionFailed"));
    },
  });

  const busy =
    acceptMutation.isPending ||
    declineMutation.isPending ||
    prepareMutation.isPending ||
    fulfillMutation.isPending;

  const orderLines = query.data?.lines ?? [];

  const filteredSortedLines = useMemo(() => {
    const queryText = debouncedSearch.toLowerCase();
    let rows = orderLines.filter((line) => {
      const sku = line.skuSnapshot?.trim() ?? "";
      if (skuFilter === "hasSku" && !sku) {
        return false;
      }
      if (skuFilter === "noSku" && sku) {
        return false;
      }
      if (!queryText) {
        return true;
      }
      return (
        line.nameSnapshot.toLowerCase().includes(queryText) ||
        sku.toLowerCase().includes(queryText)
      );
    });

    if (sortKey && sortDirection) {
      const dir = sortDirection === "asc" ? 1 : -1;
      rows = [...rows].sort((a, b) => {
        switch (sortKey) {
          case "product":
            return a.nameSnapshot.localeCompare(b.nameSnapshot) * dir;
          case "sku":
            return (a.skuSnapshot ?? "").localeCompare(b.skuSnapshot ?? "") * dir;
          case "quantity":
            return (a.qty - b.qty) * dir;
          case "unitCost":
            return (a.unitPriceSnapshot - b.unitPriceSnapshot) * dir;
          case "lineTotal":
            return (a.lineTotal - b.lineTotal) * dir;
          default:
            return 0;
        }
      });
    }

    return rows;
  }, [orderLines, debouncedSearch, skuFilter, sortKey, sortDirection]);

  const pageCount = Math.max(1, Math.ceil(filteredSortedLines.length / pageSize) || 1);
  const safePage = Math.min(page, pageCount);
  const pagedLines = useMemo(() => {
    const start = (safePage - 1) * pageSize;
    return filteredSortedLines.slice(start, start + pageSize);
  }, [filteredSortedLines, safePage, pageSize]);

  function toggleSort(key: LineSortKey) {
    const next = cycleExitsTableSort(sortKey, sortDirection, key);
    setSortKey(next.key as LineSortKey | null);
    setSortDirection(next.direction);
  }

  function buildExportModel() {
    if (!query.data) {
      throw new Error("Order is not loaded");
    }
    return buildIncomingOrderExportModel(query.data, filteredSortedLines, new Set());
  }

  async function runOutput(action: "csv" | "xlsx" | "pdf" | "print") {
    try {
      const model = buildExportModel();
      if (action === "csv") {
        downloadIncomingOrderCsv(model);
        return;
      }
      if (action === "xlsx") {
        downloadIncomingOrderXlsx(model);
        return;
      }
      if (action === "pdf") {
        downloadIncomingOrderPdf(model);
        return;
      }
      printIncomingOrderDocument();
    } catch {
      showToast({
        title: t("exitsTable.outputFailed"),
        tone: "error",
      });
    }
  }

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (query.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (query.isError || !query.data) {
    return (
      <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="incoming-order-detail-page">
        <PageHeader
          title={t("incomingOrders.detailTitle")}
          backTo="/purchasing/incoming-orders"
          backLabel={t("incomingOrders.backList")}
        />
        <ErrorState
          title={t("error.title")}
          detail={
            query.error instanceof PosApiError
              ? (query.error.problem.detail ?? query.error.message)
              : t("incomingOrders.notFound")
          }
        />
      </div>
    );
  }

  const order = query.data;
  const isNew = order.status === "New";
  const isAccepted = order.status === "Accepted";
  const isPreparing = order.status === "Preparing";
  const canAct = allowManage && online && !busy;
  const printModel = buildIncomingOrderExportModel(order, filteredSortedLines, new Set());

  return (
    <div
      className="incoming-order-detail-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="incoming-order-detail-page"
    >
      <div className="incoming-order-print-root" data-testid="incoming-order-print-root" aria-hidden>
        <h1>{printModel.poNumber}</h1>
        <p>Buyer: {printModel.buyer}</p>
        {printModel.branch ? <p>Fulfill from: {printModel.branch}</p> : null}
        <p>Order date: {printModel.orderDate}</p>
        {printModel.paymentTerm ? <p>Payment term: {printModel.paymentTerm}</p> : null}
        <table>
          <thead>
            <tr>
              <th>Product</th>
              <th>SKU</th>
              <th>Quantity</th>
              <th>Unit cost</th>
              <th>Line total</th>
            </tr>
          </thead>
          <tbody>
            {printModel.lines.map((line) => (
              <tr key={line.productId}>
                <td>{line.product}</td>
                <td>{line.sku || "—"}</td>
                <td>
                  {line.unit ? `${line.quantity} ${line.unit}` : line.quantity}
                </td>
                <td>{formatPeso(line.unitCost)}</td>
                <td>{formatPeso(line.lineTotal)}</td>
              </tr>
            ))}
          </tbody>
        </table>
        <p>
          <strong>{t("incomingOrders.orderTotal")}</strong> {formatPeso(printModel.orderTotal)}
        </p>
        {printModel.scope === "selected" ? (
          <p>
            {t("exitsTable.selectedLinesTotal")} {formatPeso(printModel.selectedLinesTotal)}
          </p>
        ) : null}
      </div>
      <PageHeader
        title={order.buyerPoNumber ?? t("incomingOrders.unnamedPo")}
        description={t("incomingOrders.detailLede")}
        backTo="/purchasing/incoming-orders"
        backLabel={t("incomingOrders.backList")}
        backTestId="page-header-back-incoming-order-detail"
      />

      <Card className="grid gap-2 p-3" data-testid="incoming-order-summary">
        <div className="flex flex-wrap items-start justify-between gap-2">
          <div className="min-w-0">
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("incomingOrders.buyer")}</p>
            <p className="m-0 font-semibold">
              {order.buyerDisplayName?.trim() || t("incomingOrders.buyerUnknown")}
            </p>
          </div>
          <StatusChip tone={incomingOrderStatusTone(order.status)}>
            {statusLabel(t, order.status, order.displayStatus)}
          </StatusChip>
        </div>
        {order.supplierBranchName ? (
          <p className="m-0 text-[length:var(--exits-text-sm)]">
            <span className="text-muted">{t("incomingOrders.deliverTo")}: </span>
            {order.supplierBranchName}
          </p>
        ) : null}
        <p className="m-0 text-[length:var(--exits-text-sm)]">
          <span className="text-muted">{t("incomingOrders.orderDate")}: </span>
          {order.orderDate}
        </p>
        <p className="m-0 text-[length:var(--exits-text-sm)]">
          <span className="text-muted">{t("purchasing.paymentTerm")}: </span>
          {order.paymentTermLabel || order.paymentTerm}
        </p>
        {order.buyerReceivingStatus ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="incoming-order-receiving">
            {order.buyerReceivingStatus}
          </p>
        ) : null}
      </Card>

      {actionError ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-danger" role="alert" data-testid="incoming-order-action-error">
          {actionError}
        </p>
      ) : null}

      {order.lines.length === 0 ? (
        <EmptyState
              variant="setup"
              align="center"
              icon={<ClipboardList className="size-5" strokeWidth={1.75} />} title={t("purchasing.linesEmpty")} detail={t("purchasing.linesRequired")} />
      ) : (
        <ExitsTableContainer data-testid="incoming-order-lines">
          <ExitsTableToolbar
            search={
              <SearchField
                label={t("exitsTable.searchProducts")}
                value={searchInput}
                placeholder={t("exitsTable.searchProducts")}
                onChange={(e) => setSearchInput(e.target.value)}
                onClear={() => setSearchInput("")}
                data-testid="incoming-order-lines-search"
              />
            }
            filter={
              <label className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                <span className="sr-only">{t("exitsTable.filter")}</span>
                <select
                  className="exits-select"
                  value={skuFilter}
                  onChange={(e) => setSkuFilter(e.target.value as LineSkuFilter)}
                  data-testid="incoming-order-lines-filter"
                  aria-label={t("exitsTable.filter")}
                >
                  <option value="all">{t("exitsTable.filterAll")}</option>
                  <option value="hasSku">{t("exitsTable.filterHasSku")}</option>
                  <option value="noSku">{t("exitsTable.filterNoSku")}</option>
                </select>
              </label>
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
                <ExitsTableHead
                  cellAlign="text"
                  sortable
                  sortDirection={sortKey === "product" ? sortDirection : null}
                  onSort={() => toggleSort("product")}
                  data-testid="incoming-order-sort-product"
                >
                  {t("purchasing.colProduct")}
                </ExitsTableHead>
                <ExitsTableHead
                  cellAlign="text"
                  sortable
                  sortDirection={sortKey === "sku" ? sortDirection : null}
                  onSort={() => toggleSort("sku")}
                  data-testid="incoming-order-sort-sku"
                >
                  {t("catalog.sku")}
                </ExitsTableHead>
                <ExitsTableHead
                  cellAlign="numeric"
                  sortable
                  sortDirection={sortKey === "quantity" ? sortDirection : null}
                  onSort={() => toggleSort("quantity")}
                  data-testid="incoming-order-sort-quantity"
                >
                  {t("purchasing.qty")}
                </ExitsTableHead>
                <ExitsTableHead
                  cellAlign="money"
                  sortable
                  sortDirection={sortKey === "unitCost" ? sortDirection : null}
                  onSort={() => toggleSort("unitCost")}
                  data-testid="incoming-order-sort-unit-cost"
                >
                  {t("purchasing.unitCost")}
                </ExitsTableHead>
                <ExitsTableHead
                  cellAlign="money"
                  sortable
                  sortDirection={sortKey === "lineTotal" ? sortDirection : null}
                  onSort={() => toggleSort("lineTotal")}
                  data-testid="incoming-order-sort-line-total"
                >
                  {t("purchasing.lineTotal")}
                </ExitsTableHead>
              </ExitsTableRow>
            </ExitsTableHeader>
            <ExitsTableBody>
              {pagedLines.map((line) => (
                <ExitsTableRow
                  key={line.productId}
                  interactive
                  data-testid={`incoming-order-line-${line.productId}`}
                >
                  <ExitsTableCell cellAlign="text" className="font-medium">
                    {line.nameSnapshot}
                  </ExitsTableCell>
                  <ExitsTableCell cellAlign="text" className="text-muted">
                    {line.skuSnapshot?.trim() || "—"}
                  </ExitsTableCell>
                  <ExitsTableCell cellAlign="numeric">{lineQtyLabel(line)}</ExitsTableCell>
                  <ExitsTableCell cellAlign="money">
                    <MoneyDisplay amount={line.unitPriceSnapshot} />
                  </ExitsTableCell>
                  <ExitsTableCell cellAlign="money" emphasis="semibold">
                    <MoneyDisplay
                      amount={line.lineTotal}
                      testId={`incoming-order-line-total-${line.productId}`}
                    />
                  </ExitsTableCell>
                </ExitsTableRow>
              ))}
            </ExitsTableBody>
            <ExitsTableFooter data-testid="incoming-order-total">
              <ExitsTableRow>
                <ExitsTableCell cellAlign="actions" colSpan={4} emphasis="bold">
                  {t("incomingOrders.orderTotal")}
                </ExitsTableCell>
                <ExitsTableCell cellAlign="money" emphasis="bold">
                  <MoneyDisplay amount={order.totalAmount} testId="incoming-order-total-amount" />
                </ExitsTableCell>
              </ExitsTableRow>
            </ExitsTableFooter>
          </ExitsTable>

          <ExitsTableMobile data-testid="incoming-order-lines-mobile">
            {pagedLines.map((line) => (
              <ExitsTableMobileRow
                key={line.productId}
                data-testid={`incoming-order-line-mobile-${line.productId}`}
              >
                <div className="exits-table-mobile__title-row">
                  <p className="exits-table-mobile__title">{line.nameSnapshot}</p>
                  <p className="exits-table-mobile__total">{formatPeso(line.lineTotal)}</p>
                </div>
                {line.skuSnapshot?.trim() ? (
                  <p className="exits-table-mobile__meta">{line.skuSnapshot.trim()}</p>
                ) : null}
                <p className="exits-table-mobile__math">
                  {lineQtyLabel(line)} × {formatPeso(line.unitPriceSnapshot)}
                </p>
              </ExitsTableMobileRow>
            ))}
            <li className="exits-table-mobile__footer" data-testid="incoming-order-total-mobile">
              <span>{t("incomingOrders.orderTotal")}</span>
              <MoneyDisplay amount={order.totalAmount} />
            </li>
          </ExitsTableMobile>

          <ExitsTablePagination
            page={safePage}
            pageSize={pageSize}
            total={filteredSortedLines.length}
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
      )}

      {isNew && showDecline ? (
        <Card className="grid gap-3 p-3" data-testid="incoming-order-decline-form">
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("incomingOrders.declineReason")}
            <select
              className="exits-select"
              value={declineReason}
              onChange={(e) => setDeclineReason(e.target.value)}
              data-testid="incoming-order-decline-reason"
            >
              <option value="">{t("incomingOrders.declineReasonOptional")}</option>
              {DECLINE_REASONS.map((reason) => (
                <option key={reason} value={reason}>
                  {declineReasonLabel(t, reason)}
                </option>
              ))}
            </select>
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("incomingOrders.declineNote")}
            <textarea
              className="min-h-20 rounded-md border border-border bg-background px-3 py-2"
              value={declineNote}
              onChange={(e) => setDeclineNote(e.target.value)}
              data-testid="incoming-order-decline-note"
            />
          </label>
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="ghost"
              disabled={busy}
              onClick={() => setShowDecline(false)}
            >
              {t("purchasing.cancel")}
            </Button>
            <Button
              type="button"
              variant="destructive"
              disabled={!canAct}
              data-testid="incoming-order-decline-confirm"
              onClick={() => declineMutation.mutate()}
            >
              {t("incomingOrders.decline")}
            </Button>
          </div>
        </Card>
      ) : null}

      {isNew && !showDecline ? (
        <div className="flex flex-wrap gap-2" data-testid="incoming-order-pending-actions">
          <Button
            type="button"
            variant="destructive"
            className="flex-1"
            disabled={!canAct}
            data-testid="incoming-order-decline"
            onClick={() => setShowDecline(true)}
          >
            {t("incomingOrders.decline")}
          </Button>
          <Button
            type="button"
            className="flex-1"
            disabled={!canAct}
            data-testid="incoming-order-accept"
            onClick={() => acceptMutation.mutate()}
          >
            {t("incomingOrders.accept")}
          </Button>
        </div>
      ) : null}

      {isAccepted ? (
        <Button
          type="button"
          className="w-full"
          disabled={!canAct}
          data-testid="incoming-order-prepare"
          onClick={() => prepareMutation.mutate()}
        >
          {t("incomingOrders.startPreparing")}
        </Button>
      ) : null}

      {isPreparing ? (
        <Button
          type="button"
          className="w-full"
          disabled={!canAct}
          data-testid="incoming-order-fulfill"
          onClick={() => fulfillMutation.mutate()}
        >
          {t("incomingOrders.markReady")}
        </Button>
      ) : null}

      {!allowManage ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("incomingOrders.viewOnly")}</p>
      ) : null}

      <Button asChild variant="ghost">
        <Link to="/purchasing/incoming-orders">{t("incomingOrders.backList")}</Link>
      </Button>
    </div>
  );
}
