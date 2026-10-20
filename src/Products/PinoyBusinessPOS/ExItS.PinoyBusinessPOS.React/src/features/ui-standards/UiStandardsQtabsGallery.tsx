import {
  useEffect,
  useRef,
  useState,
  type CSSProperties,
  type KeyboardEvent,
  type MouseEvent as ReactMouseEvent,
  type ReactNode,
} from "react";
import {
  AlarmClock,
  Calendar,
  ChevronLeft,
  ChevronRight,
  Film,
  Heart,
  Image,
  Mail,
  MapPin,
  MessageCircle,
  MoreVertical,
  Music,
  Settings,
  ShoppingBag,
  Star,
  Users,
  Video,
  type LucideIcon,
} from "lucide-react";

import { ExitsPillSelect } from "@/components/exits/ExitsPillSelect";
import { FilterChip } from "@/components/exits/FilterChip";
import { Card, CardTitle } from "@/components/ui/card";
import { DropdownMenu, MenuItem, useDismissibleOpen } from "@/components/ui/dropdown-menu";
import { SettingsSelect } from "@/components/ui/settings-select";
import { UiStandardsCopyCommand } from "@/features/ui-standards/UiStandardsCopyCommand";
import { cn } from "@/lib/cn";
import { scrollWithinContainer } from "@/lib/scroll-within";

const CARET_DOUBLE_MS = 280;

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

/** Extra tabs so the scrollable sample overflows and can be dragged sideways. */
const TABS_SCROLLABLE: readonly QtabDef[] = [
  ...TABS_3,
  { name: "photos", label: "Photos", icon: Image },
  { name: "videos", label: "Videos", icon: Video },
  { name: "addressbook", label: "Address Book", icon: Users },
  { name: "messages", label: "Messages", icon: MessageCircle },
  { name: "music", label: "Music", icon: Music },
  { name: "shopping", label: "Shopping", icon: ShoppingBag },
  { name: "favorites", label: "Favorites", icon: Heart },
  { name: "places", label: "Places", icon: MapPin },
  { name: "events", label: "Events", icon: Calendar },
  { name: "starred", label: "Starred", icon: Star },
  { name: "settings", label: "Settings", icon: Settings },
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
  arrows = false,
  moreMenu = false,
  panelIdPrefix,
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
  /** Outside carets: single click/tap → next/prev; double → end/start. */
  arrows?: boolean;
  /** Right-end vertical more control with a dropdown of all tabs. */
  moreMenu?: boolean;
  /** When set, each tab gets `id` / `aria-controls` for matching tab panels. */
  panelIdPrefix?: string;
  testId: string;
}) {
  const listRef = useRef<HTMLDivElement | null>(null);
  const refs = useRef<Array<HTMLButtonElement | null>>([]);
  const leftCaretTimer = useRef<number | null>(null);
  const rightCaretTimer = useRef<number | null>(null);
  const moreMenuState = useDismissibleOpen(false);
  const activeIndex = Math.max(
    0,
    tabs.findIndex((tab) => tab.name === value),
  );
  const [focusIndex, setFocusIndex] = useState<number | null>(null);
  const [canScrollLeft, setCanScrollLeft] = useState(false);
  const [canScrollRight, setCanScrollRight] = useState(false);
  const tabStop = focusIndex ?? activeIndex;
  const chrome = arrows || moreMenu;

  function updateOverflow() {
    const list = listRef.current;
    if (!list) {
      setCanScrollLeft(false);
      setCanScrollRight(false);
      return;
    }
    setCanScrollLeft(list.scrollLeft > 1);
    setCanScrollRight(list.scrollLeft + list.clientWidth < list.scrollWidth - 1);
  }

  useEffect(() => {
    if (!chrome) return;
    const list = listRef.current;
    if (!list) return;
    updateOverflow();
    const onScroll = () => updateOverflow();
    list.addEventListener("scroll", onScroll, { passive: true });
    const resize =
      typeof ResizeObserver !== "undefined" ? new ResizeObserver(() => updateOverflow()) : null;
    resize?.observe(list);
    return () => {
      list.removeEventListener("scroll", onScroll);
      resize?.disconnect();
      if (leftCaretTimer.current != null) window.clearTimeout(leftCaretTimer.current);
      if (rightCaretTimer.current != null) window.clearTimeout(rightCaretTimer.current);
    };
  }, [chrome, tabs]);

  function scrollTabIntoView(index: number) {
    const node = refs.current[index];
    const list = listRef.current;
    node?.focus({ preventScroll: true });
    if (node && list) scrollWithinContainer(list, node, "smooth");
    else if (node?.parentElement) scrollWithinContainer(node.parentElement, node);
    window.requestAnimationFrame(updateOverflow);
  }

  function activateIndex(index: number) {
    const next = Math.min(Math.max(index, 0), tabs.length - 1);
    const tab = tabs[next];
    if (!tab) return;
    setFocusIndex(next);
    onChange(tab.name);
    scrollTabIntoView(next);
  }

  function moveFocus(next: number) {
    const index = (next + tabs.length) % tabs.length;
    setFocusIndex(index);
    scrollTabIntoView(index);
  }

  function onKeyDown(event: KeyboardEvent<HTMLButtonElement>, index: number) {
    if (event.key === "ArrowRight") moveFocus(index + 1);
    else if (event.key === "ArrowLeft") moveFocus(index - 1);
    else if (event.key === "Home") moveFocus(0);
    else if (event.key === "End") moveFocus(tabs.length - 1);
    else return;
    event.preventDefault();
  }

  function onCaretPointer(
    side: "left" | "right",
    event: ReactMouseEvent<HTMLButtonElement>,
  ) {
    const timerRef = side === "left" ? leftCaretTimer : rightCaretTimer;
    const step = side === "left" ? -1 : 1;
    const extreme = side === "left" ? 0 : tabs.length - 1;

    if (event.detail === 2) {
      if (timerRef.current != null) {
        window.clearTimeout(timerRef.current);
        timerRef.current = null;
      }
      activateIndex(extreme);
      return;
    }

    if (event.detail !== 1) return;
    if (timerRef.current != null) window.clearTimeout(timerRef.current);
    timerRef.current = window.setTimeout(() => {
      timerRef.current = null;
      activateIndex(activeIndex + step);
    }, CARET_DOUBLE_MS);
  }

  const style = { "--qtabs-color": color, "--qtabs-bg": background ?? "transparent" } as CSSProperties;

  const tablist = (
    <div
      ref={listRef}
      role="tablist"
      aria-label={label}
      aria-orientation="horizontal"
      className={cn(
        "ui-standards-qtabs",
        inlineLabel && "ui-standards-qtabs--inline-label",
        noCaps && "ui-standards-qtabs--no-caps",
        shadow && !chrome && "ui-standards-qtabs--shadow",
      )}
      style={chrome ? undefined : style}
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
            id={panelIdPrefix ? `${panelIdPrefix}-tab-${tab.name}` : undefined}
            type="button"
            role="tab"
            aria-selected={selected}
            aria-controls={panelIdPrefix ? `${panelIdPrefix}-panel-${tab.name}` : undefined}
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

  if (!chrome) {
    return tablist;
  }

  const showLeft = canScrollLeft || activeIndex > 0;
  const showRight = canScrollRight || activeIndex < tabs.length - 1;

  return (
    <div
      className={cn("ui-standards-qtabs-arrows", shadow && "ui-standards-qtabs--shadow")}
      style={style}
      data-testid={`${testId}-arrows`}
    >
      {arrows ? (
        <button
          type="button"
          className={cn(
            "ui-standards-qtabs-caret",
            (!showLeft || activeIndex <= 0) && "ui-standards-qtabs-caret--disabled",
          )}
          aria-label="Previous tab. Double-tap to go to the first tab."
          data-testid={`${testId}-caret-left`}
          disabled={activeIndex <= 0}
          onClick={(event) => onCaretPointer("left", event)}
        >
          <ChevronLeft className="ui-standards-qtabs-caret__icon" aria-hidden />
        </button>
      ) : null}
      {tablist}
      {arrows ? (
        <button
          type="button"
          className={cn(
            "ui-standards-qtabs-caret",
            (!showRight || activeIndex >= tabs.length - 1) && "ui-standards-qtabs-caret--disabled",
          )}
          aria-label="Next tab. Double-tap to go to the last tab."
          data-testid={`${testId}-caret-right`}
          disabled={activeIndex >= tabs.length - 1}
          onClick={(event) => onCaretPointer("right", event)}
        >
          <ChevronRight className="ui-standards-qtabs-caret__icon" aria-hidden />
        </button>
      ) : null}
      {moreMenu ? (
        <DropdownMenu
          align="end"
          open={moreMenuState.open}
          onOpenChange={moreMenuState.setOpen}
          menuLabel="All tabs"
          menuClassName="ui-standards-qtabs-more-menu"
          trigger={({ id, expanded, controls, onClick, onKeyDown: onTriggerKeyDown }) => (
            <button
              id={id}
              type="button"
              className={cn("ui-standards-qtabs-caret", "ui-standards-qtabs-more")}
              aria-label="More tabs"
              aria-haspopup="menu"
              aria-expanded={expanded}
              aria-controls={controls}
              data-testid={`${testId}-more`}
              onClick={onClick}
              onKeyDown={onTriggerKeyDown}
            >
              <MoreVertical className="ui-standards-qtabs-caret__icon" aria-hidden />
            </button>
          )}
        >
          {tabs.map((tab, index) => {
            const Icon = tab.icon;
            const selected = tab.name === value;
            return (
              <MenuItem
                key={tab.name}
                data-testid={`${testId}-more-item-${tab.name}`}
                className={cn(selected && "ui-standards-qtabs-more-item--active")}
                onSelect={() => {
                  activateIndex(index);
                  moreMenuState.close();
                }}
              >
                {Icon ? <Icon className="ui-standards-qtabs-more-item__icon" aria-hidden /> : null}
                <span>{tab.label ?? tab.name}</span>
              </MenuItem>
            );
          })}
        </DropdownMenu>
      ) : null}
    </div>
  );
}

function Example({
  label,
  testId,
  command,
  children,
}: {
  label: string;
  testId: string;
  command?: string;
  children: ReactNode;
}) {
  return (
    <div className="ui-standards-q-example" data-testid={testId}>
      <div className="ui-standards-q-example__label">{label}</div>
      {children}
      {command ? (
        <code className="ui-standards-q-example__command" data-testid={`${testId}-command`}>
          {command}
        </code>
      ) : null}
    </div>
  );
}

/** Quasar QTabs Basic showcase name — Cursor: BASIC TABS + … */
export const BASIC_TABS_SAMPLE_NAME = "Basic tabs" as const;
export const BASIC_TABS_CURSOR_COMMAND = "BASIC TABS + ICON AND LABEL" as const;

const PANEL_LOREM =
  "Lorem ipsum dolor sit amet consectetur adipisicing elit.";

/** Quasar QTabs + QTabPanels “With QTabs” pattern (showcase only). */
function UiStandardsQtabsWithPanels({
  value,
  onChange,
  tabsPosition = "top",
  testId,
}: {
  value: string;
  onChange: (name: string) => void;
  tabsPosition?: "top" | "bottom";
  testId: string;
}) {
  const tabs = (
    <UiStandardsQtabs
      tabs={TABS_3.map(({ name, label }) => ({ name, label }))}
      value={value}
      onChange={onChange}
      label="Panel tabs"
      color="var(--exits-primary)"
      background="transparent"
      noCaps
      panelIdPrefix={testId}
      testId={`${testId}-tabs`}
    />
  );

  const panels = (
    <div className="ui-standards-qtab-panels" data-testid={`${testId}-panels`}>
      {TABS_3.map((tab) => {
        const selected = tab.name === value;
        return (
          <div
            key={tab.name}
            id={`${testId}-panel-${tab.name}`}
            role="tabpanel"
            tabIndex={0}
            hidden={!selected}
            aria-labelledby={`${testId}-tab-${tab.name}`}
            className={cn(
              "ui-standards-qtab-panel",
              selected && "ui-standards-qtab-panel--active",
            )}
            data-testid={`${testId}-panel-${tab.name}`}
          >
            <div className="ui-standards-qtab-panel__title">{tab.label}</div>
            <p className="ui-standards-qtab-panel__body m-0">{PANEL_LOREM}</p>
          </div>
        );
      })}
    </div>
  );

  return (
    <div
      className="ui-standards-qtabs-with-panels"
      data-testid={testId}
      data-tabs-position={tabsPosition}
    >
      {tabsPosition === "top" ? (
        <>
          {tabs}
          <div className="ui-standards-qtabs-separator" aria-hidden />
          {panels}
        </>
      ) : (
        <>
          {panels}
          <div className="ui-standards-qtabs-separator" aria-hidden />
          {tabs}
        </>
      )}
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
  const [panelTab, setPanelTab] = useState("mails");
  const [theme, setTheme] = useState("system");
  const [density, setDensity] = useState("balance");
  const [size, setSize] = useState("XXL");
  const [timing, setTiming] = useState("PayBefore");
  const [sizes, setSizes] = useState<string[]>(["S", "L", "XL"]);
  const [branchFilter, setBranchFilter] = useState<(typeof BRANCH_FILTERS)[number][0]>("all");

  return (
    <div className="flex flex-col gap-4" data-testid="ui-standard-qtabs-gallery">
      <Card
        className="flex min-w-0 flex-col gap-3 rounded-[var(--exits-radius-md)] p-3"
        data-testid="ui-standard-card-basic-tabs"
      >
        <CardTitle data-testid="ui-standard-basic-tabs-title">{BASIC_TABS_SAMPLE_NAME}</CardTitle>
        <div
          className="ui-standards-basic-tabs-grid grid grid-cols-1 gap-4 sm:grid-cols-2"
          data-testid="ui-standard-basic-tabs-grid"
        >
          <Example
            label="Icon and label"
            testId="ui-standard-qtabs-basic"
            command="cursor: ext-tabs-basic-icon-and-label"
          >
            <UiStandardsQtabs
              tabs={TABS_3}
              value={tab}
              onChange={setTab}
              label="Mail tabs"
              color="#26a69a"
              testId="ui-standard-qtabs-teal"
            />
          </Example>
          <Example
            label="Inline label"
            testId="ui-standard-qtabs-inline"
            command="cursor: ext-tabs-basic-inline-label"
          >
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
          <Example
            label="No caps"
            testId="ui-standard-qtabs-no-caps"
            command="cursor: ext-tabs-basic-no-caps"
          >
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
          <Example
            label="Icons only"
            testId="ui-standard-qtabs-icons"
            command="cursor: ext-tabs-basic-icons-only"
          >
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
          <Example
            label="Scrollable"
            testId="ui-standard-qtabs-scroll"
            command="cursor: ext-tabs-basic-scrollable"
          >
            <div className="ui-standards-qtabs-scroll-frame" data-testid="ui-standard-qtabs-scroll-frame">
              <UiStandardsQtabs
                tabs={TABS_SCROLLABLE}
                value={tab}
                onChange={setTab}
                label="Scrollable tabs"
                color="#ffffff"
                background="#1976d2"
                inlineLabel
                shadow
                arrows
                testId="ui-standard-qtabs-primary"
              />
            </div>
          </Example>
          <Example
            label="Scrollable with more"
            testId="ui-standard-qtabs-scroll-more"
            command="cursor: ext-tabs-basic-scrollable-more"
          >
            <div
              className="ui-standards-qtabs-scroll-frame"
              data-testid="ui-standard-qtabs-scroll-more-frame"
            >
              <UiStandardsQtabs
                tabs={TABS_SCROLLABLE}
                value={tab}
                onChange={setTab}
                label="Scrollable tabs with more menu"
                color="#ffffff"
                background="#00897b"
                inlineLabel
                shadow
                moreMenu
                testId="ui-standard-qtabs-teal-more"
              />
            </div>
          </Example>
        </div>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="ui-standard-qtabs-value">
          Selected: {tab}
        </p>
        <UiStandardsCopyCommand
          standard="Tabs"
          standardStatus="pilot"
          command={BASIC_TABS_CURSOR_COMMAND}
          context="Showcase: Quasar QTabs Basic (Icon and label / Inline label / No caps / Icons only / Scrollable / Scrollable with more). Product tabs use ExitsTabs."
          testId="ui-standard-basic-tabs-copy-command"
        />
      </Card>

      <Card
        className="flex min-w-0 flex-col gap-3 rounded-[var(--exits-radius-md)] p-3"
        data-testid="ui-standard-card-tab-panels"
      >
        <CardTitle data-testid="ui-standard-tab-panels-title">Tab panels</CardTitle>
        <div
          className="ui-standards-basic-tabs-grid grid grid-cols-1 gap-4 sm:grid-cols-2"
          data-testid="ui-standard-tab-panels-grid"
        >
          <Example
            label="With QTabs"
            testId="ui-standard-qtabs-panels-top"
            command="cursor: ext-tabs-panels-with-tabs"
          >
            <UiStandardsQtabsWithPanels
              value={panelTab}
              onChange={setPanelTab}
              tabsPosition="top"
              testId="ui-standard-qtabs-panels"
            />
          </Example>
          <Example
            label="Tabs below panels"
            testId="ui-standard-qtabs-panels-bottom"
            command="cursor: ext-tabs-panels-tabs-below"
          >
            <UiStandardsQtabsWithPanels
              value={panelTab}
              onChange={setPanelTab}
              tabsPosition="bottom"
              testId="ui-standard-qtabs-panels-below"
            />
          </Example>
        </div>
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="ui-standard-qtabs-panels-value"
        >
          Panel: {panelTab}
        </p>
        <UiStandardsCopyCommand
          standard="Tabs"
          standardStatus="pilot"
          command="ext-tabs-panels-with-tabs"
          context="Showcase: Quasar QTabs + QTabPanels (With QTabs / Tabs below panels). Product tabs use ExitsTabs panels."
          testId="ui-standard-tab-panels-copy-command"
        />
      </Card>

      <div
        className="ui-standards-basic-tabs-grid grid grid-cols-1 gap-4 sm:grid-cols-2"
        data-testid="ui-standard-tabs-select-pill-row"
      >
        <Example label="Pill select — attribute (single)" testId="ui-standard-tabs-select-pill">
          <ExitsPillSelect
            aria-label="Size"
            value={size}
            onChange={setSize}
            options={SIZE_OPTIONS}
            testId="ui-standard-tabs-select-pill-single"
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
      </div>
      <div
        className="ui-standards-basic-tabs-grid grid grid-cols-1 gap-4 sm:grid-cols-2"
        data-testid="ui-standard-tabs-select-tile-chips-row"
      >
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
      </div>
      <div
        className="ui-standards-basic-tabs-grid grid grid-cols-1 gap-4 sm:grid-cols-2"
        data-testid="ui-standard-tabs-select-cards-segmented-row"
      >
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
      </div>
    </div>
  );
}
