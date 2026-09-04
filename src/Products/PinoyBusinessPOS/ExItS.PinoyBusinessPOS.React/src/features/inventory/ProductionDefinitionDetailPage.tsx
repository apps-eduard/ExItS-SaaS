import { useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { canManageInventory } from "@/access/pos-capabilities";
import { getCatalogProduct } from "@/api/pos/pos-catalog-client";
import { PosApiError } from "@/api/pos/pos-http";
import {
  getProductionDefinition,
  setProductionDefinitionActive,
} from "@/api/pos/pos-production-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { ActorAttribution } from "@/features/actors/ActorAttribution";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  formatProductionDate,
  productionDefinitionStatusLabelKey,
} from "@/features/inventory/production-labels";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function ProductionDefinitionDetailPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { definitionId } = useParams<{ definitionId: string }>();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const queryClient = useQueryClient();
  const allowManage = canManageInventory(sessionGrant);
  const [error, setError] = useState<string | null>(null);
  const [toggling, setToggling] = useState(false);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: ["production-definition", workspace?.organizationId, definitionId],
    enabled: Boolean(workspace) && Boolean(definitionId) && online,
    queryFn: ({ signal }) => getProductionDefinition(workspace!, definitionId!, signal),
  });

  const outputQuery = useQuery({
    queryKey: ["catalog-product", workspace?.organizationId, query.data?.outputProductId],
    enabled: Boolean(workspace) && Boolean(query.data?.outputProductId) && online,
    queryFn: ({ signal }) => getCatalogProduct(workspace!, query.data!.outputProductId, signal),
  });

  const materialIds = (query.data?.components ?? []).map((c) => c.materialProductId);
  const materialsQuery = useQuery({
    queryKey: ["production-definition-materials", workspace?.organizationId, definitionId, materialIds],
    enabled: Boolean(workspace) && materialIds.length > 0 && online,
    queryFn: async ({ signal }) => {
      const entries = await Promise.all(
        materialIds.map(async (id) => {
          try {
            const product = await getCatalogProduct(workspace!, id, signal);
            return [id, product.name] as const;
          } catch {
            return [id, id] as const;
          }
        }),
      );
      return Object.fromEntries(entries) as Record<string, string>;
    },
  });

  const actors = useActorDirectory(workspace?.organizationId, [
    query.data?.createdByUserId,
    query.data?.updatedByUserId,
  ]);

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }
  if (!definitionId) {
    return (
      <ErrorState title={t("production.errorTitle")} detail={t("production.setups.notFound")} />
    );
  }
  if (query.isLoading) {
    return <LoadingState label={t("production.loading")} />;
  }
  if (query.isError || !query.data) {
    return (
      <ErrorState title={t("production.errorTitle")} detail={t("production.setups.notFound")} />
    );
  }

  const definition = query.data;
  const outputName = outputQuery.data?.name ?? definition.outputProductId;

  async function onToggleActive() {
    if (!workspace || !definitionId || !allowManage || !online || toggling) {
      return;
    }
    setToggling(true);
    setError(null);
    try {
      const updated = await setProductionDefinitionActive(
        workspace,
        definitionId,
        !definition.isActive,
      );
      queryClient.setQueryData(
        ["production-definition", workspace.organizationId, definitionId],
        updated,
      );
      await queryClient.invalidateQueries({ queryKey: ["production-definitions"] });
    } catch (err) {
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("production.setups.saveFailed"))
          : t("production.setups.saveFailed"),
      );
    } finally {
      setToggling(false);
    }
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="production-definition-detail-page">
      <PageHeader
        title={definition.name}
        description={t("production.setups.detailLede")}
        backTo="/inventory/production/setups"
        backLabel={t("production.backSetups")}
        backTestId="page-header-back-production-setups"
      />

      {error ? <ErrorState title={t("production.errorTitle")} detail={error} /> : null}

      <Card className="flex flex-col gap-3 p-3">
        <div className="flex flex-wrap items-center gap-2">
          <StatusChip tone={definition.isActive ? "success" : "warning"}>
            {t(productionDefinitionStatusLabelKey(definition.status))}
          </StatusChip>
          <span className="text-[length:var(--exits-text-sm)] text-muted">
            {formatProductionDate(definition.createdAtUtc)}
          </span>
        </div>
        <dl className="m-0 grid gap-3 sm:grid-cols-2">
          <div>
            <dt className="text-[length:var(--exits-text-sm)] text-muted">
              {t("production.setups.outputProduct")}
            </dt>
            <dd className="m-0" data-testid="production-setup-output">
              {outputName}
            </dd>
          </div>
          <div>
            <dt className="text-[length:var(--exits-text-sm)] text-muted">
              {t("production.setups.outputQuantity")}
            </dt>
            <dd className="m-0">{definition.outputQuantityEntered}</dd>
          </div>
          <div>
            <dt className="text-[length:var(--exits-text-sm)] text-muted">
              {t("production.setups.revisionLabel")}
            </dt>
            <dd className="m-0">{definition.revision}</dd>
          </div>
        </dl>
        <ActorAttribution
          labelKey="common.createdBy"
          actorId={definition.createdByUserId}
          occurredAtUtc={definition.createdAtUtc}
          resolved={actors.resolve(definition.createdByUserId)}
          isLoading={actors.isResolving}
          testId="production-setup-created-by"
        />
      </Card>

      <section>
        <h2 className="m-0 mb-2 text-[length:var(--exits-text-md)] font-medium">
          {t("production.setups.materials")}
        </h2>
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {definition.components.map((component) => (
            <li key={component.componentId}>
              <Card className="flex flex-col gap-1 p-3">
                <p className="m-0 font-medium">
                  {materialsQuery.data?.[component.materialProductId] ??
                    component.materialProductId}
                </p>
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {component.quantityEntered}
                </p>
              </Card>
            </li>
          ))}
        </ul>
      </section>

      <div className="flex flex-col gap-2 sm:flex-row">
        {allowManage ? (
          <Button asChild disabled={!online}>
            <Link to={`/inventory/production/setups/${definitionId}/edit`}>
              {t("production.setups.edit")}
            </Link>
          </Button>
        ) : null}
        {allowManage ? (
          <Button
            type="button"
            variant="outline"
            disabled={!online || toggling}
            onClick={() => void onToggleActive()}
            data-testid="production-setup-toggle-active"
          >
            {definition.isActive
              ? t("production.setups.setInactive")
              : t("production.setups.setActive")}
          </Button>
        ) : null}
        {allowManage && definition.isActive ? (
          <Button asChild variant="outline" disabled={!online}>
            <Link to={`/inventory/production/produce?definitionId=${definitionId}`}>
              {t("production.homeProduce")}
            </Link>
          </Button>
        ) : null}
      </div>
    </div>
  );
}
