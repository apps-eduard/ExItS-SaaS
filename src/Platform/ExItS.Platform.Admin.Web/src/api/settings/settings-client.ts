import { platformRequest } from "@/api/platform-http";
import type {
  PlatformEmailSettings,
  PlatformEmailTestBody,
  PlatformEmailTestResult,
  PlatformGeneralSettings,
  PlatformRegionalSettings,
  UpdatePlatformEmailSettingsBody,
  UpdatePlatformGeneralSettingsBody,
  UpdatePlatformRegionalSettingsBody,
} from "@/api/settings/settings-types";

function parseGeneral(payload: unknown): PlatformGeneralSettings {
  const record = payload as Record<string, unknown>;
  return {
    platformDisplayName: String(record.platformDisplayName ?? ""),
    supportEmail: typeof record.supportEmail === "string" ? record.supportEmail : null,
    brandingLogoUrl: typeof record.brandingLogoUrl === "string" ? record.brandingLogoUrl : null,
    brandingPrimaryColor:
      typeof record.brandingPrimaryColor === "string" ? record.brandingPrimaryColor : null,
    brandingAccentColor:
      typeof record.brandingAccentColor === "string" ? record.brandingAccentColor : null,
    version: Number(record.version ?? 0),
    updatedAtUtc: String(record.updatedAtUtc ?? ""),
    updatedByActorId:
      typeof record.updatedByActorId === "string" ? record.updatedByActorId : null,
  };
}

function parseEmail(payload: unknown): PlatformEmailSettings {
  const record = payload as Record<string, unknown>;
  return {
    providerMode: String(record.providerMode ?? "Smtp"),
    smtpHost: typeof record.smtpHost === "string" ? record.smtpHost : null,
    smtpPort: typeof record.smtpPort === "number" ? record.smtpPort : null,
    smtpUsername: typeof record.smtpUsername === "string" ? record.smtpUsername : null,
    passwordConfigured: Boolean(record.passwordConfigured),
    fromDisplayName: String(record.fromDisplayName ?? ""),
    fromAddress: String(record.fromAddress ?? ""),
    securityMode: String(record.securityMode ?? "None"),
    adminPublicBaseUrl:
      typeof record.adminPublicBaseUrl === "string" ? record.adminPublicBaseUrl : null,
    isConfigured: Boolean(record.isConfigured),
    version: Number(record.version ?? 0),
    updatedAtUtc: String(record.updatedAtUtc ?? ""),
    updatedByActorId:
      typeof record.updatedByActorId === "string" ? record.updatedByActorId : null,
  };
}

function parseRegional(payload: unknown): PlatformRegionalSettings {
  const record = payload as Record<string, unknown>;
  return {
    defaultTimeZoneId: String(record.defaultTimeZoneId ?? ""),
    defaultLocale: String(record.defaultLocale ?? ""),
    defaultCurrencyCode: String(record.defaultCurrencyCode ?? ""),
    defaultCountryCode: String(record.defaultCountryCode ?? ""),
    dateFormat: typeof record.dateFormat === "string" ? record.dateFormat : null,
    timeFormat: typeof record.timeFormat === "string" ? record.timeFormat : null,
    version: Number(record.version ?? 0),
    updatedAtUtc: String(record.updatedAtUtc ?? ""),
    updatedByActorId:
      typeof record.updatedByActorId === "string" ? record.updatedByActorId : null,
  };
}

export function getPlatformGeneralSettings(baseUrl: string, signal?: AbortSignal) {
  return platformRequest<unknown>(baseUrl, {
    path: "/api/v1/platform/settings/general",
    signal,
  }).then(parseGeneral);
}

export function updatePlatformGeneralSettings(
  baseUrl: string,
  body: UpdatePlatformGeneralSettingsBody,
) {
  return platformRequest<unknown>(baseUrl, {
    method: "PUT",
    path: "/api/v1/platform/settings/general",
    body,
  }).then(parseGeneral);
}

export function getPlatformEmailSettings(baseUrl: string, signal?: AbortSignal) {
  return platformRequest<unknown>(baseUrl, {
    path: "/api/v1/platform/settings/email",
    signal,
  }).then(parseEmail);
}

export function updatePlatformEmailSettings(baseUrl: string, body: UpdatePlatformEmailSettingsBody) {
  return platformRequest<unknown>(baseUrl, {
    method: "PUT",
    path: "/api/v1/platform/settings/email",
    body,
  }).then(parseEmail);
}

export function sendPlatformEmailTest(baseUrl: string, body: PlatformEmailTestBody) {
  return platformRequest<unknown>(baseUrl, {
    method: "POST",
    path: "/api/v1/platform/settings/email/test",
    body,
  }).then((payload) => {
    const record = payload as Record<string, unknown>;
    return {
      succeeded: Boolean(record.succeeded),
      message: String(record.message ?? ""),
    } satisfies PlatformEmailTestResult;
  });
}

export function getPlatformRegionalSettings(baseUrl: string, signal?: AbortSignal) {
  return platformRequest<unknown>(baseUrl, {
    path: "/api/v1/platform/settings/regional",
    signal,
  }).then(parseRegional);
}

export function updatePlatformRegionalSettings(
  baseUrl: string,
  body: UpdatePlatformRegionalSettingsBody,
) {
  return platformRequest<unknown>(baseUrl, {
    method: "PUT",
    path: "/api/v1/platform/settings/regional",
    body,
  }).then(parseRegional);
}
