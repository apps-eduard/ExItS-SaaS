export const PlatformAntiforgeryDefaults = {
  tokenPath: "/api/v1/platform/antiforgery/token",
  headerName: "X-XSRF-TOKEN",
  invalidErrorCode: "platform.antiforgery.invalid",
} as const;
