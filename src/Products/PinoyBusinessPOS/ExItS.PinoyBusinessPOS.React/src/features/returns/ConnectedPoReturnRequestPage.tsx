import { useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Undo2 } from "lucide-react";
import {
  getConnectedPoReturnEligibility,
  requestConnectedPoReturnBatch,
} from "@/api/pos/pos-connected-po-returns-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { describeReturnError } from "@/features/returns/return-errors";
import { describeConnectedPoReturnLineEligibility } from "@/features/returns/connected-po-return-eligibility";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function ConnectedPoReturnRequestPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { purchaseOrderId } = useParams<{ purchaseOrderId: string }>();
  const { boundWorkspace } = useWorkspace();

  const [quantities, setQuantities] = useState<Record<string, number>>({});
  const [reason, setReason] = useState("");
  const [notes, setNotes] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const workspace =
    boundWorkspace?.branchId && boundWorkspace.organizationId
      ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
      : null;

  const headerBack = {
    backTo: pageBackNav.returns.to,
    backLabel: t(pageBackNav.returns.labelKey),
    backTestId: "page-header-back-returns",
  };

  const eligibilityQuery = useQuery({
    queryKey: [
      "connected-po-return-eligibility",
      workspace?.organizationId,
      workspace?.branchId,
      purchaseOrderId,
    ],
    enabled: Boolean(workspace && purchaseOrderId),
    queryFn: ({ signal }) => getConnectedPoReturnEligibility(workspace!, purchaseOrderId!, signal),
  });

  const eligibility = eligibilityQuery.data;
  const allLines = eligibility?.lines ?? [];
  const returnableLines = useMemo(
    () => allLines.filter((line) => describeConnectedPoReturnLineEligibility(line).showReturnAction),
    [allLines],
  );
  const blockedLines = useMemo(
    () =>
      allLines.filter((line) => {
        const view = describeConnectedPoReturnLineEligibility(line);
        return view.status === "non_returnable" || view.status === "expired";
      }),
    [allLines],
  );

  const selected = useMemo(
    () =>
      returnableLines
        .map((line) => ({ line, quantity: quantities[line.purchaseOrderLineId] ?? 0 }))
        .filter((entry) => entry.quantity > 0),
    [quantities, returnableLines],
  );

  const estimatedValue = useMemo(
    () => selected.reduce((total, { line, quantity }) => total + quantity * line.unitPurchaseCost, 0),
    [selected],
  );

  function setQuantity(purchaseOrderLineId: string, max: number, raw: number) {
    const clamped = Number.isFinite(raw) ? Math.min(Math.max(raw, 0), max) : 0;
    setQuantities((prev) => ({ ...prev, [purchaseOrderLineId]: clamped }));
  }

  async function onSubmit() {
    if (!workspace || !purchaseOrderId || submitting) {
      return;
    }
    const trimmedReason = reason.trim();
    if (!trimmedReason || selected.length === 0) {
      setError(t("returns.reasonRequired"));
      return;
    }

    setSubmitting(true);
    setError(null);
    try {
      await requestConnectedPoReturnBatch(workspace, {
        purchaseOrderId,
        reason: trimmedReason,
        notes: notes.trim() || undefined,
        lines: selected.map(({ line, quantity }) => ({
          purchaseOrderLineId: line.purchaseOrderLineId,
          quantity,
        })),
      });
      await queryClient.invalidateQueries({ queryKey: ["connected-po-return-eligibility"] });
      await queryClient.invalidateQueries({ queryKey: ["connected-po-return-seller-inbox"] });
      navigate(`/purchasing/purchase-orders/${purchaseOrderId}`, { replace: true });
    } catch (err) {
      setError(describeReturnError(err, t));
    } finally {
      setSubmitting(false);
    }
  }

  if (!purchaseOrderId) {
    return (
      <div className="flex flex-col gap-3" data-testid="connected-po-return-missing">
        <PageHeader
          title={t("returns.connectedPo.requestTitle")}
          description={t("returns.connectedPo.loadError")}
          {...headerBack}
        />
      </div>
    );
  }

  if (!workspace || eligibilityQuery.isLoading) {
    return <LoadingSkeleton label={t("loading.label")} />;
  }

  if (eligibilityQuery.isError || !eligibility) {
    return (
      <div className="flex flex-col gap-3" data-testid="connected-po-return-error">
        <PageHeader
          title={t("returns.connectedPo.requestTitle")}
          description={t("returns.connectedPo.loadError")}
          {...headerBack}
        />
      </div>
    );
  }

  return (
    <div className="flex min-w-0 flex-col gap-3" data-testid="connected-po-return-request-page">
      <PageHeader
        title={t("returns.connectedPo.requestTitle")}
        description={`${t("returns.connectedPo.requestLede")} · ${eligibility.poNumber ?? ""}`}
        {...headerBack}
      />

      {!eligibility.canRequestReturn ? (
        <Notice tone="warning" testId="connected-po-return-blocked">
          {eligibility.blockedReason ?? t("returns.connectedPo.blocked")}
        </Notice>
      ) : null}

      {returnableLines.length === 0 ? (
        <EmptyState
          align="center"
          icon={<Undo2 className="size-5" strokeWidth={1.75} />}
          title={t("returns.connectedPo.noReturnableLines")}
          detail={t("returns.alreadyReturnedDetail")}
        />
      ) : (
        <ul className="m-0 flex list-none flex-col gap-3 p-0" data-testid="connected-po-return-lines">
          {returnableLines.map((line) => {
            const quantity = quantities[line.purchaseOrderLineId] ?? 0;
            const view = describeConnectedPoReturnLineEligibility(line);
            return (
              <li key={line.purchaseOrderLineId}>
                <Card className="p-3" data-testid={`connected-po-return-line-${line.purchaseOrderLineId}`}>
                  <div className="flex items-start justify-between gap-2">
                    <div className="min-w-0">
                      <p className="m-0 font-semibold">{line.productName}</p>
                      <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                        Return eligible · {view.availableQty} {line.unitOfMeasure} available
                        {view.expiresLabel ? ` · Until ${view.expiresLabel}` : ""}
                      </p>
                      <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                        {t("returns.connectedPo.receivedQty")}: {line.receivedQuantity}{" "}
                        {line.unitOfMeasure} · {t("returns.connectedPo.alreadyReturned")}:{" "}
                        {line.alreadyReturnedQuantity}
                      </p>
                    </div>
                    <MoneyDisplay amount={line.unitPurchaseCost} />
                  </div>

                  <div className="mt-3 flex flex-wrap items-end gap-3">
                    <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                      {t("returns.quantity")}
                      <input
                        type="number"
                        inputMode="decimal"
                        min={0}
                        max={line.returnableQuantity}
                        step="0.001"
                        value={quantity || ""}
                        data-testid={`connected-po-return-qty-${line.purchaseOrderLineId}`}
                        className="w-28 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
                        onChange={(event) =>
                          setQuantity(
                            line.purchaseOrderLineId,
                            line.returnableQuantity,
                            Number(event.target.value),
                          )
                        }
                      />
                    </label>
                    <Button
                      type="button"
                      variant="ghost"
                      disabled={quantity >= line.returnableQuantity}
                      data-testid={`connected-po-return-all-${line.purchaseOrderLineId}`}
                      onClick={() =>
                        setQuantity(
                          line.purchaseOrderLineId,
                          line.returnableQuantity,
                          line.returnableQuantity,
                        )
                      }
                    >
                      {t("returns.returnAll")}
                    </Button>
                  </div>
                </Card>
              </li>
            );
          })}
        </ul>
      )}

      {blockedLines.length > 0 ? (
        <ul className="m-0 flex list-none flex-col gap-2 p-0" data-testid="connected-po-return-blocked-lines">
          {blockedLines.map((line) => {
            const view = describeConnectedPoReturnLineEligibility(line);
            return (
              <li key={`blocked-${line.purchaseOrderLineId}`}>
                <Card className="p-3" data-testid={`connected-po-return-blocked-${line.purchaseOrderLineId}`}>
                  <p className="m-0 font-semibold">{line.productName}</p>
                  <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                    {view.status === "non_returnable"
                      ? "Non-returnable"
                      : view.expiresLabel
                        ? `Return period ended ${view.expiresLabel}`
                        : "Return period ended"}
                  </p>
                  {view.helper ? (
                    <p className="mb-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">{view.helper}</p>
                  ) : null}
                </Card>
              </li>
            );
          })}
        </ul>
      ) : null}

      <section className="catalog-form-section exits-animate-panel gap-0">
        <label
          className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
          htmlFor="connected-po-return-reason"
        >
          {t("returns.reason")}
          <input
            id="connected-po-return-reason"
            data-testid="connected-po-return-reason"
            type="text"
            required
            value={reason}
            className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            onChange={(event) => setReason(event.target.value)}
          />
        </label>
        <label
          className="mt-3 flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
          htmlFor="connected-po-return-notes"
        >
          {t("returns.notes")}
          <input
            id="connected-po-return-notes"
            data-testid="connected-po-return-notes"
            type="text"
            value={notes}
            className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            onChange={(event) => setNotes(event.target.value)}
          />
        </label>
        <p
          className="mb-0 mt-4 flex justify-between gap-2 font-semibold"
          data-testid="connected-po-return-estimate"
        >
          <span>{t("returns.connectedPo.returnValue")}</span>
          <MoneyDisplay amount={estimatedValue} />
        </p>
      </section>

      {error ? (
        <p
          data-testid="connected-po-return-error"
          className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
        >
          {error}
        </p>
      ) : null}

      <div className="catalog-form-actions">
        <div className="catalog-form-actions__primary">
          <Button
            type="button"
            className="catalog-form-actions__save"
            data-testid="connected-po-return-submit"
            disabled={
              submitting ||
              selected.length === 0 ||
              !reason.trim() ||
              !eligibility.canRequestReturn
            }
            onClick={() => void onSubmit()}
          >
            {submitting ? t("returns.submitting") : t("returns.connectedPo.submitRequest")}
          </Button>
        </div>
      </div>
    </div>
  );
}
