import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import {
  jsonResponse,
  mockAuthenticatedFetch,
  sampleAuthorization,
  sampleSession,
  textResponse,
} from "@/test/auth-fixtures";

function stubDesktop() {
  vi.spyOn(window, "matchMedia").mockImplementation((query: string) => {
    return {
      matches: query.includes("min-width: 1024px"),
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

describe("Overview dashboard", () => {
  beforeEach(() => {
    window.history.replaceState({}, "", "/admin");
    stubDesktop();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("does not flash privileged widgets while authorization is loading", async () => {
    let resolveAuthz: ((value: Response) => void) | undefined;
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/auth/me")) {
          return jsonResponse(200, sampleSession);
        }
        if (url.includes("/authorization/me")) {
          return await new Promise<Response>((resolve) => {
            resolveAuthz = resolve;
          });
        }
        return jsonResponse(404, {});
      }),
    );

    render(<App />);
    expect(await screen.findByRole("heading", { name: "Overview" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Organizations" })).not.toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Subscriptions" })).not.toBeInTheDocument();
    expect(
      screen.queryByRole("heading", { name: "Accounts needing review" }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("heading", { name: "Recent Platform activity" }),
    ).not.toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Platform readiness" })).not.toBeInTheDocument();
    expect(screen.getByLabelText("Loading dashboard")).toBeInTheDocument();

    resolveAuthz?.(jsonResponse(200, sampleAuthorization));
    expect(await screen.findByRole("heading", { name: "Organizations" })).toBeInTheDocument();
  });

  it("hides unauthorized widgets and keeps authorized ones", async () => {
    mockAuthenticatedFetch({
      permissions: ["platform.permission.view_audit_records"],
    });
    render(<App />);
    expect(
      await screen.findByRole("heading", { name: "Recent Platform activity" }),
    ).toBeInTheDocument();
    expect(await screen.findByRole("heading", { name: "Platform readiness" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Organizations" })).not.toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Subscriptions" })).not.toBeInTheDocument();
    expect(
      screen.queryByRole("heading", { name: "Accounts needing review" }),
    ).not.toBeInTheDocument();
  });

  it("renders real zero counts instead of hiding widgets or treating them as errors", async () => {
    mockAuthenticatedFetch({ organizationTotalCount: 0 });
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Organizations" })).toBeInTheDocument();
    expect(await screen.findByText("No suspended organizations.")).toBeInTheDocument();
    expect(screen.getByText("No unassigned accounts.")).toBeInTheDocument();
    expect(screen.getByText("No audit records yet.")).toBeInTheDocument();
    expect(screen.queryByText("Unable to load this summary.")).not.toBeInTheDocument();
    expect(screen.getAllByText("0").length).toBeGreaterThan(0);
  });

  it("keeps other widgets when organizations fail and retries that widget only", async () => {
    mockAuthenticatedFetch({ failOrganizations: true });
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Subscriptions" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Platform readiness" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Copy diagnostics" })).not.toBeInTheDocument();
    const orgSection = screen.getByRole("heading", { name: "Organizations" }).closest("section");
    expect(orgSection).toBeTruthy();
    expect(
      await within(orgSection as HTMLElement).findByText("Unable to load this summary."),
    ).toBeInTheDocument();
    await user.click(within(orgSection as HTMLElement).getByRole("button", { name: "Retry" }));
    await waitFor(() => {
      expect(
        vi.mocked(fetch).mock.calls.some(([input]) => String(input).includes("/organizations")),
      ).toBe(true);
    });
  });

  it("requests bounded page sizes rather than unbounded lists", async () => {
    mockAuthenticatedFetch();
    render(<App />);
    await screen.findByRole("heading", { name: "Organizations" });
    await waitFor(() => {
      const urls = vi.mocked(fetch).mock.calls.map(([input]) => String(input));
      const dataUrls = urls.filter((url) => url.includes("/api/v1/platform/"));
      expect(dataUrls.some((url) => url.includes("pageSize=100"))).toBe(false);
      expect(dataUrls.some((url) => url.includes("pageSize=1000"))).toBe(false);
      expect(
        dataUrls.some((url) => url.includes("/organizations") && url.includes("pageSize=1")),
      ).toBe(true);
      expect(dataUrls.some((url) => url.includes("/audit") && url.includes("pageSize=8"))).toBe(
        true,
      );
    });
  });

  it("localizes dashboard widgets to Filipino and keeps theme/density compatibility", async () => {
    mockAuthenticatedFetch();
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Organizations" });
    await user.click(screen.getByRole("button", { name: "Preferences" }));
    await user.click(await screen.findByRole("menuitem", { name: /Filipino/i }));
    expect(await screen.findByRole("heading", { name: "Mga Organisasyon" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Mga Subskripsyon" })).toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: "Mga account na kailangang suriin" }),
    ).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Mga kagustuhan" }));
    await user.click(await screen.findByRole("menuitem", { name: /Madilim/i }));
    expect(document.documentElement.dataset.theme).toBe("dark");
    await user.click(screen.getByRole("button", { name: "Mga kagustuhan" }));
    await user.click(await screen.findByRole("menuitem", { name: /Siksik/i }));
    expect(document.documentElement.dataset.density).toBe("compact");
    expect(screen.getByRole("heading", { name: "Mga Organisasyon" })).toBeInTheDocument();
  });
});

describe("platform health client", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("records Unhealthy from HTTP 503 instead of inventing a healthy state", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.endsWith("/health/ready")) {
          return { ...textResponse(503, "Unhealthy"), ok: false } as Response;
        }
        return textResponse(200, "Healthy");
      }),
    );
    const { getPlatformHealth } = await import("@/api/ops/health-client");
    const snapshot = await getPlatformHealth("http://localhost:8091");
    expect(snapshot.liveness.reportedStatus).toBe("Healthy");
    expect(snapshot.readiness.reportedStatus).toBe("Unhealthy");
    expect(snapshot.readiness.httpStatus).toBe(503);
  });
});
