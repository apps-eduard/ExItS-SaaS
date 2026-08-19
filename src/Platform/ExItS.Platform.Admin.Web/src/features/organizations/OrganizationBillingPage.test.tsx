import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import { mockAuthenticatedFetch } from "@/test/auth-fixtures";

const sampleOrg = {
  id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  displayName: "Northwind Market",
  slug: "northwind-market",
  status: "Active",
};

const payment = {
  id: "11111111-1111-1111-1111-111111111111",
  organizationId: sampleOrg.id,
  productCode: "POS",
  amount: 1500,
  currencyCode: "PHP",
  method: "GCash",
  status: "Confirmed",
  paidAtUtc: "2026-08-01T08:00:00Z",
  externalReference: "GCASH-1001",
};

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

describe("organization workspace billing", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("maps SaaS payments without mutation controls", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgPaymentItems: [payment],
    });
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}`);
    const user = userEvent.setup();
    render(<App />);
    const workspaceNav = await screen.findByRole("navigation", { name: "Organization workspace" });
    await user.click(within(workspaceNav).getByRole("link", { name: "Billing" }));
    await waitFor(() => {
      expect(window.location.pathname).toBe(`/admin/organizations/${sampleOrg.id}/billing`);
    });
    expect(await screen.findByRole("heading", { name: "Billing", level: 1 })).toBeInTheDocument();
    expect(screen.getByText("1500 PHP")).toBeInTheDocument();
    expect(screen.getByText("GCash")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /record/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /confirm/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /reject/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /void/i })).not.toBeInTheDocument();
  });

  it("fail-closes forbidden payments without leaking amount", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      forbiddenOrgPayments: true,
    });
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/billing`);
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Page not found" })).toBeInTheDocument();
    expect(screen.queryByText("9999.99")).not.toBeInTheDocument();
    expect(screen.queryByText("payment-secret")).not.toBeInTheDocument();
  });

  it("shows a truthful empty state", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgPaymentItems: [],
    });
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/billing`);
    render(<App />);
    expect(await screen.findByText("No SaaS payments")).toBeInTheDocument();
  });
});
