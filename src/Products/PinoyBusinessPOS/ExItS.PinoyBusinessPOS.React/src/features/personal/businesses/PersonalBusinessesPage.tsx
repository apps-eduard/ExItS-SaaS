import { useQuery } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { setOrganizationContext } from "@/api/platform/platform-auth-client";
import { listPersonalProductAffiliations } from "@/api/platform/product-affiliations-client";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import {
  businessMetaLine,
  resolveProductPortfolioCapability,
} from "@/features/personal/businesses/product-portfolio-capability";
import { PERSONAL_PRODUCT_AFFILIATIONS_QUERY_KEY } from "@/features/personal/subscriptions/PersonalProductSubscriptionsPage";
import { useI18n } from "@/i18n/I18nProvider";
import { personalPageBackNav } from "@/navigation/page-back-nav";
import { useEnterBusiness } from "@/workspace/use-switch-to-business";

export function PersonalBusinessesPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { enterBusiness, entering } = useEnterBusiness();
  const query = useQuery({
    queryKey: PERSONAL_PRODUCT_AFFILIATIONS_QUERY_KEY,
    queryFn: ({ signal }) => listPersonalProductAffiliations(signal),
  });

  async function manageSubscription(organizationId: string, route: string) {
    await setOrganizationContext(organizationId);
    navigate(route);
  }

  return (
    <div className="personal-page exits-page flex min-w-0 flex-col gap-3" data-testid="personal-businesses-page">
      <PageHeader
        title={t("personal.businesses.title")}
        description={t("personal.businesses.lede")}
        backTo={personalPageBackNav.more.to}
        backLabel={t(personalPageBackNav.more.labelKey)}
        backTestId="page-header-back-businesses"
      />
      {query.isLoading ? <LoadingSkeleton label={t("personal.businesses.title")} /> : null}
      {query.isError ? (
        <ErrorState
          title={t("personal.businesses.unavailable")}
          detail={t("personal.businesses.unavailable")}
        />
      ) : null}
      <div className="grid min-w-0 gap-3">
        {query.data?.map((row) => {
          const capability = resolveProductPortfolioCapability(row);
          const meta = businessMetaLine(row);
          const hasOrganization = Boolean(row.organizationId);
          return (
            <section
              key={row.productCode}
              className="catalog-form-section exits-animate-panel personal-section flex min-w-0 flex-col gap-2"
              data-testid={`product-portfolio-${row.productCode}`}
            >
              <h2 className="catalog-form-section__title">{row.productDisplayName}</h2>
              {hasOrganization ? (
                <>
                  {row.organizationDisplayName ? <p className="m-0">{row.organizationDisplayName}</p> : null}
                  {meta ? <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{meta}</p> : null}
                  <p className="m-0 text-[length:var(--exits-text-sm)]">{t("personal.businesses.alreadyUsing")}</p>
                  {!row.subscriptionStatus ? (
                    <p className="m-0">{t("personal.subscriptions.noActiveSubscription")}</p>
                  ) : null}
                  {!row.canManageBilling ? (
                    <p className="m-0">{t("personal.subscriptions.managedByOrganization")}</p>
                  ) : null}
                </>
              ) : (
                <p className="m-0">
                  {capability.availability === "coming-soon"
                    ? t("personal.businesses.comingSoon")
                    : t("personal.businesses.noBusinessYet")}
                </p>
              )}
              {hasOrganization && capability.availability === "coming-soon" ? (
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("personal.businesses.comingSoon")}
                </p>
              ) : null}
              <div className="flex min-w-0 flex-wrap gap-2">
                {capability.canOpenProduct && row.organizationId ? (
                  <Button
                    type="button"
                    disabled={entering}
                    data-testid={`portfolio-open-${row.productCode}`}
                    onClick={() => void enterBusiness(row.organizationId!)}
                  >
                    {t("personal.businesses.open")}
                  </Button>
                ) : null}
                {capability.canManageSubscription && row.organizationId && capability.manageRoute ? (
                  <Button
                    type="button"
                    variant="ghost"
                    data-testid={`portfolio-manage-${row.productCode}`}
                    onClick={() => void manageSubscription(row.organizationId!, capability.manageRoute!)}
                  >
                    {t("personal.subscriptions.manage")}
                  </Button>
                ) : null}
                {capability.canChoosePlan && capability.choosePlanRoute ? (
                  <Button
                    type="button"
                    data-testid={`portfolio-choose-plan-${row.productCode}`}
                    onClick={() => navigate(capability.choosePlanRoute!)}
                  >
                    {t("personal.subscriptions.choosePlan")}
                  </Button>
                ) : null}
                {capability.canStartBusiness && capability.startRoute ? (
                  <Button
                    type="button"
                    data-testid={`portfolio-start-${row.productCode}`}
                    onClick={() => navigate(capability.startRoute!)}
                  >
                    {t("personal.businesses.start")}
                  </Button>
                ) : null}
              </div>
            </section>
          );
        })}
      </div>
    </div>
  );
}
