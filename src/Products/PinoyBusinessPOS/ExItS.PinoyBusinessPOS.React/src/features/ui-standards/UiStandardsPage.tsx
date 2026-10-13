import { ArrowUp } from "lucide-react";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Navigate, useSearchParams } from "react-router-dom";
import { isFrontendLocalValidationMode } from "@/api/platform/local-validation-gate";
import { EmptyState } from "@/components/exits/EmptyState";
import { PageHeader } from "@/components/exits/PageHeader";
import { Button } from "@/components/ui/button";
import { UiStandardsTablesPanel } from "@/features/ui-standards/UiStandardsTablesPanel";
import { UiStandardLiveSamples } from "@/features/ui-standards/UiStandardLiveSamples";
import { UiStandardsPreferencePreview } from "@/features/ui-standards/UiStandardsPreferencePreview";
import { useUiStandardsDisclosure } from "@/features/ui-standards/useUiStandardsDisclosure";
import {
  filterLiveCards,
  parseUiStandardCategoryParam,
  UI_STANDARD_FILTER_OPTIONS,
  type UiStandardFilterCategory,
} from "@/features/ui-standards/ui-standard-catalog";

/**
 * Canonical ExItS UI Standard page — live samples and the locked ExitsTable reference.
 * Route: /ui-standards ( /ui-standard aliases here ).
 * Classic / Simple modes removed (TASK-66D).
 */
export function UiStandardsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [category, setCategory] = useState<UiStandardFilterCategory>(() =>
    parseUiStandardCategoryParam(searchParams.get("category")),
  );
  const { isOpen, setOpen } = useUiStandardsDisclosure();
  const mainRef = useRef<HTMLDivElement>(null);
  const [showScrollTop, setShowScrollTop] = useState(false);

  useEffect(() => {
    setCategory(parseUiStandardCategoryParam(searchParams.get("category")));
  }, [searchParams]);

  const syncParams = useCallback(
    (next: { category?: UiStandardFilterCategory }) => {
      setSearchParams(
        (prev) => {
          const params = new URLSearchParams(prev);
          const cat =
            next.category !== undefined
              ? next.category
              : parseUiStandardCategoryParam(params.get("category"));

          params.delete("q");

          if (cat !== "all") params.set("category", cat);
          else params.delete("category");

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

  const clearFilters = useCallback(() => {
    setCategory("all");
    syncParams({ category: "all" });
  }, [syncParams]);

  const visibleLiveCards = useMemo(
    () => filterLiveCards(category, ""),
    [category],
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
  const showExitsTableReference = category === "data";
  const hasAnyResult = hasLive || showExitsTableReference;

  useEffect(() => {
    const node = mainRef.current;
    if (!node) return;
    const update = () => {
      setShowScrollTop(node.scrollTop > 48);
    };
    update();
    node.addEventListener("scroll", update, { passive: true });
    const observer = typeof ResizeObserver === "undefined" ? null : new ResizeObserver(update);
    observer?.observe(node);
    return () => {
      node.removeEventListener("scroll", update);
      observer?.disconnect();
    };
  }, [category, hasAnyResult]);

  const scrollMainToTop = useCallback(() => {
    const node = mainRef.current;
    if (!node) return;
    const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    node.scrollTo({ top: 0, behavior: reduceMotion ? "auto" : "smooth" });
  }, []);

  if (!isFrontendLocalValidationMode()) {
    return <Navigate to="/" replace />;
  }

  return (
    <div
      className="exits-page ui-standards-page flex min-h-0 min-w-0 flex-1 flex-col"
      data-testid="ui-standards-page"
    >
      <div className="ui-standards-layout">
        <nav className="ui-standards-sidenav" aria-label="UI standard categories" data-testid="ui-standards-sidenav">
          <div className="ui-standards-sidenav__list" role="group" data-testid="ui-standards-category-filters">
            {UI_STANDARD_FILTER_OPTIONS.map((opt) => {
              const selected = category === opt.id;
              return (
                <button
                  key={opt.id}
                  type="button"
                  className="ui-standards-sidenav__item"
                  aria-pressed={selected}
                  data-selected={selected ? "true" : "false"}
                  data-testid={`ui-standards-filter-${opt.id}`}
                  onClick={() => setCategoryFilter(opt.id)}
                >
                  {opt.label}
                </button>
              );
            })}
          </div>
        </nav>

        <div className="ui-standards-main-frame">
        <div
          ref={mainRef}
          className="ui-standards-main flex min-w-0 flex-col gap-3"
          data-testid="ui-standards-main"
        >
      <PageHeader
        title="ExItS UI Standard"
        description="Live reference for shared components, interaction patterns, and design standards."
      />
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="ui-standard-global-note">
        Changes to global components should be reflected here and throughout the application.
      </p>

      {category === "all" ? <UiStandardsPreferencePreview /> : null}

      {!hasAnyResult ? (
        <EmptyState
          title="No UI standards found"
          detail="Try another filter."
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
        {showScrollTop ? (
          <button
            type="button"
            className="ui-standards-scroll-top"
            aria-label="Back to top"
            data-testid="ui-standards-scroll-top"
            onClick={scrollMainToTop}
          >
            <ArrowUp className="size-5" aria-hidden />
          </button>
        ) : null}
        </div>
      </div>
    </div>
  );
}
