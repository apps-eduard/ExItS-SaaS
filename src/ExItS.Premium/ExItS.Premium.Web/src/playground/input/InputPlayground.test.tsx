import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { InputPlayground } from "./InputPlayground";

describe("InputPlayground", () => {
  it("arranges Properties left of Preview and updates the live field", async () => {
    const user = userEvent.setup();
    const { container } = render(<InputPlayground />);

    expect(screen.getByRole("heading", { name: "Inputs", level: 1 })).toBeInTheDocument();
    expect(screen.getByRole("complementary", { name: "Properties" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Preview", level: 2 })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "States & examples", level: 2 })).toBeInTheDocument();
    expect(container.querySelector("section")).toHaveAttribute("data-panes", "equal");
    expect(container.querySelector("section")).toHaveAttribute("data-properties-columns", "2");

    const workspace = container.querySelector("[class*='workspace']");
    const panes = workspace
      ? [...workspace.children].map((node) => node.getAttribute("aria-label") || node.querySelector("h2")?.textContent)
      : [];
    expect(panes[0]).toBe("Properties");
    expect(panes[1]).toBe("Preview");

    expect(screen.getByRole("combobox", { name: "Appearance, Standard" })).toBeInTheDocument();
    expect(screen.getByRole("combobox", { name: "Focus Animation, Border Reveal" })).toBeInTheDocument();

    const preview = screen.getByRole("heading", { name: "Preview", level: 2 }).parentElement;
    expect(preview).not.toBeNull();
    expect(within(preview!).getByRole("textbox", { name: "Name" })).toHaveAttribute("data-focus-motion", "border-reveal");

    await user.click(screen.getByRole("combobox", { name: "Appearance, Standard" }));
    await user.click(screen.getByRole("option", { name: "Outlined" }));
    expect(within(preview!).getByRole("textbox", { name: "Name" })).toHaveAttribute("data-appearance", "outlined");

    await user.click(screen.getByRole("switch", { name: "Disabled" }));
    expect(within(preview!).getByRole("textbox", { name: "Name" })).toBeDisabled();
  });
});
