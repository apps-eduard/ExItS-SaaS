import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { useMutation, useQuery } from "@tanstack/react-query";
import { findCommercialPlan, listCommercialPlans } from "@/api/platform/commercial-plans-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import {
  getPersonalProfile,
  listOnboardingBusinessTypes,
  startBusiness,
} from "@/api/platform/start-business-client";
import { ensureOnboardingProgress } from "@/api/pos/pos-onboarding-client";
import { writePendingPostSubscriptionOnboarding } from "@/features/onboarding/post-subscription-onboarding";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import { ensureOrganizationSlug } from "@/lib/organization-slug";
import { personalPageBackNav } from "@/navigation/page-back-nav";
import { useSession } from "@/session/SessionProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function parseBoolFlag(raw: string | null, defaultValue: boolean): boolean {
  if (raw == null || raw === "") return defaultValue;
  return raw === "1" || raw.toLowerCase() === "true";
}

function nullIfBlank(value: string): string | null {
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
}

export function PersonalStartBusinessPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { refreshSession } = useSession();
  const { clearBoundWorkspace, refreshWorkspaces, bindDestination } = useWorkspace();

  const planKey = (searchParams.get("planKey") ?? "").trim();
  const startAsTrial = parseBoolFlag(searchParams.get("trial"), true);
  const payNow = parseBoolFlag(searchParams.get("payNow"), false);
  const billingRaw = searchParams.get("billing");
  const billingCycle = billingRaw === "Annual" || billingRaw === "Monthly" ? billingRaw : "Monthly";

  const [displayName, setDisplayName] = useState("");
  const [slugPreview, setSlugPreview] = useState("");
  const [primaryBusinessTypeId, setPrimaryBusinessTypeId] = useState("");
  const [useMyContactDetails, setUseMyContactDetails] = useState(true);
  const [contactEmail, setContactEmail] = useState("");
  const [contactPhone, setContactPhone] = useState("");
  const [addressLine1, setAddressLine1] = useState("");
  const [city, setCity] = useState("");
  const [region, setRegion] = useState("");
  const [postalCode, setPostalCode] = useState("");
  const [countryCode, setCountryCode] = useState("PH");
  const [formError, setFormError] = useState<string | null>(null);
  const displayNameInputRef = useRef<HTMLInputElement>(null);

  function selectPrimaryBusinessType(typeId: string) {
    setPrimaryBusinessTypeId(typeId);
    // After a long business-type list (especially mobile), move focus to Business name
    // so the next field is on-screen and ready to type.
    requestAnimationFrame(() => {
      const input = displayNameInputRef.current;
      if (!input || input.disabled) {
        return;
      }
      input.focus({ preventScroll: false });
      input.scrollIntoView({ behavior: "smooth", block: "center" });
    });
  }

  const plansQuery = useQuery({
    queryKey: ["commercial", "plans", "pinoy-business-pos"],
    queryFn: ({ signal }) => listCommercialPlans(undefined, signal),
    enabled: planKey.length > 0,
  });
  const typesQuery = useQuery({
    queryKey: ["personal", "onboarding", "business-types"],
    queryFn: ({ signal }) => listOnboardingBusinessTypes(signal),
    enabled: planKey.length > 0,
  });
  const profileQuery = useQuery({
    queryKey: ["personal", "profile"],
    queryFn: ({ signal }) => getPersonalProfile(signal),
    enabled: planKey.length > 0,
  });

  const plan = useMemo(
    () => (plansQuery.data && planKey ? findCommercialPlan(plansQuery.data, planKey) : undefined),
    [plansQuery.data, planKey],
  );

  useEffect(() => {
    if (!profileQuery.data || !useMyContactDetails) return;
    setContactEmail(profileQuery.data.email ?? "");
    setContactPhone(profileQuery.data.phone ?? "");
  }, [profileQuery.data, useMyContactDetails]);

  useEffect(() => {
    const name = displayName.trim();
    setSlugPreview(name ? ensureOrganizationSlug(name) : "");
  }, [displayName]);

  const mutation = useMutation({
    mutationFn: async () => {
      const name = displayName.trim();
      if (!name) {
        throw new Error(t("personal.startBusiness.validationRequired"));
      }
      if (!primaryBusinessTypeId) {
        throw new Error(t("personal.startBusiness.primaryTypeRequired"));
      }
      if (!planKey) {
        throw new Error(t("personal.startBusiness.planRequired"));
      }

      const slug = slugPreview || ensureOrganizationSlug(name);
      return startBusiness({
        displayName: name,
        slug,
        primaryBusinessTypeId,
        planKey,
        billingCycle,
        startAsTrial: startAsTrial && !payNow,
        payNow,
        useMyContactDetails,
        contactEmail: nullIfBlank(contactEmail),
        contactPhone: nullIfBlank(contactPhone),
        addressLine1: nullIfBlank(addressLine1),
        city: nullIfBlank(city),
        region: nullIfBlank(region),
        postalCode: nullIfBlank(postalCode),
        countryCode: nullIfBlank(countryCode),
      });
    },
    onSuccess: async (result) => {
      const orgId = result.organizationId;
      const selectedType = typesQuery.data?.find((item) => item.id === primaryBusinessTypeId);
      // Set before session refresh so auto-destination cannot send the new org to Sell.
      writePendingPostSubscriptionOnboarding({
        organizationId: orgId,
        primaryBusinessTypeId: result.primaryBusinessTypeId,
        businessTypeCode: selectedType?.code ?? null,
        businessTypeName: selectedType?.name ?? null,
        businessTypeDescription: selectedType?.description ?? null,
      });

      clearBoundWorkspace();
      const sessionStatus = await refreshSession();
      if (sessionStatus !== "authenticated") {
        setFormError(t("personal.startBusiness.sessionSwitchFailed"));
        return;
      }

      const workspace = { organizationId: orgId, branchId: result.primaryBranchId };
      const orgLabel = displayName.trim() || t("onboarding.ready.businessFallback");
      // Leave Personal-only routes before bind awaits (session is now Organization).
      navigate("/onboarding", { replace: true });

      try {
        await refreshWorkspaces();
        const bound = await bindDestination({
          organizationId: orgId,
          organizationDisplayName: orgLabel,
          branchId: null,
          branchName: null,
          experience: "manage_business",
          route: "/onboarding",
          labelKey: "experience.manageBusiness",
        });
        if (bound) {
          await ensureOnboardingProgress(workspace, {
            primaryBusinessTypeId: result.primaryBusinessTypeId,
          });
        }
      } catch {
        // Onboarding waits for the POS session grant, then can ensure progress from sessionStorage.
      }
    },
    onError: (error) => {
      if (error instanceof PlatformApiError) {
        setFormError(error.problem.detail ?? error.message ?? t("personal.startBusiness.failed"));
        return;
      }
      setFormError(error instanceof Error ? error.message : t("personal.startBusiness.failed"));
    },
  });

  if (!planKey) {
    return (
      <div
        className="personal-page exits-page flex min-w-0 flex-col gap-4"
        data-testid="personal-start-business-no-plan"
      >
        <PageHeader
          title={t("personal.startBusiness.title")}
          description={t("personal.startBusiness.lede")}
          backTo={personalPageBackNav.explore.to}
          backLabel={t(personalPageBackNav.explore.labelKey)}
          backTestId="page-header-back-start-business"
        />
        <ErrorState
          title={t("personal.startBusiness.planRequired")}
          detail={t("personal.startBusiness.planRequiredDetail")}
        />
        <Button asChild className="w-fit" data-testid="start-business-go-explore">
          <Link to="/personal/explore-pos">{t("personal.explore.title")}</Link>
        </Button>
      </div>
    );
  }

  if (plansQuery.isPending || typesQuery.isPending) {
    return <LoadingSkeleton label={t("personal.startBusiness.loading")} />;
  }

  if (plansQuery.isError || !plan) {
    return (
      <div
        className="personal-page exits-page flex flex-col gap-3"
        data-testid="personal-start-business-plan-error"
      >
        <PageHeader
          title={t("personal.startBusiness.title")}
          backTo={personalPageBackNav.explore.to}
          backLabel={t(personalPageBackNav.explore.labelKey)}
          backTestId="page-header-back-start-business"
        />
        <ErrorState
          title={t("personal.startBusiness.planLoadFailed")}
          detail={t("personal.startBusiness.planLoadFailedDetail")}
        />
        <Button asChild className="w-fit">
          <Link to="/personal/explore-pos">{t("personal.explore.title")}</Link>
        </Button>
      </div>
    );
  }

  const selectedPrice = billingCycle === "Annual" ? plan.annualPrice : plan.monthlyPrice;
  const modeLabel =
    startAsTrial && !payNow
      ? t("personal.startBusiness.modeTrial")
      : t("personal.startBusiness.modeSubscribe");

  return (
    <div className="personal-page exits-page flex min-w-0 flex-col gap-3" data-testid="personal-start-business-page">
      <PageHeader
        title={t("personal.startBusiness.title")}
        description={t("personal.startBusiness.lede")}
        backTo={personalPageBackNav.explore.to}
        backLabel={t(personalPageBackNav.explore.labelKey)}
        backTestId="page-header-back-start-business"
      />

      <section
        aria-labelledby="start-business-type-heading"
        className="catalog-form-section exits-animate-panel personal-section gap-3"
      >
        <h2 id="start-business-type-heading" className="catalog-form-section__title">
          {t("personal.startBusiness.primaryTypeTitle")}
        </h2>
        <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
          {t("personal.startBusiness.primaryTypeHint")}
        </p>
        {typesQuery.isError || (typesQuery.data?.length ?? 0) === 0 ? (
          <p className="mt-3 text-[length:var(--exits-text-sm)] text-muted">
            {t("personal.startBusiness.businessTypesLoadFailed")}
          </p>
        ) : (
          <div
            className="mt-3 grid gap-2 sm:grid-cols-2"
            role="listbox"
            aria-label={t("personal.startBusiness.primaryTypeTitle")}
          >
            {typesQuery.data!.map((type) => {
              const selected = primaryBusinessTypeId === type.id;
              return (
                <button
                  key={type.id}
                  type="button"
                  role="option"
                  aria-selected={selected}
                  disabled={mutation.isPending}
                  data-testid={`start-business-type-${type.code}`}
                  className={` rounded-[var(--exits-radius-md)] border px-3 py-2 text-left transition-colors ${
                    selected
                      ? "border-primary bg-[var(--exits-surface-muted)]"
                      : "border-border bg-surface hover:bg-[var(--exits-surface-muted)]"
                  }`}
                  onClick={() => selectPrimaryBusinessType(type.id)}
                >
                  <span className="block font-semibold">{type.name}</span>
                  {type.description ? (
                    <span className="mt-0.5 block text-[length:var(--exits-text-xs)] text-muted">
                      {type.description}
                    </span>
                  ) : null}
                </button>
              );
            })}
          </div>
        )}
      </section>

      <section
        aria-labelledby="start-business-form-heading"
        className="catalog-form-section exits-animate-panel personal-section gap-3"
      >
        <h2 id="start-business-form-heading" className="catalog-form-section__title">
          {t("personal.startBusiness.formTitle")}
        </h2>
        <label className="mt-3 flex flex-col gap-1">
          <span className="text-[length:var(--exits-text-sm)] font-medium">
            {t("personal.startBusiness.displayName")}
          </span>
          <input
            ref={displayNameInputRef}
            data-testid="start-business-display-name"
            className="rounded-[var(--exits-radius-md)] border border-border bg-background px-3"
            value={displayName}
            disabled={mutation.isPending}
            placeholder={t("personal.startBusiness.displayNamePlaceholder")}
            onChange={(e) => setDisplayName(e.target.value)}
            required
          />
        </label>
        <label className="mt-3 flex flex-col gap-1">
          <span className="text-[length:var(--exits-text-sm)] font-medium">
            {t("personal.startBusiness.slug")}
          </span>
          <input
            data-testid="start-business-slug"
            className="rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)] px-3 text-muted"
            value={slugPreview}
            readOnly
            aria-readonly="true"
            disabled={mutation.isPending}
            placeholder={t("personal.startBusiness.slugPlaceholder")}
            tabIndex={-1}
          />
        </label>
        <p className="m-0 mt-2 text-[length:var(--exits-text-xs)] text-muted">
          {t("personal.startBusiness.slugHint")}
        </p>
      </section>

      <section
        aria-labelledby="start-business-contact-heading"
        className="catalog-form-section exits-animate-panel personal-section gap-3"
      >
        <h2 id="start-business-contact-heading" className="catalog-form-section__title">
          {t("personal.startBusiness.contactTitle")}
        </h2>
        <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
          {t("personal.startBusiness.contactHelper")}
        </p>
        <label className="mt-3 flex items-center gap-2">
          <input
            type="checkbox"
            checked={useMyContactDetails}
            disabled={mutation.isPending}
            data-testid="start-business-use-my-contact"
            onChange={(e) => setUseMyContactDetails(e.target.checked)}
          />
          <span>{t("personal.startBusiness.useMyContactDetails")}</span>
        </label>
        <div className="mt-3 grid gap-3 sm:grid-cols-2">
          <Field
            label={t("personal.startBusiness.contactEmail")}
            value={contactEmail}
            disabled={mutation.isPending}
            onChange={setContactEmail}
            testId="start-business-contact-email"
          />
          <Field
            label={t("personal.startBusiness.contactPhone")}
            value={contactPhone}
            disabled={mutation.isPending}
            onChange={setContactPhone}
            testId="start-business-contact-phone"
          />
          <Field
            label={t("personal.startBusiness.addressLine1")}
            value={addressLine1}
            disabled={mutation.isPending}
            onChange={setAddressLine1}
            testId="start-business-address"
          />
          <Field
            label={t("personal.startBusiness.city")}
            value={city}
            disabled={mutation.isPending}
            onChange={setCity}
            testId="start-business-city"
          />
          <Field
            label={t("personal.startBusiness.region")}
            value={region}
            disabled={mutation.isPending}
            onChange={setRegion}
            testId="start-business-region"
          />
          <Field
            label={t("personal.startBusiness.postalCode")}
            value={postalCode}
            disabled={mutation.isPending}
            onChange={setPostalCode}
            testId="start-business-postal"
          />
          <Field
            label={t("personal.startBusiness.country")}
            value={countryCode}
            disabled={mutation.isPending}
            onChange={setCountryCode}
            testId="start-business-country"
          />
        </div>
      </section>

      <section
        aria-labelledby="start-business-plan-heading"
        className="catalog-form-section exits-animate-panel personal-section gap-3"
        data-testid="start-business-plan-summary"
      >
        <p
          id="start-business-plan-heading"
          className="catalog-form-section__title text-muted"
        >
          {t("personal.startBusiness.selectedPlan")}
        </p>
        <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
          {plan.displayName}
        </h2>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {modeLabel} ·{" "}
          {billingCycle === "Annual"
            ? t("personal.explore.billingYear")
            : t("personal.explore.billingMonth")}
        </p>
        {startAsTrial && !payNow ? (
          <p className="m-0 mt-2 text-[length:var(--exits-text-sm)]">
            {t("personal.startBusiness.trialDays").replace("{days}", String(plan.defaultTrialDays))}
          </p>
        ) : null}
        <p className="m-0 mt-2 text-[length:var(--exits-text-base)] font-semibold">
          {selectedPrice.toLocaleString()} {plan.currencyCode}
        </p>
        <label className="mt-3 flex flex-col gap-1">
          <span className="text-[length:var(--exits-text-sm)] font-medium">
            {t("personal.startBusiness.billingCycle")}
          </span>
          <select
            className="exits-select"
            value={billingCycle}
            disabled={mutation.isPending}
            data-testid="start-business-billing"
            onChange={(e) => {
              const next = e.target.value === "Annual" ? "Annual" : "Monthly";
              const params = new URLSearchParams(searchParams);
              params.set("billing", next);
              navigate(`/personal/start-business?${params.toString()}`, { replace: true });
            }}
          >
            <option value="Monthly">{t("personal.explore.billingMonth")}</option>
            <option value="Annual">{t("personal.explore.billingYear")}</option>
          </select>
        </label>
      </section>

      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("personal.startBusiness.confirmHint")}
      </p>
      {formError ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive" role="alert">
          {formError}
        </p>
      ) : null}

      <Button
        type="button"
        className="w-fit"
        disabled={mutation.isPending}
        data-testid="start-business-submit"
        onClick={() => {
          setFormError(null);
          mutation.mutate();
        }}
      >
        {mutation.isPending
          ? t("personal.startBusiness.submitting")
          : t("personal.startBusiness.confirmSubmit")}
      </Button>
    </div>
  );
}

function Field({
  label,
  value,
  onChange,
  disabled,
  testId,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
  testId: string;
}) {
  return (
    <label className="flex flex-col gap-1">
      <span className="text-[length:var(--exits-text-sm)] font-medium">{label}</span>
      <input
        data-testid={testId}
        className="rounded-[var(--exits-radius-md)] border border-border bg-background px-3"
        value={value}
        disabled={disabled}
        onChange={(e) => onChange(e.target.value)}
      />
    </label>
  );
}
