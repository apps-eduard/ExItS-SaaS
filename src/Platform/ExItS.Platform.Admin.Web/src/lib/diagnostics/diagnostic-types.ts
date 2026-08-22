export const DIAGNOSTIC_APPLICATION = "Platform Admin React";

export type DiagnosticCategory =
  | "NETWORK_ERROR"
  | "SERVICE_UNAVAILABLE"
  | "TIMEOUT"
  | "RATE_LIMITED"
  | "AUTHENTICATION_REQUIRED"
  | "FORBIDDEN"
  | "VALIDATION_ERROR"
  | "NOT_FOUND"
  | "CONFLICT"
  | "DOMAIN_ERROR"
  | "SERVER_ERROR"
  | "SECURITY_REQUEST_ERROR"
  | "REACT_RENDER_ERROR"
  | "UNEXPECTED_CLIENT_ERROR";

export type DiagnosticRecord = {
  application: string;
  errorReference: string;
  timestampUtc: string;
  buildSha?: string;
  environment?: string;
  frontendMode?: string;
  localValidationEnabled?: boolean;
  apiMode?: string;
  pagePath?: string;
  operation?: string;
  category: DiagnosticCategory;
  userMessage: string;
  httpMethod?: string;
  apiPath?: string;
  httpStatus?: number;
  httpStatusLabel?: string;
  errorCode?: string;
  traceId?: string;
  correlationId?: string;
  networkOnline?: boolean;
  networkFailureKind?: string;
  browserName?: string;
  browserVersion?: string;
  retryable?: boolean;
  errorType?: string;
  componentStack?: string;
};

export type DiagnosticEnvironment = {
  pathname?: string;
  locale?: string;
  theme?: string;
  density?: string;
  browserPlatform?: string;
  browserName?: string;
  browserVersion?: string;
  buildSha?: string;
  environment?: string;
  frontendMode?: string;
  localValidationEnabled?: boolean;
  apiMode?: string;
  networkOnline?: boolean;
  now?: () => string;
  createReference?: () => string;
};
