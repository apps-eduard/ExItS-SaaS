import { useMemo, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canManagePurchasing } from "@/access/pos-capabilities";
import { PosApiError } from "@/api/pos/pos-http";
import {
  getGoodsReceipt,
  getPurchaseOrder,
  isPurchaseOrderReceivable,
  receivePurchaseOrder,
} from "@/api/pos/pos-purchase-orders-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { buildReceivePlan, parseNonNegativeQty } from "@/features/purchasing/receive-math";
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
  goodText: string;
  damagedText: string;
  closeRemaining: boolean;
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
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [statusLocked, setStatusLocked] = useState(false);
  const goodsReceiptIdRef = useRef<string | null>(null);

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
    queryFn: async ({ signal }) => {
      const po = await getPurchaseOrder(workspace!, purchaseOrderId!, signal);
      setLines(
        po.lines.map((line) => ({
          productId: line.productId,
          name: line.nameSnapshot ?? line.productId,
          uom: line.uomSnapshot ?? "",
          orderedQty: line.orderedQty,
          receivedQty: line.receivedQty,
          outstandingQty: line.outstandingQty,
          goodText: line.outstandingQty > 0 ? String(line.outstandingQty) : "",
          damagedText: "",
          closeRemaining: false,
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
    setReviewing(true);
  }

  async function onConfirm() {
    if (!workspace || !purchaseOrderId || !canReceive || busy || statusLocked) {
      return;
    }
    const planned = tryPlan();
    if (!planned) {
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
      await receivePurchaseOrder(workspace, purchaseOrderId, {
        goodsReceiptId,
        deliveryReference: deliveryReference.trim() || null,
        notes: notes.trim() || null,
        lines: planned.map((line) => ({
          productId: line.productId,
          receiveQty: line.receiveQty,
          damagedQty: line.damagedQty,
          shortClosedQty: line.shortClosedQty,
          discrepancyKind: line.discrepancyKind,
          discrepancyNote: line.discrepancyKind && notes.trim() ? notes.trim() : null,
        })),
      });
      goodsReceiptIdRef.current = null;
      navigate(`/purchasing/${purchaseOrderId}`, { replace: true });
    } catch (err) {
      setError(t("checkout.confirmingTransaction"));
      const outcome = await resolveAmbiguousMutationOutcome({
        error: err,
        lookup: () => getGoodsReceipt(workspace, goodsReceiptId),
      });
      if (outcome.kind === "confirmed") {
        goodsReceiptIdRef.current = null;
        navigate(`/purchasing/${purchaseOrderId}`, { replace: true });
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

      <ul className="m-0 flex list-none flex-col gap-3 p-0">
        {lines.map((line) => (
          <li
            key={line.productId}
            className="rounded-md border border-border p-3"
            data-testid={`receive-line-${line.productId}`}
          >
            <div className="font-medium">{line.name}</div>
            <p className="mt-1 mb-2 text-[length:var(--exits-text-sm)] text-muted">
              {t("purchasing.ordered")}: {line.orderedQty} · {t("purchasing.received")}:{" "}
              {line.receivedQty} · {t("purchasing.outstanding")}: {line.outstandingQty} {line.uom}
            </p>
            {reviewing ? (
              <dl className="m-0 grid grid-cols-2 gap-1 text-[length:var(--exits-text-sm)]">
                <div>
                  <dt className="text-muted">{t("purchasing.goodReceived")}</dt>
                  <dd className="m-0">{line.goodText || "0"}</dd>
                </div>
                <div>
                  <dt className="text-muted">{t("purchasing.damaged")}</dt>
                  <dd className="m-0">{line.damagedText || "0"}</dd>
                </div>
              </dl>
            ) : canReceive && line.outstandingQty > 0 ? (
              <div className="grid gap-2 sm:grid-cols-2">
                <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                  {t("purchasing.goodReceived")}
                  <input
                    className="min-h-11 rounded-md border border-border bg-background px-3"
                    value={line.goodText}
                    onChange={(e) => updateLine(line.productId, { goodText: e.target.value })}
                    data-testid={`receive-good-${line.productId}`}
                  />
                </label>
                <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                  {t("purchasing.damaged")}
                  <input
                    className="min-h-11 rounded-md border border-border bg-background px-3"
                    value={line.damagedText}
                    onChange={(e) => updateLine(line.productId, { damagedText: e.target.value })}
                    data-testid={`receive-damaged-${line.productId}`}
                  />
                </label>
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
        ))}
      </ul>

      {!reviewing && canReceive ? (
        <div className="grid gap-2">
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("purchasing.deliveryReference")}
            <input
              className="min-h-11 rounded-md border border-border bg-background px-3"
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
        </div>
      ) : null}

      <div className="flex flex-wrap gap-2">
        {reviewing ? (
          <>
            <Button
              type="button"
              variant="ghost"
              className="min-h-11"
              onClick={() => setReviewing(false)}
            >
              {t("purchasing.backToReceipt")}
            </Button>
            <Button
              type="button"
              className="min-h-11"
              disabled={!canReceive || busy || statusLocked}
              onClick={() => void onConfirm()}
              data-testid="receive-confirm"
            >
              {busy ? t("purchasing.receiving") : t("purchasing.confirmReceipt")}
            </Button>
          </>
        ) : (
          <Button
            type="button"
            className="min-h-11"
            disabled={!canReceive || busy}
            onClick={onReview}
            data-testid="receive-review"
          >
            {t("purchasing.reviewReceipt")}
          </Button>
        )}
      </div>
    </div>
  );
}
