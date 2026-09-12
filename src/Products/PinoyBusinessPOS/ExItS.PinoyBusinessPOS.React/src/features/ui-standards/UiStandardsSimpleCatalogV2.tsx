import { useMemo, useState } from "react";
import {
  AlertTriangle,
  CalendarClock,
  Check,
  ChevronRight,
  ClipboardList,
  Factory,
  Info,
  Mail,
  MessageSquare,
  PackageMinus,
  Pencil,
  Plus,
  User,
  X,
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
  SimpleV2Group,
  SimpleV2Sample,
  SimpleV2Showcase,
} from "@/features/ui-standards/simple/SimpleV2Primitives";
import { useI18n } from "@/i18n/I18nProvider";

const TAG_TONES = [
  ["Primary", "primary"],
  ["Success", "success"],
  ["Info", "info"],
  ["Warning", "warning"],
  ["Danger", "danger"],
] as const;

const TAG_ICONS = {
  primary: User,
  success: Check,
  info: Info,
  warning: AlertTriangle,
  danger: X,
} as const;

const SIMPLE_V2_NAV = [
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
 * Simple V2 visual catalog — same ExItS standards as Classic, compact gallery with Cursor command above each sample.
 */
export function UiStandardsSimpleCatalogV2() {
  const { t } = useI18n();
  const [tabValue, setTabValue] = useState("overview");
  const [filterStatus, setFilterStatus] = useState("all");
  const [filterDirection, setFilterDirection] = useState("all");
  const [subnavValue, setSubnavValue] = useState("incoming");
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [editSku, setEditSku] = useState("APL-01");

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
      { key: "count", label: "Stock Count", onSelect: (): void => undefined },
      { key: "use", label: "Stock Use", onSelect: (): void => undefined },
      { key: "production", label: "Production", onSelect: (): void => undefined },
    ],
    [],
  );

  const iconActions = useMemo(
    () => [
      { key: "count", label: "Stock Count", icon: ClipboardList, onSelect: (): void => undefined },
      { key: "use", label: "Stock Use", icon: PackageMinus, onSelect: (): void => undefined },
      { key: "production", label: "Production", icon: Factory, onSelect: (): void => undefined },
    ],
    [],
  );

  const primaryActions = useMemo(
    () => [
      {
        key: "expiring",
        label: "Expiring stock",
        icon: CalendarClock,
        onSelect: (): void => undefined,
        emphasis: "primary" as const,
      },
      ...iconActions,
    ],
    [iconActions],
  );

  return (
    <div className="grid min-w-0 gap-4" data-testid="ui-standards-simple-v2-catalog">
      <div className="grid gap-1">
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="ui-standards-simple-v2-lede">
          {t("uiStandards.simpleV2Lede")}
        </p>
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t("uiStandards.simpleLedeSecondary")}</p>
      </div>

      <nav
        aria-label={t("uiStandards.simpleSectionsAria")}
        data-testid="ui-standards-simple-v2-nav"
        className="sticky top-0 z-10 -mx-1 overflow-x-auto overscroll-x-contain border-b border-border bg-[color-mix(in_srgb,var(--exits-bg)_92%,transparent)] px-1 py-2 backdrop-blur-md [scrollbar-width:thin]"
      >
        <ul className="m-0 flex list-none flex-nowrap gap-1 p-0">
          {SIMPLE_V2_NAV.map((item) => (
            <li key={item.id} className="shrink-0">
              <a
                href={`#simple-v2-${item.id}`}
                className="inline-flex rounded-full border border-border bg-surface px-2.5 py-1 text-[length:var(--exits-text-xs)] font-medium text-foreground no-underline hover:bg-[var(--exits-surface-muted)]"
              >
                {t(item.labelKey)}
              </a>
            </li>
          ))}
        </ul>
      </nav>

      {/* 1. BUTTONS */}
      <SimpleV2Showcase
        id="simple-v2-buttons"
        title={t("uiStandards.tabButtons")}
        status="LOCKED"
        testId="ui-standards-simple-v2-buttons"
      >
        <SimpleV2Group title="Default / intents">
          <SimpleV2Sample label="Primary" standard="Button" command="PRIMARY + SOFT" testId="simple-v2-btn-primary">
            <Button type="button" shape="soft">Save</Button>
          </SimpleV2Sample>
          <SimpleV2Sample label="Success" standard="Button" command="SUCCESS + SOFT" testId="simple-v2-btn-success">
            <Button type="button" variant="success" shape="soft">Approve</Button>
          </SimpleV2Sample>
          <SimpleV2Sample label="Outline" standard="Button" command="OUTLINE + SOFT" testId="simple-v2-btn-outline">
            <Button type="button" variant="outline" shape="soft">Outline</Button>
          </SimpleV2Sample>
          <SimpleV2Sample label="Ghost" standard="Button" command="GHOST + SOFT" testId="simple-v2-btn-ghost">
            <Button type="button" variant="ghost" shape="soft">Ghost</Button>
          </SimpleV2Sample>
          <SimpleV2Sample label="Danger" standard="Button" command="DANGER + SOFT" testId="simple-v2-btn-danger">
            <Button type="button" variant="destructive" shape="soft">Delete</Button>
          </SimpleV2Sample>
        </SimpleV2Group>
        <SimpleV2Group title="Treatments">
          <SimpleV2Sample label="Elevated" standard="Button" command="PRIMARY + SOFT + ELEVATED" testId="simple-v2-btn-elevated">
            <Button type="button" shape="soft" treatment="elevated">Elevated</Button>
          </SimpleV2Sample>
          <SimpleV2Sample label="Gradient" standard="Button" command="PRIMARY + SOFT + GRADIENT" testId="simple-v2-btn-gradient">
            <Button type="button" shape="soft" treatment="gradient">Gradient</Button>
          </SimpleV2Sample>
        </SimpleV2Group>
        <SimpleV2Group title="Shapes">
          <SimpleV2Sample label="Standard" standard="Button" command="PRIMARY + STANDARD" testId="simple-v2-btn-standard">
            <Button type="button" shape="standard">Standard</Button>
          </SimpleV2Sample>
          <SimpleV2Sample label="Soft" standard="Button" command="PRIMARY + SOFT" testId="simple-v2-btn-soft">
            <Button type="button" shape="soft">Soft</Button>
          </SimpleV2Sample>
          <SimpleV2Sample label="Pill" standard="Button" command="PRIMARY + PILL" testId="simple-v2-btn-pill">
            <Button type="button" shape="pill">Pill</Button>
          </SimpleV2Sample>
          <SimpleV2Sample label="Icon only" standard="Button" command="PRIMARY + ROUND + ICON ONLY" testId="simple-v2-btn-icon-only">
            <Button type="button" size="icon" shape="round" aria-label="Edit" title="Edit">
              <Pencil className="size-4" aria-hidden />
            </Button>
          </SimpleV2Sample>
        </SimpleV2Group>
        <SimpleV2Group title="Content & states">
          <SimpleV2Sample label="With icon" standard="Button" command="PRIMARY + SOFT + WITH ICON" testId="simple-v2-btn-with-icon">
            <Button type="button" shape="soft">
              <Plus className="size-4" aria-hidden />
              Add
            </Button>
          </SimpleV2Sample>
          <SimpleV2Sample label="Trailing icon" standard="Button" command="PRIMARY + SOFT + TRAILING ICON" testId="simple-v2-btn-trailing">
            <Button type="button" shape="soft">
              Next
              <ChevronRight className="size-4" aria-hidden />
            </Button>
          </SimpleV2Sample>
          <SimpleV2Sample label="Disabled" standard="Button" command="PRIMARY + SOFT + DISABLED" testId="simple-v2-btn-disabled">
            <Button type="button" shape="soft" disabled>Save</Button>
          </SimpleV2Sample>
          <SimpleV2Sample label="Sizes" standard="Button" command="PRIMARY + SOFT + LARGE" testId="simple-v2-btn-sizes">
            <Button type="button" shape="soft">Default</Button>
            <Button type="button" shape="soft" size="large">Large</Button>
          </SimpleV2Sample>
        </SimpleV2Group>
      </SimpleV2Showcase>

      {/* 2. CHIPS */}
      <SimpleV2Showcase
        id="simple-v2-chips"
        title={t("uiStandards.tabChips")}
        status="LOCKED"
        testId="ui-standards-simple-v2-chips"
      >
        <SimpleV2Group title="Status">
          <SimpleV2Sample label="Default" standard="Chip" command="STATUS CHIP" testId="simple-v2-status-default">
            {TAG_TONES.map(([label, tone]) => (
              <StatusChip key={tone} tone={tone}>{label}</StatusChip>
            ))}
          </SimpleV2Sample>
          <SimpleV2Sample label="Pills" standard="Chip" command="STATUS CHIP + PILL" testId="simple-v2-status-pills">
            {TAG_TONES.map(([label, tone]) => (
              <StatusChip key={tone} tone={tone} shape="pill">{label}</StatusChip>
            ))}
          </SimpleV2Sample>
        </SimpleV2Group>
        <SimpleV2Group title="Tag">
          <SimpleV2Sample label="Soft" standard="Chip" command="TAG CHIP + SOFT" testId="simple-v2-tag-default">
            {TAG_TONES.map(([label, tone]) => (
              <TagChip key={tone} tone={tone} shape="soft">{label}</TagChip>
            ))}
          </SimpleV2Sample>
          <SimpleV2Sample label="Pills" standard="Chip" command="TAG CHIP + PILL" testId="simple-v2-tag-pills">
            {TAG_TONES.map(([label, tone]) => (
              <TagChip key={tone} tone={tone} shape="pill">{label}</TagChip>
            ))}
          </SimpleV2Sample>
          <SimpleV2Sample label="Square" standard="Chip" command="TAG CHIP + SQUARE" testId="simple-v2-tag-square">
            {TAG_TONES.map(([label, tone]) => (
              <TagChip key={tone} tone={tone} shape="square">{label}</TagChip>
            ))}
          </SimpleV2Sample>
          <SimpleV2Sample label="Icons" standard="Chip" command="TAG CHIP + SOFT + WITH ICON" testId="simple-v2-tag-icons">
            {TAG_TONES.map(([label, tone]) => {
              const Icon = TAG_ICONS[tone];
              return (
                <TagChip key={tone} tone={tone} shape="soft" icon={<Icon className="size-3" aria-hidden />}>
                  {label}
                </TagChip>
              );
            })}
          </SimpleV2Sample>
        </SimpleV2Group>
        <SimpleV2Group title="Filter / removable / count">
          <SimpleV2Sample label="Filter" standard="Chip" command="FILTER CHIP" testId="simple-v2-chip-filter">
            <FilterChip selected>All</FilterChip>
            <FilterChip>Draft</FilterChip>
            <FilterChip>Received</FilterChip>
          </SimpleV2Sample>
          <SimpleV2Sample label="Removable" standard="Chip" command="REMOVABLE CHIP + NEUTRAL" testId="simple-v2-chip-removable">
            <RemovableChip onRemove={() => undefined} removeLabel="Remove Category">Category</RemovableChip>
          </SimpleV2Sample>
          <SimpleV2Sample label="Count chip" standard="Chip" command="COUNT CHIP + INLINE" testId="simple-v2-chip-count">
            <CountChip label="Pending" count={6} />
          </SimpleV2Sample>
        </SimpleV2Group>
      </SimpleV2Showcase>

      {/* 3. BADGES */}
      <SimpleV2Showcase
        id="simple-v2-badges"
        title={t("uiStandards.tabBadges")}
        status="LOCKED"
        testId="ui-standards-simple-v2-badges"
      >
        <SimpleV2Group title="Count badges">
          <SimpleV2Sample label="Counts" standard="Chip" command="COUNT BADGE" testId="simple-v2-badge-count">
            <CountBadge count={2} tone="neutral" />
            <CountBadge count={8} tone="primary" />
            <CountBadge count={12} tone="info" />
            <CountBadge count="99+" tone="danger" />
          </SimpleV2Sample>
          <SimpleV2Sample label="Semantic" standard="Chip" command="COUNT BADGE + SEMANTIC" testId="simple-v2-badge-semantic">
            <CountBadge count={1} tone="primary" />
            <CountBadge count={2} tone="neutral" />
            <CountBadge count={3} tone="warning" />
            <CountBadge count={4} tone="danger" />
            <StatusChip tone="info">Info</StatusChip>
            <StatusChip tone="success">Success</StatusChip>
          </SimpleV2Sample>
        </SimpleV2Group>
        <SimpleV2Group title="Composition">
          <SimpleV2Sample label="Inside control" standard="Chip" command="COUNT CHIP + INLINE" testId="simple-v2-badge-inside">
            <CountChip label="Emails" count={8} />
            <CountChip label="Messages" count={3} />
          </SimpleV2Sample>
          <SimpleV2Sample label="Overlay" standard="Chip" command="COUNT BADGE + OVERLAY" testId="simple-v2-badge-overlay">
            {[Mail, MessageSquare, CalendarClock].map((Icon, i) => (
              <span key={i} className="relative inline-flex">
                <Icon className="size-5 text-foreground" aria-hidden />
                <CountBadge count={[8, 3, 2][i]} tone="danger" className="absolute -end-2 -top-2" />
              </span>
            ))}
          </SimpleV2Sample>
        </SimpleV2Group>
      </SimpleV2Showcase>

      {/* 4. ACTION CHIPS */}
      <SimpleV2Showcase
        id="simple-v2-action-chips"
        title={t("uiStandards.tabActionChips")}
        status="PILOT"
        testId="ui-standards-simple-v2-action-chips"
      >
        <SimpleV2Group title="Default">
          <SimpleV2Sample
            label="Default"
            standard="Action Chip"
            standardStatus="pilot"
            command="ACTION CHIP + SOFT"
            testId="simple-v2-action-default"
          >
            <ActionChipBar ariaLabel="Default actions" items={defaultActions} />
          </SimpleV2Sample>
          <SimpleV2Sample
            label="With icon"
            standard="Action Chip"
            standardStatus="pilot"
            command="ACTION CHIP + SOFT + WITH ICON"
            testId="simple-v2-action-icons"
          >
            <ActionChipBar ariaLabel="Icon actions" items={iconActions} />
          </SimpleV2Sample>
          <SimpleV2Sample
            label="Primary emphasis"
            standard="Action Chip"
            standardStatus="pilot"
            command="ACTION CHIP GROUP + PRIMARY FIRST + WITH ICON"
            testId="simple-v2-action-primary"
          >
            <ActionChipBar ariaLabel="Primary first" layout="wrap" items={primaryActions} />
          </SimpleV2Sample>
        </SimpleV2Group>
        <SimpleV2Group title="Shapes">
          <SimpleV2Sample
            label="Soft"
            standard="Action Chip"
            standardStatus="pilot"
            command="ACTION CHIP + SOFT + WITH ICON"
            testId="simple-v2-action-soft"
          >
            <ActionChipBar ariaLabel="Soft actions" shape="soft" items={iconActions} />
          </SimpleV2Sample>
          <SimpleV2Sample
            label="Pill"
            standard="Action Chip"
            standardStatus="pilot"
            command="ACTION CHIP + PILL + WITH ICON"
            testId="simple-v2-action-pill"
          >
            <ActionChipBar ariaLabel="Pill actions" shape="pill" items={iconActions} />
          </SimpleV2Sample>
        </SimpleV2Group>
        <SimpleV2Group title="Grouping">
          {(
            [
              ["Inline", "inline", "ACTION CHIP GROUP + INLINE"],
              ["Wrap", "wrap", "ACTION CHIP GROUP + WRAP"],
              ["Scroll", "scroll", "ACTION CHIP GROUP + SCROLL"],
              ["Grid", "grid", "ACTION CHIP GROUP + GRID"],
            ] as const
          ).map(([label, layout, command]) => (
            <SimpleV2Sample
              key={layout}
              label={label}
              standard="Action Chip"
              standardStatus="pilot"
              command={command}
              testId={`simple-v2-action-${layout}`}
              contentClassName="min-w-0"
            >
              <ActionChipBar
                ariaLabel={`${label} actions`}
                layout={layout}
                items={iconActions}
                listClassName={
                  layout === "grid" ? "[&_.exits-action-chip]:w-full [&_.exits-action-chip]:justify-start" : undefined
                }
                className={layout === "scroll" ? "max-w-xs" : undefined}
              />
            </SimpleV2Sample>
          ))}
        </SimpleV2Group>
      </SimpleV2Showcase>

      {/* 5. TABS */}
      <SimpleV2Showcase
        id="simple-v2-tabs"
        title={t("uiStandards.tabTabs")}
        status="LOCKED"
        testId="ui-standards-simple-v2-tabs"
      >
        <SimpleV2Group title="Variants">
          {(
            [
              ["Underline", "underline", "UNDERLINE TABS"],
              ["Soft", "soft", "SOFT TABS"],
              ["Pill", "pill", "PILL TABS"],
              ["Pill bar", "pillBar", "PILL BAR TABS + SOLID PRIMARY ACTIVE"],
              ["Segmented", "segmented", "SEGMENTED TABS"],
              ["Enclosed", "enclosed", "ENCLOSED TABS"],
              ["Vertical", "vertical", "VERTICAL TABS"],
            ] as const
          ).map(([label, variant, command]) => (
            <SimpleV2Sample
              key={variant}
              label={label}
              standard="Tabs"
              command={command}
              testId={`simple-v2-tabs-${variant}`}
              contentClassName="min-w-0"
            >
              <ExitsTabs
                variant={variant}
                ariaLabel={`${label} demo`}
                value={tabValue}
                onValueChange={setTabValue}
                items={tabItems.map(({ key, label: l }) => ({ key, label: l }))}
                layout={variant === "pillBar" || variant === "segmented" ? "equal" : "content"}
              />
            </SimpleV2Sample>
          ))}
        </SimpleV2Group>
        <SimpleV2Group title="With counts">
          <SimpleV2Sample
            label="Pill bar + counts"
            standard="Tabs"
            command="PILL BAR TABS + WITH COUNT + SOLID PRIMARY ACTIVE"
            testId="simple-v2-tabs-counts"
            contentClassName="min-w-0"
          >
            <ExitsTabs
              variant="pillBar"
              layout="equal"
              ariaLabel="Tabs with counts"
              value={tabValue}
              onValueChange={setTabValue}
              items={tabItems}
            />
          </SimpleV2Sample>
        </SimpleV2Group>
      </SimpleV2Showcase>

      {/* 6. MODULE SUBNAV */}
      <SimpleV2Showcase
        id="simple-v2-module-subnav"
        title={t("uiStandards.tabModuleSubnav")}
        status="PILOT"
        testId="ui-standards-simple-v2-module-subnav"
        note={t("uiStandards.simpleModuleSubnavNote")}
      >
        <SimpleV2Group title="Variants">
          {(
            [
              ["Underline", "underline", "MODULE SUBNAV + UNDERLINE"],
              ["Soft", "soft", "MODULE SUBNAV + SOFT"],
              ["Pill", "pill", "MODULE SUBNAV + PILL"],
              ["Pill bar", "pillBar", "MODULE SUBNAV + PILL BAR + SOLID PRIMARY ACTIVE"],
              ["Compact", "compact", "MODULE SUBNAV + COMPACT"],
            ] as const
          ).map(([label, variant, command]) => (
            <SimpleV2Sample
              key={variant}
              label={label}
              standard="Module Subnav"
              standardStatus="pilot"
              command={command}
              testId={`simple-v2-subnav-${variant}`}
              contentClassName="min-w-0"
            >
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
            </SimpleV2Sample>
          ))}
        </SimpleV2Group>
        <SimpleV2Sample
          label="With icon + count"
          standard="Module Subnav"
          standardStatus="pilot"
          command="MODULE SUBNAV + PILL BAR + WITH ICON + WITH COUNT + SOLID PRIMARY ACTIVE"
          testId="simple-v2-subnav-icons"
          contentClassName="min-w-0"
        >
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
        </SimpleV2Sample>
      </SimpleV2Showcase>

      {/* 7. FILTERS */}
      <SimpleV2Showcase
        id="simple-v2-filters"
        title={t("uiStandards.tabFilters")}
        status="LOCKED"
        testId="ui-standards-simple-v2-filters"
        note={t("uiStandards.simpleFiltersNote")}
      >
        <SimpleV2Sample label="Status" standard="Chip" command="FILTER CHIP" testId="simple-v2-filter-status">
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
        </SimpleV2Sample>
        <SimpleV2Sample label="Direction" standard="Chip" command="FILTER CHIP" testId="simple-v2-filter-direction">
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
        </SimpleV2Sample>
        <SimpleV2Sample label="With counts" standard="Chip" command="FILTER CHIP + WITH COUNT" testId="simple-v2-filter-counts">
          <FilterChip selected>All 24</FilterChip>
          <FilterChip>Pending 6</FilterChip>
          <FilterChip>Completed 16</FilterChip>
        </SimpleV2Sample>
      </SimpleV2Showcase>

      {/* 8. CARDS */}
      <SimpleV2Showcase
        id="simple-v2-cards"
        title={t("uiStandards.tabCards")}
        status="LOCKED"
        testId="ui-standards-simple-v2-cards"
      >
        <SimpleV2Group title="Patterns">
          <SimpleV2Sample label="Basic" standard="Card" command="BASIC CARD + BORDERED" testId="simple-v2-card-basic" contentClassName="min-w-0">
            <Card treatment="bordered" className="p-3">
              <CardTitle as="h4" className="text-[length:var(--exits-text-sm)]">Basic</CardTitle>
              <CardDescription>Simple content card</CardDescription>
            </Card>
          </SimpleV2Sample>
          <SimpleV2Sample label="Summary" standard="Card" command="SUMMARY CARD + BORDERED" testId="simple-v2-card-summary" contentClassName="min-w-0">
            <Card treatment="bordered" className="p-3">
              <CardTitle as="h4" className="text-[length:var(--exits-text-sm)]">Order summary</CardTitle>
              <CardDescription>3 lines · ₱829.75</CardDescription>
            </Card>
          </SimpleV2Sample>
          <SimpleV2Sample label="KPI" standard="Card" command="KPI CARD + BORDERED" testId="simple-v2-card-kpi" contentClassName="min-w-0">
            <Card treatment="bordered" className="p-3">
              <CardDescription>Today</CardDescription>
              <CardTitle as="h4" className="text-[length:var(--exits-text-lg)] tabular-nums">₱12,450</CardTitle>
            </Card>
          </SimpleV2Sample>
          <SimpleV2Sample label="Action" standard="Card" command="ACTION CARD + INTERACTIVE + LIFT" testId="simple-v2-card-action" contentClassName="min-w-0">
            <Card treatment="bordered" interactive motion="lift" className="p-3 text-start">
              <CardTitle as="h4" className="text-[length:var(--exits-text-sm)]">Open report</CardTitle>
              <CardDescription>Interactive lift</CardDescription>
            </Card>
          </SimpleV2Sample>
          <SimpleV2Sample
            label="Entity"
            standard={["Card", "Chip"]}
            command="ENTITY CARD + BORDERED"
            commandContext="Active = STATUS CHIP SUCCESS"
            testId="simple-v2-card-entity"
            contentClassName="min-w-0"
          >
            <Card treatment="bordered" className="flex flex-col gap-2 p-3">
              <div className="flex items-center justify-between gap-2">
                <CardTitle as="h4" className="text-[length:var(--exits-text-sm)]">Mica Trading</CardTitle>
                <StatusChip tone="success">Active</StatusChip>
              </div>
              <CardDescription>Supplier · Cebu</CardDescription>
            </Card>
          </SimpleV2Sample>
          <SimpleV2Sample label="Product" standard="Card" command="PRODUCT CARD + BORDERED" testId="simple-v2-card-product" contentClassName="min-w-0">
            <Card treatment="bordered" className="p-3">
              <CardTitle as="h4" className="text-[length:var(--exits-text-sm)]">Apple</CardTitle>
              <CardDescription>SKU APL-01 · On hand 12</CardDescription>
            </Card>
          </SimpleV2Sample>
          <SimpleV2Sample label="Selectable" standard="Card" command="SELECTABLE CARD + SELECTED" testId="simple-v2-card-selectable" contentClassName="min-w-0">
            <Card treatment="selected" interactive className="p-3 text-start">
              <CardTitle as="h4" className="text-[length:var(--exits-text-sm)]">Main Branch</CardTitle>
              <CardDescription>Selected warehouse</CardDescription>
            </Card>
          </SimpleV2Sample>
          <SimpleV2Sample
            label="Status"
            standard={["Card", "Chip"]}
            command="STATUS CARD + BORDERED"
            commandContext="Draft = STATUS CHIP WARNING"
            testId="simple-v2-card-status"
            contentClassName="min-w-0"
          >
            <Card treatment="bordered" className="flex flex-col gap-2 p-3">
              <div className="flex items-center justify-between gap-2">
                <CardTitle as="h4" className="text-[length:var(--exits-text-sm)]">PO-1042</CardTitle>
                <StatusChip tone="warning">Draft</StatusChip>
              </div>
            </Card>
          </SimpleV2Sample>
          <SimpleV2Sample label="Compact" standard="Card" command="COMPACT CARD + BORDERED" testId="simple-v2-card-compact" contentClassName="min-w-0">
            <Card treatment="bordered" padding="compact" className="p-2">
              <CardTitle as="h4" className="text-[length:var(--exits-text-xs)]">Compact</CardTitle>
            </Card>
          </SimpleV2Sample>
          <SimpleV2Sample label="Featured" standard="Card" command="FEATURED CARD" testId="simple-v2-card-featured" contentClassName="min-w-0">
            <Card treatment="featured" className="p-3">
              <CardTitle as="h4" className="text-[length:var(--exits-text-sm)]">Featured</CardTitle>
              <CardDescription>Highlight treatment</CardDescription>
            </Card>
          </SimpleV2Sample>
        </SimpleV2Group>
        <SimpleV2Group title="Treatments">
          {(
            [
              ["Surface", "surface", "SURFACE CARD"],
              ["Bordered", "bordered", "BORDERED CARD"],
              ["Elevated", "elevated", "ELEVATED CARD"],
              ["Interactive", "bordered", "BORDERED CARD + INTERACTIVE + LIFT"],
              ["Selected", "selected", "SELECTED CARD"],
              ["Accent", "accent", "ACCENT CARD"],
            ] as const
          ).map(([label, treatment, command]) => (
            <SimpleV2Sample
              key={`${label}-${treatment}`}
              label={label}
              standard="Card"
              command={command}
              testId={`simple-v2-card-treatment-${label.toLowerCase()}`}
              contentClassName="min-w-0"
            >
              <Card
                treatment={treatment}
                interactive={label === "Interactive"}
                motion={label === "Interactive" ? "lift" : undefined}
                className="p-3 text-start"
                accentTone={treatment === "accent" ? "primary" : undefined}
              >
                <CardTitle as="h4" className="text-[length:var(--exits-text-sm)]">{label}</CardTitle>
                <CardDescription>Compact sample</CardDescription>
              </Card>
            </SimpleV2Sample>
          ))}
        </SimpleV2Group>
        <SimpleV2Group title="Motion">
          {(
            [
              ["Static", "none", "BORDERED CARD + STATIC"],
              ["Lift", "lift", "BORDERED CARD + INTERACTIVE + LIFT"],
              ["Expand", "expand", "BORDERED CARD + INTERACTIVE + EXPAND"],
            ] as const
          ).map(([label, motion, command]) => (
            <SimpleV2Sample
              key={motion}
              label={label}
              standard="Card"
              command={command}
              testId={`simple-v2-card-motion-${motion}`}
              contentClassName="min-w-0"
            >
              <Card
                treatment="bordered"
                interactive={motion !== "none"}
                motion={motion === "none" ? undefined : motion}
                className="p-3 text-start"
              >
                <CardTitle as="h4" className="text-[length:var(--exits-text-sm)]">{label}</CardTitle>
              </Card>
            </SimpleV2Sample>
          ))}
        </SimpleV2Group>
      </SimpleV2Showcase>

      {/* 9. TABLES */}
      <SimpleV2Showcase
        id="simple-v2-tables"
        title={t("uiStandards.tabTables")}
        status="LOCKED"
        testId="ui-standards-simple-v2-tables"
      >
        <SimpleV2Sample label="Simple table" standard="Table" command="SIMPLE TABLE" testId="simple-v2-table-basic" contentClassName="min-w-0 overflow-x-auto">
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
        </SimpleV2Sample>
        <SimpleV2Sample label="Full table" standard="Table" command="FULL TABLE" testId="simple-v2-table-full" contentClassName="min-w-0 overflow-x-auto">
          <ExitsTableContainer className="min-w-[22rem]">
            <ExitsTable>
              <ExitsTableHeader>
                <ExitsTableRow>
                  <ExitsTableHead>Product</ExitsTableHead>
                  <ExitsTableHead data-col-size="sku">SKU</ExitsTableHead>
                  <ExitsTableHead data-align="numeric">Qty</ExitsTableHead>
                  <ExitsTableHead data-align="numeric">Unit cost</ExitsTableHead>
                </ExitsTableRow>
              </ExitsTableHeader>
              <ExitsTableBody>
                {DEMO_ROWS.map((row) => (
                  <ExitsTableRow key={row.id}>
                    <ExitsTableCell>{row.name}</ExitsTableCell>
                    <ExitsTableCell>{row.sku}</ExitsTableCell>
                    <ExitsTableCell data-align="numeric">{row.qty}</ExitsTableCell>
                    <ExitsTableCell data-align="numeric">180.00</ExitsTableCell>
                  </ExitsTableRow>
                ))}
              </ExitsTableBody>
            </ExitsTable>
          </ExitsTableContainer>
        </SimpleV2Sample>
        <SimpleV2Sample label="With actions" standard="Table" command="FULL TABLE + ACTIONS ON" testId="simple-v2-table-actions" contentClassName="min-w-0 overflow-x-auto">
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
        </SimpleV2Sample>
        <SimpleV2Sample label="With multi select" standard="Table" command="FULL TABLE + MULTI SELECT ON" testId="simple-v2-table-select" contentClassName="min-w-0 overflow-x-auto">
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
        </SimpleV2Sample>
        <SimpleV2Sample label="With inline edit" standard="Table" command="FULL TABLE + INLINE EDIT ON" testId="simple-v2-table-inline" contentClassName="min-w-0 overflow-x-auto">
          <ExitsTableContainer className="min-w-[18rem]">
            <ExitsTable>
              <ExitsTableHeader>
                <ExitsTableRow>
                  <ExitsTableHead>Product</ExitsTableHead>
                  <ExitsTableHead>SKU</ExitsTableHead>
                </ExitsTableRow>
              </ExitsTableHeader>
              <ExitsTableBody>
                <ExitsTableRow data-editing="true">
                  <ExitsTableCell>Apple</ExitsTableCell>
                  <ExitsTableCell>
                    <input
                      className="w-full rounded border border-border bg-background px-2 py-1 text-[length:var(--exits-text-sm)]"
                      value={editSku}
                      onChange={(e) => setEditSku(e.target.value)}
                      aria-label="Edit Apple SKU"
                    />
                  </ExitsTableCell>
                </ExitsTableRow>
              </ExitsTableBody>
            </ExitsTable>
          </ExitsTableContainer>
        </SimpleV2Sample>
        <SimpleV2Sample label="With footer" standard="Table" command="FULL TABLE + FOOTER ON" testId="simple-v2-table-footer" contentClassName="min-w-0 overflow-x-auto">
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
        </SimpleV2Sample>
        <SimpleV2Sample label="With output" standard="Table" command="FULL TABLE + OUTPUT ON" testId="simple-v2-table-output" contentClassName="min-w-0 overflow-x-auto">
          <div className="grid min-w-0 gap-2">
            <ExitsTableContainer className="min-w-[16rem]">
              <ExitsTable>
                <ExitsTableHeader>
                  <ExitsTableRow>
                    <ExitsTableHead>Product</ExitsTableHead>
                    <ExitsTableHead data-align="numeric">Line</ExitsTableHead>
                  </ExitsTableRow>
                </ExitsTableHeader>
                <ExitsTableBody>
                  <ExitsTableRow>
                    <ExitsTableCell>Apple</ExitsTableCell>
                    <ExitsTableCell data-align="numeric">540.00</ExitsTableCell>
                  </ExitsTableRow>
                </ExitsTableBody>
              </ExitsTable>
            </ExitsTableContainer>
            <div className="flex justify-end text-[length:var(--exits-text-sm)] font-semibold tabular-nums" data-testid="simple-v2-table-output-total">
              Total 540.00
            </div>
          </div>
        </SimpleV2Sample>
      </SimpleV2Showcase>
    </div>
  );
}
