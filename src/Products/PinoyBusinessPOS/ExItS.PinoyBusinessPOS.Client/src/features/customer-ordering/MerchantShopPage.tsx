import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useInfiniteQuery } from "@tanstack/react-query";
import { ensurePersonalBuyerPosToken } from "@/api/platform/personal-buyer-token";
import {
  getCustomerStorefront,
  sellerWorkspace,
  type CustomerStorefrontProductDto,
} from "@/api/pos/pos-customer-orders-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { usePersonalMerchantCart } from "@/features/customer-ordering/PersonalMerchantCartProvider";
import { StorefrontProductThumbnail } from "@/features/customer-ordering/StorefrontProductThumbnail";
import { canIncrementStorefrontQuantity } from "@/features/customer-ordering/storefront-availability";
import { SellCategoryFilter } from "@/features/sell/SellCategoryFilter";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { personalPageBackNav } from "@/navigation/page-back-nav";

const STOREFRONT_PAGE_SIZE = 40;

function money(n: number): string {
  return `₱${n.toFixed(2)}`;
}

function availabilityLabel(
  t: (key: MessageKey) => string,
  product: CustomerStorefrontProductDto,
): string {
  switch (product.availabilityStatus) {
    case "OutOfStock":
      return t("orders.availabilityOut");
    case "LowStock":
      return t("orders.availabilityLow");
    case "InStock":
      return t("orders.availabilityIn");
    default:
      return t("orders.availabilityUntracked");
  }
}

export function MerchantShopPage() {
  const { t } = useI18n();
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
  });

  const storefront = query.data?.pages[0] ?? null;
  const products = useMemo(
    () => query.data?.pages.flatMap((page) => page.products) ?? [],
    [query.data?.pages],
  );

  useEffect(() => {
    if (storefront) {
      ensureMerchant(storefront.organizationId, storefront.organizationDisplayName);
      if (!branchId && storefront.branches.length > 0) {
        setBranchId(storefront.branches[0].branchId);
      }
    }
  }, [storefront, ensureMerchant, branchId]);

  if (!tokenReady && !tokenError) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (tokenError) {
    return <ErrorState title={t("orders.error")} detail={tokenError} />;
  }

  if (!online) {
    return (
      <div className="personal-page exits-page flex min-w-0 flex-col gap-4" data-testid="merchant-shop-offline">
        <PageHeader
          title={t("personal.shopLede")}
          description={t("personal.shopLede")}
          backTo={personalPageBackNav.merchants.to}
          backLabel={t(personalPageBackNav.merchants.labelKey)}
          backTestId="page-header-back-merchant-shop"
        />
        <EmptyState
          title={t("offline.internetRequiredTitle")}
          detail={t("offline.internetRequiredDetail")}
        />
      </div>
    );
  }

  if (query.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (query.isError || !storefront) {
    return (
      <div className="personal-page exits-page flex min-w-0 flex-col gap-4">
        <PageHeader
          title={t("personal.shopLede")}
          backTo={personalPageBackNav.merchants.to}
          backLabel={t(personalPageBackNav.merchants.labelKey)}
          backTestId="page-header-back-merchant-shop"
        />
        <ErrorState
          title={t("orders.error")}
          detail={query.error instanceof Error ? query.error.message : t("error.detail")}
        />
        <Button type="button" className="min-h-11 w-fit" onClick={() => void query.refetch()}>
          {t("orders.retry")}
        </Button>
      </div>
    );
  }

  if (!storefront.canCustomerOrder) {
    return (
      <div
        className="personal-page exits-page flex min-w-0 flex-col gap-4"
        data-testid="merchant-shop-unavailable"
      >
        <PageHeader
          title={storefront.organizationDisplayName}
          description={t("personal.shopLede")}
          backTo={personalPageBackNav.merchants.to}
          backLabel={t(personalPageBackNav.merchants.labelKey)}
          backTestId="page-header-back-merchant-shop"
        />
        <EmptyState
          title={t("personal.orderingUnavailable")}
          detail={t("personal.orderingUnavailableDetail")}
        />
      </div>
    );
  }

  return (
    <div
      className="personal-page exits-page flex min-w-0 flex-col gap-4"
      data-testid="merchant-shop-page"
    >
      <PageHeader
        title={storefront.organizationDisplayName}
        description={t("personal.shopLede")}
        backTo={personalPageBackNav.merchants.to}
        backLabel={t(personalPageBackNav.merchants.labelKey)}
        backTestId="page-header-back-merchant-shop"
      />

      {storefront.branches.length > 1 ? (
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
          <span>{t("orders.branch")}</span>
          <select
            className="min-h-11 rounded border px-3"
            data-testid="shop-branch-select"
            value={branchId ?? ""}
            onChange={(e) => setBranchId(e.target.value || null)}
          >
            {storefront.branches.map((b) => (
              <option key={b.branchId} value={b.branchId}>
                {b.name}
                {b.onlineOrdersPaused ? ` (${t("orders.paused")})` : ""}
              </option>
            ))}
          </select>
        </label>
      ) : null}

      <SearchField
        label={t("orders.searchProducts")}
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("orders.searchProducts")}
      />

      {storefront.categories.length > 0 ? (
        <SellCategoryFilter
          categories={storefront.categories.map((c) => ({
            categoryId: c.categoryId,
            name: c.name,
          }))}
          activeCategoryId={categoryId}
          allLabel={t("catalogGlobal.allCategories")}
          listLabel={t("catalogGlobal.allCategories")}
          onSelect={setCategoryId}
        />
      ) : null}

      {products.length === 0 ? (
        <EmptyState title={t("orders.noProductsTitle")} detail={t("orders.noProductsDetail")} />
      ) : (
        <ul className="m-0 grid list-none grid-cols-1 gap-3 p-0 sm:grid-cols-2">
          {products.map((product) => {
            const qty = quantityOf(product.productId);
            const canAdd = canIncrementStorefrontQuantity(product, qty);
            return (
              <li key={product.productId}>
                <Card className="storefront-product-card flex flex-col gap-2" data-testid="storefront-product">
                  <div className="storefront-product-card__header flex gap-3">
                    <StorefrontProductThumbnail
                      workspace={workspace}
                      sellerOrganizationId={organizationId}
                      product={product}
                    />
                    <div className="min-w-0 flex-1">
                      <div className="flex flex-wrap items-start justify-between gap-2">
                        <strong className="min-w-0">{product.name}</strong>
                        <StatusChip tone={product.isAvailable ? "success" : "danger"}>
                          {availabilityLabel(t, product)}
                        </StatusChip>
                      </div>
                      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                        {money(product.unitPrice)} / {product.unitOfMeasure}
                        {product.tracksInventory && product.availableQuantity != null
                          ? ` · ${product.availableQuantity}`
                          : ""}
                      </p>
                    </div>
                  </div>
                  <div className="flex flex-wrap items-center gap-2">
                    <Button
                      type="button"
                      variant="ghost"
                      className="min-h-11"
                      disabled={qty <= 0}
                      data-testid="cart-decrement"
                      onClick={() => decrement(product.productId)}
                    >
                      −
                    </Button>
                    <span data-testid="cart-qty">{qty}</span>
                    <Button
                      type="button"
                      className="min-h-11"
                      disabled={!canAdd}
                      data-testid="cart-increment"
                      onClick={() => increment(product)}
                    >
                      +
                    </Button>
                  </div>
                </Card>
              </li>
            );
          })}
        </ul>
      )}

      {query.hasNextPage ? (
        <Button
          type="button"
          variant="outline"
          className="min-h-11 w-full"
          data-testid="storefront-load-more"
          disabled={query.isFetchingNextPage}
          onClick={() => void query.fetchNextPage()}
        >
          {query.isFetchingNextPage ? t("loading.label") : t("inventory.loadMore")}
        </Button>
      ) : null}

      {itemCount > 0 ? (
        <div
          className="sticky bottom-2 flex flex-wrap items-center justify-between gap-2 rounded border bg-[var(--exits-surface,white)] p-3"
          data-testid="shop-cart-bar"
        >
          <span data-testid="shop-cart-summary">
            {itemCount} · {money(merchandiseSubtotal)}
          </span>
          <Button
            type="button"
            className="min-h-11"
            data-testid="shop-review"
            disabled={!cart.sellerOrganizationId}
            onClick={() => navigate(`/personal/linked-merchants/${organizationId}/shop/checkout`)}
          >
            {t("orders.reviewOrder")}
          </Button>
        </div>
      ) : null}
    </div>
  );
}
