export const AUTH_ERROR_CODES = {
  loginFailed: "application.auth.login_failed",
  userNotFound: "application.user.not_found",
  sessionInvalid: "application.auth.session_invalid",
  sessionExpired: "application.auth.session_expired",
  accountNotEligible: "application.auth.account_not_eligible",
  credentialLockedOut: "application.credential.locked_out",
  emailConflict: "application.user.email_conflict",
  passwordInvalid: "application.credential.password_invalid",
  credentialTokenInvalid: "application.auth.credential_token_invalid",
  credentialTokenExpired: "application.auth.credential_token_expired",
  invalidDisplayName: "platform.display_name.invalid",
  invalidEmail: "platform.email.invalid",
  rateLimitExceeded: "platform.rate_limit.exceeded",
} as const;

export type AuthErrorCode = (typeof AUTH_ERROR_CODES)[keyof typeof AUTH_ERROR_CODES];

export type AuthSession = {
  sessionId: string;
  userId: string;
  username: string;
  displayName: string;
  email: string;
  expiresAtUtc: string;
  absoluteExpiresAtUtc: string;
  lastActivityAtUtc?: string;
  selectedOrganizationId: string | null;
  selectedOrganizationDisplayName: string | null;
  organizationSelectionState: string;
  activeOrganizationCount: number;
  accountProfileId?: string | null;
  accountClass?: string | null;
  allowedScope?: string | null;
};

export type LoginRequest = {
  usernameOrEmail: string;
  password: string;
};

export type LoginResultDto = AuthSession & {
  sessionToken?: string;
};

export type LocalValidationIdentity = {
  key: string;
  username: string;
  displayName: string;
  email: string;
  listLabel: string;
  scopeLabel?: string;
};

export type RegisterPersonalAccountRequest = {
  displayName: string;
  email: string;
};

export type ActivateAccountRequest = {
  token: string;
  password: string;
};

export type RequestPasswordResetRequest = {
  usernameOrEmail: string;
};

export type ResetPasswordRequest = {
  token: string;
  newPassword: string;
};

export type AuthWorkflowAck = {
  message: string;
  expiresAtUtc?: string | null;
};
