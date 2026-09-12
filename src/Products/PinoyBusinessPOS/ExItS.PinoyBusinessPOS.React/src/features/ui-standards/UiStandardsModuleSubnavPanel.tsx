import { type ReactNode } from "react";
import { MemoryRouter } from "react-router-dom";
import {
  ClipboardList,
  Inbox,
  PackageCheck,
  Settings,
  Truck,
  Users,
} from "lucide-react";
import {
  ModuleSubnav,
  type ModuleSubnavActiveTreatment,
  type ModuleSubnavItem,
  type ModuleSubnavLayout,
  type ModuleSubnavVariant,
} from "@/components/exits/ModuleSubnav";
import { useI18n } from "@/i18n/I18nProvider";
import { UiStandardsSection } from "@/features/ui-standards/UiStandardsSection";
import { UiStandardsSampleCard } from "@/features/ui-standards/UiStandardsSampleCard";

const NAV_CONTEXT =
  'Use related route navigation with aria-current="page"; do not use tab/tabpanel semantics.';

const ACTIVE_PATH = "/demo/incoming";

function SampleCard({
  label,
  children,
  testId,
  hint,
  command,
  commandContext = NAV_CONTEXT,
  explanatory,
}: {
  label: string;
  children: ReactNode;
  testId?: string;
  hint?: string;
  command?: string;
  commandContext?: string;
  explanatory?: boolean;
}) {
  return (
    <UiStandardsSampleCard
      label={label}
      testId={testId}
      hint={hint}
      standard="Module Subnav"
      standardStatus="pilot"
      command={command}
      commandContext={commandContext}
      explanatory={explanatory}
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
      data-testid={`ui-standards-module-subnav-group-${slug}`}
    >
      <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-muted">{title}</h3>
      <div className="grid gap-2">{children}</div>
    </section>
  );
}

function DemoRouter({
  children,
  initialPath = ACTIVE_PATH,
}: {
  children: ReactNode;
  initialPath?: string;
}) {
  return <MemoryRouter initialEntries={[initialPath]}>{children}</MemoryRouter>;
}

type PurchasingOptions = {
  withIcons?: boolean;
  withCounts?: boolean;
  semanticIncoming?: boolean;
};

function purchasingItems({
  withIcons = false,
  withCounts = false,
  semanticIncoming = false,
}: PurchasingOptions = {}): ModuleSubnavItem[] {
  const base: Array<{
    key: string;
    label: string;
    to: string;
    icon: typeof ClipboardList;
    count: number;
  }> = [
    { key: "po", label: "Purchase orders", to: "/demo/po", icon: ClipboardList, count: 0 },
    { key: "incoming", label: "Incoming orders", to: "/demo/incoming", icon: Inbox, count: 2 },
    { key: "receive", label: "Ready to receive", to: "/demo/receive", icon: Truck, count: 0 },
    { key: "direct", label: "Direct purchases", to: "/demo/direct", icon: PackageCheck, count: 0 },
    { key: "suppliers", label: "Suppliers", to: "/demo/suppliers", icon: Users, count: 0 },
  ];

  return base.map((item) => ({
    key: item.key,
    label: item.label,
    to: item.to,
    icon: withIcons ? item.icon : undefined,
    count: withCounts ? item.count : undefined,
    countTone:
      withCounts && semanticIncoming && item.key === "incoming" ? "warning" : withCounts ? "neutral" : undefined,
    testId: `ui-standards-module-subnav-item-${item.key}`,
  }));
}

function SubnavDemo({
  variant,
  layout,
  activeTreatment,
  scrollable,
  ariaLabel,
  testId,
  items,
  className,
  initialPath = ACTIVE_PATH,
}: {
  variant: ModuleSubnavVariant;
  layout?: ModuleSubnavLayout;
  activeTreatment?: ModuleSubnavActiveTreatment;
  scrollable?: boolean;
  ariaLabel: string;
  testId: string;
  items: ReadonlyArray<ModuleSubnavItem>;
  className?: string;
  initialPath?: string;
}) {
  return (
    <DemoRouter initialPath={initialPath}>
      <ModuleSubnav
        variant={variant}
        layout={layout}
        activeTreatment={activeTreatment}
        scrollable={scrollable}
        ariaLabel={ariaLabel}
        testId={testId}
        items={items}
        className={className}
      />
    </DemoRouter>
  );
}

type DisclosureProps = {
  isOpen: (id: string) => boolean;
  setOpen: (id: string, open: boolean) => void;
};

export function UiStandardsModuleSubnavPanel({ isOpen, setOpen }: DisclosureProps) {
  const { t } = useI18n();

  const comparisonItems = purchasingItems({ withIcons: true, withCounts: true });
  const textOnly = purchasingItems();
  const withIcons = purchasingItems({ withIcons: true });
  const withNeutralCounts = purchasingItems({ withCounts: true });
  const withSemanticCounts = purchasingItems({ withCounts: true, semanticIncoming: true });
  const iconCount = purchasingItems({ withIcons: true, withCounts: true });
  const iconCountSemantic = purchasingItems({
    withIcons: true,
    withCounts: true,
    semanticIncoming: true,
  });

  const ordersItems: ModuleSubnavItem[] = [
    { key: "all", label: "All", to: "/demo/orders/all", count: 24, countTone: "neutral" },
    { key: "pending", label: "Pending", to: "/demo/orders/pending", count: 6, countTone: "neutral" },
    {
      key: "completed",
      label: "Completed",
      to: "/demo/orders/completed",
      count: 16,
      countTone: "neutral",
    },
    {
      key: "cancelled",
      label: "Cancelled",
      to: "/demo/orders/cancelled",
      count: 2,
      countTone: "neutral",
    },
  ];

  const inventoryItems: ModuleSubnavItem[] = [
    { key: "onhand", label: "On hand", to: "/demo/inventory/onhand" },
    { key: "low", label: "Low stock", to: "/demo/inventory/low", count: 12, countTone: "warning" },
    {
      key: "expiring",
      label: "Expiring",
      to: "/demo/inventory/expiring",
      count: 4,
      countTone: "warning",
    },
    { key: "movements", label: "Movements", to: "/demo/inventory/movements" },
  ];

  const settingsItems: ModuleSubnavItem[] = [
    { key: "general", label: "General", to: "/demo/settings/general", icon: Settings },
    { key: "branches", label: "Branches", to: "/demo/settings/branches", count: 3, countTone: "neutral" },
    { key: "users", label: "Users", to: "/demo/settings/users", icon: Users, count: 10, countTone: "neutral" },
    { key: "permissions", label: "Permissions", to: "/demo/settings/permissions" },
  ];

  return (
    <div className="grid gap-3" data-testid="ui-standards-module-subnav-section">
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("uiStandards.moduleSubnavPilotLede")}
      </p>

      <SampleCard
        label="MODULE SUBNAV ≠ TABS"
        hint="Route destinations inside one module — not in-page tab panels"
        explanatory
      >
        <div className="grid gap-1 text-[length:var(--exits-text-xs)] text-muted">
          <div>
            <strong className="text-foreground">MODULE SUBNAV</strong> — related pages (Purchase
            orders → Incoming → Receive). Uses <code className="text-foreground">nav</code> +{" "}
            <code className="text-foreground">aria-current=&quot;page&quot;</code>.
          </div>
          <div>
            <strong className="text-foreground">TABS</strong> — switch content in the same view
            (Overview / Details / History). Uses tablist / tab / tabpanel.
          </div>
          <div>Do not fake Tabs semantics for module routes. Prefer Module Subnav for Purchasing.</div>
        </div>
      </SampleCard>

      <UiStandardsSection
        id="module-subnav.comparison"
        title={t("uiStandards.moduleSubnavComparisonTitle")}
        description="Same Purchasing destinations across six visual variants. PILOT / CANDIDATE."
        summary="UNDERLINE · SOFT · PILL · PILL BAR · COMPACT · VERTICAL"
        open={isOpen("module-subnav.comparison")}
        onOpenChange={(open) => setOpen("module-subnav.comparison", open)}
        testId="ui-standards-module-subnav-comparison"
      >
        <div className="grid gap-3">
          {(
            [
              ["UNDERLINE", "underline", "MODULE SUBNAV + UNDERLINE"],
              ["SOFT", "soft", "MODULE SUBNAV + SOFT"],
              ["PILL", "pill", "MODULE SUBNAV + PILL"],
              ["PILL BAR", "pillBar", "MODULE SUBNAV + PILL BAR"],
              ["COMPACT", "compact", "MODULE SUBNAV + COMPACT"],
              ["VERTICAL", "vertical", "MODULE SUBNAV + VERTICAL"],
            ] as const
          ).map(([label, variant, command]) => (
            <StaticSampleGroup key={variant} title={label}>
              <SampleCard
                label={`${label} · purchasing destinations`}
                command={command}
                testId={`ui-standards-module-subnav-comparison-${variant}`}
              >
                <SubnavDemo
                  variant={variant}
                  ariaLabel={`${label} module subnav comparison`}
                  testId={`ui-standards-module-subnav-demo-${variant}`}
                  items={comparisonItems}
                  activeTreatment={variant === "pillBar" || variant === "pill" ? "solid" : undefined}
                />
              </SampleCard>
            </StaticSampleGroup>
          ))}
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="module-subnav.icons"
        title={t("uiStandards.moduleSubnavIconsTitle")}
        description="Icons are optional and independent of variant. Compare SOFT and PILL BAR."
        summary="WITHOUT ICONS · WITH ICONS"
        open={isOpen("module-subnav.icons")}
        onOpenChange={(open) => setOpen("module-subnav.icons", open)}
        testId="ui-standards-module-subnav-icons"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="SOFT">
            <SampleCard label="WITHOUT ICONS" command="MODULE SUBNAV + SOFT + NO ICON">
              <SubnavDemo
                variant="soft"
                ariaLabel="Soft without icons"
                testId="ui-standards-module-subnav-icons-soft-none"
                items={textOnly}
              />
            </SampleCard>
            <SampleCard label="WITH ICONS" command="MODULE SUBNAV + SOFT + WITH ICON">
              <SubnavDemo
                variant="soft"
                ariaLabel="Soft with icons"
                testId="ui-standards-module-subnav-icons-soft-with"
                items={withIcons}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="PILL BAR">
            <SampleCard label="WITHOUT ICONS" command="MODULE SUBNAV + PILL BAR + NO ICON">
              <SubnavDemo
                variant="pillBar"
                ariaLabel="Pill bar without icons"
                testId="ui-standards-module-subnav-icons-pillbar-none"
                items={textOnly}
              />
            </SampleCard>
            <SampleCard label="WITH ICONS" command="MODULE SUBNAV + PILL BAR + WITH ICON">
              <SubnavDemo
                variant="pillBar"
                ariaLabel="Pill bar with icons"
                testId="ui-standards-module-subnav-icons-pillbar-with"
                items={withIcons}
              />
            </SampleCard>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="module-subnav.counts"
        title={t("uiStandards.moduleSubnavCountsTitle")}
        description="CountBadge optional. Neutral by default; semantic WARNING only when meaning requires it."
        summary="NO COUNT · NEUTRAL · SEMANTIC"
        open={isOpen("module-subnav.counts")}
        onOpenChange={(open) => setOpen("module-subnav.counts", open)}
        testId="ui-standards-module-subnav-counts"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="COUNT OPTIONS">
            <SampleCard label="WITHOUT COUNT" command="MODULE SUBNAV + SOFT + NO COUNT">
              <SubnavDemo
                variant="soft"
                ariaLabel="Without counts"
                testId="ui-standards-module-subnav-counts-none"
                items={textOnly}
              />
            </SampleCard>
            <SampleCard
              label="WITH COUNT (neutral)"
              command="MODULE SUBNAV + SOFT + WITH COUNT + NEUTRAL COUNT"
            >
              <SubnavDemo
                variant="soft"
                ariaLabel="Neutral counts"
                testId="ui-standards-module-subnav-counts-neutral"
                items={withNeutralCounts}
              />
            </SampleCard>
            <SampleCard
              label="WITH SEMANTIC COUNT"
              hint="Incoming orders → warning tone"
              command="MODULE SUBNAV + SOFT + WITH COUNT + SEMANTIC COUNT"
            >
              <SubnavDemo
                variant="soft"
                ariaLabel="Semantic counts"
                testId="ui-standards-module-subnav-counts-semantic"
                items={withSemanticCounts}
              />
            </SampleCard>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="module-subnav.pill-bar"
        title={t("uiStandards.moduleSubnavPillBarTitle")}
        description="Pill Bar layouts, icon/count composition, active treatments, and mobile scroll. Recommended for Purchasing."
        summary="LAYOUT · ICON · COUNT · ACTIVE · MOBILE"
        open={isOpen("module-subnav.pill-bar")}
        onOpenChange={(open) => setOpen("module-subnav.pill-bar", open)}
        testId="ui-standards-module-subnav-pill-bar"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="LAYOUT">
            <SampleCard label="CONTENT WIDTH" command="MODULE SUBNAV + PILL BAR + CONTENT WIDTH">
              <SubnavDemo
                variant="pillBar"
                layout="content"
                ariaLabel="Pill bar content width"
                testId="ui-standards-module-subnav-pillbar-content"
                items={textOnly}
              />
            </SampleCard>
            <SampleCard label="EQUAL WIDTH" command="MODULE SUBNAV + PILL BAR + EQUAL WIDTH">
              <SubnavDemo
                variant="pillBar"
                layout="equal"
                ariaLabel="Pill bar equal width"
                testId="ui-standards-module-subnav-pillbar-equal"
                items={textOnly}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="COMPOSITION">
            <SampleCard label="WITH ICON" command="MODULE SUBNAV + PILL BAR + WITH ICON">
              <SubnavDemo
                variant="pillBar"
                layout="content"
                ariaLabel="Pill bar with icon"
                testId="ui-standards-module-subnav-pillbar-icon"
                items={withIcons}
              />
            </SampleCard>
            <SampleCard
              label="WITH COUNT"
              command="MODULE SUBNAV + PILL BAR + WITH COUNT + NEUTRAL COUNT"
            >
              <SubnavDemo
                variant="pillBar"
                layout="content"
                ariaLabel="Pill bar with count"
                testId="ui-standards-module-subnav-pillbar-count"
                items={withNeutralCounts}
              />
            </SampleCard>
            <SampleCard
              label="WITH ICON + COUNT"
              command="MODULE SUBNAV + PILL BAR + WITH ICON + WITH COUNT"
            >
              <SubnavDemo
                variant="pillBar"
                layout="content"
                ariaLabel="Pill bar icon and count"
                testId="ui-standards-module-subnav-pillbar-icon-count"
                items={iconCount}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="ACTIVE TREATMENT">
            <SampleCard
              label="SOLID PRIMARY ACTIVE"
              hint="Default for Pill Bar"
              command="MODULE SUBNAV + PILL BAR + WITH ICON + WITH COUNT + SOLID PRIMARY ACTIVE"
            >
              <SubnavDemo
                variant="pillBar"
                layout="content"
                activeTreatment="solid"
                ariaLabel="Pill bar solid active"
                testId="ui-standards-module-subnav-pillbar-solid"
                items={iconCountSemantic}
              />
            </SampleCard>
            <SampleCard
              label="SOFT PRIMARY ACTIVE"
              command="MODULE SUBNAV + PILL BAR + WITH ICON + WITH COUNT + SOFT PRIMARY ACTIVE"
            >
              <SubnavDemo
                variant="pillBar"
                layout="content"
                activeTreatment="soft"
                ariaLabel="Pill bar soft active"
                testId="ui-standards-module-subnav-pillbar-soft-active"
                items={iconCountSemantic}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="MOBILE">
            <SampleCard
              label="SCROLLABLE MOBILE"
              hint="Narrow pane · do not wrap rows"
              command="MODULE SUBNAV + PILL BAR + WITH ICON + WITH COUNT + SCROLLABLE MOBILE"
            >
              <div className="max-w-[22rem]">
                <SubnavDemo
                  variant="pillBar"
                  layout="content"
                  scrollable
                  activeTreatment="solid"
                  ariaLabel="Pill bar mobile scrollable"
                  testId="ui-standards-module-subnav-pillbar-mobile"
                  items={iconCount}
                />
              </div>
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="RECOMMENDED">
            <SampleCard
              label="RECOMMENDED FOR PURCHASING"
              hint="PILL BAR + icon + count + solid primary active"
              command="MODULE SUBNAV + PILL BAR + WITH ICON + WITH COUNT + SOLID PRIMARY ACTIVE"
              testId="ui-standards-module-subnav-pillbar-recommended"
            >
              <SubnavDemo
                variant="pillBar"
                layout="content"
                activeTreatment="solid"
                ariaLabel="Recommended purchasing module subnav"
                testId="ui-standards-module-subnav-pillbar-recommended-demo"
                items={iconCountSemantic}
              />
            </SampleCard>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="module-subnav.real-world"
        title={t("uiStandards.moduleSubnavRealWorldTitle")}
        description="Static POS-shaped module navigation — no APIs."
        summary="PURCHASING · ORDERS · INVENTORY · SETTINGS"
        open={isOpen("module-subnav.real-world")}
        onOpenChange={(open) => setOpen("module-subnav.real-world", open)}
        testId="ui-standards-module-subnav-real-world"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="PURCHASING">
            <SampleCard
              label="Pill bar · icon · count · solid"
              testId="ui-standards-module-subnav-rw-purchasing"
              command="MODULE SUBNAV + PILL BAR + WITH ICON + WITH COUNT + SOLID PRIMARY ACTIVE"
            >
              <SubnavDemo
                variant="pillBar"
                layout="content"
                activeTreatment="solid"
                ariaLabel="Purchasing module subnav"
                testId="ui-standards-module-subnav-rw-purchasing-demo"
                items={iconCountSemantic}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="ORDERS">
            <SampleCard
              label="Soft + counts"
              testId="ui-standards-module-subnav-rw-orders"
              command="MODULE SUBNAV + SOFT + WITH COUNT"
            >
              <SubnavDemo
                variant="soft"
                initialPath="/demo/orders/pending"
                ariaLabel="Orders module subnav"
                testId="ui-standards-module-subnav-rw-orders-demo"
                items={ordersItems}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="INVENTORY">
            <SampleCard
              label="Underline"
              testId="ui-standards-module-subnav-rw-inventory"
              command="MODULE SUBNAV + UNDERLINE + WITH COUNT + SEMANTIC COUNT"
            >
              <SubnavDemo
                variant="underline"
                initialPath="/demo/inventory/low"
                ariaLabel="Inventory module subnav"
                testId="ui-standards-module-subnav-rw-inventory-demo"
                items={inventoryItems}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="SETTINGS">
            <SampleCard
              label="Vertical"
              testId="ui-standards-module-subnav-rw-settings"
              command="MODULE SUBNAV + VERTICAL + WITH ICON + WITH COUNT"
            >
              <SubnavDemo
                variant="vertical"
                initialPath="/demo/settings/general"
                ariaLabel="Settings module subnav"
                testId="ui-standards-module-subnav-rw-settings-demo"
                items={settingsItems}
              />
            </SampleCard>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="module-subnav.cheatsheet"
        title={t("uiStandards.moduleSubnavCheatTitle")}
        description={t("uiStandards.moduleSubnavCheatLede")}
        summary="PILOT / CANDIDATE"
        open={isOpen("module-subnav.cheatsheet")}
        onOpenChange={(open) => setOpen("module-subnav.cheatsheet", open)}
        testId="ui-standards-module-subnav-cheatsheet"
      >
        <div
          className="rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)]/30 p-3 font-mono text-[length:var(--exits-text-xs)] leading-relaxed text-foreground"
          data-testid="ui-standards-module-subnav-cheatsheet-body"
        >
          <p className="m-0 mb-2 font-sans text-[length:var(--exits-text-sm)] font-semibold tracking-wide text-muted">
            {t("uiStandards.moduleSubnavPilotBadge")}
          </p>
          <pre className="m-0 whitespace-pre-wrap">{`VARIANTS
  MODULE SUBNAV + UNDERLINE · SOFT · PILL · PILL BAR · COMPACT · VERTICAL

OPTIONS
  WITH ICON · NO ICON · WITH COUNT · NO COUNT
  EQUAL WIDTH · CONTENT WIDTH
  SOLID PRIMARY ACTIVE · SOFT PRIMARY ACTIVE
  SCROLLABLE MOBILE

COUNT
  NEUTRAL COUNT · SEMANTIC COUNT (warning/danger when meaning requires)

RECOMMENDED
  Purchasing → MODULE SUBNAV + PILL BAR + WITH ICON + WITH COUNT + SOLID PRIMARY ACTIVE
  Orders     → MODULE SUBNAV + SOFT + WITH COUNT
  Inventory  → MODULE SUBNAV + UNDERLINE
  Settings   → MODULE SUBNAV + VERTICAL

BOUNDARY
  Module Subnav = related route navigation (aria-current="page")
  Tabs = in-page tablist / tab / tabpanel
  Do not use Tabs semantics for module destinations

STATUS
  PILOT / CANDIDATE — Docs/UI/exits-module-subnav-standard.md`}</pre>
        </div>
      </UiStandardsSection>
    </div>
  );
}
