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
    expect(toastCards.map((c) => c.id)).toEqual(expect.arrayContaining(["toasts", "messages"]));
    const danger = filterCatalogRows("all", "danger");
    expect(danger.some((r) => r.id === "button")).toBe(true);
    expect(danger.some((r) => r.id === "confirm")).toBe(true);
  });

  it("Selects category isolates select standards", () => {
    const cards = filterLiveCards("selects", "");
    expect(cards.map((c) => c.id)).toEqual(["selects"]);
    const rows = filterCatalogRows("selects", "");
    expect(rows.map((r) => r.id)).toEqual([
      "exits-select",
      "exits-multi-select",
      "exits-pill-select",
      "creatable-combobox",
      "settings-select",
    ]);
  });

  it("Messages category isolates message gallery", () => {
    expect(filterLiveCards("messages", "").map((c) => c.id)).toEqual(["messages"]);
    expect(filterCatalogRows("messages", "").map((r) => r.id)).toEqual(["message-gallery"]);
  });

  it("Forms category includes Form Controls and QuantityStepper cards", () => {
    expect(filterLiveCards("forms", "").map((c) => c.id)).toEqual(["forms", "quantityStepper"]);
    expect(filterCatalogRows("forms", "").map((r) => r.id)).toEqual(
      expect.arrayContaining(["input", "quantity-stepper"]),
    );
  });

  it("Upload category isolates upload standards", () => {
    expect(filterLiveCards("upload", "").map((c) => c.id)).toEqual(["upload"]);
    expect(filterLiveCards("all", "upload").map((c) => c.id)).toContain("upload");
    expect(filterCatalogRows("upload", "").map((r) => r.id)).toEqual(["exits-upload"]);
  });

  it("maps playground / auto / primary search to builder cards", () => {
    expect(filterLiveCards("all", "playground").map((c) => c.id)).toEqual(
      expect.arrayContaining(["buttons", "status", "selects", "dodont"]),
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
