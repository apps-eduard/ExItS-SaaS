import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { AppearancePage } from "@/features/foundation/AppearancePage";
import { FoundationHomePage } from "@/features/foundation/FoundationHomePage";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";
import { motionDurationMs } from "@/lib/motion";
import { AppErrorBoundary } from "@/components/exits/AppErrorBoundary";

function renderAppearance() {
  return render(
    <AppProviders>
      <MemoryRouter>
        <AppearancePage />
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("foundation appearance", () => {
  it("starts with System and English and persists changes", async () => {
    const user = userEvent.setup();
    renderAppearance();

    expect(document.documentElement.dataset.theme).toBe("system");
    expect(document.documentElement.lang).toBe("en");
    expect(screen.getByRole("heading", { name: "Appearance" })).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Dark" }));
    expect(document.documentElement.dataset.theme).toBe("dark");

    await user.click(screen.getByRole("button", { name: "Filipino" }));
    expect(document.documentElement.lang).toBe("fil-PH");
    expect(screen.getByRole("heading", { name: "Hitsura" })).toBeInTheDocument();

    const stored = JSON.parse(window.localStorage.getItem(UI_PREFERENCES_STORAGE_KEY) ?? "{}") as {
      theme?: string;
      locale?: string;
    };
    expect(stored.theme).toBe("dark");
    expect(stored.locale).toBe("fil-PH");
  });
});

describe("foundation home", () => {
  it("renders the operational shell preview without claiming live POS", () => {
    render(
      <AppProviders>
        <MemoryRouter>
          <FoundationHomePage />
        </MemoryRouter>
      </AppProviders>,
    );
    expect(screen.getByRole("heading", { name: "Client foundation" })).toBeInTheDocument();
    expect(screen.getByText(/not a store/i)).toBeInTheDocument();
    expect(screen.getByText("1,250.00")).toHaveClass("tabular-nums");
  });
});

describe("error boundary", () => {
  it("shows Copy Diagnostics on a runtime error", () => {
    function Boom(): null {
      throw new Error("Simulated foundation runtime error");
    }
    const consoleError = vi.spyOn(console, "error").mockImplementation(() => undefined);
    render(
      <AppProviders>
        <AppErrorBoundary>
          <Boom />
        </AppErrorBoundary>
      </AppProviders>,
    );
    expect(screen.getByRole("alert")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Copy" })).toBeInTheDocument();
    consoleError.mockRestore();
  });
});

describe("reduced motion", () => {
  it("returns zero duration when the user prefers reduced motion", () => {
    window.matchMedia = (query: string) =>
      ({
        matches: query.includes("prefers-reduced-motion"),
        media: query,
        onchange: null,
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
        addListener: () => undefined,
        removeListener: () => undefined,
        dispatchEvent: () => true,
      }) as MediaQueryList;
    expect(motionDurationMs(180)).toBe(0);
  });
});
