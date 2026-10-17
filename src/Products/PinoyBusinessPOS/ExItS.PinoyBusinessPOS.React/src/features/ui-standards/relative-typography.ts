/** Relative typography adjustments. FW/FZ tokens change the current value by a percent of itself. */

export const FONT_WEIGHT_MIN = 100;
export const FONT_WEIGHT_MAX = 900;
export const FONT_WEIGHT_MIN_STEP = 10;

export const SIDENAV_BASE_FONT_WEIGHT = 500;
export const SIDENAV_BASE_FONT_SIZE = "0.875rem";

export type TypographyProperty = "font-weight" | "font-size" | "both";

export type TypographySnapshot = {
  fontWeight: number;
  fontSize: string;
};

export type TypographyAdjustment = {
  property: TypographyProperty;
  /** Signed percent. +1 is FW+1 / FZ+1. -1 is FW-1 / FZ-1. */
  percent: number;
};

export const SIDENAV_BASE_TYPOGRAPHY: TypographySnapshot = {
  fontWeight: SIDENAV_BASE_FONT_WEIGHT,
  fontSize: SIDENAV_BASE_FONT_SIZE,
};

const SIZE_PATTERN = /^(-?(?:\d+\.?\d*|\.\d+))([a-z%]*)$/i;
const TOKEN_PATTERN = /^(FW|FZ)\s*([+-])\s*(\d+(?:\.\d+)?)$/i;

export function clampFontWeight(weight: number): number {
  return Math.min(FONT_WEIGHT_MAX, Math.max(FONT_WEIGHT_MIN, weight));
}

/**
 * FW+n / FW-n.
 * The change is n% of the current weight, and at least ±10, then rounded and clamped to 100–900.
 */
export function adjustFontWeight(current: number, percent: number): number {
  if (!Number.isFinite(current) || !Number.isFinite(percent) || percent === 0) {
    return clampFontWeight(Math.round(Number.isFinite(current) ? current : FONT_WEIGHT_MIN));
  }
  const delta = (current * percent) / 100;
  const direction = delta < 0 ? -1 : 1;
  const magnitude = Math.max(Math.abs(delta), FONT_WEIGHT_MIN_STEP);
  return clampFontWeight(Math.round(current + direction * magnitude));
}

export function parseCssSize(input: string): { value: number; unit: string } | null {
  const match = SIZE_PATTERN.exec(input.trim());
  if (!match) return null;
  const value = Number(match[1]);
  if (!Number.isFinite(value)) return null;
  return { value, unit: match[2] ?? "" };
}

/** Trim binary-float noise while keeping meaningful decimals (14.14, 0.88375). */
export function formatCssNumber(value: number): string {
  if (!Number.isFinite(value)) return "0";
  const rounded = Math.round((value + Number.EPSILON) * 1e6) / 1e6;
  return rounded.toFixed(6).replace(/\.?0+$/, "");
}

export function formatCssSize(value: number, unit: string): string {
  return `${formatCssNumber(value)}${unit}`;
}

/** FZ+n / FZ-n. Pure percent of the current size. The unit is kept. */
export function adjustFontSize(current: string, percent: number): string {
  const parsed = parseCssSize(current);
  if (!parsed || !Number.isFinite(percent) || percent === 0) return current;
  return formatCssSize(parsed.value * (1 + percent / 100), parsed.unit);
}

export function applyTypographyAdjustment(
  current: TypographySnapshot,
  adjustment: TypographyAdjustment,
): TypographySnapshot {
  if (adjustment.property === "font-weight") {
    return { ...current, fontWeight: adjustFontWeight(current.fontWeight, adjustment.percent) };
  }
  return { ...current, fontSize: adjustFontSize(current.fontSize, adjustment.percent) };
}

export function parseTypographyToken(token: string): TypographyAdjustment | null {
  const match = TOKEN_PATTERN.exec(token.trim());
  if (!match) return null;
  const magnitude = Number(match[3]);
  if (!Number.isFinite(magnitude)) return null;
  const percent = match[2] === "-" ? -magnitude : magnitude;
  return {
    property: match[1].toUpperCase() === "FW" ? "font-weight" : "font-size",
    percent,
  };
}

export function applyTypographyToken(current: TypographySnapshot, token: string): TypographySnapshot {
  const adjustment = parseTypographyToken(token);
  return adjustment ? applyTypographyAdjustment(current, adjustment) : current;
}

export function typographyToken(property: TypographyProperty, percent: number): string {
  const prefix = property === "font-weight" ? "FW" : property === "font-size" ? "FZ" : "FWZ";
  const sign = percent < 0 ? "-" : "+";
  return `${prefix}${sign}${formatCssNumber(Math.abs(percent))}`;
}

/** Unchanged typography copies as `ext typography`. Repeated presses add into one FW, FZ, or FWZ total. */
export function typographyCursorCommand(
  fontWeightPercent: number,
  fontSizePercent: number,
  bothPercent = 0,
): string {
  const parts = ["ext typography"];
  if (fontWeightPercent !== 0) parts.push(typographyToken("font-weight", fontWeightPercent));
  if (fontSizePercent !== 0) parts.push(typographyToken("font-size", fontSizePercent));
  if (bothPercent !== 0) parts.push(typographyToken("both", bothPercent));
  return parts.join(" ");
}

/** Apply the summed percents once against the original base. FWZ moves weight and size together. */
export function typographyFromOriginal(
  fontWeightPercent: number,
  fontSizePercent: number,
  bothPercent = 0,
): TypographySnapshot {
  return {
    fontWeight: adjustFontWeight(SIDENAV_BASE_TYPOGRAPHY.fontWeight, fontWeightPercent + bothPercent),
    fontSize: adjustFontSize(SIDENAV_BASE_TYPOGRAPHY.fontSize, fontSizePercent + bothPercent),
  };
}
