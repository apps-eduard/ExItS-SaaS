import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { LayoutDashboard, Search } from "lucide-react";
import {
  canViewDashboard,
  hasOrganizationManagementAuthority,
} from "@/access/pos-capabilities";
import { PageHeader } from "@/components/exits/PageHeader";
import { Button } from "@/components/ui/button";
import { pageBackNav } from "@/navigation/page-back-nav";
import { ReportHubCard, ReportHubCardGrid } from "@/features/reports/ReportHubCard";
import {
  buildReportHubCatalog,
  filterReportHubEntries,
  REPORT_HUB_CATEGORY_LABEL_KEYS,
  type ReportHubCategoryId,
} from "@/features/reports/report-hub-catalog";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { cn } from "@/lib/cn";

export function ReportsHubPage() {
  const { t } = useI18n();
  const { sessionGrant, boundWorkspace } = useWorkspace();
  const showDashboard = canViewDashboard(sessionGrant);
  const canViewPlan = hasOrganizationManagementAuthority(sessionGrant);

  const catalog = useMemo(
    () =>
      buildReportHubCatalog(sessionGrant, {
        branchType: boundWorkspace?.branchType,
      }),
    [sessionGrant, boundWorkspace?.branchType],
  );

  const [category, setCategory] = useState<ReportHubCategoryId>("overview");
  const [search, setSearch] = useState("");

  useEffect(() => {
    if (catalog.categories.length === 0) {
      return;
    }
    if (!catalog.categories.includes(category)) {
      setCategory(catalog.categories[0]!);
    }
  }, [catalog.categories, category]);

  const activeCategory = catalog.categories.includes(category)
    ? category
    : (catalog.categories[0] ?? "overview");

  const searching = search.trim().length > 0;

  const visibleEntries = useMemo(
    () =>
      filterReportHubEntries(
        catalog.entries,
        activeCategory,
        search,
        (entry) => ({
          title: t(entry.titleKey),
          description: t(entry.descriptionKey),
        }),
      ),
    [catalog.entries, activeCategory, search, t],
  );

  return (
    <div className="reports-hub-page exits-page" data-testid="reports-hub-page">
      <PageHeader
        title={t("reports.title")}
        description={t("reports.lede")}
        backTo={pageBackNav.managerHome.to}
        backLabel={t(pageBackNav.managerHome.labelKey)}
        backTestId="page-header-back-reports"
      />

      {showDashboard ? (
        <section className="reports-hub-section" data-testid="reports-dashboard-section">
          <ReportHubCardGrid testId="reports-dashboard-grid" className="reports-hub-grid--featured">
            <ReportHubCard
              to="/dashboard"
              title={t("dashboard.open")}
              description={t("reports.hub.dashboardDetail")}
              icon={LayoutDashboard}
              testId="reports-open-dashboard"
              featured
            />
          </ReportHubCardGrid>
        </section>
      ) : null}

      <div className="reports-hub-toolbar" data-testid="reports-hub-toolbar">
        <label className="reports-hub-search">
          <Search className="reports-hub-search__icon size-4 shrink-0" aria-hidden />
          <span className="sr-only">{t("reports.hub.searchLabel")}</span>
          <input
            type="search"
            className="reports-hub-search__input exits-input"
            placeholder={t("reports.hub.searchPlaceholder")}
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            data-testid="reports-hub-search"
            autoComplete="off"
          />
        </label>

        {!searching && catalog.categories.length > 0 ? (
          <div
            className="reports-hub-categories"
            role="tablist"
            aria-label={t("reports.hub.categoriesLabel")}
            data-testid="reports-hub-categories"
          >
            {catalog.categories.map((id) => {
              const selected = id === activeCategory;
              return (
                <button
                  key={id}
                  type="button"
                  role="tab"
                  aria-selected={selected}
                  className={cn(
                    "reports-hub-category",
                    selected && "reports-hub-category--active",
                  )}
                  data-testid={`reports-hub-category-${id}`}
                  onClick={() => setCategory(id)}
                >
                  {t(REPORT_HUB_CATEGORY_LABEL_KEYS[id])}
                </button>
              );
            })}
          </div>
        ) : null}
      </div>

      {catalog.showAdvancedUpgrade ? (
        <aside className="reports-hub-upgrade" data-testid="reports-hub-upgrade">
          <div className="reports-hub-upgrade__copy">
            <p className="reports-hub-upgrade__title m-0">{t("reports.hub.upgradeTitle")}</p>
            <p className="reports-hub-upgrade__detail m-0">{t("reports.hub.upgradeDetail")}</p>
          </div>
          {canViewPlan ? (
            <Button asChild variant="outline" data-testid="reports-hub-view-plan">
              <Link to="/org">{t("reports.hub.viewPlan")}</Link>
            </Button>
          ) : null}
        </aside>
      ) : null}

      <section
        className="reports-hub-section"
        data-testid={searching ? "reports-hub-search-results" : `reports-group-${activeCategory}`}
      >
        {searching ? (
          <h2 className="reports-hub-section__title exits-type-section-title m-0">
            {t("reports.hub.searchResults")}
          </h2>
        ) : null}

        {visibleEntries.length === 0 ? (
          <p
            className="m-0 text-[length:var(--exits-text-sm)] text-muted"
            data-testid="reports-hub-empty"
          >
            {searching ? t("reports.hub.searchEmpty") : t("reports.hub.categoryEmpty")}
          </p>
        ) : (
          <ReportHubCardGrid
            testId={searching ? "reports-search-grid" : `reports-grid-${activeCategory}`}
          >
            {visibleEntries.map((entry) => (
              <ReportHubCard
                key={entry.id}
                to={entry.path}
                title={t(entry.titleKey)}
                description={t(entry.descriptionKey)}
                icon={entry.icon}
                testId={entry.testId}
              />
            ))}
          </ReportHubCardGrid>
        )}
      </section>
    </div>
  );
}
