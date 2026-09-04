import { useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { canManagePurchasing } from "@/access/pos-capabilities";
import { PosApiError } from "@/api/pos/pos-http";
import {
  acceptConnectedPurchaseOrderChanges,
  cancelPurchaseOrder,
  getPurchaseOrder,
  isPurchaseOrderReceivable,
  listGoodsReceiptsForPurchaseOrder,
  submitPurchaseOrder,
  voidGoodsReceipt,
  type PosGoodsReceiptDto,
  type PosPurchaseOrderDto,
} from "@/api/pos/pos-purchase-orders-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { ActorAttribution } from "@/features/actors/ActorAttribution";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import {
  sumGoodsReceiptValue,
  sumPurchaseOrderLineTotals,
} from "@/features/purchasing/purchase-cost-display";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { receiptReverseErrorMessage } from "@/features/purchasing/receive-payment";
import { useI18n } from "@/i18n/I18nProvider";
import { resolveAmbiguousMutationOutcome } from "@/runtime/ambiguous-mutation-outcome";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const RECEIPT_VOID_REASON_MAX = 512;

function resolveOrderTotal(po: PosPurchaseOrderDto): {
  amount: number;
  labelKey: "purchasing.orderTotal" | "purchasing.confirmedTotal" | "purchasing.proposedTotal";
} {
  if (po.confirmedTotalAmount != null) {
    return { amount: po.confirmedTotalAmount, labelKey: "purchasing.confirmedTotal" };
  }
  if (po.proposedTotalAmount != null && po.displayStatus === "ChangesNeedApproval") {
    return { amount: po.proposedTotalAmount, labelKey: "purchasing.proposedTotal" };
  }
  return {
    amount: sumPurchaseOrderLineTotals(po.lines),
    labelKey: "purchasing.orderTotal",
  };
}

function GoodsReceiptCard({
  receipt,
  workspace,
  resolveActor,
  isResolving,
  allowManage,
  online,
  onReversed,
}: {
  receipt: PosGoodsReceiptDto;
  workspace: { organizationId: string; branchId: string };
  resolveActor: ReturnType<typeof useActorDirectory>["resolve"];
  isResolving: boolean;
  allowManage: boolean;
  online: boolean;
  onReversed: (updated: PosGoodsReceiptDto) => Promise<void>;
}) {
  const { t } = useI18n();
  const receiptValue = sumGoodsReceiptValue(receipt.lines);
  const delivery = receipt.deliveryReference?.trim();
  const notes = receipt.notes?.trim();
  const isPosted = (receipt.status ?? "Posted") === "Posted";
  const isVoided = receipt.status === "Voided";
  const [voidOpen, setVoidOpen] = useState(false);
  const [voidReason, setVoidReason] = useState("");
  const [voiding, setVoiding] = useState(false);
  const [voidError, setVoidError] = useState<string | null>(null);

  async function onVoid() {
    const reason = voidReason.trim();
    if (!allowManage || !online || voiding || !isPosted) {
      return;
    }
    if (!reason) {
      setVoidError(t("purchasing.reverseReasonRequired"));
      return;
    }
    setVoiding(true);
    setVoidError(null);
    try {
      const updated = await voidGoodsReceipt(workspace, receipt.goodsReceiptId, { reason });
      await onReversed(updated);
      setVoidOpen(false);
      setVoidReason("");
    } catch (err) {
      setVoidError(
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
    <Card className="flex flex-col gap-3 p-3" data-testid={`po-receipt-${receipt.grnNumber}`}>
      <div className="flex flex-wrap items-center gap-2">
        <p className="m-0 font-medium">{receipt.grnNumber}</p>
        <StatusChip tone={isVoided ? "danger" : "success"}>
          {isVoided ? t("purchasing.receiptStatus.voided") : t("purchasing.receiptStatus.posted")}
        </StatusChip>
      </div>
      <ActorAttribution
        labelKey="common.receivedBy"
        actorId={receipt.receivedBy}
        occurredAtUtc={receipt.receivedAtUtc}
        resolved={resolveActor(receipt.receivedBy)}
        isLoading={isResolving}
        testId={`po-receipt-received-by-${receipt.goodsReceiptId}`}
      />
      {isVoided && receipt.voidedByUserId ? (
        <ActorAttribution
          labelKey="purchasing.reversedBy"
          actorId={receipt.voidedByUserId}
          occurredAtUtc={receipt.voidedAtUtc}
          resolved={resolveActor(receipt.voidedByUserId)}
          isLoading={isResolving}
          testId={`po-receipt-reversed-by-${receipt.goodsReceiptId}`}
        />
      ) : null}
      {isVoided && receipt.voidReason ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid={`po-receipt-void-reason-${receipt.goodsReceiptId}`}>
          {t("purchasing.reverseReason")}: {receipt.voidReason}
        </p>
      ) : null}
      {delivery ? (
        <div>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("purchasing.deliveryReference")}
          </p>
          <p className="mt-0.5 mb-0 text-[length:var(--exits-text-sm)]">{delivery}</p>
        </div>
      ) : null}
      <div className="flex flex-wrap items-baseline justify-between gap-2 text-[length:var(--exits-text-sm)]">
        <span className="text-muted">{t("purchasing.receiptValue")}</span>
        <MoneyDisplay amount={receiptValue} testId={`po-receipt-value-${receipt.goodsReceiptId}`} />
      </div>
      <ul className="m-0 flex list-none flex-col gap-3 border-t border-border pt-3 p-0">
        {receipt.lines.map((line) => {
          const goodQty = line.quantityReceived;
          const damaged = line.damagedQty ?? 0;
          const rejected = line.rejectedQty ?? 0;
          const shortClosed = line.shortClosedQty ?? 0;
          const discrepancyNote = line.discrepancyNote?.trim();
          const showExpiryLot = Boolean(line.expiryDate) || Boolean(line.lotNumber);
          return (
            <li
              key={line.lineId}
              className="text-[length:var(--exits-text-sm)]"
              data-testid={`po-receipt-line-${line.lineId}`}
            >
              <p className="m-0 font-medium">{line.nameSnapshot}</p>
              <p className="mt-1 mb-0 text-muted">
                {t("purchasing.receivedGood")}: {goodQty} {line.uomSnapshot}
              </p>
              <p className="mt-1 mb-0 flex flex-wrap items-baseline justify-between gap-2">
                <span className="text-muted">{t("purchasing.unitPurchaseCost")}</span>
                <span>
                  <MoneyDisplay amount={line.unitPurchaseCostSnapshot} />
                  <span className="text-muted"> / {line.uomSnapshot}</span>
                </span>
              </p>
              <p className="mt-1 mb-0 flex flex-wrap items-baseline justify-between gap-2">
                <span className="text-muted">{t("purchasing.lineTotal")}</span>
                <MoneyDisplay amount={line.lineTotalSnapshot} />
              </p>
              {showExpiryLot ? (
                <>
                  <p className="mt-1 mb-0 flex flex-wrap justify-between gap-2">
                    <span className="text-muted">{t("purchasing.expiryDate")}</span>
                    <span>{line.expiryDate ?? "—"}</span>
                  </p>
                  <p className="mt-1 mb-0 flex flex-wrap justify-between gap-2">
                    <span className="text-muted">{t("purchasing.lotNumber")}</span>
                    <span>{line.lotNumber?.trim() || "—"}</span>
                  </p>
                </>
              ) : null}
              {damaged > 0 ? (
                <p className="mt-1 mb-0 text-muted">
                  {t("purchasing.damaged")}: {damaged} {line.uomSnapshot}
                </p>
              ) : null}
              {rejected > 0 ? (
                <p className="mt-1 mb-0 text-muted">
                  {t("purchasing.rejected")}: {rejected} {line.uomSnapshot}
                </p>
              ) : null}
              {shortClosed > 0 ? (
                <p className="mt-1 mb-0 text-muted">
                  {t("purchasing.shortClosed")}: {shortClosed} {line.uomSnapshot}
                </p>
              ) : null}
              {line.discrepancyKind && line.discrepancyKind !== "None" ? (
                <p className="mt-1 mb-0 text-muted">
                  {t("purchasing.discrepancy")}: {line.discrepancyKind}
                </p>
              ) : null}
              {discrepancyNote ? (
                <p className="mt-1 mb-0 text-muted">
                  {t("purchasing.discrepancyNote")}: {discrepancyNote}
                </p>
              ) : null}
            </li>
          );
        })}
      </ul>
      {notes ? (
        <p className="m-0 border-t border-border pt-2 text-[length:var(--exits-text-sm)] text-muted">
          {t("purchasing.notes")}: {notes}
        </p>
      ) : null}

      {voidError ? (
        <ErrorState title={t("purchasing.errorTitle")} detail={voidError} />
      ) : null}

      {allowManage && isPosted && online ? (
        <Button
          type="button"
          variant="outline"
          className="w-fit"
          onClick={() => {
            setVoidOpen(true);
            setVoidError(null);
          }}
          data-testid={`po-receipt-reverse-${receipt.goodsReceiptId}`}
        >
          {t("purchasing.reverseReceipt")}
        </Button>
      ) : null}

      {voidOpen ? (
        <div
          className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center"
          role="dialog"
          aria-modal="true"
          aria-labelledby={`po-receipt-reverse-title-${receipt.goodsReceiptId}`}
          data-testid={`po-receipt-reverse-dialog-${receipt.goodsReceiptId}`}
        >
          <Card className="flex w-full max-w-md flex-col gap-3 p-4">
            <h2
              id={`po-receipt-reverse-title-${receipt.goodsReceiptId}`}
              className="m-0 text-[length:var(--exits-text-lg)] font-semibold"
            >
              {t("purchasing.reverseTitle")}
            </h2>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("purchasing.reverseLede")}
            </p>
            <p className="m-0 text-[length:var(--exits-text-sm)]">
              {receipt.grnNumber} · {receipt.receivedDate}
            </p>
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              <span className="font-medium">{t("purchasing.reverseReason")}</span>
              <textarea
                className="min-h-24 rounded-[var(--exits-radius-md)] border border-border bg-background px-3 py-2"
                value={voidReason}
                maxLength={RECEIPT_VOID_REASON_MAX}
                onChange={(e) => setVoidReason(e.target.value)}
                data-testid={`po-receipt-reverse-reason-${receipt.goodsReceiptId}`}
              />
            </label>
            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                variant="destructive"
                disabled={voiding || !voidReason.trim()}
                onClick={() => void onVoid()}
                data-testid={`po-receipt-reverse-confirm-${receipt.goodsReceiptId}`}
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
                  setVoidError(null);
                }}
                data-testid={`po-receipt-reverse-cancel-${receipt.goodsReceiptId}`}
              >
                {t("purchasing.reverseCancel")}
              </Button>
            </div>
          </Card>
        </div>
      ) : null}
    </Card>
  );
}

export function PurchaseOrderDetailPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { purchaseOrderId } = useParams<{ purchaseOrderId: string }>();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const queryClient = useQueryClient();
  const allowManage = canManagePurchasing(sessionGrant);
  const [busy, setBusy] = useState(false);
  const [banner, setBanner] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: ["purchase-order", workspace?.organizationId, purchaseOrderId],
    enabled: Boolean(workspace) && Boolean(purchaseOrderId) && online,
    queryFn: ({ signal }) => getPurchaseOrder(workspace!, purchaseOrderId!, signal),
  });

  const receiptsQuery = useQuery({
    queryKey: ["purchase-order-receipts", workspace?.organizationId, purchaseOrderId],
    enabled: Boolean(workspace) && Boolean(purchaseOrderId) && online,
    queryFn: ({ signal }) =>
      listGoodsReceiptsForPurchaseOrder(workspace!, purchaseOrderId!, signal),
  });

  const po = query.data;
  const receipts = receiptsQuery.data ?? [];
  const actors = useActorDirectory(workspace?.organizationId, [
    po?.orderedBy,
    ...receipts.map((receipt) => receipt.receivedBy),
    ...receipts.map((receipt) => receipt.voidedByUserId),
  ]);
  const displayStatus = po?.displayStatus || po?.status || "";
  const needsApproval = displayStatus === "ChangesNeedApproval";
  const canSubmit = allowManage && online && po?.status === "Draft";
  const canCancel =
    allowManage && online && (po?.status === "Draft" || po?.canWithdrawConnected === true);
  const canReceive =
    allowManage && online && po != null && isPurchaseOrderReceivable(po) && !needsApproval;
  const needsProductSetup =
    allowManage &&
    online &&
    po != null &&
    (po.needsProductSetup === true || (po.productSetupRequiredCount ?? 0) > 0) &&
    (po.canReceiveConnected ?? true) &&
    !needsApproval &&
    (po.status === "Ordered" || po.status === "PartiallyReceived");
  const canAcceptChanges = allowManage && online && needsApproval;
  const orderTotal = po ? resolveOrderTotal(po) : null;

  async function runAction(
    action: () => Promise<unknown>,
    successKey: "purchasing.submitted" | "purchasing.cancelled" | "purchasing.changesAccepted",
    options?: { reconcile?: () => Promise<boolean> },
  ) {
    if (!workspace || !purchaseOrderId || busy) {
      return;
    }
    setBusy(true);
    setError(null);
    setBanner(null);
    try {
      await action();
      setBanner(t(successKey));
      await queryClient.invalidateQueries({
        queryKey: ["purchase-order", workspace.organizationId, purchaseOrderId],
      });
      await query.refetch();
    } catch (err) {
      if (options?.reconcile) {
        setError(t("checkout.confirmingTransaction"));
        const outcome = await resolveAmbiguousMutationOutcome({
          error: err,
          lookup: async () => {
            const ok = await options.reconcile!();
            if (!ok) {
              throw err;
            }
            return true;
          },
        });
        if (outcome.kind === "confirmed") {
          setError(null);
          setBanner(t(successKey));
          await queryClient.invalidateQueries({
            queryKey: ["purchase-order", workspace.organizationId, purchaseOrderId],
          });
          await query.refetch();
          return;
        }
        if (outcome.kind === "still_unknown") {
          setError(t("checkout.transactionStatusUnknown"));
          return;
        }
      }
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("purchasing.actionFailed"))
          : t("purchasing.actionFailed"),
      );
    } finally {
      setBusy(false);
    }
  }

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }
  if (!purchaseOrderId) {
    return <ErrorState title={t("purchasing.errorTitle")} detail={t("purchasing.notFound")} />;
  }
  if (query.isLoading) {
    return <LoadingState label={t("purchasing.loading")} />;
  }
  if (query.isError || !po) {
    return <ErrorState title={t("purchasing.errorTitle")} detail={t("purchasing.notFound")} />;
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="purchase-order-detail-page">
      <PageHeader
        title={po.poNumber ?? t("purchasing.detailTitle")}
        description={po.supplierName ?? t("purchasing.unknownSupplier")}
        backTo="/purchasing/orders"
        backLabel={t("purchasing.backOrders")}
        backTestId="page-header-back-purchasing"
      />
      <div className="flex flex-wrap items-center gap-2">
        <StatusChip tone="info">{displayStatus || po.status}</StatusChip>
        <span className="text-[length:var(--exits-text-sm)] text-muted">
          {t("purchasing.paymentTerm")}: {po.paymentTermLabel || po.paymentTerm || "Cash"}
        </span>
      </div>
      {!online ? (
        <Card>
          <p className="m-0">{t("purchasing.offline")}</p>
        </Card>
      ) : null}
      {needsApproval ? (
        <Card data-testid="po-needs-approval">
          <p className="m-0">{t("purchasing.changesNeedApproval")}</p>
        </Card>
      ) : null}
      {needsProductSetup ? (
        <Card className="p-3" data-testid="po-prepare-products-banner">
          <p className="m-0 font-medium">{t("purchasing.prepareProductsTitle")}</p>
          <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("purchasing.prepareProductsHelp").replace(
              "{count}",
              String(po?.productSetupRequiredCount ?? po?.lines.filter((l) => l.needsProductSetup).length ?? 0),
            )}
          </p>
          <Button asChild className="mt-3" data-testid="po-prepare-products">
            <Link to={`/purchasing/${purchaseOrderId}/prepare-products`}>
              {t("purchasing.prepareProductsAction")}
            </Link>
          </Button>
        </Card>
      ) : null}
      {canReceive && displayStatus === "Ready" ? (
        <Card data-testid="po-ready-receive">
          <p className="m-0">{t("purchasing.readyToReceive")}</p>
        </Card>
      ) : null}
      {po.canReceiveConnected === false ? (
        <Card data-testid="po-receive-gated">
          <p className="m-0">{t("purchasing.connectedReceiveBlocked")}</p>
        </Card>
      ) : null}
      {banner ? (
        <Card data-testid="po-banner">
          <p className="m-0">{banner}</p>
        </Card>
      ) : null}
      {error ? (
        <Card data-testid="po-detail-error">
          <p className="m-0 text-destructive">{error}</p>
        </Card>
      ) : null}

      <dl className="m-0 grid gap-2 sm:grid-cols-2">
        <div>
          <dt className="text-[length:var(--exits-text-sm)] text-muted">
            {t("purchasing.fieldStatus")}
          </dt>
          <dd className="m-0">{displayStatus || po.status}</dd>
        </div>
        <div>
          <dt className="text-[length:var(--exits-text-sm)] text-muted">
            {t("purchasing.fieldOrderDate")}
          </dt>
          <dd className="m-0">{po.orderDate}</dd>
        </div>
        <div>
          <dt className="text-[length:var(--exits-text-sm)] text-muted">
            {t("purchasing.fieldSupplier")}
          </dt>
          <dd className="m-0" data-testid="po-supplier-display">
            {po.supplierBranchName
              ? `${po.supplierName ?? t("purchasing.unknownSupplier")} — ${po.supplierBranchName}`
              : (po.supplierName ?? t("purchasing.unknownSupplier"))}
          </dd>
        </div>
        <div>
          <dt className="text-[length:var(--exits-text-sm)] text-muted">
            {t("purchasing.receivingAt")}
          </dt>
          <dd className="m-0" data-testid="po-receiving-branch">
            {boundWorkspace?.branchName ?? boundWorkspace?.branchId ?? "—"}
          </dd>
        </div>
        {orderTotal ? (
          <div>
            <dt className="text-[length:var(--exits-text-sm)] text-muted">
              {t(orderTotal.labelKey)}
            </dt>
            <dd className="m-0" data-testid="po-order-total">
              <MoneyDisplay amount={orderTotal.amount} />
            </dd>
          </div>
        ) : null}
      </dl>

      {po.orderedAtUtc || po.orderedBy ? (
        <ActorAttribution
          labelKey="common.orderedBy"
          actorId={po.orderedBy}
          occurredAtUtc={po.orderedAtUtc}
          resolved={actors.resolve(po.orderedBy)}
          isLoading={actors.isResolving}
          testId="po-ordered-by"
        />
      ) : null}

      <section aria-labelledby="po-lines">
        <h2 id="po-lines" className="m-0 mb-2 text-[length:var(--exits-text-md)] font-medium">
          {t("purchasing.lines")}
        </h2>
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {po.lines.map((line) => {
            const uom = line.uomSnapshot ?? "";
            return (
              <li key={line.lineId}>
                <Card className="flex flex-col gap-2 p-3" data-testid={`po-line-${line.lineId}`}>
                  <p className="m-0 font-medium">{line.nameSnapshot ?? line.productId}</p>
                  <dl className="m-0 grid gap-1 text-[length:var(--exits-text-sm)]">
                    <div className="flex flex-wrap justify-between gap-2">
                      <dt className="text-muted">{t("purchasing.ordered")}</dt>
                      <dd className="m-0">
                        {line.orderedQty} {uom}
                      </dd>
                    </div>
                    <div className="flex flex-wrap items-baseline justify-between gap-2">
                      <dt className="text-muted">{t("purchasing.unitPurchaseCost")}</dt>
                      <dd className="m-0">
                        <MoneyDisplay amount={line.unitPurchaseCost} />
                        {uom ? <span className="text-muted"> / {uom}</span> : null}
                      </dd>
                    </div>
                    <div className="flex flex-wrap items-baseline justify-between gap-2">
                      <dt className="text-muted">{t("purchasing.orderedValue")}</dt>
                      <dd className="m-0">
                        <MoneyDisplay amount={line.lineTotal} />
                      </dd>
                    </div>
                    <div className="flex flex-wrap justify-between gap-2">
                      <dt className="text-muted">{t("purchasing.received")}</dt>
                      <dd className="m-0">
                        {line.receivedQty} {uom}
                      </dd>
                    </div>
                    <div className="flex flex-wrap justify-between gap-2">
                      <dt className="text-muted">{t("purchasing.outstanding")}</dt>
                      <dd className="m-0">
                        {line.outstandingQty} {uom}
                      </dd>
                    </div>
                  </dl>
                </Card>
              </li>
            );
          })}
        </ul>
      </section>

      <section aria-labelledby="po-receipt-history" data-testid="po-receipt-history">
        <h2
          id="po-receipt-history"
          className="m-0 mb-2 text-[length:var(--exits-text-md)] font-medium"
        >
          {t("purchasing.receiptHistory")}
        </h2>
        {receiptsQuery.isLoading ? <LoadingState label={t("purchasing.loading")} /> : null}
        {!receiptsQuery.isLoading && receipts.length === 0 ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("purchasing.receiptHistoryEmpty")}
          </p>
        ) : null}
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {receipts.map((receipt) => (
            <li key={receipt.goodsReceiptId}>
              <GoodsReceiptCard
                receipt={receipt}
                workspace={workspace!}
                resolveActor={actors.resolve}
                isResolving={actors.isResolving}
                allowManage={allowManage}
                online={online}
                onReversed={async (updated) => {
                  queryClient.setQueryData(
                    ["purchase-order-receipts", workspace!.organizationId, purchaseOrderId],
                    (prev: PosGoodsReceiptDto[] | undefined) =>
                      (prev ?? []).map((r) =>
                        r.goodsReceiptId === updated.goodsReceiptId ? updated : r,
                      ),
                  );
                  await queryClient.invalidateQueries({
                    queryKey: ["purchase-order", workspace!.organizationId, purchaseOrderId],
                  });
                  await queryClient.invalidateQueries({
                    queryKey: ["purchase-order-receipts", workspace!.organizationId, purchaseOrderId],
                  });
                  await queryClient.invalidateQueries({ queryKey: ["inventory"] });
                }}
              />
            </li>
          ))}
        </ul>
      </section>

      <div className="flex flex-wrap gap-2">
        {canSubmit ? (
          <Button
            type="button"
            disabled={busy}
            onClick={() =>
              void runAction(
                () => submitPurchaseOrder(workspace, purchaseOrderId),
                "purchasing.submitted",
                {
                  reconcile: async () => {
                    const latest = await getPurchaseOrder(workspace, purchaseOrderId);
                    return latest.status.toLowerCase() === "ordered";
                  },
                },
              )
            }
            data-testid="po-submit"
          >
            {t("purchasing.submit")}
          </Button>
        ) : null}
        {canReceive ? (
          <Button asChild data-testid="po-receive">
            <Link to={`/purchasing/${purchaseOrderId}/receive`}>{t("purchasing.receive")}</Link>
          </Button>
        ) : null}
        {canAcceptChanges ? (
          <Button
            type="button"
            disabled={busy}
            onClick={() =>
              void runAction(
                () => acceptConnectedPurchaseOrderChanges(workspace, purchaseOrderId),
                "purchasing.changesAccepted",
              )
            }
            data-testid="po-accept-changes"
          >
            {t("purchasing.acceptChanges")}
          </Button>
        ) : null}
        {canCancel ? (
          <Button
            type="button"
            variant="ghost"
            disabled={busy}
            onClick={() =>
              void runAction(
                () => cancelPurchaseOrder(workspace, purchaseOrderId),
                "purchasing.cancelled",
              )
            }
            data-testid="po-cancel"
          >
            {t("purchasing.cancel")}
          </Button>
        ) : null}
      </div>
    </div>
  );
}
