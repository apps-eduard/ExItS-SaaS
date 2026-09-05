import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useMutation, useQuery } from "@tanstack/react-query";
import { canManageInventory, canManagePurchasing } from "@/access/pos-capabilities";
import { listInventory } from "@/api/pos/pos-inventory-client";
import { createStockRequest } from "@/api/pos/pos-stock-requests-client";
import { listSupplyRoutesByDestination } from "@/api/pos/pos-supply-routes-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import {
  hasConfiguredInternalSource,
  pickPreferredSourceId,
} from "@/features/replenishment/stock-request-helpers";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function StockRequestCreatePage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { boundWorkspace, sessionGrant, workspaces } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);
  const allowPurchase = canManagePurchasing(sessionGrant);
  const [sourceId, setSourceId] = useState<string>("");
  const [notes, setNotes] = useState("");
  const [qtys, setQtys] = useState<Record<string, string>>({});

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const branchNames = useMemo(() => {
    const org = workspaces.find((w) => w.organizationId === boundWorkspace?.organizationId);
    const map = new Map<string, string>();
    for (const b of org?.branches ?? []) map.set(b.branchId, b.name);
    return map;
  }, [workspaces, boundWorkspace?.organizationId]);

  const routesQuery = useQuery({
    queryKey: ["supply-routes-dest", workspace?.organizationId, workspace?.branchId],
    enabled: Boolean(workspace?.branchId),
    queryFn: ({ signal }) => listSupplyRoutesByDestination(workspace!, workspace!.branchId!, signal),
  });

  const inventoryQuery = useQuery({
    queryKey: ["inventory-for-stock-request", workspace?.organizationId, workspace?.branchId],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) => listInventory(workspace!, { pageSize: 100 }, signal),
  });

  const activeRoutes = useMemo(
    () => (routesQuery.data ?? []).filter((r) => r.isActive),
    [routesQuery.data],
  );

  useEffect(() => {
    const preferred = pickPreferredSourceId(activeRoutes);
    if (preferred && !sourceId) setSourceId(preferred);
  }, [activeRoutes, sourceId]);

  const mutation = useMutation({
    mutationFn: async () => {
      if (!workspace?.branchId || !sourceId) throw new Error("missing");
      const lines = Object.entries(qtys)
        .map(([productId, raw]) => ({ productId, requestedQuantity: Number(raw) }))
        .filter((l) => Number.isFinite(l.requestedQuantity) && l.requestedQuantity > 0);
      if (lines.length === 0) throw new Error("lines");
      return createStockRequest(workspace, {
        destinationLocationId: workspace.branchId,
        requestedSourceLocationId: sourceId,
        lines,
        notes: notes.trim() || null,
      });
    },
    onSuccess: (dto) => navigate(`/inventory/stock-requests/${dto.stockRequestId}`),
  });

  if (!workspace) {
    return <EmptyState title={t("stockRequest.title")} detail={t("stockRequest.needBranch")} />;
  }

  if (!allowManage) {
    return <EmptyState title={t("stockRequest.title")} detail={t("stockRequest.denied")} />;
  }

  if (routesQuery.isLoading || inventoryQuery.isLoading) {
    return <LoadingState label={t("stockRequest.loading")} />;
  }

  if (routesQuery.isError) {
    return <ErrorState title={t("stockRequest.loadError")} detail={t("stockRequest.loadError")} />;
  }

  if (!hasConfiguredInternalSource(activeRoutes)) {
    return (
      <div className="exits-page flex flex-col gap-3" data-testid="stock-request-no-source">
        <PageHeader
          title={t("stockRequest.title")}
          description={t("stockRequest.lede")}
          backTo={pageBackNav.inventory.to}
          backLabel={t(pageBackNav.inventory.labelKey)}
        />
        <EmptyState
          title={t("stockRequest.noSource")}
          detail={t("stockRequest.noSourceDetail")}
          action={
            allowPurchase ? (
              <div className="flex flex-wrap gap-2">
                <Button asChild variant="outline">
                  <Link to="/purchasing/orders/new">{t("stockRequest.createPo")}</Link>
                </Button>
                <Button asChild variant="outline">
                  <Link to="/purchasing/receive-stock">{t("stockRequest.receiveStock")}</Link>
                </Button>
              </div>
            ) : undefined
          }
        />
      </div>
    );
  }

  return (
    <div className="exits-page flex flex-col gap-3" data-testid="stock-request-create">
      <PageHeader
        title={t("stockRequest.title")}
        description={`${t("stockRequest.forLocation")} ${boundWorkspace?.branchName ?? ""}`}
        backTo={pageBackNav.inventory.to}
        backLabel={t(pageBackNav.inventory.labelKey)}
      />

      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        <span>{t("stockRequest.supplyFrom")}</span>
        <select
          className="exits-input"
          value={sourceId}
          onChange={(e) => setSourceId(e.target.value)}
          data-testid="stock-request-source"
        >
          {activeRoutes.map((r) => (
            <option key={r.routeId} value={r.sourceLocationId}>
              {branchNames.get(r.sourceLocationId) ?? r.sourceLocationId}
              {r.isPreferred ? ` (${t("stockRequest.preferred")})` : ""}
            </option>
          ))}
        </select>
      </label>

      <ul className="flex flex-col gap-2">
        {(inventoryQuery.data?.items ?? [])
          .filter((p) => p.isTracked)
          .map((p) => (
            <li key={p.productId} className="flex items-center gap-2 border-b border-border py-2">
              <div className="min-w-0 flex-1">
                <div className="font-medium">{p.name}</div>
                <div className="text-[length:var(--exits-text-xs)] text-muted">
                  {t("stockRequest.availableHere")}: {p.onHandQuantity}
                </div>
              </div>
              <input
                className="exits-input w-24"
                inputMode="decimal"
                placeholder="0"
                value={qtys[p.productId] ?? ""}
                onChange={(e) => setQtys((prev) => ({ ...prev, [p.productId]: e.target.value }))}
                data-testid={`stock-request-qty-${p.productId}`}
              />
            </li>
          ))}
      </ul>

      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        <span>{t("stockRequest.notes")}</span>
        <textarea className="exits-input" value={notes} onChange={(e) => setNotes(e.target.value)} rows={2} />
      </label>

      <Button
        type="button"
        disabled={mutation.isPending || !sourceId}
        onClick={() => mutation.mutate()}
        data-testid="stock-request-submit"
      >
        {t("stockRequest.submit")}
      </Button>
      {mutation.isError ? (
        <p className="text-danger text-[length:var(--exits-text-sm)]">{t("stockRequest.submitError")}</p>
      ) : null}
    </div>
  );
}
