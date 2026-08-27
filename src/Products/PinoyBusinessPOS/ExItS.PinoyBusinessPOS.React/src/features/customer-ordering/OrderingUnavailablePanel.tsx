import { Link } from "react-router-dom";
import { ArrowLeft, Receipt, Store } from "lucide-react";
import { Button } from "@/components/ui/button";
import { personalStoreDisplayName } from "@/features/customer-ordering/format-personal-store-label";
import { PersonalStoreIdentityCard } from "@/features/customer-ordering/PersonalStoreIdentity";
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
  const displayName = personalStoreDisplayName(storeName) || storeName;

  return (
    <>
      <PersonalStoreIdentityCard
        storeName={displayName}
        relationshipLabel={relationshipLabel}
        canCustomerOrder={false}
        headingLevel="h2"
        headingId="ordering-unavailable-title"
      />
      <section
        className="pc-ordering-unavailable exits-animate-panel"
        data-testid="ordering-unavailable-panel"
        aria-labelledby="ordering-unavailable-title"
      >
        <div className="pc-ordering-unavailable__message">
          <span className="pc-ordering-unavailable__icon-wrap" aria-hidden>
            <Store className="pc-ordering-unavailable__icon" />
          </span>
          <div className="min-w-0">
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
          <Button
            asChild
            className="pc-ordering-unavailable__action !h-full !min-h-[var(--exits-touch-target-min)]"
          >
            <Link to={personalPageBackNav.merchants.to} data-testid="ordering-unavailable-back-stores">
              <ArrowLeft className="size-4 shrink-0" aria-hidden />
              {t("personal.backToMerchants")}
            </Link>
          </Button>
          {statementTo ? (
            <Button
              asChild
              variant="outline"
              className="pc-ordering-unavailable__action !h-full !min-h-[var(--exits-touch-target-min)]"
            >
              <Link to={statementTo} data-testid="ordering-unavailable-open-statement">
                <Receipt className="size-4 shrink-0" aria-hidden />
                {t("personal.merchantStatement.openPurchases")}
              </Link>
            </Button>
          ) : null}
        </div>
      </section>
    </>
  );
}
