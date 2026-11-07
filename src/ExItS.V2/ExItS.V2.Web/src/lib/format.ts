const DEFAULT_LOCALE = "en-PH";

export function formatCurrency(
  value: number,
  locale = DEFAULT_LOCALE,
  currency?: string | null,
): string {
  if (currency && currency.trim()) {
    return new Intl.NumberFormat(locale, {
      style: "currency",
      currency: currency.trim(),
      maximumFractionDigits: 2,
    }).format(value);
  }
  // No authoritative currency code from the Home APIs — use locale number formatting.
  return new Intl.NumberFormat(locale, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
}

export function formatNumber(value: number, locale = DEFAULT_LOCALE): string {
  return new Intl.NumberFormat(locale).format(value);
}

export function formatDate(
  value: string | number | Date,
  locale = DEFAULT_LOCALE,
  options: Intl.DateTimeFormatOptions = {
    dateStyle: "medium",
  },
): string {
  return new Intl.DateTimeFormat(locale, options).format(new Date(value));
}

export function formatDateTime(
  value: string | number | Date,
  locale = DEFAULT_LOCALE,
): string {
  return formatDate(value, locale, {
    dateStyle: "medium",
    timeStyle: "short",
  });
}

export function formatRelativeTime(
  value: string | number | Date,
  locale = DEFAULT_LOCALE,
): string {
  const date = new Date(value);
  const diffMs = date.getTime() - Date.now();
  const absMs = Math.abs(diffMs);
  const rtf = new Intl.RelativeTimeFormat(locale, { numeric: "auto" });

  const minute = 60_000;
  const hour = 60 * minute;
  const day = 24 * hour;

  if (absMs < hour) {
    return rtf.format(Math.round(diffMs / minute), "minute");
  }
  if (absMs < day) {
    return rtf.format(Math.round(diffMs / hour), "hour");
  }
  return rtf.format(Math.round(diffMs / day), "day");
}
