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
  | "auth.signUpTitle"
  | "auth.forgotTitle"
  | "auth.resetTitle"
  | "auth.activateTitle"
  | "auth.accountNav"
  | "auth.usernameOrEmail"
  | "auth.displayName"
  | "auth.email"
  | "auth.password"
  | "auth.newPassword"
  | "auth.confirmPassword"
  | "auth.createPassword"
  | "auth.showPassword"
  | "auth.hidePassword"
  | "auth.signIn"
  | "auth.signingIn"
  | "auth.createAccount"
  | "auth.creatingAccount"
  | "auth.forgotPassword"
  | "auth.sendReset"
  | "auth.sendingReset"
  | "auth.activateAccount"
  | "auth.activating"
  | "auth.resetPassword"
  | "auth.resetting"
  | "auth.backToSignIn"
  | "auth.checkEmail"
  | "auth.forgotAck"
  | "auth.activatedNotice"
  | "auth.resetNotice"
  | "auth.invalidCredentials"
  | "auth.fieldRequired"
  | "auth.passwordsMustMatch"
  | "auth.sessionExpired"
  | "auth.localValidation"
  | "auth.testUser"
  | "auth.selectUser"
  | "auth.signOut"
  | "auth.activationLinkInvalid"
  | "auth.resetLinkInvalid"
  | "auth.tokenInvalid"
  | "auth.tokenExpired"
  | "auth.activationFailed"
  | "auth.resetFailed";

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
  "auth.signUpTitle": "Create account",
  "auth.forgotTitle": "Forgot password",
  "auth.resetTitle": "Reset password",
  "auth.activateTitle": "Activate account",
  "auth.accountNav": "Account",
  "auth.usernameOrEmail": "Username or email",
  "auth.displayName": "Display name",
  "auth.email": "Email",
  "auth.password": "Password",
  "auth.newPassword": "New password",
  "auth.confirmPassword": "Confirm password",
  "auth.createPassword": "Create your password",
  "auth.showPassword": "Show password",
  "auth.hidePassword": "Hide password",
  "auth.signIn": "Sign in",
  "auth.signingIn": "Signing in",
  "auth.createAccount": "Create account",
  "auth.creatingAccount": "Creating account",
  "auth.forgotPassword": "Forgot password",
  "auth.sendReset": "Send reset email",
  "auth.sendingReset": "Sending",
  "auth.activateAccount": "Activate account",
  "auth.activating": "Activating",
  "auth.resetPassword": "Reset password",
  "auth.resetting": "Resetting",
  "auth.backToSignIn": "Back to Sign In",
  "auth.checkEmail": "Check your email to continue.",
  "auth.forgotAck": "If an eligible account exists, a password reset email has been sent.",
  "auth.activatedNotice": "Account activated. Sign in with your password.",
  "auth.resetNotice": "Password reset. Sign in with your new password.",
  "auth.invalidCredentials": "Sign in failed. Check your username and password.",
  "auth.fieldRequired": "This field is required.",
  "auth.passwordsMustMatch": "Passwords must match.",
  "auth.sessionExpired": "Your session ended. Sign in again.",
  "auth.localValidation": "Local Validation",
  "auth.testUser": "Test User",
  "auth.selectUser": "Select user...",
  "auth.signOut": "Sign out",
  "auth.activationLinkInvalid": "Activation link is invalid or missing.",
  "auth.resetLinkInvalid": "Reset link is invalid or missing.",
  "auth.tokenInvalid": "This link is invalid or already used. Request a new email to continue.",
  "auth.tokenExpired": "This link has expired. Request a new email to continue.",
  "auth.activationFailed": "Activation could not be completed.",
  "auth.resetFailed": "Password reset could not be completed.",
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
  "auth.signUpTitle": "Gumawa ng account",
  "auth.forgotTitle": "Nakalimutan ang password",
  "auth.resetTitle": "I-reset ang password",
  "auth.activateTitle": "I-activate ang account",
  "auth.accountNav": "Account",
  "auth.usernameOrEmail": "Username o email",
  "auth.displayName": "Display name",
  "auth.email": "Email",
  "auth.password": "Password",
  "auth.newPassword": "Bagong password",
  "auth.confirmPassword": "Kumpirmahin ang password",
  "auth.createPassword": "Gumawa ng password",
  "auth.showPassword": "Ipakita ang password",
  "auth.hidePassword": "Itago ang password",
  "auth.signIn": "Mag-sign in",
  "auth.signingIn": "Nag-sa-sign in",
  "auth.createAccount": "Gumawa ng account",
  "auth.creatingAccount": "Gumagawa ng account",
  "auth.forgotPassword": "Nakalimutan ang password",
  "auth.sendReset": "Magpadala ng reset email",
  "auth.sendingReset": "Ipinapadala",
  "auth.activateAccount": "I-activate ang account",
  "auth.activating": "Ina-activate",
  "auth.resetPassword": "I-reset ang password",
  "auth.resetting": "Nire-reset",
  "auth.backToSignIn": "Bumalik sa Sign In",
  "auth.checkEmail": "Tingnan ang email para magpatuloy.",
  "auth.forgotAck": "Kung may eligible na account, naipadala na ang password reset email.",
  "auth.activatedNotice": "Na-activate ang account. Mag-sign in gamit ang password.",
  "auth.resetNotice": "Na-reset ang password. Mag-sign in gamit ang bagong password.",
  "auth.invalidCredentials": "Hindi na-sign in. Tingnan ang username at password.",
  "auth.fieldRequired": "Kailangan ang field na ito.",
  "auth.passwordsMustMatch": "Dapat magkatugma ang mga password.",
  "auth.sessionExpired": "Natapos ang session. Mag-sign in ulit.",
  "auth.localValidation": "Local Validation",
  "auth.testUser": "Test User",
  "auth.selectUser": "Pumili ng user...",
  "auth.signOut": "Mag-sign out",
  "auth.activationLinkInvalid": "Invalid o wala ang activation link.",
  "auth.resetLinkInvalid": "Invalid o wala ang reset link.",
  "auth.tokenInvalid": "Invalid o nagamit na ang link. Humiling ng bagong email.",
  "auth.tokenExpired": "Nag-expire na ang link. Humiling ng bagong email.",
  "auth.activationFailed": "Hindi natapos ang activation.",
  "auth.resetFailed": "Hindi natapos ang pag-reset ng password.",
};

export const catalogs: Record<"en" | "fil-PH", Record<MessageKey, string>> = {
  en,
  "fil-PH": filPH,
};
