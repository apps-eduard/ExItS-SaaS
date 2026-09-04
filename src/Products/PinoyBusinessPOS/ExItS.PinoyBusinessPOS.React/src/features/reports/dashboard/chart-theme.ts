/** Theme helpers for Recharts — CSS-variable aware, Capacitor/PWA-safe (SVG only). */

const FALLBACK = {
  primary: "#166534",
  success: "#166534",
  warning: "#92400e",
  danger: "#b42318",
  muted: "#64748b",
  text: "#0f172a",
  border: "#e2e8f0",
  surface: "#ffffff",
  gcash: "#0ea5e9",
  utang: "#c2410c",
  other: "#64748b",
} as const;

function cssVar(name: string, fallback: string): string {
  if (typeof window === "undefined" || typeof getComputedStyle !== "function") {
    return fallback;
  }
  const value = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
  return value || fallback;
}

export function readDashboardChartTheme() {
  return {
    primary: cssVar("--exits-primary", FALLBACK.primary),
    success: cssVar("--exits-success", FALLBACK.success),
    warning: cssVar("--exits-warning", FALLBACK.warning),
    danger: cssVar("--exits-danger", FALLBACK.danger),
    muted: cssVar("--exits-text-muted", FALLBACK.muted),
    text: cssVar("--exits-text", FALLBACK.text),
    border: cssVar("--exits-border", FALLBACK.border),
    surface: cssVar("--exits-surface", FALLBACK.surface),
    gcash: FALLBACK.gcash,
    utang: FALLBACK.utang,
    other: FALLBACK.other,
    grid: "color-mix(in srgb, var(--exits-border) 70%, transparent)",
  };
}

export function paymentMethodColor(method: string, theme = readDashboardChartTheme()): string {
  const normalized = method.trim().toLowerCase();
  if (normalized === "cash") {
    return theme.primary;
  }
  if (normalized === "manualgcash" || normalized === "gcash") {
    return theme.gcash;
  }
  if (normalized === "utang") {
    return theme.utang;
  }
  return theme.other;
}

export const CHART_INTRO_MS = 700;
export const CHART_UPDATE_MS = 450;
export const KPI_COUNT_MS = 650;
