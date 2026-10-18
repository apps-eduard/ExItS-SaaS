import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ToastProvider } from "@/components/exits/ToastProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { filterLiveCards, filterCatalogRows } from "@/features/ui-standards/ui-standard-catalog";
import { UiInteractionStandardPage } from "@/features/ui-standards/UiInteractionStandardPage";
import { UiStandardsPage } from "@/features/ui-standards/UiStandardsPage";

vi.mock("@/api/platform/local-validation-gate", () => ({
  isFrontendLocalValidationMode: () => true,
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}));

describe("UiInteractionStandardPage alias", () => {
  it("redirects /ui-standard to /ui-standards", () => {
    render(
      <PreferencesProvider>
        <ToastProvider>
          <MemoryRouter initialEntries={["/ui-standard"]}>
            <Routes>
              <Route path="/ui-standard" element={<UiInteractionStandardPage />} />
              <Route path="/ui-standards" element={<UiStandardsPage />} />
            </Routes>
          </MemoryRouter>
        </ToastProvider>
      </PreferencesProvider>,
    );
    expect(screen.getByTestId("ui-standards-page")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-cards")).toBeInTheDocument();
  });
});

describe("ui-standard-catalog filters", () => {
  it("filters live cards and catalog rows by search", () => {
    const toastCards = filterLiveCards("all", "toast");
    expect(toastCards.map((c) => c.id)).toEqual(["messages"]);
    const danger = filterCatalogRows("all", "danger");
    expect(danger.some((r) => r.id === "button")).toBe(true);
    expect(danger.some((r) => r.id === "confirm")).toBe(true);
  });

  it("Select and Tabs select own the select standards", () => {
    expect(filterCatalogRows("dropdown", "").map((r) => r.id)).toEqual(
      expect.arrayContaining(["exits-select", "exits-multi-select", "creatable-combobox"]),
    );
    expect(filterCatalogRows("tabs-select", "").map((r) => r.id)).toEqual([
      "exits-pill-select",
      "settings-select",
    ]);
    expect(filterLiveCards("tabs-select", "").map((c) => c.id)).toEqual(["tabsSelect"]);
  });

  it("Radio and Checkbox categories isolate their cards", () => {
    expect(filterLiveCards("radio", "").map((c) => c.id)).toEqual(["radio"]);
    expect(filterLiveCards("checkbox", "").map((c) => c.id)).toEqual(["checkbox"]);
    expect(filterLiveCards("toggle", "").map((c) => c.id)).toEqual(["toggle"]);
    expect(filterLiveCards("dropdown", "").map((c) => c.id)).toEqual(["dropdown"]);
  });

  it("Breadcrumbs category isolates the breadcrumb card", () => {
    expect(filterLiveCards("breadcrumbs", "").map((c) => c.id)).toEqual(["breadcrumbs"]);
  });

  it("Input category isolates the input card", () => {
    expect(filterLiveCards("inputs", "").map((c) => c.id)).toEqual(["inputs"]);
    const rows = filterCatalogRows("inputs", "");
    expect(rows.map((r) => r.id)).toEqual(["input-playground"]);
    expect(rows[0]?.standard).toBe("Input");
    expect(rows[0]?.status).toBe("Locked");
  });

  it("Messages category isolates message gallery", () => {
    expect(filterLiveCards("messages", "").map((c) => c.id)).toEqual(["messages"]);
    const rows = filterCatalogRows("messages", "");
    expect(rows.map((r) => r.id)).toEqual(["message-gallery"]);
    expect(rows[0]?.standard).toBe("Message");
    expect(rows[0]?.status).toBe("Locked");
  });

  it("Button stepper category shows the QuantityStepper standard", () => {
    expect(filterLiveCards("button-stepper", "").map((c) => c.id)).toEqual(["quantityStepper"]);
    expect(filterCatalogRows("button-stepper", "").map((r) => r.id)).toEqual(["quantity-stepper"]);
  });

  it("Upload category isolates upload standards", () => {
    expect(filterLiveCards("upload", "").map((c) => c.id)).toEqual(["upload"]);
    expect(filterLiveCards("all", "upload").map((c) => c.id)).toContain("upload");
    expect(filterCatalogRows("upload", "").map((r) => r.id)).toEqual(["exits-upload"]);
  });

  it("maps playground / auto / primary search to builder cards", () => {
    expect(filterLiveCards("all", "playground").map((c) => c.id)).toEqual(
      expect.arrayContaining(["buttons", "status", "dodont"]),
    );
    expect(filterLiveCards("all", "auto").map((c) => c.id)).toEqual(
      expect.arrayContaining(["buttons", "status"]),
    );
    expect(filterLiveCards("all", "primary").map((c) => c.id)).toEqual(
      expect.arrayContaining(["buttons", "dodont"]),
    );
    expect(filterLiveCards("buttons", "").map((c) => c.id)).toEqual(
      expect.arrayContaining(["buttons", "dodont"]),
    );
  });
});
