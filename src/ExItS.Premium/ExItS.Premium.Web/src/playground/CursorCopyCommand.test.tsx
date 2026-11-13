import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import * as color from "./color";
import { CursorCopyCommand, formatCursorClipboard } from "./CursorCopyCommand";

describe("CursorCopyCommand", () => {
  it("shows the command and copies the full Cursor prompt", async () => {
    const user = userEvent.setup();
    const spy = vi.spyOn(color, "copyText").mockResolvedValue(true);
    render(<CursorCopyCommand standard="Button" command="ext btn ripple" />);

    expect(screen.getByText("Cursor")).toBeInTheDocument();
    expect(screen.getByText("ext btn ripple")).toBeInTheDocument();
    expect(screen.getByTestId("cursor-copy-command")).toHaveAttribute("data-command", "ext btn ripple");

    await user.click(screen.getByRole("button", { name: "Copy Cursor command" }));
    expect(spy).toHaveBeenCalledWith(formatCursorClipboard("Button", "ext btn ripple"));
    expect(await screen.findByRole("button", { name: "Copied Cursor command" })).toBeInTheDocument();
  });
});
