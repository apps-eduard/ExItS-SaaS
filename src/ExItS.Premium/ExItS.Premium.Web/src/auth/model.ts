export const AUTH_SCREENS = ["login", "register", "forgot", "reset"] as const;

export type AuthScreenId = (typeof AUTH_SCREENS)[number];

export const AUTH_PREVIEW_PATH = "/authentication";
export const PERSONAL_HOME_PATH = "/personal/home";
export const WORKSPACE_PATH = "/workspace";

export const AUTH_DEMOS = [
  "empty",
  "invalid",
  "hidden",
  "visible",
  "loading",
  "failure",
  "instructions",
  "updated",
] as const;

export type AuthDemonstration = (typeof AUTH_DEMOS)[number];

export type FieldErrors = Partial<Record<"name" | "email" | "password" | "confirm", string>>;

export type AuthSeed = {
  name: string;
  email: string;
  password: string;
  confirm: string;
  remember: boolean;
  reveal: boolean;
  showErrors: boolean;
  loading: boolean;
  failure: boolean;
  instructions: boolean;
  updated: boolean;
};

const EMPTY_SEED: AuthSeed = {
  name: "",
  email: "",
  password: "",
  confirm: "",
  remember: false,
  reveal: false,
  showErrors: false,
  loading: false,
  failure: false,
  instructions: false,
  updated: false,
};

export function isAuthScreen(value: string | null | undefined): value is AuthScreenId {
  return AUTH_SCREENS.includes(value as AuthScreenId);
}

export function isAuthDemonstration(value: string | null | undefined): value is AuthDemonstration {
  return AUTH_DEMOS.includes(value as AuthDemonstration);
}

/** Design-system and account routing. `/` chooses sign-in or the account home. */
export function appSurface(
  pathname: string,
): "entry" | "input-motion" | "data-table-preview" | "authentication" | "personal-home" | "workspace" | "design-system" {
  const path = pathname.replace(/\/$/, "") || "/";
  if (path === "/") return "entry";
  if (path === "/input-motion") return "input-motion";
  if (path === "/data-table-preview") return "data-table-preview";
  if (path === AUTH_PREVIEW_PATH) return "authentication";
  if (path === PERSONAL_HOME_PATH) return "personal-home";
  if (path === WORKSPACE_PATH) return "workspace";
  if (path === "/design-system") return "design-system";
  return "design-system";
}

/** Personal accounts open Personal Home. Every other class stays out of Personal APIs. */
export function accountHome(accountClass: string | null | undefined) {
  return accountClass?.trim().toLowerCase() === "personal" ? PERSONAL_HOME_PATH : WORKSPACE_PATH;
}

export function readAuthScreen(search: string): AuthScreenId {
  const value = new URLSearchParams(search).get("screen");
  return isAuthScreen(value) ? value : "login";
}

export function authPreviewHref(screen: AuthScreenId = "login") {
  if (screen === "login") return AUTH_PREVIEW_PATH;
  return `${AUTH_PREVIEW_PATH}?screen=${screen}`;
}

export function isEmailAddress(value: string) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim());
}

export function passwordChecks(value: string) {
  return {
    length: value.length >= 10,
    letterAndNumber: /[A-Za-z]/.test(value) && /\d/.test(value),
  };
}

export function passwordReady(value: string) {
  const checks = passwordChecks(value);
  return checks.length && checks.letterAndNumber;
}

function emailError(value: string) {
  if (!value.trim()) return "Enter an email address.";
  if (!isEmailAddress(value)) return "Enter a valid email address, such as name@example.com.";
  return undefined;
}

function passwordError(value: string) {
  if (!value) return "Enter a password.";
  if (!passwordChecks(value).length) return "Use at least 10 characters.";
  if (!passwordChecks(value).letterAndNumber) return "Use at least one letter and one number.";
  return undefined;
}

export function validateLogin(input: { email: string; password: string }): FieldErrors {
  const errors: FieldErrors = {};
  const email = emailError(input.email);
  if (email) errors.email = email;
  if (!input.password) errors.password = "Enter your password.";
  return errors;
}

export function validateRegister(input: { name: string; email: string; password: string; confirm: string }): FieldErrors {
  const errors: FieldErrors = {};
  if (!input.name.trim()) errors.name = "Enter your name.";
  const email = emailError(input.email);
  if (email) errors.email = email;
  const password = passwordError(input.password);
  if (password) errors.password = password;
  if (!input.confirm) errors.confirm = "Enter the password again.";
  else if (input.confirm !== input.password) errors.confirm = "Those passwords do not match. Enter the same password in both fields.";
  return errors;
}

export function validateForgot(input: { email: string }): FieldErrors {
  const email = emailError(input.email);
  return email ? { email } : {};
}

export function validateReset(input: { password: string; confirm: string }): FieldErrors {
  const errors: FieldErrors = {};
  const password = passwordError(input.password);
  if (password) errors.password = password;
  if (!input.confirm) errors.confirm = "Enter the password again.";
  else if (input.confirm !== input.password) errors.confirm = "Those passwords do not match. Enter the same password in both fields.";
  return errors;
}

export function firstInvalid(errors: FieldErrors, order: Array<keyof FieldErrors>) {
  return order.find((key) => errors[key]);
}

/** Local review data only. Never written to storage. */
export function authSeed(demo: AuthDemonstration = "empty"): AuthSeed {
  if (demo === "invalid") {
    return {
      ...EMPTY_SEED,
      email: "ada@",
      password: "",
      confirm: "other",
      showErrors: true,
    };
  }
  if (demo === "hidden" || demo === "visible" || demo === "loading" || demo === "failure") {
    return {
      ...EMPTY_SEED,
      name: "Ada Lovelace",
      email: "ada@example.com",
      password: "ledger-1947",
      confirm: "ledger-1947",
      reveal: demo === "visible",
      loading: demo === "loading",
      failure: demo === "failure",
    };
  }
  if (demo === "instructions") {
    return { ...EMPTY_SEED, email: "ada@example.com", instructions: true };
  }
  if (demo === "updated") {
    return { ...EMPTY_SEED, updated: true };
  }
  return EMPTY_SEED;
}
