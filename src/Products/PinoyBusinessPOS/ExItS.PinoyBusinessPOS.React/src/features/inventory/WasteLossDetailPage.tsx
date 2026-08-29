import { useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { canManageInventory } from "@/access/pos-capabilities";
import { PosApiError } from "@/api/pos/pos-http";
import { getWasteLoss, voidWasteLoss } from "@/api/pos/pos-waste-loss-client";
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
import {
  formatWasteLossOccurredDate,
  sumWasteLossLineCosts,
  wasteLossCostStatusLabelKey,
  wasteLossReasonLabelKey,
  wasteLossStatusLabelKey,
} from "@/features/inventory/waste-loss-labels";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function WasteLossDetailPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { wasteLossId } = useParams<{ wasteLossId: string }>();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const queryClient = useQueryClient();
  const allowManage = canManageInventory(sessionGrant);
  const [error, setError] = useState<string | null>(null);
  const [voiding, setVoiding] = useState(false);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: ["waste-loss", workspace?.organizationId, wasteLossId],
    enabled: Boolean(workspace) && Boolean(wasteLossId) && online,
    queryFn: ({ signal }) => getWasteLoss(workspace!, wasteLossId!, signal),
  });

  const actors = useActorDirectory(workspace?.organizationId, [
    query.data?.createdByUserId,
    query.data?.voidedByUserId,
  ]);

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }
  if (!wasteLossId) {
    return <ErrorState title={t("wasteLoss.errorTitle")} detail={t("wasteLoss.notFound")} />;
  }
  if (query.isLoading) {
    return <LoadingState label={t("wasteLoss.loading")} />;
  }
  if (query.isError || !query.data) {
    return <ErrorState title={t("wasteLoss.errorTitle")} detail={t("wasteLoss.notFound")} />;
  }

  const entry = query.data;
  const isPosted = entry.status === "Posted";
  const isVoided = entry.status === "Voided";
  const notes = entry.notes?.trim();
  const lineCostTotal = sumWasteLossLineCosts(entry.lines);
  const totalCost = entry.totalCostSnapshot ?? lineCostTotal;

  async function onVoid() {
    if (!workspace || !wasteLossId || !allowManage || !online || voiding || !isPosted) {
      return;
    }
    if (!window.confirm(t("wasteLoss.voidConfirm"))) {
      return;
    }
    setVoiding(true);
    setError(null);
    try {
      const updated = await voidWasteLoss(workspace, wasteLossId);
      queryClient.setQueryData(["waste-loss", workspace.organizationId, wasteLossId], updated);
      await queryClient.invalidateQueries({ queryKey: ["waste-losses"] });
      await queryClient.invalidateQueries({ queryKey: ["inventory"] });
    } catch (err) {
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("wasteLoss.voidFailed"))
          : t("wasteLoss.voidFailed"),
      );
    } finally {
      setVoiding(false);
    }
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="waste-loss-detail-page">
      <PageHeader
        title={entry.wasteLossNumber}
        description={t("wasteLoss.detailLede")}
        backTo="/inventory/waste-loss"
        backLabel={t("wasteLoss.backList")}
        backTestId="page-header-back-waste-loss"
      />

      {error ? <ErrorState title={t("wasteLoss.errorTitle")} detail={error} /> : null}

      <section aria-labelledby="waste-loss-info">
        <h2
          id="waste-loss-info"
          className="m-0 mb-2 text-[length:var(--exits-text-md)] font-medium"
        >
          {t("wasteLoss.wastedStock")}
        </h2>
        <Card className="flex flex-col gap-3 p-3">
          <div className="flex flex-wrap items-center gap-2">
            <StatusChip tone={isVoided ? "danger" : "success"}>
              {t(wasteLossStatusLabelKey(entry.status))}
            </StatusChip>
            <span className="text-[length:var(--exits-text-sm)] text-muted">
              {formatWasteLossOccurredDate(entry.occurredAtUtc)}
            </span>
          </div>
          <dl className="m-0 grid gap-3 sm:grid-cols-2">
            <div>
              <dt className="text-[length:var(--exits-text-sm)] text-muted">
                {t("wasteLoss.reason")}
              </dt>
              <dd className="m-0" data-testid="waste-loss-detail-reason">
                {t(wasteLossReasonLabelKey(entry.reason))}
              </dd>
            </div>
            {entry.referenceNumber ? (
              <div>
                <dt className="text-[length:var(--exits-text-sm)] text-muted">
                  {t("wasteLoss.reference")}
                </dt>
                <dd className="m-0">{entry.referenceNumber}</dd>
              </div>
            ) : null}
            <div>
              <dt className="text-[length:var(--exits-text-sm)] text-muted">
                {t("wasteLoss.costStatus")}
              </dt>
              <dd className="m-0" data-testid="waste-loss-detail-cost-status">
                {t(wasteLossCostStatusLabelKey(entry.costStatus))}
              </dd>
            </div>
            {totalCost != null ? (
              <div>
                <dt className="text-[length:var(--exits-text-sm)] text-muted">
                  {t("wasteLoss.estimatedCost")}
                </dt>
                <dd className="m-0" data-testid="waste-loss-detail-cost">
                  <MoneyDisplay amount={totalCost} />
                </dd>
              </div>
            ) : entry.costStatus !== "Complete" ? (
              <div>
                <dt className="text-[length:var(--exits-text-sm)] text-muted">
                  {t("wasteLoss.estimatedCost")}
                </dt>
                <dd className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {entry.costStatus === "Partial"
                    ? t("wasteLoss.costPartialDetail")
                    : t("wasteLoss.costUnavailableDetail")}
                </dd>
              </div>
            ) : null}
          </dl>
          <ActorAttribution
            labelKey="common.recordedBy"
            actorId={entry.createdByUserId}
            occurredAtUtc={entry.createdAtUtc}
            resolved={actors.resolve(entry.createdByUserId)}
            isLoading={actors.isResolving}
            testId="waste-loss-recorded-by"
          />
          {isVoided && entry.voidedByUserId ? (
            <ActorAttribution
              labelKey="common.voidedBy"
              actorId={entry.voidedByUserId}
              occurredAtUtc={entry.voidedAtUtc ?? undefined}
              resolved={actors.resolve(entry.voidedByUserId)}
              isLoading={actors.isResolving}
              testId="waste-loss-voided-by"
            />
          ) : null}
          {notes ? (
            <div data-testid="waste-loss-detail-notes">
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("wasteLoss.notes")}
              </p>
              <p className="mt-1 mb-0 whitespace-pre-wrap text-[length:var(--exits-text-sm)]">
                {notes}
              </p>
            </div>
          ) : null}
        </Card>
      </section>

      <section aria-labelledby="waste-loss-lines">
        <h2
          id="waste-loss-lines"
          className="m-0 mb-2 text-[length:var(--exits-text-md)] font-medium"
        >
          {t("wasteLoss.lines")}
        </h2>
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {entry.lines.map((line) => (
            <li key={line.lineId}>
              <Card
                className="flex flex-col gap-2 p-3"
                data-testid={`waste-loss-line-${line.lineId}`}
              >
                <p className="m-0 font-medium">{line.nameSnapshot}</p>
                <p className="m-0 text-[length:var(--exits-text-sm)]">
                  {line.quantityEntered} {line.unitLabelSnapshot}
                </p>
                {line.lineCostSnapshot != null ? (
                  <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                    <MoneyDisplay amount={line.lineCostSnapshot} />
                  </p>
                ) : line.lineCostSnapshot == null && entry.costStatus !== "Complete" ? (
                  <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                    {t("wasteLoss.lineCostUnavailable")}
                  </p>
                ) : null}
              </Card>
            </li>
          ))}
        </ul>
      </section>

      {allowManage && isPosted ? (
        <Button
          type="button"
          variant="outline"
          className="min-h-11 w-full sm:w-auto"
          disabled={!online || voiding}
          onClick={() => void onVoid()}
          data-testid="waste-loss-void"
        >
          {voiding ? t("wasteLoss.voiding") : t("wasteLoss.void")}
        </Button>
      ) : null}
    </div>
  );
}
