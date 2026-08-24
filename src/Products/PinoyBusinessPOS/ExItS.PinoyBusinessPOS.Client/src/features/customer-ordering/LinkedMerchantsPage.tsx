import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import {
  CalendarClock,
  ClipboardList,
  Receipt,
  RefreshCw,
  ShoppingBag,
  Store,
  Truck,
  UserRoundCheck,
} from "lucide-react";
import { listLinkedMerchants } from "@/api/platform/linked-merchants-client";
import { Button } from "@/components/ui/button";
import { ActionTileGrid } from "@/components/exits/ActionTileGrid";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { personalPageBackNav } from "@/navigation/page-back-nav";

export function LinkedMerchantsPage() {
  const { t } = useI18n();
  const query = useQuery({
    queryKey: ["personal", "linked-merchants"],
    queryFn: ({ signal }) => listLinkedMerchants(1, 50, signal),
  });

  if (query.isLoading) {
    return (
      <div className="personal-page linked-merchants-page exits-page flex min-w-0 flex-col gap-3">
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
      <div className="personal-page linked-merchants-page exits-page flex min-w-0 flex-col gap-3">
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

  const items = query.data?.items ?? [];

  return (
    <div
      className="personal-page linked-merchants-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="linked-merchants-page"
    >
      <PageHeader
        title={t("personal.merchantsTitle")}
        description={t("personal.merchantsLede")}
        backTo={personalPageBackNav.more.to}
        backLabel={t(personalPageBackNav.more.labelKey)}
        backTestId="page-header-back-linked-merchants"
      />

      <div className="exits-animate-toolbar" data-testid="linked-merchants-toolbar">
        <ActionTileGrid
          tiles={[
            {
              key: "orders",
              label: t("personal.myOrdersLink"),
              icon: ClipboardList,
              to: "/personal/orders",
              testId: "open-my-orders",
            },
            {
              key: "customer-links",
              label: t("personal.customerLinks.title"),
              icon: UserRoundCheck,
              to: "/personal/customer-links",
              testId: "open-customer-links",
            },
          ]}
        />
      </div>

      {items.length === 0 ? (
        <div className="exits-animate-panel flex flex-col gap-3">
          <EmptyState
            title={t("personal.merchantsEmptyTitle")}
            detail={t("personal.merchantsEmptyDetail")}
          />
          <ActionTileGrid
            tiles={[
              {
                key: "customer-links-empty",
                label: t("personal.customerLinks.title"),
                icon: UserRoundCheck,
                to: "/personal/customer-links",
                testId: "open-customer-links-empty",
                primary: true,
              },
            ]}
          />
        </div>
      ) : (
        <section
          className="catalog-form-section exits-animate-panel personal-section gap-2"
          aria-label={t("personal.merchantsTitle")}
        >
          <h2 className="catalog-form-section__title text-muted">{t("personal.merchantsTitle")}</h2>
          <ul className="exits-list m-0 grid list-none gap-2 p-0">
            {items.map((merchant) => {
              const statementTo = `/personal/linked-merchants/${merchant.organizationId}/${merchant.businessCustomerId}`;
              const shopTo = `/personal/linked-merchants/${merchant.organizationId}/shop`;
              return (
                <li key={merchant.linkedCustomerId}>
                  <article
                    className="exits-list__card linked-merchant-card"
                    data-testid="linked-merchant-card"
                  >
                    <div className="linked-merchant-card__header">
                      <span className="linked-merchant-card__avatar" aria-hidden>
                        <Store className="size-5" />
                      </span>
                      <div className="linked-merchant-card__heading min-w-0 flex-1">
                        <div className="linked-merchant-card__title-row">
                          <strong className="exits-list__name min-w-0 truncate">
                            {merchant.organizationDisplayName}
                          </strong>
                          {merchant.canCustomerOrder ? (
                            <StatusChip tone="success">
                              {t("personal.orderingAvailable")}
                            </StatusChip>
                          ) : (
                            <StatusChip tone="warning">
                              {t("personal.orderingUnavailable")}
                            </StatusChip>
                          )}
                        </div>
                        <p className="linked-merchant-card__customer m-0 truncate">
                          {merchant.customerDisplayName}
                        </p>
                      </div>
                    </div>

                    <div className="linked-merchant-card__meta">
                      <span className="linked-merchant-card__meta-item">
                        <CalendarClock className="size-3.5 shrink-0" aria-hidden />
                        <span>{new Date(merchant.linkedAtUtc).toLocaleDateString()}</span>
                      </span>
                      {merchant.canCustomerDelivery ? (
                        <span className="linked-merchant-card__meta-item">
                          <Truck className="size-3.5 shrink-0" aria-hidden />
                          <span>{t("orders.delivery")}</span>
                        </span>
                      ) : null}
                    </div>

                    <div className="linked-merchant-card__actions">
                      {merchant.canCustomerOrder ? (
                        <>
                          <Button
                            asChild
                            className="linked-merchant-card__action min-h-11"
                            data-testid="open-merchant-shop"
                          >
                            <Link to={shopTo}>
                              <ShoppingBag className="size-4 shrink-0" aria-hidden />
                              {t("personal.shopLink")}
                            </Link>
                          </Button>
                          <Button
                            asChild
                            variant="outline"
                            className="linked-merchant-card__action min-h-11"
                            data-testid="open-merchant-statement"
                          >
                            <Link to={statementTo}>
                              <Receipt className="size-4 shrink-0" aria-hidden />
                              {t("personal.merchantStatement.openPurchases")}
                            </Link>
                          </Button>
                        </>
                      ) : (
                        <Button
                          asChild
                          className="linked-merchant-card__action linked-merchant-card__action--solo min-h-11"
                          data-testid="open-merchant-statement"
                        >
                          <Link to={statementTo}>
                            <Receipt className="size-4 shrink-0" aria-hidden />
                            {t("personal.merchantStatement.openPurchases")}
                          </Link>
                        </Button>
                      )}
                    </div>
                  </article>
                </li>
              );
            })}
          </ul>
        </section>
      )}
    </div>
  );
}
