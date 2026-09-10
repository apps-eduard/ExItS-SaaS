import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Settings2 } from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import { listCatalogCategories } from "@/api/pos/pos-catalog-client";
import {
  bulkSetInventoryReorder,
  clearInventoryReorderOverride,
  getInventoryReorderDefault,
  listInventory,
  setInventoryReorder,
  setInventoryReorderDefault,
  type BulkSetInventoryReorderMode,
  type PosInventoryAccountDto,
} from "@/api/pos/pos-inventory-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { BranchRequiredPanel } from "@/features/workspace/BranchRequiredPanel";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { OrganizationQueryGate } from "@/runtime/OrganizationQueryGate";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PAGE_SIZE = 50;

type StockStatusFilter = "All" | "InStock" | "LowStock" | "OutOfStock";
type MonitoringFilter = "All" | "BranchDefault" | "Custom" | "NotMonitored";
type EditMode = "BranchDefault" | "Custom" | "NotMonitored";

function formatQty(value: number | null | undefined): string {
  if (value == null) return "—";
  return Number.isInteger(value) ? String(value) : String(value);
}

function monitoringLabel(
  mode: string | null | undefined,
  t: (key: "lowStockSettings.monitoringBranchDefault" | "lowStockSettings.monitoringCustom" | "lowStockSettings.monitoringNotMonitored") => string,
): string {
  if (mode === "Custom") return t("lowStockSettings.monitoringCustom");
  if (mode === "NotMonitored") return t("lowStockSettings.monitoringNotMonitored");
  return t("lowStockSettings.monitoringBranchDefault");
}

function statusLabel(item: PosInventoryAccountDto): string {
  if (item.monitoringMode === "NotMonitored") return "—";
  return item.stockStatus || "—";
}

export function LowStockSettingsPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);

  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [stockStatus, setStockStatus] = useState<StockStatusFilter>("All");
  const [monitoring, setMonitoring] = useState<MonitoringFilter>("All");
  const [categoryId, setCategoryId] = useState("");
  const [page, setPage] = useState(1);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(() => new Set());
  const [selectAllFiltered, setSelectAllFiltered] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [okMessage, setOkMessage] = useState<string | null>(null);

  const [editingDefault, setEditingDefault] = useState(false);
  const [defaultLevel, setDefaultLevel] = useState("");
  const [defaultQty, setDefaultQty] = useState("");

  const [bulkOpen, setBulkOpen] = useState(false);
  const [bulkLevel, setBulkLevel] = useState("10");
  const [bulkQty, setBulkQty] = useState("");

  const [editItem, setEditItem] = useState<PosInventoryAccountDto | null>(null);
  const [editMode, setEditMode] = useState<EditMode>("Custom");
  const [editLevel, setEditLevel] = useState("");
  const [editQty, setEditQty] = useState("");

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  useEffect(() => {
    setPage(1);
    setSelectedIds(new Set());
    setSelectAllFiltered(false);
  }, [debounced, stockStatus, monitoring, categoryId, boundWorkspace?.branchId]);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const listQuery = useQuery({
    queryKey: [
      "inventory",
      "low-stock-settings",
      workspace?.organizationId,
      workspace?.branchId,
      debounced,
      stockStatus,
      monitoring,
      categoryId,
      page,
    ],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) =>
      listInventory(
        workspace!,
        {
          search: debounced || undefined,
          tracked: true,
          page,
          pageSize: PAGE_SIZE,
          stockStatus: stockStatus === "All" ? undefined : stockStatus,
          monitoringMode: monitoring === "All" ? undefined : monitoring,
          categoryId: categoryId || undefined,
        },
        signal,
      ),
  });

  const defaultQuery = useQuery({
    queryKey: ["inventory", "reorder-default", workspace?.organizationId, workspace?.branchId],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) => getInventoryReorderDefault(workspace!, signal),
  });

  const categoriesQuery = useQuery({
    queryKey: ["catalog", "categories", workspace?.organizationId],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) => listCatalogCategories(workspace!, { page: 1, pageSize: 200 }, signal),
  });

  const items = listQuery.data?.items ?? [];
  const totalCount = listQuery.data?.totalCount ?? 0;
  const pageCount = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const selectedCount = selectAllFiltered ? totalCount : selectedIds.size;
  const pageIds = items.map((item) => item.productId);
  const allPageSelected = pageIds.length > 0 && pageIds.every((id) => selectedIds.has(id));

  useEffect(() => {
    if (!editingDefault && defaultQuery.data) {
      setDefaultLevel(
        defaultQuery.data.reorderLevel == null ? "" : String(defaultQuery.data.reorderLevel),
      );
      setDefaultQty(
        defaultQuery.data.reorderQuantity == null ? "" : String(defaultQuery.data.reorderQuantity),
      );
    }
  }, [defaultQuery.data, editingDefault]);

  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey: ["inventory", "low-stock-settings"] });
    await queryClient.invalidateQueries({ queryKey: ["inventory", "reorder-default"] });
  };

  const defaultMutation = useMutation({
    mutationFn: () =>
      setInventoryReorderDefault(workspace!, {
        reorderLevel: defaultLevel.trim() === "" ? null : Number(defaultLevel),
        reorderQuantity: defaultQty.trim() === "" ? null : Number(defaultQty),
      }),
    onSuccess: async () => {
      setEditingDefault(false);
      setOkMessage(t("lowStockSettings.defaultSaved"));
      setError(null);
      await invalidate();
    },
    onError: (err) => {
      setError(err instanceof PosApiError ? err.message : t("error.title"));
    },
  });

  const runBulk = useMutation({
    mutationFn: (mode: BulkSetInventoryReorderMode) => {
      const body =
        selectAllFiltered
          ? {
              mode,
              applyToFiltered: true,
              search: debounced || undefined,
              stockStatus: stockStatus === "All" ? undefined : stockStatus,
              monitoringMode: monitoring === "All" ? undefined : monitoring,
              categoryId: categoryId || undefined,
              reorderLevel: mode === "Custom" ? Number(bulkLevel) : null,
              reorderQuantity:
                mode === "Custom" && bulkQty.trim() !== "" ? Number(bulkQty) : null,
              reason: `Bulk ${mode}`,
            }
          : {
              mode,
              productIds: [...selectedIds],
              applyToFiltered: false,
              reorderLevel: mode === "Custom" ? Number(bulkLevel) : null,
              reorderQuantity:
                mode === "Custom" && bulkQty.trim() !== "" ? Number(bulkQty) : null,
              reason: `Bulk ${mode}`,
            };
      return bulkSetInventoryReorder(workspace!, body);
    },
    onSuccess: async (result) => {
      setBulkOpen(false);
      setSelectedIds(new Set());
      setSelectAllFiltered(false);
      setOkMessage(
        t("lowStockSettings.bulkDone")
          .replace("{ok}", String(result.succeededCount))
          .replace("{skip}", String(result.skippedCount))
          .replace("{fail}", String(result.failedCount)),
      );
      setError(null);
      await invalidate();
    },
    onError: (err) => {
      setError(err instanceof PosApiError ? err.message : t("error.title"));
    },
  });

  const editMutation = useMutation({
    mutationFn: async () => {
      if (!editItem || !workspace) throw new Error("Missing product");
      if (editMode === "BranchDefault") {
        return clearInventoryReorderOverride(workspace, editItem.productId);
      }
      if (editMode === "NotMonitored") {
        return setInventoryReorder(workspace, editItem.productId, {
          reorderLevel: null,
          reorderQuantity: null,
          reason: "Don't monitor",
        });
      }
      return setInventoryReorder(workspace, editItem.productId, {
        reorderLevel: Number(editLevel),
        reorderQuantity: editQty.trim() === "" ? null : Number(editQty),
        reason: "Custom low stock level",
      });
    },
    onSuccess: async () => {
      setEditItem(null);
      setOkMessage(t("lowStockSettings.rowSaved"));
      setError(null);
      await invalidate();
    },
    onError: (err) => {
      setError(err instanceof PosApiError ? err.message : t("error.title"));
    },
  });

  if (!boundWorkspace?.branchId) {
    return <BranchRequiredPanel title={t("lowStockSettings.title")} />;
  }

  const togglePageSelection = () => {
    setSelectAllFiltered(false);
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (allPageSelected) {
        for (const id of pageIds) next.delete(id);
      } else {
        for (const id of pageIds) next.add(id);
      }
      return next;
    });
  };

  const openEdit = (item: PosInventoryAccountDto) => {
    setEditItem(item);
    const mode =
      item.monitoringMode === "Custom"
        ? "Custom"
        : item.monitoringMode === "NotMonitored"
          ? "NotMonitored"
          : "BranchDefault";
    setEditMode(mode);
    setEditLevel(item.reorderLevel == null ? "" : String(item.reorderLevel));
    setEditQty(item.reorderQuantity == null ? "" : String(item.reorderQuantity));
  };

  return (
    <div
      className="low-stock-settings-page exits-page flex h-full min-h-0 min-w-0 flex-col gap-2.5 overflow-hidden"
      data-testid="low-stock-settings-page"
    >
      <div className="shrink-0 flex min-w-0 flex-col gap-2.5">
        <PageHeader
          title={t("lowStockSettings.title")}
          subtitle={t("lowStockSettings.branchLabel").replace(
            "{branch}",
            boundWorkspace.branchName ?? boundWorkspace.branchId,
          )}
          backTo="/inventory"
          backLabel={t("inventory.open")}
        />

        {error ? <ErrorState title={t("error.title")} detail={error} /> : null}
        {okMessage ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[color:var(--exits-success)]" role="status">
            {okMessage}
          </p>
        ) : null}

        <section
          className="rounded-[var(--exits-radius-md)] border border-[color:var(--exits-border)] bg-[color:var(--exits-surface)] p-3"
          data-testid="low-stock-branch-default"
        >
          <div className="flex flex-wrap items-start justify-between gap-2">
            <div>
              <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                {t("lowStockSettings.branchDefault")}
              </h2>
              {!editingDefault ? (
                <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-[color:var(--exits-muted)]">
                  {t("lowStockSettings.lowStockAt")}: {formatQty(defaultQuery.data?.reorderLevel)} ·{" "}
                  {t("lowStockSettings.reorderQty")}: {formatQty(defaultQuery.data?.reorderQuantity)}
                </p>
              ) : (
                <div className="mt-2 flex flex-wrap gap-2">
                  <label className="grid gap-1 text-[length:var(--exits-text-xs)]">
                    {t("lowStockSettings.lowStockAt")}
                    <Input
                      value={defaultLevel}
                      onChange={(e) => setDefaultLevel(e.target.value)}
                      inputMode="decimal"
                      data-testid="branch-default-level"
                    />
                  </label>
                  <label className="grid gap-1 text-[length:var(--exits-text-xs)]">
                    {t("lowStockSettings.reorderQty")}
                    <Input
                      value={defaultQty}
                      onChange={(e) => setDefaultQty(e.target.value)}
                      inputMode="decimal"
                      data-testid="branch-default-qty"
                    />
                  </label>
                </div>
              )}
            </div>
            {allowManage ? (
              editingDefault ? (
                <div className="flex gap-2">
                  <Button type="button" variant="secondary" onClick={() => setEditingDefault(false)}>
                    {t("lowStockSettings.cancel")}
                  </Button>
                  <Button
                    type="button"
                    onClick={() => defaultMutation.mutate()}
                    disabled={defaultMutation.isPending}
                    data-testid="save-branch-default"
                  >
                    {t("lowStockSettings.save")}
                  </Button>
                </div>
              ) : (
                <Button
                  type="button"
                  variant="secondary"
                  onClick={() => setEditingDefault(true)}
                  data-testid="edit-branch-default"
                >
                  {t("lowStockSettings.editDefault")}
                </Button>
              )
            ) : null}
          </div>
        </section>

        <div className="grid gap-2 md:grid-cols-2 xl:grid-cols-4" data-testid="low-stock-filters">
          <div className="grid gap-1 text-[length:var(--exits-text-xs)]">
            <span id="low-stock-search-label">{t("lowStockSettings.searchLabel")}</span>
            <SearchField
              label={t("lowStockSettings.search")}
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              onClear={() => setSearch("")}
              placeholder={t("lowStockSettings.search")}
              data-testid="low-stock-search"
              aria-labelledby="low-stock-search-label"
            />
          </div>
          <label className="grid gap-1 text-[length:var(--exits-text-xs)]">
            {t("lowStockSettings.category")}
            <select
              className="exits-select h-10 rounded-[var(--exits-radius-md)] border border-[color:var(--exits-border)] bg-transparent px-2"
              value={categoryId}
              onChange={(e) => setCategoryId(e.target.value)}
              data-testid="low-stock-category"
            >
              <option value="">{t("lowStockSettings.allCategories")}</option>
              {(categoriesQuery.data?.items ?? []).map((cat) => (
                <option key={cat.id} value={cat.id}>
                  {cat.name}
                </option>
              ))}
            </select>
          </label>
          <label className="grid gap-1 text-[length:var(--exits-text-xs)]">
            {t("lowStockSettings.stockStatus")}
            <select
              className="exits-select h-10 rounded-[var(--exits-radius-md)] border border-[color:var(--exits-border)] bg-transparent px-2"
              value={stockStatus}
              onChange={(e) => setStockStatus(e.target.value as StockStatusFilter)}
              data-testid="low-stock-status-filter"
            >
              <option value="All">{t("lowStockSettings.filterAll")}</option>
              <option value="InStock">{t("lowStockSettings.inStock")}</option>
              <option value="LowStock">{t("lowStockSettings.lowStock")}</option>
              <option value="OutOfStock">{t("lowStockSettings.outOfStock")}</option>
            </select>
          </label>
          <label className="grid gap-1 text-[length:var(--exits-text-xs)]">
            {t("lowStockSettings.monitoring")}
            <select
              className="exits-select h-10 rounded-[var(--exits-radius-md)] border border-[color:var(--exits-border)] bg-transparent px-2"
              value={monitoring}
              onChange={(e) => setMonitoring(e.target.value as MonitoringFilter)}
              data-testid="low-stock-monitoring-filter"
            >
              <option value="All">{t("lowStockSettings.filterAll")}</option>
              <option value="BranchDefault">{t("lowStockSettings.monitoringBranchDefault")}</option>
              <option value="Custom">{t("lowStockSettings.monitoringCustom")}</option>
              <option value="NotMonitored">{t("lowStockSettings.monitoringNotMonitored")}</option>
            </select>
          </label>
        </div>

        {selectedCount > 0 && allowManage ? (
          <div
            className="flex flex-wrap items-center gap-2 rounded-[var(--exits-radius-md)] border border-[color:var(--exits-border)] bg-[color:var(--exits-surface)] p-2"
            data-testid="low-stock-bulk-bar"
          >
            <span className="text-[length:var(--exits-text-sm)] font-semibold">
              {t("lowStockSettings.selectedCount").replace("{count}", String(selectedCount))}
            </span>
            <Button type="button" onClick={() => setBulkOpen(true)} data-testid="bulk-set-level">
              {t("lowStockSettings.setLowStockLevel")}
            </Button>
            <Button
              type="button"
             
              variant="secondary"
              disabled={runBulk.isPending}
              onClick={() => runBulk.mutate("BranchDefault")}
              data-testid="bulk-use-default"
            >
              {t("lowStockSettings.useBranchDefault")}
            </Button>
            <Button
              type="button"
             
              variant="secondary"
              disabled={runBulk.isPending}
              onClick={() => runBulk.mutate("NotMonitored")}
              data-testid="bulk-dont-monitor"
            >
              {t("lowStockSettings.dontMonitor")}
            </Button>
          </div>
        ) : null}

        {allPageSelected && !selectAllFiltered && totalCount > pageIds.length ? (
          <div
            className="flex flex-wrap items-center gap-2 text-[length:var(--exits-text-sm)]"
            data-testid="select-all-filtered-prompt"
          >
            <span>
              {t("lowStockSettings.pageSelected").replace("{count}", String(pageIds.length))}
            </span>
            <Button
              type="button"
             
              variant="secondary"
              onClick={() => setSelectAllFiltered(true)}
              data-testid="select-all-filtered"
            >
              {t("lowStockSettings.selectAllFiltered").replace("{count}", String(totalCount))}
            </Button>
          </div>
        ) : null}
        {selectAllFiltered ? (
          <p className="m-0 text-[length:var(--exits-text-sm)]" data-testid="all-filtered-selected">
            {t("lowStockSettings.allFilteredSelected").replace("{count}", String(totalCount))}
          </p>
        ) : null}
      </div>

      <div className="flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden">
        <OrganizationQueryGate
          title={t("lowStockSettings.title")}
          isLoading={listQuery.isLoading}
          isError={listQuery.isError}
          hasData={Boolean(listQuery.data)}
          onRetry={() => void listQuery.refetch()}
        >
          {listQuery.isSuccess && items.length === 0 ? (
            <EmptyState title={t("lowStockSettings.empty")} detail={t("lowStockSettings.emptyDetail")} />
          ) : null}

          {/* Desktop / large: compact table */}
          <div className="hidden min-h-0 flex-1 overflow-auto xl:block" data-testid="low-stock-table-desktop">
            <table className="w-full min-w-[960px] border-collapse text-left text-[length:var(--exits-text-sm)]">
              <thead className="sticky top-0 z-10 bg-[color:var(--exits-surface)]">
                <tr className="border-b border-[color:var(--exits-border)]">
                  <th className="p-2 font-semibold">
                    <input
                      type="checkbox"
                      checked={allPageSelected}
                      onChange={togglePageSelection}
                      aria-label={t("lowStockSettings.selectPage")}
                      data-testid="select-page-header"
                    />
                  </th>
                  <th className="p-2 font-semibold">{t("lowStockSettings.product")}</th>
                  <th className="p-2 font-semibold">{t("lowStockSettings.skuBarcode")}</th>
                  <th className="p-2 font-semibold">{t("lowStockSettings.category")}</th>
                  <th className="p-2 font-semibold">{t("lowStockSettings.onHand")}</th>
                  <th className="p-2 font-semibold">{t("lowStockSettings.lowStockAt")}</th>
                  <th className="p-2 font-semibold">{t("lowStockSettings.reorderQty")}</th>
                  <th className="p-2 font-semibold">{t("lowStockSettings.status")}</th>
                  <th className="p-2 font-semibold">{t("lowStockSettings.monitoring")}</th>
                  <th className="p-2 font-semibold">{t("lowStockSettings.actions")}</th>
                </tr>
              </thead>
              <tbody>
                {items.map((item) => (
                  <tr key={item.productId} className="border-b border-[color:var(--exits-border)]">
                    <td className="p-2">
                      <input
                        type="checkbox"
                        checked={selectAllFiltered || selectedIds.has(item.productId)}
                        onChange={() => {
                          setSelectAllFiltered(false);
                          setSelectedIds((prev) => {
                            const next = new Set(prev);
                            if (next.has(item.productId)) next.delete(item.productId);
                            else next.add(item.productId);
                            return next;
                          });
                        }}
                        data-testid={`select-row-${item.productId}`}
                      />
                    </td>
                    <td className="p-2 font-medium">{item.name}</td>
                    <td className="p-2 text-[color:var(--exits-muted)]">
                      {[item.sku, item.barcode].filter(Boolean).join(" · ") || "—"}
                    </td>
                    <td className="p-2">{item.categoryName || "—"}</td>
                    <td className="p-2">{formatQty(item.onHandQuantity)}</td>
                    <td className="p-2">
                      {item.monitoringMode === "NotMonitored" ? "—" : formatQty(item.reorderLevel)}
                    </td>
                    <td className="p-2">
                      {item.monitoringMode === "NotMonitored" ? "—" : formatQty(item.reorderQuantity)}
                    </td>
                    <td className="p-2">{statusLabel(item)}</td>
                    <td className="p-2">{monitoringLabel(item.monitoringMode, t)}</td>
                    <td className="p-2">
                      {allowManage ? (
                        <Button
                          type="button"
                         
                          variant="secondary"
                          onClick={() => openEdit(item)}
                          data-testid={`edit-row-${item.productId}`}
                        >
                          {t("lowStockSettings.edit")}
                        </Button>
                      ) : null}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Tablet: reduced table with overflow */}
          <div
            className="hidden min-h-0 flex-1 overflow-auto md:block xl:hidden"
            data-testid="low-stock-table-tablet"
          >
            <table className="w-full min-w-[720px] border-collapse text-left text-[length:var(--exits-text-sm)]">
              <thead className="sticky top-0 z-10 bg-[color:var(--exits-surface)]">
                <tr className="border-b border-[color:var(--exits-border)]">
                  <th className="p-2">
                    <input type="checkbox" checked={allPageSelected} onChange={togglePageSelection} />
                  </th>
                  <th className="p-2">{t("lowStockSettings.product")}</th>
                  <th className="p-2">{t("lowStockSettings.onHand")}</th>
                  <th className="p-2">{t("lowStockSettings.lowStockAt")}</th>
                  <th className="p-2">{t("lowStockSettings.status")}</th>
                  <th className="p-2">{t("lowStockSettings.monitoring")}</th>
                  <th className="p-2">{t("lowStockSettings.actions")}</th>
                </tr>
              </thead>
              <tbody>
                {items.map((item) => (
                  <tr key={item.productId} className="border-b border-[color:var(--exits-border)]">
                    <td className="p-2">
                      <input
                        type="checkbox"
                        checked={selectAllFiltered || selectedIds.has(item.productId)}
                        onChange={() => {
                          setSelectAllFiltered(false);
                          setSelectedIds((prev) => {
                            const next = new Set(prev);
                            if (next.has(item.productId)) next.delete(item.productId);
                            else next.add(item.productId);
                            return next;
                          });
                        }}
                      />
                    </td>
                    <td className="p-2">
                      <div className="font-medium">{item.name}</div>
                      <div className="text-[length:var(--exits-text-xs)] text-[color:var(--exits-muted)]">
                        {[item.sku, item.categoryName].filter(Boolean).join(" · ")}
                      </div>
                    </td>
                    <td className="p-2">{formatQty(item.onHandQuantity)}</td>
                    <td className="p-2">
                      {item.monitoringMode === "NotMonitored" ? "—" : formatQty(item.reorderLevel)}
                    </td>
                    <td className="p-2">{statusLabel(item)}</td>
                    <td className="p-2">{monitoringLabel(item.monitoringMode, t)}</td>
                    <td className="p-2">
                      {allowManage ? (
                        <Button type="button" variant="secondary" onClick={() => openEdit(item)}>
                          {t("lowStockSettings.edit")}
                        </Button>
                      ) : null}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Mobile cards */}
          <ul
            className="m-0 grid list-none gap-2 overflow-y-auto p-0 md:hidden"
            data-testid="low-stock-mobile-list"
          >
            {items.map((item) => (
              <li
                key={item.productId}
                className={cn(
                  "rounded-[var(--exits-radius-md)] border border-[color:var(--exits-border)] p-3",
                  (selectAllFiltered || selectedIds.has(item.productId)) && "border-[color:var(--exits-primary)]",
                )}
              >
                <div className="flex items-start gap-2">
                  <input
                    type="checkbox"
                    className="mt-1"
                    checked={selectAllFiltered || selectedIds.has(item.productId)}
                    onChange={() => {
                      setSelectAllFiltered(false);
                      setSelectedIds((prev) => {
                        const next = new Set(prev);
                        if (next.has(item.productId)) next.delete(item.productId);
                        else next.add(item.productId);
                        return next;
                      });
                    }}
                  />
                  <div className="min-w-0 flex-1">
                    <div className="font-semibold">{item.name}</div>
                    <div className="mt-1 grid grid-cols-3 gap-1 text-[length:var(--exits-text-xs)]">
                      <span>
                        {t("lowStockSettings.onHand")}: {formatQty(item.onHandQuantity)}
                      </span>
                      <span>
                        {t("lowStockSettings.lowStockAt")}:{" "}
                        {item.monitoringMode === "NotMonitored" ? "—" : formatQty(item.reorderLevel)}
                      </span>
                      <span>
                        {t("lowStockSettings.status")}: {statusLabel(item)}
                      </span>
                    </div>
                    {allowManage ? (
                      <Button
                        type="button"
                       
                        variant="secondary"
                        className="mt-2"
                        onClick={() => openEdit(item)}
                      >
                        {t("lowStockSettings.edit")}
                      </Button>
                    ) : null}
                  </div>
                </div>
              </li>
            ))}
          </ul>

          {pageCount > 1 ? (
            <div className="flex items-center justify-between gap-2 py-2" data-testid="low-stock-pagination">
              <Button
                type="button"
                variant="secondary"
               
                disabled={page <= 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
              >
                {t("lowStockSettings.previous")}
              </Button>
              <span className="text-[length:var(--exits-text-sm)]">
                {page} / {pageCount}
              </span>
              <Button
                type="button"
                variant="secondary"
               
                disabled={page >= pageCount}
                onClick={() => setPage((p) => Math.min(pageCount, p + 1))}
              >
                {t("lowStockSettings.next")}
              </Button>
            </div>
          ) : null}
        </OrganizationQueryGate>
      </div>

      {bulkOpen ? (
        <div
          className="fixed inset-0 z-40 flex items-end justify-center bg-black/40 p-4 sm:items-center"
          role="dialog"
          aria-modal="true"
          data-testid="bulk-set-level-dialog"
        >
          <div className="w-full max-w-md rounded-[var(--exits-radius-lg)] bg-[color:var(--exits-surface)] p-4 shadow-lg">
            <h3 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
              {t("lowStockSettings.setLowStockLevel")}
            </h3>
            <p className="mt-1 text-[length:var(--exits-text-sm)] text-[color:var(--exits-muted)]">
              {t("lowStockSettings.selectedCount").replace("{count}", String(selectedCount))}
            </p>
            <label className="mt-3 grid gap-1 text-[length:var(--exits-text-xs)]">
              {t("lowStockSettings.lowStockWhen")}
              <Input
                value={bulkLevel}
                onChange={(e) => setBulkLevel(e.target.value)}
                inputMode="decimal"
                data-testid="bulk-level-input"
              />
            </label>
            <label className="mt-2 grid gap-1 text-[length:var(--exits-text-xs)]">
              {t("lowStockSettings.reorderQtyOptional")}
              <Input
                value={bulkQty}
                onChange={(e) => setBulkQty(e.target.value)}
                inputMode="decimal"
                data-testid="bulk-qty-input"
              />
            </label>
            <div className="mt-4 flex justify-end gap-2">
              <Button type="button" variant="secondary" onClick={() => setBulkOpen(false)}>
                {t("lowStockSettings.cancel")}
              </Button>
              <Button
                type="button"
                disabled={runBulk.isPending || Number(bulkLevel) < 0 || Number.isNaN(Number(bulkLevel))}
                onClick={() => runBulk.mutate("Custom")}
                data-testid="bulk-apply"
              >
                {t("lowStockSettings.applyToCount").replace("{count}", String(selectedCount))}
              </Button>
            </div>
          </div>
        </div>
      ) : null}

      {editItem ? (
        <div
          className="fixed inset-0 z-40 flex items-end justify-center bg-black/40 p-4 sm:items-center"
          role="dialog"
          aria-modal="true"
          data-testid="edit-low-stock-dialog"
        >
          <div className="w-full max-w-md rounded-[var(--exits-radius-lg)] bg-[color:var(--exits-surface)] p-4 shadow-lg">
            <h3 className="m-0 text-[length:var(--exits-text-md)] font-semibold">{editItem.name}</h3>
            <div className="mt-3 grid gap-2">
              {(["BranchDefault", "Custom", "NotMonitored"] as EditMode[]).map((mode) => (
                <label key={mode} className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                  <input
                    type="radio"
                    name="edit-mode"
                    checked={editMode === mode}
                    onChange={() => setEditMode(mode)}
                  />
                  {mode === "BranchDefault"
                    ? t("lowStockSettings.useBranchDefault")
                    : mode === "Custom"
                      ? t("lowStockSettings.customThreshold")
                      : t("lowStockSettings.dontMonitor")}
                </label>
              ))}
            </div>
            {editMode === "Custom" ? (
              <div className="mt-3 grid gap-2">
                <label className="grid gap-1 text-[length:var(--exits-text-xs)]">
                  {t("lowStockSettings.lowStockAt")}
                  <Input value={editLevel} onChange={(e) => setEditLevel(e.target.value)} inputMode="decimal" />
                </label>
                <label className="grid gap-1 text-[length:var(--exits-text-xs)]">
                  {t("lowStockSettings.reorderQtyOptional")}
                  <Input value={editQty} onChange={(e) => setEditQty(e.target.value)} inputMode="decimal" />
                </label>
              </div>
            ) : null}
            <div className="mt-4 flex justify-end gap-2">
              <Button type="button" variant="secondary" onClick={() => setEditItem(null)}>
                {t("lowStockSettings.cancel")}
              </Button>
              <Button
                type="button"
                disabled={editMutation.isPending}
                onClick={() => editMutation.mutate()}
                data-testid="save-row-edit"
              >
                {t("lowStockSettings.save")}
              </Button>
            </div>
          </div>
        </div>
      ) : null}

      <span className="sr-only">
        <Settings2 />
      </span>
    </div>
  );
}
