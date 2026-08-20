export type MessageKey =
  | "app.name"
  | "app.skipToContent"
  | "foundation.title"
  | "foundation.lede"
  | "foundation.next"
  | "foundation.scope"
  | "locale.label"
  | "locale.en"
  | "locale.filPH"
  | "theme.label"
  | "theme.system"
  | "theme.light"
  | "theme.dark"
  | "status.foundation"
  | "empty.title"
  | "empty.detail"
  | "error.title"
  | "error.detail"
  | "loading.label"
  | "connectivity.online"
  | "connectivity.offlineTitle"
  | "connectivity.offlineDetail"
  | "notFound.title"
  | "notFound.detail"
  | "notFound.home";

export const en: Record<MessageKey, string> = {
  "app.name": "Pinoy Business POS",
  "app.skipToContent": "Skip to content",
  "foundation.title": "Pinoy Business POS",
  "foundation.lede": "React client foundation",
  "foundation.next": "PWA foundation will be added next",
  "foundation.scope":
    "This host is the future Mobile Client candidate. Authentication, workspace, selling, and offline finance are not in this package.",
  "locale.label": "Language",
  "locale.en": "English",
  "locale.filPH": "Filipino",
  "theme.label": "Theme",
  "theme.system": "System",
  "theme.light": "Light",
  "theme.dark": "Dark",
  "status.foundation": "Foundation",
  "empty.title": "Nothing here yet",
  "empty.detail": "Product screens will arrive in later authorized packages.",
  "error.title": "Something went wrong",
  "error.detail": "Reload the page. No business data is stored in this foundation.",
  "loading.label": "Loading",
  "connectivity.online": "Online",
  "connectivity.offlineTitle": "You're offline",
  "connectivity.offlineDetail": "Reconnect to continue.",
  "notFound.title": "Page not found",
  "notFound.detail": "That route is not part of this foundation.",
  "notFound.home": "Back to foundation",
};

export const filPH: Record<MessageKey, string> = {
  "app.name": "Pinoy Business POS",
  "app.skipToContent": "Laktawan papunta sa nilalaman",
  "foundation.title": "Pinoy Business POS",
  "foundation.lede": "Pundasyon ng React client",
  "foundation.next": "Idadagdag ang PWA foundation sa susunod",
  "foundation.scope":
    "Ito ang candidate na Mobile Client sa hinaharap. Wala pang authentication, workspace, pagbebenta, o offline finance sa package na ito.",
  "locale.label": "Wika",
  "locale.en": "English",
  "locale.filPH": "Filipino",
  "theme.label": "Tema",
  "theme.system": "System",
  "theme.light": "Light",
  "theme.dark": "Dark",
  "status.foundation": "Pundasyon",
  "empty.title": "Wala pa rito",
  "empty.detail": "Darating ang product screens sa susunod na awtorisadong package.",
  "error.title": "May naganap na problema",
  "error.detail": "I-reload ang page. Walang business data sa foundation na ito.",
  "loading.label": "Naglo-load",
  "connectivity.online": "Online",
  "connectivity.offlineTitle": "Wala kang internet",
  "connectivity.offlineDetail": "Kumonekta ulit para magpatuloy.",
  "notFound.title": "Hindi nahanap ang page",
  "notFound.detail": "Hindi kasama ang route na iyon sa foundation na ito.",
  "notFound.home": "Bumalik sa foundation",
};

export const catalogs = {
  en,
  "fil-PH": filPH,
} as const;
