import { useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { canManageInventory } from "@/access/pos-capabilities";
import { PosApiError } from "@/api/pos/pos-http";
import {
  getDirectPurchaseReceipt,
  voidDirectPurchaseReceipt,
} from "@/api/pos/pos-direct-purchase-receipts-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { ActorAttribution } from "@/features/actors/ActorAttribution";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const RECEIPT_VOID_REASON_MAX = 512;

export function DirectPurchaseDetailPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { receiptId } = useParams<{ receiptId: string }>();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const queryClient = useQueryClient();
  const allowManage = canManageInventory(sessionGrant);
  const [error, setError] = useState<string | null>(null);
  const [voidOpen, setVoidOpen] = useState(false);
  const [voidReason, setVoidReason] = useState("");
  const [voiding, setVoiding] = useState(false);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: ["direct-purchase", workspace?.organizationId, receiptId],
    enabled: Boolean(workspace) && Boolean(receiptId) && online,
    queryFn: ({ signal }) => getDirectPurchaseReceipt(workspace!, receiptId!, signal),
  });

  const actors = useActorDirectory(workspace?.organizationId, [
    query.data?.createdByUserId,
    query.data?.voidedByUserId,
  ]);

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }
  if (!receiptId) {
    return (
      <ErrorState title={t("purchasing.errorTitle")} detail={t("purchasing.directNotFound")} />
    );
  }
  if (query.isLoading) {
    return <LoadingState label={t("purchasing.loading")} />;
  }
  if (query.isError || !query.data) {
    return (
      <ErrorState title={t("purchasing.errorTitle")} detail={t("purchasing.directNotFound")} />
    );
  }

  const receipt = query.data;
  const notes = receipt.notes?.trim();
  const isPosted = (receipt.status ?? "Posted") === "Posted";
  const isVoided = receipt.status === "Voided";

  async function onVoid() {
    if (!workspace || !receiptId || !allowManage || !online || voiding || !isPosted) {
      return;
    }
    const reason = voidReason.trim();
    if (!reason) {
      setError(t("purchasing.reverseReasonRequired"));
      return;
    }
    setVoiding(true);
    setError(null);
    try {
      const updated = await voidDirectPurchaseReceipt(workspace, receiptId, { reason });
      queryClient.setQueryData(["direct-purchase", workspace.organizationId, receiptId], updated);
      await queryClient.invalidateQueries({ queryKey: ["direct-purchases"] });
      await queryClient.invalidateQueries({ queryKey: ["inventory"] });
      setVoidOpen(false);
      setVoidReason("");
    } catch (err) {
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("purchasing.reverseFailed"))
          : t("purchasing.reverseFailed"),
      );
    } finally {
      setVoiding(false);
    }
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="direct-purchase-detail-page">
      <PageHeader
        title={receipt.receiptNumber}
        description={t("purchasing.directDetailLede")}
        backTo="/purchasing/direct-purchases"
        backLabel={t("purchasing.backDirect")}
        backTestId="page-header-back-purchasing"
      />

      {error ? <ErrorState title={t("purchasing.errorTitle")} detail={error} /> : null}

      <section aria-labelledby="direct-purchase-info">
        <h2
          id="direct-purchase-info"
          className="m-0 mb-2 text-[length:var(--exits-text-md)] font-medium"
        >
          {t("purchasing.purchaseInformation")}
        </h2>
        <Card className="flex flex-col gap-3 p-3">
          <div className="flex flex-wrap items-center gap-2" data-testid="direct-purchase-status">
            <StatusChip tone={isVoided ? "danger" : "success"}>
              {isVoided
                ? t("purchasing.receiptStatus.voided")
                : t("purchasing.receiptStatus.posted")}
            </StatusChip>
          </div>
          <dl className="m-0 grid gap-3 sm:grid-cols-2">
            <div>
              <dt className="text-[length:var(--exits-text-sm)] text-muted">
                {t("purchasing.purchaseDate")}
              </dt>
              <dd className="m-0" data-testid="direct-purchase-date">
                {receipt.purchaseDate}
              </dd>
            </div>
            <div>
              <dt className="text-[length:var(--exits-text-sm)] text-muted">
                {t("purchasing.boughtFrom")}
              </dt>
              <dd className="m-0" data-testid="direct-purchase-source">
                {receipt.sourceNameSnapshot ?? t("purchasing.sourceEmpty")}
              </dd>
            </div>
            {receipt.referenceNumber ? (
              <div>
                <dt className="text-[length:var(--exits-text-sm)] text-muted">
                  {t("purchasing.reference")}
                </dt>
                <dd className="m-0" data-testid="direct-purchase-reference">
                  {receipt.referenceNumber}
                </dd>
              </div>
            ) : null}
            <div>
              <dt className="text-[length:var(--exits-text-sm)] text-muted">
                {t("purchasing.totalPurchaseCost")}
              </dt>
              <dd className="m-0" data-testid="direct-purchase-total">
                <MoneyDisplay amount={receipt.totalCost} />
              </dd>
            </div>
          </dl>
          <ActorAttribution
            labelKey="common.recordedBy"
            actorId={receipt.createdByUserId}
            occurredAtUtc={receipt.createdAtUtc}
            resolved={actors.resolve(receipt.createdByUserId)}
            isLoading={actors.isResolving}
            testId="direct-purchase-recorded-by"
          />
          {isVoided && receipt.voidedByUserId ? (
            <ActorAttribution
              labelKey="purchasing.reversedBy"
              actorId={receipt.voidedByUserId}
              occurredAtUtc={receipt.voidedAtUtc}
              resolved={actors.resolve(receipt.voidedByUserId)}
              isLoading={actors.isResolving}
              testId="direct-purchase-reversed-by"
            />
          ) : null}
          {isVoided && receipt.voidReason ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="direct-purchase-void-reason">
              {t("purchasing.reverseReason")}: {receipt.voidReason}
            </p>
          ) : null}
          {notes ? (
            <div data-testid="direct-purchase-notes">
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("purchasing.notes")}
              </p>
              <p className="mt-1 mb-0 whitespace-pre-wrap text-[length:var(--exits-text-sm)]">
                {notes}
              </p>
            </div>
          ) : null}
        </Card>
      </section>

      <section aria-labelledby="direct-purchase-lines">
        <h2
          id="direct-purchase-lines"
          className="m-0 mb-2 text-[length:var(--exits-text-md)] font-medium"
        >
          {t("purchasing.items")}
        </h2>
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {receipt.lines.map((line) => (
            <li key={line.lineId}>
              <Card
                className="flex flex-col gap-2 p-3"
                data-testid={`direct-purchase-line-${line.lineId}`}
              >
                <p className="m-0 font-medium">{line.productNameSnapshot}</p>
                <p className="m-0 text-[length:var(--exits-text-sm)]">
                  {line.quantity} {line.unitOfMeasure}
                </p>
                <dl className="m-0 grid gap-1 text-[length:var(--exits-text-sm)]">
                  <div className="flex flex-wrap items-baseline justify-between gap-2">
                    <dt className="text-muted">{t("purchasing.unitPurchaseCost")}</dt>
                    <dd className="m-0">
                      <MoneyDisplay amount={line.unitCost} />
                      <span className="text-muted"> / {line.unitOfMeasure}</span>
                    </dd>
                  </div>
                  <div className="flex flex-wrap items-baseline justify-between gap-2">
                    <dt className="text-muted">{t("purchasing.lineTotal")}</dt>
                    <dd className="m-0">
                      <MoneyDisplay amount={line.lineTotal} />
                    </dd>
                  </div>
                  {line.expiryDate ? (
                    <div className="flex flex-wrap items-baseline justify-between gap-2">
                      <dt className="text-muted">{t("purchasing.expiryDate")}</dt>
                      <dd className="m-0">{line.expiryDate}</dd>
                    </div>
                  ) : null}
                  {line.expiryDate || line.lotNumber ? (
                    <div className="flex flex-wrap items-baseline justify-between gap-2">
                      <dt className="text-muted">{t("purchasing.lotNumber")}</dt>
                      <dd className="m-0">{line.lotNumber?.trim() || "—"}</dd>
                    </div>
                  ) : null}
                </dl>
              </Card>
            </li>
          ))}
        </ul>
      </section>

      {allowManage && isPosted && online ? (
        <Button
          type="button"
          variant="outline"
          className="min-h-11 w-fit"
          onClick={() => {
            setVoidOpen(true);
            setError(null);
          }}
          data-testid="direct-purchase-reverse"
        >
          {t("purchasing.reverseReceipt")}
        </Button>
      ) : null}

      {voidOpen ? (
        <div
          className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center"
          role="dialog"
          aria-modal="true"
          aria-labelledby="direct-purchase-reverse-title"
          data-testid="direct-purchase-reverse-dialog"
        >
          <Card className="flex w-full max-w-md flex-col gap-3 p-4">
            <h2
              id="direct-purchase-reverse-title"
              className="m-0 text-[length:var(--exits-text-lg)] font-semibold"
            >
              {t("purchasing.reverseTitle")}
            </h2>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("purchasing.reverseLede")}
            </p>
            <p className="m-0 text-[length:var(--exits-text-sm)]">
              {receipt.receiptNumber} · {receipt.purchaseDate}
              {receipt.sourceNameSnapshot ? ` · ${receipt.sourceNameSnapshot}` : ""}
            </p>
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              <span className="font-medium">{t("purchasing.reverseReason")}</span>
              <textarea
                className="min-h-24 rounded-[var(--exits-radius-md)] border border-border bg-background px-3 py-2"
                value={voidReason}
                maxLength={RECEIPT_VOID_REASON_MAX}
                onChange={(e) => setVoidReason(e.target.value)}
                data-testid="direct-purchase-reverse-reason"
              />
            </label>
            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                variant="destructive"
                className="min-h-11"
                disabled={voiding || !voidReason.trim()}
                onClick={() => void onVoid()}
                data-testid="direct-purchase-reverse-confirm"
              >
                {voiding ? t("purchasing.reversing") : t("purchasing.reverseConfirm")}
              </Button>
              <Button
                type="button"
                variant="outline"
                className="min-h-11"
                disabled={voiding}
                onClick={() => {
                  setVoidOpen(false);
                  setVoidReason("");
                }}
                data-testid="direct-purchase-reverse-cancel"
              >
                {t("purchasing.reverseCancel")}
              </Button>
            </div>
          </Card>
        </div>
      ) : null}
    </div>
  );
}
