import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ClipboardList, Plus } from "lucide-react";
import { canManagePurchasing } from "@/access/pos-capabilities";
import { listPurchaseOrders, type PosPurchaseOrderDto } from "@/api/pos/pos-purchase-orders-client";
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
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip, type StatusChipTone } from "@/components/exits/StatusChip";
import { useToast } from "@/components/exits/ToastProvider";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  buildPurchaseOrderListExportModel,
  downloadPurchaseOrderListCsv,
  downloadPurchaseOrderListPdf,
  downloadPurchaseOrderListXlsx,
  printPurchaseOrderListDocument,
} from "@/features/purchasing/purchase-orders-list-output";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PAGE_SIZE_OPTIONS = [10, 25, 50, 100] as const;

type StatusFilter = "" | "Draft" | "Ordered" | "PartiallyReceived" | "Received" | "Cancelled";

const STATUS_FILTERS: Array<{
  value: StatusFilter;
  key: string;
  labelKey:
    | "purchasing.statusAll"
    | "purchasing.statusDraft"
    | "purchasing.statusOrdered"
    | "purchasing.statusPartial"
    | "purchasing.statusReceived"
    | "purchasing.statusCancelled";
}> = [
  { value: "", key: "all", labelKey: "purchasing.statusAll" },
  { value: "Draft", key: "Draft", labelKey: "purchasing.statusDraft" },
  { value: "Ordered", key: "Ordered", labelKey: "purchasing.statusOrdered" },
  { value: "PartiallyReceived", key: "PartiallyReceived", labelKey: "purchasing.statusPartial" },
  { value: "Received", key: "Received", labelKey: "purchasing.statusReceived" },
  { value: "Cancelled", key: "Cancelled", labelKey: "purchasing.statusCancelled" },
];

function resolveStatusLabel(po: PosPurchaseOrderDto): string {
  return po.displayStatus || po.status;
}

/** Semantic status tones — Cancelled=danger, Waiting*=primary. */
export function purchaseOrderListStatusTone(po: PosPurchaseOrderDto): StatusChipTone {
  const label = resolveStatusLabel(po);
  const normalized = label.replace(/\s+/g, "").toLowerCase();

  if (po.status === "Cancelled" || normalized === "cancelled") {
    return "danger";
  }
  if (normalized === "waitingforsupplier" || normalized.startsWith("waiting")) {
    return "primary";
  }

  switch (po.status) {
    case "Ordered":
    case "Received":
      return "success";
    case "PartiallyReceived":
      return "warning";
    case "Draft":
      return "info";
    default:
      return "info";
  }
}

function matchesSearch(po: PosPurchaseOrderDto, query: string): boolean {
  if (!query) {
    return true;
  }
  const tokens = [
    po.poNumber ?? "",
    po.supplierName ?? "",
    po.supplierBranchName ?? "",
    po.status,
    po.displayStatus ?? "",
    po.paymentTermLabel ?? "",
    po.paymentTerm ?? "",
  ];
  return tokens.some((token) => token.toLowerCase().includes(query));
}

export function PurchaseOrdersListPage() {
  const { t } = useI18n();
  const { showToast } = useToast();
  const navigate = useNavigate();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [status, setStatus] = useState<StatusFilter>("");
  const [searchInput, setSearchInput] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");

  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedSearch(searchInput.trim()), 200);
    return () => window.clearTimeout(handle);
  }, [searchInput]);

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, status, pageSize]);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const allowManage = canManagePurchasing(sessionGrant);

  const query = useQuery({
    queryKey: [
      "purchase-orders",
      workspace?.organizationId,
      workspace?.branchId,
      status,
      page,
      pageSize,
    ],
    enabled: Boolean(workspace) && online,
    queryFn: ({ signal }) =>
      listPurchaseOrders(
        workspace!,
        { status: status || undefined, page, pageSize },
        signal,
      ),
  });

  const items: PosPurchaseOrderDto[] = query.data?.items ?? [];
  const filteredItems = useMemo(() => {
    const q = debouncedSearch.toLowerCase();
    if (!q) {
      return items;
    }
    return items.filter((po) => matchesSearch(po, q));
  }, [items, debouncedSearch]);

  const totalCount = debouncedSearch
    ? filteredItems.length
    : (query.data?.totalCount ?? 0);
  const statusFilterLabel =
    STATUS_FILTERS.find((filter) => filter.value === status)?.labelKey ?? "purchasing.statusAll";

  function buildExportModel() {
    return buildPurchaseOrderListExportModel(filteredItems, t(statusFilterLabel));
  }

  async function runOutput(action: "csv" | "xlsx" | "pdf" | "print") {
    try {
      const model = buildExportModel();
      if (action === "csv") {
        downloadPurchaseOrderListCsv(model);
        return;
      }
      if (action === "xlsx") {
        downloadPurchaseOrderListXlsx(model);
        return;
      }
      if (action === "pdf") {
        downloadPurchaseOrderListPdf(model);
        return;
      }
      printPurchaseOrderListDocument();
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

  const printModel = buildPurchaseOrderListExportModel(filteredItems, t(statusFilterLabel));

  return (
    <div
      className="purchasing-orders-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="purchase-orders-list-page"
    >
      <div className="purchase-orders-print-root incoming-order-print-root" aria-hidden>
        <h1>{t("purchasing.orders")}</h1>
        <p>{t(statusFilterLabel)}</p>
        <table>
          <thead>
            <tr>
              <th>{t("purchasing.poNumber")}</th>
              <th>{t("purchasing.supplier")}</th>
              <th>{t("purchasing.orderDate")}</th>
              <th>{t("purchasing.lines")}</th>
              <th>{t("purchasing.fieldStatus")}</th>
              <th>{t("purchasing.paymentTerm")}</th>
            </tr>
          </thead>
          <tbody>
            {printModel.rows.map((row) => (
              <tr key={`${row.poNumber}-${row.orderDate}-${row.status}`}>
                <td>{row.poNumber}</td>
                <td>{row.supplier}</td>
                <td>{row.orderDate}</td>
                <td>{row.lines}</td>
                <td>{row.status}</td>
                <td>{row.payment}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <PageHeader
        title={t("purchasing.orders")}
        description={t("purchasing.ordersLede")}
        backTo={pageBackNav.purchasing.to}
        backLabel={t(pageBackNav.purchasing.labelKey)}
        backTestId="page-header-back-purchasing"
      />

      <Notice tone="info">{t("purchasing.ordersNoStock")}</Notice>

      {!online ? (
        <Notice tone="warning" testId="purchasing-offline">
          {t("purchasing.offline")}
        </Notice>
      ) : null}

      {allowManage ? (
        <ExitsChipBar
          variant="actions"
          ariaLabel={t("purchasing.orders")}
          testId="po-toolbar"
          className="exits-animate-toolbar"
          items={[
            {
              key: "new",
              label: t("purchasing.newOrder"),
              icon: <Plus />,
              href: online ? "/purchasing/new" : undefined,
              disabled: !online,
              testId: "po-new",
              emphasis: "primary",
            },
          ]}
        />
      ) : null}

      <ExitsChipBar
        variant="filter"
        ariaLabel={t("purchasing.statusFilter")}
        testId="po-status-filter"
        items={STATUS_FILTERS.map((filter) => ({
          key: filter.key,
          label: t(filter.labelKey),
          state: status === filter.value ? "active" : "idle",
          testId: `po-status-${filter.key}`,
          onSelect: () => {
            setStatus(filter.value);
            setPage(1);
          },
        }))}
      />

      {query.isLoading ? <LoadingState label={t("purchasing.loading")} /> : null}
      {query.isError ? (
        <ErrorState title={t("purchasing.errorTitle")} detail={t("purchasing.loadFailed")} />
      ) : null}

      {!query.isLoading && !query.isError && filteredItems.length === 0 ? (
        <EmptyState
          align="center"
          icon={<ClipboardList className="size-5" strokeWidth={1.75} />}
          title={debouncedSearch ? t("purchasing.ordersNoMatch") : t("purchasing.ordersEmpty")}
          detail={
            debouncedSearch ? t("purchasing.ordersNoMatchDetail") : t("purchasing.ordersEmptyDetail")
          }
        />
      ) : null}

      {!query.isLoading && !query.isError && filteredItems.length > 0 ? (
        <ExitsTableContainer data-testid="purchase-orders-table">
          <ExitsTableToolbar
            search={
              <SearchField
                label={t("purchasing.searchOrders")}
                value={searchInput}
                placeholder={t("purchasing.searchOrders")}
                onChange={(e) => setSearchInput(e.target.value)}
                onClear={() => setSearchInput("")}
                data-testid="purchase-orders-search"
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
                <ExitsTableHead cellAlign="text">{t("purchasing.supplier")}</ExitsTableHead>
                <ExitsTableHead cellAlign="text">{t("purchasing.orderDate")}</ExitsTableHead>
                <ExitsTableHead cellAlign="numeric">{t("purchasing.lines")}</ExitsTableHead>
                <ExitsTableHead cellAlign="text">{t("purchasing.fieldStatus")}</ExitsTableHead>
                <ExitsTableHead cellAlign="text">{t("purchasing.paymentTerm")}</ExitsTableHead>
              </ExitsTableRow>
            </ExitsTableHeader>
            <ExitsTableBody>
              {filteredItems.map((po) => {
                const label = resolveStatusLabel(po);
                return (
                  <ExitsTableRow
                    key={po.purchaseOrderId}
                    interactive
                    data-testid={`po-row-${po.purchaseOrderId}`}
                    onClick={() => navigate(`/purchasing/${po.purchaseOrderId}`)}
                  >
                    <ExitsTableCell cellAlign="text" className="font-medium">
                      {po.poNumber ?? t("purchasing.unnamedPo")}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="text">
                      {po.supplierName ?? t("purchasing.unknownSupplier")}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="text">{po.orderDate}</ExitsTableCell>
                    <ExitsTableCell cellAlign="numeric" className="tabular-nums">
                      {po.lines.length}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="text">
                      <StatusChip tone={purchaseOrderListStatusTone(po)}>{label}</StatusChip>
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="text">
                      {po.paymentTermLabel || po.paymentTerm || "—"}
                    </ExitsTableCell>
                  </ExitsTableRow>
                );
              })}
            </ExitsTableBody>
          </ExitsTable>

          <ExitsTableMobile data-testid="purchase-orders-mobile">
            {filteredItems.map((po) => {
              const label = resolveStatusLabel(po);
              return (
                <ExitsTableMobileRow
                  key={po.purchaseOrderId}
                  data-testid={`po-row-mobile-${po.purchaseOrderId}`}
                  onClick={() => navigate(`/purchasing/${po.purchaseOrderId}`)}
                >
                  <div className="exits-table-mobile__title-row">
                    <p className="exits-table-mobile__title">
                      {po.poNumber ?? t("purchasing.unnamedPo")}
                    </p>
                    <StatusChip tone={purchaseOrderListStatusTone(po)}>{label}</StatusChip>
                  </div>
                  <p className="exits-table-mobile__meta">
                    {po.supplierName ?? t("purchasing.unknownSupplier")}
                  </p>
                  <p className="exits-table-mobile__math">
                    {po.orderDate} ·{" "}
                    {t("purchasing.linesCount").replace("{count}", String(po.lines.length))}
                  </p>
                </ExitsTableMobileRow>
              );
            })}
          </ExitsTableMobile>

          <ExitsTablePagination
            page={page}
            pageSize={pageSize}
            total={debouncedSearch ? filteredItems.length : totalCount}
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
