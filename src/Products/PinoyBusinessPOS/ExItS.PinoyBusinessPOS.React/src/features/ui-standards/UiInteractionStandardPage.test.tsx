import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ToastProvider } from "@/components/exits/ToastProvider";
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
      <ToastProvider>
        <MemoryRouter initialEntries={["/ui-standard"]}>
          <Routes>
            <Route path="/ui-standard" element={<UiInteractionStandardPage />} />
            <Route path="/ui-standards" element={<UiStandardsPage />} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>,
    );
    expect(screen.getByTestId("ui-standards-page")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-cards")).toBeInTheDocument();
  });
});

describe("ui-standard-catalog filters", () => {
  it("filters live cards and catalog rows by search", () => {
    const toastCards = filterLiveCards("all", "toast");
    expect(toastCards.map((c) => c.id)).toEqual(["toasts"]);
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
});
