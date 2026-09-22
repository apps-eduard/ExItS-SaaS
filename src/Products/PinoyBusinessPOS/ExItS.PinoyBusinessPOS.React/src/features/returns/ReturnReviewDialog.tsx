import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import {
  finalizeReturnBatch,
  getReturnBatchReviewPreview,
  type ReturnBatchDto,
} from "@/api/pos/pos-return-batches-client";
import { finalizeConnectedPoReturnBatch } from "@/api/pos/pos-connected-po-returns-client";
import { BottomSheet } from "@/components/exits/SheetDialog";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";

type Props = {
  open: boolean;
  workspace: PosWorkspaceScope;
  batch: ReturnBatchDto | null;
  onClose: () => void;
  onBackToEdit: () => void;
  onFinalized: (updated: ReturnBatchDto) => void;
};

export function ReturnReviewDialog({
  open,
  workspace,
  batch,
  onClose,
  onBackToEdit,
  onFinalized,
}: Props) {
  const { t } = useI18n();
  const [finalizing, setFinalizing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const reviewQuery = useQuery({
    queryKey: ["return-batch-review", workspace.organizationId, workspace.branchId, batch?.returnBatchId],
    enabled: open && Boolean(batch),
    queryFn: ({ signal }) => getReturnBatchReviewPreview(workspace, batch!.returnBatchId, signal),
  });

  const totals = useMemo(() => {
    const lines = reviewQuery.data?.lines ?? [];
    return lines.reduce(
      (acc, line) => {
        acc.returned += line.acceptedQuantity;
        acc.sellable += line.sellableQuantity ?? 0;
        acc.damaged += line.damagedQuantity ?? 0;
        return acc;
      },
      { returned: 0, sellable: 0, damaged: 0 },
    );
  }, [reviewQuery.data?.lines]);

  async function onFinalize() {
    if (!batch || finalizing) {
      return;
    }
    setFinalizing(true);
    setError(null);
    try {
      const updated =
        batch.sourceType === "ConnectedPurchaseOrder"
          ? await finalizeConnectedPoReturnBatch(
              workspace,
              batch.returnBatchId,
              batch.updatedAtUtc,
            )
          : await finalizeReturnBatch(workspace, batch.returnBatchId, {
              expectedUpdatedAtUtc: batch.updatedAtUtc,
            });
      onFinalized(updated);
      onClose();
    } catch (err) {
      setError((err as Error).message || t("error.title"));
    } finally {
      setFinalizing(false);
    }
  }

  return (
    <BottomSheet
      open={open}
      onClose={onClose}
      panelId="return-review-dialog"
      testId="return-review-dialog"
      title={t("returns.reviewReturn")}
      closeLabel={t("sell.cancel")}
      presentation="sheet-mobile-dialog-desktop"
    >
      {!batch ? null : (
        <div className="flex min-w-0 flex-col gap-3 pb-1">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{batch.batchNumber}</p>

          {reviewQuery.isLoading ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("loading.label")}</p>
          ) : null}
          {reviewQuery.isError ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
              {(reviewQuery.error as Error).message}
            </p>
          ) : null}
          {reviewQuery.data ? (
            <>
              <ul className="m-0 list-none space-y-2 p-0" data-testid="return-review-lines">
                {reviewQuery.data.lines.map((line) => (
                  <li
                    key={line.returnBatchLineId}
                    className="rounded-[var(--exits-radius-md)] border border-border p-2"
                  >
                    <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                      {line.productNameSnapshot}
                    </p>
                    <p className="m-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">
                      {t("returns.returnedQuantity")}: {line.acceptedQuantity} {line.unitOfMeasure} ·{" "}
                      {t("returns.sellableAgain")}: {line.sellableQuantity ?? 0} · {t("returns.damagedWriteOff")}:{" "}
                      {line.damagedQuantity ?? 0}
                    </p>
                    <p className="m-0 mt-1 text-[length:var(--exits-text-sm)]">
                      {t("returns.refundDue")}: <MoneyDisplay amount={line.refundAmountSnapshot} />
                    </p>
                  </li>
                ))}
              </ul>

              <div className="rounded-[var(--exits-radius-md)] border border-border p-3">
                <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                  {t("returns.inventoryImpact")}
                </p>
                <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                  {t("returns.returnedQuantity")}: {totals.returned} · {t("returns.sellableAgain")}: {totals.sellable} ·{" "}
                  {t("returns.damagedWriteOff")}: {totals.damaged}
                </p>
              </div>

              <div className="rounded-[var(--exits-radius-md)] border border-border p-3">
                <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                  {t("returns.financialImpact")}
                </p>
                <p className="m-0 mt-1 flex justify-between gap-2 text-[length:var(--exits-text-sm)]">
                  <span>{t("returns.originalSaleTotal")}</span>
                  <MoneyDisplay amount={reviewQuery.data.financialSummary.originalSaleTotal} />
                </p>
                <p className="m-0 mt-1 flex justify-between gap-2 text-[length:var(--exits-text-sm)]">
                  <span>{t("returns.amountPreviouslyPaid")}</span>
                  <MoneyDisplay amount={reviewQuery.data.financialSummary.amountPreviouslyPaid} />
                </p>
                <p className="m-0 mt-1 flex justify-between gap-2 text-[length:var(--exits-text-sm)]">
                  <span>{t("returns.remainingAmountDue")}</span>
                  <MoneyDisplay amount={reviewQuery.data.financialSummary.remainingAmountDue} />
                </p>
                <p className="m-0 mt-1 flex justify-between gap-2 text-[length:var(--exits-text-sm)]">
                  <span>{t("returns.refundDue")}</span>
                  <MoneyDisplay amount={reviewQuery.data.refundDueAmount} />
                </p>
                <p className="m-0 mt-1 flex justify-between gap-2 text-[length:var(--exits-text-sm)] font-semibold">
                  <span>{t("returns.estimatedRefund")}</span>
                  <MoneyDisplay amount={reviewQuery.data.acceptedReturnValue} />
                </p>
              </div>
            </>
          ) : null}

          {error ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">{error}</p>
          ) : null}

          <div className="mt-1 flex flex-wrap justify-end gap-2">
            <Button
              type="button"
              variant="ghost"
              disabled={finalizing}
              data-testid="return-review-back-edit"
              onClick={onBackToEdit}
            >
              {t("returns.backToEdit")}
            </Button>
            <Button
              type="button"
              onClick={() => void onFinalize()}
              disabled={finalizing || reviewQuery.isLoading || reviewQuery.isError}
              data-testid="return-review-finalize"
            >
              {finalizing ? t("returns.submitting") : t("returns.finalizeReturns")}
            </Button>
          </div>
        </div>
      )}
    </BottomSheet>
  );
}
