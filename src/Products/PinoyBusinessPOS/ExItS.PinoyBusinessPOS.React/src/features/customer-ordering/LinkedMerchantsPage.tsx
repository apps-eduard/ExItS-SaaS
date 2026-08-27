import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useInfiniteQuery } from "@tanstack/react-query";
import { Link2, RefreshCw } from "lucide-react";
import { ensurePersonalBuyerPosToken } from "@/api/platform/personal-buyer-token";
import { listLinkedMerchants } from "@/api/platform/linked-merchants-client";
import { Button } from "@/components/ui/button";
import { ActionTileGrid } from "@/components/exits/ActionTileGrid";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { LinkedMerchantsListSection } from "@/features/customer-ordering/LinkedMerchantsListSection";
import { PersonalCommerceNav } from "@/features/customer-ordering/PersonalCommerceNav";
import { useLinkedMerchantsOrderingProbes } from "@/features/customer-ordering/useLinkedMerchantsOrderingProbes";
import { selectCanonicalLinkedMerchantPerStore } from "@/features/customer-ordering/select-canonical-linked-merchant";
import { useI18n } from "@/i18n/I18nProvider";
import { personalPageBackNav } from "@/navigation/page-back-nav";

const MERCHANTS_PAGE_SIZE = 50;

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

  const items = selectCanonicalLinkedMerchantPerStore(
    query.data?.pages.flatMap((page) => page.items) ?? [],
  );
  const organizationIds = useMemo(
    () => items.map((merchant) => merchant.organizationId),
    [items],
  );
  const { byOrganizationId } = useLinkedMerchantsOrderingProbes(
    organizationIds,
    buyerTokenReady && items.length > 0,
  );

  const rows = useMemo(
    () =>
      items.map((merchant) => ({
        merchant,
        ordering:
          byOrganizationId.get(merchant.organizationId) ?? {
            canCustomerOrder: false,
            canCustomerDelivery: false,
            pending: true,
            resolved: false,
          },
      })),
    [byOrganizationId, items],
  );

  const pageShell =
    "personal-page personal-commerce-page linked-merchants-page exits-page flex min-w-0 flex-col gap-3";

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
        <LinkedMerchantsListSection
          rows={rows}
          hasNextPage={Boolean(query.hasNextPage)}
          isFetchingNextPage={query.isFetchingNextPage}
          onLoadMore={() => void query.fetchNextPage()}
        />
      )}
    </div>
  );
}
