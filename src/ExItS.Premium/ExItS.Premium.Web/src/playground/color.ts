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
