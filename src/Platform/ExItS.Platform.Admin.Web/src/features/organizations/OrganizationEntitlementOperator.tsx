import { useMemo, useState } from "react";
import { Plus, RefreshCw } from "lucide-react";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import type { EntitlementGrant, EntitlementSnapshot, FeatureOverride } from "@/api/organizations/entitlement-list-query";
import { featureSupportsNumericLimit } from "@/api/catalog/feature-catalog-types";
import { PlatformApiError } from "@/api/platform-http";
import { AdminTable } from "@/components/exits/AdminTable";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { ErrorState } from "@/components/exits/ErrorState";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { useCatalogProductFeaturesQuery } from "@/features/catalog/use-catalog-detail-queries";
import {
  useCreateFeatureOverrideMutation,
  useGenerateEntitlementSnapshotMutation,
  useReconcileEntitlementSnapshotMutation,
  useRevokeFeatureOverrideMutation,
} from "@/features/commercial/use-commercial-mutations";
import { commercialMutationFailureCopy } from "@/features/organizations/commercial-mutation-feedback";
import {
  organizationSubscriptionStatusLabel,
  organizationSubscriptionStatusTone,
} from "@/features/organizations/organization-subscription-status";
import {
  useOrganizationFeatureOverridesQuery,
  useOrganizationLatestEntitlementQuery,
} from "@/features/organizations/use-organization-workspace-queries";
import { useAuthorization } from "@/hooks/use-authorization";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";
import {
  grantSourceLabel,
  overrideEffectiveStatus,
} from "@/features/organizations/entitlement-operator-utils";

const controlClass =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";

function formatInstant(value: string | undefined, language: string): string | null {
  if (!value) {
    return null;
  }
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }
  return new Intl.DateTimeFormat(language === "fil-PH" ? "fil-PH" : "en-GB", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}

function grantCounts(grants: EntitlementGrant[]): { enabled: number; disabled: number } {
  let enabled = 0;
  let disabled = 0;
  for (const grant of grants) {
    if (grant.enabled) {
      enabled += 1;
    } else {
      disabled += 1;
    }
  }
  return { enabled, disabled };
}

function grantSummaryLabel(grants: EntitlementGrant[], t: (key: MessageKey) => string): string {
  const { enabled, disabled } = grantCounts(grants);
  return t("organization.entitlements.grant.summary")
    .replace("{enabled}", String(enabled))
    .replace("{disabled}", String(disabled));
}

function isForbidden(error: unknown): boolean {
  return error instanceof PlatformApiError && (error.status === 401 || error.status === 403);
}

function isNotFound(error: unknown): boolean {
  return error instanceof PlatformApiError && error.status === 404;
}

function EntitlementGrantDetails({
  grants,
  t,
}: {
  grants: EntitlementGrant[];
  t: (key: MessageKey) => string;
}) {
  return (
    <ul className="grid gap-1.5">
      {grants.map((grant) => (
        <li
          key={grant.featureCode}
          className="flex flex-wrap items-baseline gap-x-2 gap-y-0.5 text-[length:var(--exits-text-xs)]"
        >
          <span className="break-all font-mono text-foreground">{grant.featureCode}</span>
          <span className={grant.enabled ? "text-muted" : "font-medium text-foreground"}>
            {grant.enabled
              ? t("organization.entitlements.grant.enabled")
              : t("organization.entitlements.grant.disabled")}
          </span>
          {grant.numericLimit != null ? (
            <span className="text-muted">
              {t("organization.entitlements.grant.limit").replace(
                "{value}",
                String(grant.numericLimit),
              )}
            </span>
          ) : null}
          {grant.source ? (
            <span className="text-muted">{grantSourceLabel(grant.source, t)}</span>
          ) : null}
        </li>
      ))}
    </ul>
  );
}

export function OrganizationEntitlementOperator({
  organizationId,
  productCode,
  organizationName,
}: {
  organizationId: string;
  productCode: string;
  organizationName: string;
}) {
  const { t, language } = usePreferences();
  const authorization = useAuthorization();
  const showTable = useMediaQuery("(min-width: 768px)");
  const canManageSubscriptions = authorization.hasPermission(PLATFORM_PERMISSIONS.manageSubscriptions);
  const canManageOverrides = authorization.hasPermission(
    PLATFORM_PERMISSIONS.manageEntitlementOverrides,
  );

  const latestQuery = useOrganizationLatestEntitlementQuery(organizationId, productCode);
  const [overrideStatus, setOverrideStatus] = useState<"" | "Active" | "Revoked">("");
  const [overridePage, setOverridePage] = useState(1);
  const overridesQuery = useOrganizationFeatureOverridesQuery(organizationId, productCode, {
    status: overrideStatus,
    page: overridePage,
  });
  const featuresQuery = useCatalogProductFeaturesQuery(productCode, canManageOverrides);

  const generateMutation = useGenerateEntitlementSnapshotMutation();
  const reconcileMutation = useReconcileEntitlementSnapshotMutation();
  const createOverrideMutation = useCreateFeatureOverrideMutation();
  const revokeOverrideMutation = useRevokeFeatureOverrideMutation();

  const [currentGrantsExpanded, setCurrentGrantsExpanded] = useState(false);
  const [feedback, setFeedback] = useState<{
    tone: "info" | "danger";
    title: string;
    detail: string;
  } | null>(null);
  const [pendingReconcileHint, setPendingReconcileHint] = useState(false);
  const [confirm, setConfirm] = useState<
    | { kind: "generate"; snapshot: EntitlementSnapshot | null }
    | { kind: "reconcile"; snapshot: EntitlementSnapshot | null }
    | { kind: "revoke"; override: FeatureOverride }
    | null
  >(null);
  const [reconcileReason, setReconcileReason] = useState("");
  const [revokeReason, setRevokeReason] = useState("");
  const [createOpen, setCreateOpen] = useState(false);
  const [createFeatureCode, setCreateFeatureCode] = useState("");
  const [createEnabled, setCreateEnabled] = useState<"true" | "false">("false");
  const [createReason, setCreateReason] = useState("");
  const [createNumericLimit, setCreateNumericLimit] = useState("");
  const [createExpiresLocal, setCreateExpiresLocal] = useState("");

  const activeFeatures = useMemo(
    () => (featuresQuery.data ?? []).filter((feature) => feature.status === "Active"),
    [featuresQuery.data],
  );
  const selectedFeature = activeFeatures.find((feature) => feature.featureCode === createFeatureCode);
  const showNumericLimit = selectedFeature
    ? featureSupportsNumericLimit(selectedFeature.valueType)
    : false;

  const latestSnapshot = latestQuery.data ?? null;
  const expectedNextVersion = (latestSnapshot?.snapshotVersion ?? 0) + 1;

  const latestDiagnostic =
    latestQuery.error && !isNotFound(latestQuery.error) && !isForbidden(latestQuery.error)
      ? normalizeDiagnosticError({
          error: latestQuery.error,
          operation: "Load latest entitlement snapshot",
        })
      : null;
  const overridesDiagnostic = overridesQuery.error
    ? normalizeDiagnosticError({
        error: overridesQuery.error,
        operation: "Load feature overrides",
      })
    : null;

  const overrideTotalPages = overridesQuery.data
    ? Math.max(
        1,
        Math.ceil(overridesQuery.data.totalCount / (overridesQuery.data.pageSize || 20)),
      )
    : 1;

  async function runGenerate() {
    setFeedback(null);
    try {
      await generateMutation.mutateAsync({
        organizationId,
        productCode,
        body: { expectedNextVersion },
      });
      setPendingReconcileHint(false);
      setConfirm(null);
      setFeedback({
        tone: "info",
        title: t("organization.entitlements.generate.success"),
        detail: "",
      });
    } catch (error) {
      setFeedback({
        tone: "danger",
        ...commercialMutationFailureCopy(error, t),
      });
    }
  }

  async function runReconcile() {
    const reason = reconcileReason.trim();
    if (!reason) {
      return;
    }
    setFeedback(null);
    try {
      await reconcileMutation.mutateAsync({
        organizationId,
        productCode,
        body: { reason },
      });
      setPendingReconcileHint(false);
      setConfirm(null);
      setReconcileReason("");
      setFeedback({
        tone: "info",
        title: t("organization.entitlements.reconcile.success"),
        detail: "",
      });
    } catch (error) {
      setFeedback({
        tone: "danger",
        ...commercialMutationFailureCopy(error, t),
      });
    }
  }

  async function runCreateOverride() {
    const reason = createReason.trim();
    if (!createFeatureCode || !reason) {
      return;
    }
    let numericLimit: number | null | undefined;
    if (showNumericLimit) {
      const parsed = Number(createNumericLimit);
      if (!Number.isFinite(parsed) || parsed < 0 || !Number.isInteger(parsed)) {
        setFeedback({
          tone: "danger",
          title: t("organization.entitlements.override.create.error.validation"),
          detail: t("organization.entitlements.override.numericLimit.invalid"),
        });
        return;
      }
      numericLimit = parsed;
    }
    let expiresAtUtc: string | null = null;
    if (createExpiresLocal) {
      const parsed = new Date(createExpiresLocal);
      if (Number.isNaN(parsed.getTime()) || parsed.getTime() <= Date.now()) {
        setFeedback({
          tone: "danger",
          title: t("organization.entitlements.override.create.error.validation"),
          detail: t("organization.entitlements.override.expiry.invalid"),
        });
        return;
      }
      expiresAtUtc = parsed.toISOString();
    }
    setFeedback(null);
    try {
      await createOverrideMutation.mutateAsync({
        organizationId,
        productCode,
        body: {
          featureCode: createFeatureCode,
          enabled: createEnabled === "true",
          reason,
          numericLimit: showNumericLimit ? numericLimit : null,
          expiresAtUtc,
        },
      });
      setCreateOpen(false);
      setCreateFeatureCode("");
      setCreateEnabled("false");
      setCreateReason("");
      setCreateNumericLimit("");
      setCreateExpiresLocal("");
      setPendingReconcileHint(true);
      setFeedback({
        tone: "info",
        title: t("organization.entitlements.override.create.success"),
        detail: t("organization.entitlements.override.reconcileHint"),
      });
    } catch (error) {
      setFeedback({
        tone: "danger",
        ...commercialMutationFailureCopy(error, t),
      });
    }
  }

  async function runRevokeOverride() {
    if (!confirm || confirm.kind !== "revoke") {
      return;
    }
    const reason = revokeReason.trim();
    if (!reason) {
      return;
    }
    setFeedback(null);
    try {
      await revokeOverrideMutation.mutateAsync({
        overrideId: confirm.override.id,
        body: { reason },
      });
      setConfirm(null);
      setRevokeReason("");
      setPendingReconcileHint(true);
      setFeedback({
        tone: "info",
        title: t("organization.entitlements.override.revoke.success"),
        detail: t("organization.entitlements.override.reconcileHint"),
      });
    } catch (error) {
      setFeedback({
        tone: "danger",
        ...commercialMutationFailureCopy(error, t),
      });
    }
  }

  return (
    <div className="grid gap-4">
      {feedback ? (
        <Alert tone={feedback.tone === "danger" ? "danger" : "info"} title={feedback.title}>
          {feedback.detail ? <p>{feedback.detail}</p> : null}
        </Alert>
      ) : null}

      {pendingReconcileHint && canManageSubscriptions ? (
        <p className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3 text-[length:var(--exits-text-sm)] text-muted">
          {t("organization.entitlements.override.reconcileHint")}
        </p>
      ) : null}

      <Card className="grid gap-3 border-border bg-surface px-4 py-3">
        <div className="flex flex-wrap items-start justify-between gap-2">
          <div>
            <h2 className="text-[length:var(--exits-text-base)] font-semibold">
              {t("organization.entitlements.current.title")}
            </h2>
            <p className="mt-1 text-[length:var(--exits-text-xs)] text-muted">
              {t("organization.entitlements.current.description")}
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            {canManageSubscriptions ? (
              <>
                <Button
                  type="button"
                  size="sm"
                  variant="outline"
                  disabled={generateMutation.isPending}
                  aria-busy={generateMutation.isPending}
                  onClick={() => setConfirm({ kind: "generate", snapshot: latestSnapshot })}
                >
                  <RefreshCw aria-hidden className="mr-2 size-4" />
                  {t("organization.entitlements.generate.action")}
                </Button>
                <Button
                  type="button"
                  size="sm"
                  variant="outline"
                  disabled={reconcileMutation.isPending}
                  aria-busy={reconcileMutation.isPending}
                  onClick={() => setConfirm({ kind: "reconcile", snapshot: latestSnapshot })}
                >
                  {t("organization.entitlements.reconcile.action")}
                </Button>
              </>
            ) : null}
          </div>
        </div>

        {latestQuery.isPending ? (
          <p className="text-[length:var(--exits-text-sm)] text-muted" role="status" aria-busy="true">
            {t("organization.entitlements.current.loading")}
          </p>
        ) : null}

        {latestQuery.isError && isForbidden(latestQuery.error) ? (
          <p className="text-[length:var(--exits-text-sm)] text-muted">
            {t("organization.entitlements.unavailable")}
          </p>
        ) : null}

        {latestQuery.isError && isNotFound(latestQuery.error) ? (
          <p className="text-[length:var(--exits-text-sm)] text-muted">
            {t("organization.entitlements.current.empty")}
          </p>
        ) : null}

        {latestQuery.isError && latestDiagnostic ? (
          <ErrorState
            diagnostic={latestDiagnostic}
            title={t("organization.entitlements.current.error")}
            headingLevel="h2"
            onRetry={() => void latestQuery.refetch()}
          />
        ) : null}

        {latestSnapshot ? (
          <>
          <dl className="grid gap-2 text-[length:var(--exits-text-sm)]">
            <div className="grid gap-1 sm:grid-cols-[minmax(8rem,12rem)_1fr]">
              <dt className="text-muted">{t("organization.entitlements.column.plan")}</dt>
              <dd className="break-words font-medium">
                {latestSnapshot.planCode}
                {latestSnapshot.planVersionNumber != null
                  ? ` · v${latestSnapshot.planVersionNumber}`
                  : ""}
              </dd>
            </div>
            <div className="grid gap-1 sm:grid-cols-[minmax(8rem,12rem)_1fr]">
              <dt className="text-muted">{t("organization.entitlements.column.status")}</dt>
              <dd>
                <StatusIndicator
                  tone={organizationSubscriptionStatusTone(latestSnapshot.subscriptionStatus)}
                  label={organizationSubscriptionStatusLabel(latestSnapshot.subscriptionStatus, t)}
                />
              </dd>
            </div>
            <div className="grid gap-1 sm:grid-cols-[minmax(8rem,12rem)_1fr]">
              <dt className="text-muted">{t("organization.entitlements.current.snapshotVersion")}</dt>
              <dd>{latestSnapshot.snapshotVersion}</dd>
            </div>
            {latestSnapshot.generatedAtUtc ? (
              <div className="grid gap-1 sm:grid-cols-[minmax(8rem,12rem)_1fr]">
                <dt className="text-muted">{t("organization.entitlements.column.generated")}</dt>
                <dd>{formatInstant(latestSnapshot.generatedAtUtc, language) ?? "—"}</dd>
              </div>
            ) : null}
            {latestSnapshot.refreshByUtc ? (
              <div className="grid gap-1 sm:grid-cols-[minmax(8rem,12rem)_1fr]">
                <dt className="text-muted">{t("organization.entitlements.current.refreshBy")}</dt>
                <dd>{formatInstant(latestSnapshot.refreshByUtc, language) ?? "—"}</dd>
              </div>
            ) : null}
            {latestSnapshot.expiresAtUtc ? (
              <div className="grid gap-1 sm:grid-cols-[minmax(8rem,12rem)_1fr]">
                <dt className="text-muted">{t("organization.entitlements.current.expiresAt")}</dt>
                <dd>{formatInstant(latestSnapshot.expiresAtUtc, language) ?? "—"}</dd>
              </div>
            ) : null}
          </dl>
          <div>
            <p className="text-[length:var(--exits-text-xs)] text-muted">
              {grantSummaryLabel(latestSnapshot.grants, t)}
            </p>
            {latestSnapshot.grants.length > 0 ? (
              <>
                <Button
                  type="button"
                  size="sm"
                  variant="ghost"
                  className="mt-1 h-auto min-h-8 w-fit justify-start px-0 font-medium text-primary hover:bg-transparent hover:underline"
                  aria-expanded={currentGrantsExpanded}
                  aria-controls="current-entitlement-grants"
                  onClick={() => setCurrentGrantsExpanded((value) => !value)}
                >
                  {currentGrantsExpanded
                    ? t("organization.entitlements.grant.hide")
                    : t("organization.entitlements.grant.show")}
                </Button>
                {currentGrantsExpanded ? (
                  <div id="current-entitlement-grants" className="mt-2">
                    <EntitlementGrantDetails grants={latestSnapshot.grants} t={t} />
                  </div>
                ) : null}
              </>
            ) : (
              <p className="mt-1 text-[length:var(--exits-text-xs)] text-muted">
                {t("organization.entitlements.grant.noGrants")}
              </p>
            )}
          </div>
          </>
        ) : null}
      </Card>

      <section className="grid gap-3">
        <div className="flex flex-wrap items-end justify-between gap-2">
          <div>
            <h2 className="text-[length:var(--exits-text-base)] font-semibold">
              {t("organization.entitlements.override.title")}
            </h2>
            <p className="mt-1 text-[length:var(--exits-text-xs)] text-muted">
              {t("organization.entitlements.override.description")}
            </p>
          </div>
          {canManageOverrides ? (
            <Button type="button" size="sm" variant="outline" onClick={() => setCreateOpen(true)}>
              <Plus aria-hidden className="mr-2 size-4" />
              {t("organization.entitlements.override.create.action")}
            </Button>
          ) : null}
        </div>

        <label className="grid max-w-xs gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
          {t("organization.entitlements.override.statusFilter")}
          <select
            className={controlClass}
            value={overrideStatus}
            onChange={(event) => {
              setOverrideStatus(event.target.value as "" | "Active" | "Revoked");
              setOverridePage(1);
            }}
          >
            <option value="">{t("organization.entitlements.override.status.all")}</option>
            <option value="Active">{t("organization.entitlements.override.status.active")}</option>
            <option value="Revoked">{t("organization.entitlements.override.status.revoked")}</option>
          </select>
        </label>

        {overridesQuery.isPending ? (
          <p className="text-[length:var(--exits-text-sm)] text-muted" role="status" aria-busy="true">
            {t("organization.entitlements.override.loading")}
          </p>
        ) : null}

        {overridesQuery.isError && isForbidden(overridesQuery.error) ? (
          <p className="text-[length:var(--exits-text-sm)] text-muted">
            {t("organization.entitlements.unavailable")}
          </p>
        ) : null}

        {overridesQuery.isError && !isForbidden(overridesQuery.error) && overridesDiagnostic ? (
          <ErrorState
            diagnostic={overridesDiagnostic}
            title={t("organization.entitlements.override.error")}
            headingLevel="h2"
            onRetry={() => void overridesQuery.refetch()}
          />
        ) : null}

        {overridesQuery.data ? (
          <>
            {overridesQuery.data.items.length === 0 ? (
              <p className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3 text-[length:var(--exits-text-sm)] text-muted">
                {t("organization.entitlements.override.empty")}
              </p>
            ) : showTable ? (
              <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
                <AdminTable
                  caption={t("organization.entitlements.override.caption")}
                  empty={t("organization.entitlements.override.empty")}
                  columns={[
                    {
                      id: "feature",
                      header: t("organization.entitlements.override.column.feature"),
                      cell: (item) => (
                        <span className="break-all font-mono text-[length:var(--exits-text-xs)]">
                          {item.featureCode}
                        </span>
                      ),
                    },
                    {
                      id: "enabled",
                      header: t("organization.entitlements.override.column.enabled"),
                      cell: (item) =>
                        item.enabled
                          ? t("organization.entitlements.grant.enabled")
                          : t("organization.entitlements.grant.disabled"),
                    },
                    {
                      id: "status",
                      header: t("organization.entitlements.override.column.status"),
                      cell: (item) => {
                        const effective = overrideEffectiveStatus(item);
                        return t(
                          `organization.entitlements.override.status.${effective.toLowerCase()}` as MessageKey,
                        );
                      },
                    },
                    {
                      id: "reason",
                      header: t("organization.entitlements.override.column.reason"),
                      cell: (item) => item.reason ?? "—",
                    },
                    {
                      id: "expires",
                      header: t("organization.entitlements.override.column.expires"),
                      cell: (item) =>
                        item.expiresAtUtc
                          ? formatInstant(item.expiresAtUtc, language) ?? "—"
                          : t("organization.entitlements.override.permanent"),
                    },
                    {
                      id: "actions",
                      header: t("organization.entitlements.override.column.actions"),
                      cell: (item) =>
                        canManageOverrides && overrideEffectiveStatus(item) === "Active" ? (
                          <Button
                            type="button"
                            size="sm"
                            variant="destructive"
                            onClick={() => setConfirm({ kind: "revoke", override: item })}
                          >
                            {t("organization.entitlements.override.revoke.action")}
                          </Button>
                        ) : (
                          "—"
                        ),
                    },
                  ]}
                  rows={overridesQuery.data.items}
                />
              </div>
            ) : (
              <ul className="grid gap-2">
                {overridesQuery.data.items.map((item) => {
                  const effective = overrideEffectiveStatus(item);
                  return (
                    <li
                      key={item.id}
                      className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-2.5"
                    >
                      <p className="break-all font-mono text-[length:var(--exits-text-sm)]">
                        {item.featureCode}
                      </p>
                      <p className="mt-1 text-[length:var(--exits-text-xs)] text-muted">
                        {item.enabled
                          ? t("organization.entitlements.grant.enabled")
                          : t("organization.entitlements.grant.disabled")}
                        {" · "}
                        {t(
                          `organization.entitlements.override.status.${effective.toLowerCase()}` as MessageKey,
                        )}
                      </p>
                      <p className="mt-1 text-[length:var(--exits-text-xs)] text-muted">
                        {item.reason}
                      </p>
                      {canManageOverrides && effective === "Active" ? (
                        <Button
                          type="button"
                          size="sm"
                          variant="destructive"
                          className="mt-2"
                          onClick={() => setConfirm({ kind: "revoke", override: item })}
                        >
                          {t("organization.entitlements.override.revoke.action")}
                        </Button>
                      ) : null}
                    </li>
                  );
                })}
              </ul>
            )}
            <div className="flex flex-wrap items-center gap-2">
              <Button
                type="button"
                size="sm"
                variant="outline"
                disabled={overridePage <= 1}
                onClick={() => setOverridePage((page) => page - 1)}
              >
                {t("organizations.previous")}
              </Button>
              <p className="text-[length:var(--exits-text-xs)] text-muted">
                {t("organizations.page")} {overridePage} / {overrideTotalPages}
              </p>
              <Button
                type="button"
                size="sm"
                variant="outline"
                disabled={overridePage >= overrideTotalPages}
                onClick={() => setOverridePage((page) => page + 1)}
              >
                {t("organizations.next")}
              </Button>
            </div>
          </>
        ) : null}
      </section>

      <ConfirmActionDialog
        open={confirm?.kind === "generate"}
        title={t("organization.entitlements.generate.title")}
        description={t("organization.entitlements.generate.description")}
        confirmLabel={t("organization.entitlements.generate.action")}
        cancelLabel={t("organization.entitlements.dialog.cancel")}
        pendingLabel={t("organization.entitlements.dialog.pending")}
        pending={generateMutation.isPending}
        onCancel={() => setConfirm(null)}
        onConfirm={() => void runGenerate()}
      />

      <ConfirmActionDialog
        open={confirm?.kind === "reconcile"}
        title={t("organization.entitlements.reconcile.title")}
        description={t("organization.entitlements.reconcile.description")}
        confirmLabel={t("organization.entitlements.reconcile.action")}
        cancelLabel={t("organization.entitlements.dialog.cancel")}
        pendingLabel={t("organization.entitlements.dialog.pending")}
        pending={reconcileMutation.isPending}
        confirmDisabled={!reconcileReason.trim()}
        onCancel={() => {
          setConfirm(null);
          setReconcileReason("");
        }}
        onConfirm={() => void runReconcile()}
      >
        <dl className="grid gap-1 text-[length:var(--exits-text-xs)]">
          <div>
            <dt className="text-muted">{t("organization.entitlements.reconcile.organization")}</dt>
            <dd>{organizationName}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("organization.entitlements.product")}</dt>
            <dd>{productCode}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("organization.entitlements.current.snapshotVersion")}</dt>
            <dd>{confirm?.kind === "reconcile" ? (confirm.snapshot?.snapshotVersion ?? "—") : "—"}</dd>
          </div>
        </dl>
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
          {t("organization.entitlements.reconcile.reason")}
          <Input
            value={reconcileReason}
            onChange={(event) => setReconcileReason(event.target.value)}
          />
        </label>
      </ConfirmActionDialog>

      <ConfirmActionDialog
        open={confirm?.kind === "revoke"}
        title={t("organization.entitlements.override.revoke.title")}
        description={t("organization.entitlements.override.revoke.description")}
        confirmLabel={t("organization.entitlements.override.revoke.action")}
        cancelLabel={t("organization.entitlements.dialog.cancel")}
        pendingLabel={t("organization.entitlements.dialog.pending")}
        destructive
        pending={revokeOverrideMutation.isPending}
        confirmDisabled={!revokeReason.trim()}
        onCancel={() => {
          setConfirm(null);
          setRevokeReason("");
        }}
        onConfirm={() => void runRevokeOverride()}
      >
        {confirm?.kind === "revoke" ? (
          <dl className="grid gap-1 text-[length:var(--exits-text-xs)]">
            <div>
              <dt className="text-muted">{t("organization.entitlements.override.column.feature")}</dt>
              <dd className="break-all font-mono">{confirm.override.featureCode}</dd>
            </div>
            <div>
              <dt className="text-muted">{t("organization.entitlements.product")}</dt>
              <dd>{productCode}</dd>
            </div>
            <div>
              <dt className="text-muted">{t("organization.entitlements.override.column.enabled")}</dt>
              <dd>
                {confirm.override.enabled
                  ? t("organization.entitlements.grant.enabled")
                  : t("organization.entitlements.grant.disabled")}
              </dd>
            </div>
            <div>
              <dt className="text-muted">{t("organization.entitlements.override.column.expires")}</dt>
              <dd>
                {confirm.override.expiresAtUtc
                  ? formatInstant(confirm.override.expiresAtUtc, language) ?? "—"
                  : t("organization.entitlements.override.permanent")}
              </dd>
            </div>
          </dl>
        ) : null}
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
          {t("organization.entitlements.override.revoke.reason")}
          <Input value={revokeReason} onChange={(event) => setRevokeReason(event.target.value)} />
        </label>
      </ConfirmActionDialog>

      <ConfirmActionDialog
        open={createOpen}
        title={t("organization.entitlements.override.create.title")}
        description={t("organization.entitlements.override.create.description")}
        confirmLabel={t("organization.entitlements.override.create.action")}
        cancelLabel={t("organization.entitlements.dialog.cancel")}
        pendingLabel={t("organization.entitlements.dialog.pending")}
        pending={createOverrideMutation.isPending}
        confirmDisabled={!createFeatureCode || !createReason.trim()}
        onCancel={() => setCreateOpen(false)}
        onConfirm={() => void runCreateOverride()}
      >
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
          {t("organization.entitlements.override.create.feature")}
          <select
            className={controlClass}
            value={createFeatureCode}
            onChange={(event) => setCreateFeatureCode(event.target.value)}
          >
            <option value="">{t("organization.entitlements.override.create.feature.choose")}</option>
            {activeFeatures.map((feature) => (
              <option key={feature.featureCode} value={feature.featureCode}>
                {feature.displayName} ({feature.featureCode})
              </option>
            ))}
          </select>
        </label>
        <fieldset className="grid gap-1">
          <legend className="text-[length:var(--exits-text-xs)] font-medium text-muted">
            {t("organization.entitlements.override.create.enabled")}
          </legend>
          <label className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
            <input
              type="radio"
              name="override-enabled"
              checked={createEnabled === "true"}
              onChange={() => setCreateEnabled("true")}
            />
            {t("organization.entitlements.grant.enabled")}
          </label>
          <label className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
            <input
              type="radio"
              name="override-enabled"
              checked={createEnabled === "false"}
              onChange={() => setCreateEnabled("false")}
            />
            {t("organization.entitlements.grant.disabled")}
          </label>
        </fieldset>
        {showNumericLimit ? (
          <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
            {t("organization.entitlements.override.create.numericLimit")}
            <Input
              inputMode="numeric"
              value={createNumericLimit}
              onChange={(event) => setCreateNumericLimit(event.target.value)}
            />
          </label>
        ) : null}
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
          {t("organization.entitlements.override.create.expires")}
          <Input
            type="datetime-local"
            value={createExpiresLocal}
            onChange={(event) => setCreateExpiresLocal(event.target.value)}
          />
          <span className="font-normal">{t("organization.entitlements.override.create.expiresHint")}</span>
        </label>
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
          {t("organization.entitlements.override.create.reason")}
          <Input value={createReason} onChange={(event) => setCreateReason(event.target.value)} />
        </label>
      </ConfirmActionDialog>
    </div>
  );
}
