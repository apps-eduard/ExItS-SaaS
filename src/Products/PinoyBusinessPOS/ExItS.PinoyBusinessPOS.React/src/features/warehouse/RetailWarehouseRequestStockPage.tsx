import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useOutletContext } from "react-router-dom";
import { useMutation, useQuery } from "@tanstack/react-query";
import { Package, ShoppingCart } from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import {
  createStockRequest,
  listReplenishmentCatalog,
  type ReplenishmentCatalogItemDto,
} from "@/api/pos/pos-stock-requests-client";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import { listCatalogCategories } from "@/api/pos/pos-catalog-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { SearchField } from "@/components/exits/SearchField";
import { useToast } from "@/components/exits/ToastProvider";
import {
  formatQuantityDisplay,
  isByWeightSellingMode,
  roundQuantity,
} from "@/cart/sell-cart-helpers";
import { SellCategoryFilter } from "@/features/sell/SellCategoryFilter";
import { SellWeightEntryDialog } from "@/features/sell/SellWeightEntryDialog";
import { setOrgBottomNavHidden } from "@/features/sell/sell-org-bottom-nav-chrome";
import type { RetailWarehouseResolveState } from "@/features/warehouse/retail-warehouse-resolve";
import { summarizeRequestBasket } from "@/features/warehouse/retail-warehouse-request-math";
import {
  findRequestAvailabilityIssues,
  isRequestQuantityAllowed,
} from "@/features/warehouse/retail-warehouse-request-availability";
import {
  RequestStockCartPanel,
  type RequestStockBasketLine,
} from "@/features/warehouse/RequestStockCartPanel";
import {
  RequestStockProductCard,
  requestStockDisplayUom,
} from "@/features/warehouse/RequestStockProductCard";
import { useRetailWarehouseResolve } from "@/features/warehouse/useRetailWarehouseResolve";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type StockFilter = "all" | "low" | "out";

type WeightEntryTarget = {
  product: ReplenishmentCatalogItemDto | RequestStockBasketLine;
  initialKilograms?: number | null;
  mode: "add" | "edit";
};

const PAGE_SIZE = 40;

/** Weight dialog preview uses warehouse cost (request estimate), never branch SRP. */
function toWeightDialogProduct(
  item: ReplenishmentCatalogItemDto | RequestStockBasketLine,
): PosCatalogProductDto {
  const cost =
    item.warehouseUnitCost != null && Number.isFinite(item.warehouseUnitCost)
      ? item.warehouseUnitCost
      : 0;
  return {
    productId: item.productId,
    organizationId: "",
    name: item.name,
    sku: item.sku,
    unitOfMeasure: item.unitOfMeasure,
    sellingMode: item.sellingMode || "PerItem",
    sellingPrice: cost,
    effectiveSellingPrice: cost,
    status: "Active",
    createdAtUtc: "",
    updatedAtUtc: "",
    isTracked: true,
    onHandQuantity: item.warehouseAvailableQuantity,
  };
}

function useReadySupply(): Extract<RetailWarehouseResolveState, { kind: "ready" }> | null {
  const outlet = useOutletContext<RetailWarehouseResolveState | null>();
  const { resolveState } = useRetailWarehouseResolve();
  const state = outlet ?? resolveState;
  return state?.kind === "ready" ? state : null;
}

export function RetailWarehouseRequestStockPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { showToast } = useToast();
  const { sessionGrant } = useWorkspace();
  const { workspace } = useRetailWarehouseResolve();
  const supply = useReadySupply();
  const allowManage = canManageInventory(sessionGrant);

  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [stockFilter, setStockFilter] = useState<StockFilter>("all");
  const [categoryId, setCategoryId] = useState<string>("");
  const [page, setPage] = useState(1);
  const [basket, setBasket] = useState<RequestStockBasketLine[]>([]);
  const [requestNotes, setRequestNotes] = useState("");
  const [cartSheetOpen, setCartSheetOpen] = useState(false);
  const [sideCartLayout, setSideCartLayout] = useState(false);
  const [weightEntry, setWeightEntry] = useState<WeightEntryTarget | null>(null);
  const [flashedProductId, setFlashedProductId] = useState<string | null>(null);
  const [lineWarnings, setLineWarnings] = useState<Map<string, string>>(new Map());
  const [submitGuardMessage, setSubmitGuardMessage] = useState<string | null>(null);
  const flashTimeoutRef = useRef<number | null>(null);

  const flashProduct = useCallback((productId: string) => {
    setFlashedProductId(productId);
    if (flashTimeoutRef.current != null) {
      window.clearTimeout(flashTimeoutRef.current);
    }
    flashTimeoutRef.current = window.setTimeout(() => {
      flashTimeoutRef.current = null;
      setFlashedProductId((current) => (current === productId ? null : current));
    }, 450);
  }, []);

  useEffect(() => {
    return () => {
      if (flashTimeoutRef.current != null) {
        window.clearTimeout(flashTimeoutRef.current);
      }
    };
  }, []);

  useEffect(() => {
    if (typeof window.matchMedia !== "function") {
      return;
    }
    const media = window.matchMedia("(min-width: 900px)");
    const sync = () => setSideCartLayout(media.matches);
    sync();
    media.addEventListener("change", sync);
    return () => media.removeEventListener("change", sync);
  }, []);

  useEffect(() => {
    const hideNav = cartSheetOpen && !sideCartLayout;
    setOrgBottomNavHidden(hideNav);
    return () => {
      setOrgBottomNavHidden(false);
    };
  }, [cartSheetOpen, sideCartLayout]);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedSearch(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, stockFilter, categoryId, supply?.supplyWarehouseId]);

  const categoriesQuery = useQuery({
    queryKey: ["catalog-categories-active", workspace?.organizationId],
    enabled: Boolean(workspace),
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
    enabled: Boolean(workspace && supply?.supplyWarehouseId),
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

  // Keep basket actual-availability in sync when catalog refreshes (UI remaining only).
  useEffect(() => {
    const catalogItems = catalogQuery.data?.items;
    if (!catalogItems?.length) return;
    const byId = new Map(catalogItems.map((item) => [item.productId, item]));
    setBasket((prev) => {
      let changed = false;
      const next = prev.map((line) => {
        const match = byId.get(line.productId);
        if (!match) return line;
        if (match.warehouseAvailableQuantity === line.warehouseAvailableQuantity) {
          return line;
        }
        changed = true;
        return {
          ...line,
          warehouseAvailableQuantity: match.warehouseAvailableQuantity,
          branchOnHandQuantity: match.branchOnHandQuantity,
        };
      });
      return changed ? next : prev;
    });
  }, [catalogQuery.data?.items]);

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
      setCartSheetOpen(false);
      showToast(t("retailWarehouse.request.submitted"), "success");
      navigate(`/warehouse/requests/${dto.stockRequestId}`);
    },
  });

  function upsertLine(
    product: ReplenishmentCatalogItemDto | RequestStockBasketLine,
    quantity: number,
  ) {
    const available = product.warehouseAvailableQuantity;
    const qty = roundQuantity(quantity);
    if (!Number.isFinite(qty) || qty <= 0) {
      removeLine(product.productId);
      return;
    }
    if (!isRequestQuantityAllowed(qty, available)) {
      const uom = requestStockDisplayUom(product.sellingMode || "PerItem", product.unitOfMeasure);
      showToast(
        t("retailWarehouse.request.onlyAvailableAtWarehouse")
          .replace("{qty}", formatQuantityDisplay(Math.max(0, available)))
          .replace("{uom}", uom)
          .replace("{warehouse}", supply?.supplyWarehouseName ?? t("retailWarehouse.request.supplyWarehouse")),
        "error",
      );
      return;
    }
    setLineWarnings((prev) => {
      if (!prev.has(product.productId)) return prev;
      const next = new Map(prev);
      next.delete(product.productId);
      return next;
    });
    setSubmitGuardMessage(null);
    setBasket((prev) => {
      const existing = prev.find((l) => l.productId === product.productId);
      const next: RequestStockBasketLine = {
        productId: product.productId,
        name: product.name,
        sku: product.sku,
        unitOfMeasure: product.unitOfMeasure,
        sellingMode: product.sellingMode || "PerItem",
        quantity: qty,
        branchOnHandQuantity: product.branchOnHandQuantity,
        warehouseAvailableQuantity: product.warehouseAvailableQuantity,
        warehouseUnitCost:
          product.warehouseUnitCost != null && Number.isFinite(product.warehouseUnitCost)
            ? product.warehouseUnitCost
            : null,
      };
      if (existing) {
        return prev.map((l) => (l.productId === product.productId ? next : l));
      }
      return [...prev, next];
    });
    flashProduct(product.productId);
  }

  function selectProduct(product: ReplenishmentCatalogItemDto) {
    if (product.warehouseAvailableQuantity <= 0) {
      return;
    }
    if (isByWeightSellingMode(product.sellingMode)) {
      const existing = basketById.get(product.productId);
      setWeightEntry({
        product: existing ?? product,
        mode: existing ? "edit" : "add",
        initialKilograms: existing?.quantity ?? null,
      });
      return;
    }
    const existing = basketById.get(product.productId);
    const nextQty = (existing?.quantity ?? 0) + 1;
    if (!isRequestQuantityAllowed(nextQty, product.warehouseAvailableQuantity)) {
      const uom = requestStockDisplayUom(product.sellingMode, product.unitOfMeasure);
      showToast(
        t("retailWarehouse.request.onlyAvailableAtWarehouse")
          .replace("{qty}", formatQuantityDisplay(product.warehouseAvailableQuantity))
          .replace("{uom}", uom)
          .replace("{warehouse}", supply?.supplyWarehouseName ?? t("retailWarehouse.request.supplyWarehouse")),
        "error",
      );
      return;
    }
    upsertLine(product, nextQty);
  }

  function removeLine(productId: string) {
    setBasket((prev) => prev.filter((l) => l.productId !== productId));
    setLineWarnings((prev) => {
      if (!prev.has(productId)) return prev;
      const next = new Map(prev);
      next.delete(productId);
      return next;
    });
  }

  function updateQty(productId: string, quantity: number) {
    const line = basketById.get(productId);
    if (!line) return;
    upsertLine(line, quantity);
  }

  async function revalidateAndSubmit() {
    if (!workspace || !supply || basket.length === 0) return;
    setSubmitGuardMessage(null);
    try {
      const freshById = new Map<string, number>();
      await Promise.all(
        basket.map(async (line) => {
          const page = await listReplenishmentCatalog(workspace, {
            supplyWarehouseBranchId: supply.supplyWarehouseId,
            search: line.sku?.trim() || line.name,
            stockFilter: "all",
            page: 1,
            pageSize: 20,
          });
          const match = page.items.find((item) => item.productId === line.productId);
          if (match) {
            freshById.set(line.productId, match.warehouseAvailableQuantity);
          }
        }),
      );

      setBasket((prev) =>
        prev.map((line) => {
          const fresh = freshById.get(line.productId);
          if (fresh == null) return line;
          return { ...line, warehouseAvailableQuantity: fresh };
        }),
      );

      const refreshed = basket.map((line) => ({
        ...line,
        warehouseAvailableQuantity:
          freshById.get(line.productId) ?? line.warehouseAvailableQuantity,
        unitOfMeasure: requestStockDisplayUom(line.sellingMode, line.unitOfMeasure),
      }));
      const issues = findRequestAvailabilityIssues(
        refreshed,
        supply.supplyWarehouseName,
      );
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

  if (!allowManage) {
    return <EmptyState
              align="center"
              icon={<Package className="size-5" strokeWidth={1.75} />} title={t("stockRequest.title")} detail={t("stockRequest.denied")} />;
  }

  if (!workspace || !supply) {
    return (
      <div className="p-4">
        <LoadingSkeleton count={4} />
      </div>
    );
  }

  const items = catalogQuery.data?.items ?? [];
  const totalCount = catalogQuery.data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const categories = categoriesQuery.data?.items ?? [];
  const showMobileCartBar = !sideCartLayout && !cartSheetOpen;
  const showFloatingCart = showMobileCartBar && basket.length > 0;
  const showEmptyMobileCartBar = showMobileCartBar && basket.length === 0;
  const estimatedDisplay = basketTotals.estimatedCostTotal ?? 0;

  const cartPanelProps = {
    lines: basket,
    productCount: basketTotals.productCount,
    estimatedCostTotal: basketTotals.estimatedCostTotal,
    requestNotes,
    onRequestNotesChange: setRequestNotes,
    onIncrement: (productId: string) => {
      const line = basketById.get(productId);
      if (line) updateQty(productId, line.quantity + 1);
    },
    onDecrement: (productId: string) => {
      const line = basketById.get(productId);
      if (line) updateQty(productId, Math.max(1, line.quantity - 1));
    },
    onRemove: removeLine,
    onEditWeight: (line: RequestStockBasketLine) =>
      setWeightEntry({
        product: line,
        mode: "edit",
        initialKilograms: line.quantity,
      }),
    onSubmit: () => {
      void revalidateAndSubmit();
    },
    submitPending: mutation.isPending,
    submitError: mutation.isError,
    submitBlocked,
    lineWarnings: mergedWarnings,
    warehouseName: supply.supplyWarehouseName,
  };

  return (
    <div
      className="request-stock-floor sell-floor-root flex min-h-0 min-w-0 flex-1 flex-col"
      data-testid="retail-warehouse-request-stock"
    >
      <p
        className="m-0 mb-2 shrink-0 text-[length:var(--exits-text-xs)] text-muted"
        data-testid="retail-warehouse-supply-from"
      >
        {t("retailWarehouse.request.supplyFrom").replace("{name}", supply.supplyWarehouseName)}
      </p>

      <div className="sell-floor-layout flex min-h-0 min-w-0 flex-1 flex-col">
        <section
          data-testid="retail-warehouse-browser"
          className={cn(
            "sell-floor-workspace sell-floor-browse flex min-h-0 min-w-0 flex-1 flex-col",
            (showFloatingCart || showEmptyMobileCartBar) &&
              "pb-[calc(5.5rem+env(safe-area-inset-bottom))]",
          )}
        >
          <div className="sell-floor-workspace__search">
            <SearchField
              label={t("sell.searchLabel")}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder={t("sell.searchPlaceholder")}
              data-testid="retail-warehouse-search"
              onClear={() => setSearch("")}
              autoComplete="off"
              spellCheck={false}
            />
          </div>

          <div className="sell-floor-workspace__filters px-3 pb-0 pt-2">
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
          </div>

          <div className="sell-floor-workspace__categories">
            <SellCategoryFilter
              categories={categories.map((cat) => ({
                categoryId: cat.categoryId,
                name: cat.name,
              }))}
              activeCategoryId={categoryId || "all"}
              allLabel={t("sell.categoryAll")}
              listLabel={t("sell.categoriesLabel")}
              onSelect={(id) => setCategoryId(id === "all" ? "" : id)}
            />
          </div>

          <div
            data-testid="retail-warehouse-products"
            className="sell-floor-product-pane sell-product-grid sell-product-grid--enter min-h-0 flex-1 content-start items-start overflow-y-auto overscroll-contain"
            aria-label={t("stockRequest.search")}
          >
            {catalogQuery.isLoading ? (
              <div className="col-span-full">
                <LoadingSkeleton count={8} className="sell-product-grid__skeleton gap-[0.375rem]" />
              </div>
            ) : null}

            {catalogQuery.isError ? (
              <div className="col-span-full">
                <ErrorState
                  title={t("retailWarehouse.loadError")}
                  detail={t("retailWarehouse.loadError")}
                />
              </div>
            ) : null}

            {!catalogQuery.isLoading && !catalogQuery.isError && items.length === 0 ? (
              <div className="col-span-full flex flex-col items-center gap-2 py-6 text-center">
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("stockRequest.emptyProducts")}
                </p>
              </div>
            ) : null}

            {items.map((product) => (
              <RequestStockProductCard
                key={product.productId}
                product={product}
                requestedQtyInBasket={basketById.get(product.productId)?.quantity ?? 0}
                inBasket={basketById.has(product.productId)}
                addedFlash={flashedProductId === product.productId}
                onSelect={selectProduct}
              />
            ))}

            {totalCount > PAGE_SIZE ? (
              <div className="col-span-full flex items-center justify-between gap-2 pt-2">
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
                  onClick={() => setPage((p) => p + 1)}
                  data-testid="retail-warehouse-page-next"
                >
                  {t("retailWarehouse.request.next")}
                </Button>
              </div>
            ) : null}
          </div>
        </section>

        <aside
          data-testid="retail-warehouse-basket-desktop"
          className={cn(
            "sell-cart-landscape sell-cart-shell min-h-0 min-w-0 flex-col overflow-hidden",
            !sideCartLayout && "hidden",
          )}
          aria-label={t("retailWarehouse.request.cartLabel")}
          aria-hidden={!sideCartLayout}
        >
          {sideCartLayout ? (
            <RequestStockCartPanel {...cartPanelProps} panelId="landscape" />
          ) : null}
        </aside>
      </div>

      {showFloatingCart ? (
        <button
          type="button"
          data-testid="retail-warehouse-view-request"
          className="sell-cart-floating sell-cart-bar sell-cart-bar--filled"
          onClick={() => setCartSheetOpen(true)}
          aria-expanded={cartSheetOpen}
          aria-controls="retail-warehouse-cart-sheet-panel"
          aria-label={t("retailWarehouse.request.viewRequest").replace(
            "{count}",
            String(basketTotals.productCount),
          )}
        >
          <span className="sell-cart-bar__summary">
            <span className="sell-cart-bar__icon" aria-hidden>
              <Package className="size-5" strokeWidth={2} />
            </span>
            <span className="sell-cart-bar__copy">
              <span className="sell-cart-bar__count">
                {t("retailWarehouse.request.productsCount").replace(
                  "{count}",
                  String(basketTotals.productCount),
                )}
              </span>
              <span className="sell-cart-bar__total">₱{estimatedDisplay.toFixed(2)}</span>
            </span>
          </span>
          <span className="sell-cart-bar__action" aria-hidden>
            <span className="sell-cart-bar__action-label">{t("sell.floatingCartViewLabel")}</span>
            <ShoppingCart className="size-4 shrink-0" strokeWidth={2} />
          </span>
        </button>
      ) : null}

      {showEmptyMobileCartBar ? (
        <button
          type="button"
          data-testid="retail-warehouse-view-request"
          className="sell-cart-floating sell-cart-bar sell-cart-bar--empty"
          onClick={() => setCartSheetOpen(true)}
          aria-expanded={cartSheetOpen}
          aria-controls="retail-warehouse-cart-sheet-panel"
          aria-label={t("retailWarehouse.request.viewRequest").replace("{count}", "0")}
        >
          <span className="sell-cart-bar__summary">
            <span className="sell-cart-bar__icon" aria-hidden>
              <Package className="size-5" strokeWidth={2} />
            </span>
            <span className="sell-cart-bar__copy sell-cart-bar__copy--amount-only">
              <span className="sell-cart-bar__total">₱0.00</span>
            </span>
          </span>
          <span className="sell-cart-bar__action" aria-hidden>
            <span className="sell-cart-bar__action-label">{t("sell.floatingCartViewLabel")}</span>
            <ShoppingCart className="size-4 shrink-0" strokeWidth={2} />
          </span>
        </button>
      ) : null}

      {!sideCartLayout && cartSheetOpen ? (
        <>
          <div
            className="sell-cart-sheet-backdrop fixed inset-0 z-30 bg-black/40"
            role="presentation"
            onClick={() => setCartSheetOpen(false)}
          />
          <div
            id="retail-warehouse-cart-sheet-panel"
            data-testid="retail-warehouse-basket-sheet"
            className="sell-cart-sheet fixed inset-x-0 bottom-0 z-40 flex h-[min(88dvh,calc(100dvh-env(safe-area-inset-top,0px)))] max-h-[min(88dvh,calc(100dvh-env(safe-area-inset-top,0px)))] flex-col gap-2 overflow-hidden border border-border border-b-0 bg-surface px-4 pt-2 pb-[max(0.75rem,env(safe-area-inset-bottom))] pl-[max(1rem,env(safe-area-inset-left))] pr-[max(1rem,env(safe-area-inset-right))] shadow-[0_-8px_32px_rgba(0,0,0,0.18)]"
          >
            <div className="sell-cart-sheet__handle" aria-hidden>
              <span className="sell-cart-sheet__handle-bar" />
            </div>
            <RequestStockCartPanel
              {...cartPanelProps}
              panelId="sheet"
              showClose
              onClose={() => setCartSheetOpen(false)}
            />
          </div>
        </>
      ) : null}

      <SellWeightEntryDialog
        open={weightEntry != null}
        product={weightEntry ? toWeightDialogProduct(weightEntry.product) : null}
        initialKilograms={weightEntry?.initialKilograms ?? null}
        stockHint={{
          isTracked: true,
          onHandQuantity: weightEntry?.product.warehouseAvailableQuantity ?? null,
        }}
        maxKilograms={weightEntry?.product.warehouseAvailableQuantity ?? null}
        maxAvailableLabel={
          weightEntry
            ? t("retailWarehouse.request.maximumAvailable")
                .replace(
                  "{qty}",
                  formatQuantityDisplay(Math.max(0, weightEntry.product.warehouseAvailableQuantity)),
                )
                .replace("{uom}", "kg")
            : null
        }
        confirmAddLabel={t("retailWarehouse.request.weightAdd")}
        onConfirm={(kilograms) => {
          if (!weightEntry) return;
          upsertLine(weightEntry.product, kilograms);
          setWeightEntry(null);
        }}
        onRemove={
          weightEntry?.mode === "edit"
            ? () => {
                removeLine(weightEntry.product.productId);
                setWeightEntry(null);
              }
            : undefined
        }
        onCancel={() => setWeightEntry(null)}
      />
      {submitGuardMessage ? (
        <p
          role="alert"
          className="m-0 text-center text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
          data-testid="retail-warehouse-submit-guard"
        >
          {submitGuardMessage}
        </p>
      ) : null}
    </div>
  );
}
