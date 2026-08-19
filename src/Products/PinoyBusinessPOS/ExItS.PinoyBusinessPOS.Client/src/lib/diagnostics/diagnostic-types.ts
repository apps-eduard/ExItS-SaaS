export type DiagnosticCategory = "runtime" | "api" | "unknown";

export type DiagnosticRecord = {
  application: string;
  appVersion: string;
  errorReference: string;
  timestamp: string;
  category: DiagnosticCategory;
  message: string;
  route: string;
  errorCode?: string;
  requestCorrelationId?: string;
  locale: string;
  theme: string;
  browserPlatform: string;
};
