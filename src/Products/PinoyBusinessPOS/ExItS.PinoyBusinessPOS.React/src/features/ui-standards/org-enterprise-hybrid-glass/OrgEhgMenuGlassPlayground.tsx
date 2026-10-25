import { useState } from "react";
import {
  ArrowRightLeft,
  Bell,
  Building2,
  ChevronRight,
  CreditCard,
  HelpCircle,
  LayoutDashboard,
  LogOut,
  Moon,
  Package,
  RotateCcw,
  Settings,
  ShoppingCart,
  Sun,
  UserRound,
  Users,
  type LucideIcon,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { CardTitle } from "@/components/ui/card";
import { Switch } from "@/components/ui/switch";
import { UiStandardsCopyCommand } from "@/features/ui-standards/UiStandardsCopyCommand";
import {
  CompactSelect,
  PlaygroundTextField,
} from "@/features/ui-standards/UiStandardsPlaygrounds";
import { PlaygroundLabel, PlaygroundSection } from "@/features/ui-standards/UiStandardsSnippetBlock";
import {
  ORG_EHG_PLAYGROUND_BG_DEFAULT,
  ORG_EHG_PLAYGROUND_BG_DEFAULT_INTENSITY,
  ORG_EHG_PLAYGROUND_BG_OPTIONS,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/orgEhgPlaygroundBackground";
import { useUiStandardsShellAppearance } from "@/features/ui-standards/UiStandardsShellAppearance";

export type OrgEhgMenuGlassIntensity = "solid" | "soft" | "medium" | "strong" | "ultra";
export type OrgEhgMenuGlassPreset = "workspace" | "account" | "quick-actions";
export type OrgEhgMenuGlassDensity = "compact" | "comfortable";

export type OrgEhgMenuGlassItem = {
  id: string;
  label: string;
  Icon: LucideIcon;
  hint?: string;
  badge?: string;
  danger?: boolean;
  dividerBefore?: boolean;
};

const INTENSITIES: ReadonlyArray<{ label: string; value: OrgEhgMenuGlassIntensity }> = [
  { label: "Solid", value: "solid" },
  { label: "Soft Glass", value: "soft" },
  { label: "Medium Glass", value: "medium" },
  { label: "Strong Glass", value: "strong" },
  { label: "Ultra Glass", value: "ultra" },
];

const PRESETS: ReadonlyArray<{ label: string; value: OrgEhgMenuGlassPreset }> = [
  { label: "Workspace nav", value: "workspace" },
  { label: "Account menu", value: "account" },
  { label: "Quick actions", value: "quick-actions" },
];

const DENSITIES: ReadonlyArray<{ label: string; value: OrgEhgMenuGlassDensity }> = [
  { label: "Compact", value: "compact" },
  { label: "Comfortable", value: "comfortable" },
];

const WORKSPACE_ITEMS: readonly OrgEhgMenuGlassItem[] = [
  { id: "dashboard", label: "Dashboard", Icon: LayoutDashboard, hint: "Overview" },
  { id: "inventory", label: "Inventory", Icon: Package, badge: "14", hint: "Stock & SKUs" },
  { id: "orders", label: "Orders", Icon: ShoppingCart, badge: "5", hint: "Customer orders" },
  { id: "transfers", label: "Transfers", Icon: ArrowRightLeft, hint: "Between branches" },
  { id: "people", label: "People", Icon: Users, hint: "Staff & roles", dividerBefore: true },
  { id: "settings", label: "Branch settings", Icon: Settings, hint: "Hours & fulfillment" },
];

const ACCOUNT_ITEMS: readonly OrgEhgMenuGlassItem[] = [
  { id: "profile", label: "My profile", Icon: UserRound, hint: "Name & photo" },
  { id: "billing", label: "Billing", Icon: CreditCard, hint: "Plan & invoices" },
  { id: "alerts", label: "Notifications", Icon: Bell, badge: "3", hint: "Unread alerts" },
  { id: "help", label: "Help center", Icon: HelpCircle, hint: "Guides & support", dividerBefore: true },
  { id: "sign-out", label: "Sign out", Icon: LogOut, danger: true, dividerBefore: true },
];

const QUICK_ACTION_ITEMS: readonly OrgEhgMenuGlassItem[] = [
  { id: "new-order", label: "New customer order", Icon: ShoppingCart, hint: "Sell floor" },
  { id: "receive-po", label: "Receive purchase order", Icon: Package, hint: "Warehouse" },
  { id: "transfer", label: "Start stock transfer", Icon: ArrowRightLeft, hint: "Between branches" },
  { id: "invite", label: "Invite staff", Icon: Users, hint: "Organization", dividerBefore: true },
];

const PRESET_ITEMS: Record<OrgEhgMenuGlassPreset, readonly OrgEhgMenuGlassItem[]> = {
  workspace: WORKSPACE_ITEMS,
  account: ACCOUNT_ITEMS,
  "quick-actions": QUICK_ACTION_ITEMS,
};

const PRESET_META: Record<
  OrgEhgMenuGlassPreset,
  { eyebrow: string; title: string; subtitle: string; footer?: string }
> = {
  workspace: {
    eyebrow: "Workspace",
    title: "ExItS Retail Coop",
    subtitle: "Main branch · Owner",
    footer: "Switch branch",
  },
  account: {
    eyebrow: "Signed in",
    title: "Maria Santos",
    subtitle: "maria@exits.example · Owner",
    footer: "Manage account",
  },
  "quick-actions": {
    eyebrow: "Shortcuts",
    title: "Quick actions",
    subtitle: "Common ops for this shift",
  },
};

const DEFAULTS = {
  intensity: "medium" as OrgEhgMenuGlassIntensity,
  preset: "workspace" as OrgEhgMenuGlassPreset,
  density: "comfortable" as OrgEhgMenuGlassDensity,
  showHeader: true,
  showBadges: true,
  showHints: true,
  showFooter: true,
  selected: "inventory",
};

function menuGlassCommand(
  intensity: OrgEhgMenuGlassIntensity,
  preset: OrgEhgMenuGlassPreset,
  density: OrgEhgMenuGlassDensity,
): string {
  return ["ext", "org-ehg", "menu-glass", intensity, preset, density].join(" ");
}

/**
 * Interactive Org Enterprise Hybrid Glass menu card playground (UI Standards / DEV only).
 */
export function OrgEhgMenuGlassPlayground() {
  const { background, setBackground, theme, toggleTheme } = useUiStandardsShellAppearance();
  const [intensity, setIntensity] = useState(DEFAULTS.intensity);
  const [preset, setPreset] = useState(DEFAULTS.preset);
  const [density, setDensity] = useState(DEFAULTS.density);
  const [showHeader, setShowHeader] = useState(DEFAULTS.showHeader);
  const [showBadges, setShowBadges] = useState(DEFAULTS.showBadges);
  const [showHints, setShowHints] = useState(DEFAULTS.showHints);
  const [showFooter, setShowFooter] = useState(DEFAULTS.showFooter);
  const [selected, setSelected] = useState(DEFAULTS.selected);
  const [customTitle, setCustomTitle] = useState("");

  const meta = PRESET_META[preset];
  const items = PRESET_ITEMS[preset];

  function onPresetChange(next: OrgEhgMenuGlassPreset) {
    setPreset(next);
    setSelected(PRESET_ITEMS[next][0]?.id ?? "");
  }

  function resetPreview() {
    setIntensity(DEFAULTS.intensity);
    setPreset(DEFAULTS.preset);
    setDensity(DEFAULTS.density);
    setShowHeader(DEFAULTS.showHeader);
    setShowBadges(DEFAULTS.showBadges);
    setShowHints(DEFAULTS.showHints);
    setShowFooter(DEFAULTS.showFooter);
    setSelected(DEFAULTS.selected);
    setCustomTitle("");
    setBackground(ORG_EHG_PLAYGROUND_BG_DEFAULT);
  }

  return (
    <section
      className="org-ehg org-ehg-pg-bg org-ehg-glass-playground-card flex min-w-0 flex-col"
      data-testid="ui-standard-card-org-ehg-menu-glass-playground"
      data-bg-option={background}
      data-org-ehg-theme={theme}
      data-preview-theme={theme}
      style={{
        ["--org-ehg-bg-intensity" as string]: String(ORG_EHG_PLAYGROUND_BG_DEFAULT_INTENSITY),
      }}
    >
      <div className="org-ehg-bg-lab__base" aria-hidden />
      <div className="org-ehg-bg-lab__decor" aria-hidden />
      <div className="org-ehg-pg-bg__content org-ehg-glass-playground-card__body">
        <div className="flex items-start justify-between gap-3">
          <CardTitle>Menu card playground</CardTitle>
          <div className="flex items-center gap-2">
            <Button
              type="button"
              intent={theme === "dark" ? "primary" : "neutral"}
              appearance={theme === "dark" ? "solid" : "outline"}
              shape="pill"
              size="icon"
              aria-label={theme === "dark" ? "Light mode" : "Dark mode"}
              aria-pressed={theme === "dark"}
              data-testid="ui-standard-org-ehg-menu-pg-theme"
              onClick={toggleTheme}
            >
              {theme === "dark" ? (
                <Moon className="size-4" aria-hidden />
              ) : (
                <Sun className="size-4" aria-hidden />
              )}
            </Button>
            <Button
              type="button"
              intent="neutral"
              appearance="outline"
              shape="pill"
              size="icon"
              aria-label="Reset"
              data-testid="ui-standard-org-ehg-menu-pg-reset"
              onClick={resetPreview}
            >
              <RotateCcw className="size-4" aria-hidden />
            </Button>
          </div>
        </div>

        <PlaygroundSection testId="ui-standard-org-ehg-menu-glass-playground" className="">
          <div className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3">
            <CompactSelect
              label="Preset"
              value={preset}
              options={[...PRESETS]}
              onChange={onPresetChange}
              testId="ui-standard-org-ehg-menu-pg-preset"
            />
            <CompactSelect
              label="Intensity"
              value={intensity}
              options={[...INTENSITIES]}
              onChange={setIntensity}
              testId="ui-standard-org-ehg-menu-pg-intensity"
            />
            <CompactSelect
              label="Density"
              value={density}
              options={[...DENSITIES]}
              onChange={setDensity}
              testId="ui-standard-org-ehg-menu-pg-density"
            />
            <PlaygroundTextField
              label="Custom title"
              value={customTitle}
              onChange={setCustomTitle}
              testId="ui-standard-org-ehg-menu-pg-title"
            />
            <CompactSelect
              label="Background"
              value={background}
              options={[...ORG_EHG_PLAYGROUND_BG_OPTIONS]}
              onChange={setBackground}
              testId="ui-standard-org-ehg-menu-pg-background"
            />
          </div>

          <div className="col-span-full flex flex-row flex-wrap items-center gap-6">
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={showHeader}
                onCheckedChange={setShowHeader}
                aria-label="Show header"
                data-testid="ui-standard-org-ehg-menu-pg-header"
              />
              Header
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={showHints}
                onCheckedChange={setShowHints}
                aria-label="Show hints"
                data-testid="ui-standard-org-ehg-menu-pg-hints"
              />
              Hints
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={showBadges}
                onCheckedChange={setShowBadges}
                aria-label="Show badges"
                data-testid="ui-standard-org-ehg-menu-pg-badges"
              />
              Badges
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={showFooter}
                onCheckedChange={setShowFooter}
                aria-label="Show footer"
                data-testid="ui-standard-org-ehg-menu-pg-footer"
              />
              Footer
            </label>
          </div>

          <div
            className="flex min-w-0 flex-col gap-3"
            data-testid="ui-standard-org-ehg-menu-pg-preview-card"
          >
            <PlaygroundLabel>Preview</PlaygroundLabel>
            <div
              className="org-ehg-menu-glass-panel org-ehg-menu-glass-panel--playground"
              data-testid="ui-standard-org-ehg-menu-pg-preview"
              data-org-ehg-theme={theme}
            >
              <OrgEhgMenuGlassCard
                intensity={intensity}
                density={density}
                preset={preset}
                eyebrow={showHeader ? meta.eyebrow : undefined}
                title={showHeader ? customTitle.trim() || meta.title : undefined}
                subtitle={showHeader ? meta.subtitle : undefined}
                footerLabel={showFooter ? meta.footer : undefined}
                showBadges={showBadges}
                showHints={showHints}
                selected={selected}
                onSelect={setSelected}
                items={items}
                testId="ui-standard-org-ehg-menu-pg-preview-card-surface"
              />
            </div>
          </div>

          <UiStandardsCopyCommand
            standard="Org Menu Glass"
            standardStatus="pilot"
            command={menuGlassCommand(intensity, preset, density)}
            testId="ui-standard-org-ehg-menu-pg-copy"
          />
        </PlaygroundSection>
      </div>
    </section>
  );
}

export function OrgEhgMenuGlassCard({
  intensity = "medium",
  density = "comfortable",
  preset = "workspace",
  eyebrow,
  title,
  subtitle,
  footerLabel,
  showBadges = true,
  showHints = true,
  selected,
  onSelect,
  testId,
  items = WORKSPACE_ITEMS,
}: {
  intensity?: OrgEhgMenuGlassIntensity;
  density?: OrgEhgMenuGlassDensity;
  preset?: OrgEhgMenuGlassPreset;
  eyebrow?: string;
  title?: string;
  subtitle?: string;
  footerLabel?: string;
  showBadges?: boolean;
  showHints?: boolean;
  selected?: string;
  onSelect?: (id: string) => void;
  testId?: string;
  items?: readonly OrgEhgMenuGlassItem[];
}) {
  const showHeader = Boolean(title || eyebrow || subtitle);

  return (
    <nav
      className={[
        "org-ehg-menu-glass",
        `org-ehg-menu-glass--${intensity}`,
        `org-ehg-menu-glass--${density}`,
        `org-ehg-menu-glass--preset-${preset}`,
      ].join(" ")}
      aria-label={title || "Glass menu"}
      data-testid={testId}
    >
      {showHeader ? (
        <header className="org-ehg-menu-glass__header">
          <span className="org-ehg-menu-glass__avatar" aria-hidden>
            {preset === "account" ? (
              <UserRound className="size-4" strokeWidth={2.25} />
            ) : (
              <Building2 className="size-4" strokeWidth={2.25} />
            )}
          </span>
          <div className="org-ehg-menu-glass__header-text">
            {eyebrow ? <span className="org-ehg-menu-glass__eyebrow">{eyebrow}</span> : null}
            {title ? <strong className="org-ehg-menu-glass__heading">{title}</strong> : null}
            {subtitle ? <span className="org-ehg-menu-glass__subtitle">{subtitle}</span> : null}
          </div>
        </header>
      ) : null}

      <ul className="org-ehg-menu-glass__list" role="list">
        {items.map((item) => (
          <li key={item.id} className="org-ehg-menu-glass__item-wrap">
            {item.dividerBefore ? (
              <div className="org-ehg-menu-glass__divider" role="separator" />
            ) : null}
            <button
              type="button"
              className={[
                "org-ehg-menu-glass__item",
                selected === item.id ? "org-ehg-menu-glass__item--selected" : "",
                item.danger ? "org-ehg-menu-glass__item--danger" : "",
              ]
                .filter(Boolean)
                .join(" ")}
              aria-current={selected === item.id ? "page" : undefined}
              data-testid={testId ? `${testId}-item-${item.id}` : undefined}
              onClick={() => onSelect?.(item.id)}
            >
              <item.Icon className="org-ehg-menu-glass__icon" aria-hidden strokeWidth={2.25} />
              <span className="org-ehg-menu-glass__copy">
                <span className="org-ehg-menu-glass__label">{item.label}</span>
                {showHints && item.hint ? (
                  <span className="org-ehg-menu-glass__hint">{item.hint}</span>
                ) : null}
              </span>
              {showBadges && item.badge ? (
                <span className="org-ehg-menu-glass__badge">{item.badge}</span>
              ) : null}
              {!item.danger ? (
                <ChevronRight className="org-ehg-menu-glass__caret" aria-hidden strokeWidth={2.25} />
              ) : null}
            </button>
          </li>
        ))}
      </ul>

      {footerLabel ? (
        <footer className="org-ehg-menu-glass__footer">
          <button type="button" className="org-ehg-menu-glass__footer-btn">
            {footerLabel}
            <ChevronRight className="size-3.5" aria-hidden strokeWidth={2.25} />
          </button>
        </footer>
      ) : null}
    </nav>
  );
}
