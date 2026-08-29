import { useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { canManageInventory } from "@/access/pos-capabilities";
import { PosApiError } from "@/api/pos/pos-http";
import { getProductionRun, voidProductionRun } from "@/api/pos/pos-production-client";
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
  formatProductionDate,
  productionCostStatusLabelKey,
  productionRunStatusLabelKey,
} from "@/features/inventory/production-labels";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function ProductionRunDetailPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { runId } = useParams<{ runId: string }>();
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
    queryKey: ["production-run", workspace?.organizationId, runId],
    enabled: Boolean(workspace) && Boolean(runId) && online,
    queryFn: ({ signal }) => getProductionRun(workspace!, runId!, signal),
  });

  const actors = useActorDirectory(workspace?.organizationId, [
    query.data?.createdByUserId,
    query.data?.voidedByUserId,
  ]);

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }
  if (!runId) {
    return (
      <ErrorState title={t("production.errorTitle")} detail={t("production.runs.notFound")} />
    );
  }
  if (query.isLoading) {
    return <LoadingState label={t("production.loading")} />;
  }
  if (query.isError || !query.data) {
    return (
      <ErrorState title={t("production.errorTitle")} detail={t("production.runs.notFound")} />
    );
  }

  const entry = query.data;
  const isPosted = entry.status === "Posted";
  const isVoided = entry.status === "Voided";
  const notes = entry.notes?.trim();

  async function onVoid() {
    if (!workspace || !runId || !allowManage || !online || voiding || !isPosted) {
      return;
    }
    if (!window.confirm(t("production.runs.voidConfirm"))) {
      return;
    }
    setVoiding(true);
    setError(null);
    try {
      const updated = await voidProductionRun(workspace, runId);
      queryClient.setQueryData(["production-run", workspace.organizationId, runId], updated);
      await queryClient.invalidateQueries({ queryKey: ["production-runs"] });
      await queryClient.invalidateQueries({ queryKey: ["inventory"] });
    } catch (err) {
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("production.runs.voidFailed"))
          : t("production.runs.voidFailed"),
      );
    } finally {
      setVoiding(false);
    }
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="production-run-detail-page">
      <PageHeader
        title={entry.productionNumber}
        description={t("production.runs.detailLede")}
        backTo="/inventory/production/runs"
        backLabel={t("production.backRuns")}
        backTestId="page-header-back-production-runs"
      />

      {error ? <ErrorState title={t("production.errorTitle")} detail={error} /> : null}

      <Card className="flex flex-col gap-3 p-3">
        <div className="flex flex-wrap items-center gap-2">
          <StatusChip tone={isVoided ? "danger" : "success"}>
            {t(productionRunStatusLabelKey(entry.status))}
          </StatusChip>
          <span className="text-[length:var(--exits-text-sm)] text-muted">
            {formatProductionDate(entry.producedAtUtc)}
          </span>
        </div>
        <dl className="m-0 grid gap-3 sm:grid-cols-2">
          <div>
            <dt className="text-[length:var(--exits-text-sm)] text-muted">
              {t("production.setups.name")}
            </dt>
            <dd className="m-0">{entry.productionDefinitionNameSnapshot}</dd>
          </div>
          <div>
            <dt className="text-[length:var(--exits-text-sm)] text-muted">
              {t("production.runs.output")}
            </dt>
            <dd className="m-0" data-testid="production-run-output">
              {entry.outputNameSnapshot} · {entry.outputQuantityEntered}{" "}
              {entry.outputUnitLabelSnapshot}
            </dd>
          </div>
          {entry.referenceNumber ? (
            <div>
              <dt className="text-[length:var(--exits-text-sm)] text-muted">
                {t("production.produce.reference")}
              </dt>
              <dd className="m-0">{entry.referenceNumber}</dd>
            </div>
          ) : null}
          <div>
            <dt className="text-[length:var(--exits-text-sm)] text-muted">
              {t("production.runs.costStatus")}
            </dt>
            <dd className="m-0">{t(productionCostStatusLabelKey(entry.costStatus))}</dd>
          </div>
          {entry.totalMaterialCost != null ? (
            <div>
              <dt className="text-[length:var(--exits-text-sm)] text-muted">
                {t("production.runs.totalMaterialCost")}
              </dt>
              <dd className="m-0" data-testid="production-run-material-cost">
                <MoneyDisplay amount={entry.totalMaterialCost} />
              </dd>
            </div>
          ) : null}
          {entry.outputBaseUnitCost != null ? (
            <div>
              <dt className="text-[length:var(--exits-text-sm)] text-muted">
                {t("production.runs.outputUnitCost")}
              </dt>
              <dd className="m-0" data-testid="production-run-output-unit-cost">
                <MoneyDisplay amount={entry.outputBaseUnitCost} />
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
          testId="production-run-recorded-by"
        />
        {isVoided && entry.voidedByUserId ? (
          <ActorAttribution
            labelKey="common.voidedBy"
            actorId={entry.voidedByUserId}
            occurredAtUtc={entry.voidedAtUtc ?? undefined}
            resolved={actors.resolve(entry.voidedByUserId)}
            isLoading={actors.isResolving}
            testId="production-run-voided-by"
          />
        ) : null}
        {notes ? (
          <div>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("production.produce.notes")}
            </p>
            <p className="mt-1 mb-0 whitespace-pre-wrap text-[length:var(--exits-text-sm)]">
              {notes}
            </p>
          </div>
        ) : null}
      </Card>

      <section>
        <h2 className="m-0 mb-2 text-[length:var(--exits-text-md)] font-medium">
          {t("production.runs.materials")}
        </h2>
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {entry.materials.map((material) => (
            <li key={material.materialId}>
              <Card className="flex flex-col gap-2 p-3">
                <p className="m-0 font-medium">{material.nameSnapshot}</p>
                <p className="m-0 text-[length:var(--exits-text-sm)]">
                  {t("production.produce.expected")}: {material.expectedQuantityEntered}{" "}
                  {material.unitLabelSnapshot}
                </p>
                <p className="m-0 text-[length:var(--exits-text-sm)]">
                  {t("production.produce.actual")}: {material.actualQuantityEntered}{" "}
                  {material.unitLabelSnapshot}
                </p>
                {material.lineCostSnapshot != null ? (
                  <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                    <MoneyDisplay amount={material.lineCostSnapshot} />
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
          data-testid="production-run-void"
        >
          {voiding ? t("production.runs.voiding") : t("production.runs.void")}
        </Button>
      ) : null}
    </div>
  );
}
