import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { AppProviders } from "@/app/providers";
import { DevPortHealthPanel } from "@/features/auth/DevPortHealthPanel";

const fetchSupervisorHealth = vi.fn();
const fetchSupervisorOperation = vi.fn();
const restartSupervisorService = vi.fn();
const restartAllSupervisorApps = vi.fn();
const resetLocalValidationData = vi.fn();

vi.mock("@/api/platform/local-validation-gate", () => ({
  isFrontendLocalValidationMode: () => true,
}));

vi.mock("@/api/local-validation-supervisor-client", () => ({
  LOCAL_VALIDATION_SUPERVISOR_ORIGIN: "http://127.0.0.1:8099",
  LOCAL_VALIDATION_SUPERVISOR_PROXY_PREFIX: "/__dev__/lv-supervisor",
  resolveSupervisorBaseUrl: () => "/__dev__/lv-supervisor",
  isLocalValidationControlHost: () => true,
  fetchSupervisorHealth: (...args: unknown[]) => fetchSupervisorHealth(...args),
  fetchSupervisorOperation: (...args: unknown[]) => fetchSupervisorOperation(...args),
  restartSupervisorService: (...args: unknown[]) => restartSupervisorService(...args),
  restartAllSupervisorApps: (...args: unknown[]) => restartAllSupervisorApps(...args),
  resetLocalValidationData: (...args: unknown[]) => resetLocalValidationData(...args),
}));

describe("DevPortHealthPanel", () => {
  beforeEach(() => {
    fetchSupervisorHealth.mockReset();
    fetchSupervisorOperation.mockReset();
    restartSupervisorService.mockReset();
    restartAllSupervisorApps.mockReset();
    resetLocalValidationData.mockReset();
    fetchSupervisorOperation.mockResolvedValue({ busy: false, operation: null, progress: null });
  });

  it("renders supervisor services with restart controls and reset confirmation", async () => {
    fetchSupervisorHealth.mockResolvedValue({
      checkedAtUtc: "2026-09-07T00:00:00.000Z",
      busy: false,
      services: [
        {
          key: "platform-api",
          label: "Platform API",
          port: 8091,
          status: "Up",
          restartable: true,
        },
        {
          key: "pos-api",
          label: "POS API",
          port: 8092,
          status: "Down",
          restartable: true,
        },
        {
          key: "platform-db",
          label: "Platform DB",
          port: 15533,
          status: "Up",
          restartable: false,
        },
      ],
    });
    restartSupervisorService.mockResolvedValue({
      ok: true,
      status: 200,
      message: "POS API restarted successfully.",
    });

    render(
      <AppProviders>
        <DevPortHealthPanel />
      </AppProviders>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("dev-port-health-8091")).toBeInTheDocument();
    });

    expect(screen.getByTestId("dev-port-health")).toHaveTextContent("Local Validation");
    expect(screen.getByTestId("dev-lv-restart-pos-api")).toHaveTextContent(/Start/i);
    expect(screen.getByTestId("dev-lv-restart-platform-api")).toHaveTextContent(/Restart/i);
    expect(screen.queryByTestId("dev-lv-restart-platform-db")).not.toBeInTheDocument();
    expect(screen.getByTestId("dev-lv-restart-apps")).toBeInTheDocument();
    expect(screen.getByTestId("dev-lv-reset-open")).toBeInTheDocument();

    fireEvent.click(screen.getByTestId("dev-lv-reset-open"));
    expect(screen.getByTestId("dev-lv-reset-modal")).toBeInTheDocument();
    fireEvent.click(screen.getByTestId("dev-lv-reset-cancel"));
    expect(screen.queryByTestId("dev-lv-reset-modal")).not.toBeInTheDocument();
    expect(resetLocalValidationData).not.toHaveBeenCalled();

    fireEvent.click(screen.getByTestId("dev-lv-restart-pos-api"));
    await waitFor(() => {
      expect(restartSupervisorService).toHaveBeenCalledWith("pos-api");
    });
  });

  it("falls back to vite port health when supervisor is offline", async () => {
    fetchSupervisorHealth.mockResolvedValue(null);
    vi.spyOn(globalThis, "fetch").mockImplementation((input) => {
      const url = String(input);
      if (url.includes("/__dev__/port-health")) {
        return Promise.resolve(
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
        );
      }
      return Promise.reject(new Error(`unexpected fetch ${url}`));
    });

    render(
      <AppProviders>
        <DevPortHealthPanel />
      </AppProviders>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("dev-port-health-8091")).toBeInTheDocument();
    });
    expect(screen.getByTestId("dev-lv-supervisor-offline")).toBeInTheDocument();
    expect(screen.queryByTestId("dev-lv-restart-apps")).not.toBeInTheDocument();
  });
});
