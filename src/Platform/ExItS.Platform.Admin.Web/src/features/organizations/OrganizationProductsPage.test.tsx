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

function stubDesktop(table = true) {
  vi.spyOn(window, "matchMedia").mockImplementation((query: string) => {
    return {
      matches: query.includes("min-width: 1024px") || (table && query.includes("min-width: 768px")),
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

describe("organization workspace products", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("maps multiple product-access records without fake totals", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      commercialSummary: {
        latestEntitlements: [
          {
            id: "11111111-1111-1111-1111-111111111111",
            productCode: "POS",
            productDisplayName: "Pinoy Business POS",
            subscriptionStatus: "Active",
            snapshotVersion: 4,
            generatedAtUtc: "2026-08-01T08:00:00Z",
          },
          {
            id: "22222222-2222-2222-2222-222222222222",
            productCode: "UNKNOWN_CODE",
            subscriptionStatus: "Trialing",
          },
        ],
      },
    });
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}`);
    const user = userEvent.setup();
    render(<App />);
    const workspaceNav = await screen.findByRole("navigation", { name: "Organization workspace" });
    await user.click(within(workspaceNav).getByRole("link", { name: "Products" }));
    await waitFor(() => {
      expect(window.location.pathname).toBe(`/admin/organizations/${sampleOrg.id}/products`);
    });
    expect(await screen.findByRole("heading", { name: "Products", level: 1 })).toBeInTheDocument();
    const productsTable = screen.getByRole("table");
    expect(within(productsTable).getByText("Pinoy Business POS")).toBeInTheDocument();
    expect(screen.getByText("POS")).toBeInTheDocument();
    expect(screen.getByText("UNKNOWN_CODE")).toBeInTheDocument();
    expect(screen.getByText("Trialing")).toBeInTheDocument();
    expect(screen.getByText("4")).toBeInTheDocument();
    expect(screen.queryByText(/portfolio total/i)).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /grant/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /activate/i })).not.toBeInTheDocument();
    const crumb = screen.getByRole("navigation", { name: "Breadcrumb" });
    expect(crumb).toHaveTextContent("Products");
  });

  it("shows a truthful empty state when no entitlements are returned", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      commercialSummary: { latestEntitlements: [] },
    });
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/products`);
    render(<App />);
    expect(await screen.findByText("No product access records")).toBeInTheDocument();
  });

  it("keeps commercial errors in region with retry", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      failCommercialSummary: true,
    });
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/products`);
    render(<App />);
    expect(
      await screen.findByRole("heading", { name: "Unable to load product access." }),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Retry" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Copy error details" })).toBeInTheDocument();
  });

  it("renders canonical subscription status tones and labels", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      commercialSummary: {
        latestEntitlements: [
          {
            id: "11111111-1111-1111-1111-111111111111",
            productCode: "POS",
            subscriptionStatus: "Cancelled",
          },
          {
            id: "22222222-2222-2222-2222-222222222222",
            productCode: "PLM",
            subscriptionStatus: "Expired",
          },
          {
            id: "33333333-3333-3333-3333-333333333333",
            productCode: "CRM",
            subscriptionStatus: "GracePeriod",
          },
          {
            id: "44444444-4444-4444-4444-444444444444",
            productCode: "UNK",
            subscriptionStatus: "UnknownStatus",
          },
        ],
      },
    });
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/products`);
    render(<App />);
    expect(await screen.findByText("Cancelled")).toBeInTheDocument();
    expect(screen.getByText("Expired")).toBeInTheDocument();
    expect(screen.getByText("Grace period")).toBeInTheDocument();
    expect(screen.getByText("UnknownStatus")).toBeInTheDocument();
    const dangerDots = document.querySelectorAll(".bg-destructive");
    const warningDots = document.querySelectorAll(".bg-warning");
    expect(dangerDots.length).toBeGreaterThanOrEqual(2);
    expect(warningDots.length).toBeGreaterThanOrEqual(1);
  });
});
