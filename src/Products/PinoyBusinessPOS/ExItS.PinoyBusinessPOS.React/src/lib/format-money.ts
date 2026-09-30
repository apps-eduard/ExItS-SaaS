export function formatPeso(amount: number): string {
  return new Intl.NumberFormat("en-PH", {
    style: "currency",
    currency: "PHP",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amount);
}

/** Grouped money + currency code (e.g. 1,124.00 PHP). */
export function formatMoneyWithCode(amount: number, currencyCode = "PHP"): string {
  const code = currencyCode.trim() || "PHP";
  const grouped = new Intl.NumberFormat("en-PH", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
    useGrouping: true,
  }).format(Number.isFinite(amount) ? amount : 0);
  return `${grouped} ${code}`;
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
