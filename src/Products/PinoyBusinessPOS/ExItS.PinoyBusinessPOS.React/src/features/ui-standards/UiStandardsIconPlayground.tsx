import { useMemo, useState } from "react";
import { ChevronRight } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Switch } from "@/components/ui/switch";
import { DROPDOWN_STANDARD_COLOR, UiStandardsQdropdown } from "@/features/ui-standards/UiStandardsDropdownGallery";
import { CompactSelect, PlaygroundPreviewCard } from "@/features/ui-standards/UiStandardsPlaygrounds";
import { PlaygroundLabel, PlaygroundSection } from "@/features/ui-standards/UiStandardsSnippetBlock";
import { UiStandardsCopyCommand } from "@/features/ui-standards/UiStandardsCopyCommand";
import {
  ICON_CHIP_GLYPH_CATALOG,
  ICON_CHIP_DEFAULT_GLYPH_ID,
} from "@/features/ui-standards/icon-chip-glyphs";
import {
  ICON_CHIP_APPEARANCE_CATALOG,
  ICON_CHIP_DEFAULTS,
  ICON_CHIP_DEFAULT_PREVIEW_HOST,
  ICON_CHIP_PREVIEW_HOST_CATALOG,
  ICON_CHIP_STROKE_OPTIONS,
  iconChipCursorCommand,
  iconChipGlyphSizeToken,
  iconChipPreviewHostTestId,
  iconChipSizeToken,
  type IconChipAppearance,
  type IconChipBorderWidth,
  type IconChipMarkStyle,
  type IconChipPadding,
  type IconChipShape,
  type IconChipSize,
  type IconChipStroke,
  type IconChipPreviewHost,
  type IconChipTone,
} from "@/features/ui-standards/icon-chip-global-default";
import { ORG_EHG_QA_GLASS_CLASS_NAME } from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-qa-glass-global-default";
import {
  ExitsOrgEhgButton,
  type ExitsOrgEhgButtonIntent,
} from "@/components/exits/ExitsOrgEhgButton";
import { cn } from "@/lib/cn";

const SHAPES: ReadonlyArray<{ id: IconChipShape; label: string }> = [
  { id: "squircle", label: "Squircle" },
  { id: "disc", label: "Disc" },
  { id: "rounded", label: "Rounded" },
  { id: "square", label: "Square" },
];

const SIZES: ReadonlyArray<{ id: IconChipSize; label: string }> = [
  { id: "xs", label: "Xs" },
  { id: "sm", label: "Sm" },
  { id: "md", label: "Md" },
  { id: "lg", label: "Lg" },
  { id: "xl", label: "Xl" },
];

const TONES: ReadonlyArray<{ id: IconChipTone; label: string }> = [
  { id: "primary", label: "Primary" },
  { id: "success", label: "Success" },
  { id: "warn", label: "Warn" },
  { id: "danger", label: "Danger" },
  { id: "info", label: "Info" },
  { id: "muted", label: "Muted" },
];

const PADDINGS: ReadonlyArray<{ id: IconChipPadding; label: string }> = [
  { id: "tight", label: "Tight" },
  { id: "default", label: "Default" },
  { id: "loose", label: "Loose" },
];

const BORDER_WIDTHS: ReadonlyArray<{ id: IconChipBorderWidth; label: string }> = [
  { id: "thin", label: "Thin" },
  { id: "medium", label: "Medium" },
  { id: "thick", label: "Thick" },
];

const MARK_STYLES: ReadonlyArray<{ id: IconChipMarkStyle; label: string }> = [
  { id: "stroke", label: "Stroke" },
  { id: "filled", label: "Filled" },
];

const STROKE_SELECT_OPTIONS = ICON_CHIP_STROKE_OPTIONS.map((value) => ({
  label: String(value),
  value: String(value),
}));

function glyphSizeClass(size: IconChipSize): string {
  return `ui-standards-icon-chip__glyph--${size}`;
}

function chipHasChrome(appearance: IconChipAppearance): boolean {
  return appearance !== "bare";
}

function iconChipToneToQaIdentity(tone: IconChipTone): string {
  if (tone === "success") return "lent";
  if (tone === "warn") return "start";
  if (tone === "danger") return "stores";
  if (tone === "info") return "info";
  if (tone === "muted") return "info";
  return "primary";
}

function iconChipToneToButtonIntent(tone: IconChipTone): ExitsOrgEhgButtonIntent {
  if (tone === "success") return "success";
  if (tone === "warn") return "warn";
  if (tone === "danger") return "danger";
  if (tone === "info") return "info";
  if (tone === "muted") return "secondary";
  return "primary";
}

export function UiStandardsIconPlayground() {
  const [appearance, setAppearance] = useState(ICON_CHIP_DEFAULTS.appearance);
  const [shape, setShape] = useState(ICON_CHIP_DEFAULTS.shape);
  const [size, setSize] = useState(ICON_CHIP_DEFAULTS.size);
  const [iconSize, setIconSize] = useState(ICON_CHIP_DEFAULTS.iconSize);
  const [tone, setTone] = useState(ICON_CHIP_DEFAULTS.tone);
  const [stroke, setStroke] = useState(ICON_CHIP_DEFAULTS.stroke);
  const [glyphId, setGlyphId] = useState(ICON_CHIP_DEFAULT_GLYPH_ID);
  const [padding, setPadding] = useState(ICON_CHIP_DEFAULTS.padding);
  const [borderWidth, setBorderWidth] = useState(ICON_CHIP_DEFAULTS.borderWidth);
  const [shadow, setShadow] = useState(ICON_CHIP_DEFAULTS.shadow);
  const [markStyle, setMarkStyle] = useState(ICON_CHIP_DEFAULTS.markStyle);
  const [previewHost, setPreviewHost] = useState<IconChipPreviewHost>(ICON_CHIP_DEFAULT_PREVIEW_HOST);

  const previewHostLabel = useMemo(
    () => ICON_CHIP_PREVIEW_HOST_CATALOG.find((entry) => entry.id === previewHost)?.label ?? "Sample",
    [previewHost],
  );

  const glyph = useMemo(
    () => ICON_CHIP_GLYPH_CATALOG.find((entry) => entry.id === glyphId) ?? ICON_CHIP_GLYPH_CATALOG[0],
    [glyphId],
  );
  const Icon = glyph.icon;

  const command = iconChipCursorCommand({
    appearance,
    shape,
    size,
    iconSize,
    tone,
    stroke,
    glyphId,
    padding,
    borderWidth,
    shadow,
    markStyle,
  });

  const showChrome = chipHasChrome(appearance);
  const borderControlsEnabled =
    appearance === "outline" ||
    appearance === "ghost" ||
    appearance === "outline-ehg" ||
    appearance === "ghost-ehg";

  function resetPreview() {
    setAppearance(ICON_CHIP_DEFAULTS.appearance);
    setShape(ICON_CHIP_DEFAULTS.shape);
    setSize(ICON_CHIP_DEFAULTS.size);
    setIconSize(ICON_CHIP_DEFAULTS.iconSize);
    setTone(ICON_CHIP_DEFAULTS.tone);
    setStroke(ICON_CHIP_DEFAULTS.stroke);
    setGlyphId(ICON_CHIP_DEFAULT_GLYPH_ID);
    setPadding(ICON_CHIP_DEFAULTS.padding);
    setBorderWidth(ICON_CHIP_DEFAULTS.borderWidth);
    setShadow(ICON_CHIP_DEFAULTS.shadow);
    setMarkStyle(ICON_CHIP_DEFAULTS.markStyle);
    setPreviewHost(ICON_CHIP_DEFAULT_PREVIEW_HOST);
  }

  function renderChip(
    chipAppearance: IconChipAppearance,
    testId: string,
    options?: {
      chipShape?: IconChipShape;
      chipSize?: IconChipSize;
      markSize?: IconChipSize;
      snapshot?: Partial<typeof ICON_CHIP_DEFAULTS>;
    },
  ) {
    const chipShape = options?.chipShape ?? shape;
    const chipSize = options?.chipSize ?? size;
    const markSize = options?.markSize ?? iconSize;
    const snap = { ...ICON_CHIP_DEFAULTS, tone, stroke, padding, borderWidth, shadow, markStyle, ...options?.snapshot };
    const withChrome = chipHasChrome(chipAppearance);
    return (
      <span
        className={cn(
          "ui-standards-icon-chip",
          `ui-standards-icon-chip--${chipAppearance}`,
          withChrome && `ui-standards-icon-chip--${chipShape}`,
          withChrome && `ui-standards-icon-chip--${chipSize}`,
          withChrome && snap.padding !== "default" && `ui-standards-icon-chip--padding-${snap.padding}`,
          withChrome && snap.borderWidth !== "thin" && `ui-standards-icon-chip--border-${snap.borderWidth}`,
          withChrome && !snap.shadow && "ui-standards-icon-chip--no-shadow",
        )}
        data-testid={testId}
        data-appearance={chipAppearance}
        data-tone={snap.tone}
        data-shape={withChrome ? chipShape : "none"}
        data-size={chipSize}
        data-icon-size={markSize}
      >
        <Icon
          className={cn(
            "ui-standards-icon-chip__glyph",
            glyphSizeClass(markSize),
            snap.markStyle === "filled" && "ui-standards-icon-chip__glyph--filled",
          )}
          strokeWidth={snap.markStyle === "filled" ? 0 : snap.stroke}
          fill={snap.markStyle === "filled" ? "currentColor" : "none"}
          aria-hidden
        />
      </span>
    );
  }

  function renderPreviewHost(host: IconChipPreviewHost) {
    if (host === "action") {
      return (
        <button
          type="button"
          className={cn(
            "personal-qa-glass",
            `personal-qa-glass--${iconChipToneToQaIdentity(tone)}`,
            ORG_EHG_QA_GLASS_CLASS_NAME,
            "ui-standards-icon-chip-qa-preview",
          )}
          data-shell="org-ehg"
          data-identity={iconChipToneToQaIdentity(tone)}
          data-testid="ui-standards-icon-chip-qa-preview"
        >
          <span
            className="personal-qa-glass__icon personal-qa-glass__icon--soft ui-standards-icon-chip-qa-preview__icon"
            aria-hidden
          >
            {renderChip(appearance, "ui-standards-icon-chip-qa-slot")}
          </span>
          <span className="personal-qa-glass__label">Quick action</span>
          <ChevronRight className="personal-qa-glass__chevron" aria-hidden />
        </button>
      );
    }

    if (host === "button") {
      return (
        <ExitsOrgEhgButton
          type="button"
          intent={iconChipToneToButtonIntent(tone)}
          className="ui-standards-icon-chip-btn-preview"
          testId="ui-standards-icon-chip-btn-preview"
        >
          <span
            className="org-ehg-btn-glass__icon ui-standards-icon-chip-btn-preview__icon"
            aria-hidden
          >
            {renderChip(appearance, "ui-standards-icon-chip-btn-slot")}
          </span>
          <span>Save changes</span>
        </ExitsOrgEhgButton>
      );
    }

    if (host === "card") {
      return (
        <article className="ui-standards-icon-chip-sample-card" data-tone={tone}>
          <div className="ui-standards-icon-chip-sample-card__ribbon">
            <span className="ui-standards-icon-chip-sample-card__pennant" aria-hidden>
              {renderChip(appearance, "ui-standards-icon-chip-sample-pennant")}
            </span>
            <span className="ui-standards-icon-chip-sample-card__title">Activity summary</span>
          </div>
          <p className="ui-standards-icon-chip-sample-card__body">
            Soft Glass ribbon card — pennant uses the live icon recipe.
          </p>
        </article>
      );
    }

    if (host === "kpi") {
      return (
        <article className="ui-standards-icon-chip-kpi-preview" data-tone={tone}>
          <span className="ui-standards-icon-chip-kpi-preview__icon" aria-hidden>
            {renderChip(appearance, "ui-standards-icon-chip-kpi-slot")}
          </span>
          <div className="ui-standards-icon-chip-kpi-preview__text">
            <p className="ui-standards-icon-chip-kpi-preview__label">Open balance</p>
            <p className="ui-standards-icon-chip-kpi-preview__value">₱12,480</p>
            <p className="ui-standards-icon-chip-kpi-preview__meta">+4.2% vs last week</p>
          </div>
        </article>
      );
    }

    if (host === "pennant") {
      return (
        <div className="ui-standards-icon-chip-pennant-preview" data-tone={tone}>
          <span className="ui-standards-icon-chip-pennant-preview__chip" aria-hidden>
            {renderChip(appearance, "ui-standards-icon-chip-pennant-slot")}
          </span>
          <span className="ui-standards-icon-chip-pennant-preview__title">Activity summary</span>
        </div>
      );
    }

    if (host === "list-row") {
      return (
        <div className="ui-standards-icon-chip-list-row" data-tone={tone}>
          <span className="ui-standards-icon-chip-list-row__icon" aria-hidden>
            {renderChip(appearance, "ui-standards-icon-chip-list-slot")}
          </span>
          <div className="ui-standards-icon-chip-list-row__body">
            <span className="ui-standards-icon-chip-list-row__title">North hub store</span>
            <span className="ui-standards-icon-chip-list-row__meta">Updated 2h ago · 3 pending</span>
          </div>
          <ChevronRight className="ui-standards-icon-chip-list-row__chevron" aria-hidden />
        </div>
      );
    }

    return null;
  }

  return (
    <PlaygroundSection testId="ui-standards-icon-playground" className="">
      <div className="ui-standards-icon-chip-controls">
        <UiStandardsQdropdown
          label="Lucide mark"
          color={DROPDOWN_STANDARD_COLOR}
          value={glyph.label}
          options={ICON_CHIP_GLYPH_CATALOG.map((entry) => entry.label)}
          standard
          searchable
          onSelect={(nextLabel) => {
            const match = ICON_CHIP_GLYPH_CATALOG.find((entry) => entry.label === nextLabel);
            if (match) setGlyphId(match.id);
          }}
          testId="ui-standards-icon-glyph"
        />

        <CompactSelect
          label="Appearance"
          value={appearance}
          options={ICON_CHIP_APPEARANCE_CATALOG.map((entry) => ({ label: entry.label, value: entry.id }))}
          onChange={setAppearance}
          testId="ui-standards-icon-appearance"
        />

        <CompactSelect
          label="Shape"
          value={shape}
          options={SHAPES.map((entry) => ({ label: entry.label, value: entry.id }))}
          onChange={setShape}
          disabled={!showChrome}
          testId="ui-standards-icon-shape"
        />

        <CompactSelect
          label="Chip size"
          value={size}
          options={SIZES.map((entry) => ({ label: entry.label, value: entry.id }))}
          onChange={setSize}
          testId="ui-standards-icon-chip-size"
        />

        <CompactSelect
          label="Icon size"
          value={iconSize}
          options={SIZES.map((entry) => ({ label: entry.label, value: entry.id }))}
          onChange={setIconSize}
          testId="ui-standards-icon-icon-size"
        />

        <CompactSelect
          label="Tone"
          value={tone}
          options={TONES.map((entry) => ({ label: entry.label, value: entry.id }))}
          onChange={setTone}
          testId="ui-standards-icon-tone"
        />

        <CompactSelect
          label="Sample"
          value={previewHost}
          options={ICON_CHIP_PREVIEW_HOST_CATALOG.map((entry) => ({ label: entry.label, value: entry.id }))}
          onChange={setPreviewHost}
          testId="ui-standards-icon-preview-host"
        />

        <CompactSelect
          label="Lucide stroke"
          value={String(stroke)}
          options={STROKE_SELECT_OPTIONS}
          onChange={(next) => setStroke(Number(next) as IconChipStroke)}
          disabled={markStyle === "filled"}
          testId="ui-standards-icon-stroke"
        />

        <div>
          <PlaygroundLabel>Mark style</PlaygroundLabel>
          <div className="ui-standards-icon-chip-controls__row" role="group" aria-label="Mark style">
            {MARK_STYLES.map((entry) => (
              <Button
                key={entry.id}
                type="button"
                size="sm"
                intent={markStyle === entry.id ? "primary" : "neutral"}
                appearance={markStyle === entry.id ? "solid" : "outline"}
                data-testid={`ui-standards-icon-mark-${entry.id}`}
                onClick={() => setMarkStyle(entry.id)}
              >
                {entry.label}
              </Button>
            ))}
          </div>
        </div>

        <div>
          <PlaygroundLabel>Chip padding</PlaygroundLabel>
          <div className="ui-standards-icon-chip-controls__row" role="group" aria-label="Chip padding">
            {PADDINGS.map((entry) => (
              <Button
                key={entry.id}
                type="button"
                size="sm"
                intent={padding === entry.id ? "primary" : "neutral"}
                appearance={padding === entry.id ? "solid" : "outline"}
                disabled={!showChrome}
                data-testid={`ui-standards-icon-padding-${entry.id}`}
                onClick={() => setPadding(entry.id)}
              >
                {entry.label}
              </Button>
            ))}
          </div>
        </div>

        <div>
          <PlaygroundLabel>Border width</PlaygroundLabel>
          <div className="ui-standards-icon-chip-controls__row" role="group" aria-label="Border width">
            {BORDER_WIDTHS.map((entry) => (
              <Button
                key={entry.id}
                type="button"
                size="sm"
                intent={borderWidth === entry.id ? "primary" : "neutral"}
                appearance={borderWidth === entry.id ? "solid" : "outline"}
                disabled={!borderControlsEnabled}
                data-testid={`ui-standards-icon-border-${entry.id}`}
                onClick={() => setBorderWidth(entry.id)}
              >
                {entry.label}
              </Button>
            ))}
          </div>
        </div>

        <h3 className="sr-only">Icon shadow</h3>
        <label className="ui-standards-icon-chip-controls__switch">
          <span className="text-sm font-medium text-foreground">Shadow</span>
          <Switch
            checked={shadow}
            onCheckedChange={setShadow}
            disabled={!showChrome}
            data-testid="ui-standards-icon-shadow"
            aria-label="Icon chip shadow"
          />
        </label>
      </div>

      <div className="ui-standards-icon-chip-tokens" data-testid="ui-standards-icon-tokens">
        <span data-testid="ui-standards-icon-token-chip-size">chip: {iconChipSizeToken(size)}</span>
        <span data-testid="ui-standards-icon-token-glyph-size">glyph: {iconChipGlyphSizeToken(iconSize)}</span>
        <span data-testid="ui-standards-icon-token-stroke">stroke: {stroke}</span>
        <span data-testid="ui-standards-icon-token-tone">tone: {tone}</span>
      </div>

      <PlaygroundPreviewCard
        previewTestId="ui-standards-icon-preview"
        themeTestId="ui-standards-icon-preview-theme"
        resetTestId="ui-standards-icon-preview-reset"
        onReset={resetPreview}
        previewClassName="ui-standards-icon-chip-stage"
      >
        <div
          className="ui-standards-icon-chip-stage__lane"
          data-testid="ui-standards-icon-preview-only"
        >
          <span className="ui-standards-icon-chip-stage__lane-label">Icon only</span>
          <div className="ui-standards-icon-chip-stage__only">
            {renderChip(appearance, "ui-standards-icon-chip-live")}
          </div>
        </div>
        <div
          className="ui-standards-icon-chip-stage__lane ui-standards-icon-chip-stage__context"
          data-testid="ui-standards-icon-preview-context"
        >
          <span className="ui-standards-icon-chip-stage__lane-label">{previewHostLabel}</span>
          <div
            className="ui-standards-icon-chip-stage__context-host"
            data-testid={iconChipPreviewHostTestId(previewHost)}
            data-preview-host={previewHost}
          >
            {renderPreviewHost(previewHost)}
          </div>
        </div>
      </PlaygroundPreviewCard>

      <PlaygroundLabel>All appearances (md · squircle · primary)</PlaygroundLabel>
      <div className="ui-standards-icon-chip-variants" data-testid="ui-standards-icon-variant-row">
        {ICON_CHIP_APPEARANCE_CATALOG.map((entry) => (
          <div key={entry.id} className="ui-standards-icon-chip-variant">
            {renderChip(entry.id, `ui-standards-icon-variant-${entry.id}`, {
              chipShape: "squircle",
              chipSize: "md",
              snapshot: ICON_CHIP_DEFAULTS,
            })}
            <span className="ui-standards-icon-chip-variant__label">{entry.label}</span>
          </div>
        ))}
      </div>

      <UiStandardsCopyCommand
        standard="Icon"
        command={command}
        context="Lucide only — tones use severity tokens (warn #ff5722). Generic Soft / Soft Glass track vs Org EHG–suffixed icon chrome (card wash, QA icon-soft-fill, card-soft)."
      />
    </PlaygroundSection>
  );
}