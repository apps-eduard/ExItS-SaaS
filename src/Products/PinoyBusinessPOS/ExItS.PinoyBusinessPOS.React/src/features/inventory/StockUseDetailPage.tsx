import { useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { canManageInventory } from "@/access/pos-capabilities";
import { PosApiError } from "@/api/pos/pos-http";
import { getStockUse, voidStockUse } from "@/api/pos/pos-stock-use-client";
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
  formatStockUseOccurredDate,
  stockUseReasonLabelKey,
  stockUseStatusLabelKey,
  sumStockUseLineCosts,
} from "@/features/inventory/stock-use-labels";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function StockUseDetailPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { stockUseId } = useParams<{ stockUseId: string }>();
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
    queryKey: ["stock-use", workspace?.organizationId, stockUseId],
    enabled: Boolean(workspace) && Boolean(stockUseId) && online,
    queryFn: ({ signal }) => getStockUse(workspace!, stockUseId!, signal),
  });

  const actors = useActorDirectory(workspace?.organizationId, [
    query.data?.createdByUserId,
    query.data?.voidedByUserId,
  ]);

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }
  if (!stockUseId) {
    return <ErrorState title={t("stockUse.errorTitle")} detail={t("stockUse.notFound")} />;
  }
  if (query.isLoading) {
    return <LoadingState label={t("stockUse.loading")} />;
  }
  if (query.isError || !query.data) {
    return <ErrorState title={t("stockUse.errorTitle")} detail={t("stockUse.notFound")} />;
  }

  const entry = query.data;
  const isPosted = entry.status === "Posted";
  const isVoided = entry.status === "Voided";
  const notes = entry.notes?.trim();
  const totalCost = sumStockUseLineCosts(entry.lines);

  async function onVoid() {
    if (!workspace || !stockUseId || !allowManage || !online || voiding || !isPosted) {
      return;
    }
    if (!window.confirm(t("stockUse.voidConfirm"))) {
      return;
    }
    setVoiding(true);
    setError(null);
    try {
      const updated = await voidStockUse(workspace, stockUseId);
      queryClient.setQueryData(["stock-use", workspace.organizationId, stockUseId], updated);
      await queryClient.invalidateQueries({ queryKey: ["stock-uses"] });
      await queryClient.invalidateQueries({ queryKey: ["inventory"] });
    } catch (err) {
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("stockUse.voidFailed"))
          : t("stockUse.voidFailed"),
      );
    } finally {
      setVoiding(false);
    }
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="stock-use-detail-page">
      <PageHeader
        title={entry.stockUseNumber}
        description={t("stockUse.detailLede")}
        backTo="/inventory/stock-use"
        backLabel={t("stockUse.backList")}
        backTestId="page-header-back-stock-use"
      />

      {error ? <ErrorState title={t("stockUse.errorTitle")} detail={error} /> : null}

      <section aria-labelledby="stock-use-info">
        <h2
          id="stock-use-info"
          className="m-0 mb-2 text-[length:var(--exits-text-md)] font-medium"
        >
          {t("stockUse.usedByBusiness")}
        </h2>
        <Card className="flex flex-col gap-3 p-3">
          <div className="flex flex-wrap items-center gap-2">
            <StatusChip tone={isVoided ? "danger" : "success"}>
              {t(stockUseStatusLabelKey(entry.status))}
            </StatusChip>
            <span className="text-[length:var(--exits-text-sm)] text-muted">
              {formatStockUseOccurredDate(entry.occurredAtUtc)}
            </span>
          </div>
          <dl className="m-0 grid gap-3 sm:grid-cols-2">
            <div>
              <dt className="text-[length:var(--exits-text-sm)] text-muted">
                {t("stockUse.reason")}
              </dt>
              <dd className="m-0" data-testid="stock-use-detail-reason">
                {t(stockUseReasonLabelKey(entry.reason))}
              </dd>
            </div>
            {entry.referenceNumber ? (
              <div>
                <dt className="text-[length:var(--exits-text-sm)] text-muted">
                  {t("stockUse.reference")}
                </dt>
                <dd className="m-0">{entry.referenceNumber}</dd>
              </div>
            ) : null}
            {totalCost != null ? (
              <div>
                <dt className="text-[length:var(--exits-text-sm)] text-muted">
                  {t("stockUse.estimatedCost")}
                </dt>
                <dd className="m-0" data-testid="stock-use-detail-cost">
                  <MoneyDisplay amount={totalCost} />
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
            testId="stock-use-recorded-by"
          />
          {isVoided && entry.voidedByUserId ? (
            <ActorAttribution
              labelKey="common.voidedBy"
              actorId={entry.voidedByUserId}
              occurredAtUtc={entry.voidedAtUtc ?? undefined}
              resolved={actors.resolve(entry.voidedByUserId)}
              isLoading={actors.isResolving}
              testId="stock-use-voided-by"
            />
          ) : null}
          {notes ? (
            <div data-testid="stock-use-detail-notes">
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("stockUse.notes")}
              </p>
              <p className="mt-1 mb-0 whitespace-pre-wrap text-[length:var(--exits-text-sm)]">
                {notes}
              </p>
            </div>
          ) : null}
        </Card>
      </section>

      <section aria-labelledby="stock-use-lines">
        <h2
          id="stock-use-lines"
          className="m-0 mb-2 text-[length:var(--exits-text-md)] font-medium"
        >
          {t("stockUse.usedStock")}
        </h2>
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {entry.lines.map((line) => (
            <li key={line.lineId}>
              <Card
                className="flex flex-col gap-2 p-3"
                data-testid={`stock-use-line-${line.lineId}`}
              >
                <p className="m-0 font-medium">{line.nameSnapshot}</p>
                <p className="m-0 text-[length:var(--exits-text-sm)]">
                  {line.quantityEntered} {line.unitLabelSnapshot}
                </p>
                {line.lineCostSnapshot != null ? (
                  <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                    <MoneyDisplay amount={line.lineCostSnapshot} />
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
          data-testid="stock-use-void"
        >
          {voiding ? t("stockUse.voiding") : t("stockUse.void")}
        </Button>
      ) : null}
    </div>
  );
}
