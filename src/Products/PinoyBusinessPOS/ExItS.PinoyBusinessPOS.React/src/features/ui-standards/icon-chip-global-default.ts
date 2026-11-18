/**
 * Locked Lucide icon chrome for the UI Standards Icon playground.
 * Two appearance tracks: generic Soft / Soft Glass chips and Org EHG–suffixed counterparts.
 * Default lock: generic `soft-primary` (not EHG).
 */

import {
  ICON_CHIP_DEFAULT_GLYPH_ID,
  type IconChipGlyphId,
} from "@/features/ui-standards/icon-chip-glyphs";

export const ICON_CHIP_LIBRARY = "lucide" as const;

export type IconChipAppearance =
  | "bare"
  | "soft-primary"
  | "soft-fill"
  | "soft-glass"
  | "outline"
  | "ghost"
  | "soft-primary-ehg"
  | "soft-fill-ehg"
  | "soft-glass-ehg"
  | "outline-ehg"
  | "ghost-ehg";

export type IconChipAppearanceCatalogEntry = {
  id: IconChipAppearance;
  label: string;
};

/** Appearance dropdown — generic track first, then Org EHG counterparts. */
export const ICON_CHIP_APPEARANCE_CATALOG: ReadonlyArray<IconChipAppearanceCatalogEntry> = [
  { id: "bare", label: "Bare" },
  { id: "soft-primary", label: "Soft primary" },
  { id: "soft-fill", label: "Soft fill" },
  { id: "soft-glass", label: "Soft glass" },
  { id: "outline", label: "Outline" },
  { id: "ghost", label: "Ghost" },
  { id: "soft-primary-ehg", label: "Soft primary EHG" },
  { id: "soft-fill-ehg", label: "Soft fill EHG" },
  { id: "soft-glass-ehg", label: "Soft glass EHG" },
  { id: "outline-ehg", label: "Outline EHG" },
  { id: "ghost-ehg", label: "Ghost EHG" },
];

export function iconChipAppearanceIsEhg(appearance: IconChipAppearance): boolean {
  return appearance.endsWith("-ehg");
}

export type IconChipShape = "squircle" | "disc" | "rounded" | "square";

export type IconChipSize = "xs" | "sm" | "md" | "lg" | "xl";

export type IconChipTone = "primary" | "success" | "warn" | "danger" | "info" | "muted";

export type IconChipStroke = 1.5 | 1.75 | 2 | 2.5;

export type IconChipPadding = "tight" | "default" | "loose";

export type IconChipBorderWidth = "thin" | "medium" | "thick";

export type IconChipMarkStyle = "stroke" | "filled";

/** Icon playground — right preview column host surfaces. */
export type IconChipPreviewHost = "action" | "button" | "card" | "kpi" | "pennant" | "list-row";

export type IconChipPreviewHostCatalogEntry = {
  id: IconChipPreviewHost;
  label: string;
};

export const ICON_CHIP_PREVIEW_HOST_CATALOG: ReadonlyArray<IconChipPreviewHostCatalogEntry> = [
  { id: "action", label: "Action" },
  { id: "button", label: "Button" },
  { id: "card", label: "Card" },
  { id: "kpi", label: "KPI" },
  { id: "pennant", label: "Pennant" },
  { id: "list-row", label: "List row" },
];

export const ICON_CHIP_DEFAULT_PREVIEW_HOST: IconChipPreviewHost = "action";

export function iconChipPreviewHostTestId(host: IconChipPreviewHost): string {
  return `ui-standards-icon-preview-host-${host}`;
}

export type IconChipCommandState = {
  appearance: IconChipAppearance;
  shape: IconChipShape;
  /** Chip / container size ladder. */
  size: IconChipSize;
  /** Lucide mark (glyph) size — independent from chip container. */
  iconSize: IconChipSize;
  tone: IconChipTone;
  stroke: IconChipStroke;
  glyphId: IconChipGlyphId;
  padding: IconChipPadding;
  borderWidth: IconChipBorderWidth;
  shadow: boolean;
  markStyle: IconChipMarkStyle;
};

/** Default playground recipe: soft primary wash + squircle + md. */
export const ICON_CHIP_DEFAULT_COMMAND = "ext icon soft-primary squircle md" as const;

export const ICON_CHIP_DEFAULT_APPEARANCE: IconChipAppearance = "soft-primary";
export const ICON_CHIP_DEFAULT_SHAPE: IconChipShape = "squircle";
export const ICON_CHIP_DEFAULT_SIZE: IconChipSize = "md";
export const ICON_CHIP_DEFAULT_ICON_SIZE: IconChipSize = "md";
export const ICON_CHIP_DEFAULT_TONE: IconChipTone = "primary";
export const ICON_CHIP_DEFAULT_STROKE: IconChipStroke = 2;
export const ICON_CHIP_DEFAULT_PADDING: IconChipPadding = "default";
export const ICON_CHIP_DEFAULT_BORDER: IconChipBorderWidth = "thin";
export const ICON_CHIP_DEFAULT_SHADOW = true;
export const ICON_CHIP_DEFAULT_MARK_STYLE: IconChipMarkStyle = "stroke";

export const ICON_CHIP_SIZE_XS = "1.375rem" as const;
export const ICON_CHIP_SIZE_SM = "1.625rem" as const;
export const ICON_CHIP_SIZE_MD = "2rem" as const;
export const ICON_CHIP_SIZE_LG = "2.5rem" as const;
export const ICON_CHIP_SIZE_XL = "3rem" as const;

export const ICON_CHIP_GLYPH_XS = "0.8rem" as const;
export const ICON_CHIP_GLYPH_SM = "0.9rem" as const;
export const ICON_CHIP_GLYPH_MD = "1.05rem" as const;
export const ICON_CHIP_GLYPH_LG = "1.25rem" as const;
export const ICON_CHIP_GLYPH_XL = "1.45rem" as const;

/** @deprecated Use ICON_CHIP_DEFAULT_STROKE */
export const ICON_CHIP_STROKE = ICON_CHIP_DEFAULT_STROKE;

export const ICON_CHIP_STROKE_OPTIONS: ReadonlyArray<IconChipStroke> = [1.5, 1.75, 2, 2.5];

/** Org EHG card glass icon-chip squircle (reference). */
export const ICON_CHIP_SQUIRCLE_RADIUS = "0.55rem" as const;

/** Soft control shape (rounded icon chip). */
export const ICON_CHIP_ROUNDED_RADIUS = "0.6875rem" as const;

/** Org EHG QA icon-soft-fill squircle (reference). */
export const ICON_CHIP_SOFT_FILL_RADIUS = "0.65rem" as const;

export const ICON_CHIP_SQUARE_RADIUS = "0.25rem" as const;

export const ICON_CHIP_SOFT_PRIMARY_MIX_PCT = 16 as const;
export const ICON_CHIP_SOFT_PRIMARY_MIX_PCT_DARK = 22 as const;

export const ICON_CHIP_OUTLINE_MIX_PCT = 8 as const;
export const ICON_CHIP_OUTLINE_BORDER_MIX_PCT = 35 as const;

export const ICON_CHIP_DEFAULTS: IconChipCommandState = {
  appearance: ICON_CHIP_DEFAULT_APPEARANCE,
  shape: ICON_CHIP_DEFAULT_SHAPE,
  size: ICON_CHIP_DEFAULT_SIZE,
  iconSize: ICON_CHIP_DEFAULT_ICON_SIZE,
  tone: ICON_CHIP_DEFAULT_TONE,
  stroke: ICON_CHIP_DEFAULT_STROKE,
  glyphId: ICON_CHIP_DEFAULT_GLYPH_ID,
  padding: ICON_CHIP_DEFAULT_PADDING,
  borderWidth: ICON_CHIP_DEFAULT_BORDER,
  shadow: ICON_CHIP_DEFAULT_SHADOW,
  markStyle: ICON_CHIP_DEFAULT_MARK_STYLE,
};

function strokeToken(stroke: IconChipStroke): string {
  return `stroke-${String(stroke).replace(".", "-")}`;
}

/** Build the copyable `ext icon …` command; non-default knobs only after fixed tokens. */
export function iconChipCursorCommand(state: IconChipCommandState): string {
  const parts = ["ext", "icon", state.appearance];
  if (state.appearance !== "bare") {
    parts.push(state.shape);
  }
  parts.push(state.size);

  if (state.iconSize !== state.size || state.iconSize !== ICON_CHIP_DEFAULT_ICON_SIZE) {
    parts.push(`icon-${state.iconSize}`);
  }

  if (state.tone !== ICON_CHIP_DEFAULT_TONE) {
    parts.push(state.tone);
  }
  if (state.stroke !== ICON_CHIP_DEFAULT_STROKE) {
    parts.push(strokeToken(state.stroke));
  }
  if (state.glyphId !== ICON_CHIP_DEFAULT_GLYPH_ID) {
    parts.push(`mark-${state.glyphId}`);
  }
  if (state.padding !== ICON_CHIP_DEFAULT_PADDING) {
    parts.push(`padding-${state.padding}`);
  }
  if (state.borderWidth !== ICON_CHIP_DEFAULT_BORDER) {
    parts.push(`border-${state.borderWidth}`);
  }
  if (!state.shadow) {
    parts.push("no-shadow");
  }
  if (state.markStyle === "filled") {
    parts.push("glyph-filled");
  }

  return parts.join(" ");
}

export function iconChipSizeToken(size: IconChipSize): string {
  if (size === "xs") return ICON_CHIP_SIZE_XS;
  if (size === "sm") return ICON_CHIP_SIZE_SM;
  if (size === "lg") return ICON_CHIP_SIZE_LG;
  if (size === "xl") return ICON_CHIP_SIZE_XL;
  return ICON_CHIP_SIZE_MD;
}

export function iconChipGlyphSizeToken(size: IconChipSize): string {
  if (size === "xs") return ICON_CHIP_GLYPH_XS;
  if (size === "sm") return ICON_CHIP_GLYPH_SM;
  if (size === "lg") return ICON_CHIP_GLYPH_LG;
  if (size === "xl") return ICON_CHIP_GLYPH_XL;
  return ICON_CHIP_GLYPH_MD;
}