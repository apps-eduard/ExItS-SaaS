import { afterEach, describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Route, Routes } from "react-router-dom";
import { HomePage } from "@/features/home/HomePage";
import { AppShell } from "@/layouts/AppShell";
import { ORG_ALLOWED, ORG_DENIED, stubAccessFetch } from "@/test/access-mocks";
import { renderWithAccessGate } from "@/test/render";

function renderGate() {
  return renderWithAccessGate(
    <Routes>
      <Route element={<AppShell />}>
        <Route path="/" element={<HomePage />} />
      </Route>
    </Routes>,
  );
}

describe("organization product access gate", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("allows workspace entry when product access is granted", async () => {
    stubAccessFetch();
    renderGate();
    expect(await screen.findByRole("heading", { name: "Pinoy Loan Manager" })).toBeInTheDocument();
    expect(screen.getByText(ORG_ALLOWED.displayName)).toBeInTheDocument();
  });

  it("blocks workspace when product access is denied", async () => {
    stubAccessFetch({
      productAccess: { allowed: false, reasonCode: "product_assignment_missing" },
    });
    renderGate();
    expect(
      await screen.findByRole("heading", { name: "No Pinoy Loan Manager access" }),
    ).toBeInTheDocument();
    expect(
      screen.getByText(/does not have a Pinoy Loan Manager product-access assignment/i),
    ).toBeInTheDocument();
  });

  it("shows subscription inactive when subscription is ineligible", async () => {
    stubAccessFetch({
      productAccess: {
        allowed: false,
        reasonCode: "subscription_ineligible",
        subscriptionStatus: "Inactive",
      },
    });
    renderGate();
    expect(
      await screen.findByRole("heading", { name: "Subscription inactive" }),
    ).toBeInTheDocument();
  });

  it("requires organization selection when multiple orgs exist", async () => {
    stubAccessFetch({
      selectedOrganizationId: null,
      organizations: [ORG_ALLOWED, ORG_DENIED],
    });
    renderGate();
    expect(await screen.findByRole("heading", { name: "Choose organization" })).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: new RegExp(ORG_ALLOWED.displayName) }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: new RegExp(ORG_DENIED.displayName) }),
    ).toBeInTheDocument();
  });

  it("blocks platform sessions at account scope", async () => {
    stubAccessFetch({ accountClass: "Platform" });
    renderGate();
    expect(
      await screen.findByRole("heading", { name: "Organization account required" }),
    ).toBeInTheDocument();
  });

  it("shows zero-organization state", async () => {
    stubAccessFetch({ organizations: [] });
    renderGate();
    expect(
      await screen.findByRole("heading", { name: "No organization access" }),
    ).toBeInTheDocument();
  });

  it("retries after a product access verification error", async () => {
    stubAccessFetch({ productAccessStatus: 503 });
    renderGate();
    expect(
      await screen.findByRole("heading", { name: "Could not check access" }),
    ).toBeInTheDocument();
    stubAccessFetch();
    await userEvent.setup().click(screen.getByRole("button", { name: "Retry" }));
    expect(await screen.findByRole("heading", { name: "Pinoy Loan Manager" })).toBeInTheDocument();
  });

  it("does not persist session credentials after org and product-access bootstrap", async () => {
    stubAccessFetch();
    renderGate();
    await screen.findByRole("heading", { name: "Pinoy Loan Manager" });
    const stored = JSON.stringify({
      local: { ...window.localStorage },
      session: { ...window.sessionStorage },
    });
    expect(stored).not.toMatch(/sessionToken|authorization:\s*bearer|password/i);
  });
});
