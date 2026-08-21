import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { resolveCatalogLookup } from "@/api/pos/catalog-lookup";
import {
  CATALOG_BROWSE_PAGE_SIZE,
  listCatalogCategories,
  listCatalogProducts,
} from "@/api/pos/pos-catalog-client";
import type { PosCatalogProductDto, PosCatalogProductUnitDto } from "@/api/pos/pos-catalog-types";
import { getInventoryProduct } from "@/api/pos/pos-inventory-client";
import {
  activeSellUnits,
  formatQuantityDisplay,
  isByWeightSellingMode,
  resolveAddFlow,
  resolveStockHint,
} from "@/cart/sell-cart-helpers";
import { cartLineKey } from "@/cart/sell-cart-helpers";
import { useSessionCart, type SessionCartLine } from "@/cart/SessionCartProvider";
import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { StickyActionBar } from "@/components/exits/FoundationStates";
import { SellCartPanel } from "@/features/sell/SellCartPanel";
import { SellCategoryFilter } from "@/features/sell/SellCategoryFilter";
import { SellCustomQuantityDialog } from "@/features/sell/SellCustomQuantityDialog";
import { SellUnitEntryDialog } from "@/features/sell/SellUnitEntryDialog";
import { SellWeightEntryDialog } from "@/features/sell/SellWeightEntryDialog";
import { useShiftContext } from "@/features/shifts/ShiftContextProvider";
import { useI18n } from "@/i18n/I18nProvider";
import { formatCartSummary } from "@/lib/format-money";
import { cn } from "@/lib/cn";
import { useSellingMode } from "@/selling/SellingModeProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const SEARCH_DEBOUNCE_MS = 300;

function useDebouncedValue<T>(value: T, delayMs: number): T {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(value), delayMs);
    return () => window.clearTimeout(timer);
  }, [delayMs, value]);

  return debounced;
}

type PendingUnitEntry = {
  product: PosCatalogProductDto;
  options: PosCatalogProductUnitDto[];
  initialUnitId?: string | null;
  initialQuantity?: number;
};

type PendingWeightEntry = {
  product: PosCatalogProductDto;
  unit: PosCatalogProductUnitDto | null;
  initialKilograms?: number | null;
};

type PendingCustomQuantityEntry = {
  product: PosCatalogProductDto;
  unit: PosCatalogProductUnitDto;
  initialQuantity?: number | null;
};

export function SellFloorPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { returnRoute, exit } = useSellingMode();
  const { boundWorkspace } = useWorkspace();
  const cart = useSessionCart();
  const { readiness, hasOpenShift, currentShift } = useShiftContext();

  const [activeCategory, setActiveCategory] = useState<string>("all");
  const [searchTerm, setSearchTerm] = useState("");
  const [cartSheetOpen, setCartSheetOpen] = useState(false);
  const [searchError, setSearchError] = useState<string | null>(null);
  const [lookupProducts, setLookupProducts] = useState<PosCatalogProductDto[]>([]);
  const [lookupLoading, setLookupLoading] = useState(false);
  const [unitEntry, setUnitEntry] = useState<PendingUnitEntry | null>(null);
  const [weightEntry, setWeightEntry] = useState<PendingWeightEntry | null>(null);
  const [customQtyEntry, setCustomQtyEntry] = useState<PendingCustomQuantityEntry | null>(null);
  const lastExactScanRef = useRef<string | null>(null);

  const debouncedSearch = useDebouncedValue(searchTerm, SEARCH_DEBOUNCE_MS);
  const workspaceScope = useMemo(() => {
    if (!boundWorkspace?.branchId) {
      return null;
    }
    return {
      organizationId: boundWorkspace.organizationId,
      branchId: boundWorkspace.branchId,
    };
  }, [boundWorkspace]);

  const categoriesQuery = useQuery({
    queryKey: ["pos-catalog-categories", workspaceScope?.organizationId],
    enabled: workspaceScope !== null,
    queryFn: ({ signal }) =>
      listCatalogCategories(workspaceScope!, { status: "Active", pageSize: 50 }, signal),
  });

  const browseQuery = useQuery({
    queryKey: ["pos-catalog-browse", workspaceScope?.organizationId, activeCategory],
    enabled: workspaceScope !== null && debouncedSearch.trim().length === 0,
    queryFn: ({ signal }) =>
      listCatalogProducts(
        workspaceScope!,
        {
          status: "Active",
          categoryId: activeCategory === "all" ? undefined : activeCategory,
          page: 1,
          pageSize: CATALOG_BROWSE_PAGE_SIZE,
        },
        signal,
      ),
  });

  const stockProductId =
    unitEntry?.product.productId ??
    weightEntry?.product.productId ??
    customQtyEntry?.product.productId ??
    null;
  const stockNeedsSellable =
    (unitEntry?.product.tracksExpiration === true ||
      weightEntry?.product.tracksExpiration === true ||
      customQtyEntry?.product.tracksExpiration === true) &&
    stockProductId != null;

  const inventoryHintQuery = useQuery({
    queryKey: ["pos-sell-stock-hint", workspaceScope?.organizationId, stockProductId],
    enabled: workspaceScope !== null && stockNeedsSellable && stockProductId != null,
    queryFn: ({ signal }) => getInventoryProduct(workspaceScope!, stockProductId!, signal),
    staleTime: 30_000,
  });

  const dialogStockHint = useMemo(() => {
    const product = unitEntry?.product ?? weightEntry?.product ?? customQtyEntry?.product;
    if (!product) {
      return null;
    }
    return {
      isTracked: inventoryHintQuery.data?.isTracked ?? product.isTracked,
      onHandQuantity: inventoryHintQuery.data?.onHandQuantity ?? product.onHandQuantity,
      sellableQuantity: inventoryHintQuery.data?.sellableQuantity,
      tracksExpiration: inventoryHintQuery.data?.tracksExpiration ?? product.tracksExpiration,
    };
  }, [customQtyEntry?.product, inventoryHintQuery.data, unitEntry?.product, weightEntry?.product]);

  const openWeightEntry = useCallback(
    (
      product: PosCatalogProductDto,
      unit: PosCatalogProductUnitDto | null,
      initialKilograms?: number | null,
    ) => {
      const existing =
        initialKilograms ?? cart.getEnteredQuantity(product.productId, unit?.unitId ?? null);
      setWeightEntry({
        product,
        unit,
        initialKilograms: existing > 0 ? existing : null,
      });
    },
    [cart],
  );

  const openUnitEntry = useCallback(
    (product: PosCatalogProductDto, options: PosCatalogProductUnitDto[]) => {
      setUnitEntry({
        product,
        options,
        initialUnitId: options[0]?.unitId,
        initialQuantity: 1,
      });
    },
    [],
  );

  const openCustomQuantityEntry = useCallback(
    (
      product: PosCatalogProductDto,
      unit: PosCatalogProductUnitDto,
      initialQuantity?: number | null,
    ) => {
      const existing = initialQuantity ?? cart.getEnteredQuantity(product.productId, unit.unitId);
      setCustomQtyEntry({
        product,
        unit,
        initialQuantity: existing > 0 ? existing : null,
      });
    },
    [cart],
  );

  const beginAddProduct = useCallback(
    (product: PosCatalogProductDto) => {
      if (product.canBeSold === false) {
        return;
      }

      const flow = resolveAddFlow(product);
      switch (flow.kind) {
        case "weight":
          openWeightEntry(product, flow.unit);
          return;
        case "customQuantity":
          openCustomQuantityEntry(product, flow.unit);
          return;
        case "direct":
          cart.addLine(product, { unit: flow.unit, quantity: 1 });
          return;
        case "unitSelector":
          openUnitEntry(product, flow.units);
          return;
        case "base":
          cart.addProduct(product, 1);
          return;
      }
    },
    [cart, openCustomQuantityEntry, openUnitEntry, openWeightEntry],
  );

  useEffect(() => {
    if (!workspaceScope) {
      return;
    }

    const term = debouncedSearch.trim();
    if (!term) {
      setLookupProducts([]);
      setSearchError(null);
      setLookupLoading(false);
      lastExactScanRef.current = null;
      return;
    }

    let cancelled = false;
    setLookupLoading(true);
    setSearchError(null);

    void resolveCatalogLookup(workspaceScope, term, {
      status: "Active",
      categoryId: activeCategory === "all" ? undefined : activeCategory,
    })
      .then((result) => {
        if (cancelled) {
          return;
        }

        if (result.kind === "exact") {
          const scanKey = `${result.matchedBy}:${term}`;
          if (lastExactScanRef.current !== scanKey) {
            beginAddProduct(result.product);
            lastExactScanRef.current = scanKey;
            setSearchTerm("");
            setLookupProducts([]);
            setSearchError(null);
            return;
          }
          setLookupProducts([result.product]);
          setSearchError(null);
          return;
        }

        if (result.kind === "empty") {
          setLookupProducts([]);
          setSearchError(result.unknownBarcode ? t("sell.searchUnknownBarcode") : null);
          return;
        }

        setLookupProducts(result.products);
        setSearchError(result.unknownBarcode ? t("sell.searchUnknownBarcode") : null);
      })
      .catch(() => {
        if (!cancelled) {
          setLookupProducts([]);
          setSearchError(t("sell.catalogLoadError"));
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLookupLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [activeCategory, beginAddProduct, debouncedSearch, t, workspaceScope]);

  const displayedProducts = useMemo(() => {
    if (debouncedSearch.trim()) {
      return lookupProducts;
    }
    return (browseQuery.data?.items ?? []).filter((item) => item.canBeSold !== false);
  }, [browseQuery.data?.items, debouncedSearch, lookupProducts]);

  const productsLoading =
    (debouncedSearch.trim() ? lookupLoading : browseQuery.isLoading) &&
    displayedProducts.length === 0;

  const cartSummary = formatCartSummary(cart.lineCount, cart.subtotal);

  const handleUnitConfirm = useCallback(
    (unit: PosCatalogProductUnitDto, quantity: number) => {
      if (!unitEntry) {
        return;
      }
      const { product } = unitEntry;
      setUnitEntry(null);

      if (isByWeightSellingMode(product.sellingMode)) {
        openWeightEntry(product, unit, quantity > 0 ? quantity : null);
        return;
      }

      if (unit.allowsCustomQuantity) {
        openCustomQuantityEntry(product, unit, quantity > 0 ? quantity : null);
        return;
      }

      cart.addLine(product, { unit, quantity });
    },
    [cart, openCustomQuantityEntry, openWeightEntry, unitEntry],
  );

  const handleWeightConfirm = useCallback(
    (kilograms: number) => {
      if (!weightEntry) {
        return;
      }
      cart.addLine(weightEntry.product, {
        unit: weightEntry.unit,
        quantity: kilograms,
        replaceQuantity: true,
      });
      setWeightEntry(null);
    },
    [cart, weightEntry],
  );

  const handleCustomQuantityConfirm = useCallback(
    (quantity: number) => {
      if (!customQtyEntry) {
        return;
      }
      cart.addLine(customQtyEntry.product, {
        unit: customQtyEntry.unit,
        quantity,
        replaceQuantity: true,
      });
      setCustomQtyEntry(null);
    },
    [cart, customQtyEntry],
  );

  const synthesizeLineProduct = useCallback(
    (line: SessionCartLine): PosCatalogProductDto =>
      displayedProducts.find((item) => item.productId === line.productId) ??
      ({
        productId: line.productId,
        organizationId: workspaceScope?.organizationId ?? "",
        name: line.name,
        sku: line.sku,
        unitOfMeasure: line.baseUnitOfMeasure,
        sellingMode: line.sellingMode,
        sellingPrice: line.unitPrice,
        status: "Active",
        createdAtUtc: "",
        updatedAtUtc: "",
        units: line.productUnitId
          ? [
              {
                unitId: line.productUnitId,
                productId: line.productId,
                kind: "Sell",
                displayName: line.unitLabel,
                shortLabel: line.unitLabel,
                multiplierToBase: line.multiplierToBase,
                sellingPrice: line.unitPrice,
                allowsCustomQuantity: line.allowsCustomQuantity,
                isActive: true,
                sortOrder: 0,
              },
            ]
          : [],
      } satisfies PosCatalogProductDto),
    [displayedProducts, workspaceScope?.organizationId],
  );

  const handleEditWeight = useCallback(
    (line: SessionCartLine) => {
      const product = synthesizeLineProduct(line);
      const unit =
        activeSellUnits(product).find((item) => item.unitId === line.productUnitId) ?? null;
      openWeightEntry(product, unit, line.quantity);
    },
    [openWeightEntry, synthesizeLineProduct],
  );

  const handleEditCustomQuantity = useCallback(
    (line: SessionCartLine) => {
      const product = synthesizeLineProduct(line);
      const unit =
        activeSellUnits(product).find((item) => item.unitId === line.productUnitId) ??
        (line.productUnitId
          ? ({
              unitId: line.productUnitId,
              productId: line.productId,
              kind: "Sell",
              displayName: line.unitLabel,
              shortLabel: line.unitLabel,
              multiplierToBase: line.multiplierToBase,
              sellingPrice: line.unitPrice,
              allowsCustomQuantity: true,
              isActive: true,
              sortOrder: 0,
            } satisfies PosCatalogProductUnitDto)
          : null);
      if (!unit) {
        return;
      }
      openCustomQuantityEntry(product, unit, line.quantity);
    },
    [openCustomQuantityEntry, synthesizeLineProduct],
  );

  const cartPanelProps = {
    lines: cart.lines,
    lineCount: cart.lineCount,
    subtotal: cart.subtotal,
    onIncrement: cart.incrementLine,
    onDecrement: cart.decrementLine,
    onRemove: cart.removeLine,
    onSetQuantity: cart.setLineQuantity,
    onEditWeight: handleEditWeight,
    onEditCustomQuantity: handleEditCustomQuantity,
    onClear: cart.clear,
    checkoutReadiness: readiness,
  };

  return (
    <div
      data-testid="sell-floor"
      className="sell-floor-root -mx-[max(var(--exits-page-padding),env(safe-area-inset-left))] flex min-h-[calc(100dvh-12rem)] min-w-0 flex-col px-[max(var(--exits-page-padding),env(safe-area-inset-left))]"
    >
      <div className="mb-4 flex min-w-0 items-start justify-between gap-3">
        <PageHeader title={t("sell.title")} description={t("sell.lede")} />
        <Button
          type="button"
          variant="ghost"
          className="shrink-0"
          onClick={() => {
            exit();
            navigate(returnRoute ?? "/");
          }}
        >
          {t("sell.exitSelling")}
        </Button>
      </div>

      <div
        data-testid="sell-shift-banner"
        className="mb-4 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)] p-3"
      >
        {hasOpenShift && currentShift ? (
          <p className="m-0 text-[length:var(--exits-text-sm)]">
            {t("sell.shiftOpenBanner")
              .replace("{shift}", currentShift.shiftNumber)
              .replace(
                "{register}",
                currentShift.registerCode
                  ? `${currentShift.registerCode} — ${currentShift.registerName ?? ""}`
                  : t("shift.noRegisterOnShift"),
              )}
          </p>
        ) : (
          <div className="flex flex-wrap items-center justify-between gap-2">
            <p className="m-0 text-[length:var(--exits-text-sm)]">{t("sell.shiftClosedBanner")}</p>
            <Button
              asChild
              variant="ghost"
              className="min-h-11"
              data-testid="sell-banner-open-shift"
            >
              <Link to="/shifts/open">{t("shift.openTitle")}</Link>
            </Button>
          </div>
        )}
      </div>

      <div className="sell-floor-layout min-h-0 min-w-0 flex-1">
        <section className="sell-floor-browse flex min-h-0 min-w-0 flex-col gap-3">
          <SearchField
            data-testid="sell-search"
            label={t("sell.searchLabel")}
            autoFocus
            autoComplete="off"
            spellCheck={false}
            value={searchTerm}
            onChange={(event) => {
              lastExactScanRef.current = null;
              setSearchTerm(event.target.value);
            }}
            onClear={() => {
              lastExactScanRef.current = null;
              setSearchTerm("");
            }}
            placeholder={t("sell.searchPlaceholder")}
          />

          {searchError ? (
            <p
              data-testid="sell-search-error"
              role="alert"
              className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
            >
              {searchError}
            </p>
          ) : null}

          <SellCategoryFilter
            categories={categoriesQuery.data?.items ?? []}
            activeCategoryId={activeCategory}
            allLabel={t("sell.categoryAll")}
            listLabel={t("sell.categoriesLabel")}
            onSelect={setActiveCategory}
          />

          <div
            data-testid="sell-products"
            className="grid min-h-[12rem] flex-1 grid-cols-2 gap-3 rounded-[var(--exits-radius-lg)] border border-border bg-[var(--exits-surface-muted)] p-4 sm:grid-cols-3 lg:grid-cols-4"
            aria-label={t("sell.productsLabel")}
          >
            {productsLoading ? (
              <div className="col-span-full">
                <LoadingSkeleton count={6} className="grid grid-cols-2 gap-3 sm:grid-cols-3" />
              </div>
            ) : null}

            {!productsLoading && displayedProducts.length === 0 ? (
              <p className="col-span-full m-0 text-center text-[length:var(--exits-text-sm)] text-muted">
                {debouncedSearch.trim() ? t("sell.catalogNoResults") : t("sell.catalogEmpty")}
              </p>
            ) : null}

            {displayedProducts.map((product) => {
              const hint = resolveStockHint({
                isTracked: product.isTracked,
                onHandQuantity: product.onHandQuantity,
                unitOfMeasure: product.unitOfMeasure,
                tracksExpiration: product.tracksExpiration,
                // Tile uses catalog on-hand; sellable is loaded in entry dialogs for expiry products.
                sellableQuantity: undefined,
              });
              const flow = resolveAddFlow(product);
              return (
                <button
                  key={product.productId}
                  type="button"
                  data-testid={`sell-product-${product.productId}`}
                  className="flex min-h-[6rem] min-w-0 flex-col items-start justify-between gap-2 rounded-[var(--exits-radius-md)] border border-border bg-surface p-3 text-left transition-colors hover:border-primary"
                  onClick={() => beginAddProduct(product)}
                >
                  <span className="line-clamp-2 break-words text-[length:var(--exits-text-sm)] font-semibold">
                    {product.name}
                  </span>
                  <div className="flex w-full min-w-0 flex-col gap-1">
                    <MoneyDisplay
                      amount={product.sellingPrice}
                      className="max-w-full truncate text-muted"
                      testId={`sell-product-price-${product.productId}`}
                    />
                    {hint ? (
                      <span
                        data-testid={`sell-product-stock-${product.productId}`}
                        className="truncate text-[length:var(--exits-text-xs)] text-muted"
                      >
                        {t("sell.stockOnHand")
                          .replace("{qty}", formatQuantityDisplay(hint.quantity))
                          .replace("{unit}", hint.unitOfMeasure)}
                      </span>
                    ) : null}
                    {flow.kind === "weight" ||
                    flow.kind === "customQuantity" ||
                    flow.kind === "unitSelector" ? (
                      <span className="truncate text-[length:var(--exits-text-xs)] text-muted">
                        {flow.kind === "weight"
                          ? t("sell.tileByWeight")
                          : flow.kind === "customQuantity"
                            ? t("sell.tileCustomQty")
                            : t("sell.tileChooseUnit")}
                      </span>
                    ) : null}
                  </div>
                </button>
              );
            })}
          </div>
        </section>

        <aside
          data-testid="sell-cart-landscape"
          className="sell-cart-landscape hidden min-h-0 min-w-0 flex-col gap-3 rounded-[var(--exits-radius-lg)] border border-border bg-surface p-4"
          aria-label={t("sell.cartLabel")}
        >
          <SellCartPanel {...cartPanelProps} panelId="landscape" />
        </aside>
      </div>

      <StickyActionBar className="sell-cart-bar">
        <button
          type="button"
          data-testid="sell-cart-bar"
          className="flex w-full min-w-0 items-center justify-between gap-3 text-left"
          onClick={() => setCartSheetOpen(true)}
          aria-expanded={cartSheetOpen}
          aria-controls="sell-cart-sheet-panel"
        >
          <span className="min-w-0 truncate text-[length:var(--exits-text-sm)] font-semibold">
            {cartSummary}
          </span>
          <span className="shrink-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("sell.cartBarHint")}
          </span>
        </button>
      </StickyActionBar>

      {cartSheetOpen ? (
        <div
          className="sell-cart-sheet-backdrop fixed inset-0 z-30 bg-black/40"
          role="presentation"
          onClick={() => setCartSheetOpen(false)}
        />
      ) : null}

      <div
        id="sell-cart-sheet-panel"
        data-testid="sell-cart-sheet"
        className={cn(
          "sell-cart-sheet fixed inset-x-0 bottom-0 z-40 flex max-h-[75dvh] flex-col gap-3 rounded-t-[var(--exits-radius-lg)] border border-border bg-surface p-4 shadow-[0_-8px_32px_rgba(0,0,0,0.12)] transition-transform duration-[var(--exits-motion-normal)]",
          cartSheetOpen ? "translate-y-0" : "translate-y-full pointer-events-none",
        )}
        aria-hidden={!cartSheetOpen}
      >
        <SellCartPanel
          {...cartPanelProps}
          panelId="sheet"
          showClose
          onClose={() => setCartSheetOpen(false)}
        />
      </div>

      <SellUnitEntryDialog
        open={unitEntry != null}
        product={unitEntry?.product ?? null}
        options={unitEntry?.options ?? []}
        initialUnitId={unitEntry?.initialUnitId}
        initialQuantity={unitEntry?.initialQuantity}
        stockHint={dialogStockHint}
        onCancel={() => setUnitEntry(null)}
        onConfirm={handleUnitConfirm}
      />

      <SellWeightEntryDialog
        open={weightEntry != null}
        product={weightEntry?.product ?? null}
        unit={weightEntry?.unit ?? null}
        initialKilograms={weightEntry?.initialKilograms}
        stockHint={dialogStockHint}
        onCancel={() => setWeightEntry(null)}
        onRemove={() => {
          if (weightEntry) {
            cart.removeLine(
              cartLineKey(weightEntry.product.productId, weightEntry.unit?.unitId ?? null),
            );
            setWeightEntry(null);
          }
        }}
        onConfirm={handleWeightConfirm}
      />

      <SellCustomQuantityDialog
        open={customQtyEntry != null}
        product={customQtyEntry?.product ?? null}
        unit={customQtyEntry?.unit ?? null}
        initialQuantity={customQtyEntry?.initialQuantity}
        stockHint={dialogStockHint}
        onCancel={() => setCustomQtyEntry(null)}
        onRemove={() => {
          if (customQtyEntry) {
            cart.removeLine(
              cartLineKey(customQtyEntry.product.productId, customQtyEntry.unit.unitId),
            );
            setCustomQtyEntry(null);
          }
        }}
        onConfirm={handleCustomQuantityConfirm}
      />
    </div>
  );
}
