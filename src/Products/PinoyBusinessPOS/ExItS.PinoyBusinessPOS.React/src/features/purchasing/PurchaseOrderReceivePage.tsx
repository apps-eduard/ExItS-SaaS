import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canManagePurchasing } from "@/access/pos-capabilities";
import { PosApiError } from "@/api/pos/pos-http";
import {
  getGoodsReceipt,
  getPurchaseOrder,
  isPurchaseOrderReceivable,
  receivePurchaseOrder,
  type PosGoodsReceiptDto,
} from "@/api/pos/pos-purchase-orders-client";
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
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { useToast } from "@/components/exits/ToastProvider";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { ReceivePaymentSection } from "@/features/purchasing/ReceivePaymentSection";
import {
  formatMoneyInput,
  parseMoneyInput,
  remainingCredit,
  roundMoney,
  validateReceivePaidNow,
  type ReceivePaymentMethodCode,
  type ReceivePaymentMode,
} from "@/features/purchasing/receive-payment";
import {
  buildPurchaseOrderReceiveExportModel,
  downloadPurchaseOrderReceiveCsv,
  downloadPurchaseOrderReceivePdf,
  downloadPurchaseOrderReceiveXlsx,
  printPurchaseOrderReceiveDocument,
} from "@/features/purchasing/purchase-order-receive-output";
import { buildReceivePlan, parseNonNegativeQty } from "@/features/purchasing/receive-math";
import { selectUntrackedReceivingLines } from "@/features/purchasing/receive-tracking";
import { useI18n } from "@/i18n/I18nProvider";
import { createSecureMutationId } from "@/lib/secure-mutation-id";
import { resolveAmbiguousMutationOutcome } from "@/runtime/ambiguous-mutation-outcome";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PAGE_SIZE_OPTIONS = [10, 25, 50, 100] as const;

type LineEdit = {
  productId: string;
  name: string;
  uom: string;
  orderedQty: number;
  receivedQty: number;
  outstandingQty: number;
  unitPurchaseCost: number;
  tracksExpiration: boolean;
  isInventoryTracked: boolean;
  goodText: string;
  damagedText: string;
  closeRemaining: boolean;
  expiryDate: string;
  lotNumber: string;
};

type LineFilter = "all" | "outstanding";
type SortKey = "name" | "ordered" | "received" | "outstanding" | "unitCost";

export function PurchaseOrderReceivePage() {
  const { t } = useI18n();
  const { showToast } = useToast();
  const navigate = useNavigate();
  const online = useBrowserOnline();
  const { purchaseOrderId } = useParams<{ purchaseOrderId: string }>();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManagePurchasing(sessionGrant);
  const [lines, setLines] = useState<LineEdit[] | null>(null);
  const [deliveryReference, setDeliveryReference] = useState("");
  const [notes, setNotes] = useState("");
  const [reviewing, setReviewing] = useState(false);
  const [trackingConfirm, setTrackingConfirm] = useState(false);
  const [completedReceipt, setCompletedReceipt] = useState<PosGoodsReceiptDto | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [statusLocked, setStatusLocked] = useState(false);
  const [paidNowText, setPaidNowText] = useState("");
  const [dueDate, setDueDate] = useState("");
  const [paymentMode, setPaymentMode] = useState<ReceivePaymentMode>("paidInFull");
  const [paymentMethod, setPaymentMethod] = useState<ReceivePaymentMethodCode>("Cash");
  const [paidNowTouched, setPaidNowTouched] = useState(false);
  const [searchInput, setSearchInput] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [lineFilter, setLineFilter] = useState<LineFilter>("outstanding");
  const [sortKey, setSortKey] = useState<SortKey | null>(null);
  const [sortDirection, setSortDirection] = useState<ExitsTableSortDirection>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const goodsReceiptIdRef = useRef<string | null>(null);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const branchName = boundWorkspace?.branchName?.trim() || boundWorkspace?.branchId || "";

  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedSearch(searchInput.trim()), 200);
    return () => window.clearTimeout(handle);
  }, [searchInput]);

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, lineFilter, pageSize, sortKey, sortDirection]);

  const query = useQuery({
    queryKey: ["purchase-order", workspace?.organizationId, purchaseOrderId],
    enabled: Boolean(workspace) && Boolean(purchaseOrderId) && online,
    queryFn: async ({ signal }) => {
      const po = await getPurchaseOrder(workspace!, purchaseOrderId!, signal);
      setLines(
        po.lines.map((line) => ({
          productId: line.productId ?? "",
          name: line.nameSnapshot ?? line.productId ?? "",
          uom: line.uomSnapshot ?? "",
          orderedQty: line.orderedQty,
          receivedQty: line.receivedQty,
          outstandingQty: line.outstandingQty,
          unitPurchaseCost: line.unitPurchaseCost,
          tracksExpiration: line.tracksExpiration === true,
          // Only explicit false is untracked; undefined/true skip enable-tracking confirmation.
          isInventoryTracked: line.isInventoryTracked !== false,
          goodText: line.outstandingQty > 0 ? String(line.outstandingQty) : "",
          damagedText: "",
          closeRemaining: false,
          expiryDate: "",
          lotNumber: "",
        })),
      );
      return po;
    },
  });

  const po = query.data;
  const canReceive =
    allowManage &&
    online &&
    po != null &&
    isPurchaseOrderReceivable(po) &&
    (lines?.some((l) => l.outstandingQty > 0) ?? false);

  const hasMissingExpiryOnReceive =
    lines?.some((line) => {
      const good = parseNonNegativeQty(line.goodText) ?? 0;
      return line.tracksExpiration && good > 0 && !line.expiryDate.trim();
    }) ?? false;

  const untrackedReceivingLines = useMemo(
    () => (lines ? selectUntrackedReceivingLines(lines) : []),
    [lines],
  );

  const estimatedTotal = useMemo(() => {
    if (!lines) {
      return 0;
    }
    return roundMoney(
      lines.reduce((sum, line) => {
        const good = parseNonNegativeQty(line.goodText) ?? 0;
        return sum + good * line.unitPurchaseCost;
      }, 0),
    );
  }, [lines]);

  const filteredSortedLines = useMemo(() => {
    if (!lines) {
      return [];
    }
    const q = debouncedSearch.toLowerCase();
    let next = lines.filter((line) => {
      if (lineFilter === "outstanding" && !(line.outstandingQty > 0)) {
        return false;
      }
      if (!q) {
        return true;
      }
      return [line.name, line.uom, line.productId].some((token) =>
        token.toLowerCase().includes(q),
      );
    });
    if (sortKey && sortDirection) {
      const dir = sortDirection === "asc" ? 1 : -1;
      next = [...next].sort((a, b) => {
        const av =
          sortKey === "name"
            ? a.name.toLowerCase()
            : sortKey === "ordered"
              ? a.orderedQty
              : sortKey === "received"
                ? a.receivedQty
                : sortKey === "outstanding"
                  ? a.outstandingQty
                  : a.unitPurchaseCost;
        const bv =
          sortKey === "name"
            ? b.name.toLowerCase()
            : sortKey === "ordered"
              ? b.orderedQty
              : sortKey === "received"
                ? b.receivedQty
                : sortKey === "outstanding"
                  ? b.outstandingQty
                  : b.unitPurchaseCost;
        if (av < bv) return -1 * dir;
        if (av > bv) return 1 * dir;
        return 0;
      });
    }
    return next;
  }, [debouncedSearch, lineFilter, lines, sortDirection, sortKey]);

  const pagedLines = useMemo(() => {
    const start = (page - 1) * pageSize;
    return filteredSortedLines.slice(start, start + pageSize);
  }, [filteredSortedLines, page, pageSize]);

  useEffect(() => {
    if (paymentMode === "paidInFull") {
      setPaidNowText(formatMoneyInput(estimatedTotal));
      setDueDate("");
      return;
    }
    if (!paidNowTouched) {
      setPaidNowText(formatMoneyInput(estimatedTotal));
    }
  }, [estimatedTotal, paidNowTouched, paymentMode]);

  const paidNowValue = parseMoneyInput(paidNowText);
  const filterLabel =
    lineFilter === "outstanding"
      ? t("purchasing.receiveFilterOutstanding")
      : t("purchasing.receiveFilterAll");

  function onSort(nextKey: SortKey) {
    const next = cycleExitsTableSort(sortKey, sortDirection, nextKey);
    setSortKey(next.key as SortKey | null);
    setSortDirection(next.direction);
  }

  function buildExportModel() {
    return buildPurchaseOrderReceiveExportModel({
      poNumber: po?.poNumber ?? purchaseOrderId ?? "purchase-order",
      filterLabel,
      rows: filteredSortedLines.map((line) => ({
        product: line.name,
        uom: line.uom,
        ordered: line.orderedQty,
        received: line.receivedQty,
        outstanding: line.outstandingQty,
        unitCost: line.unitPurchaseCost,
        goodReceived: line.goodText || "0",
        damaged: line.damagedText || "0",
        closeRemaining: line.closeRemaining ? "Yes" : "No",
        expiry: line.expiryDate || "",
        lot: line.lotNumber || "",
      })),
    });
  }

  async function runOutput(action: "csv" | "xlsx" | "pdf" | "print") {
    try {
      const model = buildExportModel();
      if (action === "csv") {
        downloadPurchaseOrderReceiveCsv(model);
        return;
      }
      if (action === "xlsx") {
        downloadPurchaseOrderReceiveXlsx(model);
        return;
      }
      if (action === "pdf") {
        downloadPurchaseOrderReceivePdf(model);
        return;
      }
      printPurchaseOrderReceiveDocument();
    } catch {
      showToast({
        title: t("exitsTable.outputFailed"),
        tone: "error",
      });
    }
  }

  function onPaymentModeChange(mode: ReceivePaymentMode) {
    setPaymentMode(mode);
    setPaidNowTouched(false);
    if (mode === "paidInFull") {
      setPaidNowText(formatMoneyInput(estimatedTotal));
      setDueDate("");
    }
  }

  function updateLine(productId: string, patch: Partial<LineEdit>) {
    setLines((prev) =>
      (prev ?? []).map((line) => (line.productId === productId ? { ...line, ...patch } : line)),
    );
  }

  function tryPlan() {
    if (!lines) {
      return null;
    }
    const parsed = lines.map((line) => {
      const good = parseNonNegativeQty(line.goodText);
      const damaged = parseNonNegativeQty(line.damagedText);
      return { line, good, damaged };
    });
    if (parsed.some((p) => p.good === null || p.damaged === null)) {
      setError(t("purchasing.invalidReceiveQty"));
      return null;
    }
    const missingExpiry = parsed.find(
      ({ line, good }) => line.tracksExpiration && (good ?? 0) > 0 && !line.expiryDate.trim(),
    );
    if (missingExpiry) {
      setError(t("purchasing.expiryRequired"));
      return null;
    }
    const result = buildReceivePlan(
      parsed.map(({ line, good, damaged }) => ({
        productId: line.productId,
        outstandingQty: line.outstandingQty,
        goodQty: good!,
        damagedQty: damaged!,
        closeRemaining: line.closeRemaining,
      })),
    );
    if (!result.ok) {
      if (result.error === "over_receive") {
        setError(t("purchasing.overReceive"));
      } else if (result.error === "no_activity") {
        setError(t("purchasing.receiveRequiresLines"));
      } else {
        setError(t("purchasing.invalidReceiveQty"));
      }
      return null;
    }
    setError(null);
    return result.lines;
  }

  function onReview() {
    if (!tryPlan()) {
      return;
    }
    const paidNow =
      paymentMode === "paidInFull" ? estimatedTotal : parseMoneyInput(paidNowText);
    const paidError = validateReceivePaidNow(estimatedTotal, paidNow);
    if (paidError) {
      setError(t(paidError));
      return;
    }
    if (paidNow! > 0 && !paymentMethod) {
      setError(t("purchasing.paymentMethodRequired"));
      return;
    }
    setError(null);
    setTrackingConfirm(false);
    setReviewing(true);
  }

  function onReviewConfirmClick() {
    if (!lines) {
      return;
    }
    if (!tryPlan()) {
      return;
    }
    const paidNow =
      paymentMode === "paidInFull" ? estimatedTotal : parseMoneyInput(paidNowText);
    const paidError = validateReceivePaidNow(estimatedTotal, paidNow);
    if (paidError) {
      setError(t(paidError));
      return;
    }
    if (paidNow! > 0 && !paymentMethod) {
      setError(t("purchasing.paymentMethodRequired"));
      return;
    }
    const untracked = selectUntrackedReceivingLines(lines);
    if (untracked.length > 0) {
      setError(null);
      setTrackingConfirm(true);
      return;
    }
    void postReceive(false);
  }

  async function postReceive(enableTrackingIfNeeded: boolean) {
    if (!workspace || !purchaseOrderId || !canReceive || busy || statusLocked || !lines) {
      return;
    }
    const planned = tryPlan();
    if (!planned) {
      return;
    }
    const paidNow =
      paymentMode === "paidInFull" ? estimatedTotal : parseMoneyInput(paidNowText);
    const paidError = validateReceivePaidNow(estimatedTotal, paidNow);
    if (paidError) {
      setError(t(paidError));
      return;
    }
    if (paidNow! > 0 && !paymentMethod) {
      setError(t("purchasing.paymentMethodRequired"));
      return;
    }
    if (!goodsReceiptIdRef.current) {
      const generated = createSecureMutationId();
      if (!generated.ok) {
        setError(t("purchasing.receiveFailed"));
        return;
      }
      goodsReceiptIdRef.current = generated.id;
    }
    const goodsReceiptId = goodsReceiptIdRef.current;
    setBusy(true);
    setError(null);
    try {
      const receipt = await receivePurchaseOrder(workspace, purchaseOrderId, {
        goodsReceiptId,
        deliveryReference: deliveryReference.trim() || null,
        notes: notes.trim() || null,
        paidNow,
        dueDate:
          remainingCredit(estimatedTotal, paidNow!) > 0 && dueDate.trim()
            ? dueDate.trim()
            : null,
        paymentMethodAtReceipt: paidNow! > 0 ? paymentMethod : null,
        enableTrackingIfNeeded: enableTrackingIfNeeded ? true : undefined,
        lines: planned.map((line) => {
          const edit = lines.find((l) => l.productId === line.productId);
          const goodQty = line.receiveQty;
          return {
            productId: line.productId,
            receiveQty: line.receiveQty,
            damagedQty: line.damagedQty,
            shortClosedQty: line.shortClosedQty,
            discrepancyKind: line.discrepancyKind,
            discrepancyNote: line.discrepancyKind && notes.trim() ? notes.trim() : null,
            expiryDate:
              edit?.tracksExpiration && goodQty > 0 && edit.expiryDate.trim()
                ? edit.expiryDate.trim()
                : null,
            lotNumber:
              edit?.tracksExpiration && goodQty > 0 && edit.lotNumber.trim()
                ? edit.lotNumber.trim()
                : null,
          };
        }),
      });
      goodsReceiptIdRef.current = null;
      setCompletedReceipt(receipt);
      setBusy(false);
    } catch (err) {
      setError(t("checkout.confirmingTransaction"));
      const outcome = await resolveAmbiguousMutationOutcome({
        error: err,
        lookup: () => getGoodsReceipt(workspace, goodsReceiptId),
      });
      if (outcome.kind === "confirmed") {
        goodsReceiptIdRef.current = null;
        setCompletedReceipt(outcome.value);
        setBusy(false);
        return;
      }
      if (outcome.kind === "still_unknown") {
        setStatusLocked(true);
        setError(t("checkout.transactionStatusUnknown"));
        return;
      }
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("purchasing.receiveFailed"))
          : t("purchasing.receiveFailed"),
      );
      setBusy(false);
    }
  }

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }
  if (!purchaseOrderId) {
    return <ErrorState title={t("purchasing.errorTitle")} detail={t("purchasing.notFound")} />;
  }
  if (query.isLoading || !lines) {
    return <LoadingState label={t("purchasing.loading")} />;
  }
  if (query.isError || !po) {
    return <ErrorState title={t("purchasing.errorTitle")} detail={t("purchasing.notFound")} />;
  }

  if (completedReceipt) {
    return (
      <div className="flex min-w-0 flex-col gap-4" data-testid="purchase-order-receive-page">
        <PageHeader
          title={t("purchasing.receiptCompleted")}
          description={po.poNumber ?? t("purchasing.receiveSubtitle")}
          backTo={`/purchasing/${purchaseOrderId}`}
          backLabel={t("purchasing.backDetail")}
          backTestId="page-header-back-purchasing"
        />
        <Card data-testid="receive-completed-panel">
          <ul className="m-0 flex list-none flex-col gap-3 p-0">
            {completedReceipt.lines.map((line) => {
              const qty = line.quantityReceived ?? line.receivedQty ?? 0;
              const amount = line.lineTotalSnapshot;
              return (
                <li
                  key={line.lineId}
                  className="rounded-md border border-border p-3"
                  data-testid={`receive-completed-line-${line.productId}`}
                >
                  <div className="font-medium">{line.nameSnapshot}</div>
                  <dl className="mt-2 mb-0 grid gap-1 text-[length:var(--exits-text-sm)]">
                    <div className="flex flex-wrap gap-x-2">
                      <dt className="text-muted">{t("purchasing.received")}:</dt>
                      <dd className="m-0">
                        {qty} {line.uomSnapshot}
                      </dd>
                    </div>
                    <div className="flex flex-wrap items-baseline gap-x-2">
                      <dt className="text-muted">{t("purchasing.purchaseAmount")}:</dt>
                      <dd className="m-0">
                        <MoneyDisplay amount={amount} />
                      </dd>
                    </div>
                    {line.inventoryTrackingEnabled ? (
                      <>
                        <div className="text-muted">{t("purchasing.inventoryTrackingEnabled")}</div>
                        {line.newTrackedStock != null ? (
                          <div className="flex flex-wrap gap-x-2">
                            <dt className="text-muted">{t("purchasing.newTrackedStock")}:</dt>
                            <dd className="m-0">{line.newTrackedStock}</dd>
                          </div>
                        ) : null}
                      </>
                    ) : line.previousTrackedStock != null || line.newTrackedStock != null ? (
                      <div className="flex flex-wrap gap-x-3 gap-y-1">
                        {line.previousTrackedStock != null ? (
                          <span>
                            <span className="text-muted">{t("purchasing.previousTrackedStock")}: </span>
                            {line.previousTrackedStock}
                          </span>
                        ) : null}
                        <span>
                          <span className="text-muted">{t("purchasing.received")}: </span>
                          {qty}
                        </span>
                        {line.newTrackedStock != null ? (
                          <span>
                            <span className="text-muted">{t("purchasing.newTrackedStock")}: </span>
                            {line.newTrackedStock}
                          </span>
                        ) : null}
                      </div>
                    ) : null}
                  </dl>
                </li>
              );
            })}
          </ul>
          <div className="mt-4">
            <Button
              type="button"
              onClick={() => navigate(`/purchasing/${purchaseOrderId}`, { replace: true })}
              data-testid="receive-back-to-po"
            >
              {t("purchasing.backDetail")}
            </Button>
          </div>
        </Card>
      </div>
    );
  }

  const exportModel = buildExportModel();

  return (
    <div
      className="purchase-order-receive-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="purchase-order-receive-page"
    >
      <div className="purchase-order-receive-print-root" aria-hidden>
        <h1>{t("purchasing.receiveTitle")}</h1>
        <p>{exportModel.poNumber}</p>
        <table>
          <thead>
            <tr>
              <th>{t("purchasing.receiveProduct")}</th>
              <th>{t("purchasing.ordered")}</th>
              <th>{t("purchasing.received")}</th>
              <th>{t("purchasing.outstanding")}</th>
              <th>{t("purchasing.goodReceived")}</th>
              <th>{t("purchasing.damaged")}</th>
            </tr>
          </thead>
          <tbody>
            {exportModel.rows.map((row) => (
              <tr key={`${row.product}-${row.uom}-${row.ordered}`}>
                <td>
                  {row.product} ({row.uom})
                </td>
                <td>{row.ordered}</td>
                <td>{row.received}</td>
                <td>{row.outstanding}</td>
                <td>{row.goodReceived}</td>
                <td>{row.damaged}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <PageHeader
        title={t("purchasing.receiveTitle")}
        subtitle={po.poNumber ?? undefined}
        description={t("purchasing.receiptsStockNote")}
        backTo={`/purchasing/${purchaseOrderId}`}
        backLabel={t("purchasing.backDetail")}
        backTestId="page-header-back-purchasing"
      />
      {!online ? (
        <Notice tone="warning" testId="receive-offline">
          {t("purchasing.offline")}
        </Notice>
      ) : null}
      {po.canReceiveConnected === false ? (
        <Notice tone="warning" testId="receive-connected-gate">
          {t("purchasing.connectedReceiveBlocked")}
        </Notice>
      ) : null}
      {error ? (
        <Notice tone="danger" testId="receive-error">
          {error}
        </Notice>
      ) : null}

      {trackingConfirm ? (
        <Card data-testid="receive-tracking-confirm">
          <ul className="m-0 flex list-none flex-col gap-4 p-0">
            {untrackedReceivingLines.map((line) => (
              <li key={line.productId} data-testid={`receive-tracking-line-${line.productId}`}>
                <div className="font-medium">{line.name}</div>
                <p className="mt-1 mb-1 text-[length:var(--exits-text-sm)]">
                  {t("purchasing.received")}: {line.receivedQty} {line.uom}
                </p>
                <p className="mt-0 mb-1 flex flex-wrap items-baseline gap-1 text-[length:var(--exits-text-sm)]">
                  <span className="text-muted">{t("purchasing.purchasePrice")}:</span>
                  <MoneyDisplay amount={line.unitPurchaseCost} />
                </p>
                <p className="mt-0 mb-2 flex flex-wrap items-baseline gap-1 text-[length:var(--exits-text-sm)]">
                  <span className="text-muted">{t("purchasing.purchaseAmount")}:</span>
                  <MoneyDisplay amount={line.purchaseAmount} />
                </p>
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("purchasing.inventoryNotCurrentlyTracked")}
                </p>
                <p className="mt-2 mb-1 text-[length:var(--exits-text-sm)] font-medium">
                  {t("purchasing.receivingWill")}
                </p>
                <ul className="m-0 list-disc pl-5 text-[length:var(--exits-text-sm)]">
                  <li>{t("purchasing.enableInventoryTracking")}</li>
                  <li>{t("purchasing.trackingStartsWithReceived")}</li>
                  <li>
                    {t("purchasing.addStockToBranch").replace("{name}", branchName || "—")}
                  </li>
                </ul>
              </li>
            ))}
          </ul>
          <div className="mt-4 flex flex-wrap gap-2">
            <Button
              type="button"
              variant="ghost"
              disabled={busy || statusLocked}
              onClick={() => setTrackingConfirm(false)}
              data-testid="receive-tracking-back"
            >
              {t("purchasing.backToReceipt")}
            </Button>
            <Button
              type="button"
              disabled={!canReceive || busy || statusLocked}
              onClick={() => void postReceive(true)}
              data-testid="receive-confirm"
            >
              {busy ? t("purchasing.receiving") : t("purchasing.confirmReceipt")}
            </Button>
          </div>
        </Card>
      ) : (
        <>
          <ExitsTableContainer data-testid="receive-lines-table">
            <ExitsTableToolbar
              search={
                <SearchField
                  label={t("purchasing.searchReceiveLines")}
                  value={searchInput}
                  placeholder={t("purchasing.searchReceiveLines")}
                  onChange={(e) => setSearchInput(e.target.value)}
                  onClear={() => setSearchInput("")}
                  data-testid="receive-lines-search"
                />
              }
              filter={
                <ExitsChipBar
                  variant="filter"
                  ariaLabel={t("purchasing.receiveLineFilter")}
                  testId="receive-lines-filter"
                  items={[
                    {
                      key: "outstanding",
                      label: t("purchasing.receiveFilterOutstanding"),
                      state: lineFilter === "outstanding" ? "active" : "idle",
                      onSelect: () => setLineFilter("outstanding"),
                    },
                    {
                      key: "all",
                      label: t("purchasing.receiveFilterAll"),
                      state: lineFilter === "all" ? "active" : "idle",
                      onSelect: () => setLineFilter("all"),
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
                title={
                  debouncedSearch || lineFilter === "outstanding"
                    ? t("purchasing.receiveLinesNoMatch")
                    : t("purchasing.receiveLinesEmpty")
                }
                detail={
                  debouncedSearch || lineFilter === "outstanding"
                    ? t("purchasing.receiveLinesNoMatchDetail")
                    : t("purchasing.receiveLinesEmptyDetail")
                }
                testId="receive-lines-empty"
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
                        sortDirection={sortKey === "ordered" ? sortDirection : null}
                        onSort={() => onSort("ordered")}
                      >
                        {t("purchasing.ordered")}
                      </ExitsTableHead>
                      <ExitsTableHead
                        cellAlign="numeric"
                        sortable
                        sortDirection={sortKey === "received" ? sortDirection : null}
                        onSort={() => onSort("received")}
                      >
                        {t("purchasing.received")}
                      </ExitsTableHead>
                      <ExitsTableHead
                        cellAlign="numeric"
                        sortable
                        sortDirection={sortKey === "outstanding" ? sortDirection : null}
                        onSort={() => onSort("outstanding")}
                      >
                        {t("purchasing.outstanding")}
                      </ExitsTableHead>
                      <ExitsTableHead
                        cellAlign="numeric"
                        sortable
                        sortDirection={sortKey === "unitCost" ? sortDirection : null}
                        onSort={() => onSort("unitCost")}
                      >
                        {t("purchasing.unitPurchaseCost")}
                      </ExitsTableHead>
                      <ExitsTableHead cellAlign="text">{t("purchasing.goodReceived")}</ExitsTableHead>
                      <ExitsTableHead cellAlign="text">{t("purchasing.damaged")}</ExitsTableHead>
                      <ExitsTableHead cellAlign="text">{t("purchasing.closeAsShort")}</ExitsTableHead>
                    </ExitsTableRow>
                  </ExitsTableHeader>
                  <ExitsTableBody>
                    {pagedLines.map((line) => {
                      const goodQty = parseNonNegativeQty(line.goodText) ?? 0;
                      const showExpiry = line.tracksExpiration && goodQty > 0;
                      const editable = !reviewing && canReceive && line.outstandingQty > 0;
                      return (
                        <ExitsTableRow
                          key={line.productId}
                          data-testid={`receive-line-${line.productId}`}
                        >
                          <ExitsTableCell cellAlign="text" className="font-medium">
                            <div>{line.name}</div>
                            <div className="text-[length:var(--exits-text-xs)] text-muted">
                              {line.uom}
                              {showExpiry ? ` · ${t("purchasing.expiryDate")}` : ""}
                            </div>
                            {editable && showExpiry ? (
                              <div className="mt-2 flex min-w-[12rem] flex-col gap-1.5">
                                <input
                                  type="date"
                                  className="exits-input"
                                  value={line.expiryDate}
                                  onChange={(e) =>
                                    updateLine(line.productId, { expiryDate: e.target.value })
                                  }
                                  aria-label={t("purchasing.expiryDate")}
                                  data-testid={`receive-expiry-${line.productId}`}
                                />
                                <input
                                  className="exits-input"
                                  value={line.lotNumber}
                                  onChange={(e) =>
                                    updateLine(line.productId, { lotNumber: e.target.value })
                                  }
                                  placeholder={t("purchasing.lotNumber")}
                                  aria-label={t("purchasing.lotNumber")}
                                  data-testid={`receive-lot-${line.productId}`}
                                />
                              </div>
                            ) : null}
                            {reviewing && showExpiry ? (
                              <div className="mt-1 text-[length:var(--exits-text-xs)] text-muted">
                                {line.expiryDate || "—"}
                                {line.lotNumber.trim() ? ` · ${line.lotNumber}` : ""}
                              </div>
                            ) : null}
                          </ExitsTableCell>
                          <ExitsTableCell cellAlign="numeric" className="tabular-nums">
                            {line.orderedQty}
                          </ExitsTableCell>
                          <ExitsTableCell cellAlign="numeric" className="tabular-nums">
                            {line.receivedQty}
                          </ExitsTableCell>
                          <ExitsTableCell cellAlign="numeric" className="tabular-nums">
                            {line.outstandingQty}
                          </ExitsTableCell>
                          <ExitsTableCell
                            cellAlign="numeric"
                            data-testid={`receive-line-unit-cost-${line.productId}`}
                          >
                            <MoneyDisplay amount={line.unitPurchaseCost} />
                          </ExitsTableCell>
                          <ExitsTableCell cellAlign="text">
                            {editable ? (
                              <input
                                className="exits-input receive-qty-input"
                                value={line.goodText}
                                onChange={(e) =>
                                  updateLine(line.productId, { goodText: e.target.value })
                                }
                                inputMode="decimal"
                                aria-label={t("purchasing.goodReceived")}
                                data-testid={`receive-good-${line.productId}`}
                              />
                            ) : (
                              <span className="tabular-nums">
                                {line.goodText || "0"} {line.uom}
                              </span>
                            )}
                          </ExitsTableCell>
                          <ExitsTableCell cellAlign="text">
                            {editable ? (
                              <input
                                className="exits-input receive-qty-input"
                                value={line.damagedText}
                                onChange={(e) =>
                                  updateLine(line.productId, { damagedText: e.target.value })
                                }
                                inputMode="decimal"
                                aria-label={t("purchasing.damaged")}
                                data-testid={`receive-damaged-${line.productId}`}
                              />
                            ) : (
                              <span className="tabular-nums">{line.damagedText || "0"}</span>
                            )}
                          </ExitsTableCell>
                          <ExitsTableCell cellAlign="text">
                            {editable ? (
                              <label className="flex items-start gap-2 text-[length:var(--exits-text-sm)]">
                                <input
                                  type="checkbox"
                                  className="mt-1"
                                  checked={line.closeRemaining}
                                  onChange={(e) =>
                                    updateLine(line.productId, {
                                      closeRemaining: e.target.checked,
                                    })
                                  }
                                  data-testid={`receive-short-${line.productId}`}
                                />
                                <span className="text-muted">{t("purchasing.closeRemainingHelp")}</span>
                              </label>
                            ) : (
                              <span>{line.closeRemaining ? t("purchasing.yes") : t("purchasing.no")}</span>
                            )}
                          </ExitsTableCell>
                        </ExitsTableRow>
                      );
                    })}
                  </ExitsTableBody>
                  <ExitsTableFooter>
                    <ExitsTableRow>
                      <ExitsTableCell cellAlign="text" colSpan={4}>
                        {t("purchasing.receiptTotal")}
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="numeric" colSpan={4}>
                        <span className="font-semibold tabular-nums" data-testid="receive-estimated-total">
                          <MoneyDisplay amount={estimatedTotal} />
                        </span>
                      </ExitsTableCell>
                    </ExitsTableRow>
                  </ExitsTableFooter>
                </ExitsTable>

                <ExitsTableMobile data-testid="receive-lines-mobile">
                  {pagedLines.map((line) => {
                    const goodQty = parseNonNegativeQty(line.goodText) ?? 0;
                    const showExpiry = line.tracksExpiration && goodQty > 0;
                    const editable = !reviewing && canReceive && line.outstandingQty > 0;
                    return (
                      <ExitsTableMobileRow
                        key={line.productId}
                        data-testid={`receive-line-mobile-${line.productId}`}
                      >
                        <div className="exits-table-mobile__title-row">
                          <p className="exits-table-mobile__title">{line.name}</p>
                          <MoneyDisplay amount={line.unitPurchaseCost} />
                        </div>
                        <p className="exits-table-mobile__meta">
                          {t("purchasing.ordered")}: {line.orderedQty} · {t("purchasing.received")}:{" "}
                          {line.receivedQty} · {t("purchasing.outstanding")}: {line.outstandingQty}{" "}
                          {line.uom}
                        </p>
                        {editable ? (
                          <div className="mt-2 grid gap-2">
                            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                              {t("purchasing.goodReceived")}
                              <input
                                className="exits-input"
                                value={line.goodText}
                                onChange={(e) =>
                                  updateLine(line.productId, { goodText: e.target.value })
                                }
                                data-testid={`receive-good-mobile-${line.productId}`}
                              />
                            </label>
                            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                              {t("purchasing.damaged")}
                              <input
                                className="exits-input"
                                value={line.damagedText}
                                onChange={(e) =>
                                  updateLine(line.productId, { damagedText: e.target.value })
                                }
                                data-testid={`receive-damaged-mobile-${line.productId}`}
                              />
                            </label>
                            {showExpiry ? (
                              <>
                                <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                                  {t("purchasing.expiryDate")} *
                                  <input
                                    type="date"
                                    className="exits-input"
                                    value={line.expiryDate}
                                    onChange={(e) =>
                                      updateLine(line.productId, { expiryDate: e.target.value })
                                    }
                                    data-testid={`receive-expiry-mobile-${line.productId}`}
                                  />
                                </label>
                                <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                                  {t("purchasing.lotNumber")}
                                  <input
                                    className="exits-input"
                                    value={line.lotNumber}
                                    onChange={(e) =>
                                      updateLine(line.productId, { lotNumber: e.target.value })
                                    }
                                    data-testid={`receive-lot-mobile-${line.productId}`}
                                  />
                                </label>
                              </>
                            ) : null}
                            <label className="flex items-start gap-2 text-[length:var(--exits-text-sm)]">
                              <input
                                type="checkbox"
                                className="mt-1"
                                checked={line.closeRemaining}
                                onChange={(e) =>
                                  updateLine(line.productId, {
                                    closeRemaining: e.target.checked,
                                  })
                                }
                                data-testid={`receive-short-mobile-${line.productId}`}
                              />
                              <span>
                                <strong>{t("purchasing.closeAsShort")}</strong>
                                <br />
                                <span className="text-muted">{t("purchasing.closeRemainingHelp")}</span>
                              </span>
                            </label>
                          </div>
                        ) : (
                          <p className="exits-table-mobile__math">
                            {t("purchasing.goodReceived")}: {line.goodText || "0"} ·{" "}
                            {t("purchasing.damaged")}: {line.damagedText || "0"}
                          </p>
                        )}
                      </ExitsTableMobileRow>
                    );
                  })}
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

          {!reviewing && canReceive ? (
            <div className="grid gap-2">
              <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                {t("purchasing.deliveryReference")}
                <input
                  className="exits-input"
                  value={deliveryReference}
                  onChange={(e) => setDeliveryReference(e.target.value)}
                />
              </label>
              <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                {t("purchasing.receiveNotes")}
                <textarea
                  className="exits-input receive-stock-notes"
                  rows={2}
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                />
              </label>
              <ReceivePaymentSection
                estimatedTotal={estimatedTotal}
                mode={paymentMode}
                onModeChange={onPaymentModeChange}
                paidNowText={paidNowText}
                onPaidNowChange={(value) => {
                  setPaidNowTouched(true);
                  setPaidNowText(value);
                }}
                paymentMethod={paymentMethod}
                onPaymentMethodChange={setPaymentMethod}
                dueDate={dueDate}
                onDueDateChange={setDueDate}
                paidNowValue={
                  paymentMode === "paidInFull" ? estimatedTotal : paidNowValue
                }
              />
            </div>
          ) : null}

          {reviewing ? (
            <ReceivePaymentSection
              estimatedTotal={estimatedTotal}
              mode={paymentMode}
              onModeChange={onPaymentModeChange}
              paidNowText={paidNowText}
              onPaidNowChange={(value) => {
                setPaidNowTouched(true);
                setPaidNowText(value);
              }}
              paymentMethod={paymentMethod}
              onPaymentMethodChange={setPaymentMethod}
              dueDate={dueDate}
              onDueDateChange={setDueDate}
              paidNowValue={
                paymentMode === "paidInFull" ? estimatedTotal : paidNowValue
              }
              disabled={busy || statusLocked}
            />
          ) : null}

          <div className="receive-stock-actions">
            {reviewing ? (
              <>
                <Button
                  type="button"
                  variant="ghost"
                  onClick={() => setReviewing(false)}
                >
                  {t("purchasing.backToReceipt")}
                </Button>
                <Button
                  type="button"
                  disabled={!canReceive || busy || statusLocked}
                  onClick={onReviewConfirmClick}
                  data-testid="receive-confirm"
                >
                  {busy ? t("purchasing.receiving") : t("purchasing.confirmReceipt")}
                </Button>
              </>
            ) : (
              <Button
                type="button"
                disabled={!canReceive || busy || hasMissingExpiryOnReceive}
                onClick={onReview}
                data-testid="receive-review"
              >
                {t("purchasing.reviewReceipt")}
              </Button>
            )}
          </div>
        </>
      )}
    </div>
  );
}
