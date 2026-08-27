import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useInfiniteQuery } from "@tanstack/react-query";
import { RefreshCw, WifiOff } from "lucide-react";
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
import { useBrowserOnline } from "@/connectivity/browser-online";
import { OrderingUnavailablePanel } from "@/features/customer-ordering/OrderingUnavailablePanel";
import { PersonalStoreIdentityCard } from "@/features/customer-ordering/PersonalStoreIdentity";
import { usePersonalMerchantCart } from "@/features/customer-ordering/PersonalMerchantCartProvider";
import { PersonalCommerceNav } from "@/features/customer-ordering/PersonalCommerceNav";
import { CommerceLoadMore, ShopCartBar } from "@/features/customer-ordering/personal-commerce-ui";
import { StorefrontHeader } from "@/features/customer-ordering/StorefrontHeader";
import { StoreProductCard } from "@/features/customer-ordering/StoreProductCard";
import { canIncrementStorefrontQuantity } from "@/features/customer-ordering/storefront-availability";
import {
  personalCustomerRelationshipLabel,
  personalStoreDisplayName,
} from "@/features/customer-ordering/format-personal-store-label";
import { useLinkedMerchantShopContext } from "@/features/customer-ordering/useLinkedMerchantShopContext";
import { useLinkedMerchantsOrderingProbes } from "@/features/customer-ordering/useLinkedMerchantsOrderingProbes";
import { useI18n } from "@/i18n/I18nProvider";
import { personalPageBackNav } from "@/navigation/page-back-nav";
import { useSession } from "@/session/SessionProvider";

const STOREFRONT_PAGE_SIZE = 40;

function money(n: number): string {
  return `₱${n.toFixed(2)}`;
}

export function MerchantShopPage() {
  const { t } = useI18n();
  const { session } = useSession();
  const navigate = useNavigate();
  const online = useBrowserOnline();
  const { organizationId = "" } = useParams();
  const { cart, itemCount, merchandiseSubtotal, ensureMerchant, increment, decrement, quantityOf } =
    usePersonalMerchantCart();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [branchId, setBranchId] = useState<string | null>(null);
  const [categoryId, setCategoryId] = useState("all");
  const [tokenReady, setTokenReady] = useState(false);
  const [tokenError, setTokenError] = useState<string | null>(null);

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
  const query = useInfiniteQuery({
    queryKey: ["storefront", organizationId, branchId, debounced, categoryId],
    enabled: Boolean(workspace) && tokenReady && online,
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
  const orderingUnavailable =
    (query.isError && isCustomerOrderingUnavailable(query.error)) ||
    (storefront !== null && !storefront.canCustomerOrder);
  const merchantContextQuery = useLinkedMerchantShopContext(organizationId, Boolean(organizationId));
  const { byOrganizationId } = useLinkedMerchantsOrderingProbes(
    organizationId ? [organizationId] : [],
    tokenReady && Boolean(organizationId) && online,
  );
  const orderingProbe = byOrganizationId.get(organizationId);

  useEffect(() => {
    if (storefront) {
      ensureMerchant(storefront.organizationId, storefront.organizationDisplayName);
      if (!branchId && storefront.branches.length > 0) {
        setBranchId(storefront.branches[0].branchId);
      }
    }
  }, [storefront, ensureMerchant, branchId]);

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
            title={t("offline.internetRequiredTitle")}
            detail={t("offline.internetRequiredDetail")}
          />
        </section>
      </div>
    );
  }

  if (query.isLoading) {
    return (
      <div className={pageShell}>
        {shopPageHeader()}
        <PersonalCommerceNav active="stores" />
        {shopIdentity(
          Boolean(orderingProbe?.resolved && orderingProbe.canCustomerOrder),
          !orderingProbe || orderingProbe.pending,
        )}
        <LoadingSkeleton label={t("loading.label")} />
      </div>
    );
  }

  if (orderingUnavailable) {
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

  return (
    <div className={pageShell} data-testid="merchant-shop-page">
      {shopPageHeader()}
      <PersonalCommerceNav active="stores" />
      <StorefrontHeader
        storefront={storefront}
        branchId={branchId}
        onBranchChange={setBranchId}
        search={search}
        onSearchChange={setSearch}
        onSearchClear={() => setSearch("")}
        categoryId={categoryId}
        onCategoryChange={setCategoryId}
        relationshipLabel={relationshipLabel}
      />
      {products.length === 0 ? (
        <div className="pc-empty-panel exits-animate-panel">
          <EmptyState title={t("orders.noProductsTitle")} detail={t("orders.noProductsDetail")} />
        </div>
      ) : (
        <ul className="pc-product-grid">
          {products.map((product) => {
            const qty = quantityOf(product.productId);
            const canAdd = canIncrementStorefrontQuantity(product, qty);
            return (
              <li key={product.productId}>
                <StoreProductCard
                  product={product}
                  workspace={workspace}
                  sellerOrganizationId={organizationId}
                  quantity={qty}
                  canAdd={canAdd}
                  onIncrement={() => increment(product)}
                  onDecrement={() => decrement(product.productId)}
                  t={t}
                />
              </li>
            );
          })}
        </ul>
      )}
      {query.hasNextPage ? (
        <CommerceLoadMore
          label={t("inventory.loadMore")}
          loadingLabel={t("loading.label")}
          busy={query.isFetchingNextPage}
          testId="storefront-load-more"
          onClick={() => void query.fetchNextPage()}
        />
      ) : null}
      <ShopCartBar
        itemCount={itemCount}
        subtotalLabel={t("orders.subtotal")}
        subtotal={money(merchandiseSubtotal)}
        actionLabel={t("orders.reviewOrder")}
        disabled={!cart.sellerOrganizationId}
        onReview={() => navigate(`/personal/linked-merchants/${organizationId}/shop/checkout`)}
      />
    </div>
  );
}
