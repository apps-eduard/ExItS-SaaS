import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate, useOutletContext } from "react-router-dom";
import { useMutation, useQuery } from "@tanstack/react-query";
import {
  ArrowLeft,
  Package,
  PackagePlus,
  RotateCcw,
  Store,
  Warehouse,
} from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import {
  createStockRequest,
  listReplenishmentCatalog,
  type ReplenishmentCatalogItemDto,
} from "@/api/pos/pos-stock-requests-client";
import { listCatalogCategories } from "@/api/pos/pos-catalog-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { ExitsModal } from "@/components/exits/ExitsModal";
import { ExitsSelect } from "@/components/exits/ExitsSelect";
import { LoadingState } from "@/components/exits/LoadingState";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import { ProductSelectionToolbar } from "@/components/exits/ProductSelectionView";
import { SelectedItemsPanel } from "@/components/exits/ProductSelectionWorkspace";
import { PRODUCT_SELECTION_TABLE_MIN_PX } from "@/components/exits/product-selection-view";
import { SearchField } from "@/components/exits/SearchField";
import { useResponsiveDataLayout } from "@/components/exits/useResponsiveDataLayout";
import { useToast } from "@/components/exits/ToastProvider";
import { formatQuantityDisplay, roundQuantity } from "@/cart/sell-cart-helpers";
import { PoDocumentSummary } from "@/features/purchasing/PoDocumentSummary";
import type { RetailWarehouseResolveState } from "@/features/warehouse/retail-warehouse-resolve";
import {
  requestStockDisplayUom,
  summarizeRequestBasket,
  type RequestStockBasketLine,
} from "@/features/warehouse/retail-warehouse-request-math";
import {
  findRequestAvailabilityIssues,
  isRequestQuantityAllowed,
} from "@/features/warehouse/retail-warehouse-request-availability";
import { RequestStockItemsView } from "@/features/warehouse/RequestStockItemsView";
import { RequestStockProductSelection } from "@/features/warehouse/RequestStockProductSelection";
import { useRetailWarehouseResolve } from "@/features/warehouse/useRetailWarehouseResolve";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type StockFilter = "all" | "low" | "out";

const PAGE_SIZE = 40;

function useReadySupply(): Extract<RetailWarehouseResolveState, { kind: "ready" }> | null {
  const outlet = useOutletContext<RetailWarehouseResolveState | null>();
  const { resolveState } = useRetailWarehouseResolve();
  const state = outlet ?? resolveState;
  return state?.kind === "ready" ? state : null;
}

function formatAvailable(qty: number, uom: string): string {
  return `${formatQuantityDisplay(qty)} ${uom}`.trim();
}

export function RetailWarehouseRequestStockPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { showToast } = useToast();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const { workspace } = useRetailWarehouseResolve();
  const supply = useReadySupply();
  const allowManage = canManageInventory(sessionGrant);

  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [stockFilter, setStockFilter] = useState<StockFilter>("all");
  const [categoryId, setCategoryId] = useState("");
  const [page, setPage] = useState(1);
  const [basket, setBasket] = useState<RequestStockBasketLine[]>([]);
  const [requestNotes, setRequestNotes] = useState("");
  const [finderOpen, setFinderOpen] = useState(false);
  const [lineWarnings, setLineWarnings] = useState<Map<string, string>>(new Map());
  const [submitGuardMessage, setSubmitGuardMessage] = useState<string | null>(null);
  const finderPanelId = "request-product-finder-panel";
  const { layout: pickerLayout } = useResponsiveDataLayout({
    tableMinWidthPx: PRODUCT_SELECTION_TABLE_MIN_PX,
  });

  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedSearch(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, stockFilter, categoryId, supply?.supplyWarehouseId]);

  const categoriesQuery = useQuery({
    queryKey: ["catalog-categories-active", workspace?.organizationId],
    enabled: Boolean(workspace) && finderOpen,
    staleTime: 60_000,
    queryFn: ({ signal }) =>
      listCatalogCategories(workspace!, { status: "Active", pageSize: 100 }, signal),
  });

  const catalogQuery = useQuery({
    queryKey: [
      "replenishment-catalog",
      workspace?.organizationId,
      workspace?.branchId,
      supply?.supplyWarehouseId,
      debouncedSearch,
      stockFilter,
      categoryId,
      page,
    ],
    enabled: Boolean(workspace && supply?.supplyWarehouseId && finderOpen),
    queryFn: ({ signal }) =>
      listReplenishmentCatalog(workspace!, {
        supplyWarehouseBranchId: supply!.supplyWarehouseId,
        search: debouncedSearch || undefined,
        stockFilter,
        categoryId: categoryId || null,
        page,
        pageSize: PAGE_SIZE,
        signal,
      }),
  });

  const basketById = useMemo(() => {
    const map = new Map<string, RequestStockBasketLine>();
    for (const line of basket) map.set(line.productId, line);
    return map;
  }, [basket]);

  const basketTotals = useMemo(() => summarizeRequestBasket(basket), [basket]);
  const warehouseLabel =
    supply?.supplyWarehouseName ?? t("retailWarehouse.request.supplyWarehouse");
  const availabilityIssues = useMemo(
    () =>
      findRequestAvailabilityIssues(
        basket.map((line) => ({
          ...line,
          unitOfMeasure: requestStockDisplayUom(line.sellingMode, line.unitOfMeasure),
        })),
        warehouseLabel,
      ),
    [basket, warehouseLabel],
  );
  const mergedWarnings = useMemo(() => {
    const map = new Map(lineWarnings);
    for (const issue of availabilityIssues) {
      if (!map.has(issue.productId)) {
        map.set(issue.productId, issue.message);
      }
    }
    return map;
  }, [lineWarnings, availabilityIssues]);
  const submitBlocked = mergedWarnings.size > 0;

  const mutation = useMutation({
    mutationFn: async () => {
      if (!workspace?.branchId || !supply) throw new Error("missing");
      const lines = basket
        .filter((l) => l.quantity > 0)
        .map((l) => ({ productId: l.productId, requestedQuantity: l.quantity }));
      if (lines.length === 0) throw new Error("empty");
      return createStockRequest(workspace, {
        destinationLocationId: workspace.branchId,
        requestedSourceLocationId: supply.supplyWarehouseId,
        lines,
        notes: requestNotes.trim() || null,
      });
    },
    onSuccess: (dto) => {
      setBasket([]);
      setRequestNotes("");
      setLineWarnings(new Map());
      setSubmitGuardMessage(null);
      showToast(t("retailWarehouse.request.submitted"), "success");
      navigate(`/warehouse/requests/${dto.stockRequestId}`);
    },
  });

  const upsertLine = useCallback((line: RequestStockBasketLine) => {
    setBasket((prev) => {
      const idx = prev.findIndex((l) => l.productId === line.productId);
      if (idx < 0) return [...prev, line];
      const next = [...prev];
      next[idx] = line;
      return next;
    });
    setLineWarnings((prev) => {
      if (!prev.has(line.productId)) return prev;
      const next = new Map(prev);
      next.delete(line.productId);
      return next;
    });
    setSubmitGuardMessage(null);
  }, []);

  const removeLine = useCallback((productId: string) => {
    setBasket((prev) => prev.filter((l) => l.productId !== productId));
    setLineWarnings((prev) => {
      if (!prev.has(productId)) return prev;
      const next = new Map(prev);
      next.delete(productId);
      return next;
    });
    setSubmitGuardMessage(null);
  }, []);

  const updateQty = useCallback(
    (productId: string, quantity: number) => {
      const existing = basketById.get(productId);
      if (!existing) return;
      const qty = roundQuantity(quantity);
      if (qty <= 0) {
        removeLine(productId);
        return;
      }
      if (!isRequestQuantityAllowed(qty, existing.warehouseAvailableQuantity)) {
        setLineWarnings((prev) => {
          const next = new Map(prev);
          next.set(
            productId,
            t("retailWarehouse.request.onlyAvailableAtWarehouse")
              .replace("{qty}", formatQuantityDisplay(existing.warehouseAvailableQuantity))
              .replace(
                "{uom}",
                requestStockDisplayUom(existing.sellingMode, existing.unitOfMeasure),
              )
              .replace("{warehouse}", warehouseLabel),
          );
          return next;
        });
        return;
      }
      upsertLine({ ...existing, quantity: qty });
    },
    [basketById, removeLine, t, upsertLine, warehouseLabel],
  );

  function addProduct(product: ReplenishmentCatalogItemDto) {
    if (product.warehouseAvailableQuantity <= 0) return;
    const existing = basketById.get(product.productId);
    const nextQty = roundQuantity((existing?.quantity ?? 0) + 1);
    if (!isRequestQuantityAllowed(nextQty, product.warehouseAvailableQuantity)) {
      return;
    }
    upsertLine({
      productId: product.productId,
      name: product.name,
      sku: product.sku,
      unitOfMeasure: product.unitOfMeasure,
      sellingMode: product.sellingMode || "PerItem",
      quantity: nextQty,
      branchOnHandQuantity: product.branchOnHandQuantity,
      warehouseAvailableQuantity: product.warehouseAvailableQuantity,
      warehouseUnitCost: product.warehouseUnitCost ?? null,
      branchEffectiveSellingPrice: product.branchEffectiveSellingPrice ?? null,
    });
  }

  async function revalidateAndSubmit() {
    if (!workspace || !supply || basket.length === 0 || submitBlocked || mutation.isPending) {
      return;
    }
    setSubmitGuardMessage(null);
    try {
      const fresh = await listReplenishmentCatalog(workspace, {
        supplyWarehouseBranchId: supply.supplyWarehouseId,
        page: 1,
        pageSize: Math.max(40, basket.length),
        search: undefined,
        stockFilter: "all",
      });
      const freshById = new Map(fresh.items.map((item) => [item.productId, item]));
      setBasket((prev) =>
        prev.map((line) => {
          const match = freshById.get(line.productId);
          if (!match) return line;
          return {
            ...line,
            warehouseAvailableQuantity: match.warehouseAvailableQuantity,
            branchOnHandQuantity: match.branchOnHandQuantity,
            warehouseUnitCost: match.warehouseUnitCost ?? line.warehouseUnitCost,
          };
        }),
      );
      const refreshed = basket.map((line) => {
        const match = freshById.get(line.productId);
        return {
          ...line,
          warehouseAvailableQuantity:
            match?.warehouseAvailableQuantity ?? line.warehouseAvailableQuantity,
          unitOfMeasure: requestStockDisplayUom(line.sellingMode, line.unitOfMeasure),
        };
      });
      const issues = findRequestAvailabilityIssues(refreshed, supply.supplyWarehouseName);
      if (issues.length > 0) {
        setLineWarnings(new Map(issues.map((i) => [i.productId, i.message])));
        setSubmitGuardMessage(t("retailWarehouse.request.fixAvailabilityBeforeSubmit"));
        return;
      }
      setLineWarnings(new Map());
      mutation.mutate();
    } catch {
      setSubmitGuardMessage(t("retailWarehouse.request.revalidateFailed"));
    }
  }

  function resetForm() {
    setBasket([]);
    setRequestNotes("");
    setLineWarnings(new Map());
    setSubmitGuardMessage(null);
    setSearch("");
    setDebouncedSearch("");
    setStockFilter("all");
    setCategoryId("");
    setPage(1);
  }

  const pickerRows = useMemo(
    () => (catalogQuery.data?.items ?? []).filter((row) => !basketById.has(row.productId)),
    [catalogQuery.data?.items, basketById],
  );
  const totalCount = catalogQuery.data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const categories = categoriesQuery.data?.items ?? [];
  const categoryOptions = useMemo(
    () => [
      { value: "", label: t("retailWarehouse.request.categoryAll") },
      ...categories.map((cat) => ({ value: cat.categoryId, label: cat.name })),
    ],
    [categories, t],
  );

  const unitCount = useMemo(
    () => basket.reduce((sum, line) => sum + line.quantity, 0),
    [basket],
  );
  const estimatedDisplay = basketTotals.estimatedCostTotal;
  const submitDisabled =
    basket.length === 0 || submitBlocked || mutation.isPending || Boolean(submitGuardMessage);

  if (!allowManage) {
    return (
      <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="retail-warehouse-request-denied">
        <PageHeader
          title={t("stockRequest.title")}
          backTo="/warehouse"
          backLabel={t("retailWarehouse.request.backOverview")}
          backTestId="page-header-back-warehouse"
        />
        <ErrorState title={t("stockRequest.title")} detail={t("stockRequest.denied")} />
      </div>
    );
  }

  if (!workspace || !supply) {
    return <LoadingState label={t("stockRequest.loading")} />;
  }

  const branchName = boundWorkspace?.branchName ?? t("stockRequest.forLocation");

  return (
    <div
      className="inventory-transfer-create-page exits-page flex min-w-0 flex-col gap-4"
      data-testid="retail-warehouse-request-stock"
    >
      <PageHeader
        title={t("stockRequest.title")}
        description={t("stockRequest.lede")}
        backTo="/warehouse"
        backLabel={t("retailWarehouse.request.backOverview")}
        backTestId="page-header-back-warehouse"
      />

      {mutation.isError ? (
        <Notice tone="danger" testId="retail-warehouse-submit-error">
          {t("stockRequest.submitError")}
        </Notice>
      ) : null}

      {submitGuardMessage ? (
        <Notice tone="warning" testId="retail-warehouse-submit-guard">
          {submitGuardMessage}
        </Notice>
      ) : null}

      {mergedWarnings.size > 0 && !submitGuardMessage ? (
        <Notice tone="warning" testId="retail-warehouse-line-warnings">
          {[...mergedWarnings.values()][0]}
        </Notice>
      ) : null}

      <PoDocumentSummary
        className="po-document-summary--create transfer-create-summary"
        title={t("retailWarehouse.request.detailsTitle")}
        fields={[
          {
            key: "from",
            label: t("retailWarehouse.request.fromWarehouse"),
            value: (
              <span
                className="inline-flex min-w-0 items-center gap-2 font-semibold"
                data-testid="retail-warehouse-supply-from"
              >
                <Warehouse className="size-4 shrink-0 text-primary" strokeWidth={1.75} aria-hidden />
                <span className="min-w-0 truncate">{supply.supplyWarehouseName}</span>
              </span>
            ),
          },
          {
            key: "to",
            label: t("retailWarehouse.request.toBranch"),
            value: (
              <span
                className="inline-flex min-w-0 items-center gap-2 font-semibold"
                data-testid="retail-warehouse-request-to"
              >
                <Store className="size-4 shrink-0 text-primary" strokeWidth={1.75} aria-hidden />
                <span className="min-w-0 truncate">{branchName}</span>
              </span>
            ),
          },
        ]}
        testId="request-stock-details"
        footer={
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>
              {t("stockRequest.notes")}{" "}
              <span className="font-normal text-muted">({t("transfer.notesOptional")})</span>
            </span>
            <textarea
              className="min-h-16 rounded-md border border-border bg-background px-3 py-2"
              value={requestNotes}
              onChange={(e) => setRequestNotes(e.target.value)}
              maxLength={512}
              data-testid="retail-warehouse-notes"
            />
          </label>
        }
      />

      <div
        className="product-selection-workspace receive-stock-workspace transfer-create-workspace flex flex-col gap-3"
        data-testid="request-order-cart"
      >
        <SelectedItemsPanel
          title={t("retailWarehouse.request.items")}
          count={basket.length}
          headingId="request-draft-items-heading"
          addLabel={t("retailWarehouse.request.addProducts")}
          onAddClick={() => setFinderOpen(true)}
          finderOpen={finderOpen}
          finderPanelId={finderPanelId}
          emptyTitle={t("retailWarehouse.request.itemsEmpty")}
          emptyDetail={t("retailWarehouse.request.itemsEmptyDetail")}
          emptyTestId="request-selected-items-empty"
          addTestId="request-add-products-trigger"
          testId="request-draft-lines"
          summary={
            <div className="receive-stock-receipt__summary" data-testid="request-order-summary">
              <div className="receive-stock-receipt__summary-row">
                <span className="text-[length:var(--exits-text-sm)] text-muted">
                  {t("retailWarehouse.request.items")}
                </span>
                <span className="text-[length:var(--exits-text-sm)] tabular-nums">
                  {basket.length}
                </span>
              </div>
              <div className="receive-stock-receipt__summary-row">
                <span className="text-[length:var(--exits-text-sm)] text-muted">
                  {t("transfer.units")}
                </span>
                <span className="text-[length:var(--exits-text-md)] font-semibold tabular-nums">
                  {formatQuantityDisplay(unitCount)}
                </span>
              </div>
              {estimatedDisplay != null ? (
                <div className="receive-stock-receipt__summary-row">
                  <span className="text-[length:var(--exits-text-sm)] text-muted">
                    {t("retailWarehouse.request.estimatedWarehouseCost")}
                  </span>
                  <span
                    className="text-[length:var(--exits-text-md)] font-semibold tabular-nums"
                    data-testid="retail-warehouse-estimated-cost"
                  >
                    {formatPeso(estimatedDisplay)}
                  </span>
                </div>
              ) : null}
            </div>
          }
        >
          <RequestStockItemsView
            lines={basket.map((line) => {
              const uom = requestStockDisplayUom(line.sellingMode, line.unitOfMeasure);
              return {
                key: line.productId,
                name: line.name,
                sku: line.sku ?? null,
                quantity: line.quantity,
                unitOfMeasure: uom,
                sellingMode: line.sellingMode,
                maxQuantity: Math.max(0, line.warehouseAvailableQuantity),
                unitCost: line.warehouseUnitCost,
                hasIssue: mergedWarnings.has(line.productId),
                onQtyChange: (next) => updateQty(line.productId, next),
                onRemove: () => removeLine(line.productId),
              };
            })}
            formatAvailable={formatAvailable}
            t={t}
          />
        </SelectedItemsPanel>

        <ExitsModal
          open={finderOpen}
          onOpenChange={(open) => {
            if (!open) {
              setFinderOpen(false);
            }
          }}
          title={t("retailWarehouse.request.findProducts")}
          closeLabel={t("retailWarehouse.request.closeFindProducts")}
          testId="request-add-products"
          id={finderPanelId}
          size="lg"
          fullHeightOnCompact
          className="lg:max-h-[min(92dvh,48rem)] lg:max-w-3xl"
        >
          <div className="flex flex-col gap-3">
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("retailWarehouse.request.supplyFrom").replace(
                "{name}",
                supply.supplyWarehouseName,
              )}
            </p>
            <ProductSelectionToolbar
              className="transfer-product-selection__filters"
              testId="request-product-filters"
            >
              <ExitsSelect
                value={categoryId}
                options={categoryOptions}
                onChange={setCategoryId}
                searchable={categoryOptions.length > 8}
                searchPlaceholder={t("catalog.searchCategories")}
                menuLabel={t("retailWarehouse.request.category")}
                aria-label={t("retailWarehouse.request.category")}
                testId="request-category-select"
              />
              <SearchField
                label={t("stockRequest.search")}
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                onClear={() => setSearch("")}
                placeholder={t("stockRequest.search")}
                data-testid="retail-warehouse-search"
                containerClassName="transfer-product-selection__search"
              />
            </ProductSelectionToolbar>
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
            {catalogQuery.isLoading ? <LoadingState label={t("stockRequest.loading")} /> : null}
            {!catalogQuery.isLoading && catalogQuery.isError ? (
              <ErrorState
                title={t("retailWarehouse.loadError")}
                detail={t("retailWarehouse.loadError")}
              />
            ) : null}
            {!catalogQuery.isLoading && !catalogQuery.isError && pickerRows.length === 0 ? (
              <EmptyState
                align="center"
                size="compact"
                icon={<Package className="size-5" strokeWidth={1.75} />}
                title={t("stockRequest.emptyProducts")}
                detail={t("stockRequest.emptyProductsDetail")}
              />
            ) : null}
            {pickerRows.length > 0 ? (
              <RequestStockProductSelection
                layout={pickerLayout}
                products={pickerRows}
                formatAvailable={formatAvailable}
                onAddProduct={addProduct}
                t={t}
              />
            ) : null}
            {totalCount > PAGE_SIZE ? (
              <div className="flex items-center justify-between gap-2 pt-1">
                <Button
                  type="button"
                  variant="outline"
                  disabled={page <= 1 || catalogQuery.isFetching}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  data-testid="retail-warehouse-page-prev"
                >
                  {t("retailWarehouse.request.prev")}
                </Button>
                <span className="text-[length:var(--exits-text-sm)] text-muted">
                  {page} / {totalPages}
                </span>
                <Button
                  type="button"
                  variant="outline"
                  disabled={page >= totalPages || catalogQuery.isFetching}
                  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                  data-testid="retail-warehouse-page-next"
                >
                  {t("retailWarehouse.request.next")}
                </Button>
              </div>
            ) : null}
          </div>
        </ExitsModal>

        <div className="receive-stock-actions product-selection-workspace__actions">
          <div className="receive-stock-actions__primary">
            <Button
              type="button"
              intent="primary"
              appearance="ghost"
              className="font-semibold"
              disabled={mutation.isPending}
              onClick={() => navigate("/warehouse")}
              data-testid="request-cancel"
            >
              <ArrowLeft className="size-4 shrink-0 rtl:rotate-180" aria-hidden />
              {t("retailWarehouse.request.backOverview")}
            </Button>
            <Button
              type="button"
              variant="outline"
              disabled={mutation.isPending}
              onClick={resetForm}
              data-testid="request-reset"
            >
              <RotateCcw className="size-4 shrink-0" aria-hidden />
              {t("retailWarehouse.request.reset")}
            </Button>
            <Button
              type="button"
              disabled={submitDisabled}
              onClick={() => void revalidateAndSubmit()}
              data-testid="retail-warehouse-submit"
            >
              <PackagePlus className="size-4 shrink-0" aria-hidden />
              {mutation.isPending
                ? t("retailWarehouse.request.submitting")
                : t("stockRequest.submit")}
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
