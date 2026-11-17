import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AppearanceProvider } from "../theme/AppearanceProvider";
import { PersonalShell } from "./PersonalShell";

describe("personal shell sidebar", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("keeps a single header toggle and reflows between expanded and collapsed", async () => {
    const user = userEvent.setup();
    render(
      <AppearanceProvider>
        <PersonalShell title="Home" name="Ada Lovelace" email="ada@example.com" onLogout={() => undefined}>
          <p>Body</p>
        </PersonalShell>
      </AppearanceProvider>,
    );

    const shell = document.querySelector("[data-collapsed]") as HTMLElement;
    expect(shell).toHaveAttribute("data-collapsed", "false");
    expect(screen.getAllByRole("button", { name: /sidebar/i })).toHaveLength(1);

    const collapse = screen.getByRole("button", { name: "Collapse sidebar" });
    expect(collapse).toHaveAttribute("aria-expanded", "true");
    await user.click(collapse);

    expect(shell).toHaveAttribute("data-collapsed", "true");
    expect(window.localStorage.getItem("exits.premium.sidebar")).toBe("collapsed");
    expect(screen.getAllByRole("button", { name: /sidebar/i })).toHaveLength(1);

    const expand = screen.getByRole("button", { name: "Expand sidebar" });
    expect(expand).toHaveAttribute("aria-expanded", "false");
    expect(screen.getByRole("button", { name: "Home" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Utang" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Stores to Pay" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Settings" })).toBeInTheDocument();
    expect(document.querySelectorAll('[aria-label="Expand sidebar"]')).toHaveLength(1);

    await user.click(expand);
    expect(shell).toHaveAttribute("data-collapsed", "false");
    expect(window.localStorage.getItem("exits.premium.sidebar")).toBe("expanded");
    expect(screen.getByRole("button", { name: "Collapse sidebar" })).toHaveAttribute("aria-expanded", "true");
  });

  it("toggles light and dark from the topbar icon", async () => {
    const user = userEvent.setup();
    render(
      <AppearanceProvider>
        <PersonalShell title="Home" name="Ada Lovelace" email="ada@example.com" onLogout={() => undefined}>
          <p>Body</p>
        </PersonalShell>
      </AppearanceProvider>,
    );

    const toggle = screen.getByRole("button", { name: /Switch to (light|dark) mode/ });
    const first = toggle.getAttribute("aria-label");
    await user.click(toggle);
    expect(toggle.getAttribute("aria-label")).not.toBe(first);
    expect(document.documentElement.getAttribute("data-theme")).toMatch(/light|dark/);
  });

  it("restores the collapsed preference on load", () => {
    window.localStorage.setItem("exits.premium.sidebar", "collapsed");
    render(
      <AppearanceProvider>
        <PersonalShell title="Home" name="Ada Lovelace" email="ada@example.com" onLogout={() => undefined}>
          <p>Body</p>
        </PersonalShell>
      </AppearanceProvider>,
    );

    expect(document.querySelector("[data-collapsed]")).toHaveAttribute("data-collapsed", "true");
    expect(screen.getByRole("button", { name: "Expand sidebar" })).toBeInTheDocument();
    expect(screen.getAllByRole("button", { name: /sidebar/i })).toHaveLength(1);
    const sidebar = screen.getByLabelText("Personal");
    expect(within(sidebar).getByRole("button", { name: "Home" })).toBeInTheDocument();
  });

  it("opens a compact account menu with appearance and sign out rows", async () => {
    const user = userEvent.setup();
    const onLogout = vi.fn();
    const onSwitchProfile = vi.fn().mockResolvedValue(true);
    render(
      <AppearanceProvider>
        <PersonalShell
          title="Home"
          name="Ada Lovelace"
          email="ada@example.com"
          onLogout={onLogout}
          onSwitchProfile={onSwitchProfile}
        >
          <p>Body</p>
        </PersonalShell>
      </AppearanceProvider>,
    );

    await user.click(screen.getByRole("button", { name: "Account menu for Ada Lovelace" }));
    const menu = screen.getByRole("menu", { name: "Account menu for Ada Lovelace" });
    expect(within(menu).getByText("Ada Lovelace")).toBeInTheDocument();
    expect(within(menu).getByText("Personal")).toBeInTheDocument();
    expect(within(menu).getByText("ada@example.com")).toBeInTheDocument();
    expect(within(menu).getByRole("menuitem", { name: /Switch profile/i })).toBeInTheDocument();
    expect(within(menu).getByRole("menuitem", { name: /Appearance/i })).toBeInTheDocument();

    await user.click(within(menu).getByRole("menuitem", { name: /Appearance/i }));
    await user.click(screen.getByRole("menuitemradio", { name: "Dark" }));
    expect(document.documentElement.getAttribute("data-theme")).toBe("dark");

    await user.click(screen.getByRole("button", { name: "Account menu for Ada Lovelace" }));
    await user.click(screen.getByRole("menuitem", { name: /Switch profile/i }));
    expect(onSwitchProfile).toHaveBeenCalledOnce();

    await user.click(screen.getByRole("button", { name: "Account menu for Ada Lovelace" }));
    await user.keyboard("{Escape}");
    expect(screen.queryByRole("menu", { name: "Account menu for Ada Lovelace" })).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Account menu for Ada Lovelace" })).toHaveFocus();

    await user.click(screen.getByRole("button", { name: "Account menu for Ada Lovelace" }));
    await user.click(screen.getByRole("menuitem", { name: /Sign out/i }));
    expect(onLogout).toHaveBeenCalledOnce();
  });
});
