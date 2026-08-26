import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AppProviders } from "@/app/providers";
import { PLM_PWA_NEED_REFRESH_EVENT, PwaUpdateHost } from "@/pwa/PwaUpdateHost";

const updateServiceWorker = vi.fn().mockResolvedValue(undefined);

vi.mock("virtual:pwa-register", () => ({
  registerSW: (options?: { onNeedRefresh?: () => void }) => {
    queueMicrotask(() => options?.onNeedRefresh?.());
    return updateServiceWorker;
  },
}));

describe("PWA update host", () => {
  afterEach(() => {
    updateServiceWorker.mockClear();
  });

  it("shows a user-triggered update notice and applies once", async () => {
    const user = userEvent.setup();
    render(
      <AppProviders>
        <PwaUpdateHost />
      </AppProviders>,
    );
    expect(await screen.findByRole("status")).toHaveTextContent("Update available");
    const refresh = screen.getByRole("button", { name: "Refresh" });
    await user.click(refresh);
    await user.click(refresh);
    await waitFor(() => {
      expect(updateServiceWorker).toHaveBeenCalledTimes(1);
    });
  });

  it("can surface the notice from the test refresh event", async () => {
    render(
      <AppProviders>
        <PwaUpdateHost />
      </AppProviders>,
    );
    window.dispatchEvent(new Event(PLM_PWA_NEED_REFRESH_EVENT));
    expect(await screen.findByRole("status")).toBeInTheDocument();
  });
});
