import { useEffect, useMemo, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  Archive,
  CheckCircle2,
  FilePenLine,
  Loader2,
  PlayCircle,
  Power,
  PowerOff,
  Save,
  Send,
} from "lucide-react";
import { useForm, useWatch } from "react-hook-form";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import type { CatalogFeatureDefinition } from "@/api/catalog/feature-catalog-types";
import { featureSupportsNumericLimit } from "@/api/catalog/feature-catalog-types";
import type { CatalogFeatureGrant, CatalogPlan, CatalogPlanVersion } from "@/api/catalog/plan-catalog-types";
import { COMMERCIAL_BACKEND_GAPS } from "@/api/commercial/commercial-backend-gaps";
import { classifyCommercialMutationFailure } from "@/api/commercial/commercial-errors";
import { Alert } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  useCatalogPlanVersionsQuery,
  useCatalogProductFeaturesQuery,
} from "@/features/catalog/use-catalog-detail-queries";
import {
  useActivatePlanMutation,
  useCreateDraftPlanVersionMutation,
  useDeactivatePlanMutation,
  usePublishPlanVersionMutation,
  useRenamePlanMutation,
  useRetirePlanMutation,
  useUpdatePlanMutation,
  useUpsertDraftFeatureGrantMutation,
} from "@/features/commercial/use-commercial-mutations";
import {
  commercialValuesToBody,
  nextDraftVersionNumber,
  ORDERING_DELIVERY_FEATURE_CODES,
  planLifecycleActions,
  planToCommercialValues,
} from "@/features/plans/plan-commercial-mapping";
import { planMutationFailureCopy } from "@/features/plans/plan-mutation-feedback";
import {
  MAX_MAX_AREAS,
  MIN_MAX_AREAS,
  planCommercialSchema,
  planRenameSchema,
  type PlanCommercialValues,
  type PlanRenameValues,
} from "@/features/plans/plan-commercial-schema";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import type { MessageKey } from "@/lib/i18n/messages";

type Feedback = { tone: "info" | "danger"; title: string; detail: string };

type LifecycleConfirm = "activate" | "deactivate" | "retire";

const VERSION_STATUS_LABELS: Record<string, MessageKey> = {
  Draft: "plans.editor.versions.status.Draft",
  Published: "plans.editor.versions.status.Published",
  Retired: "plans.editor.versions.status.Retired",
};

function VersionStatusIcon({ status }: { status: string }) {
  if (status === "Published") {
    return <CheckCircle2 aria-hidden className="size-4 text-success" />;
  }
  if (status === "Draft") {
    return <FilePenLine aria-hidden className="size-4 text-warning" />;
  }
  if (status === "Retired") {
    return <Archive aria-hidden className="size-4 text-danger" />;
  }
  return null;
}

function grantForFeature(
  version: CatalogPlanVersion | undefined,
  featureCode: string,
): CatalogFeatureGrant | undefined {
  return version?.grants.find((grant) => grant.featureCode === featureCode);
}

function activeProductFeatures(features: CatalogFeatureDefinition[] | undefined) {
  return (features ?? []).filter((feature) => feature.status === "Active");
}

function BooleanSelect({
  id,
  label,
  value,
  disabled,
  onChange,
}: {
  id: string;
  label: string;
  value: boolean;
  disabled?: boolean;
  onChange: (value: boolean) => void;
}) {
  const { t } = usePreferences();
  return (
    <div className="grid gap-1">
      <Label htmlFor={id}>{label}</Label>
      <select
        id={id}
        className="h-9 rounded-md border border-input bg-background px-3 text-[length:var(--exits-text-sm)]"
        disabled={disabled}
        value={value ? "true" : "false"}
        onChange={(event) => onChange(event.target.value === "true")}
      >
        <option value="true">{t("plans.editor.boolean.yes")}</option>
        <option value="false">{t("plans.editor.boolean.no")}</option>
      </select>
    </div>
  );
}

export function PlanCommercialOperator({ plan }: { plan: CatalogPlan }) {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const canManage = authorization.hasPermission(PLATFORM_PERMISSIONS.manageCatalog);
  const [feedback, setFeedback] = useState<Feedback | null>(null);
  const [lifecycleConfirm, setLifecycleConfirm] = useState<LifecycleConfirm | null>(null);
  const [selectedDraftVersion, setSelectedDraftVersion] = useState<number | null>(null);
  const [grantDraftOverrides, setGrantDraftOverrides] = useState<
    Record<string, { enabled: boolean; numericLimit: string }>
  >({});

  const versionsQuery = useCatalogPlanVersionsQuery(plan.productCode, plan.id);
  const featuresQuery = useCatalogProductFeaturesQuery(plan.productCode, true);

  const renameMutation = useRenamePlanMutation();
  const updateMutation = useUpdatePlanMutation();
  const activateMutation = useActivatePlanMutation();
  const deactivateMutation = useDeactivatePlanMutation();
  const retireMutation = useRetirePlanMutation();
  const createDraftMutation = useCreateDraftPlanVersionMutation();
  const publishMutation = usePublishPlanVersionMutation();
  const upsertGrantMutation = useUpsertDraftFeatureGrantMutation();

  const renameForm = useForm<PlanRenameValues>({
    resolver: async (values, context, options) =>
      zodResolver(planRenameSchema)(values, context, options),
    defaultValues: { displayName: plan.displayName },
  });

  const commercialForm = useForm<PlanCommercialValues>({
    resolver: async (values, context, options) =>
      zodResolver(planCommercialSchema)(values, context, options),
    defaultValues: planToCommercialValues(plan),
  });

  useEffect(() => {
    renameForm.reset({ displayName: plan.displayName });
    commercialForm.reset(planToCommercialValues(plan));
  }, [plan, renameForm, commercialForm]);

  const lifecycle = planLifecycleActions(plan.status);
  const productFeatures = useMemo(
    () => activeProductFeatures(featuresQuery.data),
    [featuresQuery.data],
  );
  const draftVersions = useMemo(
    () => (versionsQuery.data ?? []).filter((version) => version.status === "Draft"),
    [versionsQuery.data],
  );
  const effectiveDraftVersion =
    selectedDraftVersion ?? (draftVersions[0]?.versionNumber ?? null);
  const selectedDraft = useMemo(
    () => draftVersions.find((version) => version.versionNumber === effectiveDraftVersion),
    [draftVersions, effectiveDraftVersion],
  );

  const grantDrafts = useMemo(() => {
    if (!selectedDraft) {
      return {};
    }
    const next: Record<string, { enabled: boolean; numericLimit: string }> = {};
    for (const feature of productFeatures) {
      const override = grantDraftOverrides[feature.featureCode];
      if (override) {
        next[feature.featureCode] = override;
        continue;
      }
      const grant = grantForFeature(selectedDraft, feature.featureCode);
      next[feature.featureCode] = {
        enabled: grant?.enabled ?? false,
        numericLimit: grant?.numericLimit != null ? String(grant.numericLimit) : "",
      };
    }
    return next;
  }, [grantDraftOverrides, productFeatures, selectedDraft]);

  const orderingDeliverySummary = useMemo(() => {
    const published = (versionsQuery.data ?? []).find((version) => version.status === "Published");
    const source = selectedDraft ?? published;
    return ORDERING_DELIVERY_FEATURE_CODES.map((featureCode) => {
      const definition = productFeatures.find((feature) => feature.featureCode === featureCode);
      const grant = grantForFeature(source, featureCode);
      return {
        featureCode,
        displayName: definition?.displayName ?? featureCode,
        defined: Boolean(definition),
        enabled: grant?.enabled ?? false,
      };
    });
  }, [productFeatures, selectedDraft, versionsQuery.data]);

  const mutationBusy =
    renameMutation.isPending ||
    updateMutation.isPending ||
    activateMutation.isPending ||
    deactivateMutation.isPending ||
    retireMutation.isPending ||
    createDraftMutation.isPending ||
    publishMutation.isPending ||
    upsertGrantMutation.isPending;

  function showSuccess(messageKey: MessageKey) {
    setFeedback({
      tone: "info",
      title: t("plans.mutation.success.title"),
      detail: t(messageKey),
    });
  }

  function showFailure(error: unknown) {
    const copy = planMutationFailureCopy(error, t);
    setFeedback({ tone: "danger", title: copy.title, detail: copy.detail });
  }

  function handleMutationFailure(error: unknown) {
    showFailure(error);
    if (classifyCommercialMutationFailure(error).kind === "conflict") {
      renameForm.reset({ displayName: plan.displayName });
      commercialForm.reset(planToCommercialValues(plan));
    }
  }

  async function saveRename(values: PlanRenameValues) {
    if (!canManage) return;
    setFeedback(null);
    try {
      await renameMutation.mutateAsync({
        productCode: plan.productCode,
        planId: plan.id,
        body: {
          displayName: values.displayName,
          expectedUpdatedAtUtc: plan.updatedAtUtc ?? null,
        },
      });
      showSuccess("plans.mutation.success.rename");
    } catch (error) {
      handleMutationFailure(error);
    }
  }

  async function saveCommercial(values: PlanCommercialValues) {
    if (!canManage) return;
    setFeedback(null);
    try {
      await updateMutation.mutateAsync({
        productCode: plan.productCode,
        planId: plan.id,
        body: commercialValuesToBody(values, plan.updatedAtUtc),
      });
      showSuccess("plans.mutation.success.commercial");
    } catch (error) {
      handleMutationFailure(error);
    }
  }

  async function runLifecycle(action: LifecycleConfirm) {
    if (!canManage) return;
    setFeedback(null);
    try {
      const input = { productCode: plan.productCode, planId: plan.id };
      if (action === "activate") {
        await activateMutation.mutateAsync(input);
        showSuccess("plans.mutation.success.activate");
      } else if (action === "deactivate") {
        await deactivateMutation.mutateAsync(input);
        showSuccess("plans.mutation.success.deactivate");
      } else {
        await retireMutation.mutateAsync(input);
        showSuccess("plans.mutation.success.retire");
      }
    } catch (error) {
      showFailure(error);
    } finally {
      setLifecycleConfirm(null);
    }
  }

  async function createDraftVersion() {
    if (!canManage) return;
    setFeedback(null);
    const versionNumber = nextDraftVersionNumber(versionsQuery.data);
    try {
      const version = await createDraftMutation.mutateAsync({
        productCode: plan.productCode,
        planId: plan.id,
        body: {
          versionNumber,
          billingPeriod: "Monthly",
          trialEligible: plan.trialAllowed ?? false,
          grants: [],
        },
      });
      setSelectedDraftVersion(version.versionNumber);
      setGrantDraftOverrides({});
      showSuccess("plans.mutation.success.draft");
    } catch (error) {
      showFailure(error);
    }
  }

  async function publishDraft(versionNumber: number) {
    if (!canManage) return;
    setFeedback(null);
    try {
      await publishMutation.mutateAsync({
        productCode: plan.productCode,
        planId: plan.id,
        versionNumber,
      });
      showSuccess("plans.mutation.success.publish");
    } catch (error) {
      showFailure(error);
    }
  }

  async function saveFeatureGrant(featureCode: string) {
    if (!canManage || effectiveDraftVersion == null) return;
    const draft = grantDrafts[featureCode];
    const definition = productFeatures.find((feature) => feature.featureCode === featureCode);
    if (!definition || !draft) return;
    setFeedback(null);
    try {
      const numericLimit =
        featureSupportsNumericLimit(definition.valueType) && draft.enabled && draft.numericLimit
          ? Number.parseInt(draft.numericLimit, 10)
          : null;
      await upsertGrantMutation.mutateAsync({
        productCode: plan.productCode,
        planId: plan.id,
        versionNumber: effectiveDraftVersion,
        body: {
          featureCode,
          enabled: draft.enabled,
          numericLimit,
        },
      });
      showSuccess("plans.mutation.success.grant");
      setGrantDraftOverrides({});
    } catch (error) {
      showFailure(error);
    }
  }

  const customerCreditEnabled =
    useWatch({ control: commercialForm.control, name: "customerCreditEnabled" }) ?? false;
  const advancedReportsEnabled =
    useWatch({ control: commercialForm.control, name: "advancedReportsEnabled" }) ?? false;
  const exportEnabled =
    useWatch({ control: commercialForm.control, name: "exportEnabled" }) ?? false;
  const trialAllowed = useWatch({ control: commercialForm.control, name: "trialAllowed" }) ?? false;

  return (
    <div className="grid gap-4">
      {feedback ? (
        <Alert title={feedback.title} tone={feedback.tone === "danger" ? "danger" : "info"}>
          {feedback.detail}
        </Alert>
      ) : null}

      {!canManage ? (
        <Alert title={t("plans.editor.readOnly.title")}>{t("plans.editor.readOnly.body")}</Alert>
      ) : null}

      <DashboardSection title={t("plans.editor.identity.title")}>
        <form className="grid gap-3" onSubmit={renameForm.handleSubmit(saveRename)}>
          <dl className="grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2">
            <div>
              <dt className="text-[length:var(--exits-text-xs)] text-muted">{t("plans.column.code")}</dt>
              <dd>{plan.code}</dd>
            </div>
            <div>
              <dt className="text-[length:var(--exits-text-xs)] text-muted">{t("plans.column.status")}</dt>
              <dd>
                <StatusIndicator tone={plan.status === "Active" ? "success" : plan.status === "Retired" ? "danger" : "warning"} label={plan.status} />
              </dd>
            </div>
          </dl>
          <div className="grid gap-1">
            <Label htmlFor="plan-display-name">{t("plans.column.displayName")}</Label>
            <Input
              id="plan-display-name"
              disabled={!canManage || mutationBusy}
              {...renameForm.register("displayName")}
            />
            {renameForm.formState.errors.displayName ? (
              <p className="text-[length:var(--exits-text-xs)] text-danger">
                {renameForm.formState.errors.displayName.message}
              </p>
            ) : null}
          </div>
          {canManage ? (
            <div>
              <Button type="submit" disabled={mutationBusy || !renameForm.formState.isDirty}>
                {renameMutation.isPending ? (
                  <Loader2 aria-hidden className="mr-2 size-4 animate-spin" />
                ) : (
                  <Save aria-hidden className="mr-2 size-4" />
                )}
                {t("plans.editor.rename.save")}
              </Button>
            </div>
          ) : null}
        </form>
      </DashboardSection>

      <DashboardSection
        title={t("plans.detail.pricing")}
        description={t("plans.editor.pricing.hint")}
      >
        <form className="grid gap-3" onSubmit={commercialForm.handleSubmit(saveCommercial)}>
          <div className="grid gap-3 sm:grid-cols-2">
            <div className="grid gap-1">
              <Label htmlFor="monthly-price">{t("plans.column.monthlyPrice")}</Label>
              <Input
                id="monthly-price"
                type="number"
                min={0}
                step="0.01"
                disabled={!canManage || mutationBusy}
                {...commercialForm.register("monthlyPrice", { valueAsNumber: true })}
              />
            </div>
            <div className="grid gap-1">
              <Label htmlFor="annual-price">{t("plans.detail.field.annualPrice")}</Label>
              <Input
                id="annual-price"
                type="number"
                min={0}
                step="0.01"
                disabled={!canManage || mutationBusy}
                {...commercialForm.register("annualPrice", { valueAsNumber: true })}
              />
            </div>
            <div className="grid gap-1">
              <Label htmlFor="currency-code">{t("plans.detail.field.currency")}</Label>
              <Input
                id="currency-code"
                maxLength={3}
                disabled={!canManage || mutationBusy}
                {...commercialForm.register("currencyCode")}
              />
            </div>
            <div className="grid gap-1 sm:col-span-2">
              <Label htmlFor="plan-description">{t("plans.detail.field.description")}</Label>
              <Input
                id="plan-description"
                disabled={!canManage || mutationBusy}
                {...commercialForm.register("description")}
              />
            </div>
          </div>
          {canManage ? (
            <p className="text-[length:var(--exits-text-xs)] text-muted">{t("plans.editor.saveCommercialHint")}</p>
          ) : null}
        </form>
      </DashboardSection>

      <DashboardSection title={t("plans.detail.limits")}>
        <div className="grid gap-3 sm:grid-cols-2">
          {(
            [
              ["maxBranches", "plans.detail.field.maxBranches", 0, undefined],
              ["maxActiveStaff", "plans.detail.field.maxActiveStaff", 0, undefined],
              ["maxActivePosDevices", "plans.detail.field.maxActivePosDevices", 0, undefined],
              ["maxActiveBusinessTypes", "plans.detail.field.maxActiveBusinessTypes", 0, undefined],
              ["maxAreas", "plans.detail.field.maxAreas", MIN_MAX_AREAS, MAX_MAX_AREAS],
            ] as const
          ).map(([field, labelKey, min, max]) => (
            <div key={field} className="grid gap-1">
              <Label htmlFor={field}>{t(labelKey)}</Label>
              <Input
                id={field}
                type="number"
                min={min}
                max={max}
                step={1}
                disabled={!canManage || mutationBusy}
                {...commercialForm.register(field, { valueAsNumber: true })}
              />
              {commercialForm.formState.errors[field] ? (
                <p className="text-[length:var(--exits-text-xs)] text-danger">
                  {commercialForm.formState.errors[field]?.message}
                </p>
              ) : null}
            </div>
          ))}
        </div>
      </DashboardSection>

      <DashboardSection title={t("plans.detail.features")}>
        <div className="grid gap-3 sm:grid-cols-2">
          <BooleanSelect
            id="customer-credit-enabled"
            label={t("plans.detail.field.customerCreditEnabled")}
            value={customerCreditEnabled}
            disabled={!canManage || mutationBusy}
            onChange={(value) =>
              commercialForm.setValue("customerCreditEnabled", value, { shouldDirty: true })
            }
          />
          <BooleanSelect
            id="advanced-reports-enabled"
            label={t("plans.detail.field.advancedReportsEnabled")}
            value={advancedReportsEnabled}
            disabled={!canManage || mutationBusy}
            onChange={(value) =>
              commercialForm.setValue("advancedReportsEnabled", value, { shouldDirty: true })
            }
          />
          <BooleanSelect
            id="export-enabled"
            label={t("plans.detail.field.exportEnabled")}
            value={exportEnabled}
            disabled={!canManage || mutationBusy}
            onChange={(value) =>
              commercialForm.setValue("exportEnabled", value, { shouldDirty: true })
            }
          />
        </div>
      </DashboardSection>

      <DashboardSection title={t("plans.editor.trial.title")}>
        <div className="grid gap-3 sm:grid-cols-2">
          <BooleanSelect
            id="trial-allowed"
            label={t("plans.detail.field.trialAllowed")}
            value={trialAllowed}
            disabled={!canManage || mutationBusy}
            onChange={(value) =>
              commercialForm.setValue("trialAllowed", value, { shouldDirty: true })
            }
          />
          <div className="grid gap-1">
            <Label htmlFor="default-trial-days">{t("plans.detail.field.defaultTrialDays")}</Label>
            <Input
              id="default-trial-days"
              type="number"
              min={0}
              step={1}
              disabled={!canManage || mutationBusy || !trialAllowed}
              {...commercialForm.register("defaultTrialDays", { valueAsNumber: true })}
            />
          </div>
        </div>
        <p className="mt-2 text-[length:var(--exits-text-xs)] text-muted">
          {t("plans.editor.trial.hint")}
        </p>
      </DashboardSection>

      {canManage ? (
        <div>
          <Button
            type="button"
            disabled={mutationBusy || !commercialForm.formState.isDirty}
            onClick={() => void commercialForm.handleSubmit(saveCommercial)()}
          >
            {updateMutation.isPending ? (
              <Loader2 aria-hidden className="mr-2 size-4 animate-spin" />
            ) : (
              <Save aria-hidden className="mr-2 size-4" />
            )}
            {t("plans.editor.commercial.save")}
          </Button>
        </div>
      ) : null}

      <DashboardSection title={t("plans.editor.lifecycle.title")}>
        {plan.status === "Retired" ? (
          <p className="text-[length:var(--exits-text-sm)] text-muted">{t("plans.editor.lifecycle.retired")}</p>
        ) : (
          <div className="flex flex-wrap gap-2">
            {lifecycle.canActivate ? (
              <Button
                type="button"
                variant="secondary"
                disabled={!canManage || mutationBusy}
                onClick={() => setLifecycleConfirm("activate")}
              >
                <PlayCircle aria-hidden className="mr-2 size-4" />
                {t("plans.editor.lifecycle.activate")}
              </Button>
            ) : null}
            {lifecycle.canDeactivate ? (
              <Button
                type="button"
                variant="secondary"
                disabled={!canManage || mutationBusy}
                onClick={() => setLifecycleConfirm("deactivate")}
              >
                <PowerOff aria-hidden className="mr-2 size-4" />
                {t("plans.editor.lifecycle.deactivate")}
              </Button>
            ) : null}
            {lifecycle.canRetire ? (
              <Button
                type="button"
                variant="destructive"
                disabled={!canManage || mutationBusy}
                onClick={() => setLifecycleConfirm("retire")}
              >
                <Power aria-hidden className="mr-2 size-4" />
                {t("plans.editor.lifecycle.retire")}
              </Button>
            ) : null}
          </div>
        )}
      </DashboardSection>

      <DashboardSection
        title={t("plans.editor.versions.title")}
        description={t("plans.editor.versions.hint")}
      >
        {versionsQuery.isPending ? (
          <p className="text-[length:var(--exits-text-sm)] text-muted">{t("plans.editor.versions.loading")}</p>
        ) : null}
        {versionsQuery.isError ? (
          <p className="text-[length:var(--exits-text-sm)] text-danger">{t("plans.editor.versions.error")}</p>
        ) : null}
        {(versionsQuery.data ?? []).length === 0 ? (
          <p className="text-[length:var(--exits-text-sm)] text-muted">{t("plans.editor.versions.empty")}</p>
        ) : (
          <ul className="grid gap-2">
            {(versionsQuery.data ?? []).map((version) => (
              <li
                key={version.id}
                className="flex flex-wrap items-center justify-between gap-2 rounded-md border border-border px-3 py-2"
              >
                <div className="flex items-center gap-2">
                  <VersionStatusIcon status={version.status} />
                  <span className="font-medium">
                    {t("plans.editor.versions.item").replace("{number}", String(version.versionNumber))}
                  </span>
                  <Badge tone="neutral">
                    {VERSION_STATUS_LABELS[version.status]
                      ? t(VERSION_STATUS_LABELS[version.status]!)
                      : version.status}
                  </Badge>
                </div>
                {version.status === "Draft" && canManage ? (
                  <Button
                    type="button"
                    size="sm"
                    disabled={mutationBusy}
                    onClick={() => void publishDraft(version.versionNumber)}
                  >
                    <Send aria-hidden className="mr-2 size-4" />
                    {t("plans.editor.versions.publish")}
                  </Button>
                ) : null}
              </li>
            ))}
          </ul>
        )}
        {canManage ? (
          <div className="mt-3">
            <Button type="button" variant="secondary" disabled={mutationBusy} onClick={() => void createDraftVersion()}>
              <FilePenLine aria-hidden className="mr-2 size-4" />
              {t("plans.editor.versions.createDraft")}
            </Button>
          </div>
        ) : null}
        <p className="mt-3 text-[length:var(--exits-text-xs)] text-muted">
          {COMMERCIAL_BACKEND_GAPS.planVersionRetireHttp.notes}
        </p>
      </DashboardSection>

      <DashboardSection
        title={t("plans.editor.grants.title")}
        description={t("plans.editor.grants.hint")}
      >
        {featuresQuery.isPending ? (
          <p className="text-[length:var(--exits-text-sm)] text-muted">{t("plans.editor.grants.loadingFeatures")}</p>
        ) : null}
        {productFeatures.length === 0 && !featuresQuery.isPending ? (
          <p className="text-[length:var(--exits-text-sm)] text-muted">{t("plans.editor.grants.noFeatures")}</p>
        ) : null}
        {draftVersions.length === 0 ? (
          <p className="text-[length:var(--exits-text-sm)] text-muted">{t("plans.editor.grants.noDraft")}</p>
        ) : (
          <div className="grid gap-3">
            <div className="grid gap-1">
              <Label htmlFor="draft-version-select">{t("plans.editor.grants.draftSelect")}</Label>
              <select
                id="draft-version-select"
                className="h-9 rounded-md border border-input bg-background px-3 text-[length:var(--exits-text-sm)]"
                value={effectiveDraftVersion ?? ""}
                disabled={!canManage}
                onChange={(event) => {
                  setSelectedDraftVersion(Number.parseInt(event.target.value, 10));
                  setGrantDraftOverrides({});
                }}
              >
                {draftVersions.map((version) => (
                  <option key={version.id} value={version.versionNumber}>
                    {t("plans.editor.versions.item").replace("{number}", String(version.versionNumber))}
                  </option>
                ))}
              </select>
            </div>
            <ul className="grid gap-3">
              {productFeatures.map((feature) => {
                const draft = grantDrafts[feature.featureCode] ?? { enabled: false, numericLimit: "" };
                return (
                  <li
                    key={feature.featureCode}
                    className="grid gap-2 rounded-md border border-border p-3 sm:grid-cols-[1fr_auto_auto]"
                  >
                    <div>
                      <p className="font-medium">{feature.displayName}</p>
                      <p className="text-[length:var(--exits-text-xs)] text-muted">{feature.featureCode}</p>
                      <p className="text-[length:var(--exits-text-xs)] text-muted">{feature.valueType}</p>
                    </div>
                    <BooleanSelect
                      id={`grant-enabled-${feature.featureCode}`}
                      label={t("plans.editor.grants.enabled")}
                      value={draft.enabled}
                      disabled={!canManage || mutationBusy || !selectedDraft}
                      onChange={(value) =>
                        setGrantDraftOverrides((current) => ({
                          ...current,
                          [feature.featureCode]: { ...draft, enabled: value },
                        }))
                      }
                    />
                    {featureSupportsNumericLimit(feature.valueType) ? (
                      <div className="grid gap-1">
                        <Label htmlFor={`grant-limit-${feature.featureCode}`}>
                          {t("plans.editor.grants.numericLimit")}
                        </Label>
                        <Input
                          id={`grant-limit-${feature.featureCode}`}
                          type="number"
                          min={0}
                          disabled={!canManage || mutationBusy || !draft.enabled}
                          value={draft.numericLimit}
                          onChange={(event) =>
                            setGrantDraftOverrides((current) => ({
                              ...current,
                              [feature.featureCode]: { ...draft, numericLimit: event.target.value },
                            }))
                          }
                        />
                      </div>
                    ) : null}
                    {canManage && selectedDraft ? (
                      <div className="flex items-end">
                        <Button
                          type="button"
                          size="sm"
                          disabled={mutationBusy}
                          onClick={() => void saveFeatureGrant(feature.featureCode)}
                        >
                          {t("plans.editor.grants.save")}
                        </Button>
                      </div>
                    ) : null}
                  </li>
                );
              })}
            </ul>
          </div>
        )}
        <p className="mt-3 text-[length:var(--exits-text-xs)] text-muted">
          {COMMERCIAL_BACKEND_GAPS.draftBusinessTypeGrants.notes}
        </p>
      </DashboardSection>

      <DashboardSection title={t("plans.editor.orderingDelivery.title")}>
        <ul className="grid gap-2 text-[length:var(--exits-text-sm)]">
          {orderingDeliverySummary.map((item) => (
            <li key={item.featureCode} className="flex flex-wrap items-center justify-between gap-2">
              <span>{item.displayName}</span>
              {!item.defined ? (
                <Badge tone="neutral">{t("plans.editor.orderingDelivery.notDefined")}</Badge>
              ) : (
                <Badge tone={item.enabled ? "success" : "neutral"}>
                  {item.enabled ? t("plans.editor.boolean.yes") : t("plans.editor.boolean.no")}
                </Badge>
              )}
            </li>
          ))}
        </ul>
        <p className="mt-2 text-[length:var(--exits-text-xs)] text-muted">
          {t("plans.editor.orderingDelivery.hint")}
        </p>
      </DashboardSection>

      <ConfirmActionDialog
        open={lifecycleConfirm != null}
        title={
          lifecycleConfirm === "activate"
            ? t("plans.editor.lifecycle.confirmActivate.title")
            : lifecycleConfirm === "deactivate"
              ? t("plans.editor.lifecycle.confirmDeactivate.title")
              : t("plans.editor.lifecycle.confirmRetire.title")
        }
        description={
          lifecycleConfirm === "activate"
            ? t("plans.editor.lifecycle.confirmActivate.body")
            : lifecycleConfirm === "deactivate"
              ? t("plans.editor.lifecycle.confirmDeactivate.body")
              : t("plans.editor.lifecycle.confirmRetire.body")
        }
        confirmLabel={t("plans.editor.lifecycle.confirmAction")}
        cancelLabel={t("plans.editor.dialog.cancel")}
        pendingLabel={t("plans.editor.dialog.pending")}
        pending={mutationBusy}
        destructive={lifecycleConfirm === "retire"}
        onConfirm={() => lifecycleConfirm && void runLifecycle(lifecycleConfirm)}
        onCancel={() => setLifecycleConfirm(null)}
      />
    </div>
  );
}
