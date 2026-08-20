import { describe, expect, it } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useQueryClient } from "@tanstack/react-query";
import { renderApp, renderAt, renderAuthenticatedAt } from "@/test/render";
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
  it("renders the sign-in shell without privileged or financial content", async () => {
    renderAt("/sign-in");
    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Sign in" })).toBeInTheDocument();
    });
    const page = document.body.textContent ?? "";
    for (const phrase of forbiddenBusiness) {
      expect(page.toLowerCase()).not.toContain(phrase.toLowerCase());
    }
  });

  it("provides a TanStack Query client without issuing API queries", () => {
    renderApp(<QueryProbe />);
    expect(screen.getByTestId("query-client-ready")).toHaveTextContent("ready");
  });

  it("defaults to English and System theme on preferences", async () => {
    renderAuthenticatedAt("/settings/preferences");
    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Preferences" })).toBeInTheDocument();
      expect(screen.getByRole("radio", { name: "Language: English" })).toBeInTheDocument();
      expect(screen.getByRole("radio", { name: "Theme: System" })).toBeInTheDocument();
    });
    expect(document.documentElement.lang).toBe("en");
    expect(document.documentElement.dataset.theme).toBe("system");
  });

  it("switches to Filipino and persists locale from preferences", async () => {
    const user = userEvent.setup();
    renderAuthenticatedAt("/settings/preferences");
    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Preferences" })).toBeInTheDocument();
    });
    await user.click(screen.getByRole("radio", { name: "Language: Filipino" }));
    await waitFor(() => {
      expect(document.documentElement.lang).toBe("fil-PH");
    });
    const stored = JSON.parse(window.localStorage.getItem(UI_PREFERENCES_STORAGE_KEY) ?? "{}") as {
      locale?: string;
    };
    expect(stored.locale).toBe("fil-PH");
  });

  it("switches Light and Dark preferences globally from preferences", async () => {
    const user = userEvent.setup();
    renderAuthenticatedAt("/settings/preferences");
    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Preferences" })).toBeInTheDocument();
      expect(screen.getByRole("radio", { name: /Theme: System|Tema: System/ })).toBeInTheDocument();
    });
    await user.click(screen.getByRole("radio", { name: /Theme: Dark|Tema: Dark/ }));
    await waitFor(() => {
      expect(document.documentElement.dataset.theme).toBe("dark");
    });
    await user.click(screen.getByRole("radio", { name: /Theme: Light|Tema: Light/ }));
    await waitFor(() => {
      expect(document.documentElement.dataset.theme).toBe("light");
    });
    const stored = JSON.parse(window.localStorage.getItem(UI_PREFERENCES_STORAGE_KEY) ?? "{}") as {
      theme?: string;
    };
    expect(stored.theme).toBe("light");
  });

  it("renders a 404 route without business screens", async () => {
    renderAuthenticatedAt("/this-route-does-not-exist");
    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Page not found" })).toBeInTheDocument();
      expect(screen.getByRole("link", { name: "Back to home" })).toHaveAttribute("href", "/");
    });
  });

  it("uses a min-width-safe shell structure", async () => {
    const { container } = renderAt("/sign-in");
    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Sign in" })).toBeInTheDocument();
    });
    expect(container.querySelector(".min-w-0")).not.toBeNull();
  });
});
