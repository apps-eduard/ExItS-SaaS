import { useState } from "react";
import {
  Bell,
  CalendarDays,
  ChevronDown,
  ChevronRight,
  Copy,
  Download,
  Eye,
  FileText,
  Moon,
  Package,
  Pencil,
  Plus,
  Receipt,
  RotateCcw,
  Settings,
  ShoppingCart,
  Sun,
  Trash2,
  UserRound,
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
import type {
  OrgEhgQaGlassIconShape,
  OrgEhgQaGlassSize,
  OrgEhgQaGlassTone,
  OrgEhgQaGlassTrailing,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgQuickActionGlassGallery";
import { useUiStandardsShellAppearance } from "@/features/ui-standards/UiStandardsShellAppearance";

type QaIconId =
  | "plus"
  | "file"
  | "cart"
  | "user"
  | "package"
  | "receipt"
  | "download"
  | "calendar"
  | "bell"
  | "eye"
  | "trash"
  | "settings";

const TONES: ReadonlyArray<{ label: string; value: OrgEhgQaGlassTone }> = [
  { label: "Amber", value: "amber" },
  { label: "Charcoal", value: "charcoal" },
  { label: "Cyan", value: "cyan" },
  { label: "Danger", value: "danger" },
  { label: "Emerald", value: "emerald" },
  { label: "Fuchsia", value: "fuchsia" },
  { label: "Indigo", value: "indigo" },
  { label: "Info", value: "info" },
  { label: "Inventory", value: "inventory" },
  { label: "Lime", value: "lime" },
  { label: "Neutral", value: "neutral" },
  { label: "Orange", value: "orange" },
  { label: "People", value: "people" },
  { label: "Primary", value: "primary" },
  { label: "Rose", value: "rose" },
  { label: "Sales / POS", value: "sales" },
  { label: "Secondary", value: "secondary" },
  { label: "Sky", value: "sky" },
  { label: "Success", value: "success" },
  { label: "Teal", value: "teal" },
  { label: "Violet", value: "violet" },
  { label: "Warning", value: "warning" },
];

const ICON_SHAPES: ReadonlyArray<{ label: string; value: OrgEhgQaGlassIconShape }> = [
  { label: "Soft square", value: "soft" },
  { label: "Rounded circle", value: "rounded" },
];

const SIZES: ReadonlyArray<{ label: string; value: OrgEhgQaGlassSize }> = [
  { label: "SM", value: "sm" },
  { label: "MD", value: "md" },
  { label: "LG", value: "lg" },
];

const TRAILINGS: ReadonlyArray<{ label: string; value: OrgEhgQaGlassTrailing }> = [
  { label: "Chevron", value: "chevron" },
  { label: "Caret dropdown", value: "caret" },
  { label: "Toggle", value: "toggle" },
  { label: "Action group", value: "action-group" },
  { label: "Action icon", value: "action-icon" },
  { label: "None", value: "none" },
];

const ICONS: ReadonlyArray<{ label: string; value: QaIconId; Icon: LucideIcon }> = [
  { label: "Plus", value: "plus", Icon: Plus },
  { label: "Document", value: "file", Icon: FileText },
  { label: "Cart", value: "cart", Icon: ShoppingCart },
  { label: "User", value: "user", Icon: UserRound },
  { label: "Package", value: "package", Icon: Package },
  { label: "Receipt", value: "receipt", Icon: Receipt },
  { label: "Download", value: "download", Icon: Download },
  { label: "Calendar", value: "calendar", Icon: CalendarDays },
  { label: "Bell", value: "bell", Icon: Bell },
  { label: "Eye", value: "eye", Icon: Eye },
  { label: "Trash", value: "trash", Icon: Trash2 },
  { label: "Settings", value: "settings", Icon: Settings },
];

const DEFAULTS = {
  tone: "primary" as OrgEhgQaGlassTone,
  trailing: "chevron" as OrgEhgQaGlassTrailing,
  iconId: "plus" as QaIconId,
  iconShape: "soft" as OrgEhgQaGlassIconShape,
  size: "md" as OrgEhgQaGlassSize,
  label: "Add New",
  badge: false,
  ghost: false,
  split: false,
  iconOnly: false,
  iconFilled: true,
};

function qaGlassCommand(
  tone: OrgEhgQaGlassTone,
  trailing: OrgEhgQaGlassTrailing,
  iconShape: OrgEhgQaGlassIconShape,
  size: OrgEhgQaGlassSize,
  iconOnly: boolean,
  badge: boolean,
  ghost: boolean,
  split: boolean,
): string {
  const parts = ["ext", "org-ehg", "qa-glass", tone, size, `icon-${iconShape}`];
  if (iconOnly) parts.push("icon-only");
  else parts.push(`trail-${trailing}`);
  if (badge) parts.push("badge");
  if (ghost) parts.push("ghost");
  if (split) parts.push("split");
  return parts.join(" ");
}

/**
 * Interactive Org Enterprise Hybrid Glass Quick Action playground (UI Standards / DEV only).
 */
export function OrgEhgQuickActionGlassPlayground() {
  const { background, setBackground, theme, toggleTheme } = useUiStandardsShellAppearance();
  const [tone, setTone] = useState(DEFAULTS.tone);
  const [trailing, setTrailing] = useState(DEFAULTS.trailing);
  const [iconId, setIconId] = useState(DEFAULTS.iconId);
  const [iconShape, setIconShape] = useState(DEFAULTS.iconShape);
  const [size, setSize] = useState(DEFAULTS.size);
  const [label, setLabel] = useState(DEFAULTS.label);
  const [badge, setBadge] = useState(DEFAULTS.badge);
  const [ghost, setGhost] = useState(DEFAULTS.ghost);
  const [split, setSplit] = useState(DEFAULTS.split);
  const [iconOnly, setIconOnly] = useState(DEFAULTS.iconOnly);
  const [iconFilled, setIconFilled] = useState(DEFAULTS.iconFilled);
  const [toggled, setToggled] = useState(false);

  const Icon = ICONS.find((item) => item.value === iconId)?.Icon ?? Plus;
  const displayLabel = label.trim() || "Quick action";

  function resetPreview() {
    setTone(DEFAULTS.tone);
    setTrailing(DEFAULTS.trailing);
    setIconId(DEFAULTS.iconId);
    setIconShape(DEFAULTS.iconShape);
    setSize(DEFAULTS.size);
    setLabel(DEFAULTS.label);
    setBadge(DEFAULTS.badge);
    setGhost(DEFAULTS.ghost);
    setSplit(DEFAULTS.split);
    setIconOnly(DEFAULTS.iconOnly);
    setIconFilled(DEFAULTS.iconFilled);
    setToggled(false);
    setBackground(ORG_EHG_PLAYGROUND_BG_DEFAULT);
  }

  function onIconOnlyChange(next: boolean) {
    setIconOnly(next);
    if (next) {
      setSplit(false);
      setGhost(false);
    }
  }

  function onSplitChange(next: boolean) {
    setSplit(next);
    if (next) {
      setIconOnly(false);
      setTrailing("caret");
    }
  }

  function onTrailingChange(next: OrgEhgQaGlassTrailing) {
    setTrailing(next);
    if (next === "action-group" || next === "action-icon") {
      setSplit(false);
      setIconOnly(false);
    }
  }

  const actionIconShapeClass =
    iconShape === "rounded" ? " org-ehg-qa-glass-icon--rounded" : " org-ehg-qa-glass-icon--soft";

  const actionToneClass = `org-ehg-qa-glass-icon--tone-${tone}`;

  const actionSizeClass = ` org-ehg-qa-glass-icon--size-${size}`;

  const trailingActionGroup = (
    <div
      className={`org-ehg-qa-glass__action-group org-ehg-qa-glass__action-group--size-${size}`}
      role="group"
      aria-label="Actions"
      data-testid="ui-standard-org-ehg-qa-pg-preview-group"
      data-icon-shape={iconShape}
      data-size={size}
    >
      <button
        type="button"
        className={`org-ehg-qa-glass-icon org-ehg-qa-glass-icon--action org-ehg-qa-glass-icon--tone-primary${actionSizeClass}${actionIconShapeClass}`}
        aria-label="Edit"
      >
        <Pencil className="org-ehg-qa-glass__glyph" aria-hidden strokeWidth={2.25} />
      </button>
      <button
        type="button"
        className={`org-ehg-qa-glass-icon org-ehg-qa-glass-icon--action org-ehg-qa-glass-icon--tone-secondary${actionSizeClass}${actionIconShapeClass}`}
        aria-label="Copy"
      >
        <Copy className="org-ehg-qa-glass__glyph" aria-hidden strokeWidth={2.25} />
      </button>
      <button
        type="button"
        className={`org-ehg-qa-glass-icon org-ehg-qa-glass-icon--action org-ehg-qa-glass-icon--tone-danger${actionSizeClass}${actionIconShapeClass}`}
        aria-label="Delete"
      >
        <Trash2 className="org-ehg-qa-glass__glyph" aria-hidden strokeWidth={2.25} />
      </button>
    </div>
  );

  const trailingActionIcon = (
    <button
      type="button"
      className={`org-ehg-qa-glass-icon org-ehg-qa-glass-icon--action ${actionToneClass}${actionSizeClass}${actionIconShapeClass}`}
      aria-label={displayLabel}
      data-testid="ui-standard-org-ehg-qa-pg-preview-action-icon"
      data-icon-shape={iconShape}
      data-size={size}
    >
      <Icon className="org-ehg-qa-glass__glyph" aria-hidden strokeWidth={2.25} />
    </button>
  );

  const rowClass = [
    "org-ehg-qa-glass",
    `org-ehg-qa-glass--tone-${tone}`,
    `org-ehg-qa-glass--size-${size}`,
    ghost ? "org-ehg-qa-glass--ghost" : "",
    split ? "org-ehg-qa-glass--split" : "",
  ]
    .filter(Boolean)
    .join(" ");

  const iconChip = (
    <span
      className={
        "org-ehg-qa-glass__icon" +
        (iconShape === "rounded" ? " org-ehg-qa-glass__icon--rounded" : "")
      }
      data-testid="ui-standard-org-ehg-qa-pg-preview-chip"
      data-icon-shape={iconShape}
      aria-hidden="true"
    >
      <Icon className="org-ehg-qa-glass__glyph" strokeWidth={2.25} />
      {badge ? <span className="org-ehg-qa-glass__dot" /> : null}
    </span>
  );

  const leadAction =
    trailing === "action-group"
      ? trailingActionGroup
      : trailing === "action-icon"
        ? trailingActionIcon
        : null;

  const trail =
    trailing === "toggle" ? (
      <button
        type="button"
        className={"org-ehg-qa-glass__switch" + (toggled ? " org-ehg-qa-glass__switch--on" : "")}
        aria-label={toggled ? "On" : "Off"}
        aria-pressed={toggled}
        data-testid="ui-standard-org-ehg-qa-pg-preview-switch"
        onClick={() => setToggled((v) => !v)}
      />
    ) : trailing === "caret" ? (
      <ChevronDown className="org-ehg-qa-glass__trail" aria-hidden strokeWidth={2.25} />
    ) : trailing === "chevron" ? (
      <ChevronRight className="org-ehg-qa-glass__trail" aria-hidden strokeWidth={2.25} />
    ) : null;

  const needsGroupShell =
    split || trailing === "toggle" || trailing === "action-group" || trailing === "action-icon";

  return (
    <section
      className="org-ehg org-ehg-pg-bg org-ehg-glass-playground-card flex min-w-0 flex-col"
      data-testid="ui-standard-card-org-ehg-qa-glass-playground"
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
          <CardTitle>Quick Action glass playground</CardTitle>
          <div className="flex items-center gap-2">
            <Button
              type="button"
              intent={theme === "dark" ? "primary" : "neutral"}
              appearance={theme === "dark" ? "solid" : "outline"}
              shape="pill"
              size="icon"
              aria-label={theme === "dark" ? "Light mode" : "Dark mode"}
              aria-pressed={theme === "dark"}
              data-testid="ui-standard-org-ehg-qa-pg-theme"
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
              data-testid="ui-standard-org-ehg-qa-pg-reset"
              onClick={resetPreview}
            >
              <RotateCcw className="size-4" aria-hidden />
            </Button>
          </div>
        </div>

        <PlaygroundSection testId="ui-standard-org-ehg-qa-glass-playground" className="">
          <div className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3">
            <CompactSelect
              label="Tone"
              value={tone}
              options={[...TONES]}
              onChange={setTone}
              testId="ui-standard-org-ehg-qa-pg-tone"
            />
            <CompactSelect
              label="Trailing"
              value={trailing}
              options={[...TRAILINGS]}
              onChange={onTrailingChange}
              testId="ui-standard-org-ehg-qa-pg-trailing"
            />
            <CompactSelect
              label="Icon"
              value={iconId}
              options={ICONS.map((item) => ({ label: item.label, value: item.value }))}
              onChange={setIconId}
              testId="ui-standard-org-ehg-qa-pg-icon"
            />
            <CompactSelect
              label="Icon shape"
              value={iconShape}
              options={[...ICON_SHAPES]}
              onChange={setIconShape}
              testId="ui-standard-org-ehg-qa-pg-icon-shape"
            />
            <CompactSelect
              label="Size"
              value={size}
              options={[...SIZES]}
              onChange={setSize}
              testId="ui-standard-org-ehg-qa-pg-size"
            />
            <PlaygroundTextField
              label="Label"
              value={label}
              onChange={setLabel}
              testId="ui-standard-org-ehg-qa-pg-label"
            />
            <CompactSelect
              label="Background"
              value={background}
              options={[...ORG_EHG_PLAYGROUND_BG_OPTIONS]}
              onChange={setBackground}
              testId="ui-standard-org-ehg-qa-pg-background"
            />
          </div>

          <div className="col-span-full flex flex-row flex-wrap items-center gap-6">
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={iconOnly}
                onCheckedChange={onIconOnlyChange}
                aria-label="Icon only"
                disabled={trailing === "action-group" || trailing === "action-icon"}
                data-testid="ui-standard-org-ehg-qa-pg-icon-only"
              />
              Icon only
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={iconFilled}
                onCheckedChange={setIconFilled}
                aria-label="Filled icon"
                disabled={!iconOnly || trailing === "action-group" || trailing === "action-icon"}
                data-testid="ui-standard-org-ehg-qa-pg-icon-filled"
              />
              Filled icon
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={badge}
                onCheckedChange={setBadge}
                aria-label="Badge"
                disabled={iconOnly}
                data-testid="ui-standard-org-ehg-qa-pg-badge"
              />
              Badge
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={ghost}
                onCheckedChange={setGhost}
                aria-label="Ghost"
                disabled={iconOnly || split}
                data-testid="ui-standard-org-ehg-qa-pg-ghost"
              />
              Ghost
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={split}
                onCheckedChange={onSplitChange}
                aria-label="Split"
                disabled={
                  iconOnly || trailing === "action-group" || trailing === "action-icon"
                }
                data-testid="ui-standard-org-ehg-qa-pg-split"
              />
              Split
            </label>
          </div>

          <div
            className="flex min-w-0 flex-col gap-3"
            data-testid="ui-standard-org-ehg-qa-pg-preview-card"
          >
            <PlaygroundLabel>Preview</PlaygroundLabel>
            <div
              className="org-ehg-qa-glass-panel org-ehg-qa-glass-panel--playground"
              data-org-ehg-theme={theme}
              data-bg-option={background}
              data-testid="ui-standard-org-ehg-qa-pg-preview"
              data-preview-theme={theme}
            >
              {iconOnly ? (
                <button
                  type="button"
                  className={[
                    "org-ehg-qa-glass-icon",
                    `org-ehg-qa-glass-icon--tone-${tone}`,
                    `org-ehg-qa-glass-icon--size-${size}`,
                    iconShape === "rounded"
                      ? "org-ehg-qa-glass-icon--rounded"
                      : "org-ehg-qa-glass-icon--soft",
                    iconFilled ? "org-ehg-qa-glass-icon--filled" : "",
                  ]
                    .filter(Boolean)
                    .join(" ")}
                  aria-label={displayLabel}
                  data-testid="ui-standard-org-ehg-qa-pg-preview-icon"
                  data-size={size}
                >
                  <Icon className="org-ehg-qa-glass__glyph" aria-hidden strokeWidth={2.25} />
                </button>
              ) : needsGroupShell ? (
                <div
                  className={rowClass}
                  role="group"
                  aria-label={displayLabel}
                  data-testid="ui-standard-org-ehg-qa-pg-preview-btn"
                  data-size={size}
                >
                  {split ? (
                    <>
                      <button type="button" className="org-ehg-qa-glass__split-main">
                        {iconChip}
                        <span className="org-ehg-qa-glass__label">{displayLabel}</span>
                      </button>
                      <span className="org-ehg-qa-glass__split-rule" aria-hidden />
                      <button
                        type="button"
                        className="org-ehg-qa-glass__split-caret"
                        aria-label={`${displayLabel} menu`}
                      >
                        <ChevronDown
                          className="org-ehg-qa-glass__trail"
                          aria-hidden
                          strokeWidth={2.25}
                        />
                      </button>
                    </>
                  ) : leadAction ? (
                    <>
                      {leadAction}
                      <span className="org-ehg-qa-glass__label">{displayLabel}</span>
                    </>
                  ) : (
                    <>
                      {iconChip}
                      <span className="org-ehg-qa-glass__label">{displayLabel}</span>
                      {trail}
                    </>
                  )}
                </div>
              ) : (
                <button
                  type="button"
                  className={rowClass}
                  data-testid="ui-standard-org-ehg-qa-pg-preview-btn"
                  data-size={size}
                >
                  {iconChip}
                  <span className="org-ehg-qa-glass__label">{displayLabel}</span>
                  {trail}
                </button>
              )}
            </div>
          </div>

          <UiStandardsCopyCommand
            standard="Org Quick Action Glass"
            standardStatus="pilot"
            command={qaGlassCommand(
              tone,
              trailing,
              iconShape,
              size,
              iconOnly,
              badge,
              ghost,
              split,
            )}
            testId="ui-standard-org-ehg-qa-pg-copy"
          />
        </PlaygroundSection>
      </div>
    </section>
  );
}
