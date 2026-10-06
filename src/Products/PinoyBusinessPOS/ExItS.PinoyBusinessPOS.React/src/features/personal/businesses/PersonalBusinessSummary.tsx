import { useState, type ReactNode } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useLocation } from "react-router-dom";
import { BriefcaseBusiness, Building2, type LucideIcon } from "lucide-react";
import { listPersonalProductAffiliations } from "@/api/platform/product-affiliations-client";
import {
  listPersonalWorkplaces,
  type PersonalWorkplaceWire,
} from "@/api/platform/personal-workplaces-client";
import { Button } from "@/components/ui/button";
import {
  affiliatedBusinesses,
  businessMetaLine,
  ownsProductOrganization,
  resolveProductPortfolioCapability,
} from "@/features/personal/businesses/product-portfolio-capability";
import { PERSONAL_PRODUCT_AFFILIATIONS_QUERY_KEY } from "@/features/personal/subscriptions/PersonalProductSubscriptionsPage";
import { PERSONAL_WORKPLACES_QUERY_KEY } from "@/features/personal/workplaces/PersonalWorkplacesPage";
import { WorkplaceSignInDialog } from "@/features/personal/workplaces/WorkplaceSignInDialog";
import { useI18n } from "@/i18n/I18nProvider";
import { PERSONAL_BUSINESSES_PATH, useEnterBusiness } from "@/workspace/use-switch-to-business";

function isActiveWorkplace(workplace: PersonalWorkplaceWire): boolean {
  return workplace.membershipStatus.trim().localeCompare("Active", undefined, { sensitivity: "accent" }) === 0;
}

/** Staff workplaces show the assigned POS role, not the organization-owner label. */
function staffPosRole(workplace: PersonalWorkplaceWire): string | null {
  const role = workplace.productRoleDisplay?.trim() ?? "";
  return role || null;
}

function CardHeading({ icon: Icon, children }: { icon: LucideIcon; children: ReactNode }) {
  return (
    <h2 className="catalog-form-section__title inline-flex items-center gap-1.5">
      <Icon className="size-[1.1rem] shrink-0" aria-hidden />
      {children}
    </h2>
  );
}

function BusinessHomeCard({
  testId,
  heading,
  title,
  organizationName,
  meta,
  managedByOrganization,
  actions,
}: {
  testId: string;
  heading: ReactNode;
  title: string;
  organizationName: string | null;
  meta: string | null;
  managedByOrganization: string | null;
  actions: ReactNode;
}) {
  return (
    <li className="catalog-form-section flex h-full min-w-0 flex-col gap-1" data-testid={testId}>
      {heading}
      <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">{title}</p>
      {organizationName ? <p className="m-0">{organizationName}</p> : null}
      {meta ? <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{meta}</p> : null}
      {managedByOrganization ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{managedByOrganization}</p>
      ) : null}
      <div className="mt-auto flex flex-wrap items-center gap-2">{actions}</div>
    </li>
  );
}

export function PersonalBusinessSummary() {
  const { t } = useI18n();
  const location = useLocation();
  const signInError =
    (location.state as { workplaceSignInError?: string } | null)?.workplaceSignInError ?? null;
  const { enterBusiness, entering } = useEnterBusiness();
  const [loginWorkplace, setLoginWorkplace] = useState<PersonalWorkplaceWire | null>(null);
  const query = useQuery({
    queryKey: PERSONAL_PRODUCT_AFFILIATIONS_QUERY_KEY,
    queryFn: ({ signal }) => listPersonalProductAffiliations(signal),
    meta: { suppressGlobalError: true, operation: "list personal businesses" },
  });
  const workplacesQuery = useQuery({
    queryKey: PERSONAL_WORKPLACES_QUERY_KEY,
    queryFn: ({ signal }) => listPersonalWorkplaces(signal),
    meta: { suppressGlobalError: true, operation: "list personal workplaces" },
  });

  if (query.isPending || (!query.isError && workplacesQuery.isPending)) {
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
  const owned = businesses.filter((row) => ownsProductOrganization(row));
  const ownedOrganizationIds = new Set(
    owned.map((row) => row.organizationId).filter((id): id is string => Boolean(id)),
  );
  const staffWorkplaces = (workplacesQuery.data?.ok ? workplacesQuery.data.workplaces : []).filter(
    (workplace) =>
      isActiveWorkplace(workplace) &&
      Boolean(workplace.organizationId) &&
      !ownedOrganizationIds.has(workplace.organizationId),
  );
  if (owned.length === 0 && staffWorkplaces.length === 0) {
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
    <section className="flex min-w-0 flex-col gap-3" data-testid="personal-business-summary">
      {signInError ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-danger" role="alert">
          {signInError}
        </p>
      ) : null}
      <ul
        className={
          owned.length + staffWorkplaces.length > 1
            ? "catalog-form-section-row personal-business-cards m-0 list-none p-0"
            : "catalog-form-section-row m-0 list-none p-0"
        }
      >
        {owned.map((row, index) => {
          const capability = resolveProductPortfolioCapability(row);
          const meta = businessMetaLine(row);
          return (
            <BusinessHomeCard
              key={row.productCode}
              testId={`personal-business-${row.productCode}`}
              heading={
                <CardHeading icon={Building2}>{t("personal.home.yourBusinesses")}</CardHeading>
              }
              title={row.productDisplayName}
              organizationName={row.organizationDisplayName}
              meta={meta || null}
              managedByOrganization={
                row.canManageBilling ? null : t("personal.subscriptions.managedByOrganization")
              }
              actions={
                <>
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
                </>
              }
            />
          );
        })}
        {staffWorkplaces.map((workplace, index) => {
          return (
            <BusinessHomeCard
              key={workplace.membershipId}
              testId={`personal-staff-business-${workplace.membershipId}`}
              heading={
                <CardHeading icon={BriefcaseBusiness}>{t("personal.home.yourWorkplace")}</CardHeading>
              }
              title={t("operations.shell.productName")}
              organizationName={workplace.organizationDisplayName}
              meta={staffPosRole(workplace)}
              managedByOrganization={null}
              actions={
                <>
                  <Button
                    type="button"
                    className="w-fit"
                    data-testid={`personal-staff-login-${workplace.membershipId}`}
                    onClick={() => setLoginWorkplace(workplace)}
                  >
                    {t("personal.workplaces.login")}
                  </Button>
                  {owned.length === 0 && index === 0 ? (
                    <Button asChild variant="ghost" className="w-fit">
                      <Link to={PERSONAL_BUSINESSES_PATH} data-testid="personal-home-start-another">
                        {t("personal.businesses.startAnother")}
                      </Link>
                    </Button>
                  ) : null}
                </>
              }
            />
          );
        })}
      </ul>
      <WorkplaceSignInDialog
        workplace={loginWorkplace}
        open={loginWorkplace !== null}
        returnPath="/personal"
        onClose={() => setLoginWorkplace(null)}
      />
    </section>
  );
}
