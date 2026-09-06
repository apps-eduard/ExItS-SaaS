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
import { listCatalogCategories } from "@/api/pos/pos-catalog-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { QuantityStepper } from "@/components/exits/MoneyQuantity";
import { SearchField } from "@/components/exits/SearchField";
import { BottomSheet } from "@/components/exits/SheetDialog";
import { useToast } from "@/components/exits/ToastProvider";
import type { RetailWarehouseResolveState } from "@/features/warehouse/retail-warehouse-resolve";
import { useRetailWarehouseResolve } from "@/features/warehouse/useRetailWarehouseResolve";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type StockFilter = "all" | "low" | "out";

type BasketLine = {
  productId: string;
  name: string;
  sku?: string | null;
  unitOfMeasure: string;
  quantity: number;
  branchOnHandQuantity: number;
  warehouseAvailableQuantity: number;
};

const PAGE_SIZE = 40;

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
  const totalQty = basket.reduce((sum, line) => sum + line.quantity, 0);

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

  function addProduct(product: ReplenishmentCatalogItemDto) {
    setBasket((prev) => {
      if (prev.some((l) => l.productId === product.productId)) return prev;
      return [
        ...prev,
        {
          productId: product.productId,
          name: product.name,
          sku: product.sku,
          unitOfMeasure: product.unitOfMeasure,
          quantity: 1,
          branchOnHandQuantity: product.branchOnHandQuantity,
          warehouseAvailableQuantity: product.warehouseAvailableQuantity,
        },
      ];
    });
  }

  function updateQty(productId: string, quantity: number) {
    if (!Number.isFinite(quantity) || quantity <= 0) {
      removeLine(productId);
      return;
    }
    setBasket((prev) =>
      prev.map((l) => (l.productId === productId ? { ...l, quantity } : l)),
    );
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

  const basketPanel = (
    <div className="flex h-full min-h-0 flex-col gap-2" data-testid="retail-warehouse-basket">
      <div className="flex items-center justify-between gap-2">
        <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
          {t("retailWarehouse.request.basket")}
        </h2>
        <span className="text-[length:var(--exits-text-xs)] text-muted" data-testid="retail-warehouse-basket-summary">
          {t("retailWarehouse.request.basketSummary")
            .replace("{items}", String(basket.length))
            .replace("{qty}", String(totalQty))}
        </span>
      </div>

      {basket.length === 0 ? (
        <EmptyState
          title={t("retailWarehouse.request.basketEmpty")}
          detail={t("retailWarehouse.request.basketEmptyDetail")}
        />
      ) : (
        <ul className="m-0 flex min-h-0 flex-1 list-none flex-col gap-2 overflow-y-auto p-0">
          {basket.map((line) => (
            <li
              key={line.productId}
              className="rounded-[var(--exits-radius-md)] border border-border p-2"
              data-testid={`retail-warehouse-basket-line-${line.productId}`}
            >
              <div className="flex items-start justify-between gap-2">
                <div className="min-w-0">
                  <div className="truncate font-medium">{line.name}</div>
                  {line.sku ? (
                    <div className="text-[length:var(--exits-text-xs)] text-muted">{line.sku}</div>
                  ) : null}
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
              <div className="mt-1 text-[length:var(--exits-text-xs)] text-muted">
                {t("stockRequest.col.branchStock")}: {line.branchOnHandQuantity}
                {" · "}
                {t("stockRequest.col.warehouseAvailable")}: {line.warehouseAvailableQuantity}
              </div>
              <div className="mt-2">
                <QuantityStepper
                  compact
                  value={line.quantity}
                  decreaseLabel={t("retailWarehouse.request.decrease")}
                  increaseLabel={t("retailWarehouse.request.increase")}
                  onDecrement={() => updateQty(line.productId, Math.max(1, line.quantity - 1))}
                  onIncrement={() => updateQty(line.productId, line.quantity + 1)}
                  decrementDisabled={line.quantity <= 1}
                />
              </div>
            </li>
          ))}
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
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              <span>{t("retailWarehouse.request.category")}</span>
              <select
                className="exits-input"
                value={categoryId}
                onChange={(e) => setCategoryId(e.target.value)}
                data-testid="retail-warehouse-category"
              >
                <option value="">{t("retailWarehouse.request.categoryAll")}</option>
                {categories.map((cat) => (
                  <option key={cat.categoryId} value={cat.categoryId}>
                    {cat.name}
                  </option>
                ))}
              </select>
            </label>
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

          <ul className="m-0 flex list-none flex-col gap-2 p-0">
            {items.map((product) => {
              const added = basketIds.has(product.productId);
              return (
                <li
                  key={product.productId}
                  className="flex items-center justify-between gap-2 rounded-[var(--exits-radius-md)] border border-border p-3"
                  data-testid={`retail-warehouse-product-${product.productId}`}
                >
                  <div className="min-w-0 flex-1">
                    <div className="truncate font-medium">{product.name}</div>
                    <div className="mt-0.5 flex flex-wrap gap-x-3 text-[length:var(--exits-text-xs)] text-muted">
                      {product.sku ? <span>{product.sku}</span> : null}
                      <span>
                        {t("stockRequest.col.branchStock")}: {product.branchOnHandQuantity}
                      </span>
                      <span>
                        {t("stockRequest.col.warehouseAvailable")}: {product.warehouseAvailableQuantity}
                      </span>
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
          {t("retailWarehouse.request.viewRequest").replace("{count}", String(basket.length))}
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
    </div>
  );
}
