import type { ReactElement } from "react";
import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { AppErrorBoundary } from "@/components/exits/AppErrorBoundary";
import { AppTopBar } from "@/components/exits/AppTopBar";
import { ErrorState } from "@/components/exits/ErrorState";
import { AppearancePage } from "@/features/appearance/AppearancePage";
import { HomePage } from "@/features/home/HomePage";
import { PersonalShell } from "@/features/personal/PersonalShell";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";
import { motionDurationMs } from "@/lib/motion";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";

function renderShell(ui: ReactElement, path = "/") {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={[path]}>{ui}</MemoryRouter>
    </AppProviders>,
  );
}

const forbiddenCopy = [
  "Preview",
  "Foundation",
  "1,250.00",
  "Online",
  "Synced",
  "no workspace selected",
  "Sell",
  "Inventory",
];

describe("product home", () => {
  it("renders a commercial start surface without demo or package copy", () => {
    renderShell(<HomePage />);
    expect(screen.getByRole("heading", { name: "ExItS Mobile" })).toBeInTheDocument();
    expect(screen.getByText(/business and personal ExItS experience/i)).toBeInTheDocument();
    const page = document.body.textContent ?? "";
    for (const phrase of forbiddenCopy) {
      expect(page).not.toMatch(new RegExp(phrase, "i"));
    }
  });
});

describe("product chrome", () => {
  it("keeps a compact top bar without fake workspace or connectivity", () => {
    renderShell(<AppTopBar />);
    const header = screen.getByRole("banner");
    expect(header).toHaveTextContent("ExItS Mobile");
    expect(screen.getByRole("button", { name: "Settings" })).toBeInTheDocument();
    expect(header).not.toHaveTextContent("Preview");
    expect(header).not.toHaveTextContent("Online");
    expect(header).not.toHaveTextContent("Offline");
    expect(header).not.toHaveTextContent("Synced");
    expect(header).not.toHaveTextContent("workspace");
  });

  it("renders personal bottom navigation without POS business destinations", () => {
    renderShell(<PersonalShell />);
    expect(screen.getByRole("navigation", { name: "Personal navigation" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "People" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Invitations" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Alerts" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Appearance" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Sell" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Orders" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Customers" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Inventory" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Reports" })).not.toBeInTheDocument();
  });
});

describe("appearance settings", () => {
  it("starts with System and English and persists changes", async () => {
    const user = userEvent.setup();
    renderShell(<AppearancePage />, "/appearance");

    expect(document.documentElement.dataset.theme).toBe("system");
    expect(document.documentElement.lang).toBe("en");
    expect(screen.getByRole("heading", { name: "Appearance" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Back" })).toBeInTheDocument();

    await user.click(screen.getByRole("radio", { name: "Dark" }));
    expect(document.documentElement.dataset.theme).toBe("dark");

    await user.click(screen.getByRole("radio", { name: "Filipino" }));
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

describe("error diagnostics", () => {
  it("Copy Diagnostics uses a generic message, not the sample Error text", async () => {
    const writeText = vi.spyOn(navigator.clipboard, "writeText").mockResolvedValue(undefined);
    const user = userEvent.setup();
    const record = normalizeDiagnosticError(new Error("Unable to complete this operation."), {
      locale: "en",
      theme: "light",
      pathname: "/",
      createReference: () => "ERR-HOME",
      now: () => "2026-08-19T00:00:00.000Z",
      browserPlatform: "test",
    });
    renderShell(
      <ErrorState
        title="Something went wrong"
        body="Unable to complete this operation."
        record={record}
      />,
    );
    await user.click(screen.getByRole("button", { name: "Copy" }));
    expect(writeText).toHaveBeenCalledTimes(1);
    const payload = String(writeText.mock.calls[0]?.[0]);
    expect(payload).toContain("Unexpected client error.");
    expect(payload).not.toContain("Unable to complete this operation.");
    writeText.mockRestore();
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
    expect(screen.getByRole("alert")).not.toHaveTextContent("Simulated foundation runtime error");
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
