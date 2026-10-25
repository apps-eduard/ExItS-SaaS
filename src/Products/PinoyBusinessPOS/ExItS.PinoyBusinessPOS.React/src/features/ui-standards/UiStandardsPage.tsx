import { ArrowUp, ChevronRight } from "lucide-react";
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
import { useUiStandardsSidenav } from "@/features/ui-standards/UiStandardsSidenav";
import {
  filterLiveCards,
  isUiStandardNavGroup,
  isUiStandardNavSubgroup,
  parseUiStandardCategoryParam,
  parseUiStandardsProductParam,
  UI_STANDARD_ORG_DEFAULT_CATEGORY,
  UI_STANDARD_ORG_NAV,
  UI_STANDARD_PERSONAL_DEFAULT_CATEGORY,
  UI_STANDARD_PERSONAL_NAV,
  type UiStandardFilterCategory,
  type UiStandardNavChild,
  type UiStandardsProduct,
} from "@/features/ui-standards/ui-standard-catalog";
import { OrgEhgButtonGlassGallery } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgButtonGlassGallery";
import { OrgEhgInputGlassGallery } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgInputGlassGallery";
import { OrgEhgDateGlassGallery } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgDateGlassGallery";
import { OrgEhgQuickActionGlassGallery } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgQuickActionGlassGallery";
import { OrgEhgCardGlassGallery } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgCardGlassGallery";
import { parseOrgEhgCardGlassKind } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgCardGlassKinds";
import {
  isOrgEhgCategory,
  UiStandardsOrgHybridGlass,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/UiStandardsOrgHybridGlass";
import {
  UiStandardsShellAppearanceProvider,
  UiStandardsShellRoot,
} from "@/features/ui-standards/UiStandardsShellAppearance";
import { UI_STANDARD_NAV_ICONS } from "@/features/ui-standards/ui-standards-product-icons";
import { cn } from "@/lib/cn";
import "@/features/ui-standards/ui-standards.css";
import "@/features/ui-standards/org-enterprise-hybrid-glass/org-enterprise-hybrid-glass.css";

/**
 * ExItS UI Standards (DEV only).
 * Personal shell shortcut -> ?product=personal -> classic Soft Glass playground.
 * Organization/business shell shortcut -> ?product=organization -> Hybrid Glass live pages.
 */
export function UiStandardsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const product = parseUiStandardsProductParam(searchParams.get("product"));
  const [category, setCategory] = useState<UiStandardFilterCategory>(() =>
    parseUiStandardCategoryParam(searchParams.get("category"), product),
  );
  const { isOpen, setOpen } = useUiStandardsDisclosure();
  const { open: sidenavOpen } = useUiStandardsSidenav();
  const mainRef = useRef<HTMLDivElement>(null);
  const [showScrollTop, setShowScrollTop] = useState(false);
  const [groupOpen, setGroupOpen] = useState<Record<string, boolean>>({
    "form-components": true,
    "button-components": true,
    "glass-components": true,
    "card-glass": true,
    cards: true,
    icons: true,
    "org-enterprise-hybrid-glass": true,
  });

  const nav = product === "organization" ? UI_STANDARD_ORG_NAV : UI_STANDARD_PERSONAL_NAV;
  const defaultCategory =
    product === "organization"
      ? UI_STANDARD_ORG_DEFAULT_CATEGORY
      : UI_STANDARD_PERSONAL_DEFAULT_CATEGORY;

  useEffect(() => {
    const nextProduct = parseUiStandardsProductParam(searchParams.get("product"));
    setCategory(parseUiStandardCategoryParam(searchParams.get("category"), nextProduct));
  }, [searchParams]);

  const syncParams = useCallback(
    (next: { category?: UiStandardFilterCategory; product?: UiStandardsProduct }) => {
      setSearchParams(
        (prev) => {
          const params = new URLSearchParams(prev);
          const nextProduct =
            next.product ?? parseUiStandardsProductParam(params.get("product"));
          const cat =
            next.category !== undefined
              ? next.category
              : parseUiStandardCategoryParam(params.get("category"), nextProduct);

          params.delete("q");
          params.delete("view");
          // Personal is the default surface ? omit product= from the URL to avoid remount noise.
          if (nextProduct === "organization") params.set("product", "organization");
          else params.delete("product");

          const productDefault =
            nextProduct === "organization"
              ? UI_STANDARD_ORG_DEFAULT_CATEGORY
              : UI_STANDARD_PERSONAL_DEFAULT_CATEGORY;
          if (cat !== productDefault) params.set("category", cat);
          else params.delete("category");

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
    setCategory(defaultCategory);
    syncParams({ category: defaultCategory, product });
  }, [defaultCategory, product, syncParams]);

  const visibleLiveCards = useMemo(
    () => (product === "personal" ? filterLiveCards(category, "") : []),
    [category, product],
  );

  const visibleLiveIds = useMemo(
    () => new Set(visibleLiveCards.map((c) => c.id)),
    [visibleLiveCards],
  );

  useEffect(() => {
    if (product !== "personal" || category !== "data") {
      return;
    }
    setOpen("tables.demo", true);
  }, [category, product, setOpen]);

  const showOrgEhg = product === "organization" && isOrgEhgCategory(category);
  const showButtonGlass = product === "personal" && category === "button-glass";
  const showInputGlass = product === "personal" && category === "input-glass";
  const showDateGlass = product === "personal" && category === "date-glass";
  const showQuickActionGlass = product === "personal" && category === "quick-action-glass";
  const personalCardGlassKind =
    product === "personal" ? parseOrgEhgCardGlassKind(category) : null;
  const showCardGlass = personalCardGlassKind != null;
  const hasLive = product === "personal" && visibleLiveCards.length > 0;
  const showExitsTableReference = product === "personal" && category === "data";
  const showLucide = product === "personal" && category === "lucide";
  const hasAnyResult =
    showOrgEhg ||
    showButtonGlass ||
    showInputGlass ||
    showDateGlass ||
    showQuickActionGlass ||
    showCardGlass ||
    hasLive ||
    showExitsTableReference ||
    showLucide;

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

  function renderNavItem(item: UiStandardNavChild, child: boolean) {
    const className = "ui-standards-sidenav__item" + (child ? " ui-standards-sidenav__item--child" : "");
    if (isUiStandardNavSubgroup(item)) {
      const expanded = groupOpen[item.id] ?? true;
      return (
        <div key={item.id} className="ui-standards-sidenav__group">
          <button
            type="button"
            className={className}
            aria-expanded={expanded}
            data-testid={`ui-standards-filter-${item.id}`}
            onClick={() =>
              setGroupOpen((current) => ({
                ...current,
                [item.id]: !(current[item.id] ?? true),
              }))
            }
          >
            {item.label}
            <ChevronRight className="ui-standards-sidenav__chevron" aria-hidden />
          </button>
          {expanded ? (
            <div className="ui-standards-sidenav__sublist" role="group" aria-label={item.label}>
              {item.children.map((grandchild) => renderNavItem(grandchild, true))}
            </div>
          ) : null}
        </div>
      );
    }
    if ("href" in item) {
      return (
        <a
          key={item.id}
          className={className}
          href={item.href}
          target="_blank"
          rel="noreferrer"
          data-testid={`ui-standards-filter-${item.id}`}
        >
          {item.label}
        </a>
      );
    }
    const selected = category === item.id;
    const ItemIcon = UI_STANDARD_NAV_ICONS[item.id];
    return (
      <button
        key={item.id}
        type="button"
        className={className}
        aria-pressed={selected}
        data-selected={selected ? "true" : "false"}
        data-testid={`ui-standards-filter-${item.id}`}
        onClick={() => setCategoryFilter(item.id)}
      >
        {product === "organization" ? (
          <span className="ui-standards-sidenav__item-indicator" aria-hidden="true" />
        ) : null}
        {ItemIcon ? (
          <ItemIcon className="ui-standards-sidenav__item-icon" aria-hidden strokeWidth={2.25} />
        ) : null}
        <span className="ui-standards-sidenav__item-label">{item.label}</span>
      </button>
    );
  }

  if (!isFrontendLocalValidationMode()) {
    return <Navigate to="/" replace />;
  }

  return (
    <UiStandardsShellAppearanceProvider>
    <UiStandardsShellRoot product={product}>
      <div
        className={cn(
          "ui-standards-layout",
          product === "organization" && "ui-standards-layout--org-ehg",
        )}
        data-sidenav={sidenavOpen ? "open" : "hidden"}
        data-exits-product={product}
        data-testid="ui-standards-layout"
      >
        {sidenavOpen ? (
          <nav
            id="ui-standards-sidenav"
            className={cn(
              "ui-standards-sidenav",
              product === "organization" && "ui-standards-sidenav--org-ehg",
            )}
            aria-label={
              product === "organization"
                ? "Organization Enterprise Hybrid Glass pages"
                : "Personal UI standard categories"
            }
            data-testid="ui-standards-sidenav"
          >
            <div className="ui-standards-sidenav__list" role="group" data-testid="ui-standards-category-filters">
              {nav.map((entry) => {
                if (isUiStandardNavGroup(entry)) {
                  const expanded = groupOpen[entry.id] ?? true;
                  return (
                    <div key={entry.id} className="ui-standards-sidenav__group">
                      <button
                        type="button"
                        className="ui-standards-sidenav__item"
                        aria-expanded={expanded}
                        data-testid={`ui-standards-filter-${entry.id}`}
                        onClick={() =>
                          setGroupOpen((current) => ({
                            ...current,
                            [entry.id]: !(current[entry.id] ?? true),
                          }))
                        }
                      >
                        {entry.label}
                        <ChevronRight className="ui-standards-sidenav__chevron" aria-hidden />
                      </button>
                      {expanded ? (
                        <div className="ui-standards-sidenav__sublist" role="group" aria-label={entry.label}>
                          {entry.children.map((child) => renderNavItem(child, true))}
                        </div>
                      ) : null}
                    </div>
                  );
                }
                return renderNavItem(entry, false);
              })}
            </div>
          </nav>
        ) : null}

        <div className="ui-standards-main-frame">
        <div
          ref={mainRef}
          className="ui-standards-main flex min-w-0 flex-col gap-3"
          data-testid="ui-standards-main"
        >
      <PageHeader
        title="ExItS UI Standard"
        description={
          product === "organization"
            ? "Organization Enterprise Hybrid Glass — live reference on authenticated Platform and POS APIs."
            : "Live Soft Glass / component playground for Personal UI standards."
        }
      />
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="ui-standard-global-note">
        {product === "organization"
          ? "Background from Button / Input glass playgrounds applies to this entire UI Standards shell (experimental). Light/dark follows Preferences. Personal Soft Glass production pages are unchanged."
          : "Background from glass playgrounds applies to this entire UI Standards shell (experimental). Light/dark follows Preferences. Changes to global components should still be reflected here and throughout the application."}
      </p>

      {product === "personal" && category === "all" ? <UiStandardsPreferencePreview /> : null}

      {showOrgEhg ? (
        <section className="flex min-w-0 flex-col gap-2" data-testid="ui-standards-org-ehg-section">
          <UiStandardsOrgHybridGlass category={category} />
        </section>
      ) : showButtonGlass ? (
        <section className="flex min-w-0 flex-col gap-2" data-testid="ui-standards-button-glass-section">
          <OrgEhgButtonGlassGallery />
        </section>
      ) : showInputGlass ? (
        <section className="flex min-w-0 flex-col gap-2" data-testid="ui-standards-input-glass-section">
          <OrgEhgInputGlassGallery />
        </section>
      ) : showDateGlass ? (
        <section className="flex min-w-0 flex-col gap-2" data-testid="ui-standards-date-glass-section">
          <OrgEhgDateGlassGallery />
        </section>
      ) : showQuickActionGlass ? (
        <section
          className="flex min-w-0 flex-col gap-2"
          data-testid="ui-standards-quick-action-glass-section"
        >
          <OrgEhgQuickActionGlassGallery />
        </section>
      ) : showCardGlass && personalCardGlassKind ? (
        <section className="flex min-w-0 flex-col gap-2" data-testid="ui-standards-card-glass-section">
          <OrgEhgCardGlassGallery kind={personalCardGlassKind} />
        </section>
      ) : !hasAnyResult ? (
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

          {showLucide ? (
            <section className="ui-standards-icon-frame" data-testid="ui-standards-lucide">
              <div className="ui-standards-icon-frame__bar">
                <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">Lucide</h2>
                <a href="https://lucide.dev/icons" target="_blank" rel="noreferrer">
                  Open in a new tab
                </a>
              </div>
              <iframe
                title="Lucide icons"
                src="https://lucide.dev/icons"
                data-testid="ui-standards-lucide-frame"
              />
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
    </UiStandardsShellRoot>
    </UiStandardsShellAppearanceProvider>
  );
}
