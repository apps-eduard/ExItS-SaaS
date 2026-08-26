import { parseOrganizationId } from "@/api/organizations/organization-id";
import type { CommercialEntitlementRecord } from "@/api/organizations/organization-types";
import { PlatformApiError } from "@/api/platform-http";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { useOrganizationCommercialSummaryQuery } from "@/features/organizations/use-organization-workspace-queries";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import {
  organizationSubscriptionStatusLabel,
  organizationSubscriptionStatusTone,
} from "@/features/organizations/organization-subscription-status";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import { useParams } from "react-router-dom";

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

function productLabel(record: CommercialEntitlementRecord): string {
  return record.productDisplayName || record.productCode;
}

export function OrganizationProductsPage() {
  const { t, language } = usePreferences();
  const params = useParams();
  const organizationId = parseOrganizationId(params.organizationId);
  const query = useOrganizationCommercialSummaryQuery(organizationId);
  const showTable = useMediaQuery("(min-width: 768px)");
  const diagnostic = query.error
    ? normalizeDiagnosticError({
        error: query.error,
        operation: "Load organization product access",
      })
    : null;

  if (
    query.error instanceof PlatformApiError &&
    (query.error.status === 401 || query.error.status === 403)
  ) {
    return <ShellNotFoundPage />;
  }

  const records = query.data?.latestEntitlements ?? null;

  return (
    <section className="grid max-w-3xl gap-4">
      <PageHeader
        title={t("organization.products.title")}
        description={t("organization.products.description")}
      />

      {query.isPending ? (
        <div
          className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3"
          role="status"
          aria-busy="true"
          aria-label={t("organization.products.loading")}
        >
          <DashboardWidgetSkeleton rows={5} />
        </div>
      ) : null}

      {query.isError && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("organization.products.error")}
          headingLevel="h2"
          onRetry={() => void query.refetch()}
        />
      ) : null}

      {records ? (
        showTable ? (
          <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
            <AdminTable
              caption={t("organization.products.caption")}
              empty={t("organization.products.empty")}
              columns={[
                {
                  id: "product",
                  header: t("organization.products.column.product"),
                  cell: (item) => (
                    <span className="font-medium">
                      {productLabel(item)}
                      {item.productDisplayName ? (
                        <span className="mt-0.5 block font-mono text-[length:var(--exits-text-xs)] font-normal text-muted">
                          {item.productCode}
                        </span>
                      ) : null}
                    </span>
                  ),
                },
                {
                  id: "status",
                  header: t("organization.products.column.status"),
                  cell: (item) => (
                    <StatusIndicator
                      tone={organizationSubscriptionStatusTone(item.subscriptionStatus)}
                      label={organizationSubscriptionStatusLabel(item.subscriptionStatus, t)}
                    />
                  ),
                },
                {
                  id: "revision",
                  header: t("organization.products.column.revision"),
                  cell: (item) =>
                    item.snapshotVersion != null ? String(item.snapshotVersion) : "—",
                },
                {
                  id: "generated",
                  header: t("organization.products.column.generated"),
                  cell: (item) => formatInstant(item.generatedAtUtc, language) || "—",
                },
              ]}
              rows={records}
            />
            <p className="mt-2 text-[length:var(--exits-text-xs)] text-muted">
              {t("organization.products.hint")}
            </p>
          </div>
        ) : (
          <ul className="grid gap-2">
            {records.length === 0 ? (
              <li className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-3 text-[length:var(--exits-text-sm)] text-muted">
                <p>{t("organization.products.empty")}</p>
                <p className="mt-1 text-[length:var(--exits-text-xs)]">
                  {t("organization.products.hint")}
                </p>
              </li>
            ) : (
              records.map((item) => (
                <li
                  key={item.id}
                  className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-2.5"
                >
                  <p className="font-medium">{productLabel(item)}</p>
                  <p className="mt-0.5 font-mono text-[length:var(--exits-text-xs)] text-muted">
                    {item.productCode}
                  </p>
                  <div className="mt-1.5 flex flex-wrap items-center gap-2">
                    <StatusIndicator
                      tone={organizationSubscriptionStatusTone(item.subscriptionStatus)}
                      label={organizationSubscriptionStatusLabel(item.subscriptionStatus, t)}
                    />
                    {item.snapshotVersion != null ? (
                      <span className="text-[length:var(--exits-text-xs)] text-muted">
                        {t("organization.products.column.revision")} {item.snapshotVersion}
                      </span>
                    ) : null}
                  </div>
                  {formatInstant(item.generatedAtUtc, language) ? (
                    <p className="mt-1 text-[length:var(--exits-text-xs)] text-muted">
                      {formatInstant(item.generatedAtUtc, language)}
                    </p>
                  ) : null}
                </li>
              ))
            )}
          </ul>
        )
      ) : null}
    </section>
  );
}
