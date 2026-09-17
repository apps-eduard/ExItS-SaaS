import { useEffect, useMemo, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Package } from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import {
  getDirectPurchaseReceipt,
  voidDirectPurchaseReceipt,
  type DirectPurchaseReceiptLineDto,
} from "@/api/pos/pos-direct-purchase-receipts-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
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
import { usePageSmartBack } from "@/navigation/useSmartBack";
import type { SmartBackLocationState } from "@/navigation/smart-back";
import { ActorAttribution } from "@/features/actors/ActorAttribution";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  buildDirectPurchaseDetailLinesExportModel,
  downloadDirectPurchaseDetailLinesCsv,
  downloadDirectPurchaseDetailLinesPdf,
  downloadDirectPurchaseDetailLinesXlsx,
  printDirectPurchaseDetailLinesDocument,
} from "@/features/purchasing/direct-purchase-detail-lines-output";
import {
  buildReceiveMarginWarningToast,
  type ReceiveMarginWarningFlash,
} from "@/features/purchasing/receive-cost-margin";
import { receiptReverseErrorMessage } from "@/features/purchasing/receive-payment";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const RECEIPT_VOID_REASON_MAX = 512;
const PAGE_SIZE_OPTIONS = [10, 25, 50, 100] as const;

type LineFilter = "all" | "with_expiry" | "without_expiry";
type SortKey = "name" | "quantity" | "unitCost" | "lineTotal" | "expiry";

function lineHasExpiry(line: DirectPurchaseReceiptLineDto): boolean {
  return Boolean(line.expiryDate?.trim());
}

function matchesLineSearch(line: DirectPurchaseReceiptLineDto, query: string): boolean {
  if (!query) {
    return true;
  }
  const tokens = [
    line.productNameSnapshot,
    line.unitOfMeasure,
    line.skuSnapshot ?? "",
    line.lotNumber ?? "",
    line.expiryDate ?? "",
  ];
  return tokens.some((token) => token.toLowerCase().includes(query));
}

export function DirectPurchaseDetailPage() {
  const { t } = useI18n();
  const { showToast } = useToast();
  const online = useBrowserOnline();
  const navigate = useNavigate();
  const location = useLocation();
  const { receiptId } = useParams<{ receiptId: string }>();
  const { boundWorkspace, sessionGrant, workspaces } = useWorkspace();
  const queryClient = useQueryClient();
  const allowManage = canManageInventory(sessionGrant);
  const [error, setError] = useState<string | null>(null);
  const [voidOpen, setVoidOpen] = useState(false);
  const [voidReason, setVoidReason] = useState("");
  const [voiding, setVoiding] = useState(false);
  const [searchInput, setSearchInput] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [lineFilter, setLineFilter] = useState<LineFilter>("all");
  const [sortKey, setSortKey] = useState<SortKey | null>(null);
  const [sortDirection, setSortDirection] = useState<ExitsTableSortDirection>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const smartBack = usePageSmartBack({
    fallback: "directPurchases",
    backLabel: t("purchasing.backDirect"),
    backTestId: "page-header-back-purchasing",
  });

  useEffect(() => {
    const state = location.state as
      | (SmartBackLocationState & { receiveMarginWarning?: ReceiveMarginWarningFlash })
      | null;
    const flash = state?.receiveMarginWarning;
    if (!flash || !(flash.count > 0)) {
      return;
    }
    showToast(
      buildReceiveMarginWarningToast({
        count: flash.count,
        productId: flash.productId,
        title: t("purchasing.sellingPriceNeedsReview"),
        detailSingle: t("purchasing.sellingPriceNeedsReviewDetail"),
        detailMany: t("purchasing.sellingPriceNeedsReviewDetailMany"),
        reviewPrice: t("purchasing.reviewPrice"),
        reviewPrices: t("purchasing.reviewPrices"),
      }),
    );
    const preserved: SmartBackLocationState | Record<string, never> = state?.returnTo
      ? { returnTo: state.returnTo, smartBack: true }
      : {};
    navigate(location.pathname, { replace: true, state: preserved });
  }, [location.pathname, location.state, navigate, showToast, t]);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedSearch(searchInput.trim()), 200);
    return () => window.clearTimeout(handle);
  }, [searchInput]);

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, lineFilter, pageSize]);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: ["direct-purchase", workspace?.organizationId, receiptId],
    enabled: Boolean(workspace) && Boolean(receiptId) && online,
    queryFn: ({ signal }) => getDirectPurchaseReceipt(workspace!, receiptId!, signal),
  });

  const actors = useActorDirectory(workspace?.organizationId, [
    query.data?.createdByUserId,
    query.data?.voidedByUserId,
  ]);

  const lines = query.data?.lines ?? [];

  const filteredSortedLines = useMemo(() => {
    const q = debouncedSearch.toLowerCase();
    let next = lines.filter((line) => {
      if (lineFilter === "with_expiry" && !lineHasExpiry(line)) {
        return false;
      }
      if (lineFilter === "without_expiry" && lineHasExpiry(line)) {
        return false;
      }
      return matchesLineSearch(line, q);
    });

    if (sortKey && sortDirection) {
      const dir = sortDirection === "asc" ? 1 : -1;
      next = [...next].sort((a, b) => {
        const av =
          sortKey === "name"
            ? a.productNameSnapshot.toLowerCase()
            : sortKey === "quantity"
              ? a.quantity
              : sortKey === "unitCost"
                ? a.unitCost
                : sortKey === "lineTotal"
                  ? a.lineTotal
                  : (a.expiryDate ?? "");
        const bv =
          sortKey === "name"
            ? b.productNameSnapshot.toLowerCase()
            : sortKey === "quantity"
              ? b.quantity
              : sortKey === "unitCost"
                ? b.unitCost
                : sortKey === "lineTotal"
                  ? b.lineTotal
                  : (b.expiryDate ?? "");
        if (av < bv) return -1 * dir;
        if (av > bv) return 1 * dir;
        return a.lineNumber - b.lineNumber;
      });
    } else {
      next = [...next].sort((a, b) => a.lineNumber - b.lineNumber);
    }

    return next;
  }, [debouncedSearch, lineFilter, lines, sortDirection, sortKey]);

  const pagedLines = useMemo(() => {
    const start = (page - 1) * pageSize;
    return filteredSortedLines.slice(start, start + pageSize);
  }, [filteredSortedLines, page, pageSize]);

  const filteredLineTotal = useMemo(
    () =>
      Math.round(filteredSortedLines.reduce((sum, line) => sum + line.lineTotal, 0) * 100) / 100,
    [filteredSortedLines],
  );

  const filterLabel =
    lineFilter === "with_expiry"
      ? t("purchasing.directFilterWithExpiry")
      : lineFilter === "without_expiry"
        ? t("purchasing.directFilterWithoutExpiry")
        : t("purchasing.directFilterAll");

  function onSort(nextKey: SortKey) {
    const next = cycleExitsTableSort(sortKey, sortDirection, nextKey);
    setSortKey(next.key as SortKey | null);
    setSortDirection(next.direction);
  }

  function buildExportModel() {
    return buildDirectPurchaseDetailLinesExportModel({
      receiptNumber: query.data?.receiptNumber ?? receiptId ?? "direct-purchase",
      filterLabel,
      rows: filteredSortedLines.map((line) => ({
        product: line.productNameSnapshot,
        quantity: line.quantity,
        uom: line.unitOfMeasure,
        unitCost: line.unitCost,
        lineTotal: line.lineTotal,
        expiry: line.expiryDate?.trim() || "",
        lot: line.lotNumber?.trim() || "",
      })),
    });
  }

  async function runOutput(action: "csv" | "xlsx" | "pdf" | "print") {
    try {
      const model = buildExportModel();
      if (action === "csv") {
        downloadDirectPurchaseDetailLinesCsv(model);
        return;
      }
      if (action === "xlsx") {
        downloadDirectPurchaseDetailLinesXlsx(model);
        return;
      }
      if (action === "pdf") {
        downloadDirectPurchaseDetailLinesPdf(model);
        return;
      }
      printDirectPurchaseDetailLinesDocument();
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
  if (!receiptId) {
    return (
      <ErrorState title={t("purchasing.errorTitle")} detail={t("purchasing.directNotFound")} />
    );
  }
  if (query.isLoading) {
    return <LoadingState label={t("purchasing.loading")} />;
  }
  if (query.isError || !query.data) {
    return (
      <ErrorState title={t("purchasing.errorTitle")} detail={t("purchasing.directNotFound")} />
    );
  }

  const receipt = query.data;
  const receivingBranchName = (() => {
    if (!receipt.receivingBranchId) {
      return null;
    }
    const org = workspaces.find((item) => item.organizationId === workspace.organizationId);
    const branch = org?.branches.find((item) => item.branchId === receipt.receivingBranchId);
    return branch?.name ?? receipt.receivingBranchId;
  })();
  const notes = receipt.notes?.trim();
  const isPosted = (receipt.status ?? "Posted") === "Posted";
  const isVoided = receipt.status === "Voided";
  const exportModel = buildExportModel();
  const hasActiveQuery = Boolean(debouncedSearch) || lineFilter !== "all";

  async function onVoid() {
    if (!workspace || !receiptId || !allowManage || !online || voiding || !isPosted) {
      return;
    }
    const reason = voidReason.trim();
    if (!reason) {
      setError(t("purchasing.reverseReasonRequired"));
      return;
    }
    setVoiding(true);
    setError(null);
    try {
      const updated = await voidDirectPurchaseReceipt(workspace, receiptId, { reason });
      queryClient.setQueryData(["direct-purchase", workspace.organizationId, receiptId], updated);
      await queryClient.invalidateQueries({ queryKey: ["direct-purchases"] });
      await queryClient.invalidateQueries({ queryKey: ["inventory"] });
      setVoidOpen(false);
      setVoidReason("");
    } catch (err) {
      setError(
        receiptReverseErrorMessage(
          err,
          t("purchasing.reverseFailed"),
          t("supplierPayables.reverseBlockedByPayments"),
        ),
      );
    } finally {
      setVoiding(false);
    }
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="direct-purchase-detail-page">
      <div className="direct-purchase-detail-print-root" aria-hidden>
        <h1>{t("purchasing.items")}</h1>
        <p>{exportModel.receiptNumber}</p>
        <table>
          <thead>
            <tr>
              <th>{t("purchasing.receiveProduct")}</th>
              <th>{t("purchasing.qty")}</th>
              <th>{t("purchasing.unitPurchaseCost")}</th>
              <th>{t("purchasing.lineTotal")}</th>
              <th>{t("purchasing.expiryDate")}</th>
              <th>{t("purchasing.lotNumber")}</th>
            </tr>
          </thead>
          <tbody>
            {exportModel.rows.map((row) => (
              <tr key={`${row.product}-${row.uom}-${row.quantity}-${row.lineTotal}`}>
                <td>
                  {row.product} ({row.uom})
                </td>
                <td>{row.quantity}</td>
                <td>{row.unitCost}</td>
                <td>{row.lineTotal}</td>
                <td>{row.expiry || "—"}</td>
                <td>{row.lot || "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <PageHeader
        title={receipt.receiptNumber}
        description={t("purchasing.directDetailLede")}
        {...smartBack}
      />

      {error ? <ErrorState title={t("purchasing.errorTitle")} detail={error} /> : null}

      <section aria-labelledby="direct-purchase-info">
        <h2
          id="direct-purchase-info"
          className="m-0 mb-2 text-[length:var(--exits-text-md)] font-medium"
        >
          {t("purchasing.purchaseInformation")}
        </h2>
        <Card className="flex flex-col gap-3 p-3">
          <div className="flex flex-wrap items-center gap-2" data-testid="direct-purchase-status">
            <StatusChip tone={isVoided ? "danger" : "success"}>
              {isVoided
                ? t("purchasing.receiptStatus.voided")
                : t("purchasing.receiptStatus.posted")}
            </StatusChip>
          </div>
          <dl className="m-0 grid gap-3 sm:grid-cols-2">
            <div>
              <dt className="text-[length:var(--exits-text-sm)] text-muted">
                {t("purchasing.purchaseDate")}
              </dt>
              <dd className="m-0" data-testid="direct-purchase-date">
                {receipt.purchaseDate}
              </dd>
            </div>
            <div>
              <dt className="text-[length:var(--exits-text-sm)] text-muted">
                {t("purchasing.boughtFrom")}
              </dt>
              <dd className="m-0" data-testid="direct-purchase-source">
                {receipt.sourceNameSnapshot ?? t("purchasing.sourceEmpty")}
              </dd>
            </div>
            {receivingBranchName ? (
              <div>
                <dt className="text-[length:var(--exits-text-sm)] text-muted">
                  {t("purchasing.receivingBranch")}
                </dt>
                <dd className="m-0" data-testid="direct-purchase-receiving-branch">
                  {t("purchasing.receivedAtBranch").replace("{name}", receivingBranchName)}
                </dd>
              </div>
            ) : null}
            {receipt.referenceNumber ? (
              <div>
                <dt className="text-[length:var(--exits-text-sm)] text-muted">
                  {t("purchasing.reference")}
                </dt>
                <dd className="m-0" data-testid="direct-purchase-reference">
                  {receipt.referenceNumber}
                </dd>
              </div>
            ) : null}
            <div>
              <dt className="text-[length:var(--exits-text-sm)] text-muted">
                {t("purchasing.totalPurchaseCost")}
              </dt>
              <dd className="m-0" data-testid="direct-purchase-total">
                <MoneyDisplay amount={receipt.totalCost} />
              </dd>
            </div>
          </dl>
          <ActorAttribution
            labelKey="common.recordedBy"
            actorId={receipt.createdByUserId}
            occurredAtUtc={receipt.createdAtUtc}
            resolved={actors.resolve(receipt.createdByUserId)}
            isLoading={actors.isResolving}
            testId="direct-purchase-recorded-by"
          />
          {isVoided && receipt.voidedByUserId ? (
            <ActorAttribution
              labelKey="purchasing.reversedBy"
              actorId={receipt.voidedByUserId}
              occurredAtUtc={receipt.voidedAtUtc}
              resolved={actors.resolve(receipt.voidedByUserId)}
              isLoading={actors.isResolving}
              testId="direct-purchase-reversed-by"
            />
          ) : null}
          {isVoided && receipt.voidReason ? (
            <p
              className="m-0 text-[length:var(--exits-text-sm)] text-muted"
              data-testid="direct-purchase-void-reason"
            >
              {t("purchasing.reverseReason")}: {receipt.voidReason}
            </p>
          ) : null}
          {notes ? (
            <div data-testid="direct-purchase-notes">
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("purchasing.notes")}
              </p>
              <p className="mt-1 mb-0 whitespace-pre-wrap text-[length:var(--exits-text-sm)]">
                {notes}
              </p>
            </div>
          ) : null}
        </Card>
      </section>

      <section aria-labelledby="direct-purchase-lines">
        <h2
          id="direct-purchase-lines"
          className="m-0 mb-2 text-[length:var(--exits-text-md)] font-medium"
        >
          {t("purchasing.items")}
        </h2>

        <ExitsTableContainer data-testid="direct-purchase-lines-table">
          <ExitsTableToolbar
            search={
              <SearchField
                label={t("purchasing.searchDirectLines")}
                value={searchInput}
                placeholder={t("purchasing.searchDirectLines")}
                onChange={(e) => setSearchInput(e.target.value)}
                onClear={() => setSearchInput("")}
                data-testid="direct-purchase-lines-search"
              />
            }
            filter={
              <ExitsChipBar
                variant="filter"
                ariaLabel={t("purchasing.directLineFilter")}
                testId="direct-purchase-lines-filter"
                items={[
                  {
                    key: "all",
                    label: t("purchasing.directFilterAll"),
                    state: lineFilter === "all" ? "active" : "idle",
                    onSelect: () => setLineFilter("all"),
                  },
                  {
                    key: "with_expiry",
                    label: t("purchasing.directFilterWithExpiry"),
                    state: lineFilter === "with_expiry" ? "active" : "idle",
                    onSelect: () => setLineFilter("with_expiry"),
                  },
                  {
                    key: "without_expiry",
                    label: t("purchasing.directFilterWithoutExpiry"),
                    state: lineFilter === "without_expiry" ? "active" : "idle",
                    onSelect: () => setLineFilter("without_expiry"),
                  },
                ]}
              />
            }
            output={
              <ExitsTableOutputActions
                csvLabel={t("exitsTable.exportCsv")}
                xlsxLabel={t("exitsTable.exportExcel")}
                pdfLabel={t("exitsTable.exportPdf")}
                printLabel={t("exitsTable.print")}
                menuLabel={t("exitsTable.exportPrintMenu")}
                onCsv={() => void runOutput("csv")}
                onXlsx={() => void runOutput("xlsx")}
                onPdf={() => void runOutput("pdf")}
                onPrint={() => void runOutput("print")}
              />
            }
          />

          {filteredSortedLines.length === 0 ? (
            <EmptyState
              align="center"
              size="compact"
              icon={<Package className="size-5" strokeWidth={1.75} />}
              title={
                hasActiveQuery
                  ? t("purchasing.directLinesNoMatch")
                  : t("purchasing.directLinesEmpty")
              }
              detail={
                hasActiveQuery
                  ? t("purchasing.directLinesNoMatchDetail")
                  : t("purchasing.directLinesEmptyDetail")
              }
              testId="direct-purchase-lines-empty"
            />
          ) : (
            <>
              <ExitsTable>
                <ExitsTableHeader>
                  <ExitsTableRow>
                    <ExitsTableHead
                      cellAlign="text"
                      sortable
                      sortDirection={sortKey === "name" ? sortDirection : null}
                      onSort={() => onSort("name")}
                    >
                      {t("purchasing.receiveProduct")}
                    </ExitsTableHead>
                    <ExitsTableHead
                      cellAlign="numeric"
                      sortable
                      sortDirection={sortKey === "quantity" ? sortDirection : null}
                      onSort={() => onSort("quantity")}
                    >
                      {t("purchasing.qty")}
                    </ExitsTableHead>
                    <ExitsTableHead
                      cellAlign="money"
                      sortable
                      sortDirection={sortKey === "unitCost" ? sortDirection : null}
                      onSort={() => onSort("unitCost")}
                    >
                      {t("purchasing.unitPurchaseCost")}
                    </ExitsTableHead>
                    <ExitsTableHead
                      cellAlign="money"
                      sortable
                      sortDirection={sortKey === "lineTotal" ? sortDirection : null}
                      onSort={() => onSort("lineTotal")}
                    >
                      {t("purchasing.lineTotal")}
                    </ExitsTableHead>
                    <ExitsTableHead
                      cellAlign="text"
                      sortable
                      sortDirection={sortKey === "expiry" ? sortDirection : null}
                      onSort={() => onSort("expiry")}
                    >
                      {t("purchasing.expiryDate")}
                    </ExitsTableHead>
                    <ExitsTableHead cellAlign="text">{t("purchasing.lotNumber")}</ExitsTableHead>
                  </ExitsTableRow>
                </ExitsTableHeader>
                <ExitsTableBody>
                  {pagedLines.map((line) => (
                    <ExitsTableRow
                      key={line.lineId}
                      data-testid={`direct-purchase-line-${line.lineId}`}
                    >
                      <ExitsTableCell cellAlign="text" className="font-medium">
                        {line.productNameSnapshot}
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="numeric" className="tabular-nums">
                        {line.quantity} {line.unitOfMeasure}
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="money">
                        <MoneyDisplay amount={line.unitCost} />
                        <span className="text-muted"> / {line.unitOfMeasure}</span>
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="money">
                        <MoneyDisplay amount={line.lineTotal} />
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="text">
                        {line.expiryDate?.trim() || "—"}
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="text">
                        {line.lotNumber?.trim() || "—"}
                      </ExitsTableCell>
                    </ExitsTableRow>
                  ))}
                </ExitsTableBody>
                <ExitsTableFooter>
                  <ExitsTableRow>
                    <ExitsTableCell cellAlign="text" colSpan={3}>
                      {t("purchasing.directLinesTotal")}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="money" colSpan={3}>
                      <span
                        className="font-semibold tabular-nums"
                        data-testid="direct-purchase-lines-filtered-total"
                      >
                        <MoneyDisplay amount={filteredLineTotal} />
                      </span>
                    </ExitsTableCell>
                  </ExitsTableRow>
                </ExitsTableFooter>
              </ExitsTable>

              <ExitsTableMobile data-testid="direct-purchase-lines-mobile">
                {pagedLines.map((line) => (
                  <ExitsTableMobileRow
                    key={line.lineId}
                    data-testid={`direct-purchase-line-mobile-${line.lineId}`}
                  >
                    <div className="exits-table-mobile__title-row">
                      <p className="exits-table-mobile__title">{line.productNameSnapshot}</p>
                      <MoneyDisplay amount={line.lineTotal} />
                    </div>
                    <p className="exits-table-mobile__meta">
                      {line.quantity} {line.unitOfMeasure}
                    </p>
                    <p className="exits-table-mobile__math">
                      <MoneyDisplay amount={line.unitCost} /> / {line.unitOfMeasure}
                    </p>
                    <p className="exits-table-mobile__meta">
                      {t("purchasing.expiryDate")}: {line.expiryDate?.trim() || "—"}
                    </p>
                    <p className="exits-table-mobile__meta">
                      {t("purchasing.lotNumber")}: {line.lotNumber?.trim() || "—"}
                    </p>
                  </ExitsTableMobileRow>
                ))}
              </ExitsTableMobile>

              <ExitsTablePagination
                page={page}
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
            </>
          )}
        </ExitsTableContainer>
      </section>

      {allowManage && isPosted && online ? (
        <Button
          type="button"
          variant="outline"
          className="w-fit"
          onClick={() => {
            setVoidOpen(true);
            setError(null);
          }}
          data-testid="direct-purchase-reverse"
        >
          {t("purchasing.reverseReceipt")}
        </Button>
      ) : null}

      {voidOpen ? (
        <div
          className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center"
          role="dialog"
          aria-modal="true"
          aria-labelledby="direct-purchase-reverse-title"
          data-testid="direct-purchase-reverse-dialog"
        >
          <Card className="flex w-full max-w-md flex-col gap-3 p-4">
            <h2
              id="direct-purchase-reverse-title"
              className="m-0 text-[length:var(--exits-text-lg)] font-semibold"
            >
              {t("purchasing.reverseTitle")}
            </h2>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("purchasing.reverseLede")}
            </p>
            <p className="m-0 text-[length:var(--exits-text-sm)]">
              {receipt.receiptNumber} · {receipt.purchaseDate}
              {receipt.sourceNameSnapshot ? ` · ${receipt.sourceNameSnapshot}` : ""}
            </p>
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              <span className="font-medium">{t("purchasing.reverseReason")}</span>
              <textarea
                className="min-h-24 rounded-[var(--exits-radius-md)] border border-border bg-background px-3 py-2"
                value={voidReason}
                maxLength={RECEIPT_VOID_REASON_MAX}
                onChange={(e) => setVoidReason(e.target.value)}
                data-testid="direct-purchase-reverse-reason"
              />
            </label>
            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                variant="destructive"
                disabled={voiding || !voidReason.trim()}
                onClick={() => void onVoid()}
                data-testid="direct-purchase-reverse-confirm"
              >
                {voiding ? t("purchasing.reversing") : t("purchasing.reverseConfirm")}
              </Button>
              <Button
                type="button"
                variant="outline"
                disabled={voiding}
                onClick={() => {
                  setVoidOpen(false);
                  setVoidReason("");
                }}
                data-testid="direct-purchase-reverse-cancel"
              >
                {t("purchasing.reverseCancel")}
              </Button>
            </div>
          </Card>
        </div>
      ) : null}
    </div>
  );
}
