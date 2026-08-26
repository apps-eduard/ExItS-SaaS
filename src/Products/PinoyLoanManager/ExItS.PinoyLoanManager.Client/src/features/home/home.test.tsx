import type { ReactElement } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Route, Routes } from "react-router-dom";
import { HomePage } from "@/features/home/HomePage";
import { AppShell } from "@/layouts/AppShell";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";
import { ORG_ALLOWED, stubAccessFetch } from "@/test/access-mocks";
import { renderWithAccessGate } from "@/test/render";

function renderApp(ui: ReactElement = <HomePage />) {
  stubAccessFetch();
  return renderWithAccessGate(
    <Routes>
      <Route element={<AppShell />}>
        <Route path="/" element={ui} />
      </Route>
    </Routes>,
  );
}

const forbidden = ["Preview", "Foundation", "coming soon", "1,250.00", "Online", "Synced"];

describe("product home", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("renders the product surface without demo or package copy", async () => {
    renderApp();
    expect(await screen.findByRole("heading", { name: "Pinoy Loan Manager" })).toBeInTheDocument();
    expect(
      screen.getByText(/Workspace is ready. Lending operations are not available in this gate./i),
    ).toBeInTheDocument();
    expect(screen.getByText(ORG_ALLOWED.displayName)).toBeInTheDocument();
    const page = document.body.textContent ?? "";
    for (const phrase of forbidden) {
      expect(page).not.toMatch(new RegExp(phrase, "i"));
    }
    expect(page).not.toMatch(/₱|PHP 1|borrower|disbursement|collection route/i);
  });

  it("starts with English and System and persists language and theme", async () => {
    const user = userEvent.setup();
    renderApp();
    await screen.findByRole("heading", { name: "Pinoy Loan Manager" });
    expect(document.documentElement.lang).toBe("en");
    expect(document.documentElement.dataset.theme).toBe("system");

    await user.click(screen.getByRole("radio", { name: "Dark" }));
    expect(document.documentElement.dataset.theme).toBe("dark");

    await user.click(screen.getByRole("radio", { name: "Filipino" }));
    expect(document.documentElement.lang).toBe("fil-PH");
    expect(screen.getByText(/Handa na ang workspace/i)).toBeInTheDocument();

    const stored = JSON.parse(window.localStorage.getItem(UI_PREFERENCES_STORAGE_KEY) ?? "{}") as {
      theme?: string;
      locale?: string;
    };
    expect(stored.theme).toBe("dark");
    expect(stored.locale).toBe("fil-PH");
  });

  it("moves keyboard focus onto a language control", async () => {
    const user = userEvent.setup();
    renderApp();
    await screen.findByRole("heading", { name: "Pinoy Loan Manager" });
    const english = screen.getByRole("radio", { name: "English" });
    await user.tab();
    await user.tab();
    english.focus();
    expect(document.activeElement).toBe(english);
  });
});
