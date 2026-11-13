import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { BadgePlayground } from "./BadgePlayground";

describe("BadgePlayground", () => {
  it("arranges Properties left of Preview and updates tone and appearance", async () => {
    const user = userEvent.setup();
    const { container } = render(<BadgePlayground />);

    expect(screen.getByRole("heading", { name: "Badges", level: 1 })).toBeInTheDocument();
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

    expect(screen.getByRole("combobox", { name: "Tone, Success" })).toBeInTheDocument();
    expect(screen.getByRole("combobox", { name: "Appearance, Soft" })).toBeInTheDocument();
    expect(screen.getByTestId("cursor-copy-command")).toHaveAttribute("data-command", "ext badge success");

    const preview = screen.getByRole("heading", { name: "Preview", level: 2 }).parentElement;
    expect(preview).not.toBeNull();
    const paid = within(preview!).getByText("Paid");
    expect(paid).toHaveAttribute("data-tone", "success");
    expect(paid).toHaveAttribute("data-appearance", "soft");

    await user.click(screen.getByRole("combobox", { name: "Appearance, Soft" }));
    await user.click(screen.getByRole("option", { name: "Solid" }));
    expect(within(preview!).getByText("Paid")).toHaveAttribute("data-appearance", "solid");
    expect(screen.getByTestId("cursor-copy-command")).toHaveAttribute("data-command", "ext badge success solid");

    await user.click(screen.getByRole("combobox", { name: "Tone, Success" }));
    await user.click(screen.getByRole("option", { name: "Warning" }));
    expect(within(preview!).getByText("Due")).toHaveAttribute("data-tone", "warning");
    expect(screen.getByTestId("cursor-copy-command")).toHaveAttribute("data-command", "ext badge warning solid");

    await user.click(screen.getByRole("switch", { name: "All caps" }));
    expect(within(preview!).getByText("Due")).toHaveAttribute("data-all-caps", "true");
    expect(screen.getByTestId("cursor-copy-command")).toHaveAttribute("data-command", "ext badge warning solid all-caps");

    await user.click(screen.getByRole("switch", { name: "Indicator" }));
    expect(within(preview!).getByText("Due")).toHaveAttribute("data-indicator", "true");
    expect(screen.getByTestId("cursor-copy-command")).toHaveAttribute(
      "data-command",
      "ext badge warning solid all-caps indicator",
    );
  });
});
