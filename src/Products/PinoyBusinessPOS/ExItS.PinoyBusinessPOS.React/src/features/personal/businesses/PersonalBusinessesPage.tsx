import type { CSSProperties } from "react";
import { useQuery } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { listPersonalProductAffiliations } from "@/api/platform/product-affiliations-client";
import { POS_PRODUCT_CODE } from "@/api/platform/browser-session";
import { symbol } from "@/components/icons/MaterialSymbol";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import {
  ownsProductOrganization,
  resolveProductPortfolioCapability,
} from "@/features/personal/businesses/product-portfolio-capability";
import "./PersonalOwnedBusinesses.css";
import { PERSONAL_PRODUCT_AFFILIATIONS_QUERY_KEY } from "@/features/personal/subscriptions/PersonalProductSubscriptionsPage";
import {
  INTENT_GLOBAL_HELP,
  INTENT_GLOBAL_INFO,
} from "@/features/ui-standards/intent-color-global-default";
import { useI18n } from "@/i18n/I18nProvider";
import { personalPageBackNav } from "@/navigation/page-back-nav";
import { useEnterBusiness } from "@/workspace/use-switch-to-business";

const BusinessCenterIcon = symbol("business_center");
const StoreIcon = symbol("store");

const primaryActionClass =
  "personal-owned-businesses__open personal-owned-businesses__login ui-standards-qbtn--raised";
const primaryActionStyle = { backgroundColor: INTENT_GLOBAL_INFO, color: "#fff" } as CSSProperties;
const secondaryActionClass =
  "personal-owned-businesses__explore personal-owned-businesses__login ui-standards-qbtn--raised";
const secondaryActionStyle = { backgroundColor: INTENT_GLOBAL_HELP, color: "#fff" } as CSSProperties;

export function PersonalBusinessesPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { enterBusiness, entering } = useEnterBusiness();
  const query = useQuery({
    queryKey: PERSONAL_PRODUCT_AFFILIATIONS_QUERY_KEY,
    queryFn: ({ signal }) => listPersonalProductAffiliations(signal),
  });

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
      <div className="personal-owned-businesses">
        <div className="personal-owned-businesses__list">
          {query.data?.map((row) => {
            const capability = resolveProductPortfolioCapability(row);
            const hasOrganization = Boolean(row.organizationId);
            const meta = [row.planDisplayName, row.subscriptionStatus].filter(Boolean).join(" · ");
            const role = hasOrganization
              ? ownsProductOrganization(row)
                ? t("personal.home.ownerRole")
                : row.roleDisplay?.trim() || null
              : null;
            const ready = capability.availability === "ready";
            const RailIcon = row.productCode === POS_PRODUCT_CODE ? StoreIcon : BusinessCenterIcon;
            const hasActions =
              (capability.canOpenProduct && Boolean(row.organizationId)) ||
              (capability.canManageSubscription && Boolean(row.organizationId) && Boolean(capability.manageRoute)) ||
              (capability.canChoosePlan && Boolean(capability.choosePlanRoute)) ||
              (capability.canStartBusiness && Boolean(capability.startRoute));

            return (
              <section
                key={row.productCode}
                className={`personal-home-step-card ${
                  ready ? "personal-home-step-card--primary" : "personal-home-step-card--muted"
                } exits-animate-panel`}
                data-testid={`product-portfolio-${row.productCode}`}
              >
                <div className="personal-home-step-card__rail" aria-hidden="true">
                  <span className="personal-home-step-card__icon-wrap">
                    <RailIcon className="personal-home-step-card__icon" />
                  </span>
                  {role ? <div className="personal-home-step-card__label">{role}</div> : null}
                </div>
                <div className="personal-home-step-card__panel">
                  <div className="personal-owned-businesses__copy">
                    <h2 className="personal-home-step-card__title personal-owned-businesses__name">
                      {row.organizationDisplayName || row.productDisplayName}
                    </h2>
                    {row.organizationDisplayName ? (
                      <p className="personal-owned-businesses__product">{row.productDisplayName}</p>
                    ) : null}
                    {hasOrganization ? (
                      <>
                        {meta ? <p className="personal-owned-businesses__meta">{meta}</p> : null}
                        <p className="personal-owned-businesses__meta">{t("personal.businesses.alreadyUsing")}</p>
                        {!row.subscriptionStatus ? (
                          <p className="personal-owned-businesses__meta">
                            {t("personal.subscriptions.noActiveSubscription")}
                          </p>
                        ) : null}
                        {!row.canManageBilling ? (
                          <p className="personal-owned-businesses__meta">
                            {t("personal.subscriptions.managedByOrganization")}
                          </p>
                        ) : null}
                      </>
                    ) : (
                      <p className="personal-owned-businesses__meta">
                        {ready ? t("personal.businesses.noBusinessYet") : t("personal.businesses.comingSoon")}
                      </p>
                    )}
                    {hasOrganization && !ready ? (
                      <p className="personal-owned-businesses__meta">{t("personal.businesses.comingSoon")}</p>
                    ) : null}
                  </div>
                  {hasActions ? (
                    <div className="personal-owned-businesses__actions personal-owned-businesses__actions--owned">
                      {capability.canOpenProduct && row.organizationId ? (
                        <Button
                          type="button"
                          intent="info"
                          appearance="elevated"
                          emphasis="strong"
                          shape="pill"
                          className={primaryActionClass}
                          style={primaryActionStyle}
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
                          intent="help"
                          appearance="elevated"
                          emphasis="strong"
                          shape="pill"
                          className={secondaryActionClass}
                          style={secondaryActionStyle}
                          data-testid={`portfolio-manage-${row.productCode}`}
                          onClick={() => void enterBusiness(row.organizationId!, capability.manageRoute!)}
                        >
                          {t("personal.subscriptions.manage")}
                        </Button>
                      ) : null}
                      {capability.canChoosePlan && capability.choosePlanRoute ? (
                        <Button
                          type="button"
                          intent="info"
                          appearance="elevated"
                          emphasis="strong"
                          shape="pill"
                          className={primaryActionClass}
                          style={primaryActionStyle}
                          data-testid={`portfolio-choose-plan-${row.productCode}`}
                          onClick={() => navigate(capability.choosePlanRoute!)}
                        >
                          {t("personal.subscriptions.choosePlan")}
                        </Button>
                      ) : null}
                      {capability.canStartBusiness && capability.startRoute ? (
                        <Button
                          type="button"
                          intent="info"
                          appearance="elevated"
                          emphasis="strong"
                          shape="pill"
                          className={primaryActionClass}
                          style={primaryActionStyle}
                          data-testid={`portfolio-start-${row.productCode}`}
                          onClick={() => navigate(capability.startRoute!)}
                        >
                          {t("personal.businesses.start")}
                        </Button>
                      ) : null}
                    </div>
                  ) : null}
                </div>
              </section>
            );
          })}
        </div>
      </div>
    </div>
  );
}
