import { useMemo } from "react";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { PrivacyDisclaimer } from "@/features/privacy-compliance/PrivacyDisclaimer";
import { PrivacyStatusTag } from "@/features/privacy-compliance/PrivacyStatusTag";
import {
  PrivacyAuthLoading,
  PrivacyForbidden,
} from "@/features/privacy-compliance/PrivacyGateStates";
import {
  privacyForbiddenFromError,
  usePrivacyViewGate,
} from "@/features/privacy-compliance/privacy-gate";
import { usePrivacySystemsQuery } from "@/features/privacy-compliance/use-privacy-queries";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";

export function PrivacySystemsPage() {
  const { t, language, theme, density } = usePreferences();
  const { authorization, canView } = usePrivacyViewGate();
  const query = usePrivacySystemsQuery(canView);

  const rows = useMemo(() => {
    if (!query.data) {
      return null;
    }
    return [...query.data].sort((a, b) =>
      a.code.localeCompare(b.code, undefined, { sensitivity: "base" }),
    );
  }, [query.data]);

  if (authorization.status === "loading") {
    return <PrivacyAuthLoading />;
  }
  if (!canView) {
    return <PrivacyForbidden />;
  }
  if (privacyForbiddenFromError(query.error)) {
    return <PrivacyForbidden />;
  }

  return (
    <section className="grid gap-4" data-testid="privacy-systems-page">
      <PageHeader title={t("privacy.systems.title")} description={t("privacy.systems.description")} />
      <PrivacyDisclaimer />

      {query.isPending ? (
        <div role="status" aria-busy="true" aria-label={t("privacy.systems.loading")}>
          <DashboardWidgetSkeleton />
        </div>
      ) : null}

      {query.isError ? (
        <ErrorState
          diagnostic={normalizeDiagnosticError({
            error: query.error,
            operation: "Load privacy compliance systems",
            environment: { locale: language, theme, density },
          })}
          description={t("privacy.systems.error")}
          onRetry={() => void query.refetch()}
        />
      ) : null}

      {rows ? (
        <AdminTable
          caption={t("privacy.systems.title")}
          empty={t("privacy.empty.systems")}
          rows={rows}
          columns={[
            {
              id: "code",
              header: t("privacy.column.code"),
              cell: (row) => (
                <span className="font-mono text-[length:var(--exits-text-xs)]">{row.code}</span>
              ),
            },
            {
              id: "name",
              header: t("privacy.systems.column.name"),
              cell: (row) => row.systemName,
            },
            {
              id: "purpose",
              header: t("privacy.systems.column.purpose"),
              cell: (row) => row.purpose,
            },
            {
              id: "owner",
              header: t("privacy.column.owner"),
              cell: (row) => row.owner,
            },
            {
              id: "pia",
              header: t("privacy.systems.column.piaStatus"),
              cell: (row) => <PrivacyStatusTag value={row.piaStatus} />,
            },
            {
              id: "storage",
              header: t("privacy.systems.column.storage"),
              cell: (row) => row.storageLocation,
            },
          ]}
        />
      ) : null}
    </section>
  );
}
