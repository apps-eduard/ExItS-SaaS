import { useState, type CSSProperties, type ReactNode } from "react";
import { usePreferences } from "@/hooks/usePreferences";
import {
  Bell,
  Bookmark,
  Briefcase,
  Calendar,
  Check,
  Crosshair,
  Heart,
  Info,
  Lightbulb,
  Megaphone,
  Share2,
  Star,
  Trash2,
  TriangleAlert,
} from "lucide-react";
import { ExitsTabs } from "@/components/exits/ExitsTabs";
import { INPUT_SAMPLES, inputAccent } from "@/features/ui-standards/UiStandardsInputGallery";
import { UiStandardsQslider } from "@/features/ui-standards/UiStandardsSliderGallery";
import { RADIO_STANDARD_COLOR } from "@/features/ui-standards/UiStandardsRadioGallery";
import { UiStandardsQcheckbox } from "@/features/ui-standards/UiStandardsCheckboxGallery";
import { CompactSelect, PlaygroundPreviewCard, playgroundIntentColor } from "@/features/ui-standards/UiStandardsPlaygrounds";
import { Switch } from "@/components/ui/switch";
import { PlaygroundSection } from "@/features/ui-standards/UiStandardsSnippetBlock";
import { UiStandardsCopyCommand } from "@/features/ui-standards/UiStandardsCopyCommand";

/**
 * Quasar-style card playground. Card, Header, Body, and Footer tabs adjust one preview card.
 * Showcase only — not a product card.
 * @see https://quasar.dev/vue-components/card#example--basic--basic-cards
 */
const CARD_LOREM =
  "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.";

const INTENT_OPTIONS = INPUT_SAMPLES.map((item) => ({ label: item.label, value: item.id }));

type CardHeaderKind = "none" | "title" | "subtitle";
type CardBodyKind = "none" | "text" | "sections";
type CardFooterKind = "none" | "actions" | "caption";
type CardActionAlign = "start" | "center" | "end";
type CardTitleAlign = "start" | "center" | "right";
type CardFill = "solid" | "gradient" | "outline";
type RadiusCorner = "lt" | "lb" | "rt" | "rb";
type RadiusCorners = Record<RadiusCorner, boolean>;
type BorderSide = "l" | "r" | "t" | "b";
type BorderSides = Record<BorderSide, boolean>;
type CardGradientDirection =
  | "to-bottom"
  | "to-top"
  | "to-right"
  | "to-left"
  | "to-bottom-right"
  | "to-bottom-left"
  | "to-top-right"
  | "to-top-left"
  | "center";

const HEADER_OPTIONS: ReadonlyArray<{ label: string; value: CardHeaderKind }> = [
  { label: "None", value: "none" },
  { label: "Title", value: "title" },
  { label: "Title and subtitle", value: "subtitle" },
];

const BODY_OPTIONS: ReadonlyArray<{ label: string; value: CardBodyKind }> = [
  { label: "None", value: "none" },
  { label: "Text", value: "text" },
  { label: "Two paragraphs", value: "sections" },
];

const FOOTER_OPTIONS: ReadonlyArray<{ label: string; value: CardFooterKind }> = [
  { label: "None", value: "none" },
  { label: "Actions", value: "actions" },
  { label: "Caption", value: "caption" },
];

const ALIGN_OPTIONS: ReadonlyArray<{ label: string; value: CardActionAlign }> = [
  { label: "Start", value: "start" },
  { label: "Center", value: "center" },
  { label: "End", value: "end" },
];

const BUTTON_APPEARANCE_OPTIONS = [
  { label: "Standard", value: "standard" },
  { label: "Soft", value: "soft" },
  { label: "Soft outline", value: "soft-outline" },
  { label: "Outline", value: "outline" },
  { label: "Raised", value: "raised" },
  { label: "Ghost", value: "ghost" },
  { label: "Glossy", value: "glossy" },
  { label: "Gradient", value: "gradient" },
  { label: "Rounded", value: "rounded" },
  { label: "Flat", value: "flat" },
] as const;

const BUTTON_DENSITY_OPTIONS = [
  { label: "Compact", value: "compact" },
  { label: "Standard", value: "balance" },
  { label: "Comfort", value: "comfort" },
] as const;

type ButtonAppearance = (typeof BUTTON_APPEARANCE_OPTIONS)[number]["value"];
type ButtonDensity = (typeof BUTTON_DENSITY_OPTIONS)[number]["value"];

const TITLE_ALIGN_OPTIONS: ReadonlyArray<{ label: string; value: CardTitleAlign }> = [
  { label: "Start", value: "start" },
  { label: "Center", value: "center" },
  { label: "Right", value: "right" },
];

const FILL_OPTIONS: ReadonlyArray<{ label: string; value: CardFill }> = [
  { label: "Solid", value: "solid" },
  { label: "Gradient", value: "gradient" },
  { label: "Outline", value: "outline" },
];

const DEFAULT_ELEVATION = 3;
const DEFAULT_RADIUS = 1;
const DEFAULT_BORDER = 1;
const DEFAULT_HEIGHT = 3;
const DEFAULT_WIDTH = 3;

const BORDER_SIDE_OPTIONS: ReadonlyArray<{ id: BorderSide; label: string; title: string }> = [
  { id: "l", label: "L", title: "Left" },
  { id: "r", label: "R", title: "Right" },
  { id: "t", label: "T", title: "Top" },
  { id: "b", label: "B", title: "Bottom" },
];

const NO_BORDER_SIDES: BorderSides = { l: false, r: false, t: false, b: false };

/** 1 is flat. 3 matches the current card shadow. 5 is the highest raise. */
const ELEVATION_SHADOWS = [
  "none",
  "0 1px 3px rgba(0, 0, 0, 0.2), 0 1px 1px rgba(0, 0, 0, 0.14), 0 2px 1px -1px rgba(0, 0, 0, 0.12)",
  "0 1px 5px rgba(0, 0, 0, 0.2), 0 2px 2px rgba(0, 0, 0, 0.14), 0 3px 1px -2px rgba(0, 0, 0, 0.12)",
  "0 2px 4px -1px rgba(0, 0, 0, 0.2), 0 4px 5px rgba(0, 0, 0, 0.14), 0 1px 10px rgba(0, 0, 0, 0.12)",
  "0 3px 5px -1px rgba(0, 0, 0, 0.2), 0 6px 10px rgba(0, 0, 0, 0.14), 0 1px 18px rgba(0, 0, 0, 0.12)",
] as const;

/** 1 is the current 4px corner. Each step adds a little. 10 is the largest. */
const RADIUS_SIZES = ["4px", "6px", "8px", "10px", "12px", "14px", "16px", "18px", "20px", "24px"] as const;
const RADIUS_MAX = 10;

const RADIUS_CORNER_OPTIONS: ReadonlyArray<{ id: RadiusCorner; label: string; title: string }> = [
  { id: "lt", label: "LT", title: "Left top" },
  { id: "lb", label: "LB", title: "Left bottom" },
  { id: "rt", label: "RT", title: "Right top" },
  { id: "rb", label: "RB", title: "Right bottom" },
];

const ALL_RADIUS_CORNERS: RadiusCorners = { lt: true, lb: true, rt: true, rb: true };

/** 3 is the current header. 1 and 2 are shorter. 4 and 5 are taller. */
const HEIGHT_PAD = ["0.125rem", "0.5rem", "1rem", "1rem", "1.15rem"] as const;
const HEIGHT_MIN = ["auto", "auto", "auto", "4.75rem", "7rem"] as const;

/** 3 matches the card. 1 and 2 pull both sides in. 4 and 5 push both sides out. */
const WIDTH_INSET = ["1.5rem", "0.75rem", "0px", "-0.75rem", "-1.5rem"] as const;

/** 1 is square. Each step adds a little. 10 is the largest header corner. Pill overrides this. */
const HEADER_RADIUS_SIZES = ["0px", "2px", "4px", "6px", "8px", "10px", "12px", "14px", "18px", "24px"] as const;

/** 4 matches the current title. 1 is H1. 5 is H5. */
const DEFAULT_FONT = 4;
const FONT_SIZES = ["2rem", "1.75rem", "1.5rem", "1.25rem", "1rem"] as const;

/** 1 sits inside. 3 floats on the top edge. 5 is fully outside the card. */
const HEADER_SHIFTS = ["0%", "-25%", "-50%", "-75%", "-100%"] as const;

function stepCss(steps: readonly string[], value: number): string {
  return steps[Math.min(steps.length, Math.max(1, value)) - 1] ?? steps[0];
}

function anyBorder(sides: BorderSides): boolean {
  return BORDER_SIDE_OPTIONS.some((side) => sides[side.id]);
}

function borderWidthCss(sides: BorderSides, size: number): string {
  if (!anyBorder(sides)) return "0";
  const width = `${size}px`;
  const top = sides.t ? width : "0";
  const right = sides.r ? width : "0";
  const bottom = sides.b ? width : "0";
  const left = sides.l ? width : "0";
  if (sides.t && sides.r && sides.b && sides.l) return width;
  return `${top} ${right} ${bottom} ${left}`;
}

function selectedSides(sides: BorderSides): string {
  return BORDER_SIDE_OPTIONS.filter((side) => sides[side.id])
    .map((side) => side.id)
    .join(",");
}

function selectedCorners(corners: RadiusCorners): string {
  return RADIUS_CORNER_OPTIONS.filter((corner) => corners[corner.id])
    .map((corner) => corner.id)
    .join(",");
}

function cornerRadiusCss(size: string, corners: RadiusCorners): string {
  const everyCorner = RADIUS_CORNER_OPTIONS.every((corner) => corners[corner.id]);
  if (everyCorner) return size;
  const pick = (on: boolean) => (on ? size : "0");
  return `${pick(corners.lt)} ${pick(corners.rt)} ${pick(corners.rb)} ${pick(corners.lb)}`;
}

function radiusCommand(size: number, corners: RadiusCorners, prefix: string): string | null {
  const selected = RADIUS_CORNER_OPTIONS.filter((corner) => corners[corner.id]).map((corner) => corner.id);
  if (selected.length === 4) return size === DEFAULT_RADIUS ? null : `${prefix}radius-${size}`;
  if (selected.length === 0) return `${prefix}radius-none`;
  const names = selected.join("-");
  return size === DEFAULT_RADIUS ? `${prefix}radius-${names}` : `${prefix}radius-${names}-${size}`;
}

function borderCommand(sides: BorderSides, size: number): string | null {
  const selected = BORDER_SIDE_OPTIONS.filter((side) => sides[side.id]).map((side) => side.id);
  if (selected.length === 0) return null;
  if (selected.length === 4) return size === DEFAULT_BORDER ? "border" : `border-${size}`;
  const names = selected.join("-");
  return size === DEFAULT_BORDER ? `border-${names}` : `border-${names}-${size}`;
}

const GRADIENT_OPTIONS: ReadonlyArray<{ label: string; value: CardGradientDirection; css: string }> = [
  { label: "Top to bottom", value: "to-bottom", css: "to bottom" },
  { label: "Bottom to top", value: "to-top", css: "to top" },
  { label: "Left to right", value: "to-right", css: "to right" },
  { label: "Right to left", value: "to-left", css: "to left" },
  { label: "Top left to bottom right", value: "to-bottom-right", css: "to bottom right" },
  { label: "Top right to bottom left", value: "to-bottom-left", css: "to bottom left" },
  { label: "Bottom left to top right", value: "to-top-right", css: "to top right" },
  { label: "Bottom right to top left", value: "to-top-left", css: "to top left" },
  { label: "Center", value: "center", css: "circle" },
];

function cardCursorCommand(
  intent: string,
  headerIntent: string,
  header: CardHeaderKind,
  body: CardBodyKind,
  footer: CardFooterKind,
  align: CardActionAlign,
  buttonAppearance: ButtonAppearance,
  buttonDensity: ButtonDensity,
  iconOnly: boolean,
  vertical: boolean,
  footerIntent: string,
  fill: CardFill,
  gradientDirection: CardGradientDirection,
  elevation: number,
  radius: number,
  radiusCorners: RadiusCorners,
  borderSides: BorderSides,
  borderSize: number,
  titleAlign: CardTitleAlign,
  fontSize: number,
  height: number,
  width: number,
  headerPill: boolean,
  headerElevation: number,
  headerRadius: number,
  headerRadiusCorners: RadiusCorners,
  headerBorderSides: BorderSides,
  headerBorderSize: number,
  headerAppearance: ButtonAppearance,
  headerPosition: number,
): string {
  const parts = ["ext card"];
  if (intent !== "standard") parts.push(intent);
  if (fill === "gradient") parts.push("gradient");
  if (fill === "outline") parts.push("outline");
  if (fill === "gradient" && gradientDirection !== "to-bottom") parts.push(gradientDirection);
  if (elevation === 1) parts.push("flat");
  else if (elevation !== DEFAULT_ELEVATION) parts.push(`raised-${elevation}`);
  const radiusToken = radiusCommand(radius, radiusCorners, "");
  if (radiusToken) parts.push(radiusToken);
  const border = borderCommand(borderSides, borderSize);
  if (border) parts.push(border);
  if (header === "none") parts.push("no-header");
  if (header === "subtitle") parts.push("subtitle");
  if (header !== "none" && headerIntent !== "standard") parts.push(`header-${headerIntent}`);
  if (header !== "none" && headerAppearance !== "standard") parts.push(`header-${headerAppearance}`);
  if (header !== "none" && titleAlign !== "start") parts.push(`title-${titleAlign}`);
  if (header !== "none" && fontSize !== DEFAULT_FONT) parts.push(`h${fontSize}`);
  if (header !== "none" && headerPill) parts.push("header-pill");
  if (header !== "none" && headerPosition !== 1) parts.push(`header-position-${headerPosition}`);
  if (header !== "none" && headerElevation !== 1) parts.push(`header-raised-${headerElevation}`);
  if (header !== "none" && headerPill && headerRadius !== DEFAULT_RADIUS) parts.push(`header-radius-${headerRadius}`);
  if (header !== "none" && !headerPill) {
    const headerRadiusToken = radiusCommand(headerRadius, headerRadiusCorners, "header-");
    if (headerRadiusToken) parts.push(headerRadiusToken);
  }
  if (header !== "none") {
    const headerBorder = borderCommand(headerBorderSides, headerBorderSize);
    if (headerBorder) parts.push(`header-${headerBorder}`);
  }
  if (header !== "none" && height !== DEFAULT_HEIGHT) parts.push(`height-${height}`);
  if (header !== "none" && width !== DEFAULT_WIDTH) parts.push(`width-${width}`);
  if (body === "none") parts.push("no-body");
  if (body === "sections") parts.push("sections");
  if (footer === "caption") parts.push("caption");
  else if (footer === "actions" && vertical) parts.push("icons-right");
  else if (footer === "actions" && iconOnly) parts.push("icons");
  else if (footer === "actions") parts.push("actions");
  if (footer === "actions" && align !== "start") parts.push(align);
  if (footer === "actions" && buttonAppearance !== "standard") parts.push(`btn-${buttonAppearance}`);
  if (footer === "actions" && buttonDensity !== "balance") parts.push(`btn-${buttonDensity}`);
  if (footer !== "none" && footerIntent !== "standard") parts.push(`footer-${footerIntent}`);
  return parts.join(" ");
}

function sampleById(id: string) {
  return INPUT_SAMPLES.find((item) => item.id === id) ?? INPUT_SAMPLES[0];
}

/** Outline uses the intent hue. Standard stays black so it stays visible on a light surface. */
function intentPaint(sample: { id: string; bg: string }) {
  return playgroundIntentColor(sample.id, sample.bg);
}

function outlineInk(sample: { id: string; bg: string; fg: string }) {
  return sample.id === "standard" ? sample.fg : intentPaint(sample);
}

function buttonAppearanceClass(appearance: ButtonAppearance): string {
  if (appearance === "standard") return "";
  return `ui-standards-qbtn ui-standards-qbtn--playground ui-standards-qbtn--${appearance}`;
}

function buttonAppearanceStyle(
  appearance: ButtonAppearance,
  sample: { id: string; bg: string; fg: string },
): CSSProperties | undefined {
  if (appearance === "standard") return undefined;
  const accent = playgroundIntentColor(sample.id, sample.bg.toLowerCase() === "#ffffff" ? "#000000" : sample.bg);
  if (appearance === "outline" || appearance === "ghost") {
    return {
      color: accent,
      borderColor: appearance === "outline" ? accent : "transparent",
      backgroundColor: "transparent",
    };
  }
  if (appearance === "soft" || appearance === "soft-outline") {
    return {
      color: accent,
      borderColor: appearance === "soft-outline" ? accent : undefined,
      ["--qbtn-bg" as string]: accent,
    };
  }
  const fill = intentPaint(sample);
  const style: CSSProperties & { "--qbtn-bg"?: string } = { backgroundColor: fill, color: sample.fg };
  if (appearance === "gradient") style["--qbtn-bg"] = fill;
  return style;
}

function CardIconActions({ className, style }: { className: string; style?: CSSProperties }) {
  return (
    <>
      <button type="button" className={className || undefined} style={style} aria-label="Event">
        <Calendar aria-hidden />
      </button>
      <button type="button" className={className || undefined} style={style} aria-label="Share">
        <Share2 aria-hidden />
      </button>
    </>
  );
}

function SettingSelect<T extends string>({
  label,
  value,
  options,
  onChange,
  testId,
  disabled = false,
}: {
  label: string;
  value: T;
  options: ReadonlyArray<{ label: string; value: T }>;
  onChange: (value: T) => void;
  testId: string;
  disabled?: boolean;
}) {
  return (
    <CompactSelect
      label={label}
      value={value}
      options={options}
      onChange={onChange}
      testId={testId}
      disabled={disabled}
    />
  );
}

function SettingSlider({
  label,
  hint,
  value,
  onChange,
  testId,
  disabled = false,
  max = 5,
  valueLabel,
}: {
  label: string;
  hint: string;
  value: number;
  onChange: (value: number) => void;
  testId: string;
  disabled?: boolean;
  max?: number;
  valueLabel?: string;
}) {
  return (
    <div className="ui-standards-qcard-slider">
      <div className="ui-standards-qcard-slider__label">
        <span>{label}</span>
        <span data-testid={`${testId}-value`}>{valueLabel ?? value}</span>
      </div>
      <UiStandardsQslider
        color={RADIO_STANDARD_COLOR}
        ink="#ffffff"
        value={value}
        min={1}
        max={max}
        step={1}
        snap
        showLabel
        labelAlways
        disabled={disabled}
        onChange={onChange}
        testId={testId}
      />
      <div className="ui-standards-qcard-slider__hint">{hint}</div>
    </div>
  );
}

function CheckBoxes<T extends string>({
  options,
  checks,
  onChange,
  disabled = false,
  testId,
}: {
  options: ReadonlyArray<{ id: T; label: string; title: string }>;
  checks: Record<T, boolean>;
  onChange: (checks: Record<T, boolean>) => void;
  disabled?: boolean;
  testId: string;
}) {
  return (
    <div className="ui-standards-qcard-corners" data-testid={testId}>
      {options.map((option) => (
        <span key={option.id} title={option.title}>
          <UiStandardsQcheckbox
            label={option.label}
            color={RADIO_STANDARD_COLOR}
            dense
            checked={checks[option.id]}
            disabled={disabled}
            onSelect={() => onChange({ ...checks, [option.id]: !checks[option.id] })}
            testId={`${testId}-${option.id}`}
          />
        </span>
      ))}
    </div>
  );
}

function SettingCard({ title, testId, children }: { title: string; testId: string; children: ReactNode }) {
  return (
    <article className="ui-standards-qcard ui-standards-qcard--settings" data-testid={testId}>
      <div className="ui-standards-qcard__section">
        <div className="ui-standards-qcard__title">{title}</div>
      </div>
      <div className="ui-standards-qcard__section ui-standards-qcard__section--tight ui-standards-qcard__settings">{children}</div>
    </article>
  );
}

export function UiStandardsCardPlayground() {
  const [intent, setIntent] = useState("standard");
  const [headerIntent, setHeaderIntent] = useState("standard");
  const [header, setHeader] = useState<CardHeaderKind>("title");
  const [body, setBody] = useState<CardBodyKind>("text");
  const [footer, setFooter] = useState<CardFooterKind>("none");
  const [footerIntent, setFooterIntent] = useState("standard");
  const [align, setAlign] = useState<CardActionAlign>("start");
  const [buttonAppearance, setButtonAppearance] = useState<ButtonAppearance>("standard");
  const { preferences, setDensity } = usePreferences();
  const buttonDensity = preferences.density;
  const [iconOnly, setIconOnly] = useState(false);
  const [vertical, setVertical] = useState(false);
  const [fill, setFill] = useState<CardFill>("solid");
  const [gradientDirection, setGradientDirection] = useState<CardGradientDirection>("to-bottom");
  const [elevation, setElevation] = useState(DEFAULT_ELEVATION);
  const [radius, setRadius] = useState(DEFAULT_RADIUS);
  const [radiusCorners, setRadiusCorners] = useState<RadiusCorners>(ALL_RADIUS_CORNERS);
  const [borderSides, setBorderSides] = useState<BorderSides>(NO_BORDER_SIDES);
  const [borderSize, setBorderSize] = useState(DEFAULT_BORDER);
  const [titleAlign, setTitleAlign] = useState<CardTitleAlign>("start");
  const [fontSize, setFontSize] = useState(DEFAULT_FONT);
  const [height, setHeight] = useState(DEFAULT_HEIGHT);
  const [width, setWidth] = useState(DEFAULT_WIDTH);
  const [settingsTab, setSettingsTab] = useState("card");
  const [headerPill, setHeaderPill] = useState(false);
  const [headerElevation, setHeaderElevation] = useState(1);
  const [headerRadius, setHeaderRadius] = useState(DEFAULT_RADIUS);
  const [headerRadiusCorners, setHeaderRadiusCorners] = useState<RadiusCorners>(ALL_RADIUS_CORNERS);
  const [headerBorderSides, setHeaderBorderSides] = useState<BorderSides>(NO_BORDER_SIDES);
  const [headerBorderSize, setHeaderBorderSize] = useState(DEFAULT_BORDER);
  const [headerAppearance, setHeaderAppearance] = useState<ButtonAppearance>("standard");
  const [headerPosition, setHeaderPosition] = useState(1);
  const sample = sampleById(intent);
  const headerSample = sampleById(headerIntent);
  const footerSample = sampleById(footerIntent);
  const tinted = sample.id !== "standard";
  const headerTinted = header !== "none" && headerSample.id !== "standard";
  const footerTinted = footer !== "none" && footerSample.id !== "standard";
  const actionIcons = footer === "actions" && (iconOnly || vertical);
  const sideIcons = footer === "actions" && vertical;
  const gradient = fill === "gradient";
  const gradientCss = GRADIENT_OPTIONS.find((option) => option.value === gradientDirection)?.css ?? "to bottom";
  const cardStyle = {
    "--qcard-bg": intentPaint(sample),
    "--qcard-ink": sample.fg,
    "--qcard-gradient-dir": gradientCss,
    "--qcard-radius": cornerRadiusCss(stepCss(RADIUS_SIZES, radius), radiusCorners),
    "--qcard-shadow": stepCss(ELEVATION_SHADOWS, elevation),
    "--qcard-border-width":
      fill === "outline" && !anyBorder(borderSides) ? "1px" : borderWidthCss(borderSides, borderSize),
    "--qcard-border-color":
      fill === "outline"
        ? "var(--qcard-outline)"
        : anyBorder(borderSides)
          ? "color-mix(in srgb, currentColor 55%, transparent)"
          : "transparent",
    "--qcard-outline": outlineInk(sample),
    ...(fill === "outline" ? { color: outlineInk(sample) } : {}),
  } as CSSProperties;
  function resetPreview() {
    setIntent("standard");
    setHeaderIntent("standard");
    setHeader("title");
    setBody("text");
    setFooter("none");
    setFooterIntent("standard");
    setAlign("start");
    setButtonAppearance("standard");
    setDensity("balance");
    setIconOnly(false);
    setVertical(false);
    setFill("solid");
    setGradientDirection("to-bottom");
    setElevation(DEFAULT_ELEVATION);
    setRadius(DEFAULT_RADIUS);
    setRadiusCorners(ALL_RADIUS_CORNERS);
    setBorderSides(NO_BORDER_SIDES);
    setBorderSize(DEFAULT_BORDER);
    setTitleAlign("start");
    setFontSize(DEFAULT_FONT);
    setHeight(DEFAULT_HEIGHT);
    setWidth(DEFAULT_WIDTH);
    setHeaderPill(false);
    setHeaderElevation(1);
    setHeaderRadius(DEFAULT_RADIUS);
    setHeaderRadiusCorners(ALL_RADIUS_CORNERS);
    setHeaderBorderSides(NO_BORDER_SIDES);
    setHeaderBorderSize(DEFAULT_BORDER);
    setHeaderAppearance("standard");
    setHeaderPosition(1);
    setSettingsTab("card");
  }

  const headerAccent = playgroundIntentColor(headerSample.id, inputAccent(headerSample));
  const headerOutlineLike =
    headerAppearance === "outline" || headerAppearance === "soft-outline" || headerAppearance === "ghost";
  const headerStyle = {
    "--qcard-part-bg": intentPaint(headerSample),
    "--qcard-part-ink": headerSample.fg,
    "--qheader-bg": intentPaint(headerSample),
    "--qheader-ink": headerSample.fg,
    "--qheader-accent": headerAccent,
    "--qheader-pad": stepCss(HEIGHT_PAD, height),
    "--qheader-min-height": stepCss(HEIGHT_MIN, height),
    "--qheader-inset": stepCss(WIDTH_INSET, width),
    "--qheader-font": stepCss(FONT_SIZES, fontSize),
    "--qheader-shadow": stepCss(ELEVATION_SHADOWS, headerElevation),
    "--qheader-radius": headerPill ? "9999px" : cornerRadiusCss(stepCss(HEADER_RADIUS_SIZES, headerRadius), headerRadiusCorners),
    "--qheader-shift": stepCss(HEADER_SHIFTS, headerPosition),
    "--qheader-border-width":
      headerAppearance === "outline" && !anyBorder(headerBorderSides)
        ? "1px"
        : borderWidthCss(headerBorderSides, headerBorderSize),
    "--qheader-border-color":
      headerAppearance === "outline"
        ? "var(--qheader-accent)"
        : anyBorder(headerBorderSides)
          ? "color-mix(in srgb, currentColor 55%, transparent)"
          : "transparent",
    "--qheader-outline": outlineInk(headerSample),
    ...(headerOutlineLike ? { color: headerAccent } : {}),
  } as CSSProperties;
  const footerStyle = {
    "--qcard-part-bg": intentPaint(footerSample),
    "--qcard-part-ink": footerSample.fg,
  } as CSSProperties;
  const bodyPart =
    body === "none" ? null : (
      <div
        className={"ui-standards-qcard__section" + (header === "none" ? "" : " ui-standards-qcard__section--tight")}
        data-testid="ui-standard-card-pg-body-part"
      >
        {CARD_LOREM}
        {body === "sections" ? (
          <>
            <div className="ui-standards-qcard__separator" />
            <p className="ui-standards-qcard__note">Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.</p>
          </>
        ) : null}
      </div>
    );
  const footerPart =
    footer === "none" ? null : (
      <div
        className={
          "ui-standards-qcard__footer" +
          (actionIcons ? " ui-standards-qcard__footer--icons" : "") +
          (sideIcons ? " ui-standards-qcard__footer--vertical" : "") +
          (footerTinted ? " ui-standards-qcard__footer--intent" : "")
        }
        style={footerTinted ? footerStyle : undefined}
        data-testid="ui-standard-card-pg-footer-part"
        data-intent={footerSample.id}
        data-align={footer === "actions" ? align : undefined}
        data-appearance={footer === "actions" ? buttonAppearance : undefined}
        data-density={footer === "actions" ? buttonDensity : undefined}
      >
        {footer === "caption" ? (
          <span>Updated today</span>
        ) : actionIcons ? (
          <CardIconActions className={buttonAppearanceClass(buttonAppearance)} style={buttonAppearanceStyle(buttonAppearance, footerSample)} />
        ) : (
          <>
            <button type="button" className={buttonAppearanceClass(buttonAppearance) || undefined} style={buttonAppearanceStyle(buttonAppearance, footerSample)}>
              Action 1
            </button>
            <button type="button" className={buttonAppearanceClass(buttonAppearance) || undefined} style={buttonAppearanceStyle(buttonAppearance, footerSample)}>
              Action 2
            </button>
          </>
        )}
      </div>
    );

  return (
    <PlaygroundSection testId="ui-standard-card-playground" className="">
      <PlaygroundPreviewCard
        previewTestId="ui-standard-card-pg-preview"
        themeTestId="ui-standard-card-pg-theme"
        resetTestId="ui-standard-card-pg-reset"
        onReset={resetPreview}
        previewClassName="ui-standards-qcard-stage"
      >
        <ExitsTabs
          variant="underline"
          layout="equal"
          ariaLabel="Card settings"
          value={settingsTab}
          onValueChange={setSettingsTab}
          testId="ui-standard-card-pg-tabs"
          items={[
            { key: "card", label: "Card", testId: "ui-standard-card-pg-tab-card" },
            { key: "header", label: "Header", testId: "ui-standard-card-pg-tab-header" },
            { key: "body", label: "Body", testId: "ui-standard-card-pg-tab-body" },
            { key: "footer", label: "Footer", testId: "ui-standard-card-pg-tab-footer" },
          ]}
          panels={{
            card: (
              <SettingCard title="Card" testId="ui-standard-card-pg-card-settings">
                <SettingSelect
                  label="Intent"
                  value={intent}
                  options={INTENT_OPTIONS}
                  onChange={setIntent}
                  testId="ui-standard-card-pg-card"
                />
                <SettingSelect
                  label="Fill"
                  value={fill}
                  options={FILL_OPTIONS}
                  onChange={setFill}
                  testId="ui-standard-card-pg-fill"
                />
                {gradient ? (
                  <SettingSelect
                    label="Direction"
                    value={gradientDirection}
                    options={GRADIENT_OPTIONS}
                    onChange={setGradientDirection}
                    testId="ui-standard-card-pg-gradient"
                  />
                ) : null}
                <SettingSlider
                  label="Elevation"
                  hint="1 flat · 5 raised"
                  value={elevation}
                  onChange={setElevation}
                  testId="ui-standard-card-pg-elevation"
                />
                <SettingSlider
                  label="Radius"
                  hint="1 small · 10 large"
                  value={radius}
                  onChange={setRadius}
                  testId="ui-standard-card-pg-radius"
                  max={RADIUS_MAX}
                />
                <CheckBoxes
                  options={RADIUS_CORNER_OPTIONS}
                  checks={radiusCorners}
                  onChange={setRadiusCorners}
                  testId="ui-standard-card-pg-radius-corners"
                />
                <div className="ui-standards-qcard-slider__label">
                  <span>Border</span>
                </div>
                <CheckBoxes
                  options={BORDER_SIDE_OPTIONS}
                  checks={borderSides}
                  onChange={setBorderSides}
                  testId="ui-standard-card-pg-border-sides"
                />
                <SettingSlider
                  label="Thickness"
                  hint="1 thin · 10 thick"
                  value={borderSize}
                  onChange={setBorderSize}
                  testId="ui-standard-card-pg-border-size"
                  disabled={!anyBorder(borderSides)}
                  max={10}
                />
              </SettingCard>
            ),
            header: (
              <SettingCard title="Header" testId="ui-standard-card-pg-header-settings">
                <SettingSelect
                  label="Type"
                  value={header}
                  options={HEADER_OPTIONS}
                  onChange={setHeader}
                  testId="ui-standard-card-pg-header"
                />
                <SettingSelect
                  label="Intent"
                  value={headerIntent}
                  options={INTENT_OPTIONS}
                  onChange={setHeaderIntent}
                  testId="ui-standard-card-pg-header-intent"
                />
                <SettingSelect
                  label="Appearance"
                  value={headerAppearance}
                  options={BUTTON_APPEARANCE_OPTIONS}
                  onChange={setHeaderAppearance}
                  testId="ui-standard-card-pg-header-appearance"
                  disabled={header === "none"}
                />
                <SettingSelect
                  label="Title"
                  value={titleAlign}
                  options={TITLE_ALIGN_OPTIONS}
                  onChange={setTitleAlign}
                  testId="ui-standard-card-pg-title-align"
                  disabled={header === "none"}
                />
                <SettingSlider
                  label="Font size"
                  hint="H1 large · H5 small"
                  value={fontSize}
                  valueLabel={`H${fontSize}`}
                  onChange={setFontSize}
                  testId="ui-standard-card-pg-font"
                  disabled={header === "none"}
                />
                <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                  <Switch
                    checked={headerPill}
                    disabled={header === "none"}
                    onCheckedChange={setHeaderPill}
                    aria-label="Pill"
                    data-testid="ui-standard-card-pg-header-pill"
                  />
                  Pill
                </label>
                <SettingSlider
                  label="Position"
                  hint="1 inside · 3 float · 5 outside"
                  value={headerPosition}
                  onChange={setHeaderPosition}
                  testId="ui-standard-card-pg-header-position"
                  disabled={header === "none"}
                />
                <SettingSlider
                  label="Height"
                  hint="1 short · 5 tall"
                  value={height}
                  onChange={setHeight}
                  testId="ui-standard-card-pg-height"
                  disabled={header === "none"}
                />
                <SettingSlider
                  label="Width"
                  hint="1 narrow · 5 wide"
                  value={width}
                  onChange={setWidth}
                  testId="ui-standard-card-pg-width"
                  disabled={header === "none"}
                />
                <SettingSlider
                  label="Elevation"
                  hint="1 flat · 5 raised"
                  value={headerElevation}
                  onChange={setHeaderElevation}
                  testId="ui-standard-card-pg-header-elevation"
                  disabled={header === "none"}
                />
                <SettingSlider
                  label="Radius"
                  hint="1 square · 10 large"
                  value={headerRadius}
                  onChange={setHeaderRadius}
                  testId="ui-standard-card-pg-header-radius"
                  disabled={header === "none" || headerPill}
                  max={RADIUS_MAX}
                />
                <CheckBoxes
                  options={RADIUS_CORNER_OPTIONS}
                  checks={headerRadiusCorners}
                  onChange={setHeaderRadiusCorners}
                  disabled={header === "none" || headerPill}
                  testId="ui-standard-card-pg-header-radius-corners"
                />
                <div className="ui-standards-qcard-slider__label">
                  <span>Border</span>
                </div>
                <CheckBoxes
                  options={BORDER_SIDE_OPTIONS}
                  checks={headerBorderSides}
                  onChange={setHeaderBorderSides}
                  disabled={header === "none"}
                  testId="ui-standard-card-pg-header-border-sides"
                />
                <SettingSlider
                  label="Thickness"
                  hint="1 thin · 10 thick"
                  value={headerBorderSize}
                  onChange={setHeaderBorderSize}
                  testId="ui-standard-card-pg-header-border-size"
                  disabled={header === "none" || !anyBorder(headerBorderSides)}
                  max={10}
                />
              </SettingCard>
            ),
            body: (
              <SettingCard title="Body" testId="ui-standard-card-pg-body-settings">
                <SettingSelect
                  label="Content"
                  value={body}
                  options={BODY_OPTIONS}
                  onChange={setBody}
                  testId="ui-standard-card-pg-body"
                />
              </SettingCard>
            ),
            footer: (
              <SettingCard title="Footer" testId="ui-standard-card-pg-footer-settings">
                <SettingSelect
                  label="Type"
                  value={footer}
                  options={FOOTER_OPTIONS}
                  onChange={setFooter}
                  testId="ui-standard-card-pg-footer"
                />
                <SettingSelect
                  label="Intent"
                  value={footerIntent}
                  options={INTENT_OPTIONS}
                  onChange={setFooterIntent}
                  testId="ui-standard-card-pg-footer-intent"
                />
                {footer === "actions" ? (
                  <>
                    <SettingSelect
                      label="Align"
                      value={align}
                      options={ALIGN_OPTIONS}
                      onChange={setAlign}
                      testId="ui-standard-card-pg-footer-align"
                    />
                    <SettingSelect
                      label="Appearance"
                      value={buttonAppearance}
                      options={BUTTON_APPEARANCE_OPTIONS}
                      onChange={setButtonAppearance}
                      testId="ui-standard-card-pg-footer-appearance"
                    />
                    <SettingSelect
                      label="Density"
                      value={buttonDensity}
                      options={BUTTON_DENSITY_OPTIONS}
                      onChange={setDensity}
                      testId="ui-standard-card-pg-footer-density"
                    />
                    <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                      <Switch
                        checked={iconOnly || vertical}
                        disabled={vertical}
                        onCheckedChange={setIconOnly}
                        aria-label="Icon only"
                        data-testid="ui-standard-card-pg-footer-icons"
                      />
                      Icon only
                    </label>
                    <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                      <Switch
                        checked={vertical}
                        onCheckedChange={setVertical}
                        aria-label="Right vertical"
                        data-testid="ui-standard-card-pg-footer-vertical"
                      />
                      Right vertical
                    </label>
                  </>
                ) : null}
              </SettingCard>
            ),
          }}
        />
        <div
          className={"ui-standards-qcard-preview-slot" + (headerPosition > 1 ? " ui-standards-qcard-row--room" : "")}
          data-testid="ui-standard-card-pg-preview-slot"
        >
          <div className={"ui-standards-qcard-row" + (headerPosition > 1 ? " ui-standards-qcard-row--room" : "")}>
            <article
              className={
                "ui-standards-qcard ui-standards-qcard--preview" +
                (tinted ? " ui-standards-qcard--intent" : "") +
                (gradient ? " ui-standards-qcard--gradient" : "") +
                (fill === "outline" ? " ui-standards-qcard--outline" : "") +
                (sideIcons ? " ui-standards-qcard--side" : "") +
                (headerPosition > 1 || width > DEFAULT_WIDTH ? " ui-standards-qcard--release" : "")
              }
              style={cardStyle}
              data-testid="ui-standard-card-pg-sample"
              data-intent={sample.id}
              data-elevation={elevation}
              data-radius={radius}
              data-corners={selectedCorners(radiusCorners)}
              data-border-sides={selectedSides(borderSides)}
              data-border={borderSize}
              data-gradient={gradient ? gradientDirection : undefined}
            >
              {header === "none" ? null : (
                <div
                  className={
                    "ui-standards-qcard__section ui-standards-qcard__section--header" +
                    (headerTinted && headerAppearance === "standard" ? " ui-standards-qcard__section--intent" : "") +
                    (headerPill ? " ui-standards-qcard__section--pill" : "")
                  }
                  style={headerStyle}
                  data-testid="ui-standard-card-pg-header-part"
                  data-intent={headerSample.id}
                  data-appearance={headerAppearance}
                  data-title-align={titleAlign}
                  data-font={fontSize}
                  data-pill={headerPill ? "true" : "false"}
                  data-position={headerPosition}
                  data-height={height}
                  data-width={width}
                  data-elevation={headerElevation}
                  data-radius={headerRadius}
                  data-corners={selectedCorners(headerRadiusCorners)}
                  data-border-sides={selectedSides(headerBorderSides)}
                  data-border={headerBorderSize}
                >
                  <div className="ui-standards-qcard__title">Our Changing Planet</div>
                  {header === "subtitle" ? <div className="ui-standards-qcard__subtitle">by John Doe</div> : null}
                </div>
              )}
              {sideIcons ? (
                <div className="ui-standards-qcard__body-row">
                  {bodyPart}
                  {footerPart}
                </div>
              ) : (
                <>
                  {bodyPart}
                  {footerPart}
                </>
              )}
            </article>
          </div>
        </div>
      </PlaygroundPreviewCard>
      <UiStandardsCopyCommand
        standard="Card"
        standardStatus="pilot"
        command={cardCursorCommand(
          intent,
          headerIntent,
          header,
          body,
          footer,
          align,
          buttonAppearance,
          buttonDensity,
          iconOnly,
          vertical,
          footerIntent,
          fill,
          gradientDirection,
          elevation,
          radius,
          radiusCorners,
          borderSides,
          borderSize,
          titleAlign,
          fontSize,
          height,
          width,
          headerPill,
          headerElevation,
          headerRadius,
          headerRadiusCorners,
          headerBorderSides,
          headerBorderSize,
          headerAppearance,
          headerPosition,
        )}
      />
    </PlaygroundSection>
  );
}

const RIBBON_SAMPLES = [
  { intent: "primary", label: "Primary", color: "#1976d2", appearance: "raised" },
  { intent: "secondary", label: "Secondary", color: "#26a69a", appearance: "gradient" },
  { intent: "info", label: "Info", color: "#0ea5e9", appearance: "glossy" },
  { intent: "warning", label: "Warning", color: "#ff5722", appearance: "raised" },
] as const;

function CardSampleHeading({ children, testId }: { children: ReactNode; testId: string }) {
  return (
    <p className="ui-standards-qcard-sample__heading m-0" data-testid={testId}>
      {children}
    </p>
  );
}

/** Showcase sample: Quasar-style overhanging ribbon headers (primary, secondary, info, warning). */
export function UiStandardsCardRibbonSample() {
  return (
    <div className="ui-standards-qcard-sample" data-testid="ui-standard-card-ribbon-sample">
      <CardSampleHeading testId="ui-standard-card-ribbon-heading">Card ribbon</CardSampleHeading>
      <div className="ui-standards-qcard-row ui-standards-qcard-row--ribbon">
        {RIBBON_SAMPLES.map((sample) => (
          <article
            key={sample.intent}
            className="ui-standards-qcard ui-standards-qcard--ribbon"
            data-intent={sample.intent}
            data-appearance={sample.appearance}
            data-testid={`ui-standard-card-ribbon-${sample.intent}`}
            style={{ "--qcard-ribbon-bg": sample.color } as CSSProperties}
          >
            <div
              className="ui-standards-qcard__ribbon"
              data-appearance={sample.appearance}
              data-testid={`ui-standard-card-ribbon-label-${sample.intent}`}
            >
              {sample.label}
            </div>
            <div
              className="ui-standards-qcard__ribbon-inset"
              data-testid={`ui-standard-card-ribbon-body-${sample.intent}`}
            >
              <div className="ui-standards-qcard__title">Our Changing Planet</div>
              <div className="ui-standards-qcard__subtitle">by John Doe</div>
              <div className="ui-standards-qcard__separator" />
              <p className="ui-standards-qcard__note m-0">{CARD_LOREM}</p>
            </div>
          </article>
        ))}
      </div>
    </div>
  );
}

/**
 * Showcase sample: ticket stub with side punch holes and a tear perforation.
 * Invented sample — not a Quasar copy.
 */
export function UiStandardsCardTicketSample() {
  return (
    <div className="ui-standards-qcard-sample" data-testid="ui-standard-card-ticket-sample">
      <CardSampleHeading testId="ui-standard-card-ticket-heading">Card ticket</CardSampleHeading>
      <div className="ui-standards-qcard-row ui-standards-qcard-row--ticket">
        <article className="ui-standards-qcard ui-standards-qcard--ticket" data-testid="ui-standard-card-ticket">
          <div className="ui-standards-qcard__ticket-stub" data-testid="ui-standard-card-ticket-stub">
            <span className="ui-standards-qcard__ticket-code">A-17</span>
            <span className="ui-standards-qcard__ticket-seat">Seat</span>
          </div>
          <div className="ui-standards-qcard__ticket-perforation" aria-hidden />
          <div className="ui-standards-qcard__ticket-body" data-testid="ui-standard-card-ticket-body">
            <div className="ui-standards-qcard__title">Evening Market Pass</div>
            <div className="ui-standards-qcard__subtitle">Gate 3 · 18:00</div>
            <div className="ui-standards-qcard__separator" />
            <p className="ui-standards-qcard__note m-0">
              Tear along the stub to keep your entry copy. The punch holes mark the ticket edge.
            </p>
          </div>
        </article>
      </div>
    </div>
  );
}

/** Invented sample: vertical bookmark tab on the right edge. */
export function UiStandardsCardBookmarkSample() {
  return (
    <div className="ui-standards-qcard-sample" data-testid="ui-standard-card-bookmark-sample">
      <CardSampleHeading testId="ui-standard-card-bookmark-heading">Card bookmark</CardSampleHeading>
      <div className="ui-standards-qcard-row ui-standards-qcard-row--bookmark">
        <article className="ui-standards-qcard ui-standards-qcard--bookmark" data-testid="ui-standard-card-bookmark">
          <div className="ui-standards-qcard__bookmark-tab" data-testid="ui-standard-card-bookmark-tab">
            Saved
          </div>
          <div className="ui-standards-qcard__bookmark-body" data-testid="ui-standard-card-bookmark-body">
            <div className="ui-standards-qcard__title">Recipe notes</div>
            <div className="ui-standards-qcard__subtitle">Pinned for later</div>
            <div className="ui-standards-qcard__separator" />
            <p className="ui-standards-qcard__note m-0">
              The side tab marks a saved card without covering the title.
            </p>
          </div>
        </article>
      </div>
    </div>
  );
}

/** Invented sample: wax-seal style stamp overlapping the corner. */
export function UiStandardsCardStampSample() {
  return (
    <div className="ui-standards-qcard-sample" data-testid="ui-standard-card-stamp-sample">
      <CardSampleHeading testId="ui-standard-card-stamp-heading">Card stamp</CardSampleHeading>
      <div className="ui-standards-qcard-row ui-standards-qcard-row--stamp">
        <article className="ui-standards-qcard ui-standards-qcard--stamp" data-testid="ui-standard-card-stamp">
          <div className="ui-standards-qcard__stamp-seal" data-testid="ui-standard-card-stamp-seal" aria-hidden>
            <span>OK</span>
          </div>
          <div className="ui-standards-qcard__stamp-body" data-testid="ui-standard-card-stamp-body">
            <div className="ui-standards-qcard__title">Approved order</div>
            <div className="ui-standards-qcard__subtitle">Warehouse · Batch 42</div>
            <div className="ui-standards-qcard__separator" />
            <p className="ui-standards-qcard__note m-0">
              A round seal sits on the corner like a stamped approval mark.
            </p>
          </div>
        </article>
      </div>
    </div>
  );
}

/** Invented sample: layered stack showing depth behind the front card. */
export function UiStandardsCardStackSample() {
  return (
    <div className="ui-standards-qcard-sample" data-testid="ui-standard-card-stack-sample">
      <CardSampleHeading testId="ui-standard-card-stack-heading">Card stack</CardSampleHeading>
      <div className="ui-standards-qcard-row ui-standards-qcard-row--stack">
        <div className="ui-standards-qcard-stack" data-testid="ui-standard-card-stack">
          <div className="ui-standards-qcard-stack__sheet" aria-hidden />
          <div className="ui-standards-qcard-stack__sheet" aria-hidden />
          <article className="ui-standards-qcard ui-standards-qcard--stack-front" data-testid="ui-standard-card-stack-front">
            <div className="ui-standards-qcard__stack-body" data-testid="ui-standard-card-stack-body">
              <div className="ui-standards-qcard__title">Shift handoff</div>
              <div className="ui-standards-qcard__subtitle">3 notes under this card</div>
              <div className="ui-standards-qcard__separator" />
              <p className="ui-standards-qcard__note m-0">
                Offset sheets behind the front card suggest a small pile of related items.
              </p>
            </div>
          </article>
        </div>
      </div>
    </div>
  );
}

/** Invented sample: diagonal corner stripe band. */
export function UiStandardsCardStripeSample() {
  return (
    <div className="ui-standards-qcard-sample" data-testid="ui-standard-card-stripe-sample">
      <CardSampleHeading testId="ui-standard-card-stripe-heading">Card stripe</CardSampleHeading>
      <div className="ui-standards-qcard-row ui-standards-qcard-row--stripe">
        <article className="ui-standards-qcard ui-standards-qcard--stripe" data-testid="ui-standard-card-stripe">
          <div className="ui-standards-qcard__stripe-band" data-testid="ui-standard-card-stripe-band" aria-hidden>
            <span>New</span>
          </div>
          <div className="ui-standards-qcard__stripe-body" data-testid="ui-standard-card-stripe-body">
            <div className="ui-standards-qcard__title">Menu launch</div>
            <div className="ui-standards-qcard__subtitle">Seasonal set · this week</div>
            <div className="ui-standards-qcard__separator" />
            <p className="ui-standards-qcard__note m-0">
              A diagonal band cuts the corner to mark something new without a full header.
            </p>
          </div>
        </article>
      </div>
    </div>
  );
}

/** Invented sample: top notch with a nested avatar chip. */
export function UiStandardsCardNotchSample() {
  return (
    <div className="ui-standards-qcard-sample" data-testid="ui-standard-card-notch-sample">
      <CardSampleHeading testId="ui-standard-card-notch-heading">Card notch</CardSampleHeading>
      <div className="ui-standards-qcard-row ui-standards-qcard-row--notch">
        <article className="ui-standards-qcard ui-standards-qcard--notch" data-testid="ui-standard-card-notch">
          <div className="ui-standards-qcard__notch-chip" data-testid="ui-standard-card-notch-chip" aria-hidden>
            JD
          </div>
          <div className="ui-standards-qcard__notch-body" data-testid="ui-standard-card-notch-body">
            <div className="ui-standards-qcard__title">Crew profile</div>
            <div className="ui-standards-qcard__subtitle">John Doe · Cashier</div>
            <div className="ui-standards-qcard__separator" />
            <p className="ui-standards-qcard__note m-0">
              The top edge is cut for an avatar chip that sits half outside the card.
            </p>
          </div>
        </article>
      </div>
    </div>
  );
}

/** Invented sample: paperclip bar clipped over the top edge. */
export function UiStandardsCardClipSample() {
  return (
    <div className="ui-standards-qcard-sample" data-testid="ui-standard-card-clip-sample">
      <CardSampleHeading testId="ui-standard-card-clip-heading">Card clip</CardSampleHeading>
      <div className="ui-standards-qcard-row ui-standards-qcard-row--clip">
        <article className="ui-standards-qcard ui-standards-qcard--clip" data-testid="ui-standard-card-clip">
          <div className="ui-standards-qcard__clip-bar" data-testid="ui-standard-card-clip-bar" aria-hidden />
          <div className="ui-standards-qcard__clip-body" data-testid="ui-standard-card-clip-body">
            <div className="ui-standards-qcard__title">Attached memo</div>
            <div className="ui-standards-qcard__subtitle">Front counter · today</div>
            <div className="ui-standards-qcard__separator" />
            <p className="ui-standards-qcard__note m-0">
              A metal-style clip sits over the top edge like a paper clipped to the card.
            </p>
          </div>
        </article>
      </div>
    </div>
  );
}

const FOOTER_ACTION_SAMPLES = [
  {
    intent: "primary",
    title: "Restock request",
    subtitle: "Shelf B · low stock",
    color: "#1976d2",
    ink: "#ffffff",
    actions: ["Review", "Approve"],
  },
  {
    intent: "warning",
    title: "Delay notice",
    subtitle: "Supplier ETA slipped",
    color: "#f59e0b",
    ink: "#451a03",
    actions: ["Snooze", "Escalate"],
  },
] as const;

/** Pennant badge icons reuse the Quasar button-icon set for sample variety. */
const PENNANT_SAMPLES = [
  {
    id: "blue",
    label: "Primary",
    title: "Counter goals",
    footer: "Open board",
    color: "#1976d2",
    soft: "#e3f2fd",
    ink: "#0d47a1",
    Icon: Check,
  },
  {
    id: "purple",
    label: "Secondary",
    title: "Shift pack",
    footer: "View roster",
    color: "#26a69a",
    soft: "#e0f2f1",
    ink: "#004d40",
    Icon: Bookmark,
  },
  {
    id: "pink",
    label: "Help",
    title: "Floor tips",
    footer: "Add note",
    color: "#a855f7",
    soft: "#f3e8ff",
    ink: "#6b21a8",
    Icon: Heart,
  },
  {
    id: "amber",
    label: "Amber",
    title: "Weekend push",
    footer: "Share flyer",
    color: "#ffc107",
    soft: "#fff8e1",
    ink: "#92400e",
    Icon: Bell,
  },
  {
    id: "info",
    label: "Info",
    title: "Live queue",
    footer: "Open list",
    color: "#0ea5e9",
    soft: "#e0f2fe",
    ink: "#0369a1",
    Icon: Info,
  },
  {
    id: "success",
    label: "Success",
    title: "Settled till",
    footer: "View report",
    color: "#22c55e",
    soft: "#dcfce7",
    ink: "#14532d",
    Icon: Star,
  },
  {
    id: "danger",
    label: "Danger",
    title: "Void alert",
    footer: "Review",
    color: "#ef4444",
    soft: "#fee2e2",
    ink: "#7f1d1d",
    Icon: Trash2,
  },
  {
    id: "warn",
    label: "Warn",
    title: "Low stock",
    footer: "Restock",
    color: "#ff5722",
    soft: "#ffedd5",
    ink: "#7c2d12",
    Icon: TriangleAlert,
  },
] as const;

const BANNER_SAMPLES = [
  {
    id: "blue",
    color: "#42a5f5",
    soft: "#e3f2fd",
    ink: "#1565c0",
    Icon: Crosshair,
  },
  {
    id: "purple",
    color: "#ab47bc",
    soft: "#f3e5f5",
    ink: "#7b1fa2",
    Icon: Briefcase,
  },
  {
    id: "pink",
    color: "#ec407a",
    soft: "#fce4ec",
    ink: "#c2185b",
    Icon: Lightbulb,
  },
  {
    id: "amber",
    color: "#ffa726",
    soft: "#fff8e1",
    ink: "#ef6c00",
    Icon: Megaphone,
  },
] as const;

/** Showcase sample: folded arrow-ribbon banner cards matching the reference layout. */
export function UiStandardsCardBannerSample() {
  return (
    <div className="ui-standards-qcard-sample" data-testid="ui-standard-card-banner-sample">
      <CardSampleHeading testId="ui-standard-card-banner-heading">Card banner</CardSampleHeading>
      <div className="ui-standards-qcard-row ui-standards-qcard-row--banner">
        {BANNER_SAMPLES.map((sample) => {
          const Icon = sample.Icon;
          return (
            <article
              key={sample.id}
              className="ui-standards-qcard ui-standards-qcard--banner"
              data-intent={sample.id}
              data-testid={`ui-standard-card-banner-${sample.id}`}
              style={
                {
                  "--qcard-banner": sample.color,
                  "--qcard-banner-soft": sample.soft,
                  "--qcard-banner-ink": sample.ink,
                } as CSSProperties
              }
            >
              <div
                className="ui-standards-qcard__banner-ribbon-wrap"
                data-testid={`ui-standard-card-banner-ribbon-${sample.id}`}
              >
                <span className="ui-standards-qcard__banner-fold ui-standards-qcard__banner-fold--left" aria-hidden />
                <span className="ui-standards-qcard__banner-fold ui-standards-qcard__banner-fold--right" aria-hidden />
                <div className="ui-standards-qcard__banner-ribbon">
                  <Icon className="ui-standards-qcard__banner-icon" aria-hidden strokeWidth={2.25} />
                  <span className="ui-standards-qcard__banner-label">Lorem ipsum</span>
                </div>
              </div>
              <div
                className="ui-standards-qcard__banner-body"
                data-testid={`ui-standard-card-banner-body-${sample.id}`}
              >
                <div className="ui-standards-qcard__banner-title">Lorem ipsum dolor</div>
                <p className="ui-standards-qcard__banner-note m-0">{CARD_LOREM}</p>
              </div>
            </article>
          );
        })}
      </div>
    </div>
  );
}

/**
 * Invented sample: four tinted cards with a floating medallion + footer.
 * Inspired by pennant showcases, intentionally not a fold-ribbon copy.
 */
export function UiStandardsCardPennantSample() {
  return (
    <div className="ui-standards-qcard-sample" data-testid="ui-standard-card-pennant-sample">
      <CardSampleHeading testId="ui-standard-card-pennant-heading">Card pennant</CardSampleHeading>
      <div className="ui-standards-qcard-row ui-standards-qcard-row--pennant">
        {PENNANT_SAMPLES.map((sample) => {
          const Icon = sample.Icon;
          return (
            <article
              key={sample.id}
              className="ui-standards-qcard ui-standards-qcard--pennant"
              data-intent={sample.id}
              data-testid={`ui-standard-card-pennant-${sample.id}`}
              style={
                {
                  "--qcard-pennant": sample.color,
                  "--qcard-pennant-soft": sample.soft,
                  "--qcard-pennant-ink": sample.ink,
                } as CSSProperties
              }
            >
              <button
                type="button"
                className="ui-standards-qcard__pennant-badge"
                aria-label={sample.label}
                data-testid={`ui-standard-card-pennant-ribbon-${sample.id}`}
              >
                <Icon className="ui-standards-qcard__pennant-icon" aria-hidden strokeWidth={2} />
              </button>
              <div
                className="ui-standards-qcard__pennant-body"
                data-testid={`ui-standard-card-pennant-body-${sample.id}`}
              >
                <span className="ui-standards-qcard__pennant-label">{sample.label}</span>
                <div className="ui-standards-qcard__pennant-title">{sample.title}</div>
                <p className="ui-standards-qcard__pennant-note m-0">{CARD_LOREM}</p>
              </div>
              <div
                className="ui-standards-qcard__pennant-footer"
                data-testid={`ui-standard-card-pennant-footer-${sample.id}`}
              >
                <button type="button">{sample.footer}</button>
              </div>
            </article>
          );
        })}
      </div>
    </div>
  );
}

/** Invented sample: colored action footers. */
export function UiStandardsCardFooterActionsSample() {
  return (
    <div className="ui-standards-qcard-sample" data-testid="ui-standard-card-footer-actions-sample">
      <CardSampleHeading testId="ui-standard-card-footer-actions-heading">Card footer</CardSampleHeading>
      <div className="ui-standards-qcard-row ui-standards-qcard-row--footer-sample">
        {FOOTER_ACTION_SAMPLES.map((sample) => (
          <article
            key={sample.intent}
            className="ui-standards-qcard ui-standards-qcard--footer-sample"
            data-intent={sample.intent}
            data-testid={`ui-standard-card-footer-${sample.intent}`}
            style={
              {
                "--qcard-part-bg": sample.color,
                "--qcard-part-ink": sample.ink,
              } as CSSProperties
            }
          >
            <div className="ui-standards-qcard__section ui-standards-qcard__section--tight">
              <div className="ui-standards-qcard__title">{sample.title}</div>
              <div className="ui-standards-qcard__subtitle">{sample.subtitle}</div>
              <p className="ui-standards-qcard__note m-0">Colored footer with quick actions under the body.</p>
            </div>
            <div
              className="ui-standards-qcard__footer ui-standards-qcard__footer--intent"
              data-align="end"
              data-testid={`ui-standard-card-footer-bar-${sample.intent}`}
            >
              {sample.actions.map((action) => (
                <button key={action} type="button">
                  {action}
                </button>
              ))}
            </div>
          </article>
        ))}
      </div>
    </div>
  );
}

/** Invented sample: caption-style colored footer strip. */
export function UiStandardsCardFooterCaptionSample() {
  return (
    <div className="ui-standards-qcard-sample" data-testid="ui-standard-card-footer-caption-sample">
      <CardSampleHeading testId="ui-standard-card-footer-caption-heading">Card caption</CardSampleHeading>
      <div className="ui-standards-qcard-row ui-standards-qcard-row--footer-sample">
        <article
          className="ui-standards-qcard ui-standards-qcard--footer-sample"
          data-intent="info"
          data-testid="ui-standard-card-footer-caption"
          style={{ "--qcard-part-bg": "#0ea5e9", "--qcard-part-ink": "#ffffff" } as CSSProperties}
        >
          <div className="ui-standards-qcard__section ui-standards-qcard__section--tight">
            <div className="ui-standards-qcard__title">Live queue</div>
            <div className="ui-standards-qcard__subtitle">Counter 2 · waiting</div>
            <p className="ui-standards-qcard__note m-0">A caption footer carries status without action buttons.</p>
          </div>
          <div
            className="ui-standards-qcard__footer ui-standards-qcard__footer--intent ui-standards-qcard__footer--caption"
            data-testid="ui-standard-card-footer-caption-bar"
          >
            <span>Updated 2 min ago</span>
          </div>
        </article>
        <article
          className="ui-standards-qcard ui-standards-qcard--footer-sample"
          data-intent="success"
          data-testid="ui-standard-card-footer-success"
          style={{ "--qcard-part-bg": "#16a34a", "--qcard-part-ink": "#ffffff" } as CSSProperties}
        >
          <div className="ui-standards-qcard__section ui-standards-qcard__section--tight">
            <div className="ui-standards-qcard__title">Payment cleared</div>
            <div className="ui-standards-qcard__subtitle">Invoice #4812</div>
            <p className="ui-standards-qcard__note m-0">Success tone on the footer confirms a finished step.</p>
          </div>
          <div
            className="ui-standards-qcard__footer ui-standards-qcard__footer--intent ui-standards-qcard__footer--caption"
            data-testid="ui-standard-card-footer-success-bar"
          >
            <span>Settled · cash</span>
          </div>
        </article>
      </div>
    </div>
  );
}

/** Invented sample: split danger/success footer actions. */
export function UiStandardsCardFooterSplitSample() {
  return (
    <div className="ui-standards-qcard-sample" data-testid="ui-standard-card-footer-split-sample">
      <CardSampleHeading testId="ui-standard-card-footer-split-heading">Card split footer</CardSampleHeading>
      <div className="ui-standards-qcard-row ui-standards-qcard-row--footer-sample">
        <article className="ui-standards-qcard ui-standards-qcard--footer-sample" data-testid="ui-standard-card-footer-split">
          <div className="ui-standards-qcard__section ui-standards-qcard__section--tight">
            <div className="ui-standards-qcard__title">Void sale?</div>
            <div className="ui-standards-qcard__subtitle">Order #9021 · ₱480.00</div>
            <p className="ui-standards-qcard__note m-0">Split footer colors separate cancel from confirm.</p>
          </div>
          <div className="ui-standards-qcard__footer ui-standards-qcard__footer--split" data-testid="ui-standard-card-footer-split-bar">
            <button type="button" className="ui-standards-qcard__footer-split-btn ui-standards-qcard__footer-split-btn--danger">
              Void
            </button>
            <button type="button" className="ui-standards-qcard__footer-split-btn ui-standards-qcard__footer-split-btn--success">
              Keep
            </button>
          </div>
        </article>
      </div>
    </div>
  );
}
