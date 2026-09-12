import { beforeEach, describe, expect, it } from "vitest";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
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

const rootDir = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
const globalsCss = readFileSync(resolve(rootDir, "styles/globals.css"), "utf8");
const settingsSelectSource = readFileSync(
  resolve(rootDir, "components/ui/settings-select.tsx"),
  "utf8",
);

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
      expect(screen.getByTestId("preferences-navigation-mode")).toBeInTheDocument();
    });
    expect(screen.getByRole("radio", { name: "Sidebar: Standard" })).toBeInTheDocument();
    expect(screen.getByRole("radio", { name: "Sidebar: Compact" })).toBeInTheDocument();
    expect(screen.getByRole("radio", { name: "Sidebar: Reveal" })).toBeInTheDocument();
    expect(screen.queryByTestId("preferences-navigation-empty")).not.toBeInTheDocument();
    expect(screen.getByText(/Icons and labels\./)).toBeInTheDocument();
    expect(screen.getByText(/Permanent icon rail\./)).toBeInTheDocument();
    expect(screen.getByText(/Icon rail that expands on hover or focus\./)).toBeInTheDocument();

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

  it("supports Appearance foundation: primary swatches, control shape, motion", async () => {
    const user = userEvent.setup();
    renderAuthenticatedAt("/settings/preferences/appearance");

    await waitFor(() => {
      expect(screen.getByTestId("preferences-primary-color")).toBeInTheDocument();
    });

    for (const color of [
      "green",
      "teal",
      "cyan",
      "blue",
      "indigo",
      "violet",
      "fuchsia",
      "rose",
      "orange",
    ] as const) {
      const swatch = screen.getByTestId(`preferences-primary-${color}`);
      expect(swatch).toHaveAttribute("role", "radio");
      expect(swatch.tagName).toBe("BUTTON");
      await user.click(swatch);
      await waitFor(() => {
        expect(document.documentElement.dataset.primary).toBe(color);
        expect(document.documentElement.dataset.accent).toBe(color);
        expect(swatch).toHaveAttribute("aria-checked", "true");
        expect(swatch).toHaveAttribute("data-selected", "true");
      });
    }

    expect(screen.getByRole("radio", { name: "Green" })).toHaveAttribute("title", "Green");
    expect(screen.getByRole("radio", { name: "Teal" })).toHaveAttribute("title", "Teal");
    expect(screen.getByRole("radio", { name: "Cyan" })).toHaveAttribute("title", "Cyan");
    expect(screen.getByRole("radio", { name: "Orange" })).toHaveAttribute("title", "Orange");
    expect(screen.getByRole("radio", { name: "Violet" })).toHaveAttribute("title", "Violet");
    expect(screen.getByRole("radio", { name: "Fuchsia" })).toHaveAttribute("title", "Fuchsia");
    expect(screen.queryByTestId("preferences-primary-amber")).not.toBeInTheDocument();

    await user.click(screen.getByRole("radio", { name: "Control shape: Soft" }));
    await waitFor(() => {
      expect(document.documentElement.dataset.controlShape).toBe("soft");
    });
    await user.click(screen.getByRole("radio", { name: "Control shape: Pill" }));
    await waitFor(() => {
      expect(document.documentElement.dataset.controlShape).toBe("pill");
    });
    await user.click(screen.getByRole("radio", { name: "Control shape: Standard" }));
    await waitFor(() => {
      expect(document.documentElement.dataset.controlShape).toBe("standard");
    });

    expect(screen.getByTestId("preferences-animations")).toBeInTheDocument();
    expect(screen.getByText("Animations")).toBeInTheDocument();
    expect(
      screen.getByText("Controls interface transitions and decorative motion."),
    ).toBeInTheDocument();

    await user.click(screen.getByRole("radio", { name: "Animations: Reduced" }));
    await waitFor(() => {
      expect(document.documentElement.dataset.motion).toBe("reduced");
    });
    await user.click(screen.getByRole("radio", { name: "Animations: System" }));
    await waitFor(() => {
      expect(document.documentElement.dataset.motion).toBe("system");
    });

    const stored = JSON.parse(window.localStorage.getItem(UI_PREFERENCES_STORAGE_KEY) ?? "{}") as {
      primaryColor?: string;
      controlShape?: string;
      motion?: string;
    };
    expect(stored.primaryColor).toBe("orange");
    expect(stored.controlShape).toBe("standard");
    expect(stored.motion).toBe("system");

    // Semantic colors remain independent of Primary (token definitions intact).
    expect(globalsCss).toMatch(/--exits-success:/);
    expect(globalsCss).toMatch(/--exits-warning:/);
    expect(globalsCss).toMatch(/--exits-danger:/);
    expect(globalsCss).toMatch(/--exits-info:/);
    expect(globalsCss).toContain('[data-primary="rose"]');
    expect(globalsCss).toContain('[data-primary="teal"]');
    expect(globalsCss).toContain('[data-primary="cyan"]');
    expect(globalsCss).toContain('[data-primary="indigo"]');
    expect(globalsCss).toContain('[data-primary="fuchsia"]');
    expect(globalsCss).toContain('[data-control-shape="soft"]');
    expect(globalsCss).toContain('[data-control-shape="pill"]');
    expect(globalsCss).toContain('[data-motion="reduced"]');
  });

  it("keeps Appearance controls compact (segmented rows, plain panel, 9 swatches)", async () => {
    renderAuthenticatedAt("/settings/preferences/appearance");
    await waitFor(() => {
      expect(screen.getByTestId("preferences-section-appearance")).toBeInTheDocument();
    });

    expect(screen.getByTestId("preferences-section-appearance")).toHaveAttribute(
      "data-surface",
      "plain",
    );
    expect(settingsSelectSource).toContain('variant === "segmented"');
    expect(settingsSelectSource).toContain('data-settings-variant="segmented"');
    expect(settingsSelectSource).toContain("@min-[20rem]:grid-cols-2");
    expect(settingsSelectSource).toContain("rounded-[var(--exits-control-radius)]");

    expect(screen.getByTestId("preferences-control-shape")).toHaveAttribute(
      "data-settings-variant",
      "segmented",
    );
    expect(screen.getByTestId("preferences-motion")).toHaveAttribute(
      "data-settings-variant",
      "segmented",
    );

    const primary = screen.getByTestId("preferences-primary-color");
    expect(within(primary).getAllByRole("radio")).toHaveLength(9);
    expect(within(screen.getByTestId("preferences-control-shape")).getAllByRole("radio")).toHaveLength(
      3,
    );

    const nav = screen.getByTestId("preferences-section-nav");
    expect(nav.querySelector("ul")?.className).toMatch(/gap-2\.5/);
    expect(screen.getByTestId("preferences-nav-appearance").className).toContain(
      "rounded-[var(--exits-control-radius)]",
    );
  });

  it("supports Navigation mode: Standard, Compact, and Reveal with persistence", async () => {
    const user = userEvent.setup();
    renderAuthenticatedAt("/settings/preferences/navigation");

    await waitFor(() => {
      expect(screen.getByTestId("preferences-navigation-mode")).toBeInTheDocument();
    });

    expect(document.documentElement.dataset.navigationMode).toBe("standard");
    expect(screen.getByRole("radio", { name: "Sidebar: Standard" })).toHaveAttribute(
      "aria-checked",
      "true",
    );

    await user.click(screen.getByRole("radio", { name: "Sidebar: Compact" }));
    await waitFor(() => {
      expect(document.documentElement.dataset.navigationMode).toBe("compact");
    });
    expect(screen.getByRole("radio", { name: "Sidebar: Compact" })).toHaveAttribute(
      "aria-checked",
      "true",
    );

    await user.click(screen.getByRole("radio", { name: "Sidebar: Reveal" }));
    await waitFor(() => {
      expect(document.documentElement.dataset.navigationMode).toBe("reveal");
    });
    expect(screen.getByRole("radio", { name: "Sidebar: Reveal" })).toHaveAttribute(
      "aria-checked",
      "true",
    );

    await user.click(screen.getByRole("radio", { name: "Sidebar: Standard" }));
    await waitFor(() => {
      expect(document.documentElement.dataset.navigationMode).toBe("standard");
    });

    const stored = JSON.parse(window.localStorage.getItem(UI_PREFERENCES_STORAGE_KEY) ?? "{}") as {
      navigationMode?: string;
    };
    expect(stored.navigationMode).toBe("standard");

    expect(globalsCss).toContain('[data-navigation-mode="compact"]');
    expect(globalsCss).toContain('[data-navigation-mode="reveal"]');
    expect(globalsCss).toMatch(/--exits-shell-sidebar-width:\s*15\.5rem/);
    expect(globalsCss).toMatch(
      /html\[data-navigation-mode="compact"\][\s\S]*?--exits-shell-sidebar-width:\s*3\.75rem/,
    );
    expect(globalsCss).toMatch(
      /\[data-navigation-mode="reveal"\][\s\S]*?\.admin-sidebar\.admin-sidebar--expanded:hover/,
    );
    expect(globalsCss).toMatch(/\.admin-sidebar__label/);
    expect(globalsCss).toMatch(/border-inline-start/);
    expect(globalsCss).toMatch(/--exits-sidebar-reveal-duration:\s*280ms/);
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

  it("keeps the outer Preferences drawer compact on desktop (440px preferred)", () => {
    const block = globalsCss.match(
      /\.exits-side-drawer__panel--preferences\s*\{[\s\S]*?(?=\n\/\*|\n\.exits-|\n@media|\n:root|\n\[|$)/,
    )?.[0];
    expect(globalsCss).toMatch(
      /@media\s*\(min-width:\s*64rem\)\s*\{[\s\S]*?\.exits-side-drawer__panel--preferences[\s\S]*?width:\s*27\.5rem/,
    );
    expect(globalsCss).toMatch(
      /@media\s*\(min-width:\s*40rem\)\s*\{[\s\S]*?\.exits-side-drawer__panel--preferences[\s\S]*?min\(30rem,\s*70vw\)/,
    );
    expect(globalsCss).not.toMatch(/\.exits-side-drawer__panel--preferences\s*\{[^}]*38rem/);
    expect(globalsCss).not.toMatch(/\.exits-side-drawer__panel--preferences\s*\{[^}]*50rem/);
    expect(block ?? globalsCss).toMatch(/width:\s*100%/);
    expect(settingsSelectSource).toContain("@min-[20rem]:grid-cols-2");
  });
});
