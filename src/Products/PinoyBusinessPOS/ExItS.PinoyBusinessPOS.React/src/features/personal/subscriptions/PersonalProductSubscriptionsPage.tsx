import { useQuery } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import {
  listPersonalProductAffiliations,
  type PersonalProductAffiliationDto,
} from "@/api/platform/product-affiliations-client";
import {
  ownsProductOrganization,
  resolveProductPortfolioCapability,
} from "@/features/personal/businesses/product-portfolio-capability";
import { useEnterBusiness } from "@/workspace/use-switch-to-business";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import { personalPageBackNav } from "@/navigation/page-back-nav";

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
      {query.data?.map((row) => (
        <ProductAffiliationCard
          key={row.productCode}
          row={row}
          onOpen={(organizationId) => void enterBusiness(organizationId)}
          onManage={(organizationId) => void enterBusiness(organizationId, "/org/subscription")}
        />
      ))}
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

  return (
    <section
      className="catalog-form-section exits-animate-panel personal-section flex flex-col gap-2"
      data-testid={`product-affiliation-${row.productCode}`}
    >
      <h2 className="catalog-form-section__title">{row.productDisplayName}</h2>
      {ownsOrganization ? (
        <p>{t("personal.subscriptions.alreadyHave").replace("{product}", row.productDisplayName)}</p>
      ) : !hasOrganization ? (
        <p>{t("personal.subscriptions.noOrganization")}</p>
      ) : null}
      {row.organizationDisplayName ? <p>{row.organizationDisplayName}</p> : null}
      {row.roleDisplay ? (
        <p>
          {t("personal.subscriptions.role")}: {row.roleDisplay}
        </p>
      ) : null}
      {row.planDisplayName ? <p>{row.planDisplayName}</p> : null}
      {row.subscriptionStatus ? <p>{row.subscriptionStatus}</p> : null}
      {hasOrganization && !row.subscriptionStatus ? (
        <p>{t("personal.subscriptions.noActiveSubscription")}</p>
      ) : null}
      {row.subscriptionStatus === "Trialing" && trialEnd ? (
        <p>{t("personal.subscriptions.trialEnds").replace("{date}", trialEnd)}</p>
      ) : null}
      {!row.canManageBilling && hasOrganization ? (
        <p>{t("personal.subscriptions.managedByOrganization")}</p>
      ) : null}
      <div className="flex flex-wrap gap-2">
        {capability.canOpenProduct && row.organizationId ? (
          <Button type="button" onClick={() => onOpen(row.organizationId!)}>
            {t("personal.subscriptions.openProduct")}
          </Button>
        ) : null}
        {capability.canManageSubscription && row.organizationId ? (
          <Button type="button" variant="ghost" onClick={() => onManage(row.organizationId!)}>
            {t("personal.subscriptions.manage")}
          </Button>
        ) : null}
        {capability.canManageSubscription && row.subscriptionStatus === "Trialing" && capability.choosePlanRoute ? (
          <Button type="button" variant="ghost" onClick={() => navigate(capability.choosePlanRoute!)}>
            {t("personal.subscriptions.upgrade")}
          </Button>
        ) : null}
        {capability.canChoosePlan && capability.choosePlanRoute ? (
          <Button type="button" onClick={() => navigate(capability.choosePlanRoute!)}>
            {row.subscriptionStatus ? t("personal.subscriptions.reactivate") : t("personal.subscriptions.choosePlan")}
          </Button>
        ) : null}
        {capability.canStartBusiness && capability.startRoute ? (
          <Button type="button" onClick={() => navigate(capability.startRoute!)}>
            {t("personal.subscriptions.getStarted")}
          </Button>
        ) : null}
      </div>
    </section>
  );
}
