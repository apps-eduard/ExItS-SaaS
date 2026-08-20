import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { resolveCatalogLookup } from "@/api/pos/catalog-lookup";
import {
  CATALOG_BROWSE_PAGE_SIZE,
  listCatalogCategories,
  listCatalogProducts,
} from "@/api/pos/pos-catalog-client";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import { useSessionCart } from "@/cart/SessionCartProvider";
import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { StickyActionBar } from "@/components/exits/FoundationStates";
import { SellCartPanel } from "@/features/sell/SellCartPanel";
import { SellCategoryFilter } from "@/features/sell/SellCategoryFilter";
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

export function SellFloorPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { returnRoute, exit } = useSellingMode();
  const { boundWorkspace } = useWorkspace();
  const cart = useSessionCart();

  const [activeCategory, setActiveCategory] = useState<string>("all");
  const [searchTerm, setSearchTerm] = useState("");
  const [cartSheetOpen, setCartSheetOpen] = useState(false);
  const [searchError, setSearchError] = useState<string | null>(null);
  const [lookupProducts, setLookupProducts] = useState<PosCatalogProductDto[]>([]);
  const [lookupLoading, setLookupLoading] = useState(false);
  const lastExactScanRef = useRef<string | null>(null);

  const debouncedSearch = useDebouncedValue(searchTerm, SEARCH_DEBOUNCE_MS);
  const workspaceScope = useMemo(() => {
    if (!boundWorkspace) {
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

  const addProductToCart = useCallback(
    (product: PosCatalogProductDto) => {
      if (product.canBeSold === false) {
        return;
      }
      cart.addProduct(product);
    },
    [cart],
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
            addProductToCart(result.product);
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
  }, [activeCategory, addProductToCart, debouncedSearch, t, workspaceScope]);

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

            {displayedProducts.map((product) => (
              <button
                key={product.productId}
                type="button"
                data-testid={`sell-product-${product.productId}`}
                className="flex min-h-[6rem] flex-col items-start justify-between rounded-[var(--exits-radius-md)] border border-border bg-surface p-3 text-left transition-colors hover:border-primary"
                onClick={() => addProductToCart(product)}
              >
                <span className="line-clamp-2 text-[length:var(--exits-text-sm)] font-semibold">
                  {product.name}
                </span>
                <MoneyDisplay amount={product.sellingPrice} className="text-muted" />
              </button>
            ))}
          </div>
        </section>

        <aside
          data-testid="sell-cart-landscape"
          className="sell-cart-landscape hidden min-h-0 min-w-0 flex-col gap-3 rounded-[var(--exits-radius-lg)] border border-border bg-surface p-4"
          aria-label={t("sell.cartLabel")}
        >
          <SellCartPanel
            lines={cart.lines}
            lineCount={cart.lineCount}
            subtotal={cart.subtotal}
            onIncrement={cart.incrementLine}
            onDecrement={cart.decrementLine}
            onRemove={cart.removeLine}
          />
        </aside>
      </div>

      <StickyActionBar className="sell-cart-bar">
        <button
          type="button"
          data-testid="sell-cart-bar"
          className="flex w-full items-center justify-between gap-3 text-left"
          onClick={() => setCartSheetOpen(true)}
          aria-expanded={cartSheetOpen}
          aria-controls="sell-cart-sheet-panel"
        >
          <span className="text-[length:var(--exits-text-sm)] font-semibold">{cartSummary}</span>
          <span className="text-[length:var(--exits-text-sm)] text-muted">
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
          lines={cart.lines}
          lineCount={cart.lineCount}
          subtotal={cart.subtotal}
          onIncrement={cart.incrementLine}
          onDecrement={cart.decrementLine}
          onRemove={cart.removeLine}
          showClose
          onClose={() => setCartSheetOpen(false)}
        />
      </div>
    </div>
  );
}
