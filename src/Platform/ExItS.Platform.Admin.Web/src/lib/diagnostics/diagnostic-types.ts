export const DIAGNOSTIC_APPLICATION = "ExItS Platform Admin Web";

export type DiagnosticCategory = "API" | "NETWORK" | "RENDER" | "RUNTIME" | "UNKNOWN";

export type DiagnosticRecord = {
  application: string;
  errorReference: string;
  timestamp: string;
  category: DiagnosticCategory;
  message: string;
  route?: string;
  operation?: string;
  errorType?: string;
  httpStatus?: number;
  errorCode?: string;
  requestCorrelationId?: string;
  serverTraceId?: string;
  locale?: string;
  theme?: string;
  density?: string;
  browserPlatform?: string;
  componentStack?: string;
};

export type DiagnosticEnvironment = {
  pathname?: string;
  locale?: string;
  theme?: string;
  density?: string;
  browserPlatform?: string;
  now?: () => string;
  createReference?: () => string;
};
