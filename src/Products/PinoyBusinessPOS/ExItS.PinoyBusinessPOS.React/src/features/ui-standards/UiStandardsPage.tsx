import { useCallback, useEffect, useMemo, useState } from "react";
import { Navigate, useSearchParams } from "react-router-dom";
import { isFrontendLocalValidationMode } from "@/api/platform/local-validation-gate";
import { EmptyState } from "@/components/exits/EmptyState";
import { ExitsTabs } from "@/components/exits/ExitsTabs";
import { FilterChip } from "@/components/exits/FilterChip";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";
import { UiStandardsActionChipsPanel } from "@/features/ui-standards/UiStandardsActionChipsPanel";
import { UiStandardsButtonsPanel } from "@/features/ui-standards/UiStandardsButtonsPanel";
import { UiStandardsCardsPanel } from "@/features/ui-standards/UiStandardsCardsPanel";
import { UiStandardsChipsPanel } from "@/features/ui-standards/UiStandardsChipsPanel";
import { UiStandardsDisclosureToolbar } from "@/features/ui-standards/UiStandardsDisclosureToolbar";
import { UiStandardsModuleSubnavPanel } from "@/features/ui-standards/UiStandardsModuleSubnavPanel";
import { UiStandardsSimpleCatalog } from "@/features/ui-standards/UiStandardsSimpleCatalog";
import { UiStandardsTablesPanel } from "@/features/ui-standards/UiStandardsTablesPanel";
import { UiStandardsTabsPanel } from "@/features/ui-standards/UiStandardsTabsPanel";
import { UiStandardCatalogTable } from "@/features/ui-standards/UiStandardCatalogTable";
import { UiStandardLiveSamples } from "@/features/ui-standards/UiStandardLiveSamples";
import { useUiStandardsDisclosure } from "@/features/ui-standards/useUiStandardsDisclosure";
import type { UiStandardsTab } from "@/features/ui-standards/ui-standards-disclosure";
import {
  filterCatalogRows,
  filterDetailedPanels,
  filterLiveCards,
  parseUiStandardCategoryParam,
  UI_STANDARD_FILTER_OPTIONS,
  type UiStandardFilterCategory,
  type UiStandardsDetailedTab,
} from "@/features/ui-standards/ui-standard-catalog";
import {
  parseUiStandardsViewParam,
  readUiStandardsView,
  writeUiStandardsView,
  type UiStandardsViewMode,
} from "@/features/ui-standards/ui-standards-view";

function resolveView(search: string): UiStandardsViewMode {
  return parseUiStandardsViewParam(search) ?? readUiStandardsView();
}

/**
 * Canonical ExItS UI Standard page — live samples + reused classic catalog.
 * Route: /ui-standards ( /ui-standard aliases here ).
 */
export function UiStandardsPage() {
  const { t } = useI18n();
  const [searchParams, setSearchParams] = useSearchParams();
  const [view, setViewState] = useState<UiStandardsViewMode>(() =>
    resolveView(searchParams.toString()),
  );
  const [query, setQuery] = useState(() => searchParams.get("q") ?? "");
  const [category, setCategory] = useState<UiStandardFilterCategory>(() =>
    parseUiStandardCategoryParam(searchParams.get("category")),
  );
  const [tab, setTab] = useState<UiStandardsTab>("tables");
  const { isOpen, setOpen, expandAll, collapseAll, resetLayout } = useUiStandardsDisclosure();

  useEffect(() => {
    setViewState(resolveView(searchParams.toString()));
    setQuery(searchParams.get("q") ?? "");
    setCategory(parseUiStandardCategoryParam(searchParams.get("category")));
  }, [searchParams]);

  const syncParams = useCallback(
    (next: { q?: string; category?: UiStandardFilterCategory; view?: UiStandardsViewMode }) => {
      setSearchParams(
        (prev) => {
          const params = new URLSearchParams(prev);
          const q = next.q !== undefined ? next.q : params.get("q") ?? "";
          const cat =
            next.category !== undefined
              ? next.category
              : parseUiStandardCategoryParam(params.get("category"));
          const v = next.view !== undefined ? next.view : resolveView(params.toString());

          if (q.trim()) params.set("q", q.trim());
          else params.delete("q");

          if (cat !== "all") params.set("category", cat);
          else params.delete("category");

          params.set("view", v);
          return params;
        },
        { replace: true },
      );
    },
    [setSearchParams],
  );

  const setView = useCallback(
    (next: UiStandardsViewMode) => {
      setViewState(next);
      writeUiStandardsView(next);
      syncParams({ view: next });
    },
    [syncParams],
  );

  const setCategoryFilter = useCallback(
    (next: UiStandardFilterCategory) => {
      setCategory(next);
      if (next === "data") {
        setTab("tables");
        if (view !== "classic") {
          setViewState("classic");
          writeUiStandardsView("classic");
          syncParams({ category: next, view: "classic" });
          return;
        }
      }
      syncParams({ category: next });
    },
    [syncParams, view],
  );

  const setSearchQuery = useCallback(
    (next: string) => {
      setQuery(next);
      syncParams({ q: next });
    },
    [syncParams],
  );

  const clearFilters = useCallback(() => {
    setQuery("");
    setCategory("all");
    syncParams({ q: "", category: "all" });
  }, [syncParams]);

  const visibleLiveCards = useMemo(
    () => filterLiveCards(category, query),
    [category, query],
  );
  const visibleDetailedPanels = useMemo(
    () => filterDetailedPanels(category, query),
    [category, query],
  );
  const visibleCatalogRows = useMemo(
    () => filterCatalogRows(category, query),
    [category, query],
  );

  const visibleLiveIds = useMemo(
    () => new Set(visibleLiveCards.map((c) => c.id)),
    [visibleLiveCards],
  );

  const detailedTabs = useMemo(() => {
    const tabs = visibleDetailedPanels.map((p) => p.tab);
    return tabs;
  }, [visibleDetailedPanels]);

  useEffect(() => {
    if (detailedTabs.length === 0) return;
    if (!detailedTabs.includes(tab as UiStandardsDetailedTab)) {
      setTab(detailedTabs[0]!);
    }
  }, [detailedTabs, tab]);

  useEffect(() => {
    if (category !== "data") {
      return;
    }
    setTab("tables");
    setOpen("tables.demo", true);
  }, [category, setOpen]);

  const filtersActive = category !== "all" || query.trim().length > 0;
  const hasLive = visibleLiveCards.length > 0;
  /** Data filter always surfaces the locked ExitsTable demo (classic Tables panel). */
  const forceExitsTableDemo = category === "data" || detailedTabs.includes("tables");
  const hasDetailed =
    detailedTabs.length > 0 && (view === "classic" || category === "data");
  const hasCatalog = visibleCatalogRows.length > 0;
  const hasAnyResult =
    hasLive ||
    hasCatalog ||
    hasDetailed ||
    forceExitsTableDemo ||
    (view === "simple" && !filtersActive);

  if (!isFrontendLocalValidationMode()) {
    return <Navigate to="/" replace />;
  }

  return (
    <div
      className="exits-page flex min-w-0 flex-col gap-3"
      data-testid="ui-standards-page"
      data-view={view}
    >
      <PageHeader
        title="ExItS UI Standard"
        description="Live reference for shared components, interaction patterns, and design standards."
      />
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="ui-standard-global-note">
        Changes to global components should be reflected here and throughout the application.
      </p>

      <div
        className="sticky top-0 z-20 -mx-1 flex flex-col gap-2 border-b border-border bg-[color-mix(in_srgb,var(--exits-bg)_92%,transparent)] px-1 py-2 backdrop-blur-md"
        data-testid="ui-standards-filter-bar"
      >
        <SearchField
          label="Search UI standards"
          value={query}
          onChange={(e) => setSearchQuery(e.target.value)}
          onClear={() => setSearchQuery("")}
          placeholder="Search UI standards..."
          testId="ui-standards-search"
        />
        <div
          className="flex min-w-0 flex-wrap gap-2 overflow-x-auto pb-0.5"
          role="group"
          aria-label="UI standard categories"
          data-testid="ui-standards-category-filters"
        >
          {UI_STANDARD_FILTER_OPTIONS.map((opt) => (
            <FilterChip
              key={opt.id}
              selected={category === opt.id}
              onClick={() => setCategoryFilter(opt.id)}
              data-testid={`ui-standards-filter-${opt.id}`}
            >
              {opt.label}
            </FilterChip>
          ))}
        </div>
      </div>

      <div className="flex min-w-0 flex-wrap items-center gap-2" data-testid="ui-standards-view-switcher">
        <ExitsTabs
          variant="segmented"
          layout="content"
          ariaLabel={t("uiStandards.viewSwitcherAria")}
          testId="ui-standards-view-tabs"
          value={view}
          onValueChange={(key) => setView(key as UiStandardsViewMode)}
          items={[
            { key: "classic", label: t("uiStandards.viewClassic"), testId: "ui-standards-view-classic" },
            { key: "simple", label: t("uiStandards.viewSimple"), testId: "ui-standards-view-simple" },
          ]}
        />
      </div>

      {!hasAnyResult ? (
        <EmptyState
          title="No UI standards found"
          detail="Try another search or filter."
          size="compact"
          testId="ui-standards-empty"
          action={
            <Button
              type="button"
              variant="outline"
              data-testid="ui-standards-clear-filters"
              onClick={clearFilters}
            >
              Clear filters
            </Button>
          }
        />
      ) : (
        <>
          {hasLive ? (
            <section className="flex min-w-0 flex-col gap-2" data-testid="ui-standards-live-section">
              <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">Live samples</h2>
              <UiStandardLiveSamples visibleCardIds={visibleLiveIds} />
            </section>
          ) : null}

          {hasCatalog ? <UiStandardCatalogTable rows={visibleCatalogRows} /> : null}

          {category === "data" ? (
            <section
              className="flex min-w-0 flex-col gap-2"
              data-testid="ui-standards-data-exits-table"
            >
              <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
                ExitsTable reference
              </h2>
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                Approved / locked full table: search, filter, sort, multi-select, output icons,
                pagination, actions, and field-menu inline editing.
              </p>
              <UiStandardsTablesPanel isOpen={isOpen} setOpen={setOpen} />
            </section>
          ) : null}

          {view === "simple" && category !== "data" ? (
            !filtersActive || category === "all" ? (
              <UiStandardsSimpleCatalog />
            ) : null
          ) : hasDetailed && category !== "data" ? (
            <>
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("uiStandards.prefsHint")}</p>
              <p
                className="m-0 text-[length:var(--exits-text-sm)] text-muted"
                data-testid="ui-standards-copy-hint"
              >
                {t("uiStandards.copyCommandsHint")}
              </p>

              <div
                className="sticky top-[4.5rem] z-10 -mx-1 flex flex-col gap-2 border-b border-border bg-[color-mix(in_srgb,var(--exits-bg)_92%,transparent)] px-1 py-2 backdrop-blur-md"
                data-testid="ui-standards-sticky-nav"
              >
                <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
                  Detailed catalog
                </h2>
                <UnderlineTabBar
                  ariaLabel={t("uiStandards.sections")}
                  testId="ui-standards-tabs"
                  activeKey={tab}
                  onChange={(key) => setTab(key as UiStandardsTab)}
                  items={detailedTabs.map((key) => ({
                    key,
                    label: t(
                      key === "tables"
                        ? "uiStandards.tabTables"
                        : key === "buttons"
                          ? "uiStandards.tabButtons"
                          : key === "chips"
                            ? "uiStandards.tabChips"
                            : key === "tabs"
                              ? "uiStandards.tabTabs"
                              : key === "module-subnav"
                                ? "uiStandards.tabModuleSubnav"
                                : key === "action-chips"
                                  ? "uiStandards.tabActionChips"
                                  : "uiStandards.tabCards",
                    ),
                    testId: `ui-standards-tab-${key}`,
                  }))}
                />
                <UiStandardsDisclosureToolbar
                  onExpandAll={() => expandAll(tab)}
                  onCollapseAll={() => collapseAll(tab)}
                  onReset={resetLayout}
                  expandLabel={t("uiStandards.expandAll")}
                  collapseLabel={t("uiStandards.collapseAll")}
                  resetLabel={t("uiStandards.resetLayout")}
                />
              </div>

              {tab === "tables" && detailedTabs.includes("tables") ? (
                <UiStandardsTablesPanel isOpen={isOpen} setOpen={setOpen} />
              ) : null}
              {tab === "buttons" && detailedTabs.includes("buttons") ? (
                <UiStandardsButtonsPanel isOpen={isOpen} setOpen={setOpen} />
              ) : null}
              {tab === "chips" && detailedTabs.includes("chips") ? (
                <UiStandardsChipsPanel isOpen={isOpen} setOpen={setOpen} />
              ) : null}
              {tab === "tabs" && detailedTabs.includes("tabs") ? (
                <UiStandardsTabsPanel isOpen={isOpen} setOpen={setOpen} />
              ) : null}
              {tab === "module-subnav" && detailedTabs.includes("module-subnav") ? (
                <UiStandardsModuleSubnavPanel isOpen={isOpen} setOpen={setOpen} />
              ) : null}
              {tab === "action-chips" && detailedTabs.includes("action-chips") ? (
                <UiStandardsActionChipsPanel isOpen={isOpen} setOpen={setOpen} />
              ) : null}
              {tab === "cards" && detailedTabs.includes("cards") ? (
                <UiStandardsCardsPanel isOpen={isOpen} setOpen={setOpen} />
              ) : null}
            </>
          ) : null}
        </>
      )}
    </div>
  );
}
