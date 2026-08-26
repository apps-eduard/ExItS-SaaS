import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import {
  apiValueForSimulationLabel,
  TEST_PAYMENT_SIMULATION_OPTIONS,
} from "@/features/local-validation/test-payments-simulations";
import { mockAuthenticatedFetch, sampleAuthorization } from "@/test/auth-fixtures";

const orgId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const subId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

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

function enableLocalValidationTools() {
  window.__EXITS_PLATFORM_ADMIN_WEB__ = { localValidationToolsEnabled: true };
}

describe("test-payments-simulations", () => {
  it("maps every UI simulation label to a provider-accepted token", () => {
    for (const option of TEST_PAYMENT_SIMULATION_OPTIONS) {
      expect(apiValueForSimulationLabel(option.label)).toBe(option.apiValue);
    }
    expect(TEST_PAYMENT_SIMULATION_OPTIONS.map((item) => item.label)).toEqual([
      "Succeeded",
      "Declined",
      "Pending",
      "Failed",
      "RenewalSucceeded",
      "RenewalFailed",
      "Refunded",
    ]);
  });
});

describe("TestPaymentsPage", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
    delete window.__EXITS_PLATFORM_ADMIN_WEB__;
  });

  it("hides the page outside development tools (production-like)", async () => {
    stubDesktop();
    vi.stubEnv("MODE", "production");
    mockAuthenticatedFetch({
      permissions: [...sampleAuthorization.permissions, "platform.permission.manage_subscriptions"],
    });
    enableLocalValidationTools();
    window.history.replaceState({}, "", "/admin/local-validation/test-payments");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Page not found" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Test Payments" })).not.toBeInTheDocument();
  });

  it("shows forbidden without ManageSubscriptions", async () => {
    stubDesktop();
    vi.stubEnv("MODE", "development");
    mockAuthenticatedFetch({
      permissions: ["platform.permission.view_portfolio"],
    });
    enableLocalValidationTools();
    window.history.replaceState({}, "", "/admin/local-validation/test-payments");
    render(<App />);
    expect(await screen.findByTestId("forbidden-state")).toBeInTheDocument();
  });

  it("shows unavailable when Local Validation tools are disabled", async () => {
    stubDesktop();
    vi.stubEnv("MODE", "development");
    mockAuthenticatedFetch();
    window.__EXITS_PLATFORM_ADMIN_WEB__ = { localValidationToolsEnabled: false };
    window.history.replaceState({}, "", "/admin/local-validation/test-payments");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Test Payments" })).toBeInTheDocument();
    expect(screen.getByText(/only available when Local Validation tools are enabled/i)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Run test payment" })).not.toBeInTheDocument();
  });

  it("validates before submit", async () => {
    stubDesktop();
    vi.stubEnv("MODE", "development");
    const user = userEvent.setup();
    mockAuthenticatedFetch();
    enableLocalValidationTools();
    window.history.replaceState({}, "", "/admin/local-validation/test-payments");
    render(<App />);
    expect(await screen.findByRole("button", { name: "Run test payment" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Run test payment" }));
    expect(
      await screen.findByText(/Organization and subscription must be selected/i),
    ).toBeInTheDocument();
  });

  it("runs each simulation type through the Local Validation API", async () => {
    stubDesktop();
    vi.stubEnv("MODE", "development");
    const user = userEvent.setup();
    const bodies: Array<Record<string, unknown>> = [];
    mockAuthenticatedFetch({
      organizationItems: [
        {
          id: orgId,
          displayName: "Northwind Market",
          slug: "northwind-market",
          status: "Active",
        },
      ],
      orgSubscriptionItems: [
        {
          id: subId,
          organizationId: orgId,
          productCode: "pinoy-business-pos",
          planId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
          status: "Active",
          productDisplayName: "Pinoy Business POS",
        },
      ],
      onPaymentMutation: (_method, path, body) => {
        if (path.endsWith("/local-validation/payments/simulate")) {
          bodies.push(body as Record<string, unknown>);
        }
      },
    });
    enableLocalValidationTools();
    window.history.replaceState({}, "", "/admin/local-validation/test-payments");
    render(<App />);

    expect(await screen.findByRole("button", { name: "Run test payment" })).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByRole("option", { name: /Northwind Market/i })).toBeInTheDocument();
    });
    await user.selectOptions(screen.getByLabelText("Organization"), orgId);
    await waitFor(() => {
      expect(screen.getByRole("option", { name: /Pinoy Business POS/i })).toBeInTheDocument();
    });
    await user.selectOptions(screen.getByLabelText("Subscription"), subId);

    for (const option of TEST_PAYMENT_SIMULATION_OPTIONS) {
      await user.click(screen.getByRole("button", { name: option.label }));
      await user.click(screen.getByRole("button", { name: "Run test payment" }));
      await waitFor(() => {
        expect(bodies.at(-1)?.simulation).toBe(option.apiValue);
      });
      expect(bodies.at(-1)?.purpose).toBe("admin-test");
      expect(bodies.at(-1)?.billingCycle).toBe("Monthly");
      expect(typeof bodies.at(-1)?.idempotencyKey).toBe("string");
      expect(await screen.findByText("Simulation result")).toBeInTheDocument();
    }

    expect(bodies).toHaveLength(TEST_PAYMENT_SIMULATION_OPTIONS.length);
  });

  it("shows API failure with retry and copy diagnostics", async () => {
    stubDesktop();
    vi.stubEnv("MODE", "development");
    const user = userEvent.setup();
    mockAuthenticatedFetch({
      organizationItems: [
        {
          id: orgId,
          displayName: "Northwind Market",
          slug: "northwind-market",
          status: "Active",
        },
      ],
      orgSubscriptionItems: [
        {
          id: subId,
          organizationId: orgId,
          productCode: "pinoy-business-pos",
          planId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
          status: "Active",
          productDisplayName: "Pinoy Business POS",
        },
      ],
      failLocalValidationSimulate: true,
    });
    enableLocalValidationTools();
    window.history.replaceState({}, "", "/admin/local-validation/test-payments");
    render(<App />);

    await waitFor(() => {
      expect(screen.getByRole("option", { name: /Northwind Market/i })).toBeInTheDocument();
    });
    await user.selectOptions(screen.getByLabelText("Organization"), orgId);
    await waitFor(() => {
      expect(screen.getByRole("option", { name: /Pinoy Business POS/i })).toBeInTheDocument();
    });
    await user.selectOptions(screen.getByLabelText("Subscription"), subId);
    await user.click(screen.getByRole("button", { name: "Run test payment" }));

    expect(await screen.findByRole("heading", { name: "Simulation failed" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /retry/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /copy error details/i })).toBeInTheDocument();
  });

  it("shows successful result links", async () => {
    stubDesktop();
    vi.stubEnv("MODE", "development");
    const user = userEvent.setup();
    mockAuthenticatedFetch({
      organizationItems: [
        {
          id: orgId,
          displayName: "Northwind Market",
          slug: "northwind-market",
          status: "Active",
        },
      ],
      orgSubscriptionItems: [
        {
          id: subId,
          organizationId: orgId,
          productCode: "pinoy-business-pos",
          planId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
          status: "Active",
          productDisplayName: "Pinoy Business POS",
        },
      ],
    });
    enableLocalValidationTools();
    window.history.replaceState({}, "", "/admin/local-validation/test-payments");
    render(<App />);

    await waitFor(() => {
      expect(screen.getByRole("option", { name: /Northwind Market/i })).toBeInTheDocument();
    });
    await user.selectOptions(screen.getByLabelText("Organization"), orgId);
    await waitFor(() => {
      expect(screen.getByRole("option", { name: /Pinoy Business POS/i })).toBeInTheDocument();
    });
    await user.selectOptions(screen.getByLabelText("Subscription"), subId);
    await user.click(screen.getByRole("button", { name: "Run test payment" }));

    const result = await screen.findByRole("status");
    expect(within(result).getByText("Succeeded")).toBeInTheDocument();
    expect(within(result).getByText("LV-TEST")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Open subscription" })).toHaveAttribute(
      "href",
      `/admin/subscriptions/${subId}`,
    );
    expect(screen.getByRole("link", { name: "Open payments" })).toHaveAttribute(
      "href",
      "/admin/payments/list",
    );
  });
});
