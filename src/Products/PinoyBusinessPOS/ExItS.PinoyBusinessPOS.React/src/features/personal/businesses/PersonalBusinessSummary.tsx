import { Fragment, useState, type CSSProperties } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useLocation } from "react-router-dom";
import { symbol } from "@/components/icons/MaterialSymbol";

const ArrowForwardIcon = symbol("arrow_forward");
const BusinessCenterIcon = symbol("business_center");
const DoorOpenIcon = symbol("door_open");
const LoginIcon = symbol("login");
const StoreIcon = symbol("store");
import {
  listPersonalProductAffiliations,
  type PersonalProductAffiliationDto,
} from "@/api/platform/product-affiliations-client";
import {
  listPersonalWorkplaces,
  type PersonalWorkplaceWire,
} from "@/api/platform/personal-workplaces-client";
import { Button } from "@/components/ui/button";
import {
  affiliatedBusinesses,
  ownsProductOrganization,
  resolveProductPortfolioCapability,
} from "@/features/personal/businesses/product-portfolio-capability";
import {
  INTENT_GLOBAL_HELP,
  INTENT_GLOBAL_INFO,
} from "@/features/ui-standards/intent-color-global-default";
import "./PersonalOwnedBusinesses.css";
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

/** Organization ownership is shown as Owner. Product roles such as Cashier stay as given. */
function friendlyRole(role: string | null | undefined, ownerLabel: string): string | null {
  const value = role?.trim() ?? "";
  if (!value) {
    return null;
  }
  if (
    value.localeCompare("Owner", undefined, { sensitivity: "accent" }) === 0 ||
    value.localeCompare("POS Owner", undefined, { sensitivity: "accent" }) === 0 ||
    value.localeCompare("OrganizationOwner", undefined, { sensitivity: "accent" }) === 0 ||
    value.localeCompare("Organization Owner", undefined, { sensitivity: "accent" }) === 0
  ) {
    return ownerLabel;
  }
  return value;
}

function planCaption(plan: string | null | undefined, planWord: string): string | null {
  const value = plan?.trim() ?? "";
  if (!value) {
    return null;
  }
  if (new RegExp(`\\b${planWord}\\b`, "i").test(value)) {
    return value;
  }
  return `${value} ${planWord}`;
}

function MetaLine({ parts }: { parts: string[] }) {
  if (parts.length === 0) {
    return null;
  }

  return (
    <p className="personal-owned-businesses__meta">
      {parts.map((part, index) => (
        <Fragment key={`${part}-${index}`}>
          {index > 0 ? (
            <span className="personal-owned-businesses__sep" aria-hidden="true">
              {" "}
              <span className="personal-owned-businesses__dot">·</span>
              {" "}
            </span>
          ) : null}
          <span
            className={
              part.localeCompare("Active", undefined, { sensitivity: "accent" }) === 0
                ? "personal-owned-businesses__status"
                : undefined
            }
          >
            {part}
          </span>
        </Fragment>
      ))}
    </p>
  );
}

function OwnedBusinessEntry({
  row,
  showExplore,
  showSectionHead,
  entering,
  onOpen,
}: {
  row: PersonalProductAffiliationDto;
  showExplore: boolean;
  showSectionHead: boolean;
  entering: boolean;
  onOpen: (organizationId: string) => void;
}) {
  const { t } = useI18n();
  const capability = resolveProductPortfolioCapability(row);
  const role = friendlyRole(row.roleDisplay, t("personal.home.ownerRole"));
  const plan = planCaption(row.planDisplayName, t("personal.home.planWord"));
  const status = row.subscriptionStatus?.trim() ?? "";
  const metaParts = [role, plan, status].filter((part): part is string => Boolean(part));

  const railLabel = role ?? t("personal.home.ownerRole");

  return (
    <li
      className="personal-home-step-card personal-home-step-card--primary personal-owned-businesses__entry"
      data-testid={`personal-business-${row.productCode}`}
    >
      <div className="personal-home-step-card__rail" aria-hidden="true">
        <span className="personal-home-step-card__icon-wrap">
          <StoreIcon className="personal-home-step-card__icon" />
        </span>
        <div className="personal-home-step-card__label">{railLabel}</div>
      </div>
      <div className="personal-home-step-card__panel">
        {showSectionHead ? (
          <div className="personal-owned-businesses__head">
            <h3 className="personal-owned-businesses__title">{t("personal.home.yourBusinesses")}</h3>
            <p className="personal-owned-businesses__lede">{t("personal.home.yourBusinessesLede")}</p>
          </div>
        ) : null}
        <div className="personal-owned-businesses__copy">
          {row.organizationDisplayName ? (
            <p className="personal-home-step-card__title personal-owned-businesses__name">
              {row.organizationDisplayName}
            </p>
          ) : (
            <p className="personal-home-step-card__title personal-owned-businesses__name">
              {row.productDisplayName}
            </p>
          )}
          {row.organizationDisplayName ? (
            <p className="personal-owned-businesses__product">{row.productDisplayName}</p>
          ) : null}
          <MetaLine parts={metaParts} />
          {!row.canManageBilling ? (
            <p className="personal-owned-businesses__meta">
              {t("personal.subscriptions.managedByOrganization")}
            </p>
          ) : null}
        </div>
        {(capability.canOpenProduct && row.organizationId) || showExplore ? (
          <div className="personal-owned-businesses__actions personal-owned-businesses__actions--owned">
            {capability.canOpenProduct && row.organizationId ? (
              <Button
                type="button"
                intent="info"
                appearance="elevated"
                emphasis="strong"
                shape="pill"
                className="personal-owned-businesses__open personal-owned-businesses__login ui-standards-qbtn--raised"
                style={
                  {
                    backgroundColor: INTENT_GLOBAL_INFO,
                    color: "#fff",
                  } as CSSProperties
                }
                disabled={entering}
                data-testid={`personal-business-open-${row.productCode}`}
                onClick={() => onOpen(row.organizationId!)}
              >
                <DoorOpenIcon className="size-4" aria-hidden="true" />
                {t("personal.home.openBusiness")}
              </Button>
            ) : null}
            {showExplore ? (
              <Button
                asChild
                intent="help"
                appearance="elevated"
                emphasis="strong"
                shape="pill"
                className="personal-owned-businesses__explore personal-owned-businesses__login ui-standards-qbtn--raised"
                style={
                  {
                    backgroundColor: INTENT_GLOBAL_HELP,
                    color: "#fff",
                  } as CSSProperties
                }
              >
                <Link to={PERSONAL_BUSINESSES_PATH} data-testid="personal-home-start-another">
                  {t("personal.home.exploreProducts")}
                  <ArrowForwardIcon className="size-4" aria-hidden="true" />
                </Link>
              </Button>
            ) : null}
          </div>
        ) : null}
      </div>
    </li>
  );
}

function WorkplaceEntry({
  workplace,
  showExplore,
  showSectionHead,
  onLogin,
}: {
  workplace: PersonalWorkplaceWire;
  showExplore: boolean;
  showSectionHead: boolean;
  onLogin: () => void;
}) {
  const { t } = useI18n();
  const role = staffPosRole(workplace);
  const status = workplace.membershipStatus.trim();
  const metaParts = [role, status].filter((part): part is string => Boolean(part));
  const organizationName = workplace.organizationDisplayName.trim();
  const productName = t("operations.shell.productName");

  const railLabel = role ?? t("personal.home.staffRole");

  return (
    <li
      className="personal-home-step-card personal-home-step-card--info personal-owned-businesses__entry"
      data-testid={`personal-staff-business-${workplace.membershipId}`}
    >
      <div className="personal-home-step-card__rail" aria-hidden="true">
        <span className="personal-home-step-card__icon-wrap">
          <BusinessCenterIcon className="personal-home-step-card__icon" />
        </span>
        <div className="personal-home-step-card__label">{railLabel}</div>
      </div>
      <div className="personal-home-step-card__panel">
        {showSectionHead ? (
          <div className="personal-owned-businesses__head">
            <h3 className="personal-owned-businesses__title">{t("personal.home.yourWorkplace")}</h3>
            <p className="personal-owned-businesses__lede">{t("personal.home.yourWorkplaceLede")}</p>
          </div>
        ) : null}
        <div className="personal-owned-businesses__copy">
          <p className="personal-home-step-card__title personal-owned-businesses__name">
            {organizationName || productName}
          </p>
          {organizationName ? <p className="personal-owned-businesses__product">{productName}</p> : null}
          <MetaLine parts={metaParts} />
        </div>
        <div className="personal-owned-businesses__actions personal-owned-businesses__actions--login">
          <Button
            type="button"
            intent="info"
            appearance="elevated"
            emphasis="strong"
            shape="pill"
            className="personal-owned-businesses__open personal-owned-businesses__login ui-standards-qbtn--raised"
            style={
              {
                backgroundColor: INTENT_GLOBAL_INFO,
                color: "#fff",
              } as CSSProperties
            }
            data-testid={`personal-staff-login-${workplace.membershipId}`}
            onClick={onLogin}
          >
            <LoginIcon className="size-4" aria-hidden="true" />
            {t("personal.workplaces.login")}
          </Button>
          {showExplore ? (
            <Button
              asChild
              intent="help"
              appearance="elevated"
              emphasis="strong"
              shape="pill"
              className="personal-owned-businesses__explore personal-owned-businesses__login ui-standards-qbtn--raised"
              style={
                {
                  backgroundColor: INTENT_GLOBAL_HELP,
                  color: "#fff",
                } as CSSProperties
              }
            >
              <Link to={PERSONAL_BUSINESSES_PATH} data-testid="personal-home-start-another">
                {t("personal.home.exploreProducts")}
                <ArrowForwardIcon className="size-4" aria-hidden="true" />
              </Link>
            </Button>
          ) : null}
        </div>
      </div>
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
    return null;
  }

  return (
    <section className="flex min-w-0 flex-col gap-3" data-testid="personal-business-summary">
      {signInError ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-danger" role="alert">
          {signInError}
        </p>
      ) : null}
      <ul className="catalog-form-section-row personal-business-cards m-0 list-none p-0">
        {owned.length > 0 ? (
          <li className="personal-owned-businesses" data-testid="personal-owned-businesses">
            <ul className="personal-owned-businesses__list">
              {owned.map((row, index) => (
                <OwnedBusinessEntry
                  key={row.productCode}
                  row={row}
                  showExplore={index === 0}
                  showSectionHead={index === 0}
                  entering={entering}
                  onOpen={(organizationId) => void enterBusiness(organizationId)}
                />
              ))}
            </ul>
          </li>
        ) : null}
        {staffWorkplaces.length > 0 ? (
          <li className="personal-owned-businesses" data-testid="personal-staff-workplaces">
            <ul className="personal-owned-businesses__list">
              {staffWorkplaces.map((workplace, index) => (
                <WorkplaceEntry
                  key={workplace.membershipId}
                  workplace={workplace}
                  showExplore={owned.length === 0 && index === 0}
                  showSectionHead={index === 0}
                  onLogin={() => setLoginWorkplace(workplace)}
                />
              ))}
            </ul>
          </li>
        ) : null}
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
