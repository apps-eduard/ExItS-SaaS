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
  | "status.preview"
  | "pwa.updateAvailable"
  | "pwa.refresh"
  | "auth.checking"
  | "auth.signInTitle"
  | "auth.signInSubtitle"
  | "auth.username"
  | "auth.password"
  | "auth.showPassword"
  | "auth.hidePassword"
  | "auth.submit"
  | "auth.invalidCredentials"
  | "auth.signInFailed"
  | "auth.rateLimited"
  | "auth.signedInAs"
  | "auth.signOut";

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
    "Placeholder content only. Workspace selection, PIN, and selling are not in this package.",
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
  "pwa.updateAvailable": "New version available",
  "pwa.refresh": "Refresh",
  "auth.checking": "Checking session",
  "auth.signInTitle": "Sign in",
  "auth.signInSubtitle": "Use your ExItS account. The browser keeps an HttpOnly session cookie.",
  "auth.username": "Email or username",
  "auth.password": "Password",
  "auth.showPassword": "Show password",
  "auth.hidePassword": "Hide password",
  "auth.submit": "Sign in",
  "auth.invalidCredentials": "Email or password is incorrect.",
  "auth.signInFailed": "Unable to sign in. Try again.",
  "auth.rateLimited": "Too many sign-in attempts. Try again later.",
  "auth.signedInAs": "Signed in as",
  "auth.signOut": "Sign out",
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
    "Placeholder lamang. Hindi kasama sa package na ito ang pagpili ng workspace, PIN, at pagbebenta.",
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
  "pwa.updateAvailable": "May bagong bersyon",
  "pwa.refresh": "I-refresh",
  "auth.checking": "Tinitingnan ang session",
  "auth.signInTitle": "Mag-sign in",
  "auth.signInSubtitle":
    "Gamitin ang ExItS account. HttpOnly session cookie ang iniingatan ng browser.",
  "auth.username": "Email o username",
  "auth.password": "Password",
  "auth.showPassword": "Ipakita ang password",
  "auth.hidePassword": "Itago ang password",
  "auth.submit": "Mag-sign in",
  "auth.invalidCredentials": "Mali ang email o password.",
  "auth.signInFailed": "Hindi makapag-sign in. Subukan ulit.",
  "auth.rateLimited": "Sobra na ang mga pagtatangka. Subukan ulit mamaya.",
  "auth.signedInAs": "Naka-sign in bilang",
  "auth.signOut": "Mag-sign out",
};

export const catalogs: Record<"en" | "fil-PH", Record<MessageKey, string>> = {
  en,
  "fil-PH": filPH,
};
