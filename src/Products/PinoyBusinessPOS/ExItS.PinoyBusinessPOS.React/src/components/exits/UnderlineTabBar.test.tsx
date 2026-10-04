import { render, screen } from "@testing-library/react";
import { describe, expect, it, beforeEach } from "vitest";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import { PreferencesProvider } from "@/hooks/usePreferences";
import {
  defaultUiPreferences,
  writeUiPreferences,
} from "@/lib/preferences/ui-preferences";

function renderBar() {
  return render(
    <PreferencesProvider>
      <UnderlineTabBar
        ariaLabel="Sections"
        activeKey="catalog"
        onChange={() => {}}
        testId="sections"
        items={[
          { key: "overview", label: "Overview" },
          { key: "catalog", label: "Catalog" },
        ]}
      />
    </PreferencesProvider>,
  );
}

describe("UnderlineTabBar tab style preference", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("uses section chips when the preference is tabs", () => {
    renderBar();
    expect(screen.getByTestId("sections")).toHaveClass("exits-chip-bar");
  });

  it("uses underline tabs when that preference is saved", () => {
    writeUiPreferences({ ...defaultUiPreferences, tabStyle: "underline" });
    renderBar();
    expect(screen.getByRole("tablist")).toHaveAttribute("data-variant", "underline");
    expect(screen.getByRole("tab", { name: "Catalog" })).toHaveAttribute("aria-selected", "true");
  });
});
