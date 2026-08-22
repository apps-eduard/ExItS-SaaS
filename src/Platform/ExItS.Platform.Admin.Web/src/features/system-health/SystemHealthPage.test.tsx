import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import type { SystemHealthSnapshot } from "@/api/ops/system-health-client";
import {
  jsonResponse,
  mockAuthenticatedFetch,
  sampleAuthorization,
  sampleSession,
  textResponse,
} from "@/test/auth-fixtures";

const checkedAt = "2026-08-22T11:00:00Z";

function healthySnapshot(patch?: Partial<SystemHealthSnapshot>): SystemHealthSnapshot {
  return {
    overallStatus: "Healthy",
    host: {
      cpuPercent: 12.4,
      memoryUsedBytes: 6_657_199_309,
      memoryTotalBytes: 17_179_869_184,
      storageUsedBytes: 141_733_920_768,
      storageFreeBytes: 126_701_535_232,
      storageTotalBytes: 268_435_456_000,
      uptimeSeconds: 18 * 86400 + 7 * 3600,
    },
    services: [
      { name: "platform-api", status: "Healthy", latencyMs: 21, checkedAtUtc: checkedAt },
      { name: "pos-api", status: "Healthy", latencyMs: 18, checkedAtUtc: checkedAt },
      { name: "platform-database", status: "Healthy", latencyMs: 9, checkedAtUtc: checkedAt },
      { name: "pos-database", status: "Healthy", latencyMs: 11, checkedAtUtc: checkedAt },
    ],
    build: {
      environment: "Testing",
      applicationVersion: "1.0.0",
      commitSha: "abcdef123456",
    },
    backup: {
      status: "NotAvailable",
      lastSuccessfulAtUtc: null,
      ageSeconds: null,
    },
    ...patch,
  };
}

function stubDesktop() {
  vi.spyOn(window, "matchMedia").mockImplementation((query: string) => {
    return {
      matches: query.includes("min-width: 1024px") || query.includes("min-width: 768px"),
      media: query,
      onchange: null,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      addListener: () => undefined,
      removeListener: () => undefined,
      dispatchEvent: () => true,
    } as MediaQueryList;
  });
}

describe("system health page", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("renders a healthy snapshot with resources, services, version, and backup unavailable", async () => {
    stubDesktop();
    mockAuthenticatedFetch({ systemHealth: healthySnapshot() });
    window.history.replaceState({}, "", "/admin/system-health");
    render(<App />);

    expect(await screen.findByRole("heading", { name: "System Health" })).toBeInTheDocument();
    expect(await screen.findByRole("heading", { name: "Overall" })).toBeInTheDocument();
    const overall = screen.getByRole("heading", { name: "Overall" }).closest("article");
    expect(within(overall as HTMLElement).getByText("Healthy")).toBeInTheDocument();

    expect(screen.getByText("6.2 GB / 16 GB")).toBeInTheDocument();
    expect(screen.getByText("132 GB / 250 GB")).toBeInTheDocument();
    expect(screen.getByText("18d 7h")).toBeInTheDocument();
    expect(screen.getByText("12.4%")).toBeInTheDocument();

    const table = screen.getByRole("table", { name: "Service health" });
    expect(within(table).getByText("Platform API")).toBeInTheDocument();
    expect(within(table).getByText("POS API")).toBeInTheDocument();
    expect(within(table).getByText("Platform Database")).toBeInTheDocument();
    expect(within(table).getByText("POS Database")).toBeInTheDocument();
    expect(within(table).getAllByText("21 ms").length).toBeGreaterThan(0);

    expect(screen.getByText("Testing")).toBeInTheDocument();
    expect(screen.getByText("1.0.0")).toBeInTheDocument();
    expect(screen.getByText("abcdef123456")).toBeInTheDocument();
    expect(screen.getAllByText("Not available").length).toBeGreaterThan(0);
    expect(screen.getByText(/Backup health is not implemented/i)).toBeInTheDocument();
  });

  it("renders degraded overall and service status", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      systemHealth: healthySnapshot({
        overallStatus: "Degraded",
        services: [
          { name: "platform-api", status: "Healthy", latencyMs: 8, checkedAtUtc: checkedAt },
          { name: "pos-api", status: "Degraded", latencyMs: 80, checkedAtUtc: checkedAt },
          { name: "platform-database", status: "Healthy", latencyMs: 7, checkedAtUtc: checkedAt },
          { name: "pos-database", status: "Healthy", latencyMs: 9, checkedAtUtc: checkedAt },
        ],
      }),
    });
    window.history.replaceState({}, "", "/admin/system-health");
    render(<App />);
    expect(await screen.findAllByText("Degraded")).not.toHaveLength(0);
    expect(screen.getByText("80 ms")).toBeInTheDocument();
  });

  it("renders unhealthy overall status", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      systemHealth: healthySnapshot({
        overallStatus: "Unhealthy",
        services: [
          { name: "platform-api", status: "Healthy", latencyMs: 8, checkedAtUtc: checkedAt },
          { name: "pos-api", status: "Unhealthy", latencyMs: 40, checkedAtUtc: checkedAt },
          { name: "platform-database", status: "Healthy", latencyMs: 7, checkedAtUtc: checkedAt },
          { name: "pos-database", status: "Unknown", latencyMs: null, checkedAtUtc: checkedAt },
        ],
      }),
    });
    window.history.replaceState({}, "", "/admin/system-health");
    render(<App />);
    expect(await screen.findAllByText("Unhealthy")).not.toHaveLength(0);
    expect(screen.getAllByText("Unknown").length).toBeGreaterThan(0);
  });

  it("renders unavailable services without marking them healthy", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      systemHealth: healthySnapshot({
        overallStatus: "Degraded",
        services: [
          { name: "platform-api", status: "Healthy", latencyMs: 4, checkedAtUtc: checkedAt },
          { name: "pos-api", status: "Unavailable", latencyMs: null, checkedAtUtc: checkedAt },
          { name: "platform-database", status: "Healthy", latencyMs: 5, checkedAtUtc: checkedAt },
          { name: "pos-database", status: "Unavailable", latencyMs: null, checkedAtUtc: checkedAt },
        ],
      }),
    });
    window.history.replaceState({}, "", "/admin/system-health");
    render(<App />);
    const table = await screen.findByRole("table", { name: "Service health" });
    expect(within(table).getAllByText("Unavailable")).toHaveLength(2);
    const overall = screen.getByRole("heading", { name: "Overall" }).closest("article");
    expect(within(overall as HTMLElement).queryByText("Healthy")).not.toBeInTheDocument();
    expect(within(overall as HTMLElement).getByText("Degraded")).toBeInTheDocument();
  });

  it("refreshes on demand", async () => {
    stubDesktop();
    const fetchMock = mockAuthenticatedFetch({ systemHealth: healthySnapshot() });
    window.history.replaceState({}, "", "/admin/system-health");
    render(<App />);
    expect(await screen.findByRole("table", { name: "Service health" })).toBeInTheDocument();
    const healthCalls = () =>
      fetchMock.mock.calls.filter((call) => String(call[0]).includes("/operations/system-health"))
        .length;
    const initial = healthCalls();
    await userEvent.click(screen.getByRole("button", { name: "Refresh" }));
    await waitFor(() => {
      expect(healthCalls()).toBeGreaterThan(initial);
    });
  });

  it("shows loading then error states", async () => {
    stubDesktop();
    let releaseHealth: (() => void) | undefined;
    const gate = new Promise<void>((resolve) => {
      releaseHealth = resolve;
    });
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/auth/me")) {
          return jsonResponse(200, sampleSession);
        }
        if (url.includes("/authorization/me")) {
          return jsonResponse(200, sampleAuthorization);
        }
        if (url.includes("/operations/system-health")) {
          await gate;
          return jsonResponse(500, {
            title: "Error",
            status: 500,
            detail: "Password=should-not-render",
          });
        }
        if (url.includes("/health")) {
          return textResponse(200, "Healthy");
        }
        return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
      }),
    );
    window.history.replaceState({}, "", "/admin/system-health");
    render(<App />);
    expect(await screen.findByLabelText("Loading system health")).toBeInTheDocument();
    releaseHealth?.();
    expect(await screen.findByRole("alert")).toBeInTheDocument();
    expect(screen.queryByText("Password=should-not-render")).not.toBeInTheDocument();
  });

  it("fail-closes without view_portfolio", async () => {
    stubDesktop();
    mockAuthenticatedFetch({ permissions: [], systemHealth: healthySnapshot() });
    window.history.replaceState({}, "", "/admin/system-health");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Page not found" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "System Health" })).not.toBeInTheDocument();
  });
});
