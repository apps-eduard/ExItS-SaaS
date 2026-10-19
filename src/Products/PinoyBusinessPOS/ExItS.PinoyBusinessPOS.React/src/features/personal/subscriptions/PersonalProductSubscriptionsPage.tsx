import type { CSSProperties } from "react";
import { useQuery } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import {
  listPersonalProductAffiliations,
  type PersonalProductAffiliationDto,
} from "@/api/platform/product-affiliations-client";
import { POS_PRODUCT_CODE } from "@/api/platform/browser-session";
import { symbol } from "@/components/icons/MaterialSymbol";
import {
  ownsProductOrganization,
  resolveProductPortfolioCapability,
} from "@/features/personal/businesses/product-portfolio-capability";
import "@/features/personal/businesses/PersonalOwnedBusinesses.css";
import {
  INTENT_GLOBAL_HELP,
  INTENT_GLOBAL_INFO,
} from "@/features/ui-standards/intent-color-global-default";
import { useEnterBusiness } from "@/workspace/use-switch-to-business";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import { personalPageBackNav } from "@/navigation/page-back-nav";

const BusinessCenterIcon = symbol("business_center");
const StoreIcon = symbol("store");

const primaryActionClass =
  "personal-owned-businesses__open personal-owned-businesses__login ui-standards-qbtn--raised";
const primaryActionStyle = { backgroundColor: INTENT_GLOBAL_INFO, color: "#fff" } as CSSProperties;
const secondaryActionClass =
  "personal-owned-businesses__explore personal-owned-businesses__login ui-standards-qbtn--raised";
const secondaryActionStyle = { backgroundColor: INTENT_GLOBAL_HELP, color: "#fff" } as CSSProperties;

export const PERSONAL_PRODUCT_AFFILIATIONS_QUERY_KEY = ["personal", "product-affiliations"] as const;

function formatTrialEnd(value: string | null | undefined): string | null {
  if (!value) return null;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return null;
  return date.toLocaleDateString();
}

export function PersonalProductSubscriptionsPage() {
  const { t } = useI18n();
  const { enterBusiness } = useEnterBusiness();
  const query = useQuery({
    queryKey: PERSONAL_PRODUCT_AFFILIATIONS_QUERY_KEY,
    queryFn: ({ signal }) => listPersonalProductAffiliations(signal),
  });

  return (
    <div className="personal-page exits-page flex min-w-0 flex-col gap-3" data-testid="personal-subscriptions-page">
      <PageHeader
        title={t("personal.subscriptions.title")}
        description={t("personal.subscriptions.lede")}
        backTo={personalPageBackNav.more.to}
        backLabel={t(personalPageBackNav.more.labelKey)}
        backTestId="page-header-back-subscriptions"
      />
      {query.isLoading ? <LoadingSkeleton label={t("personal.subscriptions.title")} /> : null}
      {query.isError ? (
        <ErrorState
          title={t("personal.subscriptions.loadError")}
          detail={t("personal.subscriptions.loadError")}
        />
      ) : null}
      <div className="personal-owned-businesses">
        <div className="personal-owned-businesses__list">
          {query.data?.map((row) => (
            <ProductAffiliationCard
              key={row.productCode}
              row={row}
              onOpen={(organizationId) => void enterBusiness(organizationId)}
              onManage={(organizationId) => void enterBusiness(organizationId, "/org/subscription")}
            />
          ))}
        </div>
      </div>
    </div>
  );
}

function ProductAffiliationCard({
  row,
  onOpen,
  onManage,
}: {
  row: PersonalProductAffiliationDto;
  onOpen: (organizationId: string) => void;
  onManage: (organizationId: string) => void;
}) {
  const { t } = useI18n();
  const navigate = useNavigate();
  const hasOrganization = Boolean(row.organizationId);
  const ownsOrganization = ownsProductOrganization(row);
  const trialEnd = formatTrialEnd(row.trialEndUtc);
  const capability = resolveProductPortfolioCapability(row);
  const ready = capability.availability === "ready";
  const RailIcon = row.productCode === POS_PRODUCT_CODE ? StoreIcon : BusinessCenterIcon;
  const role = hasOrganization ? row.roleDisplay?.trim() || null : null;
  const hasActions =
    (capability.canOpenProduct && Boolean(row.organizationId)) ||
    (capability.canManageSubscription && Boolean(row.organizationId)) ||
    (capability.canChoosePlan && Boolean(capability.choosePlanRoute)) ||
    (capability.canStartBusiness && Boolean(capability.startRoute));

  return (
    <section
      className={`personal-home-step-card ${
        ready ? "personal-home-step-card--primary" : "personal-home-step-card--muted"
      } exits-animate-panel`}
      data-testid={`product-affiliation-${row.productCode}`}
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
          <p className="personal-owned-businesses__meta">
            {ownsOrganization
              ? t("personal.subscriptions.alreadyHave").replace("{product}", row.productDisplayName)
              : hasOrganization
                ? t("personal.subscriptions.managedByOrganization")
                : t("personal.subscriptions.noOrganization")}
          </p>
          {row.planDisplayName || row.subscriptionStatus ? (
            <p className="personal-owned-businesses__meta">
              {row.planDisplayName ? (
                <span className="personal-owned-businesses__plan">{row.planDisplayName}</span>
              ) : null}
              {row.planDisplayName && row.subscriptionStatus ? (
                <span className="personal-owned-businesses__sep"> · </span>
              ) : null}
              {row.subscriptionStatus ? (
                <span className="personal-owned-businesses__status">{row.subscriptionStatus}</span>
              ) : null}
            </p>
          ) : null}
          {hasOrganization && !row.subscriptionStatus ? (
            <p className="personal-owned-businesses__meta">{t("personal.subscriptions.noActiveSubscription")}</p>
          ) : null}
          {row.subscriptionStatus === "Trialing" && trialEnd ? (
            <p className="personal-owned-businesses__meta">
              {t("personal.subscriptions.trialEnds").replace("{date}", trialEnd)}
            </p>
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
                onClick={() => onOpen(row.organizationId!)}
              >
                {t("personal.subscriptions.openProduct")}
              </Button>
            ) : null}
            {capability.canManageSubscription && row.organizationId ? (
              <Button
                type="button"
                intent="help"
                appearance="elevated"
                emphasis="strong"
                shape="pill"
                className={secondaryActionClass}
                style={secondaryActionStyle}
                onClick={() => onManage(row.organizationId!)}
              >
                {t("personal.subscriptions.manage")}
              </Button>
            ) : null}
            {capability.canManageSubscription && row.subscriptionStatus === "Trialing" && capability.choosePlanRoute ? (
              <Button
                type="button"
                intent="help"
                appearance="elevated"
                emphasis="strong"
                shape="pill"
                className={secondaryActionClass}
                style={secondaryActionStyle}
                onClick={() => navigate(capability.choosePlanRoute!)}
              >
                {t("personal.subscriptions.upgrade")}
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
                onClick={() => navigate(capability.choosePlanRoute!)}
              >
                {row.subscriptionStatus ? t("personal.subscriptions.reactivate") : t("personal.subscriptions.choosePlan")}
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
                onClick={() => navigate(capability.startRoute!)}
              >
                {t("personal.subscriptions.getStarted")}
              </Button>
            ) : null}
          </div>
        ) : null}
      </div>
    </section>
  );
}
