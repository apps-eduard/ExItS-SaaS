import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { GlobalErrorBoundary } from "@/diagnostics/GlobalErrorBoundary";
import { I18nProvider } from "@/i18n/I18nProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";

function BrokenChild(): never {
  throw new Error("TileGrid is not defined");
}

describe("GlobalErrorBoundary", () => {
  it("renders copyable diagnostics for render crashes", () => {
    render(
      <PreferencesProvider>
        <I18nProvider>
          <GlobalErrorBoundary>
            <BrokenChild />
          </GlobalErrorBoundary>
        </I18nProvider>
      </PreferencesProvider>,
    );

    expect(screen.getByTestId("client-error-panel")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Copy error details" })).toBeInTheDocument();
    const report = screen.getByTestId("client-error-report") as HTMLTextAreaElement;
    expect(report.value).toContain("TileGrid is not defined");
  });
});
