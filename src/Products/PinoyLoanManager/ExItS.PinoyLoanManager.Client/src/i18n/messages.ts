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
  | "theme.dark"
  | "pwa.updateAvailable"
  | "pwa.refresh"
  | "session.loading"
  | "auth.signInTitle"
  | "auth.usernameOrEmail"
  | "auth.password"
  | "auth.showPassword"
  | "auth.hidePassword"
  | "auth.signIn"
  | "auth.signingIn"
  | "auth.signInTrouble"
  | "auth.invalidCredentials"
  | "auth.fieldRequired"
  | "auth.sessionExpired"
  | "auth.localValidation"
  | "auth.testUser"
  | "auth.selectUser"
  | "auth.signOut";

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
  "pwa.updateAvailable": "Update available",
  "pwa.refresh": "Refresh",
  "session.loading": "Loading",
  "auth.signInTitle": "Sign In",
  "auth.usernameOrEmail": "Username or email",
  "auth.password": "Password",
  "auth.showPassword": "Show password",
  "auth.hidePassword": "Hide password",
  "auth.signIn": "Sign in",
  "auth.signingIn": "Signing in",
  "auth.signInTrouble": "Sign in trouble?",
  "auth.invalidCredentials": "Sign in failed. Check your username and password.",
  "auth.fieldRequired": "This field is required.",
  "auth.sessionExpired": "Your session ended. Sign in again.",
  "auth.localValidation": "Local Validation",
  "auth.testUser": "Test User",
  "auth.selectUser": "Select user...",
  "auth.signOut": "Sign out",
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
  "pwa.updateAvailable": "May update",
  "pwa.refresh": "I-refresh",
  "session.loading": "Naglo-load",
  "auth.signInTitle": "Mag-sign in",
  "auth.usernameOrEmail": "Username o email",
  "auth.password": "Password",
  "auth.showPassword": "Ipakita ang password",
  "auth.hidePassword": "Itago ang password",
  "auth.signIn": "Mag-sign in",
  "auth.signingIn": "Nag-sa-sign in",
  "auth.signInTrouble": "Problema sa pag-sign in?",
  "auth.invalidCredentials": "Hindi na-sign in. Tingnan ang username at password.",
  "auth.fieldRequired": "Kailangan ang field na ito.",
  "auth.sessionExpired": "Natapos ang session. Mag-sign in ulit.",
  "auth.localValidation": "Local Validation",
  "auth.testUser": "Test User",
  "auth.selectUser": "Pumili ng user...",
  "auth.signOut": "Mag-sign out",
};

export const catalogs: Record<"en" | "fil-PH", Record<MessageKey, string>> = {
  en,
  "fil-PH": filPH,
};
