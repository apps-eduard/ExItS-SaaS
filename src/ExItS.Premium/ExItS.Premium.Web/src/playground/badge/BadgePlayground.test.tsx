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

    const preview = screen.getByRole("heading", { name: "Preview", level: 2 }).parentElement;
    expect(preview).not.toBeNull();
    const previewCursor = within(preview!).getByTestId("cursor-copy-command");
    expect(previewCursor).toHaveAttribute("data-command", "ext badge success");

    const paid = within(preview!).getByText("Paid").closest("[data-tone]");
    expect(paid).toHaveAttribute("data-tone", "success");
    expect(paid).toHaveAttribute("data-appearance", "soft");

    await user.click(screen.getByRole("combobox", { name: "Appearance, Soft" }));
    await user.click(screen.getByRole("option", { name: "Solid" }));
    expect(within(preview!).getByText("Paid").closest("[data-tone]")).toHaveAttribute("data-appearance", "solid");
    expect(previewCursor).toHaveAttribute("data-command", "ext badge success solid");

    await user.click(screen.getByRole("combobox", { name: "Tone, Success" }));
    await user.click(screen.getByRole("option", { name: "Warning" }));
    expect(within(preview!).getByText("Due").closest("[data-tone]")).toHaveAttribute("data-tone", "warning");
    expect(previewCursor).toHaveAttribute("data-command", "ext badge warning solid");

    await user.click(screen.getByRole("switch", { name: "All caps" }));
    expect(within(preview!).getByText("Due").closest("[data-tone]")).toHaveAttribute("data-all-caps", "true");
    expect(previewCursor).toHaveAttribute("data-command", "ext badge warning solid all-caps");

    await user.click(screen.getByRole("switch", { name: "Indicator" }));
    expect(within(preview!).getByText("Due").closest("[data-tone]")).toHaveAttribute("data-indicator", "true");
    expect(previewCursor).toHaveAttribute("data-command", "ext badge warning solid all-caps indicator");

    await user.click(screen.getByRole("switch", { name: "Remove" }));
    expect(within(preview!).getByText("Due").closest("[data-tone]")).toHaveAttribute("data-removable", "true");
    expect(previewCursor).toHaveAttribute("data-command", "ext badge warning solid all-caps indicator remove");
    await user.click(within(preview!).getByRole("button", { name: "Remove" }));
    expect(within(preview!).getByText(/Removed/i)).toBeInTheDocument();
  });

  it("orders Count above Indicator and keeps Cursor copy samples", async () => {
    const user = userEvent.setup();
    render(<BadgePlayground />);

    const examples = screen.getByRole("heading", { name: "States & examples", level: 2 }).parentElement;
    expect(examples).not.toBeNull();

    const labels = [...examples!.querySelectorAll("p")].map((node) => node.textContent);
    const countIndex = labels.indexOf("Count");
    const indicatorIndex = labels.indexOf("Indicator");
    const indicatorOnlyIndex = labels.indexOf("Indicator only");
    expect(countIndex).toBeGreaterThan(-1);
    expect(indicatorIndex).toBeGreaterThan(countIndex);
    expect(indicatorOnlyIndex).toBeGreaterThan(indicatorIndex);

    const exampleCursors = within(examples!).getAllByTestId("cursor-copy-command");
    expect(exampleCursors).toHaveLength(3);
    expect(exampleCursors[0]).toHaveAttribute("data-command", "ext badge primary solid count");
    expect(exampleCursors[1]).toHaveAttribute("data-command", "ext badge success indicator");
    expect(exampleCursors[2]).toHaveAttribute("data-command", "ext badge danger indicator-only");

    await user.click(screen.getByRole("button", { name: "Count sample danger solid" }));
    expect(exampleCursors[0]).toHaveAttribute("data-command", "ext badge danger solid count");

    await user.click(screen.getByRole("button", { name: "Indicator sample warning" }));
    expect(exampleCursors[1]).toHaveAttribute("data-command", "ext badge warning indicator");

    await user.click(screen.getByRole("button", { name: "Indicator only sample info" }));
    expect(exampleCursors[2]).toHaveAttribute("data-command", "ext badge info indicator-only");
  });
});

