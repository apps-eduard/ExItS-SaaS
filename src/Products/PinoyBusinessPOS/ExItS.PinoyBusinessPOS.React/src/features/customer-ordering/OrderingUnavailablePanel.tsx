import { Link } from "react-router-dom";
import { ArrowLeft, Receipt, Store } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  MerchantOrderingBadge,
  storeDisplayInitial,
} from "@/features/customer-ordering/personal-commerce-ui";
import { useI18n } from "@/i18n/I18nProvider";
import { personalPageBackNav } from "@/navigation/page-back-nav";

type OrderingUnavailablePanelProps = {
  storeName: string;
  relationshipLabel?: string | null;
  statementTo?: string | null;
};

export function OrderingUnavailablePanel({
  storeName,
  relationshipLabel,
  statementTo,
}: OrderingUnavailablePanelProps) {
  const { t } = useI18n();

  return (
    <section
      className="pc-ordering-unavailable exits-animate-panel"
      data-testid="ordering-unavailable-panel"
      aria-labelledby="ordering-unavailable-title"
    >
      <div className="pc-ordering-unavailable__hero">
        <span className="pc-ordering-unavailable__avatar" aria-hidden>
          {storeDisplayInitial(storeName)}
        </span>
        <div className="pc-ordering-unavailable__body min-w-0 flex-1">
          <div className="pc-ordering-unavailable__header-row">
            <div className="pc-ordering-unavailable__identity">
              <h2 id="ordering-unavailable-title" className="pc-ordering-unavailable__title">
                {storeName}
              </h2>
              {relationshipLabel ? (
                <p className="pc-ordering-unavailable__relationship">{relationshipLabel}</p>
              ) : null}
            </div>
            <div className="pc-ordering-unavailable__badge">
              <MerchantOrderingBadge available={false} />
            </div>
          </div>
        </div>
      </div>

      <div className="pc-ordering-unavailable__message">
        <span className="pc-ordering-unavailable__icon-wrap" aria-hidden>
          <Store className="pc-ordering-unavailable__icon" />
        </span>
        <div className="min-w-0">
          <p className="pc-ordering-unavailable__headline">{t("personal.orderingUnavailable")}</p>
          <p className="pc-ordering-unavailable__detail">{t("personal.orderingUnavailableDetail")}</p>
          <p className="pc-ordering-unavailable__hint">{t("personal.orderingUnavailableHint")}</p>
        </div>
      </div>

      <div
        className={
          statementTo
            ? "pc-ordering-unavailable__actions"
            : "pc-ordering-unavailable__actions pc-ordering-unavailable__actions--solo"
        }
      >
        <Button asChild className="pc-ordering-unavailable__action">
          <Link to={personalPageBackNav.merchants.to} data-testid="ordering-unavailable-back-stores">
            <ArrowLeft className="size-4 shrink-0" aria-hidden />
            {t("personal.backToMerchants")}
          </Link>
        </Button>
        {statementTo ? (
          <Button asChild variant="outline" className="pc-ordering-unavailable__action">
            <Link to={statementTo} data-testid="ordering-unavailable-open-statement">
              <Receipt className="size-4 shrink-0" aria-hidden />
              {t("personal.merchantStatement.openPurchases")}
            </Link>
          </Button>
        ) : null}
      </div>
    </section>
  );
}
