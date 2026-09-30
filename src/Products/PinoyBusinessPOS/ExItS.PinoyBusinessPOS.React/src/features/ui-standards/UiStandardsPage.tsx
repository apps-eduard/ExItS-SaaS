import { useCallback, useEffect, useMemo, useState } from "react";
import { Navigate, useSearchParams } from "react-router-dom";
import { isFrontendLocalValidationMode } from "@/api/platform/local-validation-gate";
import { EmptyState } from "@/components/exits/EmptyState";
import { FilterChip } from "@/components/exits/FilterChip";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { Button } from "@/components/ui/button";
import { UiStandardsTablesPanel } from "@/features/ui-standards/UiStandardsTablesPanel";
import { UiStandardCatalogTable } from "@/features/ui-standards/UiStandardCatalogTable";
import { UiStandardLiveSamples } from "@/features/ui-standards/UiStandardLiveSamples";
import { UiStandardsPreferencePreview } from "@/features/ui-standards/UiStandardsPreferencePreview";
import { useUiStandardsDisclosure } from "@/features/ui-standards/useUiStandardsDisclosure";
import {
  filterCatalogRows,
  filterLiveCards,
  parseUiStandardCategoryParam,
  UI_STANDARD_FILTER_OPTIONS,
  type UiStandardFilterCategory,
} from "@/features/ui-standards/ui-standard-catalog";

/**
 * Canonical ExItS UI Standard page — live samples + catalog + locked ExitsTable reference.
 * Route: /ui-standards ( /ui-standard aliases here ).
 * Classic / Simple modes removed (TASK-66D).
 */
export function UiStandardsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [query, setQuery] = useState(() => searchParams.get("q") ?? "");
  const [category, setCategory] = useState<UiStandardFilterCategory>(() =>
    parseUiStandardCategoryParam(searchParams.get("category")),
  );
  const { isOpen, setOpen } = useUiStandardsDisclosure();

  useEffect(() => {
    setQuery(searchParams.get("q") ?? "");
    setCategory(parseUiStandardCategoryParam(searchParams.get("category")));
  }, [searchParams]);

  const syncParams = useCallback(
    (next: { q?: string; category?: UiStandardFilterCategory }) => {
      setSearchParams(
        (prev) => {
          const params = new URLSearchParams(prev);
          const q = next.q !== undefined ? next.q : params.get("q") ?? "";
          const cat =
            next.category !== undefined
              ? next.category
              : parseUiStandardCategoryParam(params.get("category"));

          if (q.trim()) params.set("q", q.trim());
          else params.delete("q");

          if (cat !== "all") params.set("category", cat);
          else params.delete("category");

          // Drop obsolete Classic/Simple view param if present.
          params.delete("view");
          return params;
        },
        { replace: true },
      );
    },
    [setSearchParams],
  );

  const setCategoryFilter = useCallback(
    (next: UiStandardFilterCategory) => {
      setCategory(next);
      syncParams({ category: next });
    },
    [syncParams],
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
  const visibleCatalogRows = useMemo(
    () => filterCatalogRows(category, query),
    [category, query],
  );

  const visibleLiveIds = useMemo(
    () => new Set(visibleLiveCards.map((c) => c.id)),
    [visibleLiveCards],
  );

  useEffect(() => {
    if (category !== "data") {
      return;
    }
    setOpen("tables.demo", true);
  }, [category, setOpen]);

  const hasLive = visibleLiveCards.length > 0;
  const hasCatalog = visibleCatalogRows.length > 0;
  const showExitsTableReference = category === "data";
  const hasAnyResult = hasLive || hasCatalog || showExitsTableReference;

  if (!isFrontendLocalValidationMode()) {
    return <Navigate to="/" replace />;
  }

  return (
    <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="ui-standards-page">
      <PageHeader
        title="ExItS UI Standard"
        description="Live reference for shared components, interaction patterns, and design standards."
      />
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="ui-standard-global-note">
        Changes to global components should be reflected here and throughout the application.
      </p>

      <UiStandardsPreferencePreview />

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

          {showExitsTableReference ? (
            <section
              className="flex min-w-0 flex-col gap-2"
              data-testid="ui-standards-data-exits-table"
            >
              <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
                ExitsTable reference
              </h2>
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                Use live Responsive Data View for feature previews (search, sort, export, page size,
                category). The locked full ExitsTable reference remains below for Actions / inline
                edit.
              </p>
              <UiStandardsTablesPanel isOpen={isOpen} setOpen={setOpen} />
            </section>
          ) : null}
        </>
      )}
    </div>
  );
}
