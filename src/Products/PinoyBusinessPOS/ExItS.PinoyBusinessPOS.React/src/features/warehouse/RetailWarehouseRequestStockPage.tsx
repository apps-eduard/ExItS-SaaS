import { useEffect, useMemo, useState } from "react";
import { useNavigate, useOutletContext } from "react-router-dom";
import { useMutation, useQuery } from "@tanstack/react-query";
import { Check, Plus, Trash2 } from "lucide-react";
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
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay, QuantityStepper } from "@/components/exits/MoneyQuantity";
import { SearchField } from "@/components/exits/SearchField";
import { BottomSheet } from "@/components/exits/SheetDialog";
import { useToast } from "@/components/exits/ToastProvider";
import {
  formatQuantityDisplay,
  isByWeightSellingMode,
  roundQuantity,
} from "@/cart/sell-cart-helpers";
import { SellCategoryFilter } from "@/features/sell/SellCategoryFilter";
import { SellWeightEntryDialog } from "@/features/sell/SellWeightEntryDialog";
import type { RetailWarehouseResolveState } from "@/features/warehouse/retail-warehouse-resolve";
import {
  estimateRequestLine,
  summarizeRequestBasket,
} from "@/features/warehouse/retail-warehouse-request-math";
import { useRetailWarehouseResolve } from "@/features/warehouse/useRetailWarehouseResolve";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type StockFilter = "all" | "low" | "out";

type BasketLine = {
  productId: string;
  name: string;
  sku?: string | null;
  unitOfMeasure: string;
  sellingMode: string;
  quantity: number;
  branchOnHandQuantity: number;
  warehouseAvailableQuantity: number;
  warehouseUnitCost: number | null;
  branchEffectiveSellingPrice: number | null;
};

type WeightEntryTarget = {
  product: ReplenishmentCatalogItemDto | BasketLine;
  initialKilograms?: number | null;
  mode: "add" | "edit";
};

const PAGE_SIZE = 40;

function displayUom(sellingMode: string, unitOfMeasure: string): string {
  if (isByWeightSellingMode(sellingMode)) {
    return "kg";
  }
  const trimmed = unitOfMeasure.trim();
  if (trimmed.toLowerCase() === "kilogram") {
    return "kg";
  }
  return trimmed || "pc";
}

function toWeightDialogProduct(
  item: ReplenishmentCatalogItemDto | BasketLine,
): PosCatalogProductDto {
  // Weight dialog previews branch effective selling price (not acquisition cost).
  const retail =
    item.branchEffectiveSellingPrice != null && Number.isFinite(item.branchEffectiveSellingPrice)
      ? item.branchEffectiveSellingPrice
      : 0;
  return {
    productId: item.productId,
    organizationId: "",
    name: item.name,
    sku: item.sku,
    unitOfMeasure: item.unitOfMeasure,
    sellingMode: item.sellingMode || "PerItem",
    sellingPrice: retail,
    effectiveSellingPrice: retail,
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
  const [basket, setBasket] = useState<BasketLine[]>([]);
  const [requestNotes, setRequestNotes] = useState("");
  const [mobileBasketOpen, setMobileBasketOpen] = useState(false);
  const [weightEntry, setWeightEntry] = useState<WeightEntryTarget | null>(null);

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

  const basketIds = useMemo(() => new Set(basket.map((l) => l.productId)), [basket]);
  const basketTotals = useMemo(() => summarizeRequestBasket(basket), [basket]);

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
      showToast(t("retailWarehouse.request.submitted"), "success");
      navigate(`/warehouse/requests/${dto.stockRequestId}`);
    },
  });

  function upsertLine(
    product: ReplenishmentCatalogItemDto | BasketLine,
    quantity: number,
  ) {
    const qty = roundQuantity(quantity);
    if (!Number.isFinite(qty) || qty <= 0) {
      removeLine(product.productId);
      return;
    }
    setBasket((prev) => {
      const existing = prev.find((l) => l.productId === product.productId);
      const next: BasketLine = {
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
        branchEffectiveSellingPrice:
          product.branchEffectiveSellingPrice != null &&
          Number.isFinite(product.branchEffectiveSellingPrice)
            ? product.branchEffectiveSellingPrice
            : null,
      };
      if (existing) {
        return prev.map((l) => (l.productId === product.productId ? next : l));
      }
      return [...prev, next];
    });
  }

  function addProduct(product: ReplenishmentCatalogItemDto) {
    if (basketIds.has(product.productId)) {
      return;
    }
    if (isByWeightSellingMode(product.sellingMode)) {
      setWeightEntry({ product, mode: "add", initialKilograms: null });
      return;
    }
    upsertLine(product, 1);
  }

  function updateQty(productId: string, quantity: number) {
    const line = basket.find((l) => l.productId === productId);
    if (!line) return;
    upsertLine(line, quantity);
  }

  function removeLine(productId: string) {
    setBasket((prev) => prev.filter((l) => l.productId !== productId));
  }

  if (!allowManage) {
    return <EmptyState title={t("stockRequest.title")} detail={t("stockRequest.denied")} />;
  }

  if (!workspace || !supply) {
    return <LoadingState label={t("retailWarehouse.loading")} />;
  }

  const items = catalogQuery.data?.items ?? [];
  const totalCount = catalogQuery.data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const categories = categoriesQuery.data?.items ?? [];

  const footerBlock = (
    <div
      className="flex flex-col gap-1 border-t border-border pt-2 text-[length:var(--exits-text-sm)]"
      data-testid="retail-warehouse-basket-footer"
    >
      <div className="flex justify-between gap-2 font-medium" data-testid="retail-warehouse-products-count">
        <span>{t("retailWarehouse.request.productsCount").replace("{count}", String(basketTotals.productCount))}</span>
      </div>
      <div className="flex justify-between gap-2 text-muted">
        <span>{t("retailWarehouse.request.estimatedCost")}</span>
        <span data-testid="retail-warehouse-estimated-cost">
          {basketTotals.estimatedCostTotal != null ? (
            <MoneyDisplay amount={basketTotals.estimatedCostTotal} />
          ) : (
            "—"
          )}
        </span>
      </div>
      <div className="flex justify-between gap-2 text-muted">
        <span>{t("retailWarehouse.request.potentialRetail")}</span>
        <span data-testid="retail-warehouse-potential-retail">
          {basketTotals.potentialRetailTotal != null ? (
            <MoneyDisplay amount={basketTotals.potentialRetailTotal} />
          ) : (
            "—"
          )}
        </span>
      </div>
      {basketTotals.potentialGross != null ? (
        <div className="flex justify-between gap-2 font-semibold">
          <span>{t("retailWarehouse.request.potentialGross")}</span>
          <span data-testid="retail-warehouse-potential-gross">
            <MoneyDisplay amount={basketTotals.potentialGross} />
          </span>
        </div>
      ) : null}
    </div>
  );

  const basketPanel = (
    <div className="flex h-full min-h-0 flex-col gap-2" data-testid="retail-warehouse-basket">
      <div className="flex items-center justify-between gap-2">
        <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
          {t("retailWarehouse.request.basket")}
        </h2>
        <span
          className="text-[length:var(--exits-text-xs)] text-muted"
          data-testid="retail-warehouse-basket-summary"
        >
          {t("retailWarehouse.request.productsCount").replace(
            "{count}",
            String(basketTotals.productCount),
          )}
        </span>
      </div>

      {basket.length === 0 ? (
        <EmptyState
          title={t("retailWarehouse.request.basketEmpty")}
          detail={t("retailWarehouse.request.basketEmptyDetail")}
        />
      ) : (
        <ul className="m-0 flex min-h-0 flex-1 list-none flex-col gap-2 overflow-y-auto p-0">
          {basket.map((line) => {
            const byWeight = isByWeightSellingMode(line.sellingMode);
            const uom = displayUom(line.sellingMode, line.unitOfMeasure);
            const qtyLabel = formatQuantityDisplay(line.quantity);
            const estimates = estimateRequestLine(line);
            return (
              <li
                key={line.productId}
                className="rounded-[var(--exits-radius-md)] border border-border p-2"
                data-testid={`retail-warehouse-basket-line-${line.productId}`}
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0">
                    <div className="truncate font-medium">{line.name}</div>
                    <div className="text-[length:var(--exits-text-xs)] text-muted">
                      {qtyLabel} {uom}
                      {line.warehouseUnitCost != null ? (
                        <>
                          {" · "}
                          {t("retailWarehouse.request.costPerUom")
                            .replace("{amount}", formatPeso(line.warehouseUnitCost))
                            .replace("{uom}", uom)}
                        </>
                      ) : null}
                    </div>
                  </div>
                  <button
                    type="button"
                    className="text-muted"
                    aria-label={t("retailWarehouse.request.remove")}
                    onClick={() => removeLine(line.productId)}
                    data-testid={`retail-warehouse-basket-remove-${line.productId}`}
                  >
                    <Trash2 className="size-4" />
                  </button>
                </div>

                <div className="mt-1 flex flex-col gap-0.5 text-[length:var(--exits-text-xs)] text-muted">
                  {estimates.estimatedCost != null ? (
                    <span data-testid={`retail-warehouse-line-cost-${line.productId}`}>
                      {t("retailWarehouse.request.lineEstimatedCost")}:{" "}
                      <MoneyDisplay amount={estimates.estimatedCost} className="text-[length:var(--exits-text-xs)]" />
                    </span>
                  ) : null}
                  {line.branchEffectiveSellingPrice != null ? (
                    <span>
                      {t("retailWarehouse.request.retailPerUom")
                        .replace("{amount}", formatPeso(line.branchEffectiveSellingPrice))
                        .replace("{uom}", uom)}
                    </span>
                  ) : null}
                  {estimates.potentialRetail != null ? (
                    <span data-testid={`retail-warehouse-line-retail-${line.productId}`}>
                      {t("retailWarehouse.request.linePotentialRetail")}:{" "}
                      <MoneyDisplay
                        amount={estimates.potentialRetail}
                        className="text-[length:var(--exits-text-xs)]"
                      />
                    </span>
                  ) : null}
                </div>

                <div className="mt-2">
                  {byWeight ? (
                    <Button
                      type="button"
                      variant="outline"
                      className="min-h-8 px-2 text-[length:var(--exits-text-xs)]"
                      data-testid={`retail-warehouse-edit-weight-${line.productId}`}
                      onClick={() =>
                        setWeightEntry({
                          product: line,
                          mode: "edit",
                          initialKilograms: line.quantity,
                        })
                      }
                    >
                      {qtyLabel} {uom}
                    </Button>
                  ) : (
                    <QuantityStepper
                      compact
                      value={formatQuantityDisplay(line.quantity)}
                      decreaseLabel={t("retailWarehouse.request.decrease")}
                      increaseLabel={t("retailWarehouse.request.increase")}
                      onDecrement={() => updateQty(line.productId, Math.max(1, line.quantity - 1))}
                      onIncrement={() => updateQty(line.productId, line.quantity + 1)}
                      decrementDisabled={line.quantity <= 1}
                    />
                  )}
                </div>
              </li>
            );
          })}
        </ul>
      )}

      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        <span>{t("stockRequest.notes")}</span>
        <textarea
          className="exits-input min-h-[2.75rem] resize-y"
          rows={2}
          value={requestNotes}
          onChange={(e) => setRequestNotes(e.target.value)}
          data-testid="retail-warehouse-request-notes"
        />
      </label>

      {footerBlock}

      <Button
        type="button"
        disabled={mutation.isPending || basket.length === 0}
        onClick={() => mutation.mutate()}
        data-testid="retail-warehouse-submit"
      >
        {t("stockRequest.submit")}
      </Button>
      {mutation.isError ? (
        <p className="text-danger text-[length:var(--exits-text-sm)]">{t("stockRequest.submitError")}</p>
      ) : null}
    </div>
  );

  return (
    <div className="flex min-h-0 flex-col gap-3" data-testid="retail-warehouse-request-stock">
      <div className="text-[length:var(--exits-text-sm)] text-muted">
        {t("retailWarehouse.request.supplyFrom").replace("{name}", supply.supplyWarehouseName)}
      </div>

      <div className="grid min-h-0 gap-3 md:grid-cols-[minmax(0,1.65fr)_minmax(16rem,0.35fr)]">
        <div className="flex min-w-0 flex-col gap-2" data-testid="retail-warehouse-browser">
          <SearchField
            label={t("stockRequest.search")}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder={t("stockRequest.search")}
            data-testid="retail-warehouse-search"
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

          {categories.length > 0 ? (
            <SellCategoryFilter
              categories={categories.map((cat) => ({
                categoryId: cat.categoryId,
                name: cat.name,
              }))}
              activeCategoryId={categoryId || "all"}
              allLabel={t("retailWarehouse.request.categoryAll")}
              listLabel={t("retailWarehouse.request.category")}
              onSelect={(id) => setCategoryId(id === "all" ? "" : id)}
            />
          ) : null}

          {catalogQuery.isLoading ? <LoadingState label={t("retailWarehouse.loading")} /> : null}
          {catalogQuery.isError ? (
            <ErrorState title={t("retailWarehouse.loadError")} detail={t("retailWarehouse.loadError")} />
          ) : null}

          {!catalogQuery.isLoading && !catalogQuery.isError && items.length === 0 ? (
            <EmptyState
              title={t("stockRequest.emptyProducts")}
              detail={t("stockRequest.emptyProductsDetail")}
            />
          ) : null}

          <ul className="sell-floor-browse m-0 grid list-none gap-2 p-0 sm:grid-cols-2 xl:grid-cols-3">
            {items.map((product) => {
              const added = basketIds.has(product.productId);
              const uom = displayUom(product.sellingMode, product.unitOfMeasure);
              const warehouseOut = product.warehouseAvailableQuantity <= 0;
              const byWeight = isByWeightSellingMode(product.sellingMode);
              return (
                <li
                  key={product.productId}
                  className="flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border p-3"
                  data-testid={`retail-warehouse-product-${product.productId}`}
                >
                  <div className="min-w-0 flex-1">
                    <div className="truncate font-medium">{product.name}</div>
                    {product.sku ? (
                      <div className="text-[length:var(--exits-text-xs)] text-muted">{product.sku}</div>
                    ) : null}
                    <div className="mt-1 flex flex-col gap-0.5 text-[length:var(--exits-text-xs)] text-muted">
                      <span>
                        {t("stockRequest.col.branchStock")}:{" "}
                        {formatQuantityDisplay(product.branchOnHandQuantity)} {uom}
                      </span>
                      {warehouseOut ? (
                        <span
                          className="text-[var(--exits-danger)]"
                          data-testid={`retail-warehouse-oos-${product.productId}`}
                        >
                          {t("retailWarehouse.request.warehouseOutOfStock")}
                        </span>
                      ) : (
                        <span>
                          {t("stockRequest.col.warehouseAvailable")}:{" "}
                          {formatQuantityDisplay(product.warehouseAvailableQuantity)} {uom}
                        </span>
                      )}
                      {product.warehouseUnitCost != null ? (
                        <span data-testid={`retail-warehouse-cost-${product.productId}`}>
                          {t("retailWarehouse.request.warehouseCost")
                            .replace("{amount}", formatPeso(product.warehouseUnitCost))
                            .replace("{uom}", uom)}
                        </span>
                      ) : null}
                      {product.branchEffectiveSellingPrice != null ? (
                        <span data-testid={`retail-warehouse-price-${product.productId}`}>
                          {t("retailWarehouse.request.branchPrice")
                            .replace("{amount}", formatPeso(product.branchEffectiveSellingPrice))
                            .replace("{uom}", uom)}
                        </span>
                      ) : null}
                      {byWeight ? (
                        <span className="text-[length:var(--exits-text-xs)]">
                          {t("sell.tileByWeight")}
                        </span>
                      ) : null}
                    </div>
                  </div>
                  <Button
                    type="button"
                    variant={added ? "outline" : "default"}
                    disabled={added}
                    onClick={() => addProduct(product)}
                    data-testid={`retail-warehouse-add-${product.productId}`}
                  >
                    {added ? (
                      <>
                        <Check className="size-4" aria-hidden />
                        {t("retailWarehouse.request.added")}
                      </>
                    ) : (
                      <>
                        <Plus className="size-4" aria-hidden />
                        {t("retailWarehouse.request.add")}
                      </>
                    )}
                  </Button>
                </li>
              );
            })}
          </ul>

          {totalCount > PAGE_SIZE ? (
            <div className="flex items-center justify-between gap-2">
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

        <aside
          className={cn(
            "hidden min-h-[20rem] rounded-[var(--exits-radius-md)] border border-border p-3 md:sticky md:top-3 md:block md:self-start",
          )}
          data-testid="retail-warehouse-basket-desktop"
        >
          {basketPanel}
        </aside>
      </div>

      <div className="md:hidden">
        <Button
          type="button"
          className="w-full"
          variant="outline"
          onClick={() => setMobileBasketOpen(true)}
          data-testid="retail-warehouse-view-request"
        >
          {t("retailWarehouse.request.viewRequest").replace(
            "{count}",
            String(basketTotals.productCount),
          )}
        </Button>
        <BottomSheet
          open={mobileBasketOpen}
          onClose={() => setMobileBasketOpen(false)}
          panelId="retail-warehouse-basket-sheet"
          testId="retail-warehouse-basket-sheet"
          title={t("retailWarehouse.request.basket")}
          closeLabel={t("branches.cancel")}
        >
          {basketPanel}
        </BottomSheet>
      </div>

      <SellWeightEntryDialog
        open={weightEntry != null}
        product={weightEntry ? toWeightDialogProduct(weightEntry.product) : null}
        initialKilograms={weightEntry?.initialKilograms ?? null}
        stockHint={{
          isTracked: true,
          onHandQuantity: weightEntry?.product.warehouseAvailableQuantity ?? null,
        }}
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
    </div>
  );
}
