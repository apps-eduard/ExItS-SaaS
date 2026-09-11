import { useState, type ReactNode } from "react";
import {
  Boxes,
  ChartColumn,
  ClipboardList,
  History,
  LayoutDashboard,
  LayoutGrid,
  LayoutList,
  Package,
  Settings,
  ShoppingBag,
  TriangleAlert,
  Users,
} from "lucide-react";
import { ExitsTabs, type ExitsTabItem } from "@/components/exits/ExitsTabs";
import { useI18n } from "@/i18n/I18nProvider";
import { UiStandardsSection } from "@/features/ui-standards/UiStandardsSection";

function SampleCard({
  label,
  children,
  testId,
  hint,
}: {
  label: string;
  children: ReactNode;
  testId?: string;
  hint?: string;
}) {
  return (
    <div
      className="flex flex-col gap-1.5 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)]/40 p-2"
      data-testid={testId}
    >
      <span className="text-[length:var(--exits-text-xs)] uppercase tracking-wide text-muted">{label}</span>
      <div className="min-w-0">{children}</div>
      {hint ? (
        <span className="text-[length:var(--exits-text-xs)] text-muted">{hint}</span>
      ) : null}
    </div>
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
      data-testid={`ui-standards-tabs-group-${slug}`}
    >
      <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-muted">{title}</h3>
      <div className="grid gap-2">{children}</div>
    </section>
  );
}

type DisclosureProps = {
  isOpen: (id: string) => boolean;
  setOpen: (id: string, open: boolean) => void;
};

function DemoPanel({ title }: { title: string }) {
  return (
    <p className="m-0 text-muted">
      Selected: <span className="font-medium text-foreground">{title}</span> — local demo content only.
    </p>
  );
}

export function UiStandardsTabsPanel({ isOpen, setOpen }: DisclosureProps) {
  const { t } = useI18n();
  const [underline, setUnderline] = useState("overview");
  const [soft, setSoft] = useState("overview");
  const [pill, setPill] = useState("overview");
  const [pillCounts, setPillCounts] = useState("all");
  const [pillActiveCounts, setPillActiveCounts] = useState("all");
  const [pillSemantic, setPillSemantic] = useState("low");
  const [pillIcons, setPillIcons] = useState("products");
  const [pillIconCount, setPillIconCount] = useState("products");
  const [pillOrders, setPillOrders] = useState("all");
  const [pillCatalog, setPillCatalog] = useState("all");
  const [pillInventory, setPillInventory] = useState("all");
  const [iconOptUnderline, setIconOptUnderline] = useState("products");
  const [iconOptSoft, setIconOptSoft] = useState("products");
  const [iconOptPill, setIconOptPill] = useState("products");
  const [segmented, setSegmented] = useState("list");
  const [enclosed, setEnclosed] = useState("overview");
  const [vertical, setVertical] = useState("general");
  const [neutralCounts, setNeutralCounts] = useState("orders");
  const [activeCounts, setActiveCounts] = useState("orders");
  const [semanticCounts, setSemanticCounts] = useState("low");
  const [icons, setIcons] = useState("products");
  const [iconCount, setIconCount] = useState("products");
  const [iconOnly, setIconOnly] = useState("list");
  const [states, setStates] = useState("overview");
  const [loading, setLoading] = useState("orders");
  const [longLabels, setLongLabels] = useState("connected");
  const [mobile, setMobile] = useState("overview");
  const [products, setProducts] = useState("overview");
  const [orders, setOrders] = useState("all");
  const [inventory, setInventory] = useState("onhand");
  const [customer, setCustomer] = useState("overview");
  const [settings, setSettings] = useState("general");

  const moduleNav: ExitsTabItem[] = [
    { key: "overview", label: "Overview" },
    { key: "products", label: "Products", count: 24, countTone: "neutral" },
    { key: "inventory", label: "Inventory" },
    { key: "orders", label: "Orders", count: 6, countTone: "neutral" },
  ];

  return (
    <div className="grid gap-3" data-testid="ui-standards-tabs-section">
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("uiStandards.tabsPilotLede")}</p>

      <UiStandardsSection
        id="tabs.variants"
        title={t("uiStandards.tabsVariantsTitle")}
        description="Six visual candidates. Family ≠ one style — pick by purpose. PILOT / NOT LOCKED."
        summary="UNDERLINE · SOFT · PILL · SEGMENTED · ENCLOSED · VERTICAL"
        open={isOpen("tabs.variants")}
        onOpenChange={(open) => setOpen("tabs.variants", open)}
        testId="ui-standards-tabs-variants"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="DEFAULT CANDIDATES — NOT LOCKED">
            <SampleCard label="CANDIDATE" hint="Display only — do not enforce globally yet">
              <div className="grid gap-1 text-[length:var(--exits-text-xs)] text-muted">
                <div>PAGE / MODULE NAVIGATION → UNDERLINE</div>
                <div>NORMAL CONTENT TABS → SOFT</div>
                <div>COMPACT CATEGORY / STATUS TABS → PILL</div>
                <div>VIEW SWITCHER → SEGMENTED</div>
                <div>DETAIL PANEL → ENCLOSED</div>
                <div>SETTINGS / ADMIN → VERTICAL</div>
                <div>COUNTS → neutral by default · semantic only when meaning requires it</div>
              </div>
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="UNDERLINE">
            <SampleCard label="Page-level navigation" hint="Primary bottom indicator · ~2px">
              <ExitsTabs
                variant="underline"
                ariaLabel="Underline demo"
                testId="ui-standards-tabs-demo-underline"
                value={underline}
                onValueChange={setUnderline}
                items={moduleNav}
                panels={{
                  overview: <DemoPanel title="Overview" />,
                  products: <DemoPanel title="Products" />,
                  inventory: <DemoPanel title="Inventory" />,
                  orders: <DemoPanel title="Orders" />,
                }}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="SOFT">
            <SampleCard label="Content tabs" hint="Selected soft primary surface — not a Primary Button">
              <ExitsTabs
                variant="soft"
                ariaLabel="Soft demo"
                testId="ui-standards-tabs-demo-soft"
                value={soft}
                onValueChange={setSoft}
                items={[
                  { key: "overview", label: "Overview" },
                  { key: "products", label: "Products" },
                  { key: "orders", label: "Orders" },
                ]}
                panels={{
                  overview: <DemoPanel title="Overview" />,
                  products: <DemoPanel title="Products" />,
                  orders: <DemoPanel title="Orders" />,
                }}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="PILL">
            <SampleCard
              label="Independent pills · gaps · not segmented"
              hint="Selected uses Primary tokens — not a giant Primary Button"
            >
              <ExitsTabs
                variant="pill"
                ariaLabel="Pill demo"
                testId="ui-standards-tabs-demo-pill"
                value={pill}
                onValueChange={setPill}
                items={[
                  { key: "overview", label: "Overview" },
                  { key: "products", label: "Products" },
                  { key: "orders", label: "Orders" },
                ]}
                panels={{
                  overview: <DemoPanel title="Overview" />,
                  products: <DemoPanel title="Products" />,
                  orders: <DemoPanel title="Orders" />,
                }}
              />
            </SampleCard>
            <SampleCard label="Pill + counts (neutral)">
              <ExitsTabs
                variant="pill"
                ariaLabel="Pill counts neutral"
                value={pillCounts}
                onValueChange={setPillCounts}
                items={[
                  { key: "all", label: "All", count: 24, countTone: "neutral" },
                  { key: "pending", label: "Pending", count: 6, countTone: "neutral" },
                  { key: "completed", label: "Completed", count: 16, countTone: "neutral" },
                  { key: "cancelled", label: "Cancelled", count: 2, countTone: "neutral" },
                ]}
              />
            </SampleCard>
            <SampleCard label="Pill + active-aware Primary count">
              <ExitsTabs
                variant="pill"
                ariaLabel="Pill counts active-aware"
                value={pillActiveCounts}
                onValueChange={setPillActiveCounts}
                items={[
                  {
                    key: "all",
                    label: "All",
                    count: 24,
                    countTone: pillActiveCounts === "all" ? "primary" : "neutral",
                  },
                  {
                    key: "pending",
                    label: "Pending",
                    count: 6,
                    countTone: pillActiveCounts === "pending" ? "primary" : "neutral",
                  },
                  {
                    key: "completed",
                    label: "Completed",
                    count: 16,
                    countTone: pillActiveCounts === "completed" ? "primary" : "neutral",
                  },
                  {
                    key: "cancelled",
                    label: "Cancelled",
                    count: 2,
                    countTone: pillActiveCounts === "cancelled" ? "primary" : "neutral",
                  },
                ]}
              />
            </SampleCard>
            <SampleCard label="Pill + semantic warning/danger count">
              <ExitsTabs
                variant="pill"
                ariaLabel="Pill semantic counts"
                value={pillSemantic}
                onValueChange={setPillSemantic}
                items={[
                  { key: "all", label: "All", count: 40, countTone: "neutral" },
                  { key: "low", label: "Low stock", count: 12, countTone: "warning" },
                  { key: "overdue", label: "Overdue", count: 5, countTone: "danger" },
                ]}
              />
            </SampleCard>
            <SampleCard label="Pill + icons">
              <ExitsTabs
                variant="pill"
                ariaLabel="Pill with icons"
                value={pillIcons}
                onValueChange={setPillIcons}
                items={[
                  { key: "products", label: "Products", icon: Package },
                  { key: "inventory", label: "Inventory", icon: Boxes },
                  { key: "orders", label: "Orders", icon: ClipboardList },
                  { key: "customers", label: "Customers", icon: Users },
                ]}
              />
            </SampleCard>
            <SampleCard label="Pill + icon + count">
              <ExitsTabs
                variant="pill"
                ariaLabel="Pill icon count"
                testId="ui-standards-tabs-demo-pill-icon-count"
                value={pillIconCount}
                onValueChange={setPillIconCount}
                items={[
                  {
                    key: "products",
                    label: "Products",
                    icon: Package,
                    count: 24,
                    countTone: "neutral",
                  },
                  {
                    key: "orders",
                    label: "Orders",
                    icon: ClipboardList,
                    count: 6,
                    countTone: "neutral",
                  },
                  {
                    key: "low",
                    label: "Low stock",
                    icon: TriangleAlert,
                    count: 12,
                    countTone: "warning",
                  },
                  {
                    key: "customers",
                    label: "Customers",
                    icon: Users,
                    count: 38,
                    countTone: "neutral",
                  },
                ]}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="SEGMENTED">
            <SampleCard label="View switcher" hint="Connected segments · compact · no independent pill gaps">
              <ExitsTabs
                variant="segmented"
                ariaLabel="Segmented demo"
                testId="ui-standards-tabs-demo-segmented"
                value={segmented}
                onValueChange={setSegmented}
                items={[
                  { key: "list", label: "List", icon: LayoutList },
                  { key: "grid", label: "Grid", icon: LayoutGrid },
                  { key: "chart", label: "Chart", icon: ChartColumn },
                ]}
                panels={{
                  list: <DemoPanel title="List" />,
                  grid: <DemoPanel title="Grid" />,
                  chart: <DemoPanel title="Chart" />,
                }}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="ENCLOSED">
            <SampleCard label="Detail panel" hint="Selected tab connects to content border">
              <ExitsTabs
                variant="enclosed"
                ariaLabel="Enclosed demo"
                testId="ui-standards-tabs-demo-enclosed"
                value={enclosed}
                onValueChange={setEnclosed}
                items={[
                  { key: "overview", label: "Overview" },
                  { key: "orders", label: "Orders", count: 12, countTone: "neutral" },
                  { key: "activity", label: "Activity" },
                ]}
                panels={{
                  overview: <DemoPanel title="Overview" />,
                  orders: <DemoPanel title="Orders" />,
                  activity: <DemoPanel title="Activity" />,
                }}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="VERTICAL">
            <SampleCard label="Settings / admin" hint="Side indicator + soft selected surface">
              <ExitsTabs
                variant="vertical"
                ariaLabel="Vertical demo"
                testId="ui-standards-tabs-demo-vertical"
                value={vertical}
                onValueChange={setVertical}
                items={[
                  { key: "general", label: "General" },
                  { key: "branches", label: "Branches", count: 3, countTone: "neutral" },
                  { key: "permissions", label: "Permissions" },
                  { key: "notifications", label: "Notifications", count: 8, countTone: "neutral" },
                  { key: "advanced", label: "Advanced" },
                ]}
                panels={{
                  general: <DemoPanel title="General" />,
                  branches: <DemoPanel title="Branches" />,
                  permissions: <DemoPanel title="Permissions" />,
                  notifications: <DemoPanel title="Notifications" />,
                  advanced: <DemoPanel title="Advanced" />,
                }}
              />
            </SampleCard>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="tabs.counts"
        title={t("uiStandards.tabsCountsTitle")}
        description="CountBadge reused. Label then count. Three count-tone approaches — CANDIDATES / NOT LOCKED."
        summary="COUNTS · candidates"
        open={isOpen("tabs.counts")}
        onOpenChange={(open) => setOpen("tabs.counts", open)}
        testId="ui-standards-tabs-counts"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="A — NEUTRAL COUNT ALWAYS">
            <SampleCard label="Quiet counts">
              <ExitsTabs
                variant="soft"
                ariaLabel="Neutral counts"
                value={neutralCounts}
                onValueChange={setNeutralCounts}
                items={[
                  { key: "orders", label: "Orders", count: 12, countTone: "neutral" },
                  { key: "pending", label: "Pending", count: 3, countTone: "neutral" },
                  { key: "done", label: "Completed", count: 16, countTone: "neutral" },
                ]}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="B — ACTIVE-AWARE COUNT">
            <SampleCard label="Selected → Primary badge">
              <ExitsTabs
                variant="soft"
                ariaLabel="Active-aware counts"
                value={activeCounts}
                onValueChange={setActiveCounts}
                items={[
                  {
                    key: "orders",
                    label: "Orders",
                    count: 12,
                    countTone: activeCounts === "orders" ? "primary" : "neutral",
                  },
                  {
                    key: "pending",
                    label: "Pending",
                    count: 3,
                    countTone: activeCounts === "pending" ? "primary" : "neutral",
                  },
                  {
                    key: "done",
                    label: "Completed",
                    count: 16,
                    countTone: activeCounts === "done" ? "primary" : "neutral",
                  },
                ]}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="C — SEMANTIC COUNT WHEN MEANING MATTERS">
            <SampleCard label="Warning / danger only for state">
              <ExitsTabs
                variant="soft"
                ariaLabel="Semantic counts"
                value={semanticCounts}
                onValueChange={setSemanticCounts}
                items={[
                  { key: "orders", label: "Orders", count: 24, countTone: "neutral" },
                  { key: "low", label: "Low stock", count: 12, countTone: "warning" },
                  { key: "overdue", label: "Overdue", count: 5, countTone: "danger" },
                  { key: "done", label: "Completed", count: 16, countTone: "neutral" },
                ]}
              />
            </SampleCard>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="tabs.icon-options"
        title={t("uiStandards.tabsIconOptionsTitle")}
        description="Icon and count are independent of variant. Structure: [ICON] Label [COUNT]. Compare UNDERLINE · SOFT · PILL."
        summary="TEXT · ICON · COUNT"
        open={isOpen("tabs.icon-options")}
        onOpenChange={(open) => setOpen("tabs.icon-options", open)}
        testId="ui-standards-tabs-icon-options"
      >
        <div className="grid gap-3">
          {(
            [
              ["UNDERLINE", "underline", iconOptUnderline, setIconOptUnderline],
              ["SOFT", "soft", iconOptSoft, setIconOptSoft],
              ["PILL", "pill", iconOptPill, setIconOptPill],
            ] as const
          ).map(([label, variant, value, setValue]) => (
            <StaticSampleGroup key={variant} title={label}>
              <SampleCard label="TEXT ONLY">
                <ExitsTabs
                  variant={variant}
                  ariaLabel={`${label} text only`}
                  value={value}
                  onValueChange={setValue}
                  items={[
                    { key: "products", label: "Products" },
                    { key: "orders", label: "Orders" },
                    { key: "customers", label: "Customers" },
                  ]}
                />
              </SampleCard>
              <SampleCard label="ICON + TEXT">
                <ExitsTabs
                  variant={variant}
                  ariaLabel={`${label} icon text`}
                  value={value}
                  onValueChange={setValue}
                  items={[
                    { key: "products", label: "Products", icon: Package },
                    { key: "orders", label: "Orders", icon: ClipboardList },
                    { key: "customers", label: "Customers", icon: Users },
                  ]}
                />
              </SampleCard>
              <SampleCard label="TEXT + COUNT">
                <ExitsTabs
                  variant={variant}
                  ariaLabel={`${label} text count`}
                  value={value}
                  onValueChange={setValue}
                  items={[
                    { key: "products", label: "Products", count: 24, countTone: "neutral" },
                    { key: "orders", label: "Orders", count: 6, countTone: "neutral" },
                    { key: "customers", label: "Customers", count: 38, countTone: "neutral" },
                  ]}
                />
              </SampleCard>
              <SampleCard label="ICON + TEXT + COUNT">
                <ExitsTabs
                  variant={variant}
                  ariaLabel={`${label} icon text count`}
                  value={value}
                  onValueChange={setValue}
                  items={[
                    {
                      key: "products",
                      label: "Products",
                      icon: Package,
                      count: 24,
                      countTone: "neutral",
                    },
                    {
                      key: "orders",
                      label: "Orders",
                      icon: ClipboardList,
                      count: 6,
                      countTone: "neutral",
                    },
                    {
                      key: "customers",
                      label: "Customers",
                      icon: Users,
                      count: 38,
                      countTone: "neutral",
                    },
                  ]}
                />
              </SampleCard>
            </StaticSampleGroup>
          ))}
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="tabs.icons"
        title={t("uiStandards.tabsIconsTitle")}
        description="Icons optional. Icon-only is SPECIAL USE — not default business navigation."
        summary="ICON · SPECIAL"
        open={isOpen("tabs.icons")}
        onOpenChange={(open) => setOpen("tabs.icons", open)}
        testId="ui-standards-tabs-icons"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="WITH ICON">
            <SampleCard label="Icon + text">
              <ExitsTabs
                variant="underline"
                ariaLabel="Icon tabs"
                value={icons}
                onValueChange={setIcons}
                items={[
                  { key: "overview", label: "Overview", icon: LayoutDashboard },
                  { key: "products", label: "Products", icon: Package },
                  { key: "inventory", label: "Inventory", icon: Boxes },
                  { key: "orders", label: "Orders", icon: ShoppingBag },
                  { key: "customers", label: "Customers", icon: Users },
                  { key: "settings", label: "Settings", icon: Settings },
                  { key: "activity", label: "Activity", icon: History },
                ]}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="ICON + TEXT + COUNT">
            <SampleCard label="Common POS pattern">
              <ExitsTabs
                variant="soft"
                ariaLabel="Icon count tabs"
                value={iconCount}
                onValueChange={setIconCount}
                items={[
                  {
                    key: "products",
                    label: "Products",
                    icon: Package,
                    count: 24,
                    countTone: "neutral",
                  },
                  {
                    key: "orders",
                    label: "Orders",
                    icon: ClipboardList,
                    count: 6,
                    countTone: "neutral",
                  },
                  {
                    key: "low",
                    label: "Low stock",
                    icon: TriangleAlert,
                    count: 12,
                    countTone: "warning",
                  },
                ]}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="ICON ONLY — SPECIAL USE">
            <SampleCard label="Tool views" hint="aria-label + title required">
              <ExitsTabs
                variant="segmented"
                iconOnly
                ariaLabel="Icon-only view switcher"
                value={iconOnly}
                onValueChange={setIconOnly}
                items={[
                  { key: "list", label: "List", icon: LayoutList, title: "List" },
                  { key: "grid", label: "Grid", icon: LayoutGrid, title: "Grid" },
                  { key: "chart", label: "Chart", icon: ChartColumn, title: "Chart" },
                ]}
              />
            </SampleCard>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="tabs.states"
        title={t("uiStandards.tabsStatesTitle")}
        description="Disabled, loading count, long labels. Local tabs vs route tabs are page-owned."
        summary="STATES · demo"
        open={isOpen("tabs.states")}
        onOpenChange={(open) => setOpen("tabs.states", open)}
        testId="ui-standards-tabs-states"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="DISABLED">
            <SampleCard label="Reports disabled">
              <ExitsTabs
                variant="underline"
                ariaLabel="Disabled demo"
                value={states}
                onValueChange={setStates}
                items={[
                  { key: "overview", label: "Overview" },
                  { key: "products", label: "Products" },
                  { key: "reports", label: "Reports", disabled: true },
                ]}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="LOADING COUNT">
            <SampleCard label="Quiet placeholder">
              <ExitsTabs
                variant="soft"
                ariaLabel="Loading count demo"
                value={loading}
                onValueChange={setLoading}
                items={[
                  { key: "orders", label: "Orders", loadingCount: true },
                  { key: "pending", label: "Pending", count: 3, countTone: "neutral" },
                ]}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="LONG LABELS">
            <SampleCard label="Truncation + title">
              <ExitsTabs
                variant="underline"
                ariaLabel="Long labels"
                value={longLabels}
                onValueChange={setLongLabels}
                items={[
                  {
                    key: "connected",
                    label: "Connected suppliers",
                    title: "Connected suppliers",
                  },
                  {
                    key: "history",
                    label: "Purchase order history",
                    title: "Purchase order history",
                  },
                  { key: "attention", label: "Needs attention", title: "Needs attention" },
                ]}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="LOCAL VS ROUTE">
            <SampleCard
              label="Boundary"
              hint="Visual standard is shared; routing stays page-owned"
            >
              <div className="grid gap-1 text-[length:var(--exits-text-xs)] text-muted">
                <div>
                  <strong className="text-foreground">LOCAL TABS</strong> — Details / History / Notes
                  (same view content)
                </div>
                <div>
                  <strong className="text-foreground">ROUTE TABS</strong> — Products / Inventory /
                  Orders (real navigation)
                </div>
              </div>
            </SampleCard>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="tabs.mobile"
        title={t("uiStandards.tabsMobileTitle")}
        description="Many page tabs: horizontal scroll on narrow viewports — do not wrap into three rows."
        summary="SCROLLABLE"
        open={isOpen("tabs.mobile")}
        onOpenChange={(open) => setOpen("tabs.mobile", open)}
        testId="ui-standards-tabs-mobile"
      >
        <SampleCard label="Module navigation · scrollable" hint="Resize / narrow the pane to scroll">
          <div className="max-w-full sm:max-w-md">
            <ExitsTabs
              variant="underline"
              scrollable
              ariaLabel="Mobile overflow demo"
              testId="ui-standards-tabs-demo-mobile"
              value={mobile}
              onValueChange={setMobile}
              items={[
                { key: "overview", label: "Overview" },
                { key: "products", label: "Products" },
                { key: "inventory", label: "Inventory" },
                { key: "purchasing", label: "Purchasing" },
                { key: "orders", label: "Orders" },
                { key: "customers", label: "Customers" },
                { key: "suppliers", label: "Suppliers" },
                { key: "reports", label: "Reports" },
              ]}
            />
          </div>
        </SampleCard>
      </UiStandardsSection>

      <UiStandardsSection
        id="tabs.real-world"
        title={t("uiStandards.tabsRealWorldTitle")}
        description="Static POS-shaped examples — no APIs."
        summary="CONTEXT · examples"
        open={isOpen("tabs.real-world")}
        onOpenChange={(open) => setOpen("tabs.real-world", open)}
        testId="ui-standards-tabs-real-world"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="ORDER STATUS — PILL + COUNT">
            <SampleCard label="Without icons">
              <ExitsTabs
                variant="pill"
                ariaLabel="Order status pill"
                value={pillOrders}
                onValueChange={setPillOrders}
                items={[
                  { key: "all", label: "All", count: 24, countTone: "neutral" },
                  { key: "pending", label: "Pending", count: 6, countTone: "neutral" },
                  { key: "completed", label: "Completed", count: 16, countTone: "neutral" },
                  { key: "cancelled", label: "Cancelled", count: 2, countTone: "neutral" },
                ]}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="CATALOG — PILL">
            <SampleCard label="Without icons">
              <ExitsTabs
                variant="pill"
                ariaLabel="Catalog pill"
                value={pillCatalog}
                onValueChange={setPillCatalog}
                items={[
                  { key: "all", label: "All" },
                  { key: "products", label: "Products" },
                  { key: "services", label: "Services" },
                  { key: "bundles", label: "Bundles" },
                ]}
              />
            </SampleCard>
            <SampleCard label="With icons">
              <ExitsTabs
                variant="pill"
                ariaLabel="Catalog pill icons"
                value={pillCatalog}
                onValueChange={setPillCatalog}
                items={[
                  { key: "all", label: "All", icon: LayoutGrid },
                  { key: "products", label: "Products", icon: Package },
                  { key: "services", label: "Services", icon: Settings },
                  { key: "bundles", label: "Bundles", icon: Boxes },
                ]}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="INVENTORY — PILL + SEMANTIC COUNTS">
            <SampleCard label="Without icons">
              <ExitsTabs
                variant="pill"
                ariaLabel="Inventory pill"
                value={pillInventory}
                onValueChange={setPillInventory}
                items={[
                  { key: "all", label: "All" },
                  { key: "low", label: "Low stock", count: 12, countTone: "warning" },
                  { key: "expiring", label: "Expiring", count: 4, countTone: "warning" },
                  { key: "out", label: "Out of stock", count: 3, countTone: "danger" },
                ]}
              />
            </SampleCard>
            <SampleCard label="With icons">
              <ExitsTabs
                variant="pill"
                ariaLabel="Inventory pill icons"
                value={pillInventory}
                onValueChange={setPillInventory}
                items={[
                  { key: "all", label: "All", icon: Boxes },
                  {
                    key: "low",
                    label: "Low stock",
                    icon: TriangleAlert,
                    count: 12,
                    countTone: "warning",
                  },
                  {
                    key: "expiring",
                    label: "Expiring",
                    icon: History,
                    count: 4,
                    countTone: "warning",
                  },
                  {
                    key: "out",
                    label: "Out of stock",
                    icon: Package,
                    count: 3,
                    countTone: "danger",
                  },
                ]}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="PRODUCTS">
            <SampleCard label="Underline + counts">
              <ExitsTabs
                variant="underline"
                ariaLabel="Products tabs"
                value={products}
                onValueChange={setProducts}
                items={[
                  { key: "overview", label: "Overview" },
                  { key: "inventory", label: "Inventory", count: 24, countTone: "neutral" },
                  { key: "pricing", label: "Pricing" },
                  { key: "history", label: "History" },
                ]}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="ORDERS">
            <SampleCard label="Soft + counts">
              <ExitsTabs
                variant="soft"
                ariaLabel="Orders tabs"
                value={orders}
                onValueChange={setOrders}
                items={[
                  { key: "all", label: "All", count: 24, countTone: "neutral" },
                  { key: "pending", label: "Pending", count: 6, countTone: "neutral" },
                  { key: "completed", label: "Completed", count: 16, countTone: "neutral" },
                  { key: "cancelled", label: "Cancelled", count: 2, countTone: "neutral" },
                ]}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="INVENTORY">
            <SampleCard label="Semantic low-stock / expiring">
              <ExitsTabs
                variant="soft"
                ariaLabel="Inventory tabs"
                value={inventory}
                onValueChange={setInventory}
                items={[
                  { key: "onhand", label: "On hand" },
                  { key: "low", label: "Low stock", count: 12, countTone: "warning" },
                  { key: "expiring", label: "Expiring", count: 4, countTone: "warning" },
                  { key: "movements", label: "Movements" },
                ]}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="CUSTOMER DETAIL">
            <SampleCard label="Enclosed detail">
              <ExitsTabs
                variant="enclosed"
                ariaLabel="Customer detail tabs"
                value={customer}
                onValueChange={setCustomer}
                items={[
                  { key: "overview", label: "Overview" },
                  { key: "purchases", label: "Purchases" },
                  { key: "credit", label: "Credit" },
                  { key: "activity", label: "Activity" },
                ]}
                panels={{
                  overview: <DemoPanel title="Overview" />,
                  purchases: <DemoPanel title="Purchases" />,
                  credit: <DemoPanel title="Credit" />,
                  activity: <DemoPanel title="Activity" />,
                }}
              />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="SETTINGS — VERTICAL">
            <SampleCard label="Admin sections">
              <ExitsTabs
                variant="vertical"
                ariaLabel="Settings vertical"
                value={settings}
                onValueChange={setSettings}
                items={[
                  { key: "general", label: "General" },
                  { key: "branches", label: "Branches", count: 3, countTone: "neutral" },
                  { key: "users", label: "Users", count: 10, countTone: "neutral" },
                  { key: "permissions", label: "Permissions" },
                  { key: "notifications", label: "Notifications", count: 8, countTone: "neutral" },
                ]}
                panels={{
                  general: <DemoPanel title="General" />,
                  branches: <DemoPanel title="Branches" />,
                  users: <DemoPanel title="Users" />,
                  permissions: <DemoPanel title="Permissions" />,
                  notifications: <DemoPanel title="Notifications" />,
                }}
              />
            </SampleCard>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="tabs.cheatsheet"
        title={t("uiStandards.tabsCheatTitle")}
        description={t("uiStandards.tabsCheatLede")}
        summary="PILOT · NOT LOCKED"
        open={isOpen("tabs.cheatsheet")}
        onOpenChange={(open) => setOpen("tabs.cheatsheet", open)}
        testId="ui-standards-tabs-cheatsheet"
      >
        <div
          className="rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)]/30 p-3 font-mono text-[length:var(--exits-text-xs)] leading-relaxed text-foreground"
          data-testid="ui-standards-tabs-cheatsheet-body"
        >
          <p className="m-0 mb-2 font-sans text-[length:var(--exits-text-sm)] font-semibold tracking-wide text-muted">
            {t("uiStandards.tabsPilotBadge")}
          </p>
          <pre className="m-0 whitespace-pre-wrap">{`VARIANTS
  UNDERLINE TABS · SOFT TABS · PILL TABS · SEGMENTED TABS · ENCLOSED TABS · VERTICAL TABS

OPTIONS
  WITH ICON · NO ICON · WITH COUNT · NO COUNT · SCROLLABLE · DISABLED

COUNT
  NEUTRAL COUNT · PRIMARY COUNT · SEMANTIC COUNT

EXAMPLES
  Products / Inventory / Orders → UNDERLINE TABS + WITH ICON + WITH COUNT
  Order status                  → PILL TABS + WITH COUNT
  Catalog categories            → PILL TABS
  List / Grid                   → SEGMENTED TABS + WITH ICON
  Settings                      → VERTICAL TABS + WITH ICON
  Low stock                     → WITH COUNT WARNING
  Mobile module navigation      → UNDERLINE TABS + SCROLLABLE

CANDIDATE DEFAULTS (NOT LOCKED)
  PAGE / MODULE → UNDERLINE
  CONTENT → SOFT
  COMPACT CATEGORY / STATUS → PILL
  VIEW SWITCHER → SEGMENTED
  DETAIL PANEL → ENCLOSED
  SETTINGS → VERTICAL

ICON STRUCTURE
  [ICON] Label [COUNT]

BOUNDARY
  Tabs own visuals / a11y presentation
  Pages own routes / APIs / permissions / counts source

STATUS
  PILOT / NOT LOCKED — inspect /ui-standards before locking`}</pre>
        </div>
      </UiStandardsSection>
    </div>
  );
}
