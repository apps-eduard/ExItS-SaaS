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
import { useI18n } from "@/i18n/I18nProvider";
import { resolveAmbiguousMutationOutcome } from "@/runtime/ambiguous-mutation-outcome";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

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
  resolveActor,
  isResolving,
}: {
  receipt: PosGoodsReceiptDto;
  resolveActor: ReturnType<typeof useActorDirectory>["resolve"];
  isResolving: boolean;
}) {
  const { t } = useI18n();
  const receiptValue = sumGoodsReceiptValue(receipt.lines);
  const delivery = receipt.deliveryReference?.trim();
  const notes = receipt.notes?.trim();

  return (
    <Card className="flex flex-col gap-3 p-3" data-testid={`po-receipt-${receipt.grnNumber}`}>
      <p className="m-0 font-medium">{receipt.grnNumber}</p>
      <ActorAttribution
        labelKey="common.receivedBy"
        actorId={receipt.receivedBy}
        occurredAtUtc={receipt.receivedAtUtc}
        resolved={resolveActor(receipt.receivedBy)}
        isLoading={isResolving}
        testId={`po-receipt-received-by-${receipt.goodsReceiptId}`}
      />
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
  ]);
  const displayStatus = po?.displayStatus || po?.status || "";
  const needsApproval = displayStatus === "ChangesNeedApproval";
  const canSubmit = allowManage && online && po?.status === "Draft";
  const canCancel =
    allowManage && online && (po?.status === "Draft" || po?.canWithdrawConnected === true);
  const canReceive =
    allowManage && online && po != null && isPurchaseOrderReceivable(po) && !needsApproval;
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
          <dd className="m-0">{po.supplierName ?? t("purchasing.unknownSupplier")}</dd>
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
                resolveActor={actors.resolve}
                isResolving={actors.isResolving}
              />
            </li>
          ))}
        </ul>
      </section>

      <div className="flex flex-wrap gap-2">
        {canSubmit ? (
          <Button
            type="button"
            className="min-h-11"
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
          <Button asChild className="min-h-11" data-testid="po-receive">
            <Link to={`/purchasing/${purchaseOrderId}/receive`}>{t("purchasing.receive")}</Link>
          </Button>
        ) : null}
        {canAcceptChanges ? (
          <Button
            type="button"
            className="min-h-11"
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
            className="min-h-11"
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
