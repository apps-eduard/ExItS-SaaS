import { useMemo, useRef, useState, type KeyboardEvent } from "react";
import { Link } from "react-router-dom";
import { isImportantGap } from "@/api/privacy-compliance/privacy-filters";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import {
  PrivacyDisclaimer,
  PrivacyReadinessBanner,
} from "@/features/privacy-compliance/PrivacyDisclaimer";
import { PrivacyStatusTag } from "@/features/privacy-compliance/PrivacyStatusTag";
import {
  PrivacyAuthLoading,
  PrivacyForbidden,
} from "@/features/privacy-compliance/PrivacyGateStates";
import {
  privacyForbiddenFromError,
  usePrivacyViewGate,
} from "@/features/privacy-compliance/privacy-gate";
import {
  usePrivacyOverviewQuery,
  usePrivacyRequirementsQuery,
} from "@/features/privacy-compliance/use-privacy-queries";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";
import { cn } from "@/lib/utils";

const TECHNICAL_LABELS: Record<string, MessageKey> = {
  Implemented: "privacy.technical.Implemented",
  Partial: "privacy.technical.Partial",
  Unavailable: "privacy.technical.Unavailable",
};

const LEGAL_LABELS: Record<string, MessageKey> = {
  Required: "privacy.legal.Required",
  InProgress: "privacy.legal.InProgress",
  Complete: "privacy.legal.Complete",
};

const NPC_LABELS: Record<string, MessageKey> = {
  NotVerified: "privacy.npc.NotVerified",
  Pending: "privacy.npc.Pending",
  Verified: "privacy.npc.Verified",
};

type PrivacyOverviewTab = "category" | "pia" | "gaps";

const PRIVACY_OVERVIEW_TABS: readonly {
  id: PrivacyOverviewTab;
  labelKey: MessageKey;
  panelId: string;
  tabId: string;
}[] = [
  {
    id: "category",
    labelKey: "privacy.categoryReadiness.title",
    panelId: "privacy-overview-panel-category",
    tabId: "privacy-overview-tab-category",
  },
  {
    id: "pia",
    labelKey: "privacy.piaFollowUps.title",
    panelId: "privacy-overview-panel-pia",
    tabId: "privacy-overview-tab-pia",
  },
  {
    id: "gaps",
    labelKey: "privacy.importantGaps.title",
    panelId: "privacy-overview-panel-gaps",
    tabId: "privacy-overview-tab-gaps",
  },
];

function formatInstant(value: string | null | undefined, language: string): string | null {
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

export function PrivacyOverviewPage() {
  const { t, language, theme, density } = usePreferences();
  const { authorization, canView } = usePrivacyViewGate();
  const overviewQuery = usePrivacyOverviewQuery(canView);
  const requirementsQuery = usePrivacyRequirementsQuery(canView);
  const [activeTab, setActiveTab] = useState<PrivacyOverviewTab>("category");
  const tabRefs = useRef<Partial<Record<PrivacyOverviewTab, HTMLButtonElement | null>>>({});

  const gaps = useMemo(() => {
    if (!requirementsQuery.data) {
      return null;
    }
    return requirementsQuery.data.filter(isImportantGap);
  }, [requirementsQuery.data]);

  const focusTab = (id: PrivacyOverviewTab) => {
    setActiveTab(id);
    queueMicrotask(() => {
      tabRefs.current[id]?.focus();
    });
  };

  const onTabListKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const index = PRIVACY_OVERVIEW_TABS.findIndex((tab) => tab.id === activeTab);
    if (index < 0) {
      return;
    }
    if (event.key === "ArrowRight" || event.key === "ArrowDown") {
      event.preventDefault();
      const next = PRIVACY_OVERVIEW_TABS[(index + 1) % PRIVACY_OVERVIEW_TABS.length]!;
      focusTab(next.id);
      return;
    }
    if (event.key === "ArrowLeft" || event.key === "ArrowUp") {
      event.preventDefault();
      const prev =
        PRIVACY_OVERVIEW_TABS[(index - 1 + PRIVACY_OVERVIEW_TABS.length) % PRIVACY_OVERVIEW_TABS.length]!;
      focusTab(prev.id);
      return;
    }
    if (event.key === "Home") {
      event.preventDefault();
      focusTab(PRIVACY_OVERVIEW_TABS[0]!.id);
      return;
    }
    if (event.key === "End") {
      event.preventDefault();
      focusTab(PRIVACY_OVERVIEW_TABS[PRIVACY_OVERVIEW_TABS.length - 1]!.id);
    }
  };

  if (authorization.status === "loading") {
    return <PrivacyAuthLoading />;
  }
  if (!canView) {
    return <PrivacyForbidden />;
  }
  if (privacyForbiddenFromError(overviewQuery.error)) {
    return <PrivacyForbidden />;
  }

  const data = overviewQuery.data;
  const activeMeta = PRIVACY_OVERVIEW_TABS.find((tab) => tab.id === activeTab)!;

  return (
    <section className="grid gap-4" data-testid="privacy-overview-page">
      <PageHeader title={t("privacy.overview.title")} description={t("privacy.overview.description")} />
      <PrivacyReadinessBanner />
      <PrivacyDisclaimer />

      {overviewQuery.isPending ? (
        <div role="status" aria-busy="true" aria-label={t("privacy.overview.loading")}>
          <DashboardWidgetSkeleton />
        </div>
      ) : null}

      {overviewQuery.isError ? (
        <ErrorState
          diagnostic={normalizeDiagnosticError({
            error: overviewQuery.error,
            operation: "Load privacy compliance overview",
            environment: { locale: language, theme, density },
          })}
          description={t("privacy.overview.error")}
          onRetry={() => void overviewQuery.refetch()}
        />
      ) : null}

      {data ? (
        <>
          <DashboardSection title={t("privacy.readiness.label")}>
            <div className="mb-3">
              <PrivacyStatusTag value={data.overallReadiness} />
            </div>
            <dl className="grid gap-2 sm:grid-cols-2">
              <div>
                <dt className="text-[length:var(--exits-text-xs)] text-muted">
                  {t("privacy.readiness.technical")}
                </dt>
                <dd className="font-medium">
                  {t(TECHNICAL_LABELS[data.technicalSafeguardsSummary] ?? "privacy.technical.Partial")}
                </dd>
              </div>
              <div>
                <dt className="text-[length:var(--exits-text-xs)] text-muted">
                  {t("privacy.readiness.governance")}
                </dt>
                <dd className="font-medium">{data.governanceDocumentationSummary}</dd>
              </div>
              <div>
                <dt className="text-[length:var(--exits-text-xs)] text-muted">
                  {t("privacy.readiness.legal")}
                </dt>
                <dd className="font-medium">
                  {t(LEGAL_LABELS[data.legalReviewSummary] ?? "privacy.legal.Required")}
                </dd>
              </div>
              <div>
                <dt className="text-[length:var(--exits-text-xs)] text-muted">
                  {t("privacy.readiness.npc")}
                </dt>
                <dd className="font-medium">
                  {t(NPC_LABELS[data.npcVerificationSummary] ?? "privacy.npc.NotVerified")}
                </dd>
              </div>
            </dl>
          </DashboardSection>

          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6">
            <DashboardSection title={t("privacy.metric.totalRequirements")} variant="metric">
              <p className="text-[length:var(--exits-text-xl)] font-semibold">{data.totalRequirements}</p>
            </DashboardSection>
            <DashboardSection title={t("privacy.metric.ready")} variant="metric">
              <p className="text-[length:var(--exits-text-xl)] font-semibold">{data.readyCount}</p>
            </DashboardSection>
            <DashboardSection title={t("privacy.metric.actionNeeded")} variant="metric">
              <p className="text-[length:var(--exits-text-xl)] font-semibold">{data.actionNeededCount}</p>
            </DashboardSection>
            <DashboardSection title={t("privacy.metric.externalLegal")} variant="metric">
              <p className="text-[length:var(--exits-text-xl)] font-semibold">
                {data.externalLegalReviewCount}
              </p>
            </DashboardSection>
            <DashboardSection title={t("privacy.metric.evidenceCoverage")} variant="metric">
              <p className="text-[length:var(--exits-text-xl)] font-semibold">
                {data.requirementsWithEvidenceCount} / {data.totalRequirements}
              </p>
            </DashboardSection>
            <DashboardSection title={t("privacy.metric.totalSystems")} variant="metric">
              <p className="text-[length:var(--exits-text-xl)] font-semibold">{data.totalSystems}</p>
            </DashboardSection>
          </div>

          {data.lastUpdatedUtc ? (
            <p className="text-[length:var(--exits-text-xs)] text-muted">
              {t("privacy.lastUpdated")}:{" "}
              {formatInstant(data.lastUpdatedUtc, language) ?? data.lastUpdatedUtc}
            </p>
          ) : null}

          <div
            className="rounded-[var(--exits-radius-md)] border border-border bg-surface"
            data-testid="privacy-overview-tabs"
          >
            <div
              role="tablist"
              aria-label={t("privacy.overview.title")}
              data-testid="privacy-overview-tablist"
              className="-mx-px flex gap-1 overflow-x-auto overscroll-x-contain border-b border-border px-2 pt-2 [scrollbar-width:thin]"
              onKeyDown={onTabListKeyDown}
            >
              {PRIVACY_OVERVIEW_TABS.map((tab) => {
                const selected = activeTab === tab.id;
                return (
                  <button
                    key={tab.id}
                    ref={(node) => {
                      tabRefs.current[tab.id] = node;
                    }}
                    type="button"
                    role="tab"
                    id={tab.tabId}
                    data-testid={tab.tabId}
                    aria-selected={selected}
                    aria-controls={tab.panelId}
                    tabIndex={selected ? 0 : -1}
                    className={cn(
                      "min-h-11 shrink-0 whitespace-nowrap rounded-t-[var(--exits-radius-md)] border-b-2 px-3 py-2 text-[length:var(--exits-text-sm)] font-semibold transition-[color,border-color,background-color] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-focus-ring)]",
                      selected
                        ? "border-primary text-foreground"
                        : "border-transparent text-muted hover:bg-[var(--exits-surface-muted)] hover:text-foreground",
                    )}
                    onClick={() => setActiveTab(tab.id)}
                  >
                    {t(tab.labelKey)}
                  </button>
                );
              })}
            </div>

            <div
              role="tabpanel"
              id={activeMeta.panelId}
              data-testid={activeMeta.panelId}
              aria-labelledby={activeMeta.tabId}
              className="p-4"
            >
              {activeTab === "category" ? (
                data.categorySummaries == null || data.categorySummaries.length === 0 ? (
                  <p className="text-[length:var(--exits-text-sm)] text-muted">{t("privacy.empty.data")}</p>
                ) : (
                  <AdminTable
                    caption={t("privacy.categoryReadiness.title")}
                    empty={t("privacy.empty.data")}
                    rows={data.categorySummaries.map((row) => ({
                      ...row,
                      id: `${row.group}:${row.detailRoute}`,
                    }))}
                    columns={[
                      {
                        id: "group",
                        header: t("privacy.column.category"),
                        cell: (row) => row.group,
                      },
                      {
                        id: "status",
                        header: t("privacy.column.status"),
                        cell: (row) => <PrivacyStatusTag value={row.status} />,
                      },
                      {
                        id: "count",
                        header: t("privacy.column.count"),
                        cell: (row) => row.requirementCount,
                      },
                      {
                        id: "ready",
                        header: t("privacy.metric.ready"),
                        cell: (row) => row.readyCount,
                      },
                      {
                        id: "action",
                        header: t("privacy.metric.actionNeeded"),
                        cell: (row) => row.actionNeededCount,
                      },
                      {
                        id: "evidence",
                        header: t("privacy.metric.evidenceCoverage"),
                        cell: (row) => `${row.evidenceCoveredCount} / ${row.requirementCount}`,
                      },
                      {
                        id: "reviewed",
                        header: t("privacy.lastReviewed"),
                        cell: (row) => row.lastReviewedDate ?? t("privacy.notAssessed"),
                      },
                      {
                        id: "link",
                        header: t("privacy.column.actions"),
                        cell: (row) => (
                          <Link
                            className="text-primary underline-offset-4 hover:underline"
                            to={row.detailRoute}
                          >
                            {t("privacy.viewDetails")}
                          </Link>
                        ),
                      },
                    ]}
                  />
                )
              ) : null}

              {activeTab === "pia" ? (
                data.privacyImpactFollowUps == null || data.privacyImpactFollowUps.length === 0 ? (
                  <p className="text-[length:var(--exits-text-sm)] text-muted">
                    {t("privacy.empty.piaFollowUps")}
                  </p>
                ) : (
                  <AdminTable
                    caption={t("privacy.piaFollowUps.title")}
                    empty={t("privacy.empty.piaFollowUps")}
                    rows={data.privacyImpactFollowUps.map((row) => ({
                      ...row,
                      id: row.code,
                    }))}
                    columns={[
                      {
                        id: "title",
                        header: t("privacy.column.name"),
                        cell: (row) => row.title,
                      },
                      {
                        id: "code",
                        header: t("privacy.column.code"),
                        cell: (row) => (
                          <span className="font-mono text-[length:var(--exits-text-xs)] text-muted">
                            {row.code}
                          </span>
                        ),
                      },
                      {
                        id: "status",
                        header: t("privacy.column.status"),
                        cell: (row) => <PrivacyStatusTag value={row.status} />,
                      },
                      {
                        id: "legal",
                        header: t("privacy.legalReviewRequired"),
                        cell: (row) =>
                          row.requiresDpoLegalVerification ? t("privacy.yes") : t("privacy.no"),
                      },
                      {
                        id: "evidence",
                        header: t("privacy.column.evidence"),
                        cell: (row) => row.evidenceCount,
                      },
                      {
                        id: "link",
                        header: t("privacy.column.actions"),
                        cell: () => (
                          <Link
                            className="text-primary underline-offset-4 hover:underline"
                            to="/admin/privacy-compliance/pias"
                          >
                            {t("privacy.viewDetails")}
                          </Link>
                        ),
                      },
                    ]}
                  />
                )
              ) : null}

              {activeTab === "gaps" ? (
                <>
                  {requirementsQuery.isPending ? (
                    <div role="status" aria-busy="true">
                      <DashboardWidgetSkeleton />
                    </div>
                  ) : null}
                  {requirementsQuery.isError ? (
                    privacyForbiddenFromError(requirementsQuery.error) ? (
                      <PrivacyForbidden />
                    ) : (
                      <ErrorState
                        diagnostic={normalizeDiagnosticError({
                          error: requirementsQuery.error,
                          operation: "Load privacy compliance requirements for gaps",
                          environment: { locale: language, theme, density },
                        })}
                        description={t("privacy.importantGaps.error")}
                        onRetry={() => void requirementsQuery.refetch()}
                      />
                    )
                  ) : null}
                  {gaps ? (
                    gaps.length === 0 ? (
                      <p className="text-[length:var(--exits-text-sm)] text-muted">
                        {t("privacy.empty.gaps")}
                      </p>
                    ) : (
                      <AdminTable
                        caption={t("privacy.importantGaps.title")}
                        empty={t("privacy.empty.gaps")}
                        rows={gaps}
                        columns={[
                          {
                            id: "title",
                            header: t("privacy.column.name"),
                            cell: (row) => row.title,
                          },
                          {
                            id: "code",
                            header: t("privacy.column.code"),
                            cell: (row) => (
                              <span className="font-mono text-[length:var(--exits-text-xs)] text-muted">
                                {row.code}
                              </span>
                            ),
                          },
                          {
                            id: "status",
                            header: t("privacy.column.status"),
                            cell: (row) => <PrivacyStatusTag value={row.status} />,
                          },
                          {
                            id: "link",
                            header: t("privacy.column.actions"),
                            cell: () => (
                              <Link
                                className="text-primary underline-offset-4 hover:underline"
                                to="/admin/privacy-compliance/documents"
                              >
                                {t("privacy.viewDetails")}
                              </Link>
                            ),
                          },
                        ]}
                      />
                    )
                  ) : null}
                </>
              ) : null}
            </div>
          </div>

          <div className="grid gap-4 lg:grid-cols-2">
            <DashboardSection title={t("privacy.byStatus.title")}>
              {Object.keys(data.requirementsByStatus).length === 0 ? (
                <p className="text-[length:var(--exits-text-sm)] text-muted">{t("privacy.empty.data")}</p>
              ) : (
                <AdminTable
                  caption={t("privacy.byStatus.title")}
                  empty={t("privacy.empty.data")}
                  rows={Object.entries(data.requirementsByStatus)
                    .sort(([a], [b]) => a.localeCompare(b, undefined, { sensitivity: "base" }))
                    .map(([status, count]) => ({ id: status, status, count }))}
                  columns={[
                    {
                      id: "status",
                      header: t("privacy.column.status"),
                      cell: (row) => <PrivacyStatusTag value={row.status} />,
                    },
                    {
                      id: "count",
                      header: t("privacy.column.count"),
                      cell: (row) => row.count,
                    },
                  ]}
                />
              )}
            </DashboardSection>

            <DashboardSection title={t("privacy.quickLinks.title")}>
              <div className="grid gap-2">
                {(
                  [
                    ["/admin/privacy-compliance/documents", "privacy.nav.documents"],
                    ["/admin/privacy-compliance/systems", "privacy.nav.systems"],
                    ["/admin/privacy-compliance/pias", "privacy.nav.pias"],
                    ["/admin/privacy-compliance/evidence", "privacy.nav.evidence"],
                    ["/admin/privacy-compliance/dpo-npc", "privacy.nav.dpoNpc"],
                  ] as const
                ).map(([to, key]) => (
                  <Button key={to} asChild variant="outline" className="justify-start">
                    <Link to={to}>{t(key)}</Link>
                  </Button>
                ))}
              </div>
            </DashboardSection>
          </div>
        </>
      ) : null}
    </section>
  );
}
