import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { listLinkedMerchants } from "@/api/platform/linked-merchants-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";

export function LinkedMerchantsPage() {
  const { t } = useI18n();
  const query = useQuery({
    queryKey: ["personal", "linked-merchants"],
    queryFn: ({ signal }) => listLinkedMerchants(1, 50, signal),
  });

  if (query.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (query.isError) {
    return (
      <div className="flex min-w-0 flex-col gap-4">
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

  const items = query.data?.items ?? [];

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="linked-merchants-page">
      <PageHeader title={t("personal.merchantsTitle")} description={t("personal.merchantsLede")} />
      <div className="flex flex-wrap gap-2">
        <Button asChild variant="ghost" className="min-h-11 w-fit">
          <Link to="/personal/orders">{t("personal.myOrdersLink")}</Link>
        </Button>
        <Button asChild variant="ghost" className="min-h-11 w-fit" data-testid="open-customer-links">
          <Link to="/personal/customer-links">{t("personal.customerLinks.title")}</Link>
        </Button>
      </div>
      {items.length === 0 ? (
        <EmptyState
          title={t("personal.merchantsEmptyTitle")}
          detail={t("personal.merchantsEmptyDetail")}
        />
      ) : (
        <ul className="m-0 flex list-none flex-col gap-3 p-0">
          {items.map((merchant) => (
            <li key={merchant.linkedCustomerId}>
              <Card className="flex flex-col gap-2" data-testid="linked-merchant-card">
                <div className="flex flex-wrap items-center gap-2">
                  <strong>{merchant.organizationDisplayName}</strong>
                  {merchant.canCustomerOrder ? (
                    <StatusChip tone="success">{t("personal.orderingAvailable")}</StatusChip>
                  ) : (
                    <StatusChip tone="warning">{t("personal.orderingUnavailable")}</StatusChip>
                  )}
                </div>
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {merchant.customerDisplayName}
                </p>
                {merchant.canCustomerOrder ? (
                  <Button asChild className="min-h-11 w-fit" data-testid="open-merchant-shop">
                    <Link to={`/personal/linked-merchants/${merchant.organizationId}/shop`}>
                      {t("personal.shopLink")}
                    </Link>
                  </Button>
                ) : null}
              </Card>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
