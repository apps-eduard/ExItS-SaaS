import { useState } from "react";
import {
  AlertTriangle,
  Check,
  ChevronDown,
  Heart,
  Info,
  Loader2,
  Moon,
  Plus,
  RotateCcw,
  Settings,
  ShoppingCart,
  Sparkles,
  Sun,
  type LucideIcon,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { CardTitle } from "@/components/ui/card";
import { Switch } from "@/components/ui/switch";
import { DropdownMenu, MenuItem, useDismissibleOpen } from "@/components/ui/dropdown-menu";
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

const DROPDOWN_ACTIONS = [
  { label: "Save draft", value: "save-draft" },
  { label: "Save & continue", value: "save-continue" },
  { label: "Duplicate", value: "duplicate" },
  { label: "Archive", value: "archive" },
] as const;

type GlassIntensity = "solid" | "soft" | "medium" | "strong" | "ultra";
type GlassTone =
  | "primary"
  | "secondary"
  | "outline"
  | "ghost"
  | "text"
  | "success"
  | "warning"
  | "danger"
  | "info"
  | "neutral";
type GlassSize = "xs" | "sm" | "md" | "lg" | "xl";
type GlassState = "default" | "hover" | "focused" | "pressed" | "disabled" | "loading";
type GlassIconId = "none" | "check" | "plus" | "heart" | "settings" | "cart" | "info" | "alert" | "sparkles";
type GlassIconPlace = "left" | "right" | "top";
type GlassFocusAnimation = "none" | "ring-pulse" | "glow" | "breathe";

const INTENSITIES: ReadonlyArray<{ label: string; value: GlassIntensity }> = [
  { label: "Solid", value: "solid" },
  { label: "Soft Glass", value: "soft" },
  { label: "Medium Glass", value: "medium" },
  { label: "Strong Glass", value: "strong" },
  { label: "Ultra Glass", value: "ultra" },
];

const TONES: ReadonlyArray<{ label: string; value: GlassTone }> = [
  { label: "Primary", value: "primary" },
  { label: "Secondary", value: "secondary" },
  { label: "Outline", value: "outline" },
  { label: "Ghost", value: "ghost" },
  { label: "Text", value: "text" },
  { label: "Success", value: "success" },
  { label: "Warning", value: "warning" },
  { label: "Danger", value: "danger" },
  { label: "Info", value: "info" },
  { label: "Neutral", value: "neutral" },
];

const SIZES: ReadonlyArray<{ label: string; value: GlassSize }> = [
  { label: "XS", value: "xs" },
  { label: "SM", value: "sm" },
  { label: "MD", value: "md" },
  { label: "LG", value: "lg" },
  { label: "XL", value: "xl" },
];

const STATES: ReadonlyArray<{ label: string; value: GlassState }> = [
  { label: "Default", value: "default" },
  { label: "Hover", value: "hover" },
  { label: "Focused", value: "focused" },
  { label: "Pressed", value: "pressed" },
  { label: "Disabled", value: "disabled" },
  { label: "Loading", value: "loading" },
];

const ICONS: ReadonlyArray<{ label: string; value: GlassIconId; Icon: LucideIcon | null }> = [
  { label: "None", value: "none", Icon: null },
  { label: "Check", value: "check", Icon: Check },
  { label: "Plus", value: "plus", Icon: Plus },
  { label: "Heart", value: "heart", Icon: Heart },
  { label: "Settings", value: "settings", Icon: Settings },
  { label: "Cart", value: "cart", Icon: ShoppingCart },
  { label: "Info", value: "info", Icon: Info },
  { label: "Alert", value: "alert", Icon: AlertTriangle },
  { label: "Sparkles", value: "sparkles", Icon: Sparkles },
];

const ICON_PLACES: ReadonlyArray<{ label: string; value: GlassIconPlace }> = [
  { label: "Left", value: "left" },
  { label: "Right", value: "right" },
  { label: "Top", value: "top" },
];

const FOCUS_ANIMATIONS: ReadonlyArray<{ label: string; value: GlassFocusAnimation }> = [
  { label: "None", value: "none" },
  { label: "Ring pulse", value: "ring-pulse" },
  { label: "Glow", value: "glow" },
  { label: "Breathe", value: "breathe" },
];

const DEFAULTS = {
  intensity: "medium" as GlassIntensity,
  tone: "primary" as GlassTone,
  size: "md" as GlassSize,
  state: "default" as GlassState,
  iconId: "check" as GlassIconId,
  iconPlace: "left" as GlassIconPlace,
  iconOnly: false,
  dropdown: false,
  badge: false,
  label: "Glass button",
  focusAnimation: "none" as GlassFocusAnimation,
};

function glassCommand(
  intensity: GlassIntensity,
  tone: GlassTone,
  size: GlassSize,
  state: GlassState,
  iconPlace: GlassIconPlace,
  iconOnly: boolean,
  dropdown: boolean,
  badge: boolean,
  focusAnimation: GlassFocusAnimation,
): string {
  const parts = ["ext", "org-ehg", "btn-glass", intensity, tone, size];
  if (state !== "default") parts.push(state);
  if (iconOnly) parts.push("icon-only");
  else if (iconPlace !== "left") parts.push(`icon-${iconPlace}`);
  if (dropdown) parts.push("dropdown");
  if (badge) parts.push("badge");
  if (focusAnimation !== "none") parts.push(`focus-${focusAnimation}`);
  return parts.join(" ");
}

/**
 * Interactive Org Enterprise Hybrid Glass button playground (UI Standards / DEV only).
 */
export function OrgEhgButtonGlassPlayground() {
  const { background, setBackground, theme, toggleTheme } = useUiStandardsShellAppearance();
  const [intensity, setIntensity] = useState(DEFAULTS.intensity);
  const [tone, setTone] = useState(DEFAULTS.tone);
  const [size, setSize] = useState(DEFAULTS.size);
  const [state, setState] = useState(DEFAULTS.state);
  const [iconId, setIconId] = useState(DEFAULTS.iconId);
  const [iconPlace, setIconPlace] = useState(DEFAULTS.iconPlace);
  const [iconOnly, setIconOnly] = useState(DEFAULTS.iconOnly);
  const [dropdown, setDropdown] = useState(DEFAULTS.dropdown);
  const [badge, setBadge] = useState(DEFAULTS.badge);
  const [label, setLabel] = useState(DEFAULTS.label);
  const [focusAnimation, setFocusAnimation] = useState(DEFAULTS.focusAnimation);
  const [menuChoice, setMenuChoice] = useState<string>("");
  const menu = useDismissibleOpen(false);

  const Icon = ICONS.find((item) => item.value === iconId)?.Icon ?? null;
  const showIconOnly = iconOnly;
  const PreviewIcon = showIconOnly ? Icon ?? Heart : Icon;
  const iconOnTop = !showIconOnly && iconPlace === "top" && PreviewIcon != null;
  const iconOnRight = !showIconOnly && iconPlace === "right" && PreviewIcon != null;
  const iconOnLeft = !showIconOnly && iconPlace === "left" && PreviewIcon != null;
  const disabled = state === "disabled" || state === "loading";
  const className = [
    "org-ehg-btn-glass",
    `org-ehg-btn-glass--${intensity}`,
    `org-ehg-btn-glass--tone-${tone}`,
    `org-ehg-btn-glass--size-${size}`,
    state !== "default" ? `org-ehg-btn-glass--state-${state}` : "",
    showIconOnly ? "org-ehg-btn-glass--icon-only" : "",
    iconOnTop ? "org-ehg-btn-glass--icon-top" : "",
    dropdown ? "org-ehg-btn-glass--with-dropdown" : "",
    focusAnimation !== "none" ? `org-ehg-btn-glass--focus-${focusAnimation}` : "",
  ]
    .filter(Boolean)
    .join(" ");

  function resetPreview() {
    setIntensity(DEFAULTS.intensity);
    setTone(DEFAULTS.tone);
    setSize(DEFAULTS.size);
    setState(DEFAULTS.state);
    setIconId(DEFAULTS.iconId);
    setIconPlace(DEFAULTS.iconPlace);
    setIconOnly(DEFAULTS.iconOnly);
    setDropdown(DEFAULTS.dropdown);
    setBadge(DEFAULTS.badge);
    setLabel(DEFAULTS.label);
    setBackground(ORG_EHG_PLAYGROUND_BG_DEFAULT);
    setFocusAnimation(DEFAULTS.focusAnimation);
    setMenuChoice("");
    menu.close();
  }

  function onIconPlaceChange(next: GlassIconPlace) {
    setIconPlace(next);
    if (next === "top") setIconOnly(false);
  }

  function onIconOnlyChange(next: boolean) {
    setIconOnly(next);
    if (next) setIconPlace("left");
  }

  return (
    <section
      className="org-ehg org-ehg-pg-bg org-ehg-glass-playground-card flex min-w-0 flex-col"
      data-testid="ui-standard-card-org-ehg-btn-glass-playground"
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
          <CardTitle>Glass button playground</CardTitle>
          <div className="flex items-center gap-2">
            <Button
              type="button"
              intent={theme === "dark" ? "primary" : "neutral"}
              appearance={theme === "dark" ? "solid" : "outline"}
              shape="pill"
              size="icon"
              aria-label={theme === "dark" ? "Light mode" : "Dark mode"}
              aria-pressed={theme === "dark"}
              data-testid="ui-standard-org-ehg-btn-pg-theme"
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
              data-testid="ui-standard-org-ehg-btn-pg-reset"
              onClick={resetPreview}
            >
              <RotateCcw className="size-4" aria-hidden />
            </Button>
          </div>
        </div>
      <PlaygroundSection testId="ui-standard-org-ehg-btn-glass-playground" className="">
        <div className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3">
          <CompactSelect
            label="Intensity"
            value={intensity}
            options={[...INTENSITIES]}
            onChange={setIntensity}
            testId="ui-standard-org-ehg-btn-pg-intensity"
          />
          <CompactSelect
            label="Type"
            value={tone}
            options={[...TONES]}
            onChange={setTone}
            testId="ui-standard-org-ehg-btn-pg-tone"
          />
          <CompactSelect
            label="Size"
            value={size}
            options={[...SIZES]}
            onChange={setSize}
            testId="ui-standard-org-ehg-btn-pg-size"
          />
          <CompactSelect
            label="State"
            value={state}
            options={[...STATES]}
            onChange={setState}
            testId="ui-standard-org-ehg-btn-pg-state"
          />
          <CompactSelect
            label="Icon"
            value={iconId}
            options={ICONS.map((item) => ({ label: item.label, value: item.value }))}
            onChange={setIconId}
            testId="ui-standard-org-ehg-btn-pg-icon"
          />
          <CompactSelect
            label="Icon place"
            value={iconPlace}
            options={[...ICON_PLACES]}
            onChange={onIconPlaceChange}
            testId="ui-standard-org-ehg-btn-pg-icon-place"
          />
          <PlaygroundTextField
            label="Label"
            value={label}
            onChange={setLabel}
            testId="ui-standard-org-ehg-btn-pg-label"
          />
          <CompactSelect
            label="Background"
            value={background}
            options={[...ORG_EHG_PLAYGROUND_BG_OPTIONS]}
            onChange={setBackground}
            testId="ui-standard-org-ehg-btn-pg-background"
          />
          <CompactSelect
            label="Focus animation"
            value={focusAnimation}
            options={[...FOCUS_ANIMATIONS]}
            onChange={setFocusAnimation}
            testId="ui-standard-org-ehg-btn-pg-focus-animation"
          />
        </div>

        <div className="col-span-full flex flex-row flex-wrap items-center gap-6">
          <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
            <Switch
              checked={iconOnly}
              onCheckedChange={onIconOnlyChange}
              aria-label="Icon only"
              data-testid="ui-standard-org-ehg-btn-pg-icon-only"
            />
            Icon only
          </label>
          <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
            <Switch
              checked={dropdown}
              onCheckedChange={setDropdown}
              aria-label="Dropdown"
              data-testid="ui-standard-org-ehg-btn-pg-dropdown"
            />
            Dropdown
          </label>
          <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
            <Switch
              checked={badge}
              onCheckedChange={setBadge}
              aria-label="Badge"
              data-testid="ui-standard-org-ehg-btn-pg-badge"
            />
            Badge
          </label>
        </div>

        <div
          className="flex min-w-0 flex-col gap-3"
          data-testid="ui-standard-org-ehg-btn-pg-preview-card"
        >
          <PlaygroundLabel>Preview</PlaygroundLabel>
          <div
            className="org-ehg-btn-glass-panel org-ehg-btn-glass-panel--playground"
            data-org-ehg-theme={theme}
            data-bg-option={background}
            data-testid="ui-standard-org-ehg-btn-pg-preview"
            data-preview-theme={theme}
          >
            {dropdown ? (
              <DropdownMenu
                open={menu.open && !disabled}
                onOpenChange={(next) => {
                  if (!disabled) menu.setOpen(next);
                }}
                align="start"
                menuClassName={
                  "org-ehg-glass-menu" + (theme === "dark" ? " org-ehg-glass-menu--dark" : "")
                }
                menuLabel={label.trim() || "Actions"}
                trigger={({ id, expanded, controls, onClick, onKeyDown }) => (
                  <button
                    type="button"
                    id={id}
                    className={className}
                    data-testid="ui-standard-org-ehg-btn-pg-preview-btn"
                    disabled={disabled}
                    aria-busy={state === "loading" || undefined}
                    aria-label={showIconOnly ? label.trim() || "Glass button" : undefined}
                    aria-haspopup="menu"
                    aria-expanded={expanded}
                    aria-controls={controls}
                    onClick={onClick}
                    onKeyDown={onKeyDown}
                  >
                    {state === "loading" ? null : iconOnLeft || showIconOnly || iconOnTop ? (
                      PreviewIcon ? (
                        <PreviewIcon
                          className="org-ehg-btn-glass__icon"
                          aria-hidden
                          strokeWidth={2.25}
                          data-testid="ui-standard-org-ehg-btn-pg-preview-icon"
                        />
                      ) : null
                    ) : null}
                    {showIconOnly ? null : (
                      <span>
                        {menuChoice
                          ? DROPDOWN_ACTIONS.find((item) => item.value === menuChoice)?.label ??
                            (label.trim() || "Glass button")
                          : label.trim() || "Glass button"}
                      </span>
                    )}
                    {state === "loading" ? null : iconOnRight ? (
                      PreviewIcon ? (
                        <PreviewIcon
                          className="org-ehg-btn-glass__icon"
                          aria-hidden
                          strokeWidth={2.25}
                          data-testid="ui-standard-org-ehg-btn-pg-preview-icon"
                        />
                      ) : null
                    ) : null}
                    {state === "loading" ? (
                      <Loader2
                        className="org-ehg-btn-glass__icon org-ehg-btn-glass__spin"
                        aria-hidden
                        strokeWidth={2.25}
                      />
                    ) : (
                      <ChevronDown
                        className="org-ehg-btn-glass__icon org-ehg-btn-glass__caret"
                        aria-hidden
                        strokeWidth={2.25}
                        data-testid="ui-standard-org-ehg-btn-pg-preview-caret"
                      />
                    )}
                    {badge ? (
                      <span className="org-ehg-btn-glass__badge" aria-hidden>
                        3
                      </span>
                    ) : null}
                  </button>
                )}
              >
                <div
                  className="org-ehg-glass-menu__list"
                  role="group"
                  data-testid="ui-standard-org-ehg-btn-pg-preview-menu"
                >
                  {DROPDOWN_ACTIONS.map((action) => (
                    <MenuItem
                      key={action.value}
                      className={
                        "org-ehg-glass-menu__option" +
                        (menuChoice === action.value ? " org-ehg-glass-menu__option--selected" : "")
                      }
                      data-selected={menuChoice === action.value ? "true" : "false"}
                      data-testid={`ui-standard-org-ehg-btn-pg-preview-option-${action.value}`}
                      onSelect={() => {
                        setMenuChoice(action.value);
                        menu.close();
                      }}
                    >
                      {action.label}
                    </MenuItem>
                  ))}
                </div>
              </DropdownMenu>
            ) : (
              <button
                type="button"
                className={className}
                data-testid="ui-standard-org-ehg-btn-pg-preview-btn"
                disabled={disabled}
                aria-busy={state === "loading" || undefined}
                aria-label={showIconOnly ? label.trim() || "Glass button" : undefined}
              >
                {state === "loading" ? null : iconOnLeft || showIconOnly || iconOnTop ? (
                  PreviewIcon ? (
                    <PreviewIcon
                      className="org-ehg-btn-glass__icon"
                      aria-hidden
                      strokeWidth={2.25}
                      data-testid="ui-standard-org-ehg-btn-pg-preview-icon"
                    />
                  ) : null
                ) : null}
                {showIconOnly ? null : <span>{label.trim() || "Glass button"}</span>}
                {state === "loading" ? null : iconOnRight ? (
                  PreviewIcon ? (
                    <PreviewIcon
                      className="org-ehg-btn-glass__icon"
                      aria-hidden
                      strokeWidth={2.25}
                      data-testid="ui-standard-org-ehg-btn-pg-preview-icon"
                    />
                  ) : null
                ) : null}
                {state === "loading" ? (
                  <Loader2
                    className="org-ehg-btn-glass__icon org-ehg-btn-glass__spin"
                    aria-hidden
                    strokeWidth={2.25}
                  />
                ) : null}
                {badge ? (
                  <span className="org-ehg-btn-glass__badge" aria-hidden>
                    3
                  </span>
                ) : null}
              </button>
            )}
          </div>
        </div>

        <UiStandardsCopyCommand
          standard="Org Button Glass"
          standardStatus="pilot"
          command={glassCommand(
            intensity,
            tone,
            size,
            state,
            iconPlace,
            iconOnly,
            dropdown,
            badge,
            focusAnimation,
          )}
          testId="ui-standard-org-ehg-btn-pg-copy"
        />
      </PlaygroundSection>
      </div>
    </section>
  );
}
