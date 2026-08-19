export type MessageKey =
  | "app.name"
  | "app.skipToContent"
  | "shell.settings"
  | "shell.back"
  | "home.title"
  | "home.tagline"
  | "home.welcome"
  | "empty.title"
  | "empty.body"
  | "loading.label"
  | "error.title"
  | "error.body"
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
  "shell.settings": "Settings",
  "shell.back": "Back",
  "home.title": "ExItS Mobile",
  "home.tagline":
    "Your business and personal ExItS experience, designed for phone, tablet, and desktop.",
  "home.welcome": "Welcome,",
  "empty.title": "Nothing to show yet",
  "empty.body": "There is nothing to display right now.",
  "loading.label": "Loading",
  "error.title": "Something went wrong",
  "error.body": "Unable to complete this operation.",
  "error.reset": "Try again",
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
  "pwa.updateAvailable": "New version available",
  "pwa.refresh": "Refresh",
  "auth.checking": "Checking session",
  "auth.signInTitle": "Sign in",
  "auth.signInSubtitle": "Use your ExItS account.",
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
  "shell.settings": "Mga setting",
  "shell.back": "Bumalik",
  "home.title": "ExItS Mobile",
  "home.tagline":
    "Ang iyong personal at business na karanasan sa ExItS, para sa telepono, tablet, at desktop.",
  "home.welcome": "Maligayang pagdating,",
  "empty.title": "Wala pang ipapakita",
  "empty.body": "Walang ipinapakitang listahan sa ngayon.",
  "loading.label": "Naglo-load",
  "error.title": "May nangyaring mali",
  "error.body": "Hindi natapos ang operasyon.",
  "error.reset": "Subukan ulit",
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
  "pwa.updateAvailable": "May bagong bersyon",
  "pwa.refresh": "I-refresh",
  "auth.checking": "Tinitingnan ang session",
  "auth.signInTitle": "Mag-sign in",
  "auth.signInSubtitle": "Gamitin ang iyong ExItS account.",
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
