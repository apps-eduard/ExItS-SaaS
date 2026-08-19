export const AUTH_ERROR_CODES = {
  loginFailed: "application.auth.login_failed",
  sessionInvalid: "application.auth.session_invalid",
  sessionExpired: "application.auth.session_expired",
  accountNotEligible: "application.auth.account_not_eligible",
  credentialLockedOut: "application.credential.locked_out",
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
