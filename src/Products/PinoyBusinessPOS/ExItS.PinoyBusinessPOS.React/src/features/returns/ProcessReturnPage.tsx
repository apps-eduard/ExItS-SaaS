import { Undo2 } from "lucide-react";
import { useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { acceptReturnBatch } from "@/api/pos/pos-return-batches-client";
import {
  estimateLineRefundAmount,
  estimateTotalRefundAmount,
  formatRefundMethodLabel,
  getRefundableSale,
  isCashShiftRequiredError,
  isStaleReturnConflict,
  type PosRefundableSaleLineDto,
} from "@/api/pos/pos-sale-returns-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { Notice } from "@/components/exits/Notice";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay, QuantityStepper } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { isByWeightSellingMode } from "@/cart/sell-cart-helpers";
import { describeReturnError } from "@/features/returns/return-errors";
import { resolveReturnMutationId } from "@/features/returns/return-mutation-id";
import {
  clampReturnQuantity,
  formatReturnQuantityDisplay,
  maxReturnQuantityDecimals,
  requiresWholeReturnQuantity,
} from "@/features/returns/return-quantity";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type LineDraft = {
  quantity: number;
};

type Step = "edit" | "confirm";

export function ProcessReturnPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const headerBack = {
    backTo: pageBackNav.returns.to,
    backLabel: t(pageBackNav.returns.labelKey),
    backTestId: "page-header-back-returns",
  };
  const { saleId } = useParams<{ saleId: string }>();
  const { boundWorkspace } = useWorkspace();
  const queryClient = useQueryClient();

  const [drafts, setDrafts] = useState<Record<string, LineDraft>>({});
  const [reason, setReason] = useState("");
  const [notes, setNotes] = useState("");
  const [step, setStep] = useState<Step>("edit");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [staleNotice, setStaleNotice] = useState(false);
  const [pendingReturnBatchId, setPendingReturnBatchId] = useState<string | null>(null);

  const workspace =
    boundWorkspace?.branchId && boundWorkspace.organizationId
      ? {
          organizationId: boundWorkspace.organizationId,
          branchId: boundWorkspace.branchId,
        }
      : null;

  const refundableQuery = useQuery({
    queryKey: ["refundable-sale", workspace?.organizationId, workspace?.branchId, saleId],
    enabled: Boolean(workspace && saleId),
    queryFn: async ({ signal }) => {
      const data = await getRefundableSale(workspace!, saleId!, signal);
      setDrafts((prev) => {
        const next: Record<string, LineDraft> = {};
        for (const line of data.lines) {
          const prior = prev[line.saleLineId];
          const quantity =
            prior && prior.quantity > 0 && prior.quantity <= line.refundableQuantity
              ? prior.quantity
              : 0;
          next[line.saleLineId] = {
            quantity,
          };
        }
        return next;
      });
      return data;
    },
  });

  const refundable = refundableQuery.data;

  const selectedLines = useMemo(() => {
    if (!refundable) {
      return [];
    }
    return refundable.lines
      .map((line) => {
        const draft = drafts[line.saleLineId];
        const quantity = draft?.quantity ?? 0;
        return { line, quantity };
      })
      .filter((entry) => entry.quantity > 0);
  }, [drafts, refundable]);

  const estimatedTotal = useMemo(
    () =>
      estimateTotalRefundAmount(
        selectedLines.map(({ line, quantity }) => ({
          originalQuantity: line.originalQuantity,
          originalLineTotal: line.originalLineTotal,
          previouslyReturnedQuantity: line.previouslyReturnedQuantity,
          previouslyRefundedAmount: line.previouslyRefundedAmount,
          requestedQty: quantity,
        })),
      ),
    [selectedLines],
  );

  const canContinue =
    Boolean(reason.trim()) && selectedLines.length > 0 && !submitting && step === "edit";

  function setLineQuantity(line: PosRefundableSaleLineDto, raw: number) {
    const decimals = maxReturnQuantityDecimals(line.unitOfMeasure, line.sellingMode);
    const quantity = clampReturnQuantity(raw, line.refundableQuantity, decimals);
    setDrafts((prev) => ({
      ...prev,
      [line.saleLineId]: {
        quantity,
      },
    }));
  }

  function adjustLineQuantity(line: PosRefundableSaleLineDto, delta: number) {
    const current = drafts[line.saleLineId]?.quantity ?? 0;
    const stepSize = requiresWholeReturnQuantity(line.unitOfMeasure, line.sellingMode) ? 1 : 0.001;
    setLineQuantity(line, current + delta * stepSize);
  }

  async function reloadRefundable() {
    setStaleNotice(true);
    setStep("edit");
    setError(null);
    setPendingReturnBatchId(null);
    setDrafts({});
    await queryClient.invalidateQueries({
      queryKey: ["refundable-sale", workspace?.organizationId, workspace?.branchId, saleId],
    });
    await refundableQuery.refetch();
  }

  async function onConfirmSubmit() {
    if (!workspace || !saleId || !refundable || submitting) {
      return;
    }
    const trimmedReason = reason.trim();
    if (!trimmedReason || selectedLines.length === 0) {
      setError(t("returns.reasonRequired"));
      return;
    }

    const resolved = resolveReturnMutationId(pendingReturnBatchId);
    if (!resolved.ok) {
      setError(t("returns.errorSecureId"));
      return;
    }

    setPendingReturnBatchId(resolved.id);
    await submitReturnBatch(resolved.id, trimmedReason);
  }

  async function submitReturnBatch(returnBatchId: string, trimmedReason: string) {
    if (!workspace || !saleId || !refundable) {
      return;
    }

    setSubmitting(true);
    setError(null);

    try {
      await acceptReturnBatch(workspace, {
        saleId,
        reason: trimmedReason,
        notes: notes.trim() || undefined,
        returnBatchId,
        lines: selectedLines.map(({ line, quantity }) => ({
          saleLineId: line.saleLineId,
          acceptedQuantity: quantity,
        })),
      });
      setPendingReturnBatchId(null);
      await queryClient.invalidateQueries({
        queryKey: ["return-batches", workspace.organizationId, workspace.branchId, saleId],
      });
      await queryClient.invalidateQueries({ queryKey: ["sale-returns"] });
      navigate(`/sell/sales/${saleId}/summary`, { replace: true });
    } catch (err) {
      if (isCashShiftRequiredError(err)) {
        setError(t("returns.errorNoShift"));
      } else if (isStaleReturnConflict(err)) {
        await reloadRefundable();
        setError(t("returns.errorStale"));
      } else {
        setError(describeReturnError(err, t));
      }
    } finally {
      setSubmitting(false);
    }
  }

  if (!saleId) {
    return (
      <div data-testid="process-return-missing" className="flex flex-col gap-3">
        <PageHeader title={t("returns.returnItems")} description={t("returns.missingSale")} {...headerBack} />
      </div>
    );
  }

  if (refundableQuery.isLoading || !workspace) {
    return <LoadingSkeleton label={t("loading.label")} />;
  }

  if (refundableQuery.isError || !refundable) {
    return (
      <div data-testid="process-return-error" className="flex flex-col gap-3">
        <PageHeader title={t("returns.returnItems")} description={t("returns.loadError")} {...headerBack} />
      </div>
    );
  }

  if (refundable.status !== "Completed") {
    return (
      <div data-testid="process-return-not-returnable" className="flex flex-col gap-3">
        <PageHeader title={t("returns.returnItems")} description={t("returns.cannotReturn")} {...headerBack} />
      </div>
    );
  }

  if (refundable.lines.length === 0) {
    return (
      <div data-testid="process-return-empty" className="flex flex-col gap-3">
        <PageHeader
          title={t("returns.returnItems")}
          description={`${t("returns.alreadyReturned")} · ${refundable.saleNumber}`}
          {...headerBack}
        />
        <EmptyState
              align="center"
              icon={<Undo2 className="size-5" strokeWidth={1.75} />}
          title={t("returns.alreadyReturned")}
          detail={t("returns.alreadyReturnedDetail")}
        />
      </div>
    );
  }

  if (step === "confirm") {
    return (
      <div data-testid="process-return-confirm" className="flex min-w-0 flex-col gap-3">
        <PageHeader
          title={t("returns.confirmTitle")}
          description={`${t("returns.confirmLede")} · ${refundable.saleNumber}`}
          {...headerBack}
        />
        <section className="catalog-form-section exits-animate-panel gap-0">
          <ul className="m-0 list-none space-y-2 p-0">
            {selectedLines.map(({ line, quantity }) => (
              <li key={line.saleLineId} className="text-[length:var(--exits-text-sm)]">
                <span className="font-semibold">{line.productNameSnapshot}</span>
                <span className="text-muted">
                  {" "}
                  · {formatReturnQuantityDisplay(
                    quantity,
                    line.unitOfMeasure,
                    line.sellingMode,
                  )}
                </span>
              </li>
            ))}
          </ul>
          <p className="mb-0 mt-3 text-[length:var(--exits-text-sm)]">
            {t("returns.reason")}: {reason.trim()}
          </p>
          <p className="mb-0 mt-3 flex justify-between gap-2 font-semibold">
            <span>{t("returns.estimatedRefund")}</span>
            <MoneyDisplay amount={estimatedTotal} testId="returns-confirm-estimate" />
          </p>
          <p className="mb-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">
            {t("returns.estimateDisclaimer")}
          </p>
        </section>
        {error ? (
          <p
            data-testid="returns-confirm-error"
            className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
          >
            {error}
          </p>
        ) : null}
        <div className="catalog-form-actions process-return-confirm-actions">
          <div className="catalog-form-actions__primary">
          <Button
            type="button"
            className="catalog-form-actions__save"
            data-testid="returns-confirm-submit"
            disabled={submitting}
            onClick={() => void onConfirmSubmit()}
          >
            {submitting ? t("returns.submitting") : t("returns.confirmSubmit")}
          </Button>
          </div>
          <div className="catalog-form-actions__secondary">
            <Button
              type="button"
              variant="ghost"
              className="w-full sm:w-auto"
              disabled={submitting}
              data-testid="returns-confirm-back"
              onClick={() => {
                setStep("edit");
                setError(null);
              }}
            >
              {t("returns.backToEdit")}
            </Button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div data-testid="process-return-page" className="flex min-w-0 flex-col gap-3">
      <PageHeader
        title={t("returns.returnItems")}
        description={`${t("returns.processLede")} · ${refundable.saleNumber}`}
        {...headerBack}
      />

      <section
        className="catalog-form-section exits-animate-panel gap-0"
        data-testid="returns-payment-method"
      >
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("returns.refundMethod")}
        </p>
        <p className="mb-0 mt-1 font-semibold">
          {formatRefundMethodLabel(refundable.paymentMethod)}
        </p>
      </section>

      {staleNotice ? (
        <Notice tone="danger" testId="returns-stale-banner">{t("returns.errorStale")}</Notice>
      ) : null}

      <ul className="m-0 flex list-none flex-col gap-3 p-0" data-testid="returns-lines">
        {refundable.lines.map((line) => {
          const draft = drafts[line.saleLineId] ?? {
            quantity: 0,
          };
          const byWeight = isByWeightSellingMode(line.sellingMode);
          const decimals = maxReturnQuantityDecimals(line.unitOfMeasure, line.sellingMode);
          const lineEstimate =
            draft.quantity > 0
              ? estimateLineRefundAmount({
                  originalQuantity: line.originalQuantity,
                  originalLineTotal: line.originalLineTotal,
                  previouslyReturnedQuantity: line.previouslyReturnedQuantity,
                  previouslyRefundedAmount: line.previouslyRefundedAmount,
                  requestedQty: draft.quantity,
                })
              : 0;

          return (
            <li key={line.saleLineId}>
              <Card className="p-3" data-testid={`returns-line-${line.saleLineId}`}>
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0">
                    <p className="m-0 font-semibold">{line.productNameSnapshot}</p>
                    <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                      {t("returns.returnableQty")}:{" "}
                      {formatReturnQuantityDisplay(
                        line.refundableQuantity,
                        line.unitOfMeasure,
                        line.sellingMode,
                      )}
                    </p>
                  </div>
                  <MoneyDisplay amount={line.refundableAmount} />
                </div>

                <div className="mt-3 flex flex-wrap items-end gap-3">
                  {byWeight || decimals > 0 ? (
                    <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                      {t("returns.quantity")}
                      <input
                        type="number"
                        inputMode="decimal"
                        min={0}
                        max={line.refundableQuantity}
                        step={decimals > 0 ? 0.001 : 1}
                        value={draft.quantity || ""}
                        data-testid={`returns-qty-input-${line.saleLineId}`}
                        className="w-28 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
                        onChange={(event) => {
                          const parsed = Number(event.target.value);
                          setLineQuantity(line, Number.isFinite(parsed) ? parsed : 0);
                        }}
                      />
                    </label>
                  ) : (
                    <QuantityStepper
                      value={draft.quantity}
                      increaseLabel={t("returns.increaseQty")}
                      decreaseLabel={t("returns.decreaseQty")}
                      valueTestId={`returns-qty-${line.saleLineId}`}
                      onIncrement={() => adjustLineQuantity(line, 1)}
                      onDecrement={() => adjustLineQuantity(line, -1)}
                    />
                  )}
                  <Button
                    type="button"
                    variant="ghost"
                    data-testid={`returns-return-all-${line.saleLineId}`}
                    disabled={draft.quantity >= line.refundableQuantity}
                    onClick={() => setLineQuantity(line, line.refundableQuantity)}
                  >
                    {t("returns.returnAll")}
                  </Button>
                </div>

                {draft.quantity > 0 ? (
                  <p className="mb-0 mt-3 flex justify-between gap-2 text-[length:var(--exits-text-sm)]">
                    <span className="text-muted">{t("returns.estimatedRefund")}</span>
                    <MoneyDisplay amount={lineEstimate} />
                  </p>
                ) : null}
              </Card>
            </li>
          );
        })}
      </ul>

      <section className="catalog-form-section exits-animate-panel gap-0">
        <label
          className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
          htmlFor="returns-reason"
        >
          {t("returns.reason")}
          <input
            id="returns-reason"
            data-testid="returns-reason"
            type="text"
            required
            value={reason}
            className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            onChange={(event) => setReason(event.target.value)}
          />
        </label>
        <label
          className="mt-3 flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
          htmlFor="returns-notes"
        >
          {t("returns.notes")}
          <input
            id="returns-notes"
            data-testid="returns-notes"
            type="text"
            value={notes}
            className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            onChange={(event) => setNotes(event.target.value)}
          />
        </label>
        <p
          className="mb-0 mt-4 flex justify-between gap-2 font-semibold"
          data-testid="returns-estimate-total"
        >
          <span>{t("returns.estimatedRefund")}</span>
          <MoneyDisplay amount={estimatedTotal} />
        </p>
        <p className="mb-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">
          {t("returns.estimateDisclaimer")}
        </p>
      </section>

      {error ? (
        <p
          data-testid="returns-edit-error"
          className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
        >
          {error}
        </p>
      ) : null}

      <div className="catalog-form-actions process-return-edit-actions">
        <div className="catalog-form-actions__primary">
          <Button
            type="button"
            className="catalog-form-actions__save"
            data-testid="returns-continue"
            disabled={!canContinue}
            onClick={() => {
              if (!reason.trim()) {
                setError(t("returns.reasonRequired"));
                return;
              }
              setError(null);
              setStaleNotice(false);
              setStep("confirm");
            }}
          >
            {t("returns.continue")}
          </Button>
        </div>
      </div>
    </div>
  );
}
