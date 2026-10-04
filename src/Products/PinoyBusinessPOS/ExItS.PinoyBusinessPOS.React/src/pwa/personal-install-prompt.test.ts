import { afterEach, describe, expect, it } from "vitest";
import { landingPathAfterExternalAuth } from "@/features/store/store-acquisition";
import {
  dismissPersonalInstallPrompt,
  isAutomaticPersonalInstallPath,
  isIosSafari,
  isPersonalInstallCoolingDown,
  isStandaloneDisplay,
  markPersonalInstallInstalled,
  PERSONAL_INSTALL_COOLDOWN_MS,
  PERSONAL_INSTALL_PROMPT_STORAGE_KEY,
  readPersonalInstallPrompt,
  resetPersonalInstallCaptureForTests,
  resolvePersonalInstallMode,
  shouldShowAutomaticPersonalInstall,
  shouldShowPersonalMoreInstall,
  startPersonalInstallCapture,
} from "@/pwa/personal-install-prompt";

afterEach(() => {
  resetPersonalInstallCaptureForTests();
});

describe("personal install detection", () => {
  it("treats standalone display as already installed", () => {
    expect(isStandaloneDisplay({
      navigator: {},
      matchMedia: () => ({ matches: true }),
    } as unknown as Window)).toBe(true);
    expect(resolvePersonalInstallMode({ standalone: true, hasDeferredPrompt: true })).toBe("installed");
    expect(shouldShowPersonalMoreInstall("installed")).toBe(false);
  });

  it("captures beforeinstallprompt without prompting", () => {
    startPersonalInstallCapture();
    let prompted = false;
    const event = new Event("beforeinstallprompt", { cancelable: true });
    Object.assign(event, {
      prompt: () => {
        prompted = true;
        return Promise.resolve();
      },
      userChoice: Promise.resolve({ outcome: "accepted" }),
    });
    window.dispatchEvent(event);
    expect(event.defaultPrevented).toBe(true);
    expect(prompted).toBe(false);
    expect(resolvePersonalInstallMode()).toBe("installable");
  });

  it("marks installed only when the browser fires appinstalled", () => {
    startPersonalInstallCapture();
    window.dispatchEvent(new Event("appinstalled"));
    expect(readPersonalInstallPrompt()).toEqual({ installed: true });
    expect(resolvePersonalInstallMode()).toBe("installed");
    expect(JSON.stringify(localStorage.getItem(PERSONAL_INSTALL_PROMPT_STORAGE_KEY))).not.toMatch(
      /email|token|session/i,
    );
  });

  it("detects iOS Safari and ignores other iOS browsers", () => {
    expect(isIosSafari(iosWindow("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1"))).toBe(true);
    expect(isIosSafari(iosWindow("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/120.0.0.0 Mobile/15E148 Safari/604.1"))).toBe(false);
    expect(resolvePersonalInstallMode({ standalone: false, iosSafari: true, hasDeferredPrompt: false })).toBe(
      "ios-safari",
    );
    expect(resolvePersonalInstallMode({ standalone: false, iosSafari: false, hasDeferredPrompt: false })).toBe(
      "unsupported",
    );
  });
});

describe("personal install prompt policy", () => {
  it("shows after a personal home landing and hides signed-out and connect routes", () => {
    expect(
      shouldShowAutomaticPersonalInstall({
        pathname: "/personal",
        online: true,
        mode: "installable",
      }),
    ).toBe(true);
    expect(isAutomaticPersonalInstallPath("/sign-in")).toBe(false);
    expect(isAutomaticPersonalInstallPath("/connect/EX-4827-1936")).toBe(false);
    expect(isAutomaticPersonalInstallPath("/external-login-callback")).toBe(false);
    expect(
      shouldShowAutomaticPersonalInstall({
        pathname: "/sign-in",
        online: true,
        mode: "installable",
      }),
    ).toBe(false);
    expect(
      shouldShowAutomaticPersonalInstall({
        pathname: "/personal",
        online: false,
        mode: "installable",
      }),
    ).toBe(false);
  });

  it("suppresses Maybe later for 7 days and allows the More entry during cooldown", () => {
    const dismissedAt = new Date("2026-10-01T00:00:00.000Z");
    dismissPersonalInstallPrompt(dismissedAt);
    const stored = readPersonalInstallPrompt();
    expect(stored).toEqual({ dismissedAtUtc: dismissedAt.toISOString() });
    expect(Object.keys(stored ?? {})).toEqual(["dismissedAtUtc"]);
    expect(isPersonalInstallCoolingDown(dismissedAt.getTime() + PERSONAL_INSTALL_COOLDOWN_MS - 1)).toBe(
      true,
    );
    expect(
      shouldShowAutomaticPersonalInstall({
        pathname: "/personal",
        online: true,
        mode: "installable",
        nowMs: dismissedAt.getTime() + 1000,
      }),
    ).toBe(false);
    expect(
      shouldShowAutomaticPersonalInstall({
        pathname: "/personal",
        online: true,
        mode: "installable",
        nowMs: dismissedAt.getTime() + PERSONAL_INSTALL_COOLDOWN_MS,
      }),
    ).toBe(true);
    expect(shouldShowPersonalMoreInstall("installable")).toBe(true);
  });

  it("drops malformed prompt storage", () => {
    localStorage.setItem(
      PERSONAL_INSTALL_PROMPT_STORAGE_KEY,
      JSON.stringify({ dismissedAtUtc: "2026-10-01T00:00:00.000Z", email: "a@b.c" }),
    );
    expect(readPersonalInstallPrompt()).toBeNull();
    expect(localStorage.getItem(PERSONAL_INSTALL_PROMPT_STORAGE_KEY)).toBeNull();
  });

  it("keeps Google continuation on the connect route and install eligibility on /personal", () => {
    expect(landingPathAfterExternalAuth("?target=personal")).toBe("/personal");
    expect(landingPathAfterExternalAuth("?target=personal&continue=%2Fconnect%2FEX-4827-1936")).toBe(
      "/connect/EX-4827-1936",
    );
    expect(isAutomaticPersonalInstallPath("/connect/EX-4827-1936")).toBe(false);
    expect(isAutomaticPersonalInstallPath("/personal")).toBe(true);
    markPersonalInstallInstalled();
    expect(
      shouldShowAutomaticPersonalInstall({
        pathname: "/personal",
        online: true,
        mode: "installed",
      }),
    ).toBe(false);
  });
});

function iosWindow(userAgent: string): Window {
  return {
    navigator: { userAgent, platform: "iPhone", maxTouchPoints: 1 },
  } as unknown as Window;
}
