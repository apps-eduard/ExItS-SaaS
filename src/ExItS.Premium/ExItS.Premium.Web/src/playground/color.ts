/** Normalize authored or computed color strings for display/copy. */
export function toColorText(value: string) {
  const trimmed = value.trim().replace(/\s+/g, " ");
  if (!trimmed) return "";
  if (/^oklch\(/i.test(trimmed) || /^oklab\(/i.test(trimmed)) {
    return trimmed;
  }
  const srgb = trimmed.match(/^color\(\s*srgb\s+([\d.]+)\s+([\d.]+)\s+([\d.]+)(?:\s*\/\s*([\d.]+))?\s*\)$/i);
  if (srgb) {
    const alpha = srgb[4] === undefined ? 1 : Number(srgb[4]);
    if (alpha < 1) return trimmed;
    const hex = [srgb[1], srgb[2], srgb[3]]
      .map((channel) => Math.round(Number(channel) * 255).toString(16).padStart(2, "0"))
      .join("");
    return `#${hex}`;
  }
  const match = trimmed.match(/rgba?\(\s*([\d.]+)[,\s]+([\d.]+)[,\s]+([\d.]+)(?:[,\s/]+([\d.]+))?\s*\)/);
  if (!match) return trimmed;
  const alpha = match[4] === undefined ? 1 : Number(match[4]);
  if (alpha < 1) return trimmed;
  const hex = [match[1], match[2], match[3]]
    .map((channel) => Math.round(Number(channel)).toString(16).padStart(2, "0"))
    .join("");
  return `#${hex}`;
}

function srgbChannel(value: number) {
  const scaled = value / 255;
  return scaled <= 0.04045 ? scaled / 12.92 : ((scaled + 0.055) / 1.055) ** 2.4;
}

function relativeLuminanceFromRgb(r: number, g: number, b: number) {
  return 0.2126 * srgbChannel(r) + 0.7152 * srgbChannel(g) + 0.0722 * srgbChannel(b);
}

/** Approximate OKLCH → linear sRGB for contrast checks (CSS Color Level 4). */
export function oklchToSrgb(L: number, C: number, h: number) {
  const a = C * Math.cos((h * Math.PI) / 180);
  const b = C * Math.sin((h * Math.PI) / 180);
  const l_ = L + 0.3963377774 * a + 0.2158037573 * b;
  const m_ = L - 0.1055613458 * a - 0.0638541728 * b;
  const s_ = L - 0.0894841775 * a - 1.291485548 * b;
  const l = l_ ** 3;
  const m = m_ ** 3;
  const s = s_ ** 3;
  const r = +4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s;
  const g = -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s;
  const bl = -0.0041960863 * l - 0.7034186147 * m + 1.707614701 * s;
  const encode = (channel: number) => {
    const clipped = Math.min(1, Math.max(0, channel));
    return clipped <= 0.0031308 ? 12.92 * clipped : 1.055 * clipped ** (1 / 2.4) - 0.055;
  };
  return {
    r: Math.round(encode(r) * 255),
    g: Math.round(encode(g) * 255),
    b: Math.round(encode(bl) * 255),
  };
}

export function parseColorChannels(value: string): { r: number; g: number; b: number } | null {
  const trimmed = value.trim().toLowerCase();
  const hex = trimmed.match(/^#([0-9a-f]{6})$/i);
  if (hex) {
    return {
      r: Number.parseInt(hex[1].slice(0, 2), 16),
      g: Number.parseInt(hex[1].slice(2, 4), 16),
      b: Number.parseInt(hex[1].slice(4, 6), 16),
    };
  }
  const rgb = trimmed.match(/^rgba?\(\s*([\d.]+)[,\s]+([\d.]+)[,\s]+([\d.]+)/i);
  if (rgb) {
    return { r: Number(rgb[1]), g: Number(rgb[2]), b: Number(rgb[3]) };
  }
  const oklch = trimmed.match(/^oklch\(\s*([\d.]+)%?\s+([\d.]+)\s+([\d.]+)/i);
  if (oklch) {
    const L = Number(oklch[1]) > 1 ? Number(oklch[1]) / 100 : Number(oklch[1]);
    return oklchToSrgb(L, Number(oklch[2]), Number(oklch[3]));
  }
  return null;
}

/** Relative luminance for choosing light/dark ink on a solid fill. */
export function relativeLuminance(color: string) {
  const channels = parseColorChannels(color);
  if (!channels) return 0.5;
  return relativeLuminanceFromRgb(channels.r, channels.g, channels.b);
}

function contrastRatio(foreground: string, background: string) {
  const lighter = Math.max(relativeLuminance(foreground), relativeLuminance(background));
  const darker = Math.min(relativeLuminance(foreground), relativeLuminance(background));
  return (lighter + 0.05) / (darker + 0.05);
}

/** Pick the ink with the stronger WCAG contrast (handles saturated OKLCH fills). */
export function inkForHex(color: string) {
  const dark = "#111827";
  const light = "#f8fafc";
  return contrastRatio(dark, color) >= contrastRatio(light, color) ? dark : light;
}

export function inkForColor(color: string) {
  return inkForHex(color);
}

/** Prefer leaf authored oklch/hex; resolve var()/color-mix via computed paint. */
export function resolveSwatchValue(tokenValue: string, computedBackground: string) {
  const raw = tokenValue.trim();
  const authored = toColorText(raw);
  const isLeaf =
    /^oklch\(/i.test(authored) ||
    (/^#/i.test(authored) && !/^var\(/i.test(raw) && !/^color-mix\(/i.test(raw));
  if (isLeaf) return authored;

  const fromPaint = toColorText(computedBackground);
  if (fromPaint && !/^rgba?\(\s*0[,\s]+0[,\s]+0(?:[,\s/]+0(?:\.0+)?)?\s*\)$/i.test(fromPaint)) {
    return fromPaint;
  }
  if (/^rgba?\(/i.test(authored) || /^#/i.test(authored)) return authored;
  return authored || fromPaint;
}

export async function copyText(value: string) {
  try {
    await navigator.clipboard.writeText(value);
    return true;
  } catch {
    const field = document.createElement("textarea");
    field.value = value;
    field.setAttribute("readonly", "");
    field.tabIndex = -1;
    field.style.position = "fixed";
    field.style.opacity = "0";
    document.body.append(field);
    field.focus();
    field.select();
    const ok = document.execCommand("copy");
    field.remove();
    return ok;
  }
}
