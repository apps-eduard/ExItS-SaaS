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
  submitPurchaseOrder,
} from "@/api/pos/pos-purchase-orders-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import { resolveAmbiguousMutationOutcome } from "@/runtime/ambiguous-mutation-outcome";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

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

  const po = query.data;
  const displayStatus = po?.displayStatus || po?.status || "";
  const needsApproval = displayStatus === "ChangesNeedApproval";
  const canSubmit = allowManage && online && po?.status === "Draft";
  const canCancel =
    allowManage && online && (po?.status === "Draft" || po?.canWithdrawConnected === true);
  const canReceive =
    allowManage && online && po != null && isPurchaseOrderReceivable(po) && !needsApproval;
  const canAcceptChanges = allowManage && online && needsApproval;

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
      </dl>

      <section aria-labelledby="po-lines">
        <h2 id="po-lines" className="m-0 mb-2 text-[length:var(--exits-text-md)] font-medium">
          {t("purchasing.lines")}
        </h2>
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {po.lines.map((line) => (
            <li key={line.lineId} className="rounded-md border border-border p-3">
              <div className="font-medium">{line.nameSnapshot ?? line.productId}</div>
              <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("purchasing.ordered")}: {line.orderedQty} · {t("purchasing.received")}:{" "}
                {line.receivedQty} · {t("purchasing.outstanding")}: {line.outstandingQty}
              </p>
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
