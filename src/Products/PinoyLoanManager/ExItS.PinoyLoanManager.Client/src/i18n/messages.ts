export type MessageKey =
  | "app.name"
  | "app.skipToContent"
  | "home.title"
  | "home.tagline"
  | "locale.label"
  | "locale.en"
  | "locale.filPH"
  | "theme.label"
  | "theme.system"
  | "theme.light"
  | "theme.dark";

export const en: Record<MessageKey, string> = {
  "app.name": "Pinoy Loan Manager",
  "app.skipToContent": "Skip to content",
  "home.title": "Pinoy Loan Manager",
  "home.tagline": "Lending operations for your organization.",
  "locale.label": "Language",
  "locale.en": "English",
  "locale.filPH": "Filipino",
  "theme.label": "Theme",
  "theme.system": "System",
  "theme.light": "Light",
  "theme.dark": "Dark",
};

export const filPH: Record<MessageKey, string> = {
  "app.name": "Pinoy Loan Manager",
  "app.skipToContent": "Laktawan papunta sa nilalaman",
  "home.title": "Pinoy Loan Manager",
  "home.tagline": "Mga operasyon ng pagpapautang para sa iyong organisasyon.",
  "locale.label": "Wika",
  "locale.en": "English",
  "locale.filPH": "Filipino",
  "theme.label": "Tema",
  "theme.system": "System",
  "theme.light": "Light",
  "theme.dark": "Dark",
};

export const catalogs: Record<"en" | "fil-PH", Record<MessageKey, string>> = {
  en,
  "fil-PH": filPH,
};
