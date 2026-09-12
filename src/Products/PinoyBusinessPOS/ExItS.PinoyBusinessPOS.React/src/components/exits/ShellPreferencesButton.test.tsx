import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { ShellPreferencesButton } from "@/components/exits/ShellPreferencesButton";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";

function renderButton() {
  return render(
    <MemoryRouter initialEntries={["/role/manager"]}>
      <ShellPreferencesButton label="Preferences" />
    </MemoryRouter>,
  );
}

describe("ShellPreferencesButton ambient settings gear", () => {
  beforeEach(() => {
    document.documentElement.dataset.motion = "system";
    document.documentElement.dataset.primary = "blue";
    document.documentElement.dataset.theme = "light";
    window.localStorage.removeItem(UI_PREFERENCES_STORAGE_KEY);
    Object.defineProperty(window, "matchMedia", {
      writable: true,
      value: vi.fn().mockImplementation((query: string) => ({
        matches: false,
        media: query,
        onchange: null,
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
      })),
    });
  });

  afterEach(() => {
    cleanup();
    delete document.documentElement.dataset.motion;
  });

  it("links to Preferences without changing data-primary or storage", async () => {
    renderButton();
    const link = screen.getByTestId("shell-preferences-button");
    expect(link).toHaveAttribute("href", "/settings/preferences");
    expect(link).toHaveAttribute("aria-label", "Preferences");
    expect(link).toHaveAttribute("title", "Preferences");

    await waitFor(() => {
      expect(link).toHaveAttribute("data-ambient-settings", "on");
    });

    expect(document.documentElement.dataset.primary).toBe("blue");
    expect(window.localStorage.getItem(UI_PREFERENCES_STORAGE_KEY)).toBeNull();
  });

  it("enables ambient class when motion is allowed", async () => {
    renderButton();
    const gear = await screen.findByTestId("shell-preferences-button-gear");
    await waitFor(() => {
      expect(gear.getAttribute("class") ?? "").toContain("exits-settings-gear--ambient");
    });
    expect(gear).toHaveAttribute("data-ambient-color");
    expect(gear.getAttribute("data-ambient-color")).not.toBe("primary");
  });

  it("disables rotation and color cycle when Motion=Reduced", async () => {
    document.documentElement.dataset.motion = "reduced";
    renderButton();
    const link = screen.getByTestId("shell-preferences-button");
    await waitFor(() => {
      expect(link).toHaveAttribute("data-ambient-settings", "off");
    });
    const gear = screen.getByTestId("shell-preferences-button-gear");
    expect(gear.getAttribute("class") ?? "").not.toContain("exits-settings-gear--ambient");
    expect(gear.getAttribute("class") ?? "").toContain("text-[var(--exits-primary)]");
    expect(gear).toHaveAttribute("data-ambient-color", "primary");
    expect(document.documentElement.dataset.primary).toBe("blue");
  });

  it("disables ambient animation when OS prefers-reduced-motion under System", async () => {
    Object.defineProperty(window, "matchMedia", {
      writable: true,
      value: vi.fn().mockImplementation((query: string) => ({
        matches: query.includes("prefers-reduced-motion"),
        media: query,
        onchange: null,
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
      })),
    });
    document.documentElement.dataset.motion = "system";
    renderButton();
    await waitFor(() => {
      expect(screen.getByTestId("shell-preferences-button")).toHaveAttribute(
        "data-ambient-settings",
        "off",
      );
    });
  });

  it("clears the color-cycle timer on unmount", async () => {
    const clearSpy = vi.spyOn(window, "clearInterval");
    const { unmount } = renderButton();
    await waitFor(() => {
      expect(screen.getByTestId("shell-preferences-button")).toHaveAttribute(
        "data-ambient-settings",
        "on",
      );
    });
    unmount();
    expect(clearSpy).toHaveBeenCalled();
    clearSpy.mockRestore();
  });
});
