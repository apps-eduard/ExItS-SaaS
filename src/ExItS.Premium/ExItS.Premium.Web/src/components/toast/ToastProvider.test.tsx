import { act, fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import toastCss from "./Toast.module.scss?raw";
import { TOAST_DISMISS_MS, ToastProvider, useToast } from "./ToastProvider";

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

  it("counts down with the dismiss timer, pauses on hover, and resets on the next toast", async () => {
    vi.useFakeTimers();
    render(
      <ToastProvider>
        <Harness />
      </ToastProvider>,
    );

    expect(toastCss).toContain(`--toast-dismiss: ${TOAST_DISMISS_MS}ms`);
    expect(toastCss).toContain("animation: toast-countdown var(--toast-dismiss) linear forwards");
    expect(toastCss).toContain("animation-play-state: paused");
    expect(toastCss).toContain("display: none");

    fireEvent.click(screen.getByRole("button", { name: "Save" }));
    const toast = screen.getByRole("status");
    expect(toast.querySelector("[data-toast-countdown]")).toBeTruthy();
    expect(toast.querySelector("[data-toast-countdown]")).toHaveAttribute("aria-hidden", "true");

    await act(async () => {
      await vi.advanceTimersByTimeAsync(32);
    });
    expect(toast).toHaveAttribute("data-countdown", "run");

    await act(async () => {
      await vi.advanceTimersByTimeAsync(1000);
    });
    fireEvent.mouseEnter(toast);
    expect(toast).toHaveAttribute("data-paused", "true");
    await act(async () => {
      await vi.advanceTimersByTimeAsync(TOAST_DISMISS_MS);
    });
    expect(screen.getByRole("status")).toBeInTheDocument();

    fireEvent.mouseLeave(toast);
    expect(toast).not.toHaveAttribute("data-paused");
    await act(async () => {
      await vi.advanceTimersByTimeAsync(TOAST_DISMISS_MS - 1000 - 30);
    });
    expect(screen.getByRole("status")).toBeInTheDocument();
    await act(async () => {
      await vi.advanceTimersByTimeAsync(50);
    });
    expect(screen.queryByRole("status")).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Save" }));
    const reopened = screen.getByRole("status");
    await act(async () => {
      await vi.advanceTimersByTimeAsync(32);
    });
    expect(reopened).toHaveAttribute("data-countdown", "run");
    expect(reopened).not.toHaveAttribute("data-paused");
    fireEvent.focus(screen.getByRole("button", { name: "Dismiss notification" }));
    expect(reopened).toHaveAttribute("data-paused", "true");
    await act(async () => {
      await vi.advanceTimersByTimeAsync(TOAST_DISMISS_MS);
    });
    expect(screen.getByRole("status")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Dismiss notification" }));
    expect(screen.queryByRole("status")).not.toBeInTheDocument();
  });
});

afterEach(() => {
  vi.useRealTimers();
});
