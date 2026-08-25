import type { PosDeviceDto } from "@/api/platform/pos-devices-client";

/**
 * Presentation helpers for POS device cards.
 *
 * Device identity is the durable `installationDeviceId` only (RMAP-10b). Nothing here may
 * fingerprint the browser or read an ADB serial: the user agent is used for a *cosmetic*
 * label at registration time and is never an authorization signal.
 */

/** True when the listed device is the browser/installation currently running this app. */
export function isCurrentDevice(
  device: Pick<PosDeviceDto, "installationDeviceId">,
  localInstallationId: string | null | undefined,
): boolean {
  const local = localInstallationId?.trim();
  const listed = device.installationDeviceId?.trim();
  if (!local || !listed) {
    return false;
  }
  return local.localeCompare(listed, undefined, { sensitivity: "accent" }) === 0;
}

/** Android emulator images report an `sdk_gphone*` model that means nothing to a shop owner. */
function friendlyModel(model: string | null | undefined): string | null {
  const value = model?.trim();
  if (!value) {
    return null;
  }
  if (/^sdk_gphone/i.test(value) || /^google_sdk$/i.test(value) || /^emulator/i.test(value)) {
    return "Android Emulator";
  }
  return value;
}

/** `model · platform` with empty parts dropped, so a card never shows a dangling separator. */
export function formatDeviceModelLine(
  platform: string | null | undefined,
  model: string | null | undefined,
): string {
  const parts = [friendlyModel(model), platform?.trim() || null].filter((part): part is string =>
    Boolean(part),
  );
  return parts.join(" · ");
}

export type DeviceIconKind = "phone" | "tablet" | "desktop" | "browser";

/** Coarse icon hint from the cosmetic platform/model strings — never an authorization signal. */
export function deviceIconKind(
  platform: string | null | undefined,
  model: string | null | undefined,
): DeviceIconKind {
  const combined = `${platform ?? ""} ${model ?? ""}`.toLowerCase();

  if (/\bipad\b|\btablet\b|\btab\b/.test(combined)) {
    return "tablet";
  }
  if (/\bandroid\b|\biphone\b|\bios\b|\bphone\b/.test(combined)) {
    return "phone";
  }
  if (/\bwindows\b|\bmacos\b|\bmac os\b|\blinux\b|\bchromeos\b|\bdesktop\b/.test(combined)) {
    return "desktop";
  }
  return "browser";
}

export type CurrentBrowserState = "active" | "revoked" | "unregistered";

export type CurrentBrowserResolution = {
  state: CurrentBrowserState;
  device: PosDeviceDto | null;
};

/**
 * Decide what the "this device" card should say.
 * Prefers an exact durable-identity match in the device list; falls back to the workspace
 * POS device context so a revoked browser still reports honestly when the list is empty.
 */
export function resolveCurrentBrowserState(input: {
  devices: readonly PosDeviceDto[] | null | undefined;
  localInstallationId: string | null | undefined;
  registrationStatus?: string | null;
}): CurrentBrowserResolution {
  const matched =
    (input.devices ?? []).find((device) => isCurrentDevice(device, input.localInstallationId)) ??
    null;

  if (matched) {
    if (matched.status === "Revoked") {
      return { state: "revoked", device: matched };
    }
    return { state: matched.status === "Active" ? "active" : "unregistered", device: matched };
  }

  if (input.registrationStatus === "authorized" || input.registrationStatus === "registered") {
    return { state: "active", device: null };
  }
  if (input.registrationStatus === "revoked") {
    return { state: "revoked", device: null };
  }
  return { state: "unregistered", device: null };
}

const MINUTE_MS = 60_000;
const HOUR_MS = 60 * MINUTE_MS;
const DAY_MS = 24 * HOUR_MS;

/**
 * Relative time for recent activity, absolute date beyond a week.
 * Falls back to the raw value when the timestamp cannot be parsed — never renders "Invalid Date".
 */
export function formatRelativeOrDate(
  value: string | null | undefined,
  now: Date = new Date(),
  locale?: string,
): string | null {
  const raw = value?.trim();
  if (!raw) {
    return null;
  }
  const parsed = new Date(raw);
  const time = parsed.getTime();
  if (Number.isNaN(time)) {
    return raw;
  }

  const elapsed = now.getTime() - time;
  if (elapsed >= 0 && elapsed < 7 * DAY_MS) {
    const relative = new Intl.RelativeTimeFormat(locale, { numeric: "auto" });
    if (elapsed < MINUTE_MS) {
      return relative.format(0, "minute");
    }
    if (elapsed < HOUR_MS) {
      return relative.format(-Math.floor(elapsed / MINUTE_MS), "minute");
    }
    if (elapsed < DAY_MS) {
      return relative.format(-Math.floor(elapsed / HOUR_MS), "hour");
    }
    return relative.format(-Math.floor(elapsed / DAY_MS), "day");
  }

  return new Intl.DateTimeFormat(locale, { dateStyle: "medium" }).format(parsed);
}

export type BrowserRegistrationMetadata = {
  platform: string;
  model: string;
  appVersion: string | null;
  displayMode: "standalone" | "browser";
};

type UserAgentDataBrand = { brand: string; version: string };

type CoarseUserAgentData = {
  brands?: UserAgentDataBrand[];
  platform?: string;
};

const IGNORED_BRANDS = [/not.?a.?brand/i, /^chromium$/i];

function coarseBrowserName(userAgent: string, brands: UserAgentDataBrand[] | undefined): string {
  const branded = brands?.find(
    (entry) => entry?.brand && !IGNORED_BRANDS.some((pattern) => pattern.test(entry.brand)),
  );
  if (branded) {
    return branded.brand;
  }

  // Coarse UA sniff only. Order matters: Edge/Opera also advertise Chrome.
  if (/\bEdg\//.test(userAgent)) {
    return "Edge";
  }
  if (/\bOPR\/|\bOpera\//.test(userAgent)) {
    return "Opera";
  }
  if (/\bFirefox\//.test(userAgent)) {
    return "Firefox";
  }
  if (/\bSamsungBrowser\//.test(userAgent)) {
    return "Samsung Internet";
  }
  if (/\bChrome\//.test(userAgent)) {
    return "Chrome";
  }
  if (/\bSafari\//.test(userAgent)) {
    return "Safari";
  }
  return "Browser";
}

function coarseOsName(userAgent: string, hintedPlatform: string | undefined): string | null {
  const hinted = hintedPlatform?.trim();
  if (hinted) {
    return hinted;
  }
  if (/\bAndroid\b/.test(userAgent)) {
    return "Android";
  }
  if (/\b(iPhone|iPad|iPod)\b/.test(userAgent)) {
    return "iOS";
  }
  if (/\bWindows\b/.test(userAgent)) {
    return "Windows";
  }
  if (/\bMac OS X\b|\bMacintosh\b/.test(userAgent)) {
    return "macOS";
  }
  if (/\bCrOS\b/.test(userAgent)) {
    return "ChromeOS";
  }
  if (/\bLinux\b/.test(userAgent)) {
    return "Linux";
  }
  return null;
}

export type BrowserMetadataSource = {
  userAgent?: string;
  userAgentData?: CoarseUserAgentData | null;
  appVersion?: string | null;
  standalone?: boolean;
};

/**
 * Cosmetic registration metadata for the current browser.
 * Uses only low-entropy signals already present in the request (`navigator.userAgentData.brands`
 * or a coarse UA match). High-entropy Client Hints are deliberately never requested.
 */
export function browserRegistrationMetadata(
  source?: BrowserMetadataSource,
): BrowserRegistrationMetadata {
  const nav = typeof navigator === "undefined" ? undefined : navigator;
  const userAgent = source?.userAgent ?? nav?.userAgent ?? "";
  const userAgentData =
    source?.userAgentData ??
    ((nav as Navigator & { userAgentData?: CoarseUserAgentData })?.userAgentData || null);

  const browser = coarseBrowserName(userAgent, userAgentData?.brands);
  const os = coarseOsName(userAgent, userAgentData?.platform);

  const standalone =
    source?.standalone ??
    (typeof window !== "undefined" && typeof window.matchMedia === "function"
      ? window.matchMedia("(display-mode: standalone)").matches
      : false);

  const appVersion =
    source?.appVersion ??
    (typeof import.meta !== "undefined"
      ? (import.meta.env as Record<string, string | undefined>)?.VITE_APP_VERSION?.trim() || null
      : null);

  return {
    platform: "Browser",
    model: os ? `${browser} on ${os}` : browser,
    appVersion,
    displayMode: standalone ? "standalone" : "browser",
  };
}

/**
 * Default friendly name for a newly registered browser, e.g. "Chrome on Windows".
 * Owners can always overwrite it before registering.
 */
export function suggestFriendlyName(source?: BrowserMetadataSource): string {
  return browserRegistrationMetadata(source).model;
}
