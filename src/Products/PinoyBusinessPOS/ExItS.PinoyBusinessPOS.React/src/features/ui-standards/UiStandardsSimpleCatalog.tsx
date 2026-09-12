import { useMemo, useState } from "react";
import {
  AlertTriangle,
  Check,
  ClipboardList,
  Factory,
  Mail,
  MessageSquare,
  PackageMinus,
  Pencil,
  Plus,
  CalendarClock,
  ChevronRight,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { ActionChipBar } from "@/components/exits/ActionChipBar";
import { CountBadge, CountChip } from "@/components/exits/CountChip";
import { ExitsTabs } from "@/components/exits/ExitsTabs";
import { FilterChip } from "@/components/exits/FilterChip";
import { ModuleSubnav } from "@/components/exits/ModuleSubnav";
import { RemovableChip } from "@/components/exits/RemovableChip";
import { StatusChip } from "@/components/exits/StatusChip";
import { TagChip } from "@/components/exits/TagChip";
import {
  ExitsTable,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableRow,
} from "@/components/exits/ExitsTable";
import {
  SimpleCatalogSection,
  SimpleGroup,
  SimpleSample,
  useSimpleSectionOpen,
} from "@/features/ui-standards/simple/SimpleCatalogPrimitives";
import { useI18n } from "@/i18n/I18nProvider";

const SIMPLE_NAV = [
  { id: "buttons", labelKey: "uiStandards.tabButtons" as const },
  { id: "chips", labelKey: "uiStandards.tabChips" as const },
  { id: "badges", labelKey: "uiStandards.tabBadges" as const },
  { id: "action-chips", labelKey: "uiStandards.tabActionChips" as const },
  { id: "tabs", labelKey: "uiStandards.tabTabs" as const },
  { id: "module-subnav", labelKey: "uiStandards.tabModuleSubnav" as const },
  { id: "filters", labelKey: "uiStandards.tabFilters" as const },
  { id: "cards", labelKey: "uiStandards.tabCards" as const },
  { id: "tables", labelKey: "uiStandards.tabTables" as const },
] as const;

const DEMO_ROWS = [
  { id: "1", name: "Apple", sku: "APL-01", qty: 12 },
  { id: "2", name: "Banana", sku: "BAN-02", qty: 8 },
  { id: "3", name: "Coconut", sku: "COC-03", qty: 4 },
];

/**
 * Simple UI Standards visual catalog — same ExItS standards as Classic, compact presentation.
 */
export function UiStandardsSimpleCatalog() {
  const { t } = useI18n();
  const { isOpen, setOpen } = useSimpleSectionOpen({
    buttons: true,
    chips: true,
    badges: true,
    "action-chips": true,
    tabs: true,
    "module-subnav": true,
    filters: true,
    cards: true,
    tables: true,
  });

  const [tabValue, setTabValue] = useState("overview");
  const [filterStatus, setFilterStatus] = useState("all");
  const [filterDirection, setFilterDirection] = useState("all");
  const [filterScope, setFilterScope] = useState("all");
  const [subnavValue, setSubnavValue] = useState("incoming");
  const [selectedIds, setSelectedIds] = useState<string[]>([]);

  const tabItems = useMemo(
    () => [
      { key: "overview", label: "Overview", count: 24 },
      { key: "pending", label: "Pending", count: 6 },
      { key: "done", label: "Completed", count: 16 },
    ],
    [],
  );

  const defaultActions = useMemo(
    () => [
      { key: "count", label: "Stock Count", onSelect: () => undefined },
      { key: "use", label: "Stock Use", onSelect: () => undefined },
      { key: "production", label: "Production", onSelect: () => undefined },
    ],
    [],
  );

  const iconActions = useMemo(
    () => [
      { key: "count", label: "Stock Count", icon: ClipboardList, onSelect: () => undefined },
      { key: "use", label: "Stock Use", icon: PackageMinus, onSelect: () => undefined },
      { key: "production", label: "Production", icon: Factory, onSelect: () => undefined },
    ],
    [],
  );

  const primaryActions = useMemo(
    () => [
      {
        key: "expiring",
        label: "Expiring stock",
        icon: CalendarClock,
        onSelect: () => undefined,
        emphasis: "primary" as const,
      },
      ...iconActions,
    ],
    [iconActions],
  );

  return (
    <div className="grid min-w-0 gap-3" data-testid="ui-standards-simple-catalog">
      <div className="grid gap-1">
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="ui-standards-simple-lede">
          {t("uiStandards.simpleLede")}
        </p>
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t("uiStandards.simpleLedeSecondary")}</p>
      </div>

      <nav
        aria-label={t("uiStandards.simpleSectionsAria")}
        data-testid="ui-standards-simple-nav"
        className="sticky top-0 z-10 -mx-1 overflow-x-auto overscroll-x-contain border-b border-border bg-[color-mix(in_srgb,var(--exits-bg)_92%,transparent)] px-1 py-2 backdrop-blur-md [scrollbar-width:thin]"
      >
        <ul className="m-0 flex list-none flex-nowrap gap-1 p-0">
          {SIMPLE_NAV.map((item) => (
            <li key={item.id} className="shrink-0">
              <a
                href={`#simple-${item.id}`}
                className="inline-flex rounded-full border border-border bg-surface px-2.5 py-1 text-[length:var(--exits-text-xs)] font-medium text-foreground no-underline hover:bg-[var(--exits-surface-muted)]"
              >
                {t(item.labelKey)}
              </a>
            </li>
          ))}
        </ul>
      </nav>

      {/* BUTTONS */}
      <SimpleCatalogSection id="buttons" title={t("uiStandards.tabButtons")} status="LOCKED" open={isOpen("buttons")} onOpenChange={(o) => setOpen("buttons", o)}>
        <SimpleGroup title="Intent">
          {(
            [
              ["PRIMARY", "default", "PRIMARY"],
              ["SUCCESS", "success", "SUCCESS"],
              ["MUTED", "secondary", "MUTED"],
              ["OUTLINE", "outline", "OUTLINE"],
              ["GHOST", "ghost", "GHOST"],
              ["INFO", "info", "INFO"],
              ["WARNING", "warning", "WARNING"],
              ["DANGER", "destructive", "DANGER"],
              ["DANGER STRONG", "dangerStrong", "DANGER STRONG"],
            ] as const
          ).map(([label, variant, command]) => (
            <SimpleSample key={variant} label={label} standard="Button" command={command} testId={`simple-btn-intent-${variant}`}>
              <Button type="button" variant={variant} shape="soft">
                {label === "DANGER STRONG" ? "Delete forever" : label === "PRIMARY" ? "Save" : label === "SUCCESS" ? "Approve" : label === "DANGER" ? "Delete" : label === "MUTED" ? "Reset" : label}
              </Button>
            </SimpleSample>
          ))}
        </SimpleGroup>
        <SimpleGroup title="Shape">
          {(
            [
              ["STANDARD", "standard", "PRIMARY + STANDARD"],
              ["SOFT", "soft", "PRIMARY + SOFT"],
              ["PILL", "pill", "PRIMARY + PILL"],
            ] as const
          ).map(([label, shape, command]) => (
            <SimpleSample key={shape} label={label} standard="Button" command={command} testId={`simple-btn-shape-${shape}`}>
              <Button type="button" shape={shape}>Save</Button>
            </SimpleSample>
          ))}
          <SimpleSample label="ROUND ICON ONLY" standard="Button" command="PRIMARY + ROUND + ICON ONLY" testId="simple-btn-shape-round">
            <Button type="button" size="icon" shape="round" aria-label="Edit" title="Edit">
              <Pencil className="size-4" aria-hidden />
            </Button>
          </SimpleSample>
        </SimpleGroup>
        <SimpleGroup title="Treatment">
          {(
            [
              ["FLAT", "flat", "PRIMARY + SOFT + FLAT"],
              ["ELEVATED", "elevated", "PRIMARY + SOFT + ELEVATED"],
              ["GRADIENT", "gradient", "PRIMARY + SOFT + GRADIENT"],
            ] as const
          ).map(([label, treatment, command]) => (
            <SimpleSample key={treatment} label={label} standard="Button" command={command} testId={`simple-btn-treatment-${treatment}`}>
              <Button type="button" shape="soft" treatment={treatment}>Save</Button>
            </SimpleSample>
          ))}
        </SimpleGroup>
        <SimpleGroup title="Content & states">
          <SimpleSample label="WITH ICON" standard="Button" command="PRIMARY + SOFT + WITH ICON" testId="simple-btn-with-icon">
            <Button type="button" shape="soft"><Plus className="size-4" aria-hidden />Add</Button>
          </SimpleSample>
          <SimpleSample label="TRAILING ICON" standard="Button" command="PRIMARY + SOFT + TRAILING ICON" testId="simple-btn-trailing-icon">
            <Button type="button" shape="soft">Next<ChevronRight className="size-4" aria-hidden /></Button>
          </SimpleSample>
          <SimpleSample label="DISABLED" standard="Button" command="PRIMARY + SOFT + DISABLED" testId="simple-btn-disabled">
            <Button type="button" shape="soft" disabled>Save</Button>
          </SimpleSample>
          <SimpleSample label="LOADING" standard="Button" command="PRIMARY + SOFT + LOADING" testId="simple-btn-loading">
            <Button type="button" shape="soft" disabled aria-busy>Saving…</Button>
          </SimpleSample>
        </SimpleGroup>
      </SimpleCatalogSection>

      {/* CHIPS */}
      <SimpleCatalogSection id="chips" title={t("uiStandards.tabChips")} status="LOCKED" open={isOpen("chips")} onOpenChange={(o) => setOpen("chips", o)}>
        <SimpleGroup title="Status">
          {(
            [
              ["PRIMARY", "primary"],
              ["SUCCESS", "success"],
              ["INFO", "info"],
              ["WARNING", "warning"],
              ["DANGER", "danger"],
            ] as const
          ).map(([label, tone]) => (
            <SimpleSample key={tone} label={label} standard="Chip" command={`STATUS CHIP + ${label}`} testId={`simple-status-${tone}`}>
              <StatusChip tone={tone}>{label}</StatusChip>
            </SimpleSample>
          ))}
        </SimpleGroup>
        <SimpleGroup title="Tag">
          {(
            [
              ["SOFT", "soft", "TAG CHIP + PRIMARY + SOFT"],
              ["PILL", "pill", "TAG CHIP + SUCCESS + PILL"],
              ["SQUARE", "square", "TAG CHIP + INFO + SQUARE"],
            ] as const
          ).map(([label, shape, command]) => (
            <SimpleSample key={shape} label={label} standard="Chip" command={command} testId={`simple-tag-${shape}`}>
              <div className="flex flex-wrap gap-1.5">
                <TagChip tone="primary" shape={shape}>Primary</TagChip>
                <TagChip tone="success" shape={shape} icon={<Check className="size-3" aria-hidden />}>Success</TagChip>
                <TagChip tone="warning" shape={shape} icon={<AlertTriangle className="size-3" aria-hidden />}>Warning</TagChip>
              </div>
            </SimpleSample>
          ))}
        </SimpleGroup>
        <SimpleGroup title="Removable">
          <SimpleSample label="REMOVABLE" standard="Chip" command="REMOVABLE CHIP + NEUTRAL" testId="simple-chip-removable">
            <RemovableChip onRemove={() => undefined} removeLabel="Remove Category">
              Category
            </RemovableChip>
          </SimpleSample>
        </SimpleGroup>
        <SimpleGroup title="Count chip">
          <SimpleSample label="COUNT CHIP" standard="Chip" command="COUNT CHIP + INLINE" testId="simple-count-chip">
            <CountChip label="Pending" count={6} />
          </SimpleSample>
        </SimpleGroup>
      </SimpleCatalogSection>

      {/* BADGES */}
      <SimpleCatalogSection id="badges" title={t("uiStandards.tabBadges")} status="LOCKED" open={isOpen("badges")} onOpenChange={(o) => setOpen("badges", o)}>
        <SimpleGroup title="Count">
          {(
            [
              ["NEUTRAL", "neutral", 2],
              ["PRIMARY", "primary", 8],
              ["WARNING", "warning", 4],
              ["DANGER", "danger", 3],
            ] as const
          ).map(([label, tone, count]) => (
            <SimpleSample key={tone} label={label} standard="Chip" command={`COUNT BADGE + ${label}`} testId={`simple-badge-${tone}`}>
              <CountBadge count={count} tone={tone} />
            </SimpleSample>
          ))}
        </SimpleGroup>
        <SimpleGroup title="Overlay / inside control">
          <SimpleSample label="OVERLAY" standard="Chip" command="COUNT BADGE + OVERLAY" testId="simple-badge-overlay">
            <div className="flex flex-wrap gap-4">
              {[Mail, MessageSquare, CalendarClock].map((Icon, i) => (
                <span key={i} className="relative inline-flex">
                  <Icon className="size-5 text-foreground" aria-hidden />
                  <CountBadge count={[8, 3, 2][i]} tone="danger" className="absolute -end-2 -top-2" />
                </span>
              ))}
            </div>
          </SimpleSample>
          <SimpleSample label="INSIDE CONTROL" standard="Chip" command="COUNT CHIP + INLINE" testId="simple-badge-inside">
            <div className="flex flex-wrap gap-2">
              <CountChip label="Emails" count={8} />
              <CountChip label="Messages" count={3} />
            </div>
          </SimpleSample>
        </SimpleGroup>
      </SimpleCatalogSection>

      {/* ACTION CHIPS */}
      <SimpleCatalogSection id="action-chips" title={t("uiStandards.tabActionChips")} status="PILOT" open={isOpen("action-chips")} onOpenChange={(o) => setOpen("action-chips", o)}>
        <SimpleGroup title="Default / icon / primary">
          <SimpleSample label="DEFAULT" standard="Action Chip" standardStatus="pilot" command="ACTION CHIP + SOFT" testId="simple-action-default">
            <ActionChipBar ariaLabel="Default actions" items={defaultActions} />
          </SimpleSample>
          <SimpleSample label="WITH ICON" standard="Action Chip" standardStatus="pilot" command="ACTION CHIP + SOFT + WITH ICON" testId="simple-action-icons">
            <ActionChipBar ariaLabel="Icon actions" items={iconActions} />
          </SimpleSample>
          <SimpleSample label="PRIMARY EMPHASIS" standard="Action Chip" standardStatus="pilot" command="ACTION CHIP GROUP + PRIMARY FIRST + WITH ICON" testId="simple-action-primary">
            <ActionChipBar ariaLabel="Primary first" layout="wrap" items={primaryActions} />
          </SimpleSample>
        </SimpleGroup>
        <SimpleGroup title="Shape">
          <SimpleSample label="SOFT" standard="Action Chip" standardStatus="pilot" command="ACTION CHIP + SOFT + WITH ICON" testId="simple-action-shape-soft">
            <ActionChipBar ariaLabel="Soft actions" shape="soft" items={iconActions} />
          </SimpleSample>
          <SimpleSample label="PILL" standard="Action Chip" standardStatus="pilot" command="ACTION CHIP + PILL + WITH ICON" testId="simple-action-shape-pill">
            <ActionChipBar ariaLabel="Pill actions" shape="pill" items={iconActions} />
          </SimpleSample>
        </SimpleGroup>
        <SimpleGroup title="Grouping">
          {(
            [
              ["INLINE", "inline", "ACTION CHIP GROUP + INLINE"],
              ["WRAP", "wrap", "ACTION CHIP GROUP + WRAP"],
              ["SCROLL", "scroll", "ACTION CHIP GROUP + SCROLL"],
              ["GRID", "grid", "ACTION CHIP GROUP + GRID"],
            ] as const
          ).map(([label, layout, command]) => (
            <SimpleSample key={layout} label={label} standard="Action Chip" standardStatus="pilot" command={command} testId={`simple-action-${layout}`} contentClassName="min-w-0">
              <ActionChipBar
                ariaLabel={`${label} actions`}
                layout={layout}
                items={iconActions}
                listClassName={layout === "grid" ? "[&_.exits-action-chip]:w-full [&_.exits-action-chip]:justify-start" : undefined}
                className={layout === "scroll" ? "max-w-xs" : undefined}
              />
            </SimpleSample>
          ))}
        </SimpleGroup>
      </SimpleCatalogSection>

      {/* TABS */}
      <SimpleCatalogSection id="tabs" title={t("uiStandards.tabTabs")} status="LOCKED" open={isOpen("tabs")} onOpenChange={(o) => setOpen("tabs", o)}>
        <SimpleGroup title="Variants">
          {(
            [
              ["UNDERLINE", "underline", "UNDERLINE TABS"],
              ["SOFT", "soft", "SOFT TABS"],
              ["PILL", "pill", "PILL TABS"],
              ["PILL BAR", "pillBar", "PILL BAR TABS + SOLID PRIMARY ACTIVE"],
              ["SEGMENTED", "segmented", "SEGMENTED TABS"],
              ["ENCLOSED", "enclosed", "ENCLOSED TABS"],
              ["VERTICAL", "vertical", "VERTICAL TABS"],
            ] as const
          ).map(([label, variant, command]) => (
            <SimpleSample key={variant} label={label} standard="Tabs" command={command} testId={`simple-tabs-${variant}`} contentClassName="min-w-0">
              <ExitsTabs
                variant={variant}
                ariaLabel={`${label} demo`}
                value={tabValue}
                onValueChange={setTabValue}
                items={tabItems.map(({ key, label: l }) => ({ key, label: l }))}
                layout={variant === "pillBar" || variant === "segmented" ? "equal" : "content"}
              />
            </SimpleSample>
          ))}
        </SimpleGroup>
        <SimpleGroup title="With counts">
          <SimpleSample label="PILL BAR + COUNT" standard="Tabs" command="PILL BAR TABS + WITH COUNT + SOLID PRIMARY ACTIVE" testId="simple-tabs-counts" contentClassName="min-w-0">
            <ExitsTabs
              variant="pillBar"
              layout="equal"
              ariaLabel="Tabs with counts"
              value={tabValue}
              onValueChange={setTabValue}
              items={tabItems}
            />
          </SimpleSample>
        </SimpleGroup>
      </SimpleCatalogSection>

      {/* MODULE SUBNAV */}
      <SimpleCatalogSection id="module-subnav" title={t("uiStandards.tabModuleSubnav")} status="PILOT" open={isOpen("module-subnav")} onOpenChange={(o) => setOpen("module-subnav", o)}>
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t("uiStandards.simpleModuleSubnavNote")}</p>
        <SimpleGroup title="Variants">
          {(
            [
              ["UNDERLINE", "underline", "MODULE SUBNAV + UNDERLINE"],
              ["SOFT", "soft", "MODULE SUBNAV + SOFT"],
              ["PILL", "pill", "MODULE SUBNAV + PILL"],
              ["PILL BAR", "pillBar", "MODULE SUBNAV + PILL BAR + SOLID PRIMARY ACTIVE"],
            ] as const
          ).map(([label, variant, command]) => (
            <SimpleSample key={variant} label={label} standard="Module Subnav" standardStatus="pilot" command={command} testId={`simple-subnav-${variant}`} contentClassName="min-w-0">
              <ModuleSubnav
                variant={variant}
                ariaLabel={`${label} module subnav`}
                value={subnavValue}
                onValueChange={setSubnavValue}
                activeTreatment="solid"
                items={[
                  { key: "po", label: "Purchase orders", to: "/demo/po" },
                  { key: "incoming", label: "Incoming orders", to: "/demo/incoming", count: 2 },
                  { key: "suppliers", label: "Suppliers", to: "/demo/suppliers" },
                ]}
              />
            </SimpleSample>
          ))}
        </SimpleGroup>
        <SimpleSample label="WITH ICON + COUNT" standard="Module Subnav" standardStatus="pilot" command="MODULE SUBNAV + PILL BAR + WITH ICON + WITH COUNT + SOLID PRIMARY ACTIVE" testId="simple-subnav-icons" contentClassName="min-w-0">
          <ModuleSubnav
            variant="pillBar"
            ariaLabel="Purchasing subnav"
            value={subnavValue}
            onValueChange={setSubnavValue}
            items={[
              { key: "po", label: "Purchase orders", to: "/demo/po", icon: ClipboardList },
              { key: "incoming", label: "Incoming orders", to: "/demo/incoming", icon: PackageMinus, count: 2 },
              { key: "suppliers", label: "Suppliers", to: "/demo/suppliers", icon: Factory },
            ]}
          />
        </SimpleSample>
      </SimpleCatalogSection>

      {/* FILTERS */}
      <SimpleCatalogSection id="filters" title={t("uiStandards.tabFilters")} status="LOCKED" open={isOpen("filters")} onOpenChange={(o) => setOpen("filters", o)}>
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t("uiStandards.simpleFiltersNote")}</p>
        <SimpleGroup title="Segmented filter">
          <SimpleSample label="STATUS" standard="Chip" command="FILTER CHIP" testId="simple-filter-status" contentClassName="flex flex-wrap gap-1.5">
            {(
              [
                ["all", "All"],
                ["draft", "Draft"],
                ["transit", "In transit"],
                ["received", "Received"],
              ] as const
            ).map(([key, label]) => (
              <FilterChip key={key} selected={filterStatus === key} onClick={() => setFilterStatus(key)}>
                {label}
              </FilterChip>
            ))}
          </SimpleSample>
          <SimpleSample label="DIRECTION" standard="Chip" command="FILTER CHIP" testId="simple-filter-direction" contentClassName="flex flex-wrap gap-1.5">
            {(
              [
                ["all", "All"],
                ["outgoing", "Outgoing"],
                ["incoming", "Incoming"],
              ] as const
            ).map(([key, label]) => (
              <FilterChip key={key} selected={filterDirection === key} onClick={() => setFilterDirection(key)}>
                {label}
              </FilterChip>
            ))}
          </SimpleSample>
          <SimpleSample label="SCOPE" standard="Chip" command="FILTER CHIP" testId="simple-filter-scope" contentClassName="flex flex-wrap gap-1.5">
            {(
              [
                ["all", "All"],
                ["org", "Organization"],
                ["branch", "Branch"],
              ] as const
            ).map(([key, label]) => (
              <FilterChip key={key} selected={filterScope === key} onClick={() => setFilterScope(key)}>
                {label}
              </FilterChip>
            ))}
          </SimpleSample>
          <SimpleSample label="WITH COUNT" standard="Chip" command="FILTER CHIP + WITH COUNT" testId="simple-filter-count" contentClassName="flex flex-wrap gap-1.5">
            <FilterChip selected>All 24</FilterChip>
            <FilterChip>Pending 6</FilterChip>
            <FilterChip>Completed 16</FilterChip>
          </SimpleSample>
        </SimpleGroup>
      </SimpleCatalogSection>

      {/* CARDS */}
      <SimpleCatalogSection id="cards" title={t("uiStandards.tabCards")} status="LOCKED" open={isOpen("cards")} onOpenChange={(o) => setOpen("cards", o)}>
        <SimpleGroup title="Treatments">
          {(
            [
              ["SURFACE", "surface", "SURFACE CARD"],
              ["BORDERED", "bordered", "BORDERED CARD"],
              ["ELEVATED", "elevated", "ELEVATED CARD"],
              ["SELECTED", "selected", "SELECTED CARD"],
              ["ACCENT", "accent", "ACCENT CARD"],
            ] as const
          ).map(([label, treatment, command]) => (
            <SimpleSample key={treatment} label={label} standard="Card" command={command} testId={`simple-card-${treatment}`} contentClassName="min-w-0">
              <Card treatment={treatment} className="p-3" accentTone={treatment === "accent" ? "primary" : undefined}>
                <CardTitle as="h4" className="text-[length:var(--exits-text-sm)]">{label}</CardTitle>
                <CardDescription>Compact sample</CardDescription>
              </Card>
            </SimpleSample>
          ))}
        </SimpleGroup>
        <SimpleGroup title="Patterns">
          <SimpleSample label="BASIC" standard="Card" command="BASIC CARD + BORDERED" testId="simple-card-basic" contentClassName="min-w-0">
            <Card treatment="bordered" className="p-3">
              <CardTitle as="h4" className="text-[length:var(--exits-text-sm)]">Basic</CardTitle>
              <CardDescription>Simple content card</CardDescription>
            </Card>
          </SimpleSample>
          <SimpleSample label="KPI" standard="Card" command="KPI CARD + BORDERED" testId="simple-card-kpi" contentClassName="min-w-0">
            <Card treatment="bordered" className="p-3">
              <CardDescription>Today</CardDescription>
              <CardTitle as="h4" className="text-[length:var(--exits-text-lg)] tabular-nums">₱12,450</CardTitle>
            </Card>
          </SimpleSample>
          <SimpleSample label="ACTION" standard="Card" command="ACTION CARD + INTERACTIVE + LIFT" testId="simple-card-action" contentClassName="min-w-0">
            <Card treatment="bordered" interactive motion="lift" className="p-3 text-start">
              <CardTitle as="h4" className="text-[length:var(--exits-text-sm)]">Open report</CardTitle>
              <CardDescription>Interactive lift</CardDescription>
            </Card>
          </SimpleSample>
          <SimpleSample
            label="STATUS"
            standard={["Card", "Chip"]}
            command="STATUS CARD + BORDERED"
            commandContext="Active = STATUS CHIP SUCCESS"
            testId="simple-card-chip"
            contentClassName="min-w-0"
          >
            <Card treatment="bordered" className="flex flex-col gap-2 p-3">
              <div className="flex items-center justify-between gap-2">
                <CardTitle as="h4" className="text-[length:var(--exits-text-sm)]">Account</CardTitle>
                <StatusChip tone="success">Active</StatusChip>
              </div>
              <CardDescription>Entity summary</CardDescription>
            </Card>
          </SimpleSample>
          <SimpleSample label="COMPACT" standard="Card" command="COMPACT CARD + BORDERED" testId="simple-card-compact" contentClassName="min-w-0">
            <Card treatment="bordered" padding="compact" className="p-2">
              <CardTitle as="h4" className="text-[length:var(--exits-text-xs)]">Compact</CardTitle>
            </Card>
          </SimpleSample>
        </SimpleGroup>
        <SimpleGroup title="Motion">
          {(
            [
              ["STATIC", "none", "BORDERED CARD + STATIC"],
              ["LIFT", "lift", "BORDERED CARD + INTERACTIVE + LIFT"],
              ["EXPAND", "expand", "BORDERED CARD + INTERACTIVE + EXPAND"],
            ] as const
          ).map(([label, motion, command]) => (
            <SimpleSample key={motion} label={label} standard="Card" command={command} testId={`simple-card-motion-${motion}`} contentClassName="min-w-0">
              <Card treatment="bordered" interactive={motion !== "none"} motion={motion === "none" ? undefined : motion} className="p-3 text-start">
                <CardTitle as="h4" className="text-[length:var(--exits-text-sm)]">{label}</CardTitle>
              </Card>
            </SimpleSample>
          ))}
        </SimpleGroup>
      </SimpleCatalogSection>

      {/* TABLES */}
      <SimpleCatalogSection id="tables" title={t("uiStandards.tabTables")} status="LOCKED" open={isOpen("tables")} onOpenChange={(o) => setOpen("tables", o)}>
        <SimpleGroup title="Configurations">
          <SimpleSample label="SIMPLE TABLE" standard="Table" command="SIMPLE TABLE" testId="simple-table-basic" contentClassName="min-w-0 overflow-x-auto">
            <ExitsTableContainer className="min-w-[18rem]">
              <ExitsTable>
                <ExitsTableHeader>
                  <ExitsTableRow>
                    <ExitsTableHead>Product</ExitsTableHead>
                    <ExitsTableHead>SKU</ExitsTableHead>
                    <ExitsTableHead data-align="numeric">Qty</ExitsTableHead>
                  </ExitsTableRow>
                </ExitsTableHeader>
                <ExitsTableBody>
                  {DEMO_ROWS.map((row) => (
                    <ExitsTableRow key={row.id}>
                      <ExitsTableCell>{row.name}</ExitsTableCell>
                      <ExitsTableCell>{row.sku}</ExitsTableCell>
                      <ExitsTableCell data-align="numeric">{row.qty}</ExitsTableCell>
                    </ExitsTableRow>
                  ))}
                </ExitsTableBody>
              </ExitsTable>
            </ExitsTableContainer>
          </SimpleSample>
          <SimpleSample label="WITH ACTIONS" standard="Table" command="FULL TABLE + ACTIONS ON" testId="simple-table-actions" contentClassName="min-w-0 overflow-x-auto">
            <ExitsTableContainer className="min-w-[20rem]">
              <ExitsTable>
                <ExitsTableHeader>
                  <ExitsTableRow>
                    <ExitsTableHead>Product</ExitsTableHead>
                    <ExitsTableHead>SKU</ExitsTableHead>
                    <ExitsTableHead>Actions</ExitsTableHead>
                  </ExitsTableRow>
                </ExitsTableHeader>
                <ExitsTableBody>
                  {DEMO_ROWS.slice(0, 2).map((row) => (
                    <ExitsTableRow key={row.id}>
                      <ExitsTableCell>{row.name}</ExitsTableCell>
                      <ExitsTableCell>{row.sku}</ExitsTableCell>
                      <ExitsTableCell>
                        <Button type="button" variant="ghost" size="icon" shape="round" aria-label={`Edit ${row.name}`}>
                          <Pencil className="size-3.5" aria-hidden />
                        </Button>
                      </ExitsTableCell>
                    </ExitsTableRow>
                  ))}
                </ExitsTableBody>
              </ExitsTable>
            </ExitsTableContainer>
          </SimpleSample>
          <SimpleSample label="WITH MULTI SELECT" standard="Table" command="FULL TABLE + MULTI SELECT ON" testId="simple-table-select" contentClassName="min-w-0 overflow-x-auto">
            <ExitsTableContainer className="min-w-[20rem]">
              <ExitsTable>
                <ExitsTableHeader>
                  <ExitsTableRow>
                    <ExitsTableHead>
                      <input
                        type="checkbox"
                        aria-label="Select all"
                        checked={selectedIds.length === DEMO_ROWS.length}
                        onChange={(e) => setSelectedIds(e.target.checked ? DEMO_ROWS.map((r) => r.id) : [])}
                      />
                    </ExitsTableHead>
                    <ExitsTableHead>Product</ExitsTableHead>
                    <ExitsTableHead>SKU</ExitsTableHead>
                  </ExitsTableRow>
                </ExitsTableHeader>
                <ExitsTableBody>
                  {DEMO_ROWS.map((row) => (
                    <ExitsTableRow key={row.id} data-selected={selectedIds.includes(row.id) ? "true" : undefined}>
                      <ExitsTableCell>
                        <input
                          type="checkbox"
                          aria-label={`Select ${row.name}`}
                          checked={selectedIds.includes(row.id)}
                          onChange={(e) =>
                            setSelectedIds((prev) =>
                              e.target.checked ? [...prev, row.id] : prev.filter((id) => id !== row.id),
                            )
                          }
                        />
                      </ExitsTableCell>
                      <ExitsTableCell>{row.name}</ExitsTableCell>
                      <ExitsTableCell>{row.sku}</ExitsTableCell>
                    </ExitsTableRow>
                  ))}
                </ExitsTableBody>
              </ExitsTable>
            </ExitsTableContainer>
          </SimpleSample>
          <SimpleSample label="WITH FOOTER" standard="Table" command="FULL TABLE + FOOTER ON" testId="simple-table-footer" contentClassName="min-w-0 overflow-x-auto">
            <ExitsTableContainer className="min-w-[18rem]">
              <ExitsTable>
                <ExitsTableHeader>
                  <ExitsTableRow>
                    <ExitsTableHead>Product</ExitsTableHead>
                    <ExitsTableHead data-align="numeric">Qty</ExitsTableHead>
                  </ExitsTableRow>
                </ExitsTableHeader>
                <ExitsTableBody>
                  {DEMO_ROWS.slice(0, 2).map((row) => (
                    <ExitsTableRow key={row.id}>
                      <ExitsTableCell>{row.name}</ExitsTableCell>
                      <ExitsTableCell data-align="numeric">{row.qty}</ExitsTableCell>
                    </ExitsTableRow>
                  ))}
                </ExitsTableBody>
                <tfoot>
                  <ExitsTableRow>
                    <ExitsTableCell>Total</ExitsTableCell>
                    <ExitsTableCell data-align="numeric">20</ExitsTableCell>
                  </ExitsTableRow>
                </tfoot>
              </ExitsTable>
            </ExitsTableContainer>
          </SimpleSample>
        </SimpleGroup>
      </SimpleCatalogSection>
    </div>
  );
}
