import { useEffect, useMemo, useState } from "react";
import { useInfiniteQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import {
  CalendarClock,
  Link2,
  Package,
  Receipt,
  RefreshCw,
  ShoppingBag,
  Truck,
} from "lucide-react";
import { ensurePersonalBuyerPosToken } from "@/api/platform/personal-buyer-token";
import { listLinkedMerchants, type LinkedMerchantDto } from "@/api/platform/linked-merchants-client";
import { Button } from "@/components/ui/button";
import { ActionTileGrid } from "@/components/exits/ActionTileGrid";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import {
  CommerceLoadMore,
  MerchantOrderingBadge,
  storeDisplayInitial,
} from "@/features/customer-ordering/personal-commerce-ui";
import { PersonalCommerceNav } from "@/features/customer-ordering/PersonalCommerceNav";
import {
  type MerchantOrderingProbe,
  useLinkedMerchantsOrderingProbes,
} from "@/features/customer-ordering/useLinkedMerchantsOrderingProbes";
import { useI18n } from "@/i18n/I18nProvider";
import { personalPageBackNav } from "@/navigation/page-back-nav";

const MERCHANTS_PAGE_SIZE = 50;

function LinkedMerchantStoreCard({
  merchant,
  index,
  ordering,
}: {
  merchant: LinkedMerchantDto;
  index: number;
  ordering: MerchantOrderingProbe;
}) {
  const { t } = useI18n();
  const statementTo = `/personal/linked-merchants/${merchant.organizationId}/${merchant.businessCustomerId}`;
  const shopTo = `/personal/linked-merchants/${merchant.organizationId}/shop`;
  const canCustomerOrder = ordering.resolved && ordering.canCustomerOrder;
  const canCustomerDelivery = ordering.resolved && ordering.canCustomerDelivery;

  return (
    <li
      className="pc-store-directory__item"
      style={{ animationDelay: `${Math.min(index, 8) * 45 + 40}ms` }}
    >
      <article className="pc-store-card" data-testid="linked-merchant-card">
        <div className="pc-store-card__top">
          <span className="pc-store-card__avatar" aria-hidden>
            {storeDisplayInitial(merchant.organizationDisplayName)}
          </span>
          <div className="pc-store-card__body">
            <div className="pc-store-card__identity pc-store-card__identity--inline">
              <h3 className="pc-store-card__name">{merchant.organizationDisplayName}</h3>
              <Link2 className="pc-store-card__link-icon size-3.5 shrink-0" aria-hidden />
              <span className="pc-store-card__relationship">{merchant.customerDisplayName}</span>
            </div>
            <div className="pc-store-card__badge-row">
              <MerchantOrderingBadge
                available={canCustomerOrder}
                pending={ordering.pending}
              />
            </div>
          </div>
        </div>

        <div className="pc-store-card__meta">
          <span className="pc-store-card__meta-item">
            <CalendarClock className="size-3.5 shrink-0" aria-hidden />
            {new Date(merchant.linkedAtUtc).toLocaleDateString()}
          </span>
          {canCustomerOrder ? (
            canCustomerDelivery ? (
              <span className="pc-store-card__meta-item">
                <Truck className="size-3.5 shrink-0" aria-hidden />
                {t("orders.delivery")}
              </span>
            ) : (
              <span className="pc-store-card__meta-item">
                <Package className="size-3.5 shrink-0" aria-hidden />
                {t("orders.pickup")}
              </span>
            )
          ) : null}
        </div>

        <div
          className={
            canCustomerOrder
              ? "pc-store-card__actions"
              : "pc-store-card__actions pc-store-card__actions--solo"
          }
        >
          {canCustomerOrder ? (
            <>
              <Button
                asChild
                className="pc-store-card__action pc-store-card__action--shop"
                data-testid="open-merchant-shop"
              >
                <Link to={shopTo}>
                  <ShoppingBag className="pc-store-card__action-icon size-4 shrink-0" aria-hidden />
                  {t("personal.shopLink")}
                </Link>
              </Button>
              <Button
                asChild
                variant="outline"
                className="pc-store-card__action pc-store-card__action--statement"
                data-testid="open-merchant-statement"
              >
                <Link to={statementTo}>
                  <Receipt className="pc-store-card__action-icon size-4 shrink-0" aria-hidden />
                  {t("personal.merchantStatement.openPurchases")}
                </Link>
              </Button>
            </>
          ) : (
            <Button
              asChild
              className="pc-store-card__action pc-store-card__action--statement"
              data-testid="open-merchant-statement"
            >
              <Link to={statementTo}>
                <Receipt className="pc-store-card__action-icon size-4 shrink-0" aria-hidden />
                {t("personal.merchantStatement.openPurchases")}
              </Link>
            </Button>
          )}
        </div>
      </article>
    </li>
  );
}

export function LinkedMerchantsPage() {
  const { t } = useI18n();
  const [buyerTokenReady, setBuyerTokenReady] = useState(false);
  const query = useInfiniteQuery({
    queryKey: ["personal", "linked-merchants"],
    initialPageParam: 1,
    queryFn: ({ pageParam, signal }) => listLinkedMerchants(pageParam, MERCHANTS_PAGE_SIZE, signal),
    getNextPageParam: (lastPage) => {
      const loaded = lastPage.page * lastPage.pageSize;
      return loaded < lastPage.totalCount ? lastPage.page + 1 : undefined;
    },
  });

  useEffect(() => {
    let cancelled = false;
    void ensurePersonalBuyerPosToken().then((result) => {
      if (!cancelled) {
        setBuyerTokenReady(result.ok);
      }
    });
    return () => {
      cancelled = true;
    };
  }, []);

  const items = query.data?.pages.flatMap((page) => page.items) ?? [];
  const organizationIds = useMemo(
    () => items.map((merchant) => merchant.organizationId),
    [items],
  );
  const { byOrganizationId } = useLinkedMerchantsOrderingProbes(
    organizationIds,
    buyerTokenReady && items.length > 0,
  );

  const pageShell = "personal-page personal-commerce-page linked-merchants-page exits-page flex min-w-0 flex-col gap-3";

  if (query.isLoading) {
    return (
      <div className={pageShell}>
        <PageHeader
          title={t("personal.merchantsTitle")}
          description={t("personal.merchantsLede")}
          backTo={personalPageBackNav.more.to}
          backLabel={t(personalPageBackNav.more.labelKey)}
          backTestId="page-header-back-linked-merchants"
        />
        <LoadingSkeleton label={t("loading.label")} />
      </div>
    );
  }

  if (query.isError) {
    return (
      <div className={pageShell}>
        <PageHeader
          title={t("personal.merchantsTitle")}
          description={t("personal.merchantsLede")}
          backTo={personalPageBackNav.more.to}
          backLabel={t(personalPageBackNav.more.labelKey)}
          backTestId="page-header-back-linked-merchants"
        />
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
    <div className={pageShell} data-testid="linked-merchants-page">
      <PageHeader
        title={t("personal.merchantsTitle")}
        description={t("personal.merchantsLede")}
        backTo={personalPageBackNav.more.to}
        backLabel={t(personalPageBackNav.more.labelKey)}
        backTestId="page-header-back-linked-merchants"
      />

      <PersonalCommerceNav active="stores" />

      {items.length === 0 ? (
        <div className="pc-empty-panel exits-animate-panel flex flex-col gap-3">
          <EmptyState
            title={t("personal.merchantsEmptyTitle")}
            detail={t("personal.merchantsEmptyDetail")}
          />
          <Button asChild className="min-h-11 w-full gap-2" data-testid="open-customer-links-empty">
            <Link to="/personal/customer-links">
              <Link2 className="size-4 shrink-0" aria-hidden />
              {t("personal.customerLinks.title")}
            </Link>
          </Button>
        </div>
      ) : (
        <section
          className="pc-store-list-section exits-animate-panel flex flex-col gap-3"
          aria-label={t("personal.merchantsTitle")}
        >
          <div className="pc-store-list-section__header">
            <h2 className="pc-section-heading">{t("personal.merchantsTitle")}</h2>
            <span className="pc-store-list-section__count">{items.length}</span>
          </div>
          <ul className="pc-store-directory">
            {items.map((merchant, index) => (
              <LinkedMerchantStoreCard
                key={merchant.linkedCustomerId}
                merchant={merchant}
                index={index}
                ordering={
                  byOrganizationId.get(merchant.organizationId) ?? {
                    canCustomerOrder: false,
                    canCustomerDelivery: false,
                    pending: true,
                    resolved: false,
                  }
                }
              />
            ))}
          </ul>

          {query.hasNextPage ? (
            <CommerceLoadMore
              label={t("inventory.loadMore")}
              loadingLabel={t("loading.label")}
              busy={query.isFetchingNextPage}
              testId="linked-merchants-load-more"
              onClick={() => void query.fetchNextPage()}
            />
          ) : null}
        </section>
      )}
    </div>
  );
}
