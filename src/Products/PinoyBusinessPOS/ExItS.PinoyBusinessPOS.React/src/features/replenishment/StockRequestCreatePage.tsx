import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useMutation, useQuery } from "@tanstack/react-query";
import { canManageInventory, canManagePurchasing } from "@/access/pos-capabilities";
import {
  getOrganizationInventorySummary,
  listInventory,
  type PosInventoryAccountDto,
} from "@/api/pos/pos-inventory-client";
import { createStockRequest } from "@/api/pos/pos-stock-requests-client";
import { listSupplyRoutesByDestination } from "@/api/pos/pos-supply-routes-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import {
  warehouseOnlyActiveRoutes,
  type CoverageLocation,
} from "@/features/replenishment/supply-coverage-helpers";
import {
  hasConfiguredInternalSource,
  pickPreferredSourceId,
} from "@/features/replenishment/stock-request-helpers";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type StockFilter = "all" | "low" | "out";

function matchesStockFilter(product: PosInventoryAccountDto, filter: StockFilter): boolean {
  if (filter === "low") return product.isLowStock || product.onHandQuantity <= 0;
  if (filter === "out") return product.onHandQuantity <= 0;
  return true;
}

export function StockRequestCreatePage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { boundWorkspace, sessionGrant, workspaces } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);
  const allowPurchase = canManagePurchasing(sessionGrant);
  const [sourceId, setSourceId] = useState<string>("");
  const [notes, setNotes] = useState("");
  const [qtys, setQtys] = useState<Record<string, string>>({});
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [stockFilter, setStockFilter] = useState<StockFilter>("all");

  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedSearch(search.trim().toLowerCase()), 200);
    return () => window.clearTimeout(handle);
  }, [search]);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const orgBranches = useMemo(() => {
    const org = workspaces.find((w) => w.organizationId === boundWorkspace?.organizationId);
    return org?.branches ?? [];
  }, [workspaces, boundWorkspace?.organizationId]);

  const branchNames = useMemo(() => {
    const map = new Map<string, string>();
    for (const b of orgBranches) map.set(b.branchId, b.name);
    return map;
  }, [orgBranches]);

  const locationById = useMemo(() => {
    const map = new Map<string, CoverageLocation>();
    for (const b of orgBranches) {
      map.set(b.branchId, {
        id: b.branchId,
        name: b.name,
        branchType: b.branchType ?? "Retail",
        status: b.isActive ? "Active" : "Inactive",
        areaId: b.areaId,
        areaName: b.areaName,
      });
    }
    return map;
  }, [orgBranches]);

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
    () => warehouseOnlyActiveRoutes(routesQuery.data ?? [], locationById),
    [routesQuery.data, locationById],
  );

  useEffect(() => {
    const preferred = pickPreferredSourceId(activeRoutes);
    if (preferred && !sourceId) setSourceId(preferred);
  }, [activeRoutes, sourceId]);

  const products = useMemo(() => {
    const all = (inventoryQuery.data?.items ?? []).filter((p) => p.isTracked);
    return all.filter((p) => {
      if (!matchesStockFilter(p, stockFilter)) return false;
      if (!debouncedSearch) return true;
      return p.name.toLowerCase().includes(debouncedSearch);
    });
  }, [inventoryQuery.data?.items, stockFilter, debouncedSearch]);

  const warehouseAvailProductIds = useMemo(
    () => products.slice(0, 40).map((p) => p.productId),
    [products],
  );

  const warehouseAvailQuery = useQuery({
    queryKey: [
      "stock-request-warehouse-avail",
      workspace?.organizationId,
      sourceId,
      warehouseAvailProductIds.join(","),
    ],
    enabled: Boolean(workspace && sourceId && warehouseAvailProductIds.length > 0),
    queryFn: async ({ signal }) => {
      const map = new Map<string, number>();
      await Promise.all(
        warehouseAvailProductIds.map(async (productId) => {
          try {
            const summary = await getOrganizationInventorySummary(workspace!, productId, signal);
            const branch = summary.branches.find((b) => b.branchId === sourceId);
            map.set(productId, branch?.availableQuantity ?? 0);
          } catch {
            // leave unset — UI shows em dash
          }
        }),
      );
      return map;
    },
  });

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

  const warehouseQty = (productId: string): string => {
    const value = warehouseAvailQuery.data?.get(productId);
    if (value == null) return "—";
    return String(value);
  };

  return (
    <div className="exits-page flex flex-col gap-3" data-testid="stock-request-create">
      <PageHeader
        title={t("stockRequest.title")}
        description={`${t("stockRequest.forLocation")} ${boundWorkspace?.branchName ?? ""}`}
        backTo="/inventory/stock-requests"
        backLabel={t("stockRequest.listTitle")}
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

      <SearchField
        label={t("stockRequest.search")}
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        placeholder={t("stockRequest.search")}
        data-testid="stock-request-search"
        onClear={() => setSearch("")}
      />

      <ExitsChipBar
        ariaLabel={t("stockRequest.filterLabel")}
        variant="filter"
        items={[
          {
            key: "all",
            label: t("stockRequest.filter.all"),
            state: stockFilter === "all" ? "active" : "idle",
            onSelect: () => setStockFilter("all"),
          },
          {
            key: "low",
            label: t("stockRequest.filter.lowStock"),
            state: stockFilter === "low" ? "active" : "idle",
            onSelect: () => setStockFilter("low"),
          },
          {
            key: "out",
            label: t("stockRequest.filter.outOfStock"),
            state: stockFilter === "out" ? "active" : "idle",
            onSelect: () => setStockFilter("out"),
          },
        ]}
      />

      {/* Desktop grid */}
      <div className="hidden md:block" data-testid="stock-request-product-grid">
        <div
          className="grid grid-cols-[minmax(0,1fr)_6.5rem_7rem_6rem] gap-2 border-b border-border px-1 pb-2 text-[length:var(--exits-text-xs)] font-medium text-muted"
          data-testid="stock-request-grid-headers"
        >
          <span>{t("stockRequest.col.product")}</span>
          <span className="text-right">{t("stockRequest.col.branchStock")}</span>
          <span className="text-right">{t("stockRequest.col.warehouseAvailable")}</span>
          <span className="text-right">{t("stockRequest.col.requestQty")}</span>
        </div>
        <ul className="m-0 flex list-none flex-col p-0">
          {products.map((p) => (
            <li
              key={p.productId}
              className="grid grid-cols-[minmax(0,1fr)_6.5rem_7rem_6rem] items-center gap-2 border-b border-border py-2"
            >
              <div className="min-w-0">
                <div className="truncate font-medium">{p.name}</div>
              </div>
              <div className="text-right text-[length:var(--exits-text-sm)] tabular-nums">
                {p.onHandQuantity}
              </div>
              <div className="text-right text-[length:var(--exits-text-sm)] tabular-nums text-muted">
                {warehouseQty(p.productId)}
              </div>
              <div className="flex justify-end">
                <input
                  className={cn("exits-input w-24 max-w-[6rem] shrink-0")}
                  inputMode="decimal"
                  placeholder="0"
                  value={qtys[p.productId] ?? ""}
                  onChange={(e) => setQtys((prev) => ({ ...prev, [p.productId]: e.target.value }))}
                  data-testid={`stock-request-qty-${p.productId}`}
                  aria-label={`${t("stockRequest.col.requestQty")} ${p.name}`}
                />
              </div>
            </li>
          ))}
        </ul>
      </div>

      {/* Mobile cards */}
      <ul className="m-0 flex list-none flex-col gap-2 p-0 md:hidden" data-testid="stock-request-product-cards">
        {products.map((p) => (
          <li
            key={p.productId}
            className="rounded-[var(--exits-radius-md)] border border-border p-3"
            data-testid={`stock-request-card-${p.productId}`}
          >
            <div className="font-medium">{p.name}</div>
            <div className="mt-1 flex flex-wrap gap-x-3 gap-y-0.5 text-[length:var(--exits-text-xs)] text-muted">
              <span>
                {t("stockRequest.col.branchStock")}: {p.onHandQuantity}
              </span>
              <span>
                {t("stockRequest.col.warehouseAvailable")}: {warehouseQty(p.productId)}
              </span>
            </div>
            <label className="mt-2 flex items-center justify-between gap-2 text-[length:var(--exits-text-sm)]">
              <span>{t("stockRequest.col.requestQty")}</span>
              <input
                className={cn("exits-input w-24 max-w-[6rem] shrink-0")}
                inputMode="decimal"
                placeholder="0"
                value={qtys[p.productId] ?? ""}
                onChange={(e) => setQtys((prev) => ({ ...prev, [p.productId]: e.target.value }))}
                data-testid={`stock-request-qty-mobile-${p.productId}`}
              />
            </label>
          </li>
        ))}
      </ul>

      {products.length === 0 ? (
        <EmptyState title={t("stockRequest.emptyProducts")} detail={t("stockRequest.emptyProductsDetail")} />
      ) : null}

      <label className="flex max-h-24 flex-col gap-1 text-[length:var(--exits-text-sm)]">
        <span>{t("stockRequest.notes")}</span>
        <textarea
          className="exits-input max-h-20 resize-none"
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          rows={2}
          data-testid="stock-request-notes"
        />
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
