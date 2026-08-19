import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AppProviders } from "@/app/providers";
import { applyPwaUpdateIfAllowed } from "@/pwa/apply-pwa-update";
import { PwaUpdateNotice } from "@/pwa/PwaUpdateNotice";
import {
  createPwaManifest,
  PWA_API_PATH_PATTERN,
  PWA_AUTH_PATH_PATTERN,
  PWA_PLATFORM_API_PATH_PATTERN,
  PWA_THEME_COLOR,
} from "@/pwa/pwa-manifest";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";

describe("PWA manifest", () => {
  it("declares installable standalone identity", () => {
    const manifest = createPwaManifest();
    expect(manifest.name).toBe("Pinoy Loan Manager");
    expect(manifest.short_name).toBe("PinoyLoan");
    expect(manifest.start_url).toBe("/");
    expect(manifest.display).toBe("standalone");
    expect(manifest.theme_color).toBe(PWA_THEME_COLOR);
    expect(manifest.description.toLowerCase()).not.toContain("foundation");
    expect(PWA_API_PATH_PATTERN.test("/api/loans")).toBe(true);
    expect(PWA_PLATFORM_API_PATH_PATTERN.test("/platform-api/api/v1/platform/auth/me")).toBe(true);
    expect(PWA_AUTH_PATH_PATTERN.test("/api/v1/platform/auth/me")).toBe(true);
    expect(PWA_API_PATH_PATTERN.test("/appearance")).toBe(false);
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
    render(
      <AppProviders>
        <PwaUpdateNotice visible={false} onRefresh={vi.fn()} />
      </AppProviders>,
    );
    expect(screen.queryByRole("status")).not.toBeInTheDocument();
  });

  it("requires an explicit Refresh and never auto-applies", async () => {
    const user = userEvent.setup();
    const onRefresh = vi.fn();
    render(
      <AppProviders>
        <PwaUpdateNotice visible onRefresh={onRefresh} />
      </AppProviders>,
    );
    expect(onRefresh).not.toHaveBeenCalled();
    expect(screen.getByRole("status")).toHaveTextContent("Update available");
    await user.click(screen.getByRole("button", { name: "Refresh" }));
    expect(onRefresh).toHaveBeenCalledTimes(1);
  });

  it("shows Filipino copy", () => {
    window.localStorage.setItem(
      UI_PREFERENCES_STORAGE_KEY,
      JSON.stringify({ theme: "light", locale: "fil-PH" }),
    );
    render(
      <AppProviders>
        <PwaUpdateNotice visible onRefresh={vi.fn()} />
      </AppProviders>,
    );
    expect(screen.getByRole("status")).toHaveTextContent("May update");
    expect(screen.getByRole("button", { name: "I-refresh" })).toBeInTheDocument();
  });

  it("respects a future unsaved-work guard", async () => {
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
