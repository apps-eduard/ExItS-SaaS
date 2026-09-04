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
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
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
import { buildReceivePlan, parseNonNegativeQty } from "@/features/purchasing/receive-math";
import { selectUntrackedReceivingLines } from "@/features/purchasing/receive-tracking";
import { useI18n } from "@/i18n/I18nProvider";
import { createSecureMutationId } from "@/lib/secure-mutation-id";
import { resolveAmbiguousMutationOutcome } from "@/runtime/ambiguous-mutation-outcome";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

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

export function PurchaseOrderReceivePage() {
  const { t } = useI18n();
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
  const goodsReceiptIdRef = useRef<string | null>(null);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const branchName = boundWorkspace?.branchName?.trim() || boundWorkspace?.branchId || "";

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

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="purchase-order-receive-page">
      <PageHeader
        title={t("purchasing.receiveTitle")}
        description={po.poNumber ?? t("purchasing.receiveSubtitle")}
        backTo={`/purchasing/${purchaseOrderId}`}
        backLabel={t("purchasing.backDetail")}
        backTestId="page-header-back-purchasing"
      />
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("purchasing.receiptsStockNote")}
      </p>
      {!online ? (
        <Card>
          <p className="m-0">{t("purchasing.offline")}</p>
        </Card>
      ) : null}
      {po.canReceiveConnected === false ? (
        <Card data-testid="receive-connected-gate">
          <p className="m-0">{t("purchasing.connectedReceiveBlocked")}</p>
        </Card>
      ) : null}
      {error ? (
        <Card data-testid="receive-error">
          <p className="m-0 text-destructive">{error}</p>
        </Card>
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
          <ul className="m-0 flex list-none flex-col gap-3 p-0">
            {lines.map((line) => {
              const goodQty = parseNonNegativeQty(line.goodText) ?? 0;
              const showExpiry = line.tracksExpiration && goodQty > 0;
              return (
                <li
                  key={line.productId}
                  className="rounded-md border border-border p-3"
                  data-testid={`receive-line-${line.productId}`}
                >
                  <div className="font-medium">{line.name}</div>
                  <p className="mt-1 mb-2 text-[length:var(--exits-text-sm)] text-muted">
                    {t("purchasing.ordered")}: {line.orderedQty} · {t("purchasing.received")}:{" "}
                    {line.receivedQty} · {t("purchasing.outstanding")}: {line.outstandingQty}{" "}
                    {line.uom}
                  </p>
                  <p
                    className="mt-0 mb-2 flex flex-wrap items-baseline gap-1 text-[length:var(--exits-text-sm)]"
                    data-testid={`receive-line-unit-cost-${line.productId}`}
                  >
                    <span className="text-muted">{t("purchasing.unitPurchaseCost")}:</span>
                    <MoneyDisplay amount={line.unitPurchaseCost} />
                    {line.uom ? <span className="text-muted">/ {line.uom}</span> : null}
                  </p>
                  {reviewing ? (
                    <dl className="m-0 grid grid-cols-2 gap-1 text-[length:var(--exits-text-sm)]">
                      <div>
                        <dt className="text-muted">{t("purchasing.goodReceived")}</dt>
                        <dd className="m-0">
                          {line.goodText || "0"} {line.uom}
                        </dd>
                      </div>
                      <div>
                        <dt className="text-muted">{t("purchasing.damaged")}</dt>
                        <dd className="m-0">{line.damagedText || "0"}</dd>
                      </div>
                      {showExpiry ? (
                        <>
                          <div>
                            <dt className="text-muted">{t("purchasing.expiryDate")}</dt>
                            <dd className="m-0">{line.expiryDate || "—"}</dd>
                          </div>
                          <div>
                            <dt className="text-muted">{t("purchasing.lotNumber")}</dt>
                            <dd className="m-0">{line.lotNumber.trim() || "—"}</dd>
                          </div>
                        </>
                      ) : null}
                    </dl>
                  ) : canReceive && line.outstandingQty > 0 ? (
                    <div className="grid gap-2 sm:grid-cols-2">
                      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                        {t("purchasing.goodReceived")}
                        <input
                          className="rounded-md border border-border bg-background px-3"
                          value={line.goodText}
                          onChange={(e) => updateLine(line.productId, { goodText: e.target.value })}
                          data-testid={`receive-good-${line.productId}`}
                        />
                      </label>
                      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                        {t("purchasing.damaged")}
                        <input
                          className="rounded-md border border-border bg-background px-3"
                          value={line.damagedText}
                          onChange={(e) =>
                            updateLine(line.productId, { damagedText: e.target.value })
                          }
                          data-testid={`receive-damaged-${line.productId}`}
                        />
                      </label>
                      {showExpiry ? (
                        <>
                          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                            {t("purchasing.expiryDate")} *
                            <input
                              type="date"
                              className="rounded-md border border-border bg-background px-3"
                              value={line.expiryDate}
                              onChange={(e) =>
                                updateLine(line.productId, { expiryDate: e.target.value })
                              }
                              data-testid={`receive-expiry-${line.productId}`}
                            />
                          </label>
                          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                            {t("purchasing.lotNumber")}
                            <input
                              className="rounded-md border border-border bg-background px-3"
                              value={line.lotNumber}
                              onChange={(e) =>
                                updateLine(line.productId, { lotNumber: e.target.value })
                              }
                              data-testid={`receive-lot-${line.productId}`}
                            />
                          </label>
                          <p className="col-span-full m-0 text-[length:var(--exits-text-sm)] text-muted">
                            {t("purchasing.receiveExpiryHelper")}
                          </p>
                        </>
                      ) : null}
                      <label className="col-span-full flex items-start gap-2 text-[length:var(--exits-text-sm)]">
                        <input
                          type="checkbox"
                          className="mt-1"
                          checked={line.closeRemaining}
                          onChange={(e) =>
                            updateLine(line.productId, { closeRemaining: e.target.checked })
                          }
                          data-testid={`receive-short-${line.productId}`}
                        />
                        <span>
                          <strong>{t("purchasing.closeAsShort")}</strong>
                          <br />
                          <span className="text-muted">{t("purchasing.closeRemainingHelp")}</span>
                        </span>
                      </label>
                    </div>
                  ) : null}
                </li>
              );
            })}
          </ul>

          {!reviewing && canReceive ? (
            <div className="grid gap-2">
              <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                {t("purchasing.deliveryReference")}
                <input
                  className="rounded-md border border-border bg-background px-3"
                  value={deliveryReference}
                  onChange={(e) => setDeliveryReference(e.target.value)}
                />
              </label>
              <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                {t("purchasing.receiveNotes")}
                <textarea
                  className="min-h-20 rounded-md border border-border bg-background px-3 py-2"
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

          <div className="flex flex-wrap gap-2">
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
                disabled={!canReceive || busy}
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
