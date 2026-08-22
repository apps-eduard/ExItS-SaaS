import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { PlatformApiError } from "@/api/platform-http";
import {
  updatePersonalFeatureDefinition,
  type PersonalFeatureDefinition,
} from "@/api/personal-features/personal-features-client";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { ForbiddenState } from "@/features/overview/ForbiddenState";
import {
  personalFeatureDetailQueryKey,
  personalFeaturesListQueryKey,
  usePersonalFeatureDetailQuery,
} from "@/features/personal-features/use-personal-features-queries";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import { env } from "@/lib/env";

function formatInstant(value: string, language: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }
  return new Intl.DateTimeFormat(language === "fil-PH" ? "fil-PH" : "en-GB", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}

function PersonalFeatureEditForm({
  feature,
  onUpdated,
}: {
  feature: PersonalFeatureDefinition;
  onUpdated: (next: PersonalFeatureDefinition) => void;
}) {
  const { t } = usePreferences();
  const [displayName, setDisplayName] = useState(feature.displayName);
  const [isActive, setIsActive] = useState(feature.isActive);
  const [rewardPrice, setRewardPrice] = useState(
    feature.rewardPointsPrice == null ? "" : String(feature.rewardPointsPrice),
  );
  const [durationDays, setDurationDays] = useState(
    feature.defaultEntitlementDurationDays == null
      ? ""
      : String(feature.defaultEntitlementDurationDays),
  );
  const [expectedUpdatedAtUtc, setExpectedUpdatedAtUtc] = useState(feature.updatedAtUtc);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<{ title: string; detail?: string; conflict?: boolean } | null>(
    null,
  );
  const [success, setSuccess] = useState<string | null>(null);

  function resetForm() {
    setDisplayName(feature.displayName);
    setIsActive(feature.isActive);
    setRewardPrice(
      feature.rewardPointsPrice == null ? "" : String(feature.rewardPointsPrice),
    );
    setDurationDays(
      feature.defaultEntitlementDurationDays == null
        ? ""
        : String(feature.defaultEntitlementDurationDays),
    );
    setExpectedUpdatedAtUtc(feature.updatedAtUtc);
    setError(null);
    setSuccess(null);
  }

  async function handleSave() {
    if (busy) {
      return;
    }
    if (!displayName.trim()) {
      setError({ title: t("personalFeatures.validation.displayName") });
      return;
    }
    const parsedPrice = rewardPrice.trim() === "" ? null : Number.parseInt(rewardPrice, 10);
    const parsedDuration =
      durationDays.trim() === "" ? null : Number.parseInt(durationDays, 10);
    if (parsedPrice != null && (!Number.isFinite(parsedPrice) || parsedPrice < 1)) {
      setError({ title: t("personalFeatures.validation.rewardPrice") });
      return;
    }
    if (
      parsedDuration != null &&
      (!Number.isFinite(parsedDuration) || parsedDuration < 1 || parsedDuration > 3650)
    ) {
      setError({ title: t("personalFeatures.validation.duration") });
      return;
    }

    setBusy(true);
    setError(null);
    setSuccess(null);
    try {
      const updated = await updatePersonalFeatureDefinition(
        env.platformApiBaseUrl,
        feature.featureCode,
        {
          displayName: displayName.trim(),
          isActive,
          rewardPointsPrice: parsedPrice,
          defaultEntitlementDurationDays: parsedDuration,
          expectedUpdatedAtUtc,
        },
      );
      onUpdated(updated);
      setExpectedUpdatedAtUtc(updated.updatedAtUtc);
      setSuccess(t("personalFeatures.saveSuccess"));
    } catch (err) {
      const conflict = err instanceof PlatformApiError && err.status === 409;
      setError({
        title: conflict ? t("personalFeatures.conflict") : t("personalFeatures.saveFailed"),
        detail:
          err instanceof PlatformApiError
            ? (err.problem.detail ?? err.message)
            : err instanceof Error
              ? err.message
              : undefined,
        conflict,
      });
    } finally {
      setBusy(false);
    }
  }

  return (
    <DashboardSection title={t("personalFeatures.editSection")}>
      {success ? (
        <Alert title={success} tone="success" data-testid="personal-features-save-success" />
      ) : null}
      {error ? (
        <Alert
          title={error.title}
          tone="danger"
          data-testid={
            error.conflict ? "personal-features-save-conflict" : "personal-features-save-error"
          }
        >
          {error.detail}
        </Alert>
      ) : null}
      <div className="grid gap-3 sm:grid-cols-2">
        <label className="grid gap-1 text-[length:var(--exits-text-sm)] sm:col-span-2" htmlFor="pf-code">
          {t("personalFeatures.featureCode")}
          <Input id="pf-code" value={feature.featureCode} disabled />
        </label>
        <label
          className="grid gap-1 text-[length:var(--exits-text-sm)] sm:col-span-2"
          htmlFor="pf-name"
        >
          {t("personalFeatures.displayName")}
          <Input
            id="pf-name"
            data-testid="personal-features-edit-name"
            value={displayName}
            disabled={busy}
            onChange={(event) => setDisplayName(event.target.value)}
          />
        </label>
        <label className="flex items-center gap-2 text-[length:var(--exits-text-sm)] sm:col-span-2">
          <input
            type="checkbox"
            data-testid="personal-features-edit-active"
            checked={isActive}
            disabled={busy}
            onChange={(event) => setIsActive(event.target.checked)}
          />
          {t("personalFeatures.enabled")}
        </label>
        <label className="grid gap-1 text-[length:var(--exits-text-sm)]" htmlFor="pf-price">
          {t("personalFeatures.rewardPoints")}
          <Input
            id="pf-price"
            type="number"
            min={1}
            data-testid="personal-features-edit-price"
            value={rewardPrice}
            disabled={busy}
            placeholder={t("personalFeatures.notRedeemable")}
            onChange={(event) => setRewardPrice(event.target.value)}
          />
          <span className="text-[length:var(--exits-text-xs)] text-muted">
            {t("personalFeatures.rewardPriceHint")}
          </span>
        </label>
        <label className="grid gap-1 text-[length:var(--exits-text-sm)]" htmlFor="pf-duration">
          {t("personalFeatures.durationDays")}
          <Input
            id="pf-duration"
            type="number"
            min={1}
            max={3650}
            data-testid="personal-features-edit-duration"
            value={durationDays}
            disabled={busy}
            placeholder={t("personalFeatures.indefinite")}
            onChange={(event) => setDurationDays(event.target.value)}
          />
          <span className="text-[length:var(--exits-text-xs)] text-muted">
            {t("personalFeatures.durationHint")}
          </span>
        </label>
      </div>
      <div className="mt-3 flex flex-wrap gap-2">
        <Button
          type="button"
          size="sm"
          disabled={busy}
          data-testid="personal-features-save"
          onClick={() => void handleSave()}
        >
          {busy ? t("personalFeatures.saving") : t("personalFeatures.save")}
        </Button>
        <Button type="button" size="sm" variant="outline" disabled={busy} onClick={resetForm}>
          {t("personalFeatures.reset")}
        </Button>
      </div>
    </DashboardSection>
  );
}

export function PersonalFeatureDetailPage() {
  const { t, language } = usePreferences();
  const authorization = useAuthorization();
  const queryClient = useQueryClient();
  const params = useParams();
  const featureCode = params.featureCode ? decodeURIComponent(params.featureCode) : null;

  const canView =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.viewPortfolio);
  const canManage =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.manageCatalog);

  const query = usePersonalFeatureDetailQuery(featureCode, canView);

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="mt-3 h-16 w-full max-w-xl" />
      </section>
    );
  }

  if (!canView) {
    return <ForbiddenState requiredPermission={PLATFORM_PERMISSIONS.viewPortfolio} />;
  }

  if (!featureCode) {
    return (
      <section className="grid gap-4">
        <PageHeader title={t("nav.personalFeatures")} description={t("personalFeatures.notFound")} />
        <Link to="/admin/personal-features">{t("personalFeatures.back")}</Link>
      </section>
    );
  }

  if (
    query.error instanceof PlatformApiError &&
    (query.error.status === 401 || query.error.status === 403)
  ) {
    return <ForbiddenState requiredPermission={PLATFORM_PERMISSIONS.viewPortfolio} />;
  }

  if (query.isPending) {
    return (
      <section
        className="grid max-w-3xl gap-3"
        role="status"
        aria-busy="true"
        aria-label={t("personalFeatures.loading")}
      >
        <DashboardWidgetSkeleton rows={6} />
      </section>
    );
  }

  if (query.isError) {
    return (
      <ErrorState
        diagnostic={normalizeDiagnosticError({
          error: query.error,
          operation: "Load personal feature",
        })}
        title={t("personalFeatures.detailError")}
        headingLevel="h1"
        onRetry={() => void query.refetch()}
      />
    );
  }

  const feature = query.data;
  if (!feature) {
    return (
      <section className="grid gap-4">
        <PageHeader title={t("nav.personalFeatures")} description={t("personalFeatures.notFound")} />
        <Link to="/admin/personal-features">{t("personalFeatures.back")}</Link>
      </section>
    );
  }

  function onUpdated(next: PersonalFeatureDefinition) {
    queryClient.setQueryData(personalFeatureDetailQueryKey(next.featureCode), next);
    void queryClient.invalidateQueries({ queryKey: personalFeaturesListQueryKey });
  }

  return (
    <section className="grid max-w-3xl gap-4" data-testid="personal-feature-detail-page">
      <p className="text-[length:var(--exits-text-sm)]">
        <Link className="text-primary hover:underline" to="/admin/personal-features">
          {t("personalFeatures.back")}
        </Link>
      </p>

      <PageHeader
        title={feature.displayName}
        description={t("personalFeatures.detailSubtitle")}
        actions={
          <StatusIndicator
            tone={feature.isActive ? "success" : "neutral"}
            label={feature.isActive ? t("personalFeatures.active") : t("personalFeatures.inactive")}
          />
        }
      />

      <DashboardSection title={t("personalFeatures.summary")}>
        <dl className="grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2">
          <div>
            <dt className="text-[length:var(--exits-text-xs)] text-muted">
              {t("personalFeatures.featureCode")}
            </dt>
            <dd className="font-mono text-[length:var(--exits-text-xs)]">{feature.featureCode}</dd>
          </div>
          <div>
            <dt className="text-[length:var(--exits-text-xs)] text-muted">
              {t("personalFeatures.displayName")}
            </dt>
            <dd>{feature.displayName}</dd>
          </div>
          <div>
            <dt className="text-[length:var(--exits-text-xs)] text-muted">
              {t("personalFeatures.rewardPoints")}
            </dt>
            <dd>
              {feature.rewardPointsPrice == null
                ? t("personalFeatures.notRedeemable")
                : feature.rewardPointsPrice}
            </dd>
          </div>
          <div>
            <dt className="text-[length:var(--exits-text-xs)] text-muted">
              {t("personalFeatures.durationDays")}
            </dt>
            <dd>
              {feature.defaultEntitlementDurationDays == null
                ? t("personalFeatures.indefinite")
                : feature.defaultEntitlementDurationDays}
            </dd>
          </div>
          <div className="sm:col-span-2">
            <dt className="text-[length:var(--exits-text-xs)] text-muted">
              {t("personalFeatures.updatedAt")}
            </dt>
            <dd className="tabular-nums text-muted">
              {formatInstant(feature.updatedAtUtc, language)}
            </dd>
          </div>
        </dl>
      </DashboardSection>

      {canManage ? (
        <PersonalFeatureEditForm
          key={feature.featureCode}
          feature={feature}
          onUpdated={onUpdated}
        />
      ) : (
        <Alert
          title={t("shell.forbidden.accessDenied")}
          tone="info"
          data-testid="personal-features-manage-forbidden"
        >
          <p className="font-mono text-[length:var(--exits-text-xs)] text-muted">
            {PLATFORM_PERMISSIONS.manageCatalog}
          </p>
        </Alert>
      )}
    </section>
  );
}
