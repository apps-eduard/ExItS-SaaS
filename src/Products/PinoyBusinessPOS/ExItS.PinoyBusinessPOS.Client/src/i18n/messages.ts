export type MessageKey =
  | "app.name"
  | "app.skipToContent"
  | "shell.productPlaceholder"
  | "shell.contextPlaceholder"
  | "shell.preview"
  | "nav.home"
  | "nav.appearance"
  | "foundation.title"
  | "foundation.subtitle"
  | "foundation.intro"
  | "foundation.notLiveNotice"
  | "foundation.opsPreviewTitle"
  | "foundation.opsPreviewHint"
  | "foundation.sampleAmountLabel"
  | "foundation.densityNote"
  | "foundation.statesTitle"
  | "empty.title"
  | "empty.body"
  | "loading.label"
  | "error.title"
  | "error.body"
  | "error.simulate"
  | "error.reset"
  | "diagnostics.copy"
  | "diagnostics.copied"
  | "theme.label"
  | "theme.system"
  | "theme.light"
  | "theme.dark"
  | "locale.label"
  | "locale.en"
  | "locale.filPH"
  | "appearance.title"
  | "appearance.subtitle"
  | "status.foundation"
  | "status.preview";

export const en: Record<MessageKey, string> = {
  "app.name": "ExItS Mobile",
  "app.skipToContent": "Skip to content",
  "shell.productPlaceholder": "Foundation preview",
  "shell.contextPlaceholder": "No workspace selected",
  "shell.preview": "Preview",
  "nav.home": "Home",
  "nav.appearance": "Appearance",
  "foundation.title": "Client foundation",
  "foundation.subtitle": "Visual shell, theme, and language — no live business data.",
  "foundation.intro":
    "This screen proves the Mobile Client host: shared top bar, phone bottom navigation, tablet and desktop layout, and ExItS tokens. It is not a store, not a checkout, and not Platform Admin.",
  "foundation.notLiveNotice":
    "Placeholder content only. Selling, auth, and workspace selection are not in this package.",
  "foundation.opsPreviewTitle": "Compact operations chrome",
  "foundation.opsPreviewHint":
    "Cashier surfaces use compact spacing. Controls stay at least 44 CSS pixels.",
  "foundation.sampleAmountLabel": "Sample total",
  "foundation.densityNote":
    "Personal and forms use Comfortable density. Compact reduces spacing, not touch size.",
  "foundation.statesTitle": "Shared states",
  "empty.title": "Nothing to show yet",
  "empty.body": "Live lists will appear here after later packages. This empty state is shared.",
  "loading.label": "Loading",
  "error.title": "Something went wrong",
  "error.body": "Unable to complete this operation.",
  "error.simulate": "Simulate runtime error",
  "error.reset": "Back to foundation",
  "diagnostics.copy": "Copy",
  "diagnostics.copied": "Copied",
  "theme.label": "Theme",
  "theme.system": "System",
  "theme.light": "Light",
  "theme.dark": "Dark",
  "locale.label": "Language",
  "locale.en": "English",
  "locale.filPH": "Filipino",
  "appearance.title": "Appearance",
  "appearance.subtitle": "Language and theme apply immediately across the client.",
  "status.foundation": "Foundation",
  "status.preview": "Preview",
};

export const filPH: Record<MessageKey, string> = {
  "app.name": "ExItS Mobile",
  "app.skipToContent": "Laktawan papunta sa nilalaman",
  "shell.productPlaceholder": "Preview ng foundation",
  "shell.contextPlaceholder": "Walang napiling workspace",
  "shell.preview": "Preview",
  "nav.home": "Home",
  "nav.appearance": "Hitsura",
  "foundation.title": "Foundation ng client",
  "foundation.subtitle": "Shell, tema, at wika — walang live na datos ng negosyo.",
  "foundation.intro":
    "Pinapatunayan ng screen na ito ang Mobile Client host: shared top bar, bottom navigation sa telepono, layout sa tablet at desktop, at mga token ng ExItS. Hindi ito tindahan, hindi checkout, at hindi Platform Admin.",
  "foundation.notLiveNotice":
    "Placeholder lamang. Hindi kasama sa package na ito ang pagbebenta, auth, at pagpili ng workspace.",
  "foundation.opsPreviewTitle": "Compact na operations chrome",
  "foundation.opsPreviewHint":
    "Masikip ang spacing sa cashier surfaces. Hindi bababa sa 44 CSS pixels ang mga control.",
  "foundation.sampleAmountLabel": "Halimbawang total",
  "foundation.densityNote":
    "Comfortable ang density para sa Personal at mga form. Ang Compact ay nabawasang spacing, hindi maliit na control.",
  "foundation.statesTitle": "Mga shared state",
  "empty.title": "Wala pang ipapakita",
  "empty.body":
    "Lalabas ang live na listahan sa susunod na package. Shared ang empty state na ito.",
  "loading.label": "Naglo-load",
  "error.title": "May nangyaring mali",
  "error.body": "Hindi natapos ang operasyon.",
  "error.simulate": "Simulahin ang runtime error",
  "error.reset": "Bumalik sa foundation",
  "diagnostics.copy": "Kopyahin",
  "diagnostics.copied": "Nakopya",
  "theme.label": "Tema",
  "theme.system": "System",
  "theme.light": "Light",
  "theme.dark": "Dark",
  "locale.label": "Wika",
  "locale.en": "English",
  "locale.filPH": "Filipino",
  "appearance.title": "Hitsura",
  "appearance.subtitle": "Agad na nalalapat ang wika at tema sa buong client.",
  "status.foundation": "Foundation",
  "status.preview": "Preview",
};

export const catalogs: Record<"en" | "fil-PH", Record<MessageKey, string>> = {
  en,
  "fil-PH": filPH,
};
