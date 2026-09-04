export function formatPeso(amount: number): string {
  return new Intl.NumberFormat("en-PH", {
    style: "currency",
    currency: "PHP",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amount);
}

/** Display-only PHP formatting for cash denomination values (₱1,000 / ₱0.25). */
export function formatDenominationCurrency(value: number): string {
  const fractionDigits = Number.isInteger(value) ? 0 : 2;
  return new Intl.NumberFormat("en-PH", {
    style: "currency",
    currency: "PHP",
    currencyDisplay: "narrowSymbol",
    minimumFractionDigits: fractionDigits,
    maximumFractionDigits: 2,
  }).format(value);
}

export function formatCartSummary(lineCount: number, subtotal: number): string {
  const countLabel = lineCount === 1 ? "1 item" : `${lineCount} items`;
  return `${countLabel} · ${formatPeso(subtotal)}`;
}
