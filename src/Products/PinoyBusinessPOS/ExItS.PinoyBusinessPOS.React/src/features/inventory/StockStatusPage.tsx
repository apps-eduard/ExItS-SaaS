import { useEffect, useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Boxes } from "lucide-react";
import { listCatalogCategories } from "@/api/pos/pos-catalog-client";
import {
  getInventoryProduct,
  getInventoryStockStatus,
  type InventoryStockStatusRowDto,
  type InventoryStockStatusState,
} from "@/api/pos/pos-inventory-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { ExitsResponsiveDataView } from "@/components/exits/ExitsResponsiveDataView";
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
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { useToast } from "@/components/exits/ToastProvider";
import { RESPONSIVE_DATA_TABLE_MIN_MD } from "@/components/exits/responsive-data-view";
import { useResponsiveDataLayout } from "@/components/exits/useResponsiveDataLayout";
import {
  exportStockStatusCsv,
  exportStockStatusXlsx,
} from "@/features/inventory/stock-status-export";
import { formatInventoryQty } from "@/features/inventory/inventory-reservation-display";
import { BranchRequiredPanel } from "@/features/workspace/BranchRequiredPanel";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PAGE_SIZE = 50;

const STOCK_STATES: InventoryStockStatusState[] = [
  "All",
  "Available",
  "LowStock",
  "OutOfStock",
  "Reserved",
  "Damaged",
  "InspectionHold",
  "PendingReturn",
  "Expired",
  "SaleBlocked",
];

type BranchScope = "current" | "all";

function stockStateLabelKey(state: InventoryStockStatusState): MessageKey {
  switch (state) {
    case "All":
      return "stockStatus.stateAll";
    case "Available":
      return "stockStatus.stateAvailable";
    case "LowStock":
      return "stockStatus.stateLowStock";
    case "OutOfStock":
      return "stockStatus.stateOutOfStock";
    case "Reserved":
      return "stockStatus.stateReserved";
    case "Damaged":
      return "stockStatus.stateDamaged";
    case "InspectionHold":
      return "stockStatus.stateInspectionHold";
    case "PendingReturn":
      return "stockStatus.statePendingReturn";
    case "Expired":
      return "stockStatus.stateExpired";
    case "SaleBlocked":
      return "stockStatus.stateSaleBlocked";
  }
}

function parseStockState(value: string | null): InventoryStockStatusState {
  if (!value) return "All";
  const match = STOCK_STATES.find((s) => s.toLowerCase() === value.toLowerCase());
  return match ?? "All";
}

function qtyCell(value: number): string {
  return formatInventoryQty(value);
}

export function StockStatusPage() {
  const { t } = useI18n();
  const { showToast } = useToast();
  const { layout } = useResponsiveDataLayout({ tableMinWidthPx: RESPONSIVE_DATA_TABLE_MIN_MD });
  const { boundWorkspace, workspaces } = useWorkspace();
  const [searchParams, setSearchParams] = useSearchParams();

  const productIdFilter = searchParams.get("productId")?.trim() || "";
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [stockState, setStockState] = useState<InventoryStockStatusState>(() =>
    parseStockState(searchParams.get("stockState")),
  );
  const [categoryId, setCategoryId] = useState(searchParams.get("categoryId") ?? "");
  const [branchScope, setBranchScope] = useState<BranchScope>("current");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(PAGE_SIZE);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  useEffect(() => {
    setPage(1);
  }, [
    debounced,
    stockState,
    categoryId,
    branchScope,
    productIdFilter,
    pageSize,
    boundWorkspace?.branchId,
  ]);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const authorizedBranches = useMemo(() => {
    const org = workspaces.find((w) => w.organizationId === boundWorkspace?.organizationId);
    return (org?.branches ?? []).filter((b) => b.isActive);
  }, [workspaces, boundWorkspace?.organizationId]);

  const canSelectAllBranches = authorizedBranches.length > 1;

  const categoriesQuery = useQuery({
    queryKey: ["catalog", "categories", workspace?.organizationId],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) =>
      listCatalogCategories(workspace!, { status: "Active", page: 1, pageSize: 200 }, signal),
  });

  const query = useQuery({
    queryKey: [
      "inventory",
      "stock-status",
      workspace?.organizationId,
      workspace?.branchId,
      branchScope,
      categoryId,
      productIdFilter,
      debounced,
      stockState,
      page,
      pageSize,
    ],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) =>
      getInventoryStockStatus(
        workspace!,
        {
          branchId: branchScope === "current" ? workspace!.branchId : undefined,
          categoryId: categoryId || undefined,
          productId: productIdFilter || undefined,
          search: debounced || undefined,
          stockState,
          page,
          pageSize,
        },
        signal,
      ),
  });

  const rows = query.data?.rows ?? [];
  const totalCount = query.data?.totalCount ?? 0;
  const generatedAtUtc = query.data?.generatedAtUtc ?? new Date().toISOString();

  const productLabelQuery = useQuery({
    queryKey: ["inventory", "product-label", workspace?.organizationId, productIdFilter],
    enabled: Boolean(workspace && productIdFilter),
    queryFn: ({ signal }) => getInventoryProduct(workspace!, productIdFilter, signal),
    staleTime: 60_000,
  });

  const productFilterLabel =
    rows.find((r) => r.productId === productIdFilter)?.productName?.trim()
    || productLabelQuery.data?.name?.trim()
    || null;

  const scopeLabel =
    branchScope === "all"
      ? t("stockStatus.scopeAllBranches")
      : boundWorkspace?.branchName?.trim() || t("stockStatus.scopeCurrentBranch");

  function updateStockState(next: InventoryStockStatusState) {
    setStockState(next);
    const nextParams = new URLSearchParams(searchParams);
    if (next === "All") {
      nextParams.delete("stockState");
    } else {
      nextParams.set("stockState", next);
    }
    setSearchParams(nextParams, { replace: true });
  }

  function clearProductFilter() {
    const nextParams = new URLSearchParams(searchParams);
    nextParams.delete("productId");
    setSearchParams(nextParams, { replace: true });
  }

  async function runOutput(action: "csv" | "xlsx" | "print") {
    try {
      const exportArgs = {
        rows,
        organizationName: boundWorkspace?.organizationDisplayName,
        scopeLabel,
        stockState,
        search: debounced || productIdFilter || null,
        generatedAtUtc,
      };
      if (action === "csv") {
        exportStockStatusCsv(exportArgs);
        return;
      }
      if (action === "xlsx") {
        exportStockStatusXlsx(exportArgs);
        return;
      }
      window.print();
    } catch {
      showToast(t("exitsTable.outputFailed"), "error");
    }
  }

  if (!workspace) {
    return <BranchRequiredPanel title={t("stockStatus.title")} />;
  }

  const toolbar = (
    <ExitsTableToolbar
      search={
        <SearchField
          label={t("stockStatus.search")}
          value={search}
          placeholder={t("stockStatus.searchPlaceholder")}
          onChange={(e) => setSearch(e.target.value)}
          onClear={() => setSearch("")}
          data-testid="stock-status-search"
        />
      }
      filter={
        <div className="flex min-w-0 flex-wrap items-end gap-2">
          <label className="flex min-w-[10rem] flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span className="text-muted">{t("stockStatus.branchFilter")}</span>
            <select
              className="exits-select h-9 rounded-[var(--exits-radius-md)] border border-border bg-background px-2"
              value={branchScope}
              onChange={(e) => setBranchScope(e.target.value as BranchScope)}
              data-testid="stock-status-branch-filter"
            >
              <option value="current">{t("stockStatus.scopeCurrentBranch")}</option>
              {canSelectAllBranches ? (
                <option value="all">{t("stockStatus.scopeAllBranches")}</option>
              ) : null}
            </select>
          </label>
          <label className="flex min-w-[10rem] flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span className="text-muted">{t("stockStatus.categoryFilter")}</span>
            <select
              className="exits-select h-9 rounded-[var(--exits-radius-md)] border border-border bg-background px-2"
              value={categoryId}
              onChange={(e) => setCategoryId(e.target.value)}
              data-testid="stock-status-category-filter"
            >
              <option value="">{t("stockStatus.allCategories")}</option>
              {(categoriesQuery.data?.items ?? []).map((category) => (
                <option key={category.categoryId} value={category.categoryId}>
                  {category.name}
                </option>
              ))}
            </select>
          </label>
        </div>
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
          onPrint={() => runOutput("print")}
          disabled={rows.length === 0}
        />
      }
    />
  );

  const table = (
    <ExitsTableContainer data-testid="stock-status-table-desktop">
      <ExitsTable>
        <ExitsTableHeader>
          <ExitsTableRow>
            <ExitsTableHead cellAlign="text" colSize="flex">
              {t("stockStatus.colProduct")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="text" className="hidden lg:table-cell">
              {t("stockStatus.colCategory")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="text">{t("stockStatus.colBranch")}</ExitsTableHead>
            <ExitsTableHead cellAlign="center" colSize="numeric">
              {t("stockStatus.colOnHand")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="center" colSize="numeric" className="hidden md:table-cell">
              {t("stockStatus.colSellable")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="center" colSize="numeric" className="hidden lg:table-cell">
              {t("stockStatus.colReserved")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="center" colSize="numeric">
              {t("stockStatus.colAvailable")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="center" colSize="numeric" className="hidden xl:table-cell">
              {t("stockStatus.colDamaged")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="center" colSize="numeric" className="hidden xl:table-cell">
              {t("stockStatus.colHold")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="center" colSize="numeric" className="hidden xl:table-cell">
              {t("stockStatus.colPendingReturn")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="center" colSize="numeric" className="hidden xl:table-cell">
              {t("stockStatus.colExpired")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="center" colSize="numeric" className="hidden xl:table-cell">
              {t("stockStatus.colSaleBlocked")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="center" colSize="numeric" className="hidden xl:table-cell">
              {t("stockStatus.colIncoming")}
            </ExitsTableHead>
            <ExitsTableHead cellAlign="center" colSize="numeric" className="hidden xl:table-cell">
              {t("stockStatus.colInTransitOut")}
            </ExitsTableHead>
          </ExitsTableRow>
        </ExitsTableHeader>
        <ExitsTableBody>
          {rows.map((row) => (
            <StockStatusDesktopRow key={`${row.productId}-${row.branchId}`} row={row} />
          ))}
        </ExitsTableBody>
      </ExitsTable>
    </ExitsTableContainer>
  );

  const list = (
    <ExitsTableMobile data-testid="stock-status-table-mobile">
      {rows.map((row) => (
        <ExitsTableMobileRow
          key={`${row.productId}-${row.branchId}`}
          data-testid={`stock-status-mobile-row-${row.productId}-${row.branchId}`}
        >
          <div className="exits-table-mobile__title-row">
            <Link
              to={`/inventory/${row.productId}`}
              className="exits-table-mobile__title font-medium text-foreground"
            >
              {row.productName}
            </Link>
          </div>
          <p className="exits-table-mobile__meta m-0">
            {row.branchName}
            {row.categoryName?.trim() ? ` · ${row.categoryName.trim()}` : ""}
            {row.sku?.trim() ? ` · ${row.sku.trim()}` : ""}
          </p>
          <p className="exits-table-mobile__math mt-1 mb-0 tabular-nums">
            {t("stockStatus.mobileMetrics")
              .replace("{onHand}", qtyCell(row.onHandQuantity))
              .replace("{available}", qtyCell(row.availableQuantity))
              .replace("{uom}", row.unitOfMeasure)}
          </p>
        </ExitsTableMobileRow>
      ))}
    </ExitsTableMobile>
  );

  return (
    <div
      className="stock-status-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="stock-status-page"
    >
      <PageHeader
        title={t("stockStatus.title")}
        subtitle={boundWorkspace?.branchName ?? undefined}
        description={t("stockStatus.lede")}
        backTo={pageBackNav.inventory.to}
        backLabel={t(pageBackNav.inventory.labelKey)}
        backTestId="page-header-back-inventory"
        actions={
          <Button asChild variant="outline" size="sm">
            <Link
              to="/reports/operational/inventory-movements"
              data-testid="stock-status-view-movements"
            >
              {t("stockStatus.viewMovements")}
            </Link>
          </Button>
        }
      />

      <p
        className="m-0 text-[length:var(--exits-text-sm)] text-muted"
        data-testid="stock-status-current-only"
      >
        {t("stockStatus.currentOnly")}
      </p>

      {productIdFilter ? (
        <div
          className="flex flex-wrap items-center gap-2 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)] px-3 py-2 text-[length:var(--exits-text-sm)]"
          data-testid="stock-status-product-filter-banner"
        >
          <span className="min-w-0">
            {productFilterLabel
              ? t("stockStatus.productFilterActive").replace("{productName}", productFilterLabel)
              : t("stockStatus.productFilterActivePending")}
          </span>
          <Button
            type="button"
            intent="neutral"
            appearance="outline"
            size="sm"
            className="shrink-0"
            onClick={clearProductFilter}
            data-testid="stock-status-clear-product-filter"
          >
            {t("stockStatus.clearProductFilter")}
          </Button>
        </div>
      ) : null}

      <ExitsChipBar
        variant="filter"
        className="exits-chip-bar--scroll"
        ariaLabel={t("stockStatus.stockStateFilter")}
        testId="stock-status-state-filter"
        items={STOCK_STATES.map((state) => ({
          key: state,
          label: t(stockStateLabelKey(state)),
          state: stockState === state ? ("active" as const) : ("idle" as const),
          testId: `stock-status-state-${state}`,
          onSelect: () => updateStockState(state),
        }))}
      />

      {query.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {query.isError ? (
        <ErrorState title={t("error.title")} detail={t("stockStatus.loadFailed")} />
      ) : null}

      {!query.isLoading && !query.isError && rows.length === 0 ? (
        <EmptyState
          align="center"
          variant={debounced || productIdFilter || stockState !== "All" ? "filtered" : "default"}
          icon={<Boxes className="size-5" strokeWidth={1.75} />}
          title={t("stockStatus.empty")}
          detail={t("stockStatus.emptyDetail")}
        />
      ) : null}

      {!query.isLoading && !query.isError && rows.length > 0 ? (
        <ExitsResponsiveDataView
          layout={layout}
          toolbar={toolbar}
          table={table}
          list={list}
          testId="stock-status-data"
          pagination={
            <ExitsTablePagination
              page={page}
              pageSize={pageSize}
              total={totalCount}
              pageSizeOptions={[25, 50, 100]}
              onPageChange={setPage}
              onPageSizeChange={setPageSize}
              rowsPerPageLabel={t("exitsTable.rowsPerPage")}
              previousLabel={t("transfer.prevPage")}
              nextLabel={t("transfer.nextPage")}
              rangeLabel={t("exitsTable.range")}
              data-testid="stock-status-pagination"
            />
          }
        />
      ) : null}

      <div className="stock-status-print-document hidden print:block" data-testid="stock-status-print">
        <h1>{t("stockStatus.title")}</h1>
        <p>{scopeLabel}</p>
        <p>{t("stockStatus.currentOnly")}</p>
        <table>
          <thead>
            <tr>
              <th>{t("stockStatus.colProduct")}</th>
              <th>{t("stockStatus.colBranch")}</th>
              <th>{t("stockStatus.colOnHand")}</th>
              <th>{t("stockStatus.colAvailable")}</th>
              <th>{t("stockStatus.colReserved")}</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={`${row.productId}-${row.branchId}`}>
                <td>{row.productName}</td>
                <td>{row.branchName}</td>
                <td>
                  {qtyCell(row.onHandQuantity)} {row.unitOfMeasure}
                </td>
                <td>
                  {qtyCell(row.availableQuantity)} {row.unitOfMeasure}
                </td>
                <td>
                  {qtyCell(row.reservedQuantity)} {row.unitOfMeasure}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function StockStatusDesktopRow({ row }: { row: InventoryStockStatusRowDto }) {
  return (
    <ExitsTableRow data-testid={`stock-status-row-${row.productId}-${row.branchId}`}>
      <ExitsTableCell cellAlign="text" colSize="flex" className="font-medium">
        <Link
          to={`/inventory/${row.productId}`}
          className="text-foreground underline-offset-2 hover:underline"
        >
          {row.productName}
        </Link>
        {row.sku?.trim() ? (
          <span className="mt-0.5 block text-[length:var(--exits-text-xs)] text-muted">
            {row.sku.trim()}
          </span>
        ) : null}
      </ExitsTableCell>
      <ExitsTableCell cellAlign="text" className="hidden text-muted lg:table-cell">
        {row.categoryName?.trim() || "—"}
      </ExitsTableCell>
      <ExitsTableCell cellAlign="text">{row.branchName}</ExitsTableCell>
      <ExitsTableCell cellAlign="center" colSize="numeric" className="tabular-nums">
        {qtyCell(row.onHandQuantity)}
      </ExitsTableCell>
      <ExitsTableCell
        cellAlign="center"
        colSize="numeric"
        className="hidden tabular-nums md:table-cell"
      >
        {qtyCell(row.sellableQuantity)}
      </ExitsTableCell>
      <ExitsTableCell
        cellAlign="center"
        colSize="numeric"
        className="hidden tabular-nums lg:table-cell"
      >
        {qtyCell(row.reservedQuantity)}
      </ExitsTableCell>
      <ExitsTableCell cellAlign="center" colSize="numeric" className="tabular-nums font-medium">
        {qtyCell(row.availableQuantity)}
      </ExitsTableCell>
      <ExitsTableCell
        cellAlign="center"
        colSize="numeric"
        className="hidden tabular-nums xl:table-cell"
      >
        {qtyCell(row.damagedQuantity)}
      </ExitsTableCell>
      <ExitsTableCell
        cellAlign="center"
        colSize="numeric"
        className="hidden tabular-nums xl:table-cell"
      >
        {qtyCell(row.inspectionHoldQuantity)}
      </ExitsTableCell>
      <ExitsTableCell
        cellAlign="center"
        colSize="numeric"
        className="hidden tabular-nums xl:table-cell"
      >
        {qtyCell(row.pendingReturnQuantity)}
      </ExitsTableCell>
      <ExitsTableCell
        cellAlign="center"
        colSize="numeric"
        className="hidden tabular-nums xl:table-cell"
      >
        {qtyCell(row.expiredQuantity)}
      </ExitsTableCell>
      <ExitsTableCell
        cellAlign="center"
        colSize="numeric"
        className="hidden tabular-nums xl:table-cell"
      >
        {qtyCell(row.saleBlockedQuantity)}
      </ExitsTableCell>
      <ExitsTableCell
        cellAlign="center"
        colSize="numeric"
        className="hidden tabular-nums xl:table-cell"
      >
        {qtyCell(row.inTransitInboundQuantity)}
      </ExitsTableCell>
      <ExitsTableCell
        cellAlign="center"
        colSize="numeric"
        className="hidden tabular-nums xl:table-cell"
      >
        {qtyCell(row.inTransitOutboundQuantity)}
      </ExitsTableCell>
    </ExitsTableRow>
  );
}
