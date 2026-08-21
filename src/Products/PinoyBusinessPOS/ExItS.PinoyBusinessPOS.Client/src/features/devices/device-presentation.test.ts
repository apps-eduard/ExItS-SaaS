import { describe, expect, it } from "vitest";
import type { PosDeviceDto } from "@/api/platform/pos-devices-client";
import {
  browserRegistrationMetadata,
  deviceIconKind,
  formatDeviceModelLine,
  formatRelativeOrDate,
  isCurrentDevice,
  resolveCurrentBrowserState,
  suggestFriendlyName,
} from "@/features/devices/device-presentation";

const LOCAL_ID = "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee";
const OTHER_ID = "11111111-2222-4333-8444-555555555555";

function device(overrides: Partial<PosDeviceDto> = {}): PosDeviceDto {
  return {
    id: "device-1",
    organizationId: "org-1",
    branchId: "branch-1",
    installationDeviceId: LOCAL_ID,
    friendlyName: "Counter browser",
    platform: "Browser",
    model: "Chrome on Windows",
    appVersion: "1.2.3",
    status: "Active",
    registeredAtUtc: "2026-08-01T01:00:00Z",
    lastSeenAtUtc: "2026-08-22T01:00:00Z",
    revokedAtUtc: null,
    ...overrides,
  };
}

describe("isCurrentDevice", () => {
  it("matches the durable installation id ignoring case and padding", () => {
    expect(isCurrentDevice(device(), ` ${LOCAL_ID.toUpperCase()} `)).toBe(true);
  });

  it("does not match a different installation id", () => {
    expect(isCurrentDevice(device({ installationDeviceId: OTHER_ID }), LOCAL_ID)).toBe(false);
  });

  it("never matches when the local identity is missing", () => {
    expect(isCurrentDevice(device(), null)).toBe(false);
    expect(isCurrentDevice(device({ installationDeviceId: "" }), LOCAL_ID)).toBe(false);
  });
});

describe("formatDeviceModelLine", () => {
  it("renames Android emulator images to a friendly label", () => {
    expect(formatDeviceModelLine("Android", "sdk_gphone64_x86_64")).toBe(
      "Android Emulator · Android",
    );
  });

  it("omits empty parts instead of leaving a dangling separator", () => {
    expect(formatDeviceModelLine("Browser", null)).toBe("Browser");
    expect(formatDeviceModelLine(null, "Chrome on Windows")).toBe("Chrome on Windows");
    expect(formatDeviceModelLine("  ", "")).toBe("");
  });
});

describe("formatRelativeOrDate", () => {
  const now = new Date("2026-08-22T12:00:00Z");

  it("returns null for missing values", () => {
    expect(formatRelativeOrDate(null, now, "en")).toBeNull();
    expect(formatRelativeOrDate("   ", now, "en")).toBeNull();
  });

  it("uses relative wording inside a week", () => {
    expect(formatRelativeOrDate("2026-08-22T10:00:00Z", now, "en")).toBe("2 hours ago");
    expect(formatRelativeOrDate("2026-08-20T12:00:00Z", now, "en")).toBe("2 days ago");
  });

  it("falls back to an absolute date beyond a week", () => {
    expect(formatRelativeOrDate("2026-01-05T12:00:00Z", now, "en")).toContain("2026");
  });

  it("echoes unparseable input rather than rendering Invalid Date", () => {
    expect(formatRelativeOrDate("not-a-timestamp", now, "en")).toBe("not-a-timestamp");
  });
});

describe("browserRegistrationMetadata", () => {
  it("prefers low-entropy userAgentData brands", () => {
    const metadata = browserRegistrationMetadata({
      userAgent: "Mozilla/5.0 (Windows NT 10.0) Chrome/140.0",
      userAgentData: {
        brands: [
          { brand: "Not.A/Brand", version: "99" },
          { brand: "Chromium", version: "140" },
          { brand: "Google Chrome", version: "140" },
        ],
        platform: "Windows",
      },
      appVersion: "9.9.9",
      standalone: false,
    });
    expect(metadata).toEqual({
      platform: "Browser",
      model: "Google Chrome on Windows",
      appVersion: "9.9.9",
      displayMode: "browser",
    });
  });

  it("falls back to a coarse user agent parse", () => {
    const metadata = browserRegistrationMetadata({
      userAgent: "Mozilla/5.0 (Linux; Android 14) AppleWebKit Chrome/140.0 Mobile Safari",
      userAgentData: null,
      appVersion: null,
      standalone: true,
    });
    expect(metadata.model).toBe("Chrome on Android");
    expect(metadata.displayMode).toBe("standalone");
    expect(metadata.platform).toBe("Browser");
  });

  it("prefers Edge over the Chrome token it also advertises", () => {
    expect(
      browserRegistrationMetadata({
        userAgent: "Mozilla/5.0 (Windows NT 10.0) Chrome/140.0 Safari/537.36 Edg/140.0",
        userAgentData: null,
      }).model,
    ).toBe("Edge on Windows");
  });

  it("suggests the same label as the friendly name default", () => {
    const source = {
      userAgent: "Mozilla/5.0 (Macintosh; Mac OS X) Safari/605",
      userAgentData: null,
    };
    expect(suggestFriendlyName(source)).toBe("Safari on macOS");
  });
});

describe("deviceIconKind", () => {
  it.each([
    ["Android", "Pixel 8", "phone"],
    ["Browser", "Chrome on Android", "phone"],
    ["Android", "Galaxy Tab S9", "tablet"],
    ["iOS", "iPad Pro", "tablet"],
    ["Browser", "Chrome on Windows", "desktop"],
    ["Browser", null, "browser"],
  ] as const)("maps %s / %s to %s", (platform, model, expected) => {
    expect(deviceIconKind(platform, model)).toBe(expected);
  });
});

describe("resolveCurrentBrowserState", () => {
  it("reports active when the matching device is Active", () => {
    const result = resolveCurrentBrowserState({
      devices: [device({ installationDeviceId: OTHER_ID, id: "other" }), device()],
      localInstallationId: LOCAL_ID,
      registrationStatus: "authorized",
    });
    expect(result.state).toBe("active");
    expect(result.device?.id).toBe("device-1");
  });

  it("reports revoked when the matching device is Revoked", () => {
    const result = resolveCurrentBrowserState({
      devices: [device({ status: "Revoked", revokedAtUtc: "2026-08-22T02:00:00Z" })],
      localInstallationId: LOCAL_ID,
      registrationStatus: "authorized",
    });
    expect(result.state).toBe("revoked");
  });

  it("falls back to the workspace registration status when nothing matches", () => {
    expect(
      resolveCurrentBrowserState({
        devices: [device({ installationDeviceId: OTHER_ID })],
        localInstallationId: LOCAL_ID,
        registrationStatus: "revoked",
      }).state,
    ).toBe("revoked");

    expect(
      resolveCurrentBrowserState({
        devices: [],
        localInstallationId: LOCAL_ID,
        registrationStatus: "unregistered",
      }).state,
    ).toBe("unregistered");
  });
});
