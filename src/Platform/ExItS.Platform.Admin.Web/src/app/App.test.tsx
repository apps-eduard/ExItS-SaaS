import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { App } from "@/app/App";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";

describe("App foundation", () => {
  it("renders the scaffold through router and QueryClient providers", () => {
    render(<App />);

    expect(screen.getByRole("heading", { name: "ExItS Platform Admin Web" })).toBeInTheDocument();
    expect(screen.getByText(/Design foundation preview/i)).toBeInTheDocument();
  });

  it("defaults to System theme, English, and Balanced density", () => {
    render(<App />);

    expect(document.documentElement.dataset.theme).toBe("system");
    expect(document.documentElement.lang).toBe("en");
    expect(document.documentElement.dataset.density).toBe("balanced");
    expect(screen.getByRole("button", { name: "System" })).toHaveAttribute("aria-pressed", "true");
    expect(screen.getByRole("button", { name: "English" })).toHaveAttribute("aria-pressed", "true");
    expect(screen.getByRole("button", { name: "Balanced" })).toHaveAttribute(
      "aria-pressed",
      "true",
    );
  });

  it("applies Light and Dark theme selections and persists them", async () => {
    const user = userEvent.setup();
    const { unmount } = render(<App />);

    await user.click(screen.getByRole("button", { name: "Light" }));
    expect(document.documentElement.dataset.theme).toBe("light");
    expect(JSON.parse(window.localStorage.getItem(UI_PREFERENCES_STORAGE_KEY) ?? "{}").theme).toBe(
      "light",
    );

    await user.click(screen.getByRole("button", { name: "Dark" }));
    expect(document.documentElement.dataset.theme).toBe("dark");
    unmount();

    render(<App />);
    expect(document.documentElement.dataset.theme).toBe("dark");
    expect(screen.getByRole("button", { name: "Dark" })).toHaveAttribute("aria-pressed", "true");
  });

  it("keeps System mode as an explicit preference so OS color-scheme can drive tokens", async () => {
    const user = userEvent.setup();
    render(<App />);

    await user.click(screen.getByRole("button", { name: "Dark" }));
    await user.click(screen.getByRole("button", { name: "System" }));
    expect(document.documentElement.dataset.theme).toBe("system");
    expect(JSON.parse(window.localStorage.getItem(UI_PREFERENCES_STORAGE_KEY) ?? "{}").theme).toBe(
      "system",
    );
  });

  it("switches to Filipino, updates document language, and shows translated labels", async () => {
    const user = userEvent.setup();
    render(<App />);

    await user.click(screen.getByRole("button", { name: "Filipino" }));
    expect(document.documentElement.lang).toBe("fil-PH");
    expect(screen.getByText(/Paunang pagtingin sa disenyo/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Filipino" })).toHaveAttribute(
      "aria-pressed",
      "true",
    );
    expect(screen.getByRole("button", { name: "Pangunahing aksyon" })).toBeInTheDocument();
  });

  it("applies Comfortable and Compact density and persists Compact", async () => {
    const user = userEvent.setup();
    const { unmount } = render(<App />);

    await user.click(screen.getByRole("button", { name: "Comfortable" }));
    expect(document.documentElement.dataset.density).toBe("comfortable");

    await user.click(screen.getByRole("button", { name: "Compact" }));
    expect(document.documentElement.dataset.density).toBe("compact");
    unmount();

    render(<App />);
    expect(document.documentElement.dataset.density).toBe("compact");
    expect(screen.getByRole("button", { name: "Compact" })).toHaveAttribute("aria-pressed", "true");
  });

  it("falls back to defaults when stored preferences are corrupt", () => {
    window.localStorage.setItem(UI_PREFERENCES_STORAGE_KEY, "{not-json");
    render(<App />);
    expect(document.documentElement.dataset.theme).toBe("system");
    expect(document.documentElement.lang).toBe("en");
    expect(document.documentElement.dataset.density).toBe("balanced");
  });
});
