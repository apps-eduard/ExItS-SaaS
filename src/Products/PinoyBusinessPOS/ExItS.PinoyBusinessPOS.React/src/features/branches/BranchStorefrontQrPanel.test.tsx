import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { BranchStorefrontQrPanel } from "@/features/branches/BranchStorefrontQrPanel";

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}));

const getOrganizationPublicIdentity = vi.fn();
const getBranchFulfillmentReadiness = vi.fn();

vi.mock("@/api/platform/public-identity-client", () => ({
  getOrganizationPublicIdentity: (...args: unknown[]) => getOrganizationPublicIdentity(...args),
}));

vi.mock("@/api/platform/branch-fulfillment-client", () => ({
  getBranchFulfillmentReadiness: (...args: unknown[]) => getBranchFulfillmentReadiness(...args),
}));

vi.mock("@/features/qr/QrCodeImage", () => ({
  QrCodeImage: ({ payload }: { payload: string }) => (
    <img data-testid="branch-storefront-qr-image" alt={payload} />
  ),
}));

vi.mock("@/features/qr/download-qr-png", () => ({
  downloadQrPng: vi.fn(),
}));

const ORG = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const KALIBO = "56a8a186-1111-4111-8111-111111111111";

function renderPanel() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <Routes>
          <Route
            path="/"
            element={
              <BranchStorefrontQrPanel
                organizationId={ORG}
                organizationDisplayName="Mica Store"
                branchId={KALIBO}
                branchName="Kalibo Branch"
                branchStatus="Active"
              />
            }
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("BranchStorefrontQrPanel", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getOrganizationPublicIdentity.mockResolvedValue({
      publicOrganizationId: "ORG123456",
      displayName: "Mica Store",
      qrPayload: "exits://qr/v1/organization/ORG123456",
    });
  });

  it("BRANCHQR-07/08 shows copy and download when storefront ready", async () => {
    getBranchFulfillmentReadiness.mockResolvedValue({
      customerOrderingReady: true,
      customerOrderingEnabled: true,
      onlineOrdersPaused: false,
    });
    renderPanel();
    await waitFor(() => {
      expect(screen.getByTestId("branch-storefront-qr-image")).toBeInTheDocument();
    });
    expect(screen.getByTestId("branch-storefront-qr-url")).toHaveTextContent(
      `/store/ORG123456/b/${KALIBO}`,
    );
    expect(screen.getByTestId("branch-storefront-qr-copy")).toBeInTheDocument();
    expect(screen.getByTestId("branch-storefront-qr-download")).toBeInTheDocument();
  });

  it("does not generate QR when storefront is not ready", async () => {
    getBranchFulfillmentReadiness.mockResolvedValue({
      customerOrderingReady: false,
      customerOrderingEnabled: false,
      onlineOrdersPaused: false,
    });
    renderPanel();
    await waitFor(() => {
      expect(screen.getByTestId("branch-storefront-qr-not-ready")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("branch-storefront-qr-image")).not.toBeInTheDocument();
  });
});
