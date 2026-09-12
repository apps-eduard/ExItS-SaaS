import { beforeEach, describe, expect, it } from "vitest";
import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { renderAuthenticatedAt } from "@/test/render";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";
import {
  isPreferencesDestination,
  isSafePreferencesReturnPath,
} from "@/features/preferences/preferences-return";
import {
  parsePreferencesSection,
  preferencesSectionPath,
} from "@/features/preferences/preferences-sections";

describe("preferences sections helpers", () => {
  it("defaults Appearance path and parses section ids", () => {
    expect(preferencesSectionPath()).toBe("/settings/preferences/appearance");
    expect(parsePreferencesSection("/settings/preferences/appearance")).toBe("appearance");
    expect(parsePreferencesSection("/settings/preferences/language-region")).toBe(
      "language-region",
    );
    expect(parsePreferencesSection("/settings/preferences")).toBeNull();
  });

  it("treats nested preference paths as destinations, not return targets", () => {
    expect(isPreferencesDestination("/settings/preferences/appearance")).toBe(true);
    expect(isSafePreferencesReturnPath("/settings/preferences/appearance")).toBe(false);
    expect(isSafePreferencesReturnPath("/inventory")).toBe(true);
  });
});

describe("Preferences menu foundation", () => {
  beforeEach(() => {
    window.localStorage.removeItem(UI_PREFERENCES_STORAGE_KEY);
  });

  it("opens /settings/preferences on Appearance by default", async () => {
    renderAuthenticatedAt("/settings/preferences");
    await waitFor(() => {
      expect(screen.getByTestId("preferences-drawer")).toBeInTheDocument();
      expect(screen.getByTestId("preferences-section-appearance")).toBeInTheDocument();
    });
    expect(screen.getByTestId("preferences-section-nav")).toHaveAttribute(
      "data-active-section",
      "appearance",
    );
    expect(screen.getByTestId("preferences-nav-appearance")).toHaveAttribute(
      "aria-current",
      "page",
    );
    expect(screen.queryByRole("tab")).not.toBeInTheDocument();
    expect(screen.getByRole("radio", { name: "Theme: System" })).toBeInTheDocument();
    expect(screen.getByRole("radio", { name: "Density: Balance" })).toBeInTheDocument();
    expect(screen.queryByRole("radio", { name: "Language: English" })).not.toBeInTheDocument();
  });

  it("navigates Appearance, Language & Region, Navigation, and Accessibility", async () => {
    const user = userEvent.setup();
    renderAuthenticatedAt("/settings/preferences/appearance");

    await waitFor(() => {
      expect(screen.getByTestId("preferences-section-appearance")).toBeInTheDocument();
    });

    await user.click(screen.getByTestId("preferences-nav-language-region"));
    await waitFor(() => {
      expect(screen.getByTestId("preferences-section-language-region")).toBeInTheDocument();
    });
    expect(screen.getByTestId("preferences-nav-language-region")).toHaveAttribute(
      "aria-current",
      "page",
    );
    expect(screen.getByRole("radio", { name: "Language: English" })).toBeInTheDocument();

    await user.click(screen.getByTestId("preferences-nav-navigation"));
    await waitFor(() => {
      expect(screen.getByTestId("preferences-section-navigation")).toBeInTheDocument();
      expect(screen.getByTestId("preferences-navigation-empty")).toBeInTheDocument();
    });

    await user.click(screen.getByTestId("preferences-nav-accessibility"));
    await waitFor(() => {
      expect(screen.getByTestId("preferences-section-accessibility")).toBeInTheDocument();
      expect(screen.getByTestId("preferences-accessibility-empty")).toBeInTheDocument();
    });
  });

  it("keeps Theme, Density, and Language controls functional with persistence", async () => {
    const user = userEvent.setup();
    renderAuthenticatedAt("/settings/preferences/appearance");

    await waitFor(() => {
      expect(screen.getByRole("radio", { name: "Theme: System" })).toBeInTheDocument();
    });

    await user.click(screen.getByRole("radio", { name: "Theme: Dark" }));
    await waitFor(() => {
      expect(document.documentElement.dataset.theme).toBe("dark");
    });

    await user.click(screen.getByRole("radio", { name: "Density: Compact" }));
    await waitFor(() => {
      expect(document.documentElement.dataset.density).toBe("compact");
    });

    await user.click(screen.getByTestId("preferences-nav-language-region"));
    await waitFor(() => {
      expect(screen.getByRole("radio", { name: "Language: English" })).toBeInTheDocument();
    });
    await user.click(screen.getByRole("radio", { name: "Language: Filipino" }));
    await waitFor(() => {
      expect(document.documentElement.lang).toBe("fil-PH");
    });

    const stored = JSON.parse(window.localStorage.getItem(UI_PREFERENCES_STORAGE_KEY) ?? "{}") as {
      theme?: string;
      density?: string;
      locale?: string;
    };
    expect(stored.theme).toBe("dark");
    expect(stored.density).toBe("compact");
    expect(stored.locale).toBe("fil-PH");
  });

  it("renders mobile-friendly section nav without tab semantics", async () => {
    renderAuthenticatedAt("/settings/preferences/appearance");
    await waitFor(() => {
      expect(screen.getByTestId("preferences-section-nav")).toBeInTheDocument();
    });
    const nav = screen.getByTestId("preferences-section-nav");
    expect(nav.querySelector('[role="tablist"]')).toBeNull();
    expect(within(nav).getAllByRole("link")).toHaveLength(4);
    expect(screen.getByTestId("preferences-layout").className).toMatch(/flex-col/);
  });

  it("keeps logical start/end classes for RTL-safe layout", async () => {
    renderAuthenticatedAt("/settings/preferences/appearance");
    await waitFor(() => {
      expect(screen.getByTestId("preferences-layout")).toBeInTheDocument();
    });
    const rail = screen.getByTestId("preferences-section-nav").parentElement;
    expect(rail?.className).toMatch(/\bsm:border-e\b/);
    expect(rail?.className).toMatch(/\bsm:pe-3\b/);
    expect(rail?.className).not.toMatch(/\bborder-r\b/);
    expect(rail?.className).not.toMatch(/\bpr-3\b/);
  });
});
