import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { ButtonPlayground } from "./ButtonPlayground";

describe("ButtonPlayground", () => {
  it("arranges Properties left of Preview and updates the live button", async () => {
    const user = userEvent.setup();
    const { container } = render(<ButtonPlayground />);

    expect(screen.getByRole("heading", { name: "Button", level: 1 })).toBeInTheDocument();
    expect(screen.getByRole("complementary", { name: "Properties" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Preview", level: 2 })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "States & examples", level: 2 })).toBeInTheDocument();

    const workspace = container.querySelector("[class*='workspace']");
    const panes = workspace
      ? [...workspace.children].map((node) => node.getAttribute("aria-label") || node.querySelector("h2")?.textContent)
      : [];
    expect(panes[0]).toBe("Properties");
    expect(panes[1]).toBe("Preview");

    expect(screen.getByRole("combobox", { name: "Tone, Primary" })).toBeInTheDocument();
    expect(screen.getByRole("combobox", { name: "Appearance, Solid" })).toBeInTheDocument();

    const preview = screen.getByRole("heading", { name: "Preview", level: 2 }).parentElement;
    expect(preview).not.toBeNull();
    expect(within(preview!).getByRole("button", { name: "Save" })).toHaveAttribute("data-appearance", "solid");

    await user.click(screen.getByRole("combobox", { name: "Appearance, Solid" }));
    await user.click(screen.getByRole("option", { name: "Soft" }));
    expect(within(preview!).getByRole("button", { name: "Save" })).toHaveAttribute("data-appearance", "soft");

    await user.click(screen.getByRole("switch", { name: "Disabled" }));
    expect(within(preview!).getByRole("button", { name: "Save" })).toBeDisabled();
  });
});
