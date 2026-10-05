import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { AppProviders } from "@/app/providers";
import { PwaUpdateHost } from "@/pwa/PwaUpdateHost";

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
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("does not show an update prompt", async () => {
    render(
      <AppProviders>
        <PwaUpdateHost />
      </AppProviders>,
    );
    await waitFor(() => {
      expect(screen.getByTestId("pwa-update-host")).toHaveAttribute("data-ready", "true");
    });
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Refresh now" })).not.toBeInTheDocument();
  });
});
