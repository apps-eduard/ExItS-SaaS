import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { useMutation, useQuery } from "@tanstack/react-query";
import { findCommercialPlan, listCommercialPlans } from "@/api/platform/commercial-plans-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import {
  getPersonalProfile,
  listOnboardingBusinessTypes,
  startBusiness,
} from "@/api/platform/start-business-client";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import { ensureOrganizationSlug } from "@/lib/organization-slug";
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
  const { clearBoundWorkspace, refreshWorkspaces } = useWorkspace();

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
    onSuccess: async () => {
      clearBoundWorkspace();
      await refreshSession();
      await refreshWorkspaces();
      navigate("/workspace", { replace: true });
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
      <div className="flex min-w-0 flex-col gap-4" data-testid="personal-start-business-no-plan">
        <PageHeader
          title={t("personal.startBusiness.title")}
          description={t("personal.startBusiness.lede")}
        />
        <ErrorState
          title={t("personal.startBusiness.planRequired")}
          detail={t("personal.startBusiness.planRequiredDetail")}
        />
        <Button asChild className="min-h-11 w-fit" data-testid="start-business-go-explore">
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
      <div className="flex flex-col gap-3" data-testid="personal-start-business-plan-error">
        <ErrorState
          title={t("personal.startBusiness.planLoadFailed")}
          detail={t("personal.startBusiness.planLoadFailedDetail")}
        />
        <Button asChild className="min-h-11 w-fit">
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
    <div className="flex min-w-0 flex-col gap-4" data-testid="personal-start-business-page">
      <PageHeader
        title={t("personal.startBusiness.title")}
        description={t("personal.startBusiness.lede")}
      />
      <Button asChild variant="ghost" className="min-h-11 w-fit">
        <Link to="/personal/explore-pos">{t("personal.startBusiness.changePlan")}</Link>
      </Button>

      <section
        aria-labelledby="start-business-type-heading"
        className="rounded-[var(--exits-radius-md)] border border-border bg-surface p-4"
      >
        <h2
          id="start-business-type-heading"
          className="m-0 text-[length:var(--exits-text-base)] font-semibold"
        >
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
                  className={`min-h-11 rounded-[var(--exits-radius-md)] border px-3 py-2 text-left transition-colors ${
                    selected
                      ? "border-primary bg-[var(--exits-surface-muted)]"
                      : "border-border bg-surface hover:bg-[var(--exits-surface-muted)]"
                  }`}
                  onClick={() => setPrimaryBusinessTypeId(type.id)}
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
        className="rounded-[var(--exits-radius-md)] border border-border bg-surface p-4"
      >
        <h2
          id="start-business-form-heading"
          className="m-0 text-[length:var(--exits-text-base)] font-semibold"
        >
          {t("personal.startBusiness.formTitle")}
        </h2>
        <label className="mt-3 flex flex-col gap-1">
          <span className="text-[length:var(--exits-text-sm)] font-medium">
            {t("personal.startBusiness.displayName")}
          </span>
          <input
            data-testid="start-business-display-name"
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-background px-3"
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
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)] px-3 text-muted"
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
        className="rounded-[var(--exits-radius-md)] border border-border bg-surface p-4"
      >
        <h2
          id="start-business-contact-heading"
          className="m-0 text-[length:var(--exits-text-base)] font-semibold"
        >
          {t("personal.startBusiness.contactTitle")}
        </h2>
        <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
          {t("personal.startBusiness.contactHelper")}
        </p>
        <label className="mt-3 flex min-h-11 items-center gap-2">
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
        className="rounded-[var(--exits-radius-md)] border border-border bg-surface p-4"
        data-testid="start-business-plan-summary"
      >
        <p
          id="start-business-plan-heading"
          className="m-0 text-[length:var(--exits-text-xs)] uppercase tracking-wide text-muted"
        >
          {t("personal.startBusiness.selectedPlan")}
        </p>
        <h2 className="m-0 mt-1 text-[length:var(--exits-text-lg)] font-semibold">
          {plan.displayName}
        </h2>
        <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
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
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-background px-3"
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
        className="min-h-11 w-fit"
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
        className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-background px-3"
        value={value}
        disabled={disabled}
        onChange={(e) => onChange(e.target.value)}
      />
    </label>
  );
}
