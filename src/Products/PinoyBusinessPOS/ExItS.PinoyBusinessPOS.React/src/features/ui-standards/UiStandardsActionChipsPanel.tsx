import { useState, type ComponentProps, type ReactNode } from "react";
import {
  ClipboardList,
  Factory,
  History,
  MoreHorizontal,
  PackageMinus,
  PackagePlus,
  Plus,
  RefreshCw,
  Search,
  Settings2,
  Shield,
  Trash2,
  Truck,
  UserPlus,
  Users,
  CalendarClock,
  ChevronRight,
} from "lucide-react";
import {
  ActionChipBar,
  type ActionChipItem,
  type ActionChipGroupLayout,
  type ActionChipShape,
  type ActionChipVisual,
} from "@/components/exits/ActionChipBar";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";
import { UiStandardsSection } from "@/features/ui-standards/UiStandardsSection";
import { UiStandardsSampleCard } from "@/features/ui-standards/UiStandardsSampleCard";

const ACTION_CONTEXT =
  "Use lightweight page/module actions. Keep navigation items as links and direct actions as buttons. Do not use tab/filter/module-subnav semantics.";

const INVENTORY_CONTEXT =
  "Use lightweight Inventory workflow shortcuts. Keep navigation items as links and direct actions as buttons.";

type DisclosureProps = {
  isOpen: (id: string) => boolean;
  setOpen: (id: string, open: boolean) => void;
};

function SampleCard({
  label,
  children,
  testId,
  hint,
  command,
  commandContext = ACTION_CONTEXT,
  explanatory,
  contentClassName,
}: {
  label: string;
  children: ReactNode;
  testId?: string;
  hint?: string;
  command?: string;
  commandContext?: string;
  explanatory?: boolean;
  contentClassName?: string;
}) {
  return (
    <UiStandardsSampleCard
      label={label}
      testId={testId}
      hint={hint}
      standard="Action Chip"
      standardStatus="pilot"
      command={command}
      commandContext={commandContext}
      explanatory={explanatory}
      contentClassName={contentClassName}
    >
      {children}
    </UiStandardsSampleCard>
  );
}

function StaticSampleGroup({ title, children }: { title: string; children: ReactNode }) {
  const slug = title
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-|-$/g, "");
  return (
    <section
      className="grid gap-2 border-t border-border pt-3 first:border-t-0 first:pt-0"
      data-testid={`ui-standards-action-chips-group-${slug}`}
    >
      <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-muted">{title}</h3>
      <div className="grid gap-2">{children}</div>
    </section>
  );
}

function MobileFrame({
  width,
  children,
  label,
}: {
  width: number;
  children: ReactNode;
  label?: string;
}) {
  return (
    <div className="grid gap-1">
      {label ? (
        <span className="text-[length:var(--exits-text-xs)] text-muted">
          {label} · {width}px
        </span>
      ) : (
        <span className="text-[length:var(--exits-text-xs)] text-muted">{width}px</span>
      )}
      <div
        className="overflow-hidden rounded-[var(--exits-radius-sm)] border border-dashed border-border bg-[var(--exits-surface)] p-2"
        style={{ width: `min(100%, ${width}px)` }}
        data-mobile-frame={width}
      >
        {children}
      </div>
    </div>
  );
}

function stockCountItem(overrides: Partial<ActionChipItem> = {}): ActionChipItem {
  return {
    key: "stock-count",
    label: "Stock Count",
    icon: ClipboardList,
    onSelect: () => undefined,
    testId: "action-chip-stock-count",
    ...overrides,
  };
}

function inventoryQuickActions(opts?: {
  primaryKey?: string;
  withCounts?: boolean;
}): ActionChipItem[] {
  const primaryKey = opts?.primaryKey ?? "expiring";
  const items: ActionChipItem[] = [
    {
      key: "low-stock",
      label: "Low stock settings",
      icon: Settings2,
      href: "/demo/low-stock",
      emphasis: primaryKey === "low-stock" ? "primary" : "default",
    },
    {
      key: "expiring",
      label: "Expiring stock",
      icon: CalendarClock,
      href: "/demo/expiring",
      emphasis: primaryKey === "expiring" ? "primary" : "default",
      count: opts?.withCounts ? 4 : undefined,
      countTone: "warning",
      countZeroMode: "hide",
    },
    {
      key: "stock-count",
      label: "Stock Count",
      icon: ClipboardList,
      href: "/demo/stock-count",
    },
    {
      key: "stock-use",
      label: "Stock Use",
      icon: PackageMinus,
      href: "/demo/stock-use",
    },
    {
      key: "waste",
      label: "Waste / Loss",
      icon: Trash2,
      href: "/demo/waste",
    },
    {
      key: "production",
      label: "Production",
      icon: Factory,
      href: "/demo/production",
    },
  ];
  return items;
}

type ActionChipBarPropsSections = NonNullable<ComponentProps<typeof ActionChipBar>["sections"]>;

function DemoBar({
  items,
  variant = "soft",
  shape = "soft",
  layout = "wrap",
  ariaLabel = "Action chips demo",
  testId,
  primaryTreatment = "tinted",
  fullWidthMobile,
  sections,
  overflowAfter,
  trailingItems,
  groupRole,
  listClassName,
  className,
}: {
  items: ReadonlyArray<ActionChipItem>;
  variant?: ActionChipVisual;
  shape?: ActionChipShape;
  layout?: ActionChipGroupLayout;
  ariaLabel?: string;
  testId?: string;
  primaryTreatment?: "tinted" | "solid";
  fullWidthMobile?: boolean;
  sections?: ActionChipBarPropsSections;
  overflowAfter?: number;
  trailingItems?: ReadonlyArray<ActionChipItem>;
  groupRole?: ComponentProps<typeof ActionChipBar>["groupRole"];
  listClassName?: string;
  className?: string;
}) {
  return (
    <ActionChipBar
      items={items}
      variant={variant}
      shape={shape}
      layout={layout}
      ariaLabel={ariaLabel}
      testId={testId}
      primaryTreatment={primaryTreatment}
      fullWidthMobile={fullWidthMobile}
      sections={sections}
      overflowAfter={overflowAfter}
      trailingItems={trailingItems}
      groupRole={groupRole}
      listClassName={listClassName}
      className={className}
    />
  );
}

export function UiStandardsActionChipsPanel({ isOpen, setOpen }: DisclosureProps) {
  const { t } = useI18n();
  const [refreshing, setRefreshing] = useState(false);

  const inventoryItems = inventoryQuickActions({ primaryKey: "expiring" });
  const inventoryPrimaryFirst = inventoryQuickActions({ primaryKey: "expiring" });

  return (
    <div className="grid gap-3" data-testid="ui-standards-action-chips-section">
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("uiStandards.actionChipsPilotLede")}</p>

      <SampleCard
        label="ACTION CHIP ≠ TABS / FILTER / MODULE SUBNAV / BUTTON"
        hint="Same labels, different jobs"
        explanatory
      >
        <div className="grid gap-1 text-[length:var(--exits-text-xs)] text-muted">
          <div>
            <strong className="text-foreground">ACTION CHIP</strong> — lightweight quick action (Refresh,
            Export, Scan).
          </div>
          <div>
            <strong className="text-foreground">NAV ACTION CHIP</strong> — lightweight shortcut to another
            workflow (Stock Count, Waste / Loss). Not structural module nav.
          </div>
          <div>
            <strong className="text-foreground">FILTER CHIP</strong> — changes the current dataset.
          </div>
          <div>
            <strong className="text-foreground">MODULE SUBNAV</strong> — sibling routes that define a
            module&apos;s structure.
          </div>
          <div>
            <strong className="text-foreground">BUTTON</strong> — primary / destructive / form submission.
          </div>
        </div>
      </SampleCard>

      <UiStandardsSection
        id="action-chips.overview"
        title={t("uiStandards.actionChipsOverviewTitle")}
        description={t("uiStandards.actionChipsOverviewLede")}
        summary="PILOT"
        open={isOpen("action-chips.overview")}
        onOpenChange={(open) => setOpen("action-chips.overview", open)}
      >
        <div className="grid gap-2 md:grid-cols-2">
          <SampleCard label="ACTION CHIP" command="ACTION CHIP + SOFT + WITH ICON" testId="ui-standards-action-chip-overview-action">
            <DemoBar
              ariaLabel="Action chip overview"
              items={[
                {
                  key: "refresh",
                  label: "Refresh",
                  icon: RefreshCw,
                  onSelect: () => undefined,
                },
              ]}
            />
          </SampleCard>
          <SampleCard
            label="NAV ACTION CHIP"
            command="NAV ACTION CHIP + SOFT + WITH ICON"
            testId="ui-standards-action-chip-overview-nav"
          >
            <DemoBar
              ariaLabel="Nav action chip overview"
              items={[
                {
                  key: "stock-count",
                  label: "Stock Count",
                  icon: ClipboardList,
                  href: "/demo/stock-count",
                },
              ]}
            />
          </SampleCard>
          <SampleCard
            label="FILTER CHIP (NOT ACTION CHIP)"
            explanatory
            hint="Dataset filter — use Filter Chip / ExitsChipBar variant=filter"
            testId="ui-standards-action-chip-overview-filter"
          >
            <ExitsChipBar
              variant="filter"
              ariaLabel="Filter comparison"
              items={[
                { key: "all", label: "All", state: "active", onSelect: () => undefined },
                { key: "active", label: "Active", state: "idle", onSelect: () => undefined },
              ]}
            />
          </SampleCard>
          <SampleCard
            label="BUTTON (NOT ACTION CHIP)"
            explanatory
            hint="Strong hierarchy CTA — use locked Button Standard"
            testId="ui-standards-action-chip-overview-button"
          >
            <Button type="button" shape="soft">
              <Plus className="size-4" aria-hidden />
              New transfer
            </Button>
          </SampleCard>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="action-chips.variants"
        title={t("uiStandards.actionChipsVariantsTitle")}
        description={t("uiStandards.actionChipsVariantsLede")}
        summary="VISUALS"
        open={isOpen("action-chips.variants")}
        onOpenChange={(open) => setOpen("action-chips.variants", open)}
      >
        <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
          {(
            [
              ["SOFT", "soft", "ACTION CHIP + SOFT"],
              ["OUTLINE", "outline", "ACTION CHIP + OUTLINE"],
              ["GHOST", "ghost", "ACTION CHIP + GHOST"],
              ["SOLID PRIMARY", "solidPrimary", "ACTION CHIP + SOLID PRIMARY"],
              ["TINTED PRIMARY", "tintedPrimary", "ACTION CHIP + TINTED PRIMARY"],
              ["ELEVATED", "elevated", "ACTION CHIP + ELEVATED"],
              ["GRADIENT", "gradient", "ACTION CHIP + GRADIENT"],
            ] as const
          ).map(([label, visual, command]) => (
            <SampleCard
              key={visual}
              label={label}
              command={command}
              hint={visual === "gradient" ? "SPECIAL USE / NOT DEFAULT" : undefined}
              testId={`ui-standards-action-chip-variant-${visual}`}
            >
              <DemoBar
                variant={visual}
                ariaLabel={`${label} variant`}
                items={[stockCountItem({ key: visual, visual })]}
              />
            </SampleCard>
          ))}
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="action-chips.shapes"
        title={t("uiStandards.actionChipsShapesTitle")}
        description={t("uiStandards.actionChipsShapesLede")}
        summary="SHAPES"
        open={isOpen("action-chips.shapes")}
        onOpenChange={(open) => setOpen("action-chips.shapes", open)}
      >
        <div className="grid gap-2 sm:grid-cols-3">
          {(
            [
              ["PILL", "pill", "ACTION CHIP + PILL"],
              ["SOFT SHAPE", "soft", "ACTION CHIP + SOFT SHAPE"],
              ["SQUARE", "square", "ACTION CHIP + SQUARE"],
            ] as const
          ).map(([label, shape, command]) => (
            <SampleCard key={shape} label={label} command={command} testId={`ui-standards-action-chip-shape-${shape}`}>
              <DemoBar shape={shape} items={[stockCountItem({ key: `shape-${shape}` })]} ariaLabel={label} />
            </SampleCard>
          ))}
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="action-chips.content"
        title={t("uiStandards.actionChipsContentTitle")}
        description={t("uiStandards.actionChipsContentLede")}
        summary="CONTENT"
        open={isOpen("action-chips.content")}
        onOpenChange={(open) => setOpen("action-chips.content", open)}
      >
        <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
          <SampleCard label="TEXT ONLY" command="ACTION CHIP + SOFT" testId="ui-standards-action-chip-text-only">
            <DemoBar items={[{ key: "t", label: "Export", onSelect: () => undefined }]} />
          </SampleCard>
          <SampleCard label="WITH ICON" command="ACTION CHIP + SOFT + WITH ICON" testId="ui-standards-action-chip-with-icon">
            <DemoBar items={[stockCountItem()]} />
          </SampleCard>
          <SampleCard
            label="TRAILING ICON"
            command="ACTION CHIP + SOFT + TRAILING ICON"
            testId="ui-standards-action-chip-trailing-icon"
          >
            <DemoBar
              items={[
                {
                  key: "go",
                  label: "Open workflow",
                  trailingIcon: ChevronRight,
                  href: "/demo/go",
                },
              ]}
            />
          </SampleCard>
          <SampleCard label="ICON ONLY" command="ACTION CHIP + ICON ONLY" testId="ui-standards-action-chip-icon-only">
            <DemoBar
              items={[
                {
                  key: "refresh",
                  label: "Refresh",
                  icon: RefreshCw,
                  iconOnly: true,
                  ariaLabel: "Refresh",
                  title: "Refresh",
                  onSelect: () => undefined,
                },
              ]}
            />
          </SampleCard>
          <SampleCard
            label="WITH COUNT"
            command="NAV ACTION CHIP + WITH ICON + WITH COUNT + HIDE ZERO"
            testId="ui-standards-action-chip-with-count"
          >
            <DemoBar
              items={[
                {
                  key: "incoming",
                  label: "Incoming stock",
                  icon: PackagePlus,
                  href: "/demo/incoming",
                  count: 2,
                  countTone: "neutral",
                  countZeroMode: "hide",
                },
                {
                  key: "zero",
                  label: "Ready",
                  icon: Truck,
                  href: "/demo/ready",
                  count: 0,
                  countZeroMode: "hide",
                },
              ]}
            />
          </SampleCard>
          <SampleCard
            label="PRIMARY EMPHASIS"
            command="ACTION CHIP + TINTED PRIMARY + WITH ICON"
            testId="ui-standards-action-chip-primary-emphasis"
          >
            <DemoBar
              items={[
                {
                  key: "expiring",
                  label: "Expiring stock",
                  icon: CalendarClock,
                  href: "/demo/expiring",
                  emphasis: "primary",
                },
                stockCountItem({ key: "sc2", href: "/demo/sc" }),
              ]}
            />
          </SampleCard>
          <SampleCard label="DISABLED" command="ACTION CHIP + DISABLED" testId="ui-standards-action-chip-disabled">
            <DemoBar
              items={[
                {
                  key: "d",
                  label: "Export",
                  icon: History,
                  onSelect: () => undefined,
                  disabled: true,
                },
              ]}
            />
          </SampleCard>
          <SampleCard label="LOADING" command="ACTION CHIP + LOADING" testId="ui-standards-action-chip-loading">
            <DemoBar
              items={[
                {
                  key: "r",
                  label: refreshing ? "Refreshing" : "Refresh",
                  icon: RefreshCw,
                  loading: refreshing,
                  onSelect: () => {
                    setRefreshing(true);
                    window.setTimeout(() => setRefreshing(false), 800);
                  },
                },
              ]}
            />
          </SampleCard>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="action-chips.groups"
        title={t("uiStandards.actionChipsGroupsTitle")}
        description={t("uiStandards.actionChipsGroupsLede")}
        summary="GROUPS"
        open={isOpen("action-chips.groups")}
        onOpenChange={(open) => setOpen("action-chips.groups", open)}
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="INLINE">
            <SampleCard label="DESKTOP" command="ACTION CHIP GROUP + INLINE" testId="ui-standards-action-chip-group-inline">
              <DemoBar
                layout="inline"
                items={inventoryItems.slice(2, 5)}
                ariaLabel="Inline actions"
              />
            </SampleCard>
            <SampleCard label="MOBILE" command="ACTION CHIP GROUP + INLINE" testId="ui-standards-action-chip-group-inline-mobile">
              <MobileFrame width={360}>
                <DemoBar layout="inline" items={inventoryItems.slice(2, 5)} ariaLabel="Inline mobile" />
              </MobileFrame>
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="WRAP">
            <SampleCard label="DESKTOP" command="ACTION CHIP GROUP + WRAP" testId="ui-standards-action-chip-group-wrap">
              <DemoBar layout="wrap" items={inventoryItems} ariaLabel="Wrap actions" />
            </SampleCard>
            <SampleCard
              label="MOBILE WRAP"
              command="ACTION CHIP GROUP + WRAP MOBILE"
              testId="ui-standards-action-chip-group-wrap-mobile"
            >
              <div className="grid gap-2">
                {[320, 360, 390, 430].map((w) => (
                  <MobileFrame key={w} width={w}>
                    <DemoBar layout="wrap" items={inventoryItems} ariaLabel={`Wrap ${w}`} />
                  </MobileFrame>
                ))}
              </div>
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="SCROLL">
            <SampleCard label="DESKTOP" command="ACTION CHIP GROUP + SCROLL" testId="ui-standards-action-chip-group-scroll">
              <DemoBar layout="scroll" items={inventoryItems} ariaLabel="Scroll actions" className="max-w-xl" />
            </SampleCard>
            <SampleCard
              label="MOBILE SCROLL"
              command="ACTION CHIP GROUP + SCROLL MOBILE"
              testId="ui-standards-action-chip-group-scroll-mobile"
            >
              <MobileFrame width={360}>
                <DemoBar layout="scroll" items={inventoryItems} ariaLabel="Scroll mobile" />
              </MobileFrame>
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="RESPONSIVE AUTO">
            <SampleCard
              label="DESKTOP / TABLET"
              command="ACTION CHIP GROUP + RESPONSIVE AUTO"
              testId="ui-standards-action-chip-group-responsive"
            >
              <DemoBar layout="responsiveAuto" items={inventoryItems} ariaLabel="Responsive auto" />
            </SampleCard>
            <SampleCard
              label="MOBILE AUTO"
              command="ACTION CHIP GROUP + RESPONSIVE AUTO"
              testId="ui-standards-action-chip-group-responsive-mobile"
            >
              <div className="grid gap-2">
                {[320, 390].map((w) => (
                  <MobileFrame key={w} width={w}>
                    <DemoBar layout="responsiveAuto" items={inventoryItems} ariaLabel={`Auto ${w}`} />
                  </MobileFrame>
                ))}
              </div>
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="GRID">
            <SampleCard
              label="DESKTOP / TABLET"
              command="ACTION CHIP GROUP + GRID + WITH ICON"
              testId="ui-standards-action-chip-group-grid"
            >
              <DemoBar
                layout="grid"
                items={inventoryItems.slice(2)}
                ariaLabel="Grid actions"
                listClassName="[&_.exits-action-chip]:w-full [&_.exits-action-chip]:justify-start"
              />
            </SampleCard>
            <SampleCard
              label="MOBILE GRID"
              command="ACTION CHIP GROUP + GRID MOBILE"
              testId="ui-standards-action-chip-group-grid-mobile"
            >
              <MobileFrame width={390}>
                <DemoBar
                  layout="grid"
                  items={inventoryItems.slice(2)}
                  ariaLabel="Grid mobile"
                  listClassName="[&_.exits-action-chip]:w-full [&_.exits-action-chip]:justify-start"
                />
              </MobileFrame>
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="PRIMARY FIRST">
            <SampleCard
              label="ONE PRIMARY + SECONDARIES"
              command="ACTION CHIP GROUP + PRIMARY FIRST"
              testId="ui-standards-action-chip-group-primary-first"
            >
              <DemoBar layout="wrap" items={inventoryPrimaryFirst} ariaLabel="Primary first" />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="SECTIONED">
            <SampleCard
              label="CATEGORIES"
              command="ACTION CHIP GROUP + SECTIONED"
              hint="Only when enough actions justify labels"
              testId="ui-standards-action-chip-group-sectioned"
            >
              <DemoBar
                items={inventoryItems.slice(2)}
                sections={[
                  { id: "stock", label: "Stock", itemKeys: ["stock-count", "stock-use"] },
                  { id: "ops", label: "Operations", itemKeys: ["waste", "production"] },
                ]}
                ariaLabel="Sectioned actions"
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="OVERFLOW">
            <SampleCard
              label="MORE ▾"
              command="ACTION CHIP GROUP + OVERFLOW"
              hint="Candidate only — do not hide primary actions"
              testId="ui-standards-action-chip-group-overflow"
            >
              <DemoBar
                layout="inline"
                overflowAfter={3}
                items={inventoryItems.map((item) => ({
                  ...item,
                  href: undefined,
                  onSelect: () => undefined,
                }))}
                ariaLabel="Overflow actions"
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="PRIMARY BUTTON + ACTION CHIPS">
            <SampleCard
              label="MIXED COMPOSITION"
              command="PRIMARY BUTTON + ACTION CHIP GROUP"
              testId="ui-standards-action-chip-group-button-plus"
            >
              <div className="flex min-w-0 flex-wrap items-center gap-2">
                <Button type="button" shape="soft">
                  <Plus className="size-4" aria-hidden />
                  New transfer
                </Button>
                <DemoBar
                  layout="inline"
                  groupRole="toolbar"
                  items={[
                    { key: "sc", label: "Stock Count", icon: ClipboardList, href: "/demo/sc" },
                    { key: "su", label: "Stock Use", icon: PackageMinus, href: "/demo/su" },
                    { key: "h", label: "History", icon: History, href: "/demo/h" },
                  ]}
                  ariaLabel="Secondary shortcuts"
                />
              </div>
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="TRAILING UTILITIES">
            <SampleCard
              label="START ACTIONS + END UTILITIES"
              command="ACTION CHIP GROUP + TRAILING UTILITIES"
              testId="ui-standards-action-chip-group-trailing"
            >
              <DemoBar
                layout="wrap"
                items={inventoryItems.slice(2, 5)}
                trailingItems={[
                  {
                    key: "refresh",
                    label: "Refresh",
                    icon: RefreshCw,
                    iconOnly: true,
                    ariaLabel: "Refresh",
                    title: "Refresh",
                    onSelect: () => undefined,
                  },
                  {
                    key: "more",
                    label: "More",
                    icon: MoreHorizontal,
                    iconOnly: true,
                    ariaLabel: "More",
                    title: "More",
                    onSelect: () => undefined,
                  },
                ]}
                ariaLabel="Trailing utilities"
                testId="ui-standards-action-chip-trailing-demo"
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="FULL WIDTH MOBILE">
            <SampleCard
              label="SINGULAR SHORTCUT"
              command="ACTION CHIP + FULL WIDTH MOBILE"
              testId="ui-standards-action-chip-full-width-mobile"
            >
              <MobileFrame width={360}>
                <DemoBar
                  fullWidthMobile
                  items={[
                    {
                      key: "start",
                      label: "Start count",
                      icon: ClipboardList,
                      onSelect: () => undefined,
                      emphasis: "primary",
                    },
                  ]}
                  ariaLabel="Full width mobile"
                />
              </MobileFrame>
            </SampleCard>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="action-chips.real-world"
        title={t("uiStandards.actionChipsRealWorldTitle")}
        description={t("uiStandards.actionChipsRealWorldLede")}
        summary="EXAMPLES"
        open={isOpen("action-chips.real-world")}
        onOpenChange={(open) => setOpen("action-chips.real-world", open)}
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="INVENTORY">
            {(
              [
                ["WRAP", "wrap", "ACTION CHIP GROUP + WRAP + WITH ICON"],
                ["SCROLL", "scroll", "ACTION CHIP GROUP + SCROLL + WITH ICON"],
                ["GRID", "grid", "ACTION CHIP GROUP + GRID + WITH ICON"],
                ["PRIMARY FIRST", "wrap", "ACTION CHIP GROUP + PRIMARY FIRST + WITH ICON"],
                ["RESPONSIVE AUTO", "responsiveAuto", "ACTION CHIP GROUP + RESPONSIVE AUTO + WITH ICON"],
              ] as const
            ).map(([label, layout, command]) => (
              <SampleCard
                key={label}
                label={label}
                command={command}
                commandContext={INVENTORY_CONTEXT}
                testId={`ui-standards-action-chip-rw-inventory-${layout === "wrap" && label === "PRIMARY FIRST" ? "primary-first" : layout}`}
              >
                <div className="grid gap-2">
                  <DemoBar
                    layout={layout}
                    items={label === "PRIMARY FIRST" ? inventoryPrimaryFirst : inventoryItems}
                    ariaLabel={`Inventory ${label}`}
                    listClassName={
                      layout === "grid"
                        ? "[&_.exits-action-chip]:w-full [&_.exits-action-chip]:justify-start"
                        : undefined
                    }
                  />
                  <MobileFrame width={360} label="Mobile">
                    <DemoBar
                      layout={layout}
                      items={label === "PRIMARY FIRST" ? inventoryPrimaryFirst : inventoryItems}
                      ariaLabel={`Inventory ${label} mobile`}
                      listClassName={
                        layout === "grid"
                          ? "[&_.exits-action-chip]:w-full [&_.exits-action-chip]:justify-start"
                          : undefined
                      }
                    />
                  </MobileFrame>
                </div>
              </SampleCard>
            ))}
          </StaticSampleGroup>

          <StaticSampleGroup title="PURCHASING">
            <SampleCard
              label="PURCHASING SHORTCUTS"
              command="ACTION CHIP GROUP + WRAP + WITH ICON"
              testId="ui-standards-action-chip-rw-purchasing"
            >
              <DemoBar
                layout="wrap"
                items={[
                  { key: "po", label: "New purchase order", icon: Plus, href: "/demo/po", emphasis: "primary" },
                  { key: "direct", label: "Direct purchase", icon: PackagePlus, href: "/demo/direct" },
                  { key: "receive", label: "Receive stock", icon: Truck, href: "/demo/receive" },
                  { key: "suppliers", label: "Suppliers", icon: Users, href: "/demo/suppliers" },
                ]}
                ariaLabel="Purchasing shortcuts"
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="STOCK COUNT">
            <SampleCard
              label="STOCK COUNT TOOLS"
              command="ACTION CHIP GROUP + INLINE + WITH ICON"
              testId="ui-standards-action-chip-rw-stock-count"
            >
              <DemoBar
                layout="inline"
                items={[
                  { key: "new", label: "New count", icon: Plus, onSelect: () => undefined, emphasis: "primary" },
                  { key: "history", label: "History", icon: History, href: "/demo/history" },
                  { key: "refresh", label: "Refresh", icon: RefreshCw, onSelect: () => undefined },
                ]}
                ariaLabel="Stock count tools"
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="RETURNS">
            <SampleCard
              label="RETURNS SHORTCUTS"
              command="ACTION CHIP GROUP + WRAP + WITH ICON"
              testId="ui-standards-action-chip-rw-returns"
            >
              <DemoBar
                layout="wrap"
                items={[
                  { key: "new", label: "New return", icon: Plus, onSelect: () => undefined, emphasis: "primary" },
                  { key: "search", label: "Search receipt", icon: Search, onSelect: () => undefined },
                  { key: "history", label: "Return history", icon: History, href: "/demo/returns-history" },
                ]}
                ariaLabel="Returns shortcuts"
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="STAFF">
            <SampleCard
              label="STAFF SHORTCUTS"
              command="ACTION CHIP GROUP + WRAP + WITH ICON"
              testId="ui-standards-action-chip-rw-staff"
            >
              <DemoBar
                layout="wrap"
                items={[
                  { key: "invite", label: "Invite staff", icon: UserPlus, onSelect: () => undefined, emphasis: "primary" },
                  { key: "roles", label: "Roles", icon: Users, href: "/demo/roles" },
                  { key: "perms", label: "Permissions", icon: Shield, href: "/demo/permissions" },
                ]}
                ariaLabel="Staff shortcuts"
              />
            </SampleCard>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="action-chips.cheatsheet"
        title={t("uiStandards.actionChipsCheatTitle")}
        description={t("uiStandards.actionChipsCheatLede")}
        summary="PILOT / CANDIDATE"
        open={isOpen("action-chips.cheatsheet")}
        onOpenChange={(open) => setOpen("action-chips.cheatsheet", open)}
      >
        <SampleCard label="PILOT STATUS" explanatory>
          <div className="grid gap-1 text-[length:var(--exits-text-xs)] text-muted" data-testid="ui-standards-action-chips-cheatsheet">
            <div>{t("uiStandards.actionChipsPilotBadge")}</div>
            <div>Baseline production: ExitsChipBar variant=&quot;actions&quot;</div>
            <div>Pilot component: ActionChipBar</div>
            <pre className="m-0 mt-2 overflow-auto whitespace-pre-wrap font-mono text-[length:var(--exits-text-xs)] text-foreground">
{`ACTION CHIP
NAV ACTION CHIP
ACTION CHIP + SOFT | OUTLINE | GHOST | SOLID PRIMARY | TINTED PRIMARY | ELEVATED | GRADIENT
ACTION CHIP + PILL | SOFT SHAPE | SQUARE
ACTION CHIP + WITH ICON | TRAILING ICON | ICON ONLY | WITH COUNT | DISABLED | LOADING
ACTION CHIP GROUP + INLINE | WRAP | SCROLL | RESPONSIVE AUTO | GRID | PRIMARY FIRST | SECTIONED | OVERFLOW
ACTION CHIP GROUP + WRAP MOBILE | SCROLL MOBILE | GRID MOBILE
ACTION CHIP + FULL WIDTH MOBILE
PRIMARY BUTTON + ACTION CHIP GROUP`}
            </pre>
          </div>
        </SampleCard>
      </UiStandardsSection>
    </div>
  );
}
