import { useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { sortByCode } from "@/api/privacy-compliance/privacy-filters";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { PrivacyDisclaimer } from "@/features/privacy-compliance/PrivacyDisclaimer";
import {
  PrivacyAuthLoading,
  PrivacyForbidden,
} from "@/features/privacy-compliance/PrivacyGateStates";
import {
  privacyForbiddenFromError,
  usePrivacyViewGate,
} from "@/features/privacy-compliance/privacy-gate";
import {
  usePrivacyAggregatedEvidenceQuery,
  usePrivacyRequirementsQuery,
} from "@/features/privacy-compliance/use-privacy-queries";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";

function formatInstant(value: string | undefined, language: string): string | null {
  if (!value) {
    return null;
  }
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }
  return new Intl.DateTimeFormat(language === "fil-PH" ? "fil-PH" : "en-US", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "UTC",
  }).format(date);
}

export function PrivacyEvidencePage() {
  const { t, language, theme, density } = usePreferences();
  const { authorization, canView } = usePrivacyViewGate();
  const [searchParams, setSearchParams] = useSearchParams();
  const requirementFromUrl = searchParams.get("requirementId") ?? "";
  const [draftRequirement, setDraftRequirement] = useState(requirementFromUrl);

  const requirementsQuery = usePrivacyRequirementsQuery(canView);
  const scopedRequirements = useMemo(() => {
    if (!requirementsQuery.data) {
      return undefined;
    }
    const sorted = sortByCode(requirementsQuery.data);
    if (!requirementFromUrl.trim()) {
      return sorted;
    }
    return sorted.filter(
      (row) => row.id.localeCompare(requirementFromUrl, undefined, { sensitivity: "accent" }) === 0,
    );
  }, [requirementsQuery.data, requirementFromUrl]);

  const evidenceQuery = usePrivacyAggregatedEvidenceQuery(
    canView && requirementsQuery.isSuccess,
    scopedRequirements,
  );

  if (authorization.status === "loading") {
    return <PrivacyAuthLoading />;
  }
  if (!canView) {
    return <PrivacyForbidden />;
  }
  if (
    privacyForbiddenFromError(requirementsQuery.error) ||
    privacyForbiddenFromError(evidenceQuery.error)
  ) {
    return <PrivacyForbidden />;
  }

  const loading = requirementsQuery.isPending || evidenceQuery.isPending;
  const error = requirementsQuery.error ?? evidenceQuery.error;

  return (
    <section className="grid gap-4" data-testid="privacy-evidence-page">
      <PageHeader title={t("privacy.evidence.title")} description={t("privacy.evidence.description")} />
      <PrivacyDisclaimer />

      <form
        className="flex flex-wrap items-end gap-2"
        data-testid="privacy-evidence-filters"
        onSubmit={(event) => {
          event.preventDefault();
          const next = new URLSearchParams();
          if (draftRequirement.trim().length > 0) {
            next.set("requirementId", draftRequirement.trim());
          }
          setSearchParams(next, { replace: true });
        }}
      >
        <label
          className="grid gap-1 text-[length:var(--exits-text-sm)]"
          htmlFor="privacy-evidence-requirement"
        >
          {t("privacy.filter.requirement")}
          <select
            id="privacy-evidence-requirement"
            className="h-9 min-w-[16rem] max-w-full rounded-[var(--exits-density-radius)] border border-border bg-surface px-2"
            value={draftRequirement}
            onChange={(event) => setDraftRequirement(event.target.value)}
            disabled={!requirementsQuery.data}
          >
            <option value="">{t("privacy.filter.all")}</option>
            {(requirementsQuery.data ? sortByCode(requirementsQuery.data) : []).map((row) => (
              <option key={row.id} value={row.id}>
                {row.code} — {row.title}
              </option>
            ))}
          </select>
        </label>
        <Button type="submit" size="sm">
          {t("privacy.filter.apply")}
        </Button>
        <Button
          type="button"
          size="sm"
          variant="outline"
          onClick={() => {
            setDraftRequirement("");
            setSearchParams({}, { replace: true });
          }}
        >
          {t("privacy.filter.reset")}
        </Button>
      </form>

      {loading ? (
        <div role="status" aria-busy="true" aria-label={t("privacy.evidence.loading")}>
          <DashboardWidgetSkeleton />
        </div>
      ) : null}

      {error ? (
        <ErrorState
          diagnostic={normalizeDiagnosticError({
            error,
            operation: "Load privacy compliance evidence",
            environment: { locale: language, theme, density },
          })}
          description={t("privacy.evidence.error")}
          onRetry={() => {
            void requirementsQuery.refetch();
            void evidenceQuery.refetch();
          }}
        />
      ) : null}

      {evidenceQuery.data ? (
        <AdminTable
          caption={t("privacy.evidence.title")}
          empty={t("privacy.empty.evidence")}
          rows={evidenceQuery.data}
          columns={[
            {
              id: "requirement",
              header: t("privacy.column.requirement"),
              cell: (row) => `${row.requirementCode} — ${row.requirementTitle}`,
            },
            {
              id: "kind",
              header: t("privacy.column.evidenceKind"),
              cell: (row) => row.evidence.kind,
            },
            {
              id: "label",
              header: t("privacy.column.name"),
              cell: (row) => row.evidence.label,
            },
            {
              id: "reference",
              header: t("privacy.column.reference"),
              cell: (row) => (
                <span className="break-all font-mono text-[length:var(--exits-text-xs)]">
                  {row.evidence.referencePath}
                </span>
              ),
            },
            {
              id: "created",
              header: t("privacy.column.createdUtc"),
              cell: (row) =>
                formatInstant(row.evidence.createdAtUtc, language) ?? row.evidence.createdAtUtc,
            },
          ]}
        />
      ) : null}
    </section>
  );
}
