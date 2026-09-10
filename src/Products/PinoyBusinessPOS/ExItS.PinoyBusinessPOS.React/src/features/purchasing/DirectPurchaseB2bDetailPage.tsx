import { useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronDown } from "lucide-react";
import { getDirectPurchaseB2bDetail } from "@/api/pos/pos-direct-purchases-client";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function DirectPurchaseB2bDetailPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { saleId = "" } = useParams<{ saleId: string }>();
  const { boundWorkspace } = useWorkspace();
  const [disclaimerOpen, setDisclaimerOpen] = useState(true);

  const workspace = useMemo(
    () =>
      boundWorkspace?.organizationId
        ? {
            organizationId: boundWorkspace.organizationId,
            branchId: boundWorkspace.branchId ?? undefined,
          }
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: ["direct-purchase-b2b", workspace?.organizationId, saleId],
    enabled: Boolean(workspace) && Boolean(saleId) && online,
    queryFn: ({ signal }) => getDirectPurchaseB2bDetail(workspace!, saleId, signal),
  });

  const pageShell =
    "purchasing-direct-b2b-page exits-page flex min-w-0 flex-col gap-3";

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (!online) {
    return (
      <div className={pageShell} data-testid="direct-purchase-b2b-detail-page">
        <PageHeader
          title={t("purchasing.b2bDetailTitle")}
          backTo="/purchasing/direct-purchases"
          backLabel={t("purchasing.directPurchases")}
          backTestId="page-header-back-direct-purchases"
        />
        <ErrorState title={t("offline.internetRequiredTitle")} detail={t("purchasing.offline")} />
      </div>
    );
  }

  if (query.isLoading) {
    return <LoadingState label={t("purchasing.loading")} />;
  }

  if (query.isError || !query.data) {
    return (
      <div className={pageShell} data-testid="direct-purchase-b2b-detail-page">
        <PageHeader
          title={t("purchasing.b2bDetailTitle")}
          backTo="/purchasing/direct-purchases"
          backLabel={t("purchasing.directPurchases")}
          backTestId="page-header-back-direct-purchases"
        />
        <ErrorState
          title={t("purchasing.directNotFound")}
          detail={t("purchasing.b2bDetailMissing")}
        />
      </div>
    );
  }

  const detail = query.data;

  return (
    <div className={pageShell} data-testid="direct-purchase-b2b-detail-page">
      <PageHeader
        title={t("purchasing.b2bDetailTitle")}
        description={detail.saleNumber}
        backTo="/purchasing/direct-purchases"
        backLabel={t("purchasing.directPurchases")}
        backTestId="page-header-back-direct-purchases"
      />

      <section className="pc-receipt-disclaimer exits-animate-panel" data-testid="b2b-disclaimer">
        <button
          type="button"
          className="pc-receipt-disclaimer__toggle"
          aria-expanded={disclaimerOpen}
          data-testid="b2b-disclaimer-toggle"
          onClick={() => setDisclaimerOpen((open) => !open)}
        >
          <span>{t("summary.disclaimerTitle")}</span>
          <ChevronDown
            className={cn(
              "pc-receipt-disclaimer__chevron size-4 shrink-0",
              disclaimerOpen && "pc-receipt-disclaimer__chevron--open",
            )}
            aria-hidden
          />
        </button>
        {disclaimerOpen ? (
          <p className="pc-receipt-disclaimer__body" data-testid="b2b-disclaimer-body">
            {t("summary.disclaimerBody")}
          </p>
        ) : null}
      </section>

      <section className="pc-receipt-card exits-animate-panel" data-testid="b2b-summary">
        <dl className="pc-receipt-card__meta-list m-0">
          <div className="pc-receipt-card__meta-row">
            <dt>{t("purchasing.b2bPurchasedFrom")}</dt>
            <dd className="font-semibold" data-testid="b2b-seller-name">
              {detail.sellerDisplayName}
            </dd>
          </div>
          {detail.sellerPublicOrganizationId ? (
            <div className="pc-receipt-card__meta-row">
              <dt>{t("purchasing.b2bSellerOrgId")}</dt>
              <dd data-testid="b2b-seller-org">{detail.sellerPublicOrganizationId}</dd>
            </div>
          ) : null}
          {detail.sellerStoreDisplayName ? (
            <div className="pc-receipt-card__meta-row">
              <dt>{t("purchasing.b2bSellerStore")}</dt>
              <dd data-testid="b2b-seller-store">{detail.sellerStoreDisplayName}</dd>
            </div>
          ) : null}
          <div className="pc-receipt-card__meta-row">
            <dt>{t("summary.saleNumber")}</dt>
            <dd className="font-semibold" data-testid="b2b-sale-number">
              {detail.saleNumber}
            </dd>
          </div>
          <div className="pc-receipt-card__meta-row">
            <dt>{t("summary.dateTime")}</dt>
            <dd data-testid="b2b-datetime">{new Date(detail.occurredAtUtc).toLocaleString()}</dd>
          </div>
          <div className="pc-receipt-card__meta-row">
            <dt>{t("summary.paymentMethod")}</dt>
            <dd data-testid="b2b-payment">{detail.paymentMethod}</dd>
          </div>
          <div className="pc-receipt-card__meta-row">
            <dt>{t("summary.status")}</dt>
            <dd data-testid="b2b-status">{detail.status}</dd>
          </div>
          <div className="pc-receipt-card__meta-row">
            <dt>{t("purchasing.directColType")}</dt>
            <dd data-testid="b2b-source-badge">{t("purchasing.directBadgeB2b")}</dd>
          </div>
        </dl>

        <ul className="pc-receipt-card__line-list m-0 list-none p-0" data-testid="b2b-lines">
          {detail.lines.map((line) => (
            <li
              key={line.lineNumber}
              className="pc-receipt-card__line"
              data-testid={`b2b-line-${line.lineNumber}`}
            >
              <span className="min-w-0 truncate text-[length:var(--exits-text-sm)]">
                {line.productNameSnapshot} × {line.quantity} {line.unitOfMeasure}
                {line.lineDiscountAmount > 0
                  ? ` (−${line.lineDiscountAmount.toFixed(2)})`
                  : ""}
              </span>
              <MoneyDisplay amount={line.lineTotal} className="pc-receipt-line__total" />
            </li>
          ))}
        </ul>

        <div className="pc-receipt-card__totals">
          <p className="pc-receipt-card__total-row">
            <span className="text-muted">{t("summary.subtotal")}</span>
            <MoneyDisplay amount={detail.subtotal} />
          </p>
          {detail.discountTotal > 0 ? (
            <p className="pc-receipt-card__total-row">
              <span className="text-muted">{t("purchasing.b2bDiscount")}</span>
              <MoneyDisplay amount={detail.discountTotal} />
            </p>
          ) : null}
          <p className="pc-receipt-card__total-row pc-receipt-card__total-row--emphasis">
            <span>{t("summary.total")}</span>
            <MoneyDisplay amount={detail.totalAmount} testId="b2b-total" />
          </p>
        </div>
      </section>
    </div>
  );
}
