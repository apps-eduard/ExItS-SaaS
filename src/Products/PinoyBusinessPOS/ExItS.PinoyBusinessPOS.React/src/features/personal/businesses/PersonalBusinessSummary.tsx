import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { Building2 } from "lucide-react";
import { listPersonalProductAffiliations } from "@/api/platform/product-affiliations-client";
import { Button } from "@/components/ui/button";
import {
  affiliatedBusinesses,
  businessMetaLine,
  resolveProductPortfolioCapability,
} from "@/features/personal/businesses/product-portfolio-capability";
import { PERSONAL_PRODUCT_AFFILIATIONS_QUERY_KEY } from "@/features/personal/subscriptions/PersonalProductSubscriptionsPage";
import { useI18n } from "@/i18n/I18nProvider";
import { PERSONAL_BUSINESSES_PATH, useEnterBusiness } from "@/workspace/use-switch-to-business";

export function PersonalBusinessSummary() {
  const { t } = useI18n();
  const { enterBusiness, entering } = useEnterBusiness();
  const query = useQuery({
    queryKey: PERSONAL_PRODUCT_AFFILIATIONS_QUERY_KEY,
    queryFn: ({ signal }) => listPersonalProductAffiliations(signal),
    meta: { suppressGlobalError: true, operation: "list personal businesses" },
  });

  if (query.isPending) {
    return null;
  }

  if (query.isError) {
    return (
      <section
        className="catalog-form-section exits-animate-panel personal-section gap-2"
        data-testid="personal-business-summary"
      >
        <h2 className="catalog-form-section__title">{t("personal.home.yourBusinesses")}</h2>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("personal.businesses.unavailable")}
        </p>
      </section>
    );
  }

  const businesses = affiliatedBusinesses(query.data ?? []);
  if (businesses.length === 0) {
    return (
      <section
        className="catalog-form-section exits-animate-panel personal-section flex min-w-0 flex-col gap-2"
        data-testid="personal-business-summary"
      >
        <h2 className="catalog-form-section__title">{t("personal.home.startBusinessCard")}</h2>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("personal.home.startBusinessLede")}
        </p>
        <Button asChild className="w-fit">
          <Link to={PERSONAL_BUSINESSES_PATH} data-testid="personal-home-start-business">
            {t("personal.businesses.start")}
          </Link>
        </Button>
      </section>
    );
  }

  return (
    <section
      className="catalog-form-section exits-animate-panel personal-section flex min-w-0 flex-col gap-3"
      data-testid="personal-business-summary"
    >
      <h2 className="catalog-form-section__title inline-flex items-center gap-1.5">
        <Building2 className="size-[1.1rem] shrink-0" aria-hidden />
        {t("personal.home.yourBusinesses")}
      </h2>
      <ul className="m-0 grid list-none gap-2 p-0">
        {businesses.map((row, index) => {
          const capability = resolveProductPortfolioCapability(row);
          const meta = businessMetaLine(row);
          return (
            <li
              key={row.productCode}
              className="flex min-w-0 flex-col gap-1 border-b border-border pb-2 last:border-b-0 last:pb-0"
              data-testid={`personal-business-${row.productCode}`}
            >
              <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">{row.productDisplayName}</p>
              {row.organizationDisplayName ? <p className="m-0">{row.organizationDisplayName}</p> : null}
              {meta ? <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{meta}</p> : null}
              {!row.canManageBilling ? (
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("personal.subscriptions.managedByOrganization")}
                </p>
              ) : null}
              <div className="flex flex-wrap items-center gap-2">
                {capability.canOpenProduct && row.organizationId ? (
                  <Button
                    type="button"
                    className="w-fit"
                    disabled={entering}
                    data-testid={`personal-business-open-${row.productCode}`}
                    onClick={() => void enterBusiness(row.organizationId!)}
                  >
                    {t("personal.subscriptions.openProduct")}
                  </Button>
                ) : null}
                {index === 0 ? (
                  <Button asChild variant="ghost" className="w-fit">
                    <Link to={PERSONAL_BUSINESSES_PATH} data-testid="personal-home-start-another">
                      {t("personal.businesses.startAnother")}
                    </Link>
                  </Button>
                ) : null}
              </div>
            </li>
          );
        })}
      </ul>
    </section>
  );
}
