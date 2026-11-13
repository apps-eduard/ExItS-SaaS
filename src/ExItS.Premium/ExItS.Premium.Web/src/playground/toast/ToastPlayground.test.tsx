import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { ToastProvider } from "../../components/toast/ToastProvider";
import { ToastPlayground } from "./ToastPlayground";

function renderPlayground() {
  return render(
    <ToastProvider>
      <ToastPlayground />
    </ToastProvider>,
  );
}

describe("ToastPlayground", () => {
  it("arranges Properties left of Preview and shows a toast from the preview action", async () => {
    const user = userEvent.setup();
    const { container } = renderPlayground();

    expect(screen.getByRole("heading", { name: "Toasts", level: 1 })).toBeInTheDocument();
    expect(screen.getByRole("complementary", { name: "Properties" })).toBeInTheDocument();
    expect(container.querySelector("section")).toHaveAttribute("data-panes", "equal");
    expect(container.querySelector("section")).toHaveAttribute("data-properties-columns", "2");

    const preview = screen.getByRole("heading", { name: "Preview", level: 2 }).parentElement;
    expect(preview).not.toBeNull();
    const previewCursor = within(preview!).getByTestId("cursor-copy-command");
    expect(previewCursor).toHaveAttribute("data-command", "ext toast success description");

    await user.click(within(preview!).getByRole("button", { name: "Show toast" }));
    expect(await screen.findByRole("status")).toHaveTextContent("Saved");
    expect(screen.getByRole("status")).toHaveTextContent("The change is local to this preview.");
  });

  it("updates the Cursor command when tone and description change", async () => {
    const user = userEvent.setup();
    renderPlayground();

    const preview = screen.getByRole("heading", { name: "Preview", level: 2 }).parentElement;
    const previewCursor = within(preview!).getByTestId("cursor-copy-command");

    await user.click(screen.getByRole("combobox", { name: "Tone, Success" }));
    await user.click(screen.getByRole("option", { name: "Danger" }));
    expect(previewCursor).toHaveAttribute("data-command", "ext toast danger description");

    await user.click(screen.getByRole("switch", { name: "Description" }));
    expect(previewCursor).toHaveAttribute("data-command", "ext toast danger");
  });
});
