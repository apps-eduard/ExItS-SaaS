import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { AppProviders } from "@/app/providers";
import { DevPortHealthPanel } from "@/features/auth/DevPortHealthPanel";

vi.mock("@/api/platform/local-validation-gate", () => ({
  isFrontendLocalValidationMode: () => true,
}));

describe("DevPortHealthPanel", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("renders port and server name without IP addresses", async () => {
    vi.spyOn(globalThis, "fetch").mockImplementation(() =>
      Promise.resolve(
        new Response(
          JSON.stringify({
            checkedAtUtc: "2026-09-07T00:00:00.000Z",
            ports: [
              { port: 8091, name: "Platform API", up: true },
              { port: 8092, name: "POS API", up: false },
            ],
          }),
          { status: 200, headers: { "Content-Type": "application/json" } },
        ),
      ),
    );

    render(
      <AppProviders>
        <DevPortHealthPanel />
      </AppProviders>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("dev-port-health-8091")).toBeInTheDocument();
    });

    expect(screen.getByTestId("dev-port-health-8091")).toHaveAttribute("data-up", "true");
    expect(screen.getByTestId("dev-port-health-8092")).toHaveAttribute("data-up", "false");
    expect(screen.getByTestId("dev-port-health")).toHaveTextContent(":8091");
    expect(screen.getByTestId("dev-port-health")).toHaveTextContent("Platform API");
    expect(screen.getByTestId("dev-port-health")).toHaveTextContent("POS API");
    expect(screen.getByTestId("dev-port-health").textContent).not.toMatch(
      /127\.0\.0\.1|localhost/i,
    );

    const callsBefore = vi.mocked(globalThis.fetch).mock.calls.length;
    fireEvent.click(screen.getByTestId("dev-port-health-refresh"));
    await waitFor(() => {
      expect(globalThis.fetch).toHaveBeenCalledTimes(callsBefore + 1);
    });
  });
});
