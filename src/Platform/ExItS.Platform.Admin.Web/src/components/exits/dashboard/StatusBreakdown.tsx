export type StatusBreakdownItem = {
  key: string;
  label: string;
  value: string;
};

export function formatStatusLine(items: StatusBreakdownItem[]): string {
  return items.map((item) => `${item.value} ${item.label.toLowerCase()}`).join(" · ");
}
