import type { Language } from "@/lib/preferences/ui-preferences";

function localeTag(language: Language): string {
  return language === "fil-PH" ? "fil-PH" : "en-PH";
}

export function formatDate(value: Date, language: Language): string {
  return new Intl.DateTimeFormat(localeTag(language), {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(value);
}

export function formatNumber(value: number, language: Language): string {
  return new Intl.NumberFormat(localeTag(language)).format(value);
}

export function formatCurrency(value: number, language: Language, currency = "PHP"): string {
  return new Intl.NumberFormat(localeTag(language), {
    style: "currency",
    currency,
  }).format(value);
}
