import { useEffect, useMemo, useState } from "react";
import type { CustomerStorefrontProductDto } from "@/api/pos/pos-customer-orders-client";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import { useInfiniteQuery } from "@tanstack/react-query";
import { Banknote, RefreshCw, ShoppingCart, Users, WifiOff } from "lucide-react";
import { ensurePersonalBuyerPosToken } from "@/api/platform/personal-buyer-token";
import {
  getCustomerStorefront,
  isCustomerOrderingUnavailable,
  sellerWorkspace,
} from "@/api/pos/pos-customer-orders-client";
import { ActionTileGrid } from "@/components/exits/ActionTileGrid";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { OrderingUnavailablePanel } from "@/features/customer-ordering/OrderingUnavailablePanel";
import { PersonalStoreIdentityCard } from "@/features/customer-ordering/PersonalStoreIdentity";
import { usePersonalMerchantCart } from "@/features/customer-ordering/PersonalMerchantCartProvider";
import { PersonalCommerceNav } from "@/features/customer-ordering/PersonalCommerceNav";
import { CommerceLoadMore } from "@/features/customer-ordering/personal-commerce-ui";
import { shopCheckoutBlocker } from "@/features/customer-ordering/checkout-place-readiness";
import { ShopOrderCart } from "@/features/customer-ordering/ShopOrderCart";
import { StoreProductCard } from "@/features/customer-ordering/StoreProductCard";
import { SellCategoryFilter } from "@/features/sell/SellCategoryFilter";
import { SellWeightEntryDialog } from "@/features/sell/SellWeightEntryDialog";
import { useMediaMin } from "@/hooks/useMediaQuery";
import { cn } from "@/lib/cn";
import {
  canIncrementStorefrontQuantity,
  isKiloStorefrontProduct,
} from "@/features/customer-ordering/storefront-availability";
import {
  personalCustomerRelationshipLabel,
  personalStoreDisplayName,
} from "@/features/customer-ordering/format-personal-store-label";
import { useLinkedMerchantShopContext } from "@/features/customer-ordering/useLinkedMerchantShopContext";
import { useLinkedMerchantsOrderingProbes } from "@/features/customer-ordering/useLinkedMerchantsOrderingProbes";
import { normalizePublicBranchId } from "@/features/store/business-qr-url";
import { peekStoreAcquisitionIntent } from "@/features/store/store-acquisition";
import { useI18n } from "@/i18n/I18nProvider";
import { personalPageBackNav } from "@/navigation/page-back-nav";
import { useSession } from "@/session/SessionProvider";

const STOREFRONT_PAGE_SIZE = 40;

function toWeightDialogProduct(
  product: CustomerStorefrontProductDto,
  organizationId: string,
): PosCatalogProductDto {
  return {
    productId: product.productId,
    organizationId,
    name: product.name,
    sku: product.sku ?? null,
    unitOfMeasure: product.unitOfMeasure,
    sellingMode: "ByWeight",
    sellingPrice: product.unitPrice,
    effectiveSellingPrice: product.unitPrice,
    status: "Active",
    createdAtUtc: "1970-01-01T00:00:00.000Z",
    updatedAtUtc: "1970-01-01T00:00:00.000Z",
    isTracked: product.tracksInventory,
    onHandQuantity: product.availableQuantity ?? undefined,
    stockStatus: product.availabilityStatus,
  };
}

export function MerchantShopPage() {
  const { t } = useI18n();
  const { session } = useSession();
  const navigate = useNavigate();
  const online = useBrowserOnline();
  const { organizationId = "" } = useParams();
  const [searchParams] = useSearchParams();
  const { cart, merchandiseSubtotal, ensureMerchant, increment, setQuantity, clearLines, quantityOf } =
    usePersonalMerchantCart();
  const sideCartLayout = useMediaMin(1024);
  const [cartSheetOpen, setCartSheetOpen] = useState(false);
  const [weightProduct, setWeightProduct] = useState<CustomerStorefrontProductDto | null>(null);
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const lockedBranchId = useMemo(() => {
    const fromQuery = normalizePublicBranchId(searchParams.get("branchId"));
    if (fromQuery) {
      return fromQuery;
    }
    return peekStoreAcquisitionIntent()?.branchId ?? null;
  }, [searchParams]);
  const [branchId, setBranchId] = useState<string | null>(() => lockedBranchId);
  const [categoryId, setCategoryId] = useState("all");
  const [tokenReady, setTokenReady] = useState(false);
  const [tokenError, setTokenError] = useState<string | null>(null);

  useEffect(() => {
    if (lockedBranchId) {
      setBranchId(lockedBranchId);
    }
  }, [lockedBranchId]);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  useEffect(() => {
    let cancelled = false;
    void (async () => {
      const result = await ensurePersonalBuyerPosToken();
      if (cancelled) return;
      if (result.ok) {
        setTokenReady(true);
        setTokenError(null);
      } else {
        setTokenReady(false);
        setTokenError(result.detail);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const workspace = useMemo(
    () => (organizationId ? sellerWorkspace(organizationId, branchId) : null),
    [organizationId, branchId],
  );
  const merchantContextQuery = useLinkedMerchantShopContext(organizationId, Boolean(organizationId));
  const platformBusinessCustomerId = merchantContextQuery.data?.businessCustomerId;

  const query = useInfiniteQuery({
    queryKey: ["storefront", organizationId, branchId, debounced, categoryId, platformBusinessCustomerId],
    enabled: Boolean(workspace) && tokenReady && online && Boolean(platformBusinessCustomerId),
    initialPageParam: 1,
    queryFn: ({ pageParam, signal }) =>
      getCustomerStorefront(
        workspace!,
        organizationId,
        {
          search: debounced || undefined,
          categoryId: categoryId === "all" ? undefined : categoryId,
          fulfillmentBranchId: branchId ?? undefined,
          page: pageParam,
          pageSize: STOREFRONT_PAGE_SIZE,
          platformBusinessCustomerId: platformBusinessCustomerId ?? undefined,
        },
        signal,
      ),
    getNextPageParam: (lastPage) => {
      const loaded = lastPage.page * lastPage.pageSize;
      return loaded < lastPage.productTotalCount ? lastPage.page + 1 : undefined;
    },
    meta: { suppressGlobalError: true, operation: "load merchant storefront" },
  });
  const storefront = query.data?.pages[0] ?? null;
  const products = useMemo(
    () => query.data?.pages.flatMap((page) => page.products) ?? [],
    [query.data?.pages],
  );
  const productsById = useMemo(() => {
    const map = new Map<string, CustomerStorefrontProductDto>();
    for (const product of products) {
      map.set(product.productId, product);
    }
    return map;
  }, [products]);
  const orderingUnavailable =
    (query.isError && isCustomerOrderingUnavailable(query.error)) ||
    (storefront !== null && !storefront.canCustomerOrder);
  const { byOrganizationId } = useLinkedMerchantsOrderingProbes(
    organizationId
      ? [
          {
            organizationId,
            platformBusinessCustomerId: platformBusinessCustomerId ?? null,
          },
        ]
      : [],
    tokenReady && Boolean(organizationId) && online && Boolean(platformBusinessCustomerId),
  );
  const orderingProbe = byOrganizationId.get(organizationId);

  useEffect(() => {
    if (storefront) {
      ensureMerchant(storefront.organizationId, storefront.organizationDisplayName);
      if (lockedBranchId) {
        // Branch QR / ?branchId= must keep exact branch — never silently fall back.
        return;
      }
      if (!branchId && storefront.branches.length > 0) {
        setBranchId(storefront.branches[0].branchId);
      }
    }
  }, [storefront, ensureMerchant, branchId, lockedBranchId]);

  const lockedBranchMissing =
    Boolean(lockedBranchId) &&
    Boolean(storefront) &&
    !storefront!.branches.some((b) => b.branchId.toLowerCase() === lockedBranchId);

  const pageShell =
    "personal-page personal-commerce-page merchant-shop-page exits-page flex min-w-0 flex-col gap-3";
  const storeName = personalStoreDisplayName(
    storefront?.organizationDisplayName ?? merchantContextQuery.data?.organizationDisplayName,
  );
  const relationshipLabel = personalCustomerRelationshipLabel(
    merchantContextQuery.data?.customerDisplayName,
    session?.displayName,
  );

  function shopPageHeader() {
    return (
      <PageHeader
        title={t("personal.shopLink")}
        description={t("personal.shopLede")}
        backTo={personalPageBackNav.merchants.to}
        backLabel={t(personalPageBackNav.merchants.labelKey)}
        backTestId="page-header-back-merchant-shop"
      />
    );
  }

  function shopIdentity(canCustomerOrder: boolean, orderingPending = false) {
    if (!storeName) {
      return null;
    }
    return (
      <PersonalStoreIdentityCard
        storeName={storeName}
        relationshipLabel={relationshipLabel}
        canCustomerOrder={canCustomerOrder}
        orderingPending={orderingPending}
        headingLevel="h2"
      />
    );
  }

  if (!tokenReady && !tokenError) {
    return (
      <div className={pageShell}>
        {shopPageHeader()}
        <LoadingSkeleton label={t("loading.label")} />
      </div>
    );
  }

  if (tokenError) {
    return (
      <div className={pageShell}>
        {shopPageHeader()}
        <ErrorState title={t("orders.error")} detail={tokenError} />
      </div>
    );
  }

  if (!online) {
    return (
      <div className={pageShell} data-testid="merchant-shop-offline">
        {shopPageHeader()}
        <PersonalCommerceNav active="stores" />
        {shopIdentity(false, Boolean(orderingProbe?.pending))}
        <section
          className="pc-commerce-status exits-animate-panel"
          data-testid="merchant-shop-offline-panel"
        >
          <span
            className="pc-commerce-status__icon-wrap pc-commerce-status__icon-wrap--offline"
            aria-hidden
          >
            <WifiOff className="pc-commerce-status__icon" />
          </span>
          <EmptyState
              variant="setup"
              align="center"
              icon={<Users className="size-5" strokeWidth={1.75} />}
            title={t("offline.internetRequiredTitle")}
            detail={t("offline.internetRequiredDetail")}
          />
        </section>
      </div>
    );
  }

  if (
    merchantContextQuery.isLoading ||
    query.isLoading ||
    (tokenReady && online && !platformBusinessCustomerId && !merchantContextQuery.isError)
  ) {
    return (
      <div className={pageShell}>
        {shopPageHeader()}
        <PersonalCommerceNav active="stores" />
        {shopIdentity(
          Boolean(orderingProbe?.resolved && orderingProbe.canCustomerOrder),
          !orderingProbe || orderingProbe.pending || merchantContextQuery.isLoading,
        )}
        <LoadingSkeleton label={t("loading.label")} />
      </div>
    );
  }

  if (
    orderingUnavailable ||
    (!platformBusinessCustomerId && merchantContextQuery.isFetched) ||
    (orderingProbe?.resolved === true && !orderingProbe.canCustomerOrder)
  ) {
    return (
      <div className={pageShell} data-testid="merchant-shop-unavailable">
        {shopPageHeader()}
        <PersonalCommerceNav active="stores" />
        <OrderingUnavailablePanel
          storeName={storeName || t("personal.shopLink")}
          relationshipLabel={relationshipLabel}
          statementTo={merchantContextQuery.data?.statementTo}
        />
      </div>
    );
  }

  if (query.isError || !storefront) {
    return (
      <div className={pageShell} data-testid="merchant-shop-error">
        {shopPageHeader()}
        <PersonalCommerceNav active="stores" />
        {shopIdentity(
          Boolean(orderingProbe?.resolved && orderingProbe.canCustomerOrder),
          !orderingProbe || orderingProbe.pending,
        )}
        <ErrorState
          title={t("orders.error")}
          detail={query.error instanceof Error ? query.error.message : t("error.detail")}
        />
        <div className="exits-animate-toolbar">
          <ActionTileGrid
            tiles={[
              {
                key: "retry",
                label: t("orders.retry"),
                icon: RefreshCw,
                onClick: () => void query.refetch(),
              },
            ]}
          />
        </div>
      </div>
    );
  }

  if (lockedBranchMissing) {
    return (
      <div className={pageShell} data-testid="merchant-shop-branch-missing">
        {shopPageHeader()}
        <PersonalCommerceNav active="stores" />
        {shopIdentity(storefront.canCustomerOrder)}
        <ErrorState
          title={t("orders.branchUnavailableTitle")}
          detail={t("orders.branchUnavailableDetail")}
        />
      </div>
    );
  }

  const selectedBranch =
    storefront.branches.find((branch) => branch.branchId === branchId) ??
    storefront.branches[0] ??
    null;
  const showFloatingCart = !sideCartLayout && !cartSheetOpen && cart.lines.length > 0;
  const checkoutBlock = shopCheckoutBlocker(
    storefront.branches,
    storefront.canCustomerDelivery,
    storefront.fulfillmentAvailability,
  );
  const continueToCheckout = () => {
    if (checkoutBlock) {
      return;
    }
    navigate(`/personal/linked-merchants/${organizationId}/shop/checkout`);
  };
  const cartPanel = (sheet: boolean) => (
    <ShopOrderCart
      lines={cart.lines}
      productsById={productsById}
      workspace={workspace}
      sellerOrganizationId={organizationId}
      subtotal={merchandiseSubtotal}
      showClose={sheet}
      onClose={sheet ? () => setCartSheetOpen(false) : undefined}
      onChangeQuantity={setQuantity}
      onEditWeight={setWeightProduct}
      onClear={clearLines}
      onContinue={continueToCheckout}
      continueBlockedDetail={checkoutBlock ? t(checkoutBlock) : null}
    />
  );

  return (
    <div className={`${pageShell} merchant-shop-page--floor`} data-testid="merchant-shop-page">
      <PageHeader
        variant="compact"
        title={storeName || t("personal.shopLink")}
        subtitle={selectedBranch?.name}
        backTo={personalPageBackNav.merchants.to}
        backLabel={t(personalPageBackNav.merchants.labelKey)}
        backTestId="page-header-back-merchant-shop"
      />
      <div className="sell-floor-root flex min-h-0 min-w-0 flex-1 flex-col" data-testid="shop-order-floor">
        <div className="sell-floor-layout flex min-h-0 min-w-0 flex-1 flex-col">
          <section
            className={cn(
              "sell-floor-workspace sell-floor-browse flex min-h-0 min-w-0 flex-1 flex-col",
              showFloatingCart && "pb-[calc(5.5rem+env(safe-area-inset-bottom))]",
            )}
          >
            {storefront.branches.length > 1 && !lockedBranchId ? (
              <div className="sell-floor-workspace__search">
                <label className="pc-field">
                  <span className="pc-field__label">{t("orders.branch")}</span>
                  <select
                    className="pc-field__control"
                    data-testid="shop-branch-select"
                    value={branchId ?? ""}
                    onChange={(event) => setBranchId(event.target.value || null)}
                  >
                    {storefront.branches.map((branch) => (
                      <option key={branch.branchId} value={branch.branchId}>
                        {branch.name}
                        {branch.onlineOrdersPaused ? ` (${t("orders.paused")})` : ""}
                      </option>
                    ))}
                  </select>
                </label>
              </div>
            ) : null}
            <div className="sell-floor-workspace__search">
              <SearchField
                label={t("sell.searchLabel")}
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                onClear={() => setSearch("")}
                placeholder={t("sell.searchPlaceholder")}
              />
            </div>
            <div className="sell-floor-workspace__categories">
              <SellCategoryFilter
                categories={storefront.categories.map((category) => ({
                  categoryId: category.categoryId,
                  name: category.name,
                }))}
                activeCategoryId={categoryId}
                allLabel={t("sell.categoryAll")}
                listLabel={t("sell.categoriesLabel")}
                onSelect={setCategoryId}
              />
            </div>
            <div
              className="sell-floor-product-pane sell-product-grid sell-product-grid--enter min-h-0 flex-1 content-start items-start overflow-y-auto overscroll-contain"
              data-testid="storefront-products"
            >
              {products.length === 0 ? (
                <div className="col-span-full">
                  <EmptyState
                    align="center"
                    icon={<Users className="size-5" strokeWidth={1.75} />}
                    title={t("orders.noProductsTitle")}
                    detail={t("orders.noProductsDetail")}
                  />
                </div>
              ) : (
                products.map((product) => {
                  const qty = quantityOf(product.productId);
                  const kilo = isKiloStorefrontProduct(product);
                  const canAdd = kilo
                    ? product.isAvailable &&
                      product.unitPrice > 0 &&
                      (!product.tracksInventory || (product.availableQuantity ?? 0) > 0 || qty > 0)
                    : canIncrementStorefrontQuantity(product, qty);
                  return (
                    <StoreProductCard
                      key={product.productId}
                      product={product}
                      workspace={workspace}
                      sellerOrganizationId={organizationId}
                      quantity={qty}
                      canAdd={canAdd}
                      onAdd={() => {
                        if (kilo) {
                          setWeightProduct(product);
                          return;
                        }
                        increment(product);
                      }}
                      t={t}
                    />
                  );
                })
              )}
              {query.hasNextPage ? (
                <div className="col-span-full">
                  <CommerceLoadMore
                    label={t("inventory.loadMore")}
                    loadingLabel={t("loading.label")}
                    busy={query.isFetchingNextPage}
                    testId="storefront-load-more"
                    onClick={() => void query.fetchNextPage()}
                  />
                </div>
              ) : null}
            </div>
          </section>
          <aside
            className={cn(
              "sell-cart-landscape sell-cart-shell min-h-0 min-w-0 flex-col overflow-hidden",
              !sideCartLayout && "hidden",
            )}
            aria-label={t("sell.cartLabel")}
            aria-hidden={!sideCartLayout}
          >
            {sideCartLayout ? cartPanel(false) : null}
          </aside>
        </div>
      </div>

      {showFloatingCart ? (
        <button
          type="button"
          className="sell-cart-floating sell-cart-bar sell-cart-bar--filled"
          onClick={() => setCartSheetOpen(true)}
          aria-expanded={cartSheetOpen}
          aria-label={t("sell.floatingCartView")}
        >
          <span className="sell-cart-bar__summary">
            <span className="sell-cart-bar__icon" aria-hidden>
              <Banknote className="size-5" strokeWidth={2} />
            </span>
            <span className="sell-cart-bar__copy">
              <span className="sell-cart-bar__count">
                {cart.lines.length}{" "}
                {cart.lines.length === 1 ? t("sell.cartItemSingular") : t("sell.cartItemPlural")}
              </span>
              <span className="sell-cart-bar__total">₱{merchandiseSubtotal.toFixed(2)}</span>
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
            className="sell-cart-sheet fixed inset-x-0 bottom-0 z-40 flex h-[min(88dvh,calc(100dvh-env(safe-area-inset-top,0px)))] max-h-[min(88dvh,calc(100dvh-env(safe-area-inset-top,0px)))] flex-col gap-2 overflow-hidden border border-border border-b-0 bg-surface px-4 pt-2 pb-[max(0.75rem,env(safe-area-inset-bottom))] pl-[max(1rem,env(safe-area-inset-left))] pr-[max(1rem,env(safe-area-inset-right))] shadow-[0_-8px_32px_rgba(0,0,0,0.18)]"
          >
            <div className="sell-cart-sheet__handle" aria-hidden>
              <span className="sell-cart-sheet__handle-bar" />
            </div>
            {cartPanel(true)}
          </div>
        </>
      ) : null}

      <SellWeightEntryDialog
        open={weightProduct != null}
        product={weightProduct ? toWeightDialogProduct(weightProduct, organizationId) : null}
        initialKilograms={weightProduct ? quantityOf(weightProduct.productId) : null}
        maxKilograms={
          weightProduct?.tracksInventory ? (weightProduct.availableQuantity ?? null) : null
        }
        stockHint={
          weightProduct
            ? {
                isTracked: weightProduct.tracksInventory,
                onHandQuantity: weightProduct.availableQuantity,
                stockStatus: weightProduct.availabilityStatus,
              }
            : null
        }
        onConfirm={(kilograms) => {
          if (weightProduct) {
            setQuantity(weightProduct, kilograms);
          }
          setWeightProduct(null);
        }}
        onRemove={
          weightProduct && quantityOf(weightProduct.productId) > 0
            ? () => {
                setQuantity(weightProduct, 0);
                setWeightProduct(null);
              }
            : undefined
        }
        onCancel={() => setWeightProduct(null)}
      />
    </div>
  );
}
