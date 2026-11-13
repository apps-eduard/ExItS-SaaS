export function toColorText(value: string) {
  const match = value.match(/rgba?\(\s*([\d.]+)[,\s]+([\d.]+)[,\s]+([\d.]+)(?:[,\s/]+([\d.]+))?\s*\)/);
  if (!match) return value.trim();
  const alpha = match[4] === undefined ? 1 : Number(match[4]);
  if (alpha < 1) return value.replace(/\s+/g, " ");
  const hex = [match[1], match[2], match[3]]
    .map((channel) => Math.round(Number(channel)).toString(16).padStart(2, "0"))
    .join("");
  return `#${hex}`;
}

/** Relative luminance for choosing light/dark ink on a solid fill. */
export function relativeLuminance(hex: string) {
  const match = hex.trim().match(/^#?([0-9a-f]{6})$/i);
  if (!match) return 0.5;
  const channels = [0, 2, 4].map((index) => {
    const channel = Number.parseInt(match[1].slice(index, index + 2), 16) / 255;
    return channel <= 0.03928 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4;
  });
  return 0.2126 * channels[0] + 0.7152 * channels[1] + 0.0722 * channels[2];
}

export function inkForHex(hex: string) {
  return relativeLuminance(hex) > 0.55 ? "#111827" : "#f8fafc";
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
