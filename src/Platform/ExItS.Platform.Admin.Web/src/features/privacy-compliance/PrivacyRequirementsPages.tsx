import { useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import {
  matchesCategorySegment,
  matchesDocuments,
  sortByCode,
} from "@/api/privacy-compliance/privacy-filters";
import type { PrivacyCategorySegment } from "@/api/privacy-compliance/privacy-types";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { PrivacyDisclaimer } from "@/features/privacy-compliance/PrivacyDisclaimer";
import { PrivacyRequirementDrawer } from "@/features/privacy-compliance/PrivacyRequirementDrawer";
import { PrivacyStatusTag } from "@/features/privacy-compliance/PrivacyStatusTag";
import {
  PrivacyAuthLoading,
  PrivacyForbidden,
} from "@/features/privacy-compliance/PrivacyGateStates";
import {
  privacyForbiddenFromError,
  usePrivacyViewGate,
} from "@/features/privacy-compliance/privacy-gate";
import { usePrivacyRequirementsQuery } from "@/features/privacy-compliance/use-privacy-queries";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

type Mode =
  | { kind: "documents" }
  | { kind: "category"; segment: PrivacyCategorySegment };

const CATEGORY_COPY: Record<
  PrivacyCategorySegment,
  { title: MessageKey; description: MessageKey }
> = {
  pias: { title: "privacy.pias.title", description: "privacy.pias.description" },
  "data-inventory": {
    title: "privacy.dataInventory.title",
    description: "privacy.dataInventory.description",
  },
  retention: { title: "privacy.retention.title", description: "privacy.retention.description" },
  incidents: { title: "privacy.incidents.title", description: "privacy.incidents.description" },
  vendors: { title: "privacy.vendors.title", description: "privacy.vendors.description" },
  "dpo-npc": { title: "privacy.dpoNpc.title", description: "privacy.dpoNpc.description" },
};

const DOCUMENT_CATEGORY_OPTIONS = [
  { value: "", labelKey: "privacy.filter.all" as const },
  { value: "CustomerFacing", labelKey: "privacy.category.CustomerFacing" as const },
  { value: "Internal", labelKey: "privacy.category.Internal" as const },
  { value: "RegulatoryReadiness", labelKey: "privacy.category.RegulatoryReadiness" as const },
];

export function PrivacyDocumentsPage() {
  return <PrivacyRequirementsListPage mode={{ kind: "documents" }} />;
}

export function PrivacyCategoryPage({ segment }: { segment: PrivacyCategorySegment }) {
  return <PrivacyRequirementsListPage mode={{ kind: "category", segment }} />;
}

function PrivacyRequirementsListPage({ mode }: { mode: Mode }) {
  const { t, language, theme, density } = usePreferences();
  const { authorization, canView } = usePrivacyViewGate();
  const [searchParams, setSearchParams] = useSearchParams();
  const categoryFromUrl = searchParams.get("category") ?? "";
  const [draftCategory, setDraftCategory] = useState(categoryFromUrl);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [drawerOpen, setDrawerOpen] = useState(false);

  const query = usePrivacyRequirementsQuery(canView);

  const filtered = useMemo(() => {
    if (!query.data) {
      return null;
    }
    const items =
      mode.kind === "documents"
        ? query.data.filter((row) =>
            matchesDocuments(row, categoryFromUrl.trim().length > 0 ? categoryFromUrl : null),
          )
        : query.data.filter((row) => matchesCategorySegment(row, mode.segment));
    return sortByCode(items);
  }, [query.data, mode, categoryFromUrl]);

  if (authorization.status === "loading") {
    return <PrivacyAuthLoading />;
  }
  if (!canView) {
    return <PrivacyForbidden />;
  }
  if (privacyForbiddenFromError(query.error)) {
    return <PrivacyForbidden />;
  }

  const title =
    mode.kind === "documents"
      ? t("privacy.documents.title")
      : t(CATEGORY_COPY[mode.segment].title);
  const description =
    mode.kind === "documents"
      ? t("privacy.documents.description")
      : t(CATEGORY_COPY[mode.segment].description);
  const testId =
    mode.kind === "documents"
      ? "privacy-documents-page"
      : `privacy-category-${mode.segment}-page`;

  return (
    <section className="grid gap-4" data-testid={testId}>
      <PageHeader title={title} description={description} />
      <PrivacyDisclaimer />

      {mode.kind === "documents" ? (
        <form
          className="flex flex-wrap items-end gap-2"
          data-testid="privacy-documents-filters"
          onSubmit={(event) => {
            event.preventDefault();
            const next = new URLSearchParams();
            if (draftCategory.trim().length > 0) {
              next.set("category", draftCategory.trim());
            }
            setSearchParams(next, { replace: true });
          }}
        >
          <label className="grid gap-1 text-[length:var(--exits-text-sm)]" htmlFor="privacy-doc-category">
            {t("privacy.filter.category")}
            <select
              id="privacy-doc-category"
              className="h-9 min-w-[12rem] rounded-[var(--exits-density-radius)] border border-border bg-surface px-2"
              value={draftCategory}
              onChange={(event) => setDraftCategory(event.target.value)}
            >
              {DOCUMENT_CATEGORY_OPTIONS.map((option) => (
                <option key={option.value || "all"} value={option.value}>
                  {t(option.labelKey)}
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
              setDraftCategory("");
              setSearchParams({}, { replace: true });
            }}
          >
            {t("privacy.filter.reset")}
          </Button>
        </form>
      ) : null}

      {query.isPending ? (
        <div role="status" aria-busy="true" aria-label={t("privacy.requirements.loading")}>
          <DashboardWidgetSkeleton />
        </div>
      ) : null}

      {query.isError ? (
        <ErrorState
          diagnostic={normalizeDiagnosticError({
            error: query.error,
            operation: "Load privacy compliance requirements",
            environment: { locale: language, theme, density },
          })}
          description={t("privacy.requirements.error")}
          onRetry={() => void query.refetch()}
        />
      ) : null}

      {filtered ? (
        <AdminTable
          caption={title}
          empty={t("privacy.empty.requirements")}
          rows={filtered}
          columns={[
            {
              id: "code",
              header: t("privacy.column.code"),
              cell: (row) => (
                <span className="font-mono text-[length:var(--exits-text-xs)]">{row.code}</span>
              ),
            },
            {
              id: "title",
              header: t("privacy.column.name"),
              cell: (row) => row.title,
            },
            {
              id: "category",
              header: t("privacy.column.category"),
              cell: (row) => row.category,
            },
            {
              id: "level",
              header: t("privacy.column.level"),
              cell: (row) => <PrivacyStatusTag value={row.requirementLevel} />,
            },
            {
              id: "status",
              header: t("privacy.column.status"),
              cell: (row) => <PrivacyStatusTag value={row.status} />,
            },
            {
              id: "evidence",
              header: t("privacy.column.evidence"),
              cell: (row) => row.evidenceCount,
            },
            {
              id: "actions",
              header: t("privacy.column.actions"),
              cell: (row) => (
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() => {
                    setSelectedId(row.id);
                    setDrawerOpen(true);
                  }}
                >
                  {t("privacy.viewDetails")}
                </Button>
              ),
            },
          ]}
        />
      ) : null}

      <PrivacyRequirementDrawer
        requirementId={selectedId}
        open={drawerOpen}
        onOpenChange={(open) => {
          setDrawerOpen(open);
          if (!open) {
            setSelectedId(null);
          }
        }}
      />
    </section>
  );
}
