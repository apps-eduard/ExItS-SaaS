import { useRef, useState, type CSSProperties, type KeyboardEvent, type ReactNode } from "react";
import { AlarmClock, Film, Image, Mail, Users, Video, type LucideIcon } from "lucide-react";

import { ExitsPillSelect } from "@/components/exits/ExitsPillSelect";
import { FilterChip } from "@/components/exits/FilterChip";
import { SettingsSelect } from "@/components/ui/settings-select";
import { cn } from "@/lib/cn";
import { scrollWithinContainer } from "@/lib/scroll-within";

/**
 * Quasar QTabs "Basic" example (tab selection only, no panels).
 * Showcase only — product tabs use `ExitsTabs` / `UnderlineTabBar`.
 * @see https://quasar.dev/vue-components/tabs
 */
type QtabDef = { name: string; label?: string; icon?: LucideIcon };

const TABS_3: readonly QtabDef[] = [
  { name: "mails", label: "Mails", icon: Mail },
  { name: "alarms", label: "Alarms", icon: AlarmClock },
  { name: "movies", label: "Movies", icon: Film },
];

const TABS_6: readonly QtabDef[] = [
  ...TABS_3,
  { name: "photos", label: "Photos", icon: Image },
  { name: "videos", label: "Videos", icon: Video },
  { name: "addressbook", label: "Address Book", icon: Users },
];

export function UiStandardsQtabs({
  tabs,
  value,
  onChange,
  label,
  color,
  background,
  inlineLabel = false,
  noCaps = false,
  shadow = false,
  testId,
}: {
  tabs: readonly QtabDef[];
  value: string;
  onChange: (name: string) => void;
  label: string;
  color: string;
  background?: string;
  inlineLabel?: boolean;
  noCaps?: boolean;
  shadow?: boolean;
  testId: string;
}) {
  const refs = useRef<Array<HTMLButtonElement | null>>([]);
  const activeIndex = Math.max(
    0,
    tabs.findIndex((tab) => tab.name === value),
  );
  const [focusIndex, setFocusIndex] = useState<number | null>(null);
  const tabStop = focusIndex ?? activeIndex;

  function moveFocus(next: number) {
    const index = (next + tabs.length) % tabs.length;
    setFocusIndex(index);
    const node = refs.current[index];
    node?.focus({ preventScroll: true });
    if (node?.parentElement) scrollWithinContainer(node.parentElement, node);
  }

  function onKeyDown(event: KeyboardEvent<HTMLButtonElement>, index: number) {
    if (event.key === "ArrowRight") moveFocus(index + 1);
    else if (event.key === "ArrowLeft") moveFocus(index - 1);
    else if (event.key === "Home") moveFocus(0);
    else if (event.key === "End") moveFocus(tabs.length - 1);
    else return;
    event.preventDefault();
  }

  const style = { "--qtabs-color": color, "--qtabs-bg": background ?? "transparent" } as CSSProperties;

  return (
    <div
      role="tablist"
      aria-label={label}
      aria-orientation="horizontal"
      className={cn(
        "ui-standards-qtabs",
        inlineLabel && "ui-standards-qtabs--inline-label",
        noCaps && "ui-standards-qtabs--no-caps",
        shadow && "ui-standards-qtabs--shadow",
      )}
      style={style}
      data-testid={testId}
      onBlur={(event) => {
        if (!event.currentTarget.contains(event.relatedTarget as Node | null)) setFocusIndex(null);
      }}
    >
      {tabs.map((tab, index) => {
        const Icon = tab.icon;
        const selected = tab.name === value;
        return (
          <button
            key={tab.name}
            ref={(node) => {
              refs.current[index] = node;
            }}
            type="button"
            role="tab"
            aria-selected={selected}
            aria-label={tab.label ? undefined : tab.name}
            tabIndex={index === tabStop ? 0 : -1}
            className={cn("ui-standards-qtab", selected && "ui-standards-qtab--active")}
            data-testid={`${testId}-${tab.name}`}
            onClick={() => {
              setFocusIndex(index);
              onChange(tab.name);
            }}
            onKeyDown={(event) => onKeyDown(event, index)}
          >
            <span className="ui-standards-qtab__content">
              {Icon ? <Icon className="ui-standards-qtab__icon" aria-hidden /> : null}
              {tab.label ? <span className="ui-standards-qtab__label">{tab.label}</span> : null}
            </span>
            <span className="ui-standards-qtab__indicator" aria-hidden />
          </button>
        );
      })}
    </div>
  );
}

function Example({ label, testId, children }: { label: string; testId: string; children: ReactNode }) {
  return (
    <div className="ui-standards-q-example" data-testid={testId}>
      <div className="ui-standards-q-example__label">{label}</div>
      {children}
    </div>
  );
}

const THEME_OPTIONS = [
  { value: "system", label: "System" },
  { value: "light", label: "Light" },
  { value: "dark", label: "Dark" },
];

const SIZE_OPTIONS = ["XS", "S", "M", "L", "XL", "XXL"].map((size) => ({ value: size, label: size }));

const BRANCH_FILTERS = [
  ["all", "All"],
  ["active", "Active"],
  ["inactive", "Inactive"],
] as const;

export function UiStandardsQtabsGallery(): ReactNode {
  const [tab, setTab] = useState("mails");
  const [theme, setTheme] = useState("system");
  const [density, setDensity] = useState("balance");
  const [size, setSize] = useState("XXL");
  const [timing, setTiming] = useState("PayBefore");
  const [sizes, setSizes] = useState<string[]>(["S", "L", "XL"]);
  const [branchFilter, setBranchFilter] = useState<(typeof BRANCH_FILTERS)[number][0]>("all");

  return (
    <div className="flex flex-col gap-4" data-testid="ui-standard-qtabs-gallery">
      <Example label="Pill select — attribute (single)" testId="ui-standard-tabs-select-pill">
        <ExitsPillSelect
          aria-label="Size"
          value={size}
          onChange={setSize}
          options={SIZE_OPTIONS}
          testId="ui-standard-tabs-select-pill-single"
        />
      </Example>
      <Example label="Attribute select — tile (single)" testId="ui-standard-tabs-select-tile">
        <ExitsPillSelect
          appearance="tile"
          aria-label="Payment timing"
          value={timing}
          onChange={setTiming}
          options={[
            { value: "PayBefore", label: "Pay in advance" },
            { value: "PayOnDelivery", label: "Pay on delivery" },
            { value: "Utang", label: "Supplier Utang" },
          ]}
          className="grid-cols-3"
          testId="ui-standard-tabs-select-tile-single"
        />
      </Example>
      <Example label="Pill select — multi" testId="ui-standard-tabs-select-multi">
        <ExitsPillSelect
          mode="multi"
          aria-label="Available sizes"
          value={sizes}
          onChange={setSizes}
          options={SIZE_OPTIONS}
          testId="ui-standard-tabs-select-pill-multi"
        />
      </Example>
      <Example label="Filter chips — exclusive" testId="ui-standard-tabs-select-chips">
        <div className="flex flex-wrap gap-2" role="group" aria-label="Branch filter">
          {BRANCH_FILTERS.map(([key, label]) => (
            <FilterChip
              key={key}
              selected={branchFilter === key}
              onClick={() => setBranchFilter(key)}
              data-testid={`ui-standard-tabs-select-filter-${key}`}
            >
              {label}
            </FilterChip>
          ))}
        </div>
      </Example>
      <Example label="Card select (ExItS)" testId="ui-standard-tabs-select-cards">
        <SettingsSelect
          label="Appearance"
          variant="cards"
          value={theme}
          options={THEME_OPTIONS}
          onChange={setTheme}
          testId="ui-standard-tabs-select-theme"
        />
      </Example>
      <Example label="Segmented select (ExItS)" testId="ui-standard-tabs-select-segmented">
        <SettingsSelect
          label="Density"
          variant="segmented"
          value={density}
          options={[
            { value: "compact", label: "Compact" },
            { value: "balance", label: "Balance" },
            { value: "comfort", label: "Comfort" },
          ]}
          onChange={setDensity}
          testId="ui-standard-tabs-select-density"
        />
      </Example>
      <div className="flex max-w-[600px] flex-col gap-4">
      <Example label="Icon and label" testId="ui-standard-qtabs-basic">
        <UiStandardsQtabs tabs={TABS_3} value={tab} onChange={setTab} label="Mail tabs" color="#26a69a" testId="ui-standard-qtabs-teal" />
      </Example>
      <Example label="Inline label" testId="ui-standard-qtabs-inline">
        <UiStandardsQtabs
          tabs={TABS_3}
          value={tab}
          onChange={setTab}
          label="Inline label tabs"
          color="#ffffff"
          background="#9c27b0"
          inlineLabel
          shadow
          testId="ui-standard-qtabs-purple"
        />
      </Example>
      <Example label="No caps" testId="ui-standard-qtabs-no-caps">
        <UiStandardsQtabs
          tabs={TABS_3.map(({ name, label }) => ({ name, label }))}
          value={tab}
          onChange={setTab}
          label="No caps tabs"
          color="#ffffff"
          background="#ff9800"
          noCaps
          shadow
          testId="ui-standard-qtabs-orange"
        />
      </Example>
      <Example label="Icons only" testId="ui-standard-qtabs-icons">
        <UiStandardsQtabs
          tabs={TABS_3.map(({ name, icon }) => ({ name, icon }))}
          value={tab}
          onChange={setTab}
          label="Icon tabs"
          color="#ffeb3b"
          background="#26a69a"
          shadow
          testId="ui-standard-qtabs-yellow"
        />
      </Example>
      <Example label="Scrollable" testId="ui-standard-qtabs-scroll">
        <UiStandardsQtabs
          tabs={TABS_6}
          value={tab}
          onChange={setTab}
          label="Scrollable tabs"
          color="#ffffff"
          background="#1976d2"
          inlineLabel
          shadow
          testId="ui-standard-qtabs-primary"
        />
      </Example>
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="ui-standard-qtabs-value">
        Selected: {tab}
      </p>
      </div>
    </div>
  );
}
