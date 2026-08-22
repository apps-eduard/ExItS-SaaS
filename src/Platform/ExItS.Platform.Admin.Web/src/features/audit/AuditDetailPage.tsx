import type { ReactNode } from "react";
import { Link, useParams } from "react-router-dom";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { parseAuditRecordId } from "@/api/audit/audit-list-query";
import { PlatformApiError } from "@/api/platform-http";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { usePlatformAuditDetailQuery } from "@/features/audit/use-audit-queries";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

function outcomeTone(outcome: string): "success" | "warning" | "danger" | "neutral" {
  if (outcome === "Succeeded") {
    return "success";
  }
  if (outcome === "Denied") {
    return "warning";
  }
  if (outcome === "Failed") {
    return "danger";
  }
  return "neutral";
}

function outcomeLabel(t: (key: MessageKey) => string, outcome: string): string {
  if (outcome === "Succeeded") {
    return t("dashboard.audit.outcome.Succeeded");
  }
  if (outcome === "Denied") {
    return t("dashboard.audit.outcome.Denied");
  }
  if (outcome === "Failed") {
    return t("dashboard.audit.outcome.Failed");
  }
  return outcome;
}

function formatInstant(value: string | undefined, language: string): string {
  if (!value) {
    return "—";
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

function DetailField({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="grid gap-1 border-b border-border py-3 last:border-b-0 sm:grid-cols-[12rem_1fr] sm:gap-4">
      <dt className="text-[length:var(--exits-text-xs)] font-semibold tracking-wide text-muted uppercase">
        {label}
      </dt>
      <dd className="break-words text-[length:var(--exits-text-sm)] text-foreground">{children}</dd>
    </div>
  );
}

export function AuditDetailPage() {
  const { t, language, theme, density } = usePreferences();
  const authorization = useAuthorization();
  const params = useParams();
  const auditId = parseAuditRecordId(params.auditId);

  const canView =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.viewAuditRecords);

  const query = usePlatformAuditDetailQuery(canView ? auditId : null);

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true" className="grid gap-4">
        <DashboardWidgetSkeleton />
      </section>
    );
  }

  if (!canView || auditId == null) {
    return <ShellNotFoundPage />;
  }

  if (
    query.error instanceof PlatformApiError &&
    (query.error.status === 401 || query.error.status === 403)
  ) {
    return <ShellNotFoundPage />;
  }

  if (query.error instanceof PlatformApiError && query.error.status === 404) {
    return (
      <section className="grid gap-4" data-testid="audit-detail-not-found">
        <PageHeader title={t("audit.detail.title")} description={t("audit.detail.notFound")} />
        <Button asChild variant="outline" className="w-fit">
          <Link to="/admin/audit">{t("audit.detail.back")}</Link>
        </Button>
      </section>
    );
  }

  if (query.isPending) {
    return (
      <section aria-busy="true" className="grid gap-4">
        <PageHeader title={t("audit.detail.title")} description={t("audit.detail.description")} />
        <DashboardWidgetSkeleton />
      </section>
    );
  }

  if (query.isError) {
    return (
      <section className="grid gap-4">
        <PageHeader title={t("audit.detail.title")} description={t("audit.detail.description")} />
        <ErrorState
          diagnostic={normalizeDiagnosticError({
            error: query.error,
            operation: "Load audit record",
            environment: { locale: language, theme, density },
          })}
          description={t("audit.detail.error")}
          onRetry={() => void query.refetch()}
        />
        <Button asChild variant="outline" className="w-fit">
          <Link to="/admin/audit">{t("audit.detail.back")}</Link>
        </Button>
      </section>
    );
  }

  const record = query.data;

  return (
    <section className="grid gap-4" data-testid="audit-detail-page">
      <PageHeader
        title={t("audit.detail.title")}
        description={t("audit.detail.description")}
        actions={
          <Button asChild variant="outline" size="sm">
            <Link to="/admin/audit">{t("audit.detail.back")}</Link>
          </Button>
        }
      />

      <DashboardSection title={t("audit.detail.section.event")}>
        <dl>
          <DetailField label={t("audit.detail.field.occurred")}>
            {formatInstant(record.occurredAtUtc, language)}
          </DetailField>
          <DetailField label={t("audit.detail.field.outcome")}>
            <StatusIndicator tone={outcomeTone(record.outcome)} label={outcomeLabel(t, record.outcome)} />
          </DetailField>
          <DetailField label={t("audit.detail.field.action")}>
            <span className="font-mono text-[length:var(--exits-text-xs)]">{record.actionCode}</span>
          </DetailField>
          <DetailField label={t("audit.detail.field.summary")}>
            {record.summary?.trim() ? record.summary : "—"}
          </DetailField>
          <DetailField label={t("audit.detail.field.reason")}>
            {record.reason?.trim() ? record.reason : "—"}
          </DetailField>
        </dl>
      </DashboardSection>

      <DashboardSection title={t("audit.detail.section.actor")}>
        <dl>
          <DetailField label={t("audit.detail.field.actorIdentifier")}>
            {record.actorIdentifier}
          </DetailField>
          <DetailField label={t("audit.detail.field.actorType")}>{record.actorType}</DetailField>
        </dl>
      </DashboardSection>

      <DashboardSection title={t("audit.detail.section.target")}>
        <dl>
          <DetailField label={t("audit.detail.field.targetType")}>{record.targetType}</DetailField>
          <DetailField label={t("audit.detail.field.targetId")}>
            <span className="font-mono text-[length:var(--exits-text-xs)]">{record.targetId}</span>
          </DetailField>
          <DetailField label={t("audit.detail.field.organization")}>
            {record.organizationId ? (
              <Link
                className="text-primary underline-offset-4 hover:underline"
                to={`/admin/organizations/${record.organizationId}`}
              >
                {record.organizationId}
              </Link>
            ) : (
              "—"
            )}
          </DetailField>
          <DetailField label={t("audit.detail.field.product")}>
            {record.productCode?.trim() ? record.productCode : "—"}
          </DetailField>
        </dl>
      </DashboardSection>

      <DashboardSection title={t("audit.detail.section.trace")}>
        <dl>
          <DetailField label={t("audit.detail.field.correlationId")}>
            {record.correlationId?.trim() ? (
              <span className="font-mono text-[length:var(--exits-text-xs)]">
                {record.correlationId}
              </span>
            ) : (
              "—"
            )}
          </DetailField>
          <DetailField label={t("audit.detail.field.recordId")}>
            <span className="font-mono text-[length:var(--exits-text-xs)]">{record.id}</span>
          </DetailField>
        </dl>
      </DashboardSection>
    </section>
  );
}
