import { useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createSaleReturn,
  estimateLineRefundAmount,
  estimateTotalRefundAmount,
  formatRefundMethodLabel,
  getRefundableSale,
  isCashRefundMethod,
  isCashShiftRequiredError,
  isGCashRefundMethod,
  isStaleReturnConflict,
  isUtangRefundMethod,
  type PosRefundableSaleLineDto,
  type RestockDisposition,
} from "@/api/pos/pos-sale-returns-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay, QuantityStepper } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { isByWeightSellingMode } from "@/cart/sell-cart-helpers";
import { describeReturnError } from "@/features/returns/return-errors";
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
  disposition: RestockDisposition;
};

type Step = "edit" | "confirm" | "success";

function newReturnId(): string {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }
  return "00000000-0000-4000-8000-000000000000";
}

export function ProcessReturnPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
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
  const [completedReturnId, setCompletedReturnId] = useState<string | null>(null);
  const [completedRefund, setCompletedRefund] = useState<number | null>(null);
  const [completedMethod, setCompletedMethod] = useState<string | null>(null);
  const [pendingReturnId, setPendingReturnId] = useState<string | null>(null);

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
            disposition: prior?.disposition ?? "ReturnToStock",
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
        return { line, quantity, disposition: draft?.disposition ?? "ReturnToStock" };
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
        disposition: prev[line.saleLineId]?.disposition ?? "ReturnToStock",
      },
    }));
  }

  function adjustLineQuantity(line: PosRefundableSaleLineDto, delta: number) {
    const current = drafts[line.saleLineId]?.quantity ?? 0;
    const stepSize = requiresWholeReturnQuantity(line.unitOfMeasure, line.sellingMode) ? 1 : 0.001;
    setLineQuantity(line, current + delta * stepSize);
  }

  function setDisposition(saleLineId: string, disposition: RestockDisposition) {
    setDrafts((prev) => ({
      ...prev,
      [saleLineId]: {
        quantity: prev[saleLineId]?.quantity ?? 0,
        disposition,
      },
    }));
  }

  async function reloadRefundable() {
    setStaleNotice(true);
    setStep("edit");
    setError(null);
    setPendingReturnId(null);
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

    const returnId = pendingReturnId ?? newReturnId();
    setPendingReturnId(returnId);
    setSubmitting(true);
    setError(null);

    try {
      const created = await createSaleReturn(workspace, {
        saleId,
        reason: trimmedReason,
        notes: notes.trim() || undefined,
        returnId,
        lines: selectedLines.map(({ line, quantity, disposition }) => ({
          saleLineId: line.saleLineId,
          quantity,
          restockDisposition: disposition,
        })),
      });
      setCompletedReturnId(created.returnId);
      setCompletedRefund(created.totalRefundAmount);
      setCompletedMethod(created.refundMethod);
      setStep("success");
      setPendingReturnId(null);
      await queryClient.invalidateQueries({ queryKey: ["sale-returns"] });
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
        <PageHeader title={t("returns.returnItems")} description={t("returns.missingSale")} />
        <Button asChild variant="ghost" className="min-h-11 w-fit">
          <Link to="/returns">{t("returns.back")}</Link>
        </Button>
      </div>
    );
  }

  if (refundableQuery.isLoading || !workspace) {
    return <LoadingSkeleton label={t("loading.label")} />;
  }

  if (refundableQuery.isError || !refundable) {
    return (
      <div data-testid="process-return-error" className="flex flex-col gap-3">
        <PageHeader title={t("returns.returnItems")} description={t("returns.loadError")} />
        <Button asChild className="min-h-11 w-fit">
          <Link to="/returns">{t("returns.back")}</Link>
        </Button>
      </div>
    );
  }

  if (refundable.status !== "Completed") {
    return (
      <div data-testid="process-return-not-returnable" className="flex flex-col gap-3">
        <PageHeader title={t("returns.returnItems")} description={t("returns.cannotReturn")} />
        <Button asChild className="min-h-11 w-fit">
          <Link to="/returns">{t("returns.back")}</Link>
        </Button>
      </div>
    );
  }

  if (refundable.lines.length === 0) {
    return (
      <div data-testid="process-return-empty" className="flex flex-col gap-3">
        <PageHeader
          title={t("returns.returnItems")}
          description={`${t("returns.alreadyReturned")} · ${refundable.saleNumber}`}
        />
        <EmptyState
          title={t("returns.alreadyReturned")}
          detail={t("returns.alreadyReturnedDetail")}
        />
        <Button asChild className="min-h-11 w-fit">
          <Link to="/returns">{t("returns.back")}</Link>
        </Button>
      </div>
    );
  }

  if (step === "success" && completedReturnId != null && completedRefund != null) {
    const method = completedMethod ?? refundable.paymentMethod;
    return (
      <div data-testid="process-return-success" className="flex min-w-0 flex-col gap-4">
        <PageHeader title={t("returns.successTitle")} description={refundable.saleNumber} />
        <Card>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("returns.refundAmount")}
          </p>
          <p className="mb-0 mt-1 text-[length:var(--exits-text-lg)] font-semibold">
            <MoneyDisplay amount={completedRefund} testId="returns-final-refund" />
          </p>
          {isCashRefundMethod(method) ? (
            <p className="mb-0 mt-3" data-testid="returns-success-cash">
              {t("returns.successCash")}
            </p>
          ) : null}
          {isGCashRefundMethod(method) ? (
            <p className="mb-0 mt-3" data-testid="returns-success-gcash">
              {t("returns.successGCash")}
            </p>
          ) : null}
          {isUtangRefundMethod(method) ? (
            <p className="mb-0 mt-3" data-testid="returns-success-utang">
              {t("returns.successUtang")}
            </p>
          ) : null}
          <p className="mb-0 mt-2 text-[length:var(--exits-text-sm)] text-muted">
            {t("returns.refundMethod")}: {formatRefundMethodLabel(method)}
          </p>
        </Card>
        <div className="flex flex-wrap gap-2">
          <Button asChild className="min-h-11" data-testid="returns-view-detail">
            <Link to={`/returns/${completedReturnId}`}>{t("returns.viewDetail")}</Link>
          </Button>
          <Button asChild variant="ghost" className="min-h-11">
            <Link to="/returns">{t("returns.back")}</Link>
          </Button>
        </div>
      </div>
    );
  }

  if (step === "confirm") {
    return (
      <div data-testid="process-return-confirm" className="flex min-w-0 flex-col gap-4">
        <PageHeader
          title={t("returns.confirmTitle")}
          description={`${t("returns.confirmLede")} · ${refundable.saleNumber}`}
        />
        <Card>
          <ul className="m-0 list-none space-y-2 p-0">
            {selectedLines.map(({ line, quantity, disposition }) => (
              <li key={line.saleLineId} className="text-[length:var(--exits-text-sm)]">
                <span className="font-semibold">{line.productNameSnapshot}</span>
                <span className="text-muted">
                  {" "}
                  · {formatReturnQuantityDisplay(
                    quantity,
                    line.unitOfMeasure,
                    line.sellingMode,
                  )} ·{" "}
                  {disposition === "ReturnToStock"
                    ? t("returns.putBackInStock")
                    : t("returns.doNotReturnToStock")}
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
        </Card>
        {error ? (
          <p
            data-testid="returns-confirm-error"
            className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
          >
            {error}
          </p>
        ) : null}
        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            className="min-h-11"
            data-testid="returns-confirm-submit"
            disabled={submitting}
            onClick={() => void onConfirmSubmit()}
          >
            {submitting ? t("returns.submitting") : t("returns.confirmSubmit")}
          </Button>
          <Button
            type="button"
            variant="ghost"
            className="min-h-11"
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
    );
  }

  return (
    <div data-testid="process-return-page" className="flex min-w-0 flex-col gap-4">
      <PageHeader
        title={t("returns.returnItems")}
        description={`${t("returns.processLede")} · ${refundable.saleNumber}`}
      />

      <Card data-testid="returns-payment-method">
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("returns.refundMethod")}
        </p>
        <p className="mb-0 mt-1 font-semibold">
          {formatRefundMethodLabel(refundable.paymentMethod)}
        </p>
      </Card>

      {staleNotice ? (
        <Card data-testid="returns-stale-banner">
          <p className="m-0 text-[length:var(--exits-text-sm)]">{t("returns.errorStale")}</p>
        </Card>
      ) : null}

      <ul className="m-0 flex list-none flex-col gap-3 p-0" data-testid="returns-lines">
        {refundable.lines.map((line) => {
          const draft = drafts[line.saleLineId] ?? {
            quantity: 0,
            disposition: "ReturnToStock" as RestockDisposition,
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
                        className="min-h-11 w-28 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
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
                    className="min-h-11"
                    data-testid={`returns-return-all-${line.saleLineId}`}
                    disabled={draft.quantity >= line.refundableQuantity}
                    onClick={() => setLineQuantity(line, line.refundableQuantity)}
                  >
                    {t("returns.returnAll")}
                  </Button>
                </div>

                <fieldset className="mt-3 border-0 p-0">
                  <legend className="mb-1 text-[length:var(--exits-text-sm)]">
                    {t("returns.stockDisposition")}
                  </legend>
                  <div className="flex flex-wrap gap-2">
                    <Button
                      type="button"
                      variant={draft.disposition === "ReturnToStock" ? "default" : "ghost"}
                      className="min-h-11"
                      data-testid={`returns-restock-${line.saleLineId}`}
                      onClick={() => setDisposition(line.saleLineId, "ReturnToStock")}
                    >
                      {t("returns.putBackInStock")}
                    </Button>
                    <Button
                      type="button"
                      variant={draft.disposition === "DoNotRestock" ? "default" : "ghost"}
                      className="min-h-11"
                      data-testid={`returns-no-restock-${line.saleLineId}`}
                      onClick={() => setDisposition(line.saleLineId, "DoNotRestock")}
                    >
                      {t("returns.doNotReturnToStock")}
                    </Button>
                  </div>
                  <p className="mb-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">
                    {draft.disposition === "ReturnToStock"
                      ? t("returns.putBackHint")
                      : t("returns.doNotRestockHint")}
                  </p>
                </fieldset>

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

      <Card>
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
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
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
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
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
      </Card>

      {error ? (
        <p
          data-testid="returns-edit-error"
          className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
        >
          {error}
        </p>
      ) : null}

      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          className="min-h-11"
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
        <Button
          type="button"
          variant="ghost"
          className="min-h-11"
          onClick={() => navigate("/returns")}
        >
          {t("returns.back")}
        </Button>
      </div>
    </div>
  );
}
