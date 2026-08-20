import { describe, expect, it } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useQueryClient } from "@tanstack/react-query";
import { renderApp, renderAt } from "@/test/render";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";

const forbiddenBusiness = [
  "1,250.00",
  "₱",
  "checkout",
  "shopping cart",
  "customer ledger",
  "store name",
  "cashier shift",
];

function QueryProbe() {
  const client = useQueryClient();
  return <p data-testid="query-client-ready">{client ? "ready" : "missing"}</p>;
}

describe("POS React foundation", () => {
  it("renders the foundation shell without privileged or financial content", () => {
    renderApp();
    expect(screen.getByRole("heading", { name: "Pinoy Business POS" })).toBeInTheDocument();
    expect(screen.getByText("React client foundation")).toBeInTheDocument();
    expect(screen.getByText("PWA foundation will be added next")).toBeInTheDocument();
    const page = document.body.textContent ?? "";
    for (const phrase of forbiddenBusiness) {
      expect(page.toLowerCase()).not.toContain(phrase.toLowerCase());
    }
  });

  it("provides a TanStack Query client without issuing API queries", () => {
    renderApp(<QueryProbe />);
    expect(screen.getByTestId("query-client-ready")).toHaveTextContent("ready");
  });

  it("defaults to English and System theme", () => {
    renderApp();
    expect(document.documentElement.lang).toBe("en");
    expect(document.documentElement.dataset.theme).toBe("system");
    expect(screen.getByRole("radio", { name: /English/ })).toHaveAttribute("aria-checked", "true");
    expect(screen.getByRole("radio", { name: /System/ })).toHaveAttribute("aria-checked", "true");
  });

  it("switches to Filipino and persists locale", async () => {
    const user = userEvent.setup();
    renderApp();
    await user.click(screen.getByRole("radio", { name: /Filipino/ }));
    expect(document.documentElement.lang).toBe("fil-PH");
    expect(screen.getByText("Pundasyon ng React client")).toBeInTheDocument();
    expect(screen.getByText("Idadagdag ang PWA foundation sa susunod")).toBeInTheDocument();
    const stored = JSON.parse(window.localStorage.getItem(UI_PREFERENCES_STORAGE_KEY) ?? "{}") as {
      locale?: string;
    };
    expect(stored.locale).toBe("fil-PH");
  });

  it("switches Light and Dark preferences globally", async () => {
    const user = userEvent.setup();
    renderApp();
    await user.click(screen.getByRole("radio", { name: /Dark/ }));
    expect(document.documentElement.dataset.theme).toBe("dark");
    await user.click(screen.getByRole("radio", { name: /Light/ }));
    expect(document.documentElement.dataset.theme).toBe("light");
    const stored = JSON.parse(window.localStorage.getItem(UI_PREFERENCES_STORAGE_KEY) ?? "{}") as {
      theme?: string;
    };
    expect(stored.theme).toBe("light");
  });

  it("renders a 404 route without business screens", () => {
    renderAt("/this-route-does-not-exist");
    expect(screen.getByRole("heading", { name: "Page not found" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Back to foundation" })).toHaveAttribute("href", "/");
  });

  it("uses a min-width-safe shell structure", () => {
    const { container } = renderApp();
    expect(container.querySelector(".min-w-0")).not.toBeNull();
    expect(container.querySelector(".overflow-x-hidden")).not.toBeNull();
  });
});
