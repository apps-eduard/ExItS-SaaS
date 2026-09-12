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

describe("Preferences icon top navigation", () => {
  beforeEach(() => {
    window.localStorage.removeItem(UI_PREFERENCES_STORAGE_KEY);
  });

  it("opens /settings/preferences on Appearance with icon-only top nav", async () => {
    renderAuthenticatedAt("/settings/preferences");
    await waitFor(() => {
      expect(screen.getByTestId("preferences-drawer")).toBeInTheDocument();
      expect(screen.getByTestId("preferences-section-appearance")).toBeInTheDocument();
    });

    const nav = screen.getByTestId("preferences-section-nav");
    expect(nav).toHaveAttribute("data-active-section", "appearance");
    expect(nav).toHaveAttribute("data-variant", "icon-top");
    expect(nav.querySelector('[role="tablist"]')).toBeNull();
    expect(screen.queryByRole("tab")).not.toBeInTheDocument();

    const appearance = screen.getByTestId("preferences-nav-appearance");
    expect(appearance).toHaveAttribute("aria-current", "page");
    expect(appearance).toHaveAttribute("aria-label", "Appearance");
    expect(appearance).toHaveAttribute("title", "Appearance");
    expect(within(appearance).queryByText("Appearance")).not.toBeInTheDocument();

    expect(screen.getByRole("radio", { name: "Theme: System" })).toBeInTheDocument();
    expect(screen.getByRole("radio", { name: "Density: Balance" })).toBeInTheDocument();
    expect(screen.queryByRole("radio", { name: "Language: English" })).not.toBeInTheDocument();
  });

  it("renders four icon navigation controls with labels and tooltips", async () => {
    renderAuthenticatedAt("/settings/preferences/appearance");
    await waitFor(() => {
      expect(screen.getByTestId("preferences-section-nav")).toBeInTheDocument();
    });

    const nav = screen.getByTestId("preferences-section-nav");
    const links = within(nav).getAllByRole("link");
    expect(links).toHaveLength(4);

    expect(screen.getByRole("link", { name: "Appearance" })).toHaveAttribute("title", "Appearance");
    expect(screen.getByRole("link", { name: "Language & Region" })).toHaveAttribute(
      "title",
      "Language & Region",
    );
    expect(screen.getByRole("link", { name: "Navigation" })).toHaveAttribute("title", "Navigation");
    expect(screen.getByRole("link", { name: "Accessibility" })).toHaveAttribute(
      "title",
      "Accessibility",
    );
  });

  it("navigates sections via icon top nav without a left menu", async () => {
    const user = userEvent.setup();
    renderAuthenticatedAt("/settings/preferences/appearance");

    await waitFor(() => {
      expect(screen.getByTestId("preferences-section-appearance")).toBeInTheDocument();
    });

    // No reserved left sidebar rail
    expect(screen.getByTestId("preferences-layout").className).toMatch(/flex-col/);
    expect(screen.getByTestId("preferences-layout").className).not.toMatch(/sm:flex-row/);
    expect(screen.queryByText(/^Language & Region$/)).not.toBeInTheDocument(); // no left-menu text label

    await user.click(screen.getByTestId("preferences-nav-language-region"));
    await waitFor(() => {
      expect(screen.getByTestId("preferences-section-language-region")).toBeInTheDocument();
    });
    expect(screen.getByTestId("preferences-nav-language-region")).toHaveAttribute(
      "aria-current",
      "page",
    );
    expect(screen.getByRole("heading", { name: "Language & Region" })).toBeInTheDocument();
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

    await user.click(screen.getByTestId("preferences-nav-appearance"));
    await waitFor(() => {
      expect(screen.getByTestId("preferences-section-appearance")).toBeInTheDocument();
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

  it("uses shared icon top nav on all widths (no separate mobile nav)", async () => {
    renderAuthenticatedAt("/settings/preferences/appearance");
    await waitFor(() => {
      expect(screen.getByTestId("preferences-section-nav")).toBeInTheDocument();
    });
    expect(screen.getByTestId("preferences-section-nav")).toHaveAttribute("data-variant", "icon-top");
    expect(screen.queryByTestId("preferences-section-nav-mobile")).not.toBeInTheDocument();
    expect(within(screen.getByTestId("preferences-section-nav")).getAllByRole("link")).toHaveLength(
      4,
    );
  });

  it("avoids physical left/right layout utilities for RTL-safe spacing", async () => {
    renderAuthenticatedAt("/settings/preferences/appearance");
    await waitFor(() => {
      expect(screen.getByTestId("preferences-layout")).toBeInTheDocument();
    });
    const layout = screen.getByTestId("preferences-layout");
    expect(layout.className).not.toMatch(/\bborder-r\b/);
    expect(layout.className).not.toMatch(/\bpr-\d/);
    expect(layout.className).not.toMatch(/\bml-\d/);
    expect(layout.className).not.toMatch(/\bmr-\d/);
    expect(screen.getByTestId("preferences-section-content").className).not.toContain(
      "sm:max-w-[30rem]",
    );
  });
});
