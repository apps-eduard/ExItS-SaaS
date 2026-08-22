import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { planDetailHref } from "@/api/catalog/plan-list-query";
import { mockAuthenticatedFetch, sampleAuthorization } from "@/test/auth-fixtures";

const productId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const growthPlanId = "dddddddd-dddd-dddd-dddd-dddddddddddd";

const pinoyProduct = {
  id: productId,
  code: "pinoy-business-pos",
  displayName: "Pinoy Business POS",
  status: "Active",
  createdAtUtc: "2026-01-01T08:00:00Z",
  updatedAtUtc: "2026-08-01T08:00:00Z",
};

const growthPlan = {
  id: growthPlanId,
  productCode: "pinoy-business-pos",
  code: "growth",
  displayName: "Growth",
  status: "Active",
  maxActivePosDevices: 3,
  monthlyPrice: 699,
  currencyCode: "PHP",
  updatedAtUtc: "2026-08-01T08:00:00Z",
};

function manageCatalogPermissions() {
  return [...sampleAuthorization.permissions, PLATFORM_PERMISSIONS.manageCatalog];
}

function renderProductDetail(options?: Parameters<typeof mockAuthenticatedFetch>[0]) {
  mockAuthenticatedFetch({
    permissions: manageCatalogPermissions(),
    catalogProductItems: [pinoyProduct],
    catalogPlanItems: [growthPlan],
    catalogProductPlans: [growthPlan],
    ...options,
  });
  window.history.pushState({}, "", `/admin/products/${productId}`);
  render(<App />);
}

describe("ProductDetailPage lifecycle operator", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("gates lifecycle controls without manage_catalog permission", async () => {
    mockAuthenticatedFetch({ catalogProductItems: [pinoyProduct] });
    window.history.pushState({}, "", `/admin/products/${productId}`);
    render(<App />);

    expect(await screen.findByRole("heading", { name: "Pinoy Business POS" })).toBeInTheDocument();
    expect(screen.getByText(/read-only product view/i)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /save display name/i })).not.toBeInTheDocument();
  });

  it("renames product display name and keeps code immutable", async () => {
    const mutations: Array<{ method: string; path: string; body: unknown }> = [];
    renderProductDetail({
      onProductMutation: (method, path, body) => {
        mutations.push({ method, path, body });
      },
    });

    expect(await screen.findByRole("heading", { name: "Pinoy Business POS" })).toBeInTheDocument();
    const nameInput = screen.getByLabelText(/display name/i);
    await userEvent.clear(nameInput);
    await userEvent.type(nameInput, "Pinoy Business POS (Dev)");
    await userEvent.click(screen.getByRole("button", { name: /save display name/i }));

    await waitFor(() => {
      expect(mutations.some((item) => item.method === "PATCH" && item.path.includes("/rename"))).toBe(
        true,
      );
    });
    expect(screen.getByText("pinoy-business-pos")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Pinoy Business POS (Dev)" })).toBeInTheDocument();
    });
  });

  it("deactivates an active product", async () => {
    renderProductDetail();
    expect(await screen.findByRole("heading", { name: "Pinoy Business POS" })).toBeInTheDocument();
    await userEvent.click(screen.getByRole("button", { name: /^deactivate$/i }));
    await userEvent.click(screen.getByRole("button", { name: /^confirm$/i }));
    await waitFor(() => {
      expect(screen.getByText(/product deactivated/i)).toBeInTheDocument();
    });
  });

  it("retired product shows terminal lifecycle state with no outbound actions", async () => {
    renderProductDetail({
      catalogProductItems: [{ ...pinoyProduct, status: "Retired" }],
    });
    expect(await screen.findByRole("heading", { name: "Pinoy Business POS" })).toBeInTheDocument();
    expect(screen.getByText(/outbound lifecycle transitions are blocked/i)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /^activate$/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /^deactivate$/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /retire product/i })).not.toBeInTheDocument();
  });

  it("surfaces rename conflict and restores server display name", async () => {
    renderProductDetail({
      productMutationError: {
        status: 409,
        errorCode: "application.product.concurrency_conflict",
        detail: "Product was updated by another operator.",
      },
    });
    expect(await screen.findByRole("heading", { name: "Pinoy Business POS" })).toBeInTheDocument();
    const nameInput = screen.getByLabelText(/display name/i);
    await userEvent.clear(nameInput);
    await userEvent.type(nameInput, "Stale rename");
    await userEvent.click(screen.getByRole("button", { name: /save display name/i }));
    expect(await screen.findByText(/conflict/i)).toBeInTheDocument();
    await waitFor(() => {
      expect(nameInput).toHaveValue("Pinoy Business POS");
    });
  });

  it("does not expose create product controls", async () => {
    renderProductDetail();
    expect(await screen.findByRole("heading", { name: "Pinoy Business POS" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /create product/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /new product/i })).not.toBeInTheDocument();
  });

  it("links Growth plan from product detail without breaking PA-COM-03 route", async () => {
    renderProductDetail();
    const growthLink = await screen.findByRole("link", { name: "Growth" });
    expect(growthLink).toHaveAttribute("href", planDetailHref(growthPlanId));
  });

  it("PA-COM-03 plan commercial editor still renders on plan detail", async () => {
    mockAuthenticatedFetch({
      permissions: manageCatalogPermissions(),
      catalogPlanItems: [growthPlan],
    });
    window.history.pushState({}, "", planDetailHref(growthPlanId));
    render(<App />);
    expect(await screen.findByRole("button", { name: /save commercial package/i })).toBeInTheDocument();
  });
});
