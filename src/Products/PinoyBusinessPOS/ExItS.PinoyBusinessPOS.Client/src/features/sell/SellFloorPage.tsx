import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ShoppingCart, Info } from "lucide-react";
import { resolveCatalogLookup } from "@/api/pos/catalog-lookup";
import {
  CATALOG_BROWSE_PAGE_SIZE,
  listCatalogCategories,
  listCatalogProducts,
} from "@/api/pos/pos-catalog-client";
import type {
  PosCatalogProductDto,
  PosCatalogProductUnitDto,
  PosProductCategoryDto,
} from "@/api/pos/pos-catalog-types";
import { getInventoryProduct } from "@/api/pos/pos-inventory-client";
import {
  activeSellUnits,
  cartLineKey,
  evaluateStockGuard,
  findCartStockIssues,
  formatStockUnavailableMessage,
  isByWeightSellingMode,
  resolveAddFlow,
  sumCartBaseQuantityForProduct,
  type StockGuardInput,
} from "@/cart/sell-cart-helpers";
import { useSessionCart, type SessionCartLine } from "@/cart/SessionCartProvider";
import { Button } from "@/components/ui/button";
import { SearchField } from "@/components/exits/SearchField";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { SellCartPanel } from "@/features/sell/SellCartPanel";
import { SellCategoryFilter } from "@/features/sell/SellCategoryFilter";
import { SellCustomQuantityDialog } from "@/features/sell/SellCustomQuantityDialog";
import { SellPriceOverrideDialog } from "@/features/sell/SellPriceOverrideDialog";
import { SellProductCard } from "@/features/sell/SellProductCard";
import { SellReadinessStrip } from "@/features/sell/SellReadinessStrip";
import { evaluateMidSessionSellBlock } from "@/features/sell/sell-readiness";
import { SellUnitEntryDialog } from "@/features/sell/SellUnitEntryDialog";
import { SellWeightEntryDialog } from "@/features/sell/SellWeightEntryDialog";
import {
  canCreateSale,
  canOverrideSalePrice,
  canOverrideSalePriceUnlimited,
} from "@/access/pos-capabilities";
import type { CheckoutShiftReadiness } from "@/features/shifts/checkout-readiness";
import { useShiftContext } from "@/features/shifts/ShiftContextProvider";
import { useI18n } from "@/i18n/I18nProvider";
import { formatCartSummary } from "@/lib/format-money";
import { cn } from "@/lib/cn";
import {
  listCachedCatalogCategories,
  listCachedCatalogProducts,
  replaceCatalogCache,
} from "@/offline/catalog-cache";
import { refreshPriceAuthoritiesForProducts } from "@/offline/price-authority-refresh";
import { useSellOfflineReadiness } from "@/features/sell/use-sell-offline-readiness";
import { useSellingMode } from "@/selling/SellingModeProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const SEARCH_DEBOUNCE_MS = 300;

function stockInputFromProduct(
  product: PosCatalogProductDto,
  sellableQuantity?: number | null,
): StockGuardInput {
  return {
    isTracked: product.isTracked,
    onHandQuantity: product.onHandQuantity,
    unitOfMeasure: product.unitOfMeasure,
    tracksExpiration: product.tracksExpiration,
    sellableQuantity: sellableQuantity ?? undefined,
    sellingMode: product.sellingMode,
  };
}

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
  const { boundWorkspace, sessionGrant, posDevice } = useWorkspace();
  const cart = useSessionCart();
  const { readiness, hasOpenShift, currentShift } = useShiftContext();
  const sellReadiness = useSellOfflineReadiness();
  /**
   * A warm offline session keeps the device and shift it already proved while online.
   * Mid-session warnings would otherwise fire on every offline render even though the
   * cashier never lost the device or closed the shift.
   */
  const continuedOffline = sellReadiness.fromSnapshot;
  const midSessionBlock = continuedOffline
    ? ({ kind: "none" } as const)
    : evaluateMidSessionSellBlock({
        posDevice,
        shiftReadiness: readiness,
      });
  const effectiveReadiness: CheckoutShiftReadiness = continuedOffline
    ? {
        status: "ready",
        shiftId: sellReadiness.shiftId,
        registerId: null,
        shiftGateReady: true,
        moneyPostReady: sellReadiness.moneyPostReady,
      }
    : readiness;
  const allowCreateSale = canCreateSale(sessionGrant);
  const allowOverrideSalePrice = canOverrideSalePrice(sessionGrant);
  const allowOverrideUnlimited = canOverrideSalePriceUnlimited(sessionGrant);

  const [activeCategory, setActiveCategory] = useState<string>("all");
  const [searchTerm, setSearchTerm] = useState("");
  const [cartSheetOpen, setCartSheetOpen] = useState(false);
  const [sideCartLayout, setSideCartLayout] = useState(false);
  const [searchError, setSearchError] = useState<string | null>(null);
  const [lookupProducts, setLookupProducts] = useState<PosCatalogProductDto[]>([]);
  const [lookupLoading, setLookupLoading] = useState(false);
  const [unitEntry, setUnitEntry] = useState<PendingUnitEntry | null>(null);
  const [weightEntry, setWeightEntry] = useState<PendingWeightEntry | null>(null);
  const [customQtyEntry, setCustomQtyEntry] = useState<PendingCustomQuantityEntry | null>(null);
  const [priceOverrideLine, setPriceOverrideLine] = useState<SessionCartLine | null>(null);
  const [infoOpen, setInfoOpen] = useState(false);
  const [entryStockError, setEntryStockError] = useState<string | null>(null);
  const [stockBanner, setStockBanner] = useState<string | null>(null);
  const [flashedProductId, setFlashedProductId] = useState<string | null>(null);
  const lastExactScanRef = useRef<string | null>(null);

  const flashProduct = useCallback((productId: string) => {
    setFlashedProductId(productId);
    window.setTimeout(() => {
      setFlashedProductId((current) => (current === productId ? null : current));
    }, 450);
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

  const online = sellReadiness.online;
  const offlineDb = sellReadiness.offlineContext?.db ?? null;
  const [cachedProducts, setCachedProducts] = useState<PosCatalogProductDto[]>([]);
  const [cachedCategories, setCachedCategories] = useState<PosProductCategoryDto[]>([]);

  const browseProducts = browseQuery.data?.items;
  const browseCategories = categoriesQuery.data?.items;

  /**
   * Write-through only: a successful online browse of the full catalog is the single source of
   * the offline cache. Nothing else may create catalog rows on this device.
   */
  useEffect(() => {
    if (!online || !offlineDb || !browseProducts || !browseCategories) {
      return;
    }
    if (activeCategory !== "all") {
      return;
    }
    void replaceCatalogCache(offlineDb, browseProducts, browseCategories).catch(() => {
      // A cache write failure must never interrupt selling.
    });
  }, [activeCategory, browseCategories, browseProducts, offlineDb, online]);

  /**
   * Lease the price of everything just cached (RMAP-21 Review Repair 01), so an offline Cash sale
   * is priced by something the server signed rather than by this device's memory of a shelf price.
   */
  useEffect(() => {
    if (!online || !offlineDb || !workspaceScope || !browseProducts) {
      return;
    }
    const controller = new AbortController();
    void refreshPriceAuthoritiesForProducts(
      offlineDb,
      workspaceScope,
      browseProducts,
      controller.signal,
    ).catch(() => {
      // No lease simply means no offline sale for that product; selling online is unaffected.
    });
    return () => controller.abort();
  }, [browseProducts, offlineDb, online, workspaceScope]);

  useEffect(() => {
    if (online || !offlineDb) {
      return;
    }
    let cancelled = false;
    void Promise.all([
      listCachedCatalogProducts(offlineDb),
      listCachedCatalogCategories(offlineDb),
    ]).then(([products, categories]) => {
      if (!cancelled) {
        setCachedProducts(products);
        setCachedCategories(categories);
      }
    });
    return () => {
      cancelled = true;
    };
  }, [offlineDb, online]);

  const usingCachedCatalog = !online && cachedProducts.length > 0;

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
      setEntryStockError(null);
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
      setEntryStockError(null);
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
      setEntryStockError(null);
      setCustomQtyEntry({
        product,
        unit,
        initialQuantity: existing > 0 ? existing : null,
      });
    },
    [cart],
  );

  const resolveSellableForProduct = useCallback(
    (productId: string) =>
      stockProductId === productId ? inventoryHintQuery.data?.sellableQuantity : undefined,
    [inventoryHintQuery.data?.sellableQuantity, stockProductId],
  );

  const tryGuardAdd = useCallback(
    (
      product: PosCatalogProductDto,
      quantity: number,
      unit: PosCatalogProductUnitDto | null | undefined,
      options?: { replaceQuantity?: boolean; reportTo?: "entry" | "banner" },
    ): boolean => {
      const key = cartLineKey(product.productId, unit?.unitId ?? null);
      const replace = options?.replaceQuantity === true;
      const otherBase = sumCartBaseQuantityForProduct(cart.lines, product.productId, key);
      const existing = cart.getLine(key);
      const requested = replace ? quantity : (existing?.quantity ?? 0) + quantity;
      const check = evaluateStockGuard({
        stock: stockInputFromProduct(product, resolveSellableForProduct(product.productId)),
        requestedQuantity: requested,
        multiplierToBase: unit && unit.multiplierToBase > 0 ? unit.multiplierToBase : 1,
        otherCartBaseQuantity: otherBase,
      });
      if (check.ok) {
        setEntryStockError(null);
        setStockBanner(null);
        return true;
      }
      const message = formatStockUnavailableMessage(check);
      if (options?.reportTo === "entry") {
        setEntryStockError(message);
      } else {
        setStockBanner(message);
      }
      return false;
    },
    [cart, resolveSellableForProduct],
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
          if (!tryGuardAdd(product, 1, flow.unit, { reportTo: "banner" })) {
            return;
          }
          cart.addLine(product, { unit: flow.unit, quantity: 1 });
          flashProduct(product.productId);
          return;
        case "unitSelector":
          openUnitEntry(product, flow.units);
          return;
        case "base":
          if (!tryGuardAdd(product, 1, null, { reportTo: "banner" })) {
            return;
          }
          cart.addProduct(product, 1);
          flashProduct(product.productId);
          return;
      }
    },
    [cart, flashProduct, openCustomQuantityEntry, openUnitEntry, openWeightEntry, tryGuardAdd],
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
    const term = debouncedSearch.trim().toLowerCase();
    if (term) {
      if (lookupProducts.length > 0 || !usingCachedCatalog) {
        return lookupProducts;
      }
      return cachedProducts.filter(
        (item) =>
          item.canBeSold !== false &&
          (item.name.toLowerCase().includes(term) ||
            item.sku?.toLowerCase() === term ||
            item.barcode?.toLowerCase() === term),
      );
    }
    const live = (browseProducts ?? []).filter((item) => item.canBeSold !== false);
    if (live.length > 0 || !usingCachedCatalog) {
      return live;
    }
    return cachedProducts.filter(
      (item) =>
        item.canBeSold !== false &&
        (activeCategory === "all" || item.categoryId === activeCategory),
    );
  }, [
    activeCategory,
    browseProducts,
    cachedProducts,
    debouncedSearch,
    lookupProducts,
    usingCachedCatalog,
  ]);

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

      if (isByWeightSellingMode(product.sellingMode)) {
        setUnitEntry(null);
        openWeightEntry(product, unit, quantity > 0 ? quantity : null);
        return;
      }

      if (unit.allowsCustomQuantity) {
        setUnitEntry(null);
        openCustomQuantityEntry(product, unit, quantity > 0 ? quantity : null);
        return;
      }

      if (!tryGuardAdd(product, quantity, unit, { reportTo: "entry" })) {
        return;
      }
      setUnitEntry(null);
      cart.addLine(product, { unit, quantity });
      flashProduct(product.productId);
    },
    [cart, flashProduct, openCustomQuantityEntry, openWeightEntry, tryGuardAdd, unitEntry],
  );

  const handleWeightConfirm = useCallback(
    (kilograms: number) => {
      if (!weightEntry) {
        return;
      }
      if (
        !tryGuardAdd(weightEntry.product, kilograms, weightEntry.unit, {
          replaceQuantity: true,
          reportTo: "entry",
        })
      ) {
        return;
      }
      cart.addLine(weightEntry.product, {
        unit: weightEntry.unit,
        quantity: kilograms,
        replaceQuantity: true,
      });
      setWeightEntry(null);
      flashProduct(weightEntry.product.productId);
    },
    [cart, flashProduct, tryGuardAdd, weightEntry],
  );

  const handleCustomQuantityConfirm = useCallback(
    (quantity: number) => {
      if (!customQtyEntry) {
        return;
      }
      if (
        !tryGuardAdd(customQtyEntry.product, quantity, customQtyEntry.unit, {
          replaceQuantity: true,
          reportTo: "entry",
        })
      ) {
        return;
      }
      cart.addLine(customQtyEntry.product, {
        unit: customQtyEntry.unit,
        quantity,
        replaceQuantity: true,
      });
      setCustomQtyEntry(null);
      flashProduct(customQtyEntry.product.productId);
    },
    [cart, customQtyEntry, flashProduct, tryGuardAdd],
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

  const handleChangePrice = useCallback((line: SessionCartLine) => {
    setPriceOverrideLine(line);
  }, []);

  const stockByProductId = useMemo(() => {
    const map = new Map<string, StockGuardInput>();
    const register = (product: PosCatalogProductDto) => {
      const sellable =
        stockProductId === product.productId
          ? inventoryHintQuery.data?.sellableQuantity
          : undefined;
      const existing = map.get(product.productId);
      map.set(product.productId, {
        ...stockInputFromProduct(product, sellable ?? existing?.sellableQuantity),
        onHandQuantity:
          inventoryHintQuery.data?.productId === product.productId &&
          inventoryHintQuery.data.onHandQuantity != null
            ? inventoryHintQuery.data.onHandQuantity
            : (product.onHandQuantity ?? existing?.onHandQuantity),
        isTracked:
          inventoryHintQuery.data?.productId === product.productId
            ? (inventoryHintQuery.data.isTracked ?? product.isTracked)
            : product.isTracked,
      });
    };
    for (const product of displayedProducts) {
      register(product);
    }
    for (const product of cachedProducts) {
      if (!map.has(product.productId)) {
        register(product);
      }
    }
    return map;
  }, [
    cachedProducts,
    displayedProducts,
    inventoryHintQuery.data,
    stockProductId,
  ]);

  const cartStockIssues = useMemo(
    () => findCartStockIssues(cart.lines, stockByProductId),
    [cart.lines, stockByProductId],
  );

  const handleCartIncrement = useCallback(
    (lineKey: string) => {
      const line = cart.getLine(lineKey);
      if (!line) {
        return;
      }
      const stock = stockByProductId.get(line.productId);
      if (!stock?.isTracked) {
        cart.incrementLine(lineKey);
        return;
      }
      const step = line.allowsCustomQuantity || isByWeightSellingMode(line.sellingMode) ? 0.001 : 1;
      const otherBase = sumCartBaseQuantityForProduct(cart.lines, line.productId, line.lineKey);
      const check = evaluateStockGuard({
        stock,
        requestedQuantity: line.quantity + step,
        multiplierToBase: line.multiplierToBase,
        otherCartBaseQuantity: otherBase,
      });
      if (!check.ok) {
        setStockBanner(formatStockUnavailableMessage(check));
        return;
      }
      setStockBanner(null);
      cart.incrementLine(lineKey);
    },
    [cart, stockByProductId],
  );

  const handleCartSetQuantity = useCallback(
    (lineKey: string, quantity: number) => {
      const line = cart.getLine(lineKey);
      if (!line) {
        return;
      }
      const stock = stockByProductId.get(line.productId);
      if (!stock?.isTracked) {
        cart.setLineQuantity(lineKey, quantity);
        return;
      }
      const otherBase = sumCartBaseQuantityForProduct(cart.lines, line.productId, line.lineKey);
      const check = evaluateStockGuard({
        stock,
        requestedQuantity: quantity,
        multiplierToBase: line.multiplierToBase,
        otherCartBaseQuantity: otherBase,
      });
      if (!check.ok) {
        setStockBanner(formatStockUnavailableMessage(check));
        return;
      }
      setStockBanner(null);
      cart.setLineQuantity(lineKey, quantity);
    },
    [cart, stockByProductId],
  );

  const cartPanelProps = {
    lines: cart.lines,
    lineCount: cart.lineCount,
    subtotal: cart.subtotal,
    onIncrement: handleCartIncrement,
    onDecrement: cart.decrementLine,
    onRemove: cart.removeLine,
    onSetQuantity: handleCartSetQuantity,
    onEditWeight: handleEditWeight,
    onEditCustomQuantity: handleEditCustomQuantity,
    onChangePrice: allowOverrideSalePrice ? handleChangePrice : undefined,
    onClear: cart.clear,
    checkoutReadiness: effectiveReadiness,
    canCreateSale: allowCreateSale,
    canOverrideSalePrice: allowOverrideSalePrice,
    midSessionBlock: midSessionBlock.kind,
    stockIssues: cartStockIssues,
    stockBanner,
    suppressMidSessionWarning: true,
  };

  const showMobileCartBar = !sideCartLayout && !cartSheetOpen;
  const showFloatingCart = showMobileCartBar && cart.lineCount > 0;
  const showEmptyMobileCartBar = showMobileCartBar && cart.lineCount === 0;

  return (
    <div
      data-testid="sell-floor"
      className="sell-floor-root flex min-h-0 min-w-0 flex-col"
    >
      <header className="sell-floor-toolbar shrink-0">
        <div className="sell-floor-toolbar__title">
          <h1 className="sell-floor-toolbar__heading">{t("sell.title")}</h1>
          <button
            type="button"
            data-testid="sell-info-toggle"
            className="sell-floor-toolbar__info"
            aria-label={t("sell.infoToggle")}
            aria-expanded={infoOpen}
            aria-controls="sell-info-panel"
            onClick={() => setInfoOpen((open) => !open)}
          >
            <Info className="size-4" aria-hidden />
          </button>
        </div>
        <Button
          type="button"
          variant="ghost"
          className="sell-floor-toolbar__exit"
          onClick={() => {
            exit();
            navigate(returnRoute ?? "/");
          }}
        >
          {t("sell.exitSelling")}
        </Button>
        {infoOpen ? (
          <div
            id="sell-info-panel"
            data-testid="sell-info-panel"
            className="sell-info-panel sell-floor-toolbar__tips"
          >
            <ul className="m-0 list-disc space-y-1 pl-4 text-[length:var(--exits-text-xs)] text-muted">
              <li>{t("sell.info.search")}</li>
              <li>{t("sell.info.shift")}</li>
              <li>{t("sell.info.device")}</li>
              <li>{t("sell.info.weighted")}</li>
            </ul>
          </div>
        ) : null}
      </header>

      <SellReadinessStrip
        continuedOffline={continuedOffline}
        currentShift={currentShift}
        hasOpenShift={hasOpenShift}
        midSessionBlock={midSessionBlock.kind}
        offlineShiftNumber={sellReadiness.openShiftNumber}
        readiness={effectiveReadiness}
        variant="banner"
      />

      {stockBanner ? (
        <p
          role="alert"
          data-testid="sell-stock-banner"
          className="mb-3 m-0 shrink-0 rounded-[var(--exits-radius-md)] border border-[var(--exits-danger)]/40 bg-[var(--exits-surface-muted)] px-3 py-2 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
        >
          {stockBanner}
        </p>
      ) : null}

      <div className="sell-floor-layout min-h-0 min-w-0 flex-1">
        <section
          className={cn(
            "sell-floor-workspace sell-floor-browse flex min-h-0 min-w-0 flex-col",
            (showFloatingCart || showEmptyMobileCartBar) &&
              "pb-[calc(5.5rem+env(safe-area-inset-bottom))]",
          )}
        >
          <div className="sell-floor-workspace__search">
            <SearchField
            data-testid="sell-search"
            containerClassName="shrink-0"
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
          </div>

          {usingCachedCatalog ? (
            <p
              data-testid="sell-offline-catalog-notice"
              className="m-0 text-[length:var(--exits-text-xs)] text-muted"
            >
              {t("offline.cachedCatalogNotice")}
            </p>
          ) : null}

          {searchError && !usingCachedCatalog ? (
            <p
              data-testid="sell-search-error"
              role="alert"
              className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
            >
              {searchError}
            </p>
          ) : null}

          <div className="sell-floor-workspace__categories">
            <SellCategoryFilter
            categories={
              (browseCategories ?? []).length > 0 || !usingCachedCatalog
                ? (browseCategories ?? [])
                : cachedCategories
            }
            activeCategoryId={activeCategory}
            allLabel={t("sell.categoryAll")}
            listLabel={t("sell.categoriesLabel")}
            onSelect={setActiveCategory}
            />
          </div>

          <div
            key={activeCategory}
            data-testid="sell-products"
            className="sell-floor-product-pane sell-product-grid sell-product-grid--enter min-h-0 flex-1 content-start items-start overflow-y-auto"
            aria-label={t("sell.productsLabel")}
          >
            {productsLoading ? (
              <div className="col-span-full">
                <LoadingSkeleton count={8} className="sell-product-grid__skeleton gap-[0.375rem]" />
              </div>
            ) : null}

            {!productsLoading && displayedProducts.length === 0 ? (
              <p className="col-span-full m-0 text-center text-[length:var(--exits-text-sm)] text-muted">
                {debouncedSearch.trim() ? t("sell.catalogNoResults") : t("sell.catalogEmpty")}
              </p>
            ) : null}

            {displayedProducts.map((product) => (
              <SellProductCard
                key={product.productId}
                addedFlash={flashedProductId === product.productId}
                product={product}
                workspace={workspaceScope}
                onAdd={beginAddProduct}
              />
            ))}
          </div>
        </section>

        <aside
          data-testid="sell-cart-landscape"
          className="sell-cart-landscape sell-cart-shell hidden min-h-0 min-w-0 flex-col overflow-hidden"
          aria-label={t("sell.cartLabel")}
        >
          <SellCartPanel {...cartPanelProps} panelId="landscape" />
        </aside>
      </div>

      {showFloatingCart ? (
        <button
          type="button"
          data-testid="sell-cart-bar"
          className="sell-cart-floating sell-cart-bar sell-cart-bar--filled"
          onClick={() => setCartSheetOpen(true)}
          aria-expanded={cartSheetOpen}
          aria-controls="sell-cart-sheet-panel"
        >
          <span className="inline-flex min-w-0 items-center gap-2">
            <ShoppingCart className="size-5 shrink-0" aria-hidden />
            <span className="min-w-0 truncate text-[length:var(--exits-text-sm)] font-semibold">
              {t("sell.floatingCartSummary")
                .replace("{count}", String(cart.lineCount))
                .replace("{subtotal}", cart.subtotal.toFixed(2))}
            </span>
          </span>
          <span className="shrink-0 text-[length:var(--exits-text-sm)] font-medium">
            {t("sell.floatingCartView")}
          </span>
          <span className="sr-only">{cartSummary}</span>
        </button>
      ) : null}

      {showEmptyMobileCartBar ? (
        <button
          type="button"
          data-testid="sell-cart-bar-empty"
          className="sell-cart-floating sell-cart-bar sell-cart-bar--empty"
          onClick={() => setCartSheetOpen(true)}
          aria-expanded={cartSheetOpen}
          aria-controls="sell-cart-sheet-panel"
        >
          <span className="inline-flex min-w-0 items-center gap-2">
            <ShoppingCart className="size-5 shrink-0" aria-hidden />
            <span className="min-w-0 truncate text-[length:var(--exits-text-sm)] font-medium">
              {t("sell.cartLabel")} · {t("sell.payAddItems")}
            </span>
          </span>
          <span className="shrink-0 text-[length:var(--exits-text-sm)] font-medium">
            {t("sell.floatingCartView")}
          </span>
        </button>
      ) : null}

      {!sideCartLayout ? (
        <div
          className={cn(
            "sell-cart-sheet-backdrop fixed inset-0 z-30 bg-black/40 transition-opacity duration-[var(--exits-motion-normal)]",
            cartSheetOpen ? "opacity-100" : "pointer-events-none opacity-0",
          )}
          role="presentation"
          aria-hidden={!cartSheetOpen}
          onClick={() => setCartSheetOpen(false)}
        />
      ) : null}

      {!sideCartLayout ? (
        <div
          id="sell-cart-sheet-panel"
          data-testid="sell-cart-sheet"
          className={cn(
            "sell-cart-sheet fixed inset-0 z-40 flex flex-col gap-3 border-border bg-surface p-4 pt-[max(1rem,env(safe-area-inset-top))] pb-[max(1rem,env(safe-area-inset-bottom))] pl-[max(1rem,env(safe-area-inset-left))] pr-[max(1rem,env(safe-area-inset-right))] shadow-[0_-8px_32px_rgba(0,0,0,0.12)] transition-transform duration-[var(--exits-motion-normal)] ease-[var(--exits-ease-emphasized)]",
            cartSheetOpen ? "translate-y-0" : "pointer-events-none translate-y-full",
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
      ) : null}

      <SellUnitEntryDialog
        open={unitEntry != null}
        product={unitEntry?.product ?? null}
        options={unitEntry?.options ?? []}
        initialUnitId={unitEntry?.initialUnitId}
        initialQuantity={unitEntry?.initialQuantity}
        stockHint={dialogStockHint}
        stockError={entryStockError}
        onCancel={() => {
          setEntryStockError(null);
          setUnitEntry(null);
        }}
        onConfirm={handleUnitConfirm}
      />

      <SellWeightEntryDialog
        open={weightEntry != null}
        product={weightEntry?.product ?? null}
        unit={weightEntry?.unit ?? null}
        initialKilograms={weightEntry?.initialKilograms}
        stockHint={dialogStockHint}
        stockError={entryStockError}
        onCancel={() => {
          setEntryStockError(null);
          setWeightEntry(null);
        }}
        onRemove={() => {
          if (weightEntry) {
            cart.removeLine(
              cartLineKey(weightEntry.product.productId, weightEntry.unit?.unitId ?? null),
            );
            setEntryStockError(null);
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
        stockError={entryStockError}
        onCancel={() => {
          setEntryStockError(null);
          setCustomQtyEntry(null);
        }}
        onRemove={() => {
          if (customQtyEntry) {
            cart.removeLine(
              cartLineKey(customQtyEntry.product.productId, customQtyEntry.unit.unitId),
            );
            setEntryStockError(null);
            setCustomQtyEntry(null);
          }
        }}
        onConfirm={handleCustomQuantityConfirm}
      />

      <SellPriceOverrideDialog
        open={priceOverrideLine != null}
        productName={priceOverrideLine?.name ?? ""}
        currentUnitPrice={priceOverrideLine?.unitPrice ?? 0}
        initialRequestedUnitPrice={priceOverrideLine?.priceOverride?.requestedUnitPrice ?? null}
        initialReason={priceOverrideLine?.priceOverride?.reason ?? null}
        allowUnlimited={allowOverrideUnlimited}
        onCancel={() => setPriceOverrideLine(null)}
        onUseRegularPrice={() => {
          if (priceOverrideLine) {
            cart.setLinePriceOverride(priceOverrideLine.lineKey, null);
            setPriceOverrideLine(null);
          }
        }}
        onApply={(requestedUnitPrice, reason) => {
          if (!priceOverrideLine) {
            return;
          }
          cart.setLinePriceOverride(priceOverrideLine.lineKey, {
            requestedUnitPrice,
            reason,
            expectedBaselineUnitPrice: priceOverrideLine.unitPrice,
          });
          setPriceOverrideLine(null);
        }}
      />
    </div>
  );
}
