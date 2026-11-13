import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { ToastProvider, useToast } from "./ToastProvider";

function Harness() {
  const toast = useToast();
  return (
    <>
      <button type="button" onClick={() => toast.push({ tone: "success", title: "Saved" })}>
        Save
      </button>
      <button type="button" onClick={() => toast.setPosition("top-start")}>
        Top start
      </button>
      <span data-testid="toast-position">{toast.position}</span>
    </>
  );
}

describe("ToastProvider", () => {
  it("announces a success toast and dismisses it", async () => {
    const user = userEvent.setup();
    render(
      <ToastProvider>
        <Harness />
      </ToastProvider>,
    );
    expect(screen.getByLabelText("Notifications")).toHaveAttribute("data-position", "bottom-end");
    await user.click(screen.getByRole("button", { name: "Save" }));
    expect(screen.getByRole("status")).toHaveTextContent("Saved");
    await user.click(screen.getByRole("button", { name: "Dismiss notification" }));
    expect(screen.queryByRole("status")).not.toBeInTheDocument();
  });

  it("moves the stack when position changes", async () => {
    const user = userEvent.setup();
    render(
      <ToastProvider>
        <Harness />
      </ToastProvider>,
    );
    await user.click(screen.getByRole("button", { name: "Top start" }));
    expect(screen.getByTestId("toast-position")).toHaveTextContent("top-start");
    expect(screen.getByLabelText("Notifications")).toHaveAttribute("data-position", "top-start");
  });
});
