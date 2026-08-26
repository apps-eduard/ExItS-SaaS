import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AppProviders } from "@/app/providers";
import { POS_PWA_NEED_REFRESH_EVENT, PwaUpdateHost } from "@/pwa/PwaUpdateHost";

const registerSW = vi.fn();

vi.mock("virtual:pwa-register", () => ({
  registerSW: (options?: { onNeedRefresh?: () => void }) => registerSW(options),
}));

describe("PWA update host", () => {
  afterEach(() => {
    registerSW.mockClear();
  });

  it("does not register a service worker during Vite development", async () => {
    render(
      <AppProviders>
        <PwaUpdateHost />
      </AppProviders>,
    );
    await waitFor(() => {
      expect(screen.getByTestId("pwa-update-host")).toHaveAttribute("data-ready", "true");
    });
    expect(registerSW).not.toHaveBeenCalled();
    expect(screen.queryByRole("status")).not.toBeInTheDocument();
  });

  it("can surface the notice from the test refresh event without registering SW in development", async () => {
    const user = userEvent.setup();
    render(
      <AppProviders>
        <PwaUpdateHost />
      </AppProviders>,
    );
    window.dispatchEvent(new Event(POS_PWA_NEED_REFRESH_EVENT));
    expect(await screen.findByRole("status")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Refresh" }));
    expect(registerSW).not.toHaveBeenCalled();
  });
});
