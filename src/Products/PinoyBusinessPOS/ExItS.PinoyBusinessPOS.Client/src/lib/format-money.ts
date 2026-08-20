export function formatPeso(amount: number): string {
  return new Intl.NumberFormat("en-PH", {
    style: "currency",
    currency: "PHP",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amount);
}

export function formatCartSummary(lineCount: number, subtotal: number): string {
  const countLabel = lineCount === 1 ? "1 item" : `${lineCount} items`;
  return `${countLabel} · ${formatPeso(subtotal)}`;
}
