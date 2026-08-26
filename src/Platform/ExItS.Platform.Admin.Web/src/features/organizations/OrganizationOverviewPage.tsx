import { parseOrganizationId } from "@/api/organizations/organization-id";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { OrganizationBrandingEditor } from "@/features/organizations/OrganizationBrandingEditor";
import { OrganizationLifecycleOperator } from "@/features/organizations/OrganizationLifecycleOperator";
import { OrganizationProfileEditor } from "@/features/organizations/OrganizationProfileEditor";
import {
  useOrganizationCommercialSummaryQuery,
  useOrganizationDetailQuery,
} from "@/features/organizations/use-organization-workspace-queries";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";
import { useParams } from "react-router-dom";

const STATUS_LABELS: Record<string, MessageKey> = {
  Active: "dashboard.status.Active",
  Suspended: "dashboard.status.Suspended",
  Closed: "dashboard.status.Closed",
};

function statusTone(status: string): "success" | "warning" | "danger" | "neutral" {
  if (status === "Active") {
    return "success";
  }
  if (status === "Suspended") {
    return "warning";
  }
  if (status === "Closed") {
    return "danger";
  }
  return "neutral";
}

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

export function OrganizationOverviewPage() {
  const { t, language } = usePreferences();
  const params = useParams();
  const organizationId = parseOrganizationId(params.organizationId);
  const organizationQuery = useOrganizationDetailQuery(organizationId);
  const commercialQuery = useOrganizationCommercialSummaryQuery(organizationId);
  const organization = organizationQuery.data;

  if (!organization) {
    return null;
  }

  const commercialDiagnostic = commercialQuery.error
    ? normalizeDiagnosticError({
        error: commercialQuery.error,
        operation: "Load commercial summary",
      })
    : null;

  return (
    <section className="grid max-w-3xl gap-4">
      <PageHeader
        title={organization.displayName}
        description={organization.slug}
        actions={
          <StatusIndicator
            tone={statusTone(organization.status)}
            label={
              STATUS_LABELS[organization.status]
                ? t(STATUS_LABELS[organization.status]!)
                : organization.status
            }
          />
        }
      />

      <DashboardSection title={t("organization.overview.identity")}>
        <dl className="grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2">
          <div className="min-w-0">
            <dt className="text-[length:var(--exits-text-xs)] text-muted">
              {t("organizations.column.identifier")}
            </dt>
            <dd className="break-words font-mono text-[length:var(--exits-text-xs)]">
              {organization.slug}
            </dd>
          </div>
          <div className="min-w-0">
            <dt className="text-[length:var(--exits-text-xs)] text-muted">
              {t("organizations.column.status")}
            </dt>
            <dd>
              {STATUS_LABELS[organization.status]
                ? t(STATUS_LABELS[organization.status]!)
                : organization.status}
            </dd>
          </div>
          <div className="min-w-0">
            <dt className="text-[length:var(--exits-text-xs)] text-muted">
              {t("organizations.column.created")}
            </dt>
            <dd className="tabular-nums text-muted">
              {formatInstant(organization.createdAtUtc, language) ?? "—"}
            </dd>
          </div>
          <div className="min-w-0">
            <dt className="text-[length:var(--exits-text-xs)] text-muted">
              {t("organizations.column.updated")}
            </dt>
            <dd className="tabular-nums text-muted">
              {formatInstant(organization.updatedAtUtc, language) ?? "—"}
            </dd>
          </div>
        </dl>
        <div className="mt-3">
          <OrganizationLifecycleOperator organization={organization} />
        </div>
      </DashboardSection>

      <OrganizationProfileEditor
        key={`profile-${organization.updatedAtUtc ?? organization.id}`}
        organization={organization}
      />
      <OrganizationBrandingEditor
        key={`branding-${organization.updatedAtUtc ?? organization.id}`}
        organization={organization}
      />

      <DashboardSection
        title={t("organization.overview.commercial")}
        description={t("organization.overview.commercial.hint")}
      >
        {commercialQuery.isPending ? (
          <div
            role="status"
            aria-busy="true"
            aria-label={t("organization.overview.commercial.loading")}
          >
            <DashboardWidgetSkeleton rows={4} />
          </div>
        ) : null}
        {commercialQuery.isError && commercialDiagnostic ? (
          <ErrorState
            diagnostic={commercialDiagnostic}
            title={t("organization.overview.commercial.error")}
            headingLevel="h2"
            onRetry={() => void commercialQuery.refetch()}
          />
        ) : null}
        {commercialQuery.data ? (
          <div className="grid gap-3 text-[length:var(--exits-text-sm)]">
            <CommercialRecordList
              title={t("organization.overview.subscriptions")}
              empty={t("organization.overview.commercial.empty")}
              items={commercialQuery.data.subscriptions.map((item) => ({
                id: item.id,
                primary: item.productCode,
                secondary: item.status,
              }))}
            />
            <CommercialRecordList
              title={t("organization.overview.payments")}
              empty={t("organization.overview.commercial.empty")}
              items={commercialQuery.data.payments.map((item) => ({
                id: item.id,
                primary: item.productCode,
                secondary: item.status,
              }))}
            />
            <CommercialRecordList
              title={t("organization.overview.entitlements")}
              empty={t("organization.overview.commercial.empty")}
              items={commercialQuery.data.latestEntitlements.map((item) => ({
                id: item.id,
                primary: item.productCode,
                secondary: item.subscriptionStatus,
              }))}
            />
          </div>
        ) : null}
      </DashboardSection>
    </section>
  );
}

function CommercialRecordList({
  title,
  empty,
  items,
}: {
  title: string;
  empty: string;
  items: Array<{ id: string; primary: string; secondary: string }>;
}) {
  return (
    <div className="min-w-0">
      <h3 className="text-[length:var(--exits-text-xs)] font-medium text-muted">{title}</h3>
      {items.length === 0 ? (
        <p className="mt-1 text-muted">{empty}</p>
      ) : (
        <ul className="mt-1 grid gap-1">
          {items.map((item) => (
            <li key={item.id} className="min-w-0 break-words">
              <span className="font-mono text-[length:var(--exits-text-xs)]">{item.primary}</span>
              <span className="text-muted"> · {item.secondary}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
