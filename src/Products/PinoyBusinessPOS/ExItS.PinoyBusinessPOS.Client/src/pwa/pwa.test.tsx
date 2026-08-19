import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AppProviders } from "@/app/providers";
import { applyPwaUpdateIfAllowed } from "@/pwa/apply-pwa-update";
import { PwaUpdateNotice } from "@/pwa/PwaUpdateNotice";
import {
  createPwaManifest,
  PWA_API_PATH_PATTERN,
  PWA_API_PORT_PATTERN,
  PWA_APP_NAME,
  PWA_BACKGROUND_COLOR,
  PWA_DEFAULT_APP_VERSION,
  PWA_DISPLAY,
  PWA_ICON_FILES,
  PWA_PLATFORM_API_PREFIX_PATTERN,
  PWA_SHORT_NAME,
  PWA_START_URL,
  PWA_THEME_COLOR,
} from "@/pwa/pwa-manifest";
import { getAppVersion } from "@/api/http";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";

function renderNotice(visible = true, onRefresh = vi.fn()) {
  return {
    onRefresh,
    ...render(
      <AppProviders>
        <PwaUpdateNotice visible={visible} onRefresh={onRefresh} />
      </AppProviders>,
    ),
  };
}

describe("PWA manifest identity", () => {
  it("declares installable standalone identity and required icons", () => {
    const manifest = createPwaManifest();
    expect(manifest.name).toBe(PWA_APP_NAME);
    expect(manifest.short_name).toBe(PWA_SHORT_NAME);
    expect(manifest.start_url).toBe(PWA_START_URL);
    expect(manifest.display).toBe(PWA_DISPLAY);
    expect(manifest.theme_color).toBe(PWA_THEME_COLOR);
    expect(manifest.background_color).toBe(PWA_BACKGROUND_COLOR);
    expect(PWA_ICON_FILES).toEqual(
      expect.arrayContaining([
        "icon-192.png",
        "icon-512.png",
        "icon-192-maskable.png",
        "icon-512-maskable.png",
      ]),
    );
    expect(manifest.icons.some((icon) => icon.sizes === "192x192" && icon.purpose === "any")).toBe(
      true,
    );
    expect(
      manifest.icons.some((icon) => icon.sizes === "512x512" && icon.purpose === "maskable"),
    ).toBe(true);
  });

  it("keeps API traffic on NetworkOnly patterns", () => {
    expect(PWA_API_PATH_PATTERN.test("/api/sales")).toBe(true);
    expect(PWA_API_PATH_PATTERN.test("/platform-api/api/v1/platform/auth/me")).toBe(true);
    expect(PWA_PLATFORM_API_PREFIX_PATTERN.test("/platform-api/api/v1/platform/auth/login")).toBe(
      true,
    );
    expect(PWA_PLATFORM_API_PREFIX_PATTERN.test("/appearance")).toBe(false);
    expect(PWA_API_PORT_PATTERN.test("http://127.0.0.1:8091/health")).toBe(true);
    expect(PWA_API_PORT_PATTERN.test("http://127.0.0.1:8092/sales")).toBe(true);
    expect(PWA_API_PORT_PATTERN.test("http://127.0.0.1:4175/")).toBe(false);
  });

  it("exposes a build/release identifier for diagnostics", () => {
    expect(getAppVersion()).toBe(PWA_DEFAULT_APP_VERSION);
  });
});

describe("PWA update apply", () => {
  it("does not apply until the user action is allowed", () => {
    const apply = vi.fn();
    expect(applyPwaUpdateIfAllowed(apply, () => false)).toBe(false);
    expect(apply).not.toHaveBeenCalled();
    expect(applyPwaUpdateIfAllowed(apply)).toBe(true);
    expect(apply).toHaveBeenCalledTimes(1);
  });
});

describe("PWA update notice", () => {
  it("stays hidden until an update is waiting", () => {
    renderNotice(false);
    expect(screen.queryByRole("status")).not.toBeInTheDocument();
  });

  it("shows English copy and requires an explicit Refresh", async () => {
    const user = userEvent.setup();
    const { onRefresh } = renderNotice(true);
    expect(onRefresh).not.toHaveBeenCalled();
    expect(screen.getByRole("status")).toHaveTextContent("New version available");
    await user.click(screen.getByRole("button", { name: "Refresh" }));
    expect(onRefresh).toHaveBeenCalledTimes(1);
  });

  it("shows Filipino copy", () => {
    window.localStorage.setItem(
      UI_PREFERENCES_STORAGE_KEY,
      JSON.stringify({ theme: "light", locale: "fil-PH" }),
    );
    renderNotice(true);
    expect(screen.getByRole("status")).toHaveTextContent("May bagong bersyon");
    expect(screen.getByRole("button", { name: "I-refresh" })).toBeInTheDocument();
  });

  it("uses surface tokens in Light and Dark", () => {
    window.localStorage.setItem(
      UI_PREFERENCES_STORAGE_KEY,
      JSON.stringify({ theme: "light", locale: "en" }),
    );
    const { unmount } = renderNotice(true);
    expect(document.documentElement.dataset.theme).toBe("light");
    expect(screen.getByRole("status").querySelector("div")).toHaveClass("bg-surface");
    unmount();
    window.localStorage.setItem(
      UI_PREFERENCES_STORAGE_KEY,
      JSON.stringify({ theme: "dark", locale: "en" }),
    );
    renderNotice(true);
    expect(document.documentElement.dataset.theme).toBe("dark");
    expect(screen.getByRole("status").querySelector("div")).toHaveClass("bg-surface");
  });

  it("does not apply when a future dirty-state guard blocks", async () => {
    const user = userEvent.setup();
    const onRefresh = vi.fn();
    render(
      <AppProviders>
        <PwaUpdateNotice visible onRefresh={onRefresh} guard={() => false} />
      </AppProviders>,
    );
    await user.click(screen.getByRole("button", { name: "Refresh" }));
    expect(onRefresh).not.toHaveBeenCalled();
  });
});
