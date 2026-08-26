import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Check, ChevronRight, Loader2, Package, SkipForward, Store } from "lucide-react";
import {
  getOrganization,
  updateOrganizationProfile,
} from "@/api/platform/organization-profile-client";
import {
  getPublishedTemplate,
  listPublishedTemplates,
} from "@/api/platform/merchant-catalog-client";
import {
  ensureOnboardingProgress,
  getOnboardingProgress,
  updateOnboardingProgress,
  type OrganizationOnboardingProgressDto,
} from "@/api/pos/pos-onboarding-client";
import { importTemplateBatch } from "@/api/pos/pos-catalog-import-client";
import {
  getOperationalSetup,
  updateOperationalSetup,
} from "@/api/pos/pos-operational-setup-client";
import { PosApiError } from "@/api/pos/pos-http";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { resolveBusinessSetupPreset } from "@/features/onboarding/business-setup-presets";
import {
  ONBOARDING_WIZARD_STEPS,
  resolveOnboardingWizardStep,
  type OnboardingWizardStep,
} from "@/features/onboarding/onboarding-steps";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";
import { useSession } from "@/session/SessionProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function nullIfBlank(value: string): string | null {
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
}

export function PostSubscriptionOnboardingPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { session } = useSession();
  const { boundWorkspace, workspaces, bindDestination, status: workspaceStatus } = useWorkspace();

  const organizationId =
    boundWorkspace?.organizationId ??
    session?.selectedOrganizationId ??
    workspaces[0]?.organizationId ??
    null;

  const workspaceScope = useMemo(
    () =>
      organizationId
        ? { organizationId, branchId: boundWorkspace?.branchId ?? null }
        : null,
    [boundWorkspace?.branchId, organizationId],
  );

  const progressQuery = useQuery({
    queryKey: ["pos", "onboarding", "progress", organizationId],
    enabled: Boolean(workspaceScope),
    queryFn: async ({ signal }) => {
      try {
        return await getOnboardingProgress(workspaceScope!, signal);
      } catch (error) {
        if (!(error instanceof PosApiError) || error.status !== 404) {
          throw error;
        }
        // Existing orgs have no progress row — never backfill from a casual visit.
        // Only ensure when Start Business left a pending flag for this org.
        const pendingRaw = sessionStorage.getItem("exits.postSubscriptionOnboarding");
        if (!pendingRaw || !organizationId) {
          return null;
        }
        try {
          const pending = JSON.parse(pendingRaw) as {
            organizationId?: string;
            primaryBusinessTypeId?: string | null;
          };
          if (pending.organizationId !== organizationId) {
            return null;
          }
          const created = await ensureOnboardingProgress(
            workspaceScope!,
            { primaryBusinessTypeId: pending.primaryBusinessTypeId ?? null },
            signal,
          );
          sessionStorage.removeItem("exits.postSubscriptionOnboarding");
          return created;
        } catch {
          return null;
        }
      }
    },
    retry: false,
  });

  const [stepOverride, setStepOverride] = useState<OnboardingWizardStep | null>(null);
  const step =
    stepOverride ?? resolveOnboardingWizardStep(progressQuery.data ?? null);

  useEffect(() => {
    setStepOverride(null);
  }, [progressQuery.data?.organizationSetupStatus, progressQuery.data?.businessSetupStatus, progressQuery.data?.productTemplateStatus]);

  if (!organizationId || workspaceStatus === "loading") {
    return <LoadingSkeleton label={t("onboarding.loading")} />;
  }

  if (!workspaceScope) {
    return (
      <ErrorState title={t("onboarding.missingOrgTitle")} detail={t("onboarding.missingOrgDetail")} />
    );
  }

  if (progressQuery.isPending) {
    return <LoadingSkeleton label={t("onboarding.loading")} />;
  }

  if (progressQuery.isError) {
    return (
      <div className="exits-page mx-auto flex w-full max-w-xl flex-col gap-3 p-3">
        <ErrorState
          title={t("onboarding.loadErrorTitle")}
          detail={t("onboarding.loadErrorDetail")}
        />
        <Button type="button" className="min-h-11 w-full" onClick={() => void progressQuery.refetch()}>
          {t("onboarding.retry")}
        </Button>
      </div>
    );
  }

  if (!progressQuery.data) {
    return (
      <div className="exits-page mx-auto flex w-full max-w-xl flex-col gap-3 p-3">
        <ErrorState
          title={t("onboarding.notRequiredTitle")}
          detail={t("onboarding.notRequiredDetail")}
        />
        <Button type="button" className="min-h-11 w-full" onClick={() => navigate("/sell", { replace: true })}>
          {t("onboarding.ready.startSelling")}
        </Button>
      </div>
    );
  }

  const progress = progressQuery.data;

  async function refreshProgress() {
    await queryClient.invalidateQueries({ queryKey: ["pos", "onboarding", "progress", organizationId] });
  }

  return (
    <div
      className="exits-page mx-auto flex w-full max-w-xl min-w-0 flex-col gap-3 p-3"
      data-testid="post-subscription-onboarding-page"
    >
      <PageHeader title={t("onboarding.title")} description={t("onboarding.lede")} />

      <nav
        className="onboarding-progress flex flex-wrap gap-2"
        aria-label={t("onboarding.progressLabel")}
        data-testid="onboarding-progress"
      >
        {ONBOARDING_WIZARD_STEPS.map((item, index) => {
          const label = t(`onboarding.step.${item}` as MessageKey);
          const active = item === step;
          return (
            <span
              key={item}
              className={cn(
                "inline-flex min-h-9 items-center gap-1 rounded-[var(--exits-radius-md)] border px-2.5 text-[length:var(--exits-text-xs)] font-semibold",
                active
                  ? "border-[var(--exits-primary)] bg-[color-mix(in_srgb,var(--exits-primary)_12%,transparent)] text-[var(--exits-primary)]"
                  : "border-border text-muted",
              )}
              aria-current={active ? "step" : undefined}
            >
              <span aria-hidden>{index + 1}</span>
              {label}
            </span>
          );
        })}
      </nav>

      {step === "organization" ? (
        <OrganizationSetupStep
          workspace={workspaceScope}
          organizationId={organizationId}
          progress={progress}
          onAdvanced={async () => {
            await refreshProgress();
            setStepOverride("business");
          }}
        />
      ) : null}
      {step === "business" ? (
        <BusinessSetupStep
          workspace={workspaceScope}
          progress={progress}
          onAdvanced={async () => {
            await refreshProgress();
            setStepOverride("products");
          }}
        />
      ) : null}
      {step === "products" ? (
        <ProductTemplateStep
          workspace={workspaceScope}
          progress={progress}
          onAdvanced={async () => {
            await refreshProgress();
            setStepOverride("ready");
          }}
        />
      ) : null}
      {step === "ready" ? (
        <ReadyStep
          workspace={workspaceScope}
          progress={progress}
          organizationId={organizationId}
          bindDestination={bindDestination}
          workspaces={workspaces}
          onDone={() => navigate("/sell", { replace: true })}
          onFinishLater={() => navigate("/org", { replace: true })}
        />
      ) : null}
    </div>
  );
}

function OrganizationSetupStep({
  workspace,
  organizationId,
  progress,
  onAdvanced,
}: {
  workspace: { organizationId: string; branchId?: string | null };
  organizationId: string;
  progress: OrganizationOnboardingProgressDto;
  onAdvanced: () => Promise<void>;
}) {
  const { t } = useI18n();
  const orgQuery = useQuery({
    queryKey: ["platform", "organization", organizationId],
    queryFn: ({ signal }) => getOrganization(organizationId, signal),
  });
  const setupQuery = useQuery({
    queryKey: ["pos", "operational-setup", organizationId],
    queryFn: ({ signal }) => getOperationalSetup(workspace, signal),
    retry: false,
  });

  const [displayName, setDisplayName] = useState("");
  const [contactPhone, setContactPhone] = useState("");
  const [contactEmail, setContactEmail] = useState("");
  const [addressLine1, setAddressLine1] = useState("");
  const [city, setCity] = useState("");
  const [region, setRegion] = useState("");
  const [postalCode, setPostalCode] = useState("");
  const [countryCode, setCountryCode] = useState("PH");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!orgQuery.data) return;
    setDisplayName(orgQuery.data.displayName ?? "");
    setContactPhone(orgQuery.data.profile.contactPhone ?? setupQuery.data?.contactPhone ?? "");
    setContactEmail(orgQuery.data.profile.contactEmail ?? "");
    setAddressLine1(orgQuery.data.profile.addressLine1 ?? setupQuery.data?.businessAddress ?? "");
    setCity(orgQuery.data.profile.city ?? "");
    setRegion(orgQuery.data.profile.region ?? "");
    setPostalCode(orgQuery.data.profile.postalCode ?? "");
    setCountryCode(orgQuery.data.profile.countryCode ?? "PH");
  }, [orgQuery.data, setupQuery.data]);

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (!orgQuery.data) throw new Error(t("onboarding.org.saveFailed"));
      await updateOrganizationProfile(organizationId, {
        displayName: displayName.trim() || orgQuery.data.displayName,
        contactEmail: nullIfBlank(contactEmail),
        contactPhone: nullIfBlank(contactPhone),
        addressLine1: nullIfBlank(addressLine1),
        city: nullIfBlank(city),
        region: nullIfBlank(region),
        postalCode: nullIfBlank(postalCode),
        countryCode: nullIfBlank(countryCode),
        expectedUpdatedAtUtc: orgQuery.data.updatedAtUtc,
      });
      // New orgs often have incomplete POS operational setup — PUT update requires completed setup.
      // Platform organization profile is the authoritative onboarding write for step 1.
      const setupComplete = Boolean(setupQuery.data?.isComplete ?? setupQuery.data?.isCompleted);
      if (setupQuery.data && setupComplete) {
        try {
          await updateOperationalSetup(workspace, {
            storeDisplayName: displayName.trim() || setupQuery.data.storeDisplayName,
            currencyCode: setupQuery.data.currencyCode,
            taxPricingMode: setupQuery.data.taxPricingMode,
            taxRatePercent: setupQuery.data.taxRatePercent,
            expectedUpdatedAtUtc: setupQuery.data.updatedAtUtc,
            contactPhone: nullIfBlank(contactPhone),
            businessAddress: nullIfBlank(addressLine1),
            receiptHeader: setupQuery.data.receiptHeader,
            receiptFooter: setupQuery.data.receiptFooter,
            cashCountMode: setupQuery.data.cashCountMode,
            openingCashCountMode: setupQuery.data.openingCashCountMode,
            closingCashCountMode: setupQuery.data.closingCashCountMode,
          });
        } catch {
          // Platform profile save is enough to continue; POS setup is best-effort.
        }
      }
      await updateOnboardingProgress(workspace, { organizationSetupStatus: "Completed" });
    },
    onSuccess: async () => {
      setError(null);
      await onAdvanced();
    },
    onError: (err) => {
      setError(
        err instanceof PlatformApiError || err instanceof PosApiError
          ? (err.problem?.detail ?? err.message)
          : t("onboarding.org.saveFailed"),
      );
    },
  });

  const skipMutation = useMutation({
    mutationFn: async () => {
      await updateOnboardingProgress(workspace, { organizationSetupStatus: "Skipped" });
    },
    onSuccess: async () => {
      setError(null);
      await onAdvanced();
    },
    onError: () => setError(t("onboarding.org.saveFailed")),
  });

  if (orgQuery.isPending) {
    return <LoadingSkeleton label={t("onboarding.loading")} />;
  }

  return (
    <section className="catalog-form-section flex flex-col gap-3" data-testid="onboarding-org-step">
      <h2 className="catalog-form-section__title m-0">{t("onboarding.org.title")}</h2>
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("onboarding.org.lede")}</p>

      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        {t("onboarding.org.displayName")}
        <input
          className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
          value={displayName}
          onChange={(e) => setDisplayName(e.target.value)}
          data-testid="onboarding-org-display-name"
        />
      </label>
      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        {t("onboarding.org.contactPhone")}
        <input
          className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
          value={contactPhone}
          onChange={(e) => setContactPhone(e.target.value)}
          data-testid="onboarding-org-phone"
        />
      </label>
      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        {t("onboarding.org.contactEmail")}
        <input
          className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
          value={contactEmail}
          onChange={(e) => setContactEmail(e.target.value)}
          data-testid="onboarding-org-email"
        />
      </label>
      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        {t("onboarding.org.address")}
        <input
          className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
          value={addressLine1}
          onChange={(e) => setAddressLine1(e.target.value)}
          data-testid="onboarding-org-address"
        />
      </label>
      <div className="grid grid-cols-2 gap-2">
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("onboarding.org.city")}
          <input
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={city}
            onChange={(e) => setCity(e.target.value)}
          />
        </label>
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("onboarding.org.region")}
          <input
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={region}
            onChange={(e) => setRegion(e.target.value)}
          />
        </label>
      </div>
      <div className="grid grid-cols-2 gap-2">
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("onboarding.org.postalCode")}
          <input
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={postalCode}
            onChange={(e) => setPostalCode(e.target.value)}
          />
        </label>
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("onboarding.org.country")}
          <input
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={countryCode}
            onChange={(e) => setCountryCode(e.target.value)}
          />
        </label>
      </div>

      {error ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]" role="alert">
          {error}
        </p>
      ) : null}

      <Button
        type="button"
        className="min-h-11 w-full"
        disabled={saveMutation.isPending}
        data-testid="onboarding-org-save"
        onClick={() => void saveMutation.mutateAsync()}
      >
        {saveMutation.isPending ? <Loader2 className="size-4 animate-spin" /> : null}
        {t("onboarding.org.saveContinue")}
      </Button>
      <Button
        type="button"
        variant="ghost"
        className="min-h-11 w-full"
        disabled={skipMutation.isPending}
        data-testid="onboarding-org-skip"
        onClick={() => void skipMutation.mutateAsync()}
      >
        <SkipForward className="size-4" aria-hidden />
        {t("onboarding.skipForNow")}
      </Button>
      <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t("onboarding.org.skipHint")}</p>
      <span className="sr-only">{progress.organizationSetupStatus}</span>
    </section>
  );
}

function readPendingBusinessTypeHint(): {
  code: string | null;
  name: string | null;
  description: string | null;
} {
  try {
    const raw = sessionStorage.getItem("exits.postSubscriptionOnboarding");
    if (!raw) return { code: null, name: null, description: null };
    const pending = JSON.parse(raw) as {
      businessTypeCode?: string | null;
      businessTypeName?: string | null;
      businessTypeDescription?: string | null;
    };
    return {
      code: pending.businessTypeCode?.trim() || null,
      name: pending.businessTypeName?.trim() || null,
      description: pending.businessTypeDescription?.trim() || null,
    };
  } catch {
    return { code: null, name: null, description: null };
  }
}

function BusinessSetupStep({
  workspace,
  progress,
  onAdvanced,
}: {
  workspace: { organizationId: string; branchId?: string | null };
  progress: OrganizationOnboardingProgressDto;
  onAdvanced: () => Promise<void>;
}) {
  const { t } = useI18n();
  const [error, setError] = useState<string | null>(null);
  const hint = useMemo(() => readPendingBusinessTypeHint(), []);
  const preset = resolveBusinessSetupPreset(hint.code ?? hint.name);

  const applyMutation = useMutation({
    mutationFn: async () => {
      await updateOnboardingProgress(workspace, {
        businessSetupStatus: "Completed",
        primaryBusinessTypeId: progress.primaryBusinessTypeId,
      });
    },
    onSuccess: async () => {
      setError(null);
      await onAdvanced();
    },
    onError: () => setError(t("onboarding.business.applyFailed")),
  });

  const skipMutation = useMutation({
    mutationFn: async () => {
      await updateOnboardingProgress(workspace, { businessSetupStatus: "Skipped" });
    },
    onSuccess: async () => {
      setError(null);
      await onAdvanced();
    },
    onError: () => setError(t("onboarding.business.applyFailed")),
  });

  return (
    <section className="catalog-form-section flex flex-col gap-3" data-testid="onboarding-business-step">
      <h2 className="catalog-form-section__title m-0">{t("onboarding.business.title")}</h2>
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("onboarding.business.lede")}</p>
      <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t("onboarding.business.notProducts")}</p>

      <div className="rounded-[var(--exits-radius-lg)] border border-border bg-surface p-4" data-testid="onboarding-business-preset">
        <div className="mb-2 flex items-center gap-2">
          <Store className="size-5 text-[var(--exits-primary)]" aria-hidden />
          <h3 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {hint.name ?? t(preset.titleKey as MessageKey)}
          </h3>
        </div>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {hint.description || t(preset.blurbKey as MessageKey)}
        </p>
        <ul className="mt-3 mb-0 grid list-none gap-1.5 p-0">
          {preset.bulletKeys.map((key) => (
            <li key={key} className="flex items-start gap-2 text-[length:var(--exits-text-sm)]">
              <Check className="mt-0.5 size-4 shrink-0 text-[var(--exits-primary)]" aria-hidden />
              <span>{t(key as MessageKey)}</span>
            </li>
          ))}
        </ul>
      </div>

      {error ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]" role="alert">
          {error}
        </p>
      ) : null}

      <Button
        type="button"
        className="min-h-11 w-full"
        disabled={applyMutation.isPending}
        data-testid="onboarding-business-apply"
        onClick={() => void applyMutation.mutateAsync()}
      >
        {t("onboarding.business.useSetup")}
      </Button>
      <Button
        type="button"
        variant="ghost"
        className="min-h-11 w-full"
        disabled={skipMutation.isPending}
        data-testid="onboarding-business-skip"
        onClick={() => void skipMutation.mutateAsync()}
      >
        <SkipForward className="size-4" aria-hidden />
        {t("onboarding.skipForNow")}
      </Button>
    </section>
  );
}

function ProductTemplateStep({
  workspace,
  progress,
  onAdvanced,
}: {
  workspace: { organizationId: string; branchId?: string | null };
  progress: OrganizationOnboardingProgressDto;
  onAdvanced: () => Promise<void>;
}) {
  const { t } = useI18n();
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const templatesQuery = useQuery({
    queryKey: ["catalog", "templates", progress.primaryBusinessTypeId],
    queryFn: ({ signal }) =>
      listPublishedTemplates(
        {
          businessTypeId: progress.primaryBusinessTypeId ?? undefined,
          pageSize: 20,
        },
        signal,
      ),
  });

  const previewQuery = useQuery({
    queryKey: ["catalog", "template", selectedId],
    enabled: Boolean(selectedId),
    queryFn: ({ signal }) => getPublishedTemplate(selectedId!, signal),
  });

  const importMutation = useMutation({
    mutationFn: async () => {
      if (!selectedId) throw new Error(t("onboarding.products.selectRequired"));
      await importTemplateBatch(workspace, {
        platformTemplateId: selectedId,
        batchNumber: 1,
        idempotencyKey: `onboarding-${workspace.organizationId}-${selectedId}-batch-1`,
      });
      await updateOnboardingProgress(workspace, { productTemplateStatus: "Completed" });
    },
    onSuccess: async () => {
      setError(null);
      await onAdvanced();
    },
    onError: (err) => {
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? err.message)
          : t("onboarding.products.importFailed"),
      );
    },
  });

  const skipMutation = useMutation({
    mutationFn: async () => {
      await updateOnboardingProgress(workspace, { productTemplateStatus: "Skipped" });
    },
    onSuccess: async () => {
      setError(null);
      await onAdvanced();
    },
    onError: () => setError(t("onboarding.products.importFailed")),
  });

  const templates = templatesQuery.data?.items ?? [];

  return (
    <section className="catalog-form-section flex flex-col gap-3" data-testid="onboarding-products-step">
      <h2 className="catalog-form-section__title m-0">{t("onboarding.products.title")}</h2>
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("onboarding.products.lede")}</p>

      {templatesQuery.isPending ? (
        <LoadingSkeleton label={t("onboarding.loading")} />
      ) : templates.length === 0 ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("onboarding.products.empty")}</p>
      ) : (
        <ul className="m-0 grid list-none gap-2 p-0">
          {templates.map((template) => {
            const selected = selectedId === template.id;
            return (
              <li key={template.id}>
                <button
                  type="button"
                  className={cn(
                    "flex w-full min-h-11 items-start justify-between gap-3 rounded-[var(--exits-radius-md)] border bg-surface p-3 text-left",
                    selected ? "border-[var(--exits-primary)]" : "border-border",
                  )}
                  aria-selected={selected}
                  data-testid={`onboarding-template-${template.id}`}
                  onClick={() => setSelectedId(template.id)}
                >
                  <span className="min-w-0">
                    <span className="block font-semibold text-[length:var(--exits-text-sm)]">
                      {template.name}
                    </span>
                    <span className="mt-1 block text-[length:var(--exits-text-xs)] text-muted">
                      {t("onboarding.products.productCount").replace(
                        "{count}",
                        String(template.productCount),
                      )}
                    </span>
                    {template.description ? (
                      <span className="mt-1 block text-[length:var(--exits-text-xs)] text-muted">
                        {template.description}
                      </span>
                    ) : null}
                  </span>
                  <Package className="size-4 shrink-0 text-muted" aria-hidden />
                </button>
              </li>
            );
          })}
        </ul>
      )}

      {previewQuery.data ? (
        <div className="rounded-[var(--exits-radius-md)] border border-border p-3" data-testid="onboarding-template-preview">
          <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {t("onboarding.products.previewTitle")}
          </p>
          <ul className="mb-0 mt-2 grid list-none gap-1 p-0">
            {previewQuery.data.products.slice(0, 6).map((product) => (
              <li key={product.id} className="text-[length:var(--exits-text-xs)] text-muted">
                {product.productName ?? product.sku ?? product.globalProductId}
              </li>
            ))}
          </ul>
        </div>
      ) : null}

      {error ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]" role="alert">
          {error}
        </p>
      ) : null}

      <Button
        type="button"
        className="min-h-11 w-full"
        disabled={!selectedId || importMutation.isPending}
        data-testid="onboarding-products-import"
        onClick={() => void importMutation.mutateAsync()}
      >
        {importMutation.isPending ? <Loader2 className="size-4 animate-spin" /> : null}
        {t("onboarding.products.addStarter")}
      </Button>
      <Button
        type="button"
        variant="ghost"
        className="min-h-11 w-full"
        disabled={skipMutation.isPending}
        data-testid="onboarding-products-empty"
        onClick={() => void skipMutation.mutateAsync()}
      >
        <SkipForward className="size-4" aria-hidden />
        {t("onboarding.products.startEmpty")}
      </Button>
    </section>
  );
}

function ReadyStep({
  workspace,
  progress,
  organizationId,
  bindDestination,
  workspaces,
  onDone,
  onFinishLater,
}: {
  workspace: { organizationId: string; branchId?: string | null };
  progress: OrganizationOnboardingProgressDto;
  organizationId: string;
  bindDestination: (destination: {
    organizationId: string;
    organizationDisplayName: string;
    branchId: string | null;
    branchName: string | null;
    experience: "start_selling";
    route: string;
    labelKey: "experience.startSelling";
  }) => Promise<boolean>;
  workspaces: Array<{
    organizationId: string;
    displayName: string;
    branches: Array<{ branchId: string; name: string }>;
  }>;
  onDone: () => void;
  onFinishLater: () => void;
}) {
  const { t } = useI18n();
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  const orgName =
    workspaces.find((item) => item.organizationId === organizationId)?.displayName ??
    t("onboarding.ready.businessFallback");

  async function startSelling() {
    if (busy) return;
    setBusy(true);
    setActionError(null);
    try {
      try {
        await updateOnboardingProgress(workspace, { overallStatus: "Completed" });
      } catch {
        // Already completed / transient failure — still enter Sell.
      }

      const ws = workspaces.find((item) => item.organizationId === organizationId);
      const branch = ws?.branches[0] ?? null;
      // Prefer a selling bind when a branch exists, but never block leaving the wizard
      // if org-context/bind fails (common on first-run Local Validation).
      if (ws && branch) {
        try {
          await bindDestination({
            organizationId,
            organizationDisplayName: ws.displayName,
            branchId: branch.branchId,
            branchName: branch.name,
            experience: "start_selling",
            route: "/sell",
            labelKey: "experience.startSelling",
          });
        } catch {
          // Keep current org bind and continue.
        }
      }
      onDone();
    } catch {
      setActionError(t("onboarding.ready.actionFailed"));
    } finally {
      setBusy(false);
    }
  }

  async function finishLater() {
    if (busy) return;
    setBusy(true);
    setActionError(null);
    try {
      if (progress.overallStatus !== "Completed") {
        try {
          await updateOnboardingProgress(workspace, { overallStatus: "FinishedLater" });
        } catch {
          // Still leave the wizard — setup remains available from More.
        }
      }
      onFinishLater();
    } catch {
      setActionError(t("onboarding.ready.actionFailed"));
    } finally {
      setBusy(false);
    }
  }

  function stepLine(status: string, doneKey: MessageKey, skipKey: MessageKey): string {
    if (status === "Completed") return t(doneKey);
    if (status === "Skipped") return t(skipKey);
    return t(skipKey);
  }

  return (
    <section className="catalog-form-section flex flex-col gap-3" data-testid="onboarding-ready-step">
      <h2 className="catalog-form-section__title m-0">{t("onboarding.ready.title")}</h2>
      <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">{orgName}</p>
      <ul className="m-0 grid list-none gap-2 p-0 text-[length:var(--exits-text-sm)]">
        <li className="flex items-center gap-2">
          <Check className="size-4 text-[var(--exits-primary)]" aria-hidden />
          {t("onboarding.ready.businessCreated")}
        </li>
        <li className="flex items-center gap-2">
          <Check className="size-4 text-[var(--exits-primary)]" aria-hidden />
          {stepLine(
            progress.organizationSetupStatus,
            "onboarding.ready.orgSaved",
            "onboarding.ready.orgSkipped",
          )}
        </li>
        <li className="flex items-center gap-2">
          <Check className="size-4 text-[var(--exits-primary)]" aria-hidden />
          {stepLine(
            progress.businessSetupStatus,
            "onboarding.ready.businessApplied",
            "onboarding.ready.businessSkipped",
          )}
        </li>
        <li className="flex items-center gap-2">
          <Check className="size-4 text-[var(--exits-primary)]" aria-hidden />
          {stepLine(
            progress.productTemplateStatus,
            "onboarding.ready.productsAdded",
            "onboarding.ready.productsEmpty",
          )}
        </li>
      </ul>

      {actionError ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]" role="alert">
          {actionError}
        </p>
      ) : null}

      <div
        className="sticky bottom-0 z-40 -mx-1 mt-1 flex flex-col gap-2 border-t border-border bg-surface px-1 pt-3 pb-[max(0.75rem,env(safe-area-inset-bottom))]"
        data-testid="onboarding-ready-actions"
      >
        <Button
          type="button"
          className="min-h-11 w-full"
          disabled={busy}
          data-testid="onboarding-start-selling"
          onClick={() => void startSelling()}
        >
          {busy ? <Loader2 className="size-4 animate-spin" aria-hidden /> : null}
          {t("onboarding.ready.startSelling")}
          <ChevronRight className="size-4" aria-hidden />
        </Button>
        <Button
          type="button"
          variant="ghost"
          className="min-h-11 w-full"
          disabled={busy}
          data-testid="onboarding-finish-later"
          onClick={() => void finishLater()}
        >
          {t("onboarding.ready.finishLater")}
        </Button>
        <Link
          to="/catalog/products"
          className="inline-flex min-h-11 items-center justify-center text-[length:var(--exits-text-sm)] font-semibold text-primary no-underline"
          data-testid="onboarding-add-products"
        >
          {t("onboarding.ready.addProducts")}
        </Link>
      </div>
    </section>
  );
}
