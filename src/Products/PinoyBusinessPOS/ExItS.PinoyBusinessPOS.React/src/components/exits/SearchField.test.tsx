import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { SearchField } from "@/components/exits/SearchField";
import { PreferencesProvider } from "@/hooks/usePreferences";
import {
  defaultUiPreferences,
  UI_PREFERENCES_STORAGE_KEY,
  writeUiPreferences,
  applyUiPreferences,
} from "@/lib/preferences/ui-preferences";

const rootDir = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
const globalsCss = readFileSync(resolve(rootDir, "styles/globals.css"), "utf8");

function renderSearch(ui: ReactNode) {
  return render(<PreferencesProvider>{ui}</PreferencesProvider>);
}

describe("SearchField standard foundation", () => {
  beforeEach(() => {
    window.localStorage.removeItem(UI_PREFERENCES_STORAGE_KEY);
    applyUiPreferences(defaultUiPreferences);
  });

  it("renders search icon, placeholder, value, and clear action", async () => {
    const user = userEvent.setup();
    const onClear = vi.fn();
    const onChange = vi.fn();
    renderSearch(
      <SearchField
        label="Search products"
        placeholder="Search products..."
        value="coffee"
        onChange={onChange}
        onClear={onClear}
      />,
    );

    expect(screen.getByLabelText("Search products")).toHaveValue("coffee");
    expect(screen.getByPlaceholderText("Search products...")).toBeInTheDocument();
    expect(screen.getByTestId("exits-search-field").querySelector(".exits-search-field__icon")).toBeTruthy();
    await user.click(screen.getByLabelText("Clear search"));
    expect(onClear).toHaveBeenCalledOnce();
  });

  it("hides clear when empty and supports disabled", () => {
    const { rerender } = renderSearch(
      <SearchField label="Search" value="" onChange={() => undefined} />,
    );
    expect(screen.queryByLabelText("Clear search")).not.toBeInTheDocument();

    rerender(
      <PreferencesProvider>
        <SearchField label="Search" value="x" onChange={() => undefined} disabled />
      </PreferencesProvider>,
    );
    expect(screen.getByLabelText("Search")).toBeDisabled();
    expect(screen.queryByLabelText("Clear search")).not.toBeInTheDocument();
    expect(screen.getByTestId("exits-search-field").className).toMatch(/exits-search-field--disabled/);
  });

  it("Auto shape follows Control Shape; explicit shape overrides", () => {
    writeUiPreferences({ ...defaultUiPreferences, controlShape: "standard" });
    applyUiPreferences({ ...defaultUiPreferences, controlShape: "standard" });
    const { unmount } = renderSearch(
      <SearchField label="Search" value="" onChange={() => undefined} />,
    );
    expect(screen.getByTestId("exits-search-field")).toHaveAttribute("data-shape", "auto");
    expect(document.documentElement.dataset.controlShape).toBe("standard");
    unmount();

    writeUiPreferences({ ...defaultUiPreferences, controlShape: "pill" });
    applyUiPreferences({ ...defaultUiPreferences, controlShape: "pill" });
    renderSearch(
      <>
        <SearchField label="Auto" value="" onChange={() => undefined} />
        <SearchField label="Forced standard" value="" onChange={() => undefined} shape="standard" />
        <SearchField label="Forced pill" value="" onChange={() => undefined} shape="pill" />
      </>,
    );
    expect(document.documentElement.dataset.controlShape).toBe("pill");
    expect(screen.getByLabelText("Auto").closest(".exits-search-field")).toHaveAttribute(
      "data-shape",
      "auto",
    );
    expect(screen.getByLabelText("Forced standard").closest(".exits-search-field")).toHaveAttribute(
      "data-shape",
      "standard",
    );
    expect(screen.getByLabelText("Forced pill").closest(".exits-search-field")).toHaveAttribute(
      "data-shape",
      "pill",
    );
  });

  it("uses thin semantic focus tokens (not legacy thick ring-2 utilities)", () => {
    expect(globalsCss).toMatch(/\.exits-search-field:focus-within/);
    expect(globalsCss).toMatch(
      /\.exits-search-field:focus-within[\s\S]*?box-shadow:\s*0\s+0\s+0\s+(?:1px|var\(--exits-field-focus-ring-width\))/,
    );
    expect(globalsCss).toMatch(/\.exits-search-field\[data-shape="pill"\]/);
    expect(globalsCss).toMatch(/--exits-search-radius:\s*var\(--exits-control-radius\)/);
    expect(globalsCss).not.toMatch(
      /exits-search-field__input[\s\S]{0,200}focus-visible:ring-2/,
    );
  });

  it("keeps form fields on --exits-field-radius under Pill Control Shape", () => {
    expect(globalsCss).toMatch(/--exits-field-radius:\s*var\(--exits-radius-md\)/);
    expect(globalsCss).toMatch(
      /\.exits-input\s*\{[^}]*border-radius:\s*var\(--exits-field-radius/,
    );
    expect(globalsCss).toMatch(
      /select\.exits-input[\s\S]*?border-radius:\s*var\(--exits-field-radius/,
    );
    expect(globalsCss).toMatch(
      /\[data-control-shape="pill"\][\s\S]*?--exits-control-radius:\s*9999px/,
    );
    expect(globalsCss).toMatch(
      /\[data-control-shape="pill"\][\s\S]*?--exits-field-radius:\s*var\(--exits-radius-md\)/,
    );

    writeUiPreferences({ ...defaultUiPreferences, controlShape: "pill" });
    applyUiPreferences({ ...defaultUiPreferences, controlShape: "pill" });
    render(
      <PreferencesProvider>
        <input className="exits-input" aria-label="Name" data-testid="form-text" />
        <select className="exits-input" aria-label="Status" data-testid="form-select">
          <option>Active</option>
        </select>
        <textarea className="exits-input" aria-label="Notes" data-testid="form-textarea" />
        <SearchField label="Search people" value="" onChange={() => undefined} />
      </PreferencesProvider>,
    );

    expect(document.documentElement.dataset.controlShape).toBe("pill");
    expect(screen.getByTestId("form-text").className).toMatch(/exits-input/);
    expect(screen.getByTestId("form-select").className).toMatch(/exits-input/);
    expect(screen.getByTestId("form-textarea").className).toMatch(/exits-input/);
    // Form fields must not use control-radius utility / pill search shell.
    expect(screen.getByTestId("form-text").className).not.toMatch(/exits-search-field/);
    expect(screen.getByTestId("exits-search-field")).toHaveAttribute("data-shape", "auto");
  });

  it("places leading/clear with logical inset properties for RTL", () => {
    expect(globalsCss).toMatch(/\.exits-search-field__leading[\s\S]*?inset-inline-start/);
    expect(globalsCss).toMatch(/\.exits-search-field__clear[\s\S]*?inset-inline-end/);
    expect(globalsCss).toMatch(/padding-inline-start:\s*2\.35rem/);
  });

  it("shows loading spinner in leading slot", () => {
    renderSearch(
      <SearchField label="Search" value="" onChange={() => undefined} loading />,
    );
    expect(screen.getByTestId("exits-search-field").className).toMatch(/exits-search-field--loading/);
    expect(screen.getByTestId("exits-search-field").querySelector(".exits-search-field__spinner")).toBeTruthy();
  });
});
