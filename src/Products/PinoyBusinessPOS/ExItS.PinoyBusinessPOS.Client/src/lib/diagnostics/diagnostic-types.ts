export type DiagnosticCategory = "runtime" | "api" | "unknown";

export type DiagnosticRecord = {
  application: string;
  appVersion: string;
  errorReference: string;
  timestamp: string;
  category: DiagnosticCategory;
  message: string;
  route: string;
  httpStatus?: number;
  errorCode?: string;
  requestCorrelationId?: string;
  locale: string;
  theme: string;
  browserPlatform: string;
};

export const GENERIC_RUNTIME_MESSAGE = "Unexpected client error.";
export const GENERIC_API_MESSAGE = "API request failed.";
