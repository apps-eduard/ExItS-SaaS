import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ShoppingCart, Banknote, Info, PackageX, X } from "lucide-react";
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
  isCommittedOutOfStock,
  resolveAddFlow,
  sumCartBaseQuantityForProduct,
  type StockGuardInput,
} from "@/cart/sell-cart-helpers";
import { useSessionCart, type SessionCartLine } from "@/cart/SessionCartProvider";
import { OnlineRequiredPageState } from "@/components/exits/OnlineRequiredBoot";
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
import { setOrgBottomNavHidden } from "@/features/sell/sell-org-bottom-nav-chrome";
import {
  canCreateSale,
  canManageCatalog,
  canOverrideSalePrice,
  canOverrideSalePriceUnlimited,
} from "@/access/pos-capabilities";
import type { CheckoutShiftReadiness } from "@/features/shifts/checkout-readiness";
import { useShiftContext } from "@/features/shifts/ShiftContextProvider";
import { useMediaMin } from "@/hooks/useMediaQuery";
import { useI18n } from "@/i18n/I18nProvider";
import { formatCartSummary } from "@/lib/format-money";
import { cn } from "@/lib/cn";
import {
  listCachedCatalogCategories,
  listCachedCatalogProducts,
} from "@/offline/catalog-cache";
import { resolveBranchStockGuardQuantity } from "@/features/catalog/catalog-stock-display";
import { syncCatalogCacheIfNeeded } from "@/offline/catalog-cache-sync";
import { refreshPriceAuthoritiesIfNeeded } from "@/offline/price-authority-sync";
import { useSellOfflineReadiness } from "@/features/sell/use-sell-offline-readiness";
import { useSellingMode } from "@/selling/SellingModeProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const SEARCH_DEBOUNCE_MS = 300;

function stockInputFromProduct(
  product: PosCatalogProductDto,
  sellableQuantity?: number | null,
): StockGuardInput {
  const branchAvailable = resolveBranchStockGuardQuantity(product);
  return {
    isTracked: product.isTracked,
    onHandQuantity: branchAvailable ?? 0,
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
  const { boundWorkspace, sessionGrant, posDevice, deviceEnforcementEnabled } = useWorkspace();
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
        deviceEnforcementEnabled,
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
  const allowManageCatalog = canManageCatalog(sessionGrant);
  const allowOverrideSalePrice = canOverrideSalePrice(sessionGrant);
  const allowOverrideUnlimited = canOverrideSalePriceUnlimited(sessionGrant);

  const [activeCategory, setActiveCategory] = useState<string>("all");
  const [searchTerm, setSearchTerm] = useState("");
  const [cartSheetOpen, setCartSheetOpen] = useState(false);
  const [sideCartLayout, setSideCartLayout] = useState(false);
  /** Desktop/landscape: keep search autofocus. Mobile: do not summon the virtual keyboard. */
  const desktopSearchAutofocus = useMediaMin(900);
  const [searchError, setSearchError] = useState<string | null>(null);
  const [lookupProducts, setLookupProducts] = useState<PosCatalogProductDto[]>([]);
  const [lookupLoading, setLookupLoading] = useState(false);
  const [unitEntry, setUnitEntry] = useState<PendingUnitEntry | null>(null);
  const [weightEntry, setWeightEntry] = useState<PendingWeightEntry | null>(null);
  const [customQtyEntry, setCustomQtyEntry] = useState<PendingCustomQuantityEntry | null>(null);
  const [priceOverrideLine, setPriceOverrideLine] = useState<SessionCartLine | null>(null);
  const [infoOpen, setInfoOpen] = useState(false);
  const [showOutOfStock, setShowOutOfStock] = useState(false);
  const [entryStockError, setEntryStockError] = useState<string | null>(null);
  const [stockBanner, setStockBanner] = useState<string | null>(null);
  const [flashedProductId, setFlashedProductId] = useState<string | null>(null);
  const lastExactScanRef = useRef<string | null>(null);
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
    const hideNav = cartSheetOpen && !sideCartLayout;
    setOrgBottomNavHidden(hideNav);
    return () => {
      setOrgBottomNavHidden(false);
    };
  }, [cartSheetOpen, sideCartLayout]);

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
    staleTime: 30_000,
    meta: { suppressGlobalError: true, operation: "list sell catalog categories" },
    queryFn: ({ signal }) =>
      listCatalogCategories(workspaceScope!, { status: "Active", pageSize: 50 }, signal),
  });

  const browseQuery = useQuery({
    queryKey: ["pos-catalog-browse", workspaceScope?.organizationId, workspaceScope?.branchId, activeCategory],
    enabled: workspaceScope !== null && debouncedSearch.trim().length === 0,
    staleTime: 30_000,
    meta: { suppressGlobalError: true, operation: "browse sell catalog products" },
    queryFn: ({ signal }) =>
      listCatalogProducts(
        workspaceScope!,
        {
          status: "Active",
          categoryId: activeCategory === "all" ? undefined : activeCategory,
          canBeSold: true,
          commerciallyOffered: true,
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
    if (!online || !offlineDb || !browseProducts || !browseCategories || !workspaceScope) {
      return;
    }
    if (activeCategory !== "all") {
      return;
    }
    void syncCatalogCacheIfNeeded(
      offlineDb,
      workspaceScope,
      browseProducts,
      browseCategories,
    ).catch(() => {
      // A cache write failure must never interrupt selling.
    });
  }, [activeCategory, browseCategories, browseProducts, offlineDb, online, workspaceScope]);

  /**
   * Lease the price of everything just cached (RMAP-21 Review Repair 01), so an offline Cash sale
   * is priced by something the server signed rather than by this device's memory of a shelf price.
   */
  useEffect(() => {
    if (!online || !offlineDb || !workspaceScope || !browseProducts) {
      return;
    }
    const controller = new AbortController();
    void refreshPriceAuthoritiesIfNeeded(
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
      stockStatus: inventoryHintQuery.data?.stockStatus ?? product.stockStatus,
      isLowStock: inventoryHintQuery.data?.isLowStock,
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
      canBeSold: true,
      commerciallyOffered: true,
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

  const visibleProducts = useMemo(() => {
    if (showOutOfStock) {
      return displayedProducts.filter((product) => isCommittedOutOfStock(product));
    }
    return displayedProducts.filter((product) => !isCommittedOutOfStock(product));
  }, [displayedProducts, showOutOfStock]);

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
            : (resolveBranchStockGuardQuantity(product) ?? existing?.onHandQuantity ?? 0),
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
    workspace: workspaceScope,
  };

  const showMobileCartBar = !sideCartLayout && !cartSheetOpen;
  const showFloatingCart = showMobileCartBar && cart.lineCount > 0;
  const showEmptyMobileCartBar = showMobileCartBar && cart.lineCount === 0;

  return (
    <div
      data-testid="sell-floor"
      className="sell-floor-root flex min-h-0 min-w-0 flex-col"
    >
      {!online ? (
        <OnlineRequiredPageState
          title={t("sell.title")}
          detail={t("connectivity.pageNeedsInternet")}
          testId="sell-online-required"
        />
      ) : null}
      <header className="sell-floor-toolbar shrink-0">
        <div className="sell-floor-toolbar__title">
          <h1 className="sell-floor-toolbar__heading">{t("sell.title")}</h1>
          <button
            type="button"
            data-testid="sell-info-toggle"
            className="sell-floor-toolbar__info sell-floor-toolbar__chip"
            aria-label={t("sell.infoToggle")}
            aria-expanded={infoOpen}
            aria-controls="sell-info-panel"
            onClick={() => setInfoOpen((open) => !open)}
          >
            <Info className="size-3.5" aria-hidden />
            <span>{t("sell.infoChip")}</span>
          </button>
          <button
            type="button"
            data-testid="sell-out-of-stock-toggle"
            className="sell-floor-toolbar__info sell-floor-toolbar__chip"
            aria-label={showOutOfStock ? t("sell.hideOutOfStock") : t("sell.showOutOfStock")}
            aria-pressed={showOutOfStock}
            onClick={() => setShowOutOfStock((open) => !open)}
          >
            <PackageX className="sell-floor-toolbar__chip-icon--oos" aria-hidden />
            <span>{t("sell.stockOut")}</span>
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
            <div className="sell-info-panel__bar">
              <ul className="m-0 min-w-0 flex-1 list-disc space-y-1 pl-4 text-[length:var(--exits-text-xs)] text-muted">
                <li>{t("sell.info.search")}</li>
                <li>{t("sell.info.shift")}</li>
                {deviceEnforcementEnabled !== false ? (
                  <li>{t("sell.info.device")}</li>
                ) : null}
                <li>{t("sell.info.weighted")}</li>
                <li>{t("sell.info.cartNotHeld")}</li>
              </ul>
              <button
                type="button"
                data-testid="sell-info-close"
                className="sell-floor-toolbar__info sell-info-panel__close"
                aria-label={t("sell.info.close")}
                onClick={() => setInfoOpen(false)}
              >
                <X className="size-4" aria-hidden />
              </button>
            </div>
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
            autoFocus={desktopSearchAutofocus}
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
            onSelect={(categoryId) => {
              setShowOutOfStock(false);
              setActiveCategory(categoryId);
            }}
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

            {!productsLoading && visibleProducts.length === 0 ? (
              <div className="col-span-full flex flex-col items-center gap-2 text-center">
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {debouncedSearch.trim()
                    ? t("sell.catalogNoResults")
                    : showOutOfStock
                      ? t("sell.outOfStockEmpty")
                      : displayedProducts.length > 0
                        ? t("sell.outOfStockHiddenEmpty")
                        : t("sell.catalogEmpty")}
                </p>
                {!debouncedSearch.trim() &&
                !showOutOfStock &&
                displayedProducts.length === 0 &&
                allowManageCatalog ? (
                  <Link
                    to="/catalog/products/new"
                    className="inline-flex items-center justify-center text-[length:var(--exits-text-sm)] font-semibold text-primary no-underline"
                    data-testid="sell-empty-add-product"
                  >
                    {t("sell.catalogEmptyAddProduct")}
                  </Link>
                ) : null}
              </div>
            ) : null}

            {visibleProducts.map((product) => (
              <SellProductCard
                key={product.productId}
                addedFlash={flashedProductId === product.productId}
                product={product}
                workspace={workspaceScope}
                onAdd={beginAddProduct}
                cartReservedBaseQty={sumCartBaseQuantityForProduct(cart.lines, product.productId)}
                unavailable={isCommittedOutOfStock(product)}
              />
            ))}
          </div>
        </section>

        <aside
          data-testid="sell-cart-landscape"
          className={cn(
            "sell-cart-landscape sell-cart-shell min-h-0 min-w-0 flex-col overflow-hidden",
            !sideCartLayout && "hidden",
          )}
          aria-label={t("sell.cartLabel")}
          aria-hidden={!sideCartLayout}
        >
          {sideCartLayout ? <SellCartPanel {...cartPanelProps} panelId="landscape" /> : null}
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
          aria-label={t("sell.floatingCartView")}
        >
          <span className="sell-cart-bar__summary">
            <span className="sell-cart-bar__icon" aria-hidden>
              <Banknote className="size-5" strokeWidth={2} />
            </span>
            <span className="sell-cart-bar__copy">
              <span className="sell-cart-bar__count">
                {cart.lineCount}{" "}
                {cart.lineCount === 1 ? t("sell.cartItemSingular") : t("sell.cartItemPlural")}
              </span>
              <span className="sell-cart-bar__total">₱{cart.subtotal.toFixed(2)}</span>
            </span>
          </span>
          <span className="sell-cart-bar__action" data-testid="sell-cart-bar-view" aria-hidden>
            <span className="sell-cart-bar__action-label">{t("sell.floatingCartViewLabel")}</span>
            <ShoppingCart className="size-4 shrink-0" strokeWidth={2} />
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
          aria-label={t("sell.floatingCartView")}
        >
          <span className="sell-cart-bar__summary">
            <span className="sell-cart-bar__icon" aria-hidden>
              <Banknote className="size-5" strokeWidth={2} />
            </span>
            <span className="sell-cart-bar__copy sell-cart-bar__copy--amount-only">
              <span className="sell-cart-bar__total">₱0.00</span>
            </span>
          </span>
          <span className="sell-cart-bar__action" data-testid="sell-cart-bar-view" aria-hidden>
            <span className="sell-cart-bar__action-label">{t("sell.floatingCartViewLabel")}</span>
            <ShoppingCart className="size-4 shrink-0" strokeWidth={2} />
          </span>
        </button>
      ) : null}

      {/* Unmount when closed — a persistent opacity-0 full-screen layer at z-30
          competed with bottom nav and could leave clicks dead after rapid tab switches. */}
      {!sideCartLayout && cartSheetOpen ? (
        <>
          <div
            className="sell-cart-sheet-backdrop fixed inset-0 z-30 bg-black/40"
            role="presentation"
            onClick={() => setCartSheetOpen(false)}
          />
          <div
            id="sell-cart-sheet-panel"
            data-testid="sell-cart-sheet"
            className="sell-cart-sheet fixed inset-x-0 bottom-0 z-40 flex h-[min(88dvh,calc(100dvh-env(safe-area-inset-top,0px)))] max-h-[min(88dvh,calc(100dvh-env(safe-area-inset-top,0px)))] flex-col gap-2 overflow-hidden border border-border border-b-0 bg-surface px-4 pt-2 pb-[max(0.75rem,env(safe-area-inset-bottom))] pl-[max(1rem,env(safe-area-inset-left))] pr-[max(1rem,env(safe-area-inset-right))] shadow-[0_-8px_32px_rgba(0,0,0,0.18)]"
            aria-hidden={false}
          >
            <div className="sell-cart-sheet__handle" aria-hidden>
              <span className="sell-cart-sheet__handle-bar" />
            </div>
            <SellCartPanel
              {...cartPanelProps}
              panelId="sheet"
              showClose
              onClose={() => setCartSheetOpen(false)}
            />
          </div>
        </>
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
