import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as linkedClient from "@/api/pos/pos-linked-customers-client";
import * as sellerIdentity from "@/features/documents/resolve-customer-seller-identity";
import { LinkedMerchantReceiptPage } from "@/features/personal/linked-merchants/LinkedMerchantReceiptPage";

vi.mock("@/api/pos/pos-linked-customers-client", async (importOriginal) => {
  const actual = await importOriginal<typeof linkedClient>();
  return {
    ...actual,
    getLinkedCustomerSaleReceipt: vi.fn(),
  };
});

vi.mock("@/features/documents/resolve-customer-seller-identity", async (importOriginal) => {
  const actual = await importOriginal<typeof sellerIdentity>();
  return {
    ...actual,
    getOrganizationDocumentPublicIdentity: vi.fn().mockResolvedValue({
      organizationId: "11111111-1111-1111-1111-111111111111",
      displayName: "Paul Coffee",
      publicOrganizationId: null,
      logoUrl: null,
      businessPhone: "0917",
      businessEmail: "a@b.com",
      addressLine1: "Iloilo City",
      addressLine2: null,
      city: null,
      region: null,
      postalCode: null,
      countryCode: "PH",
    }),
  };
});

vi.mock("@/connectivity/browser-online", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/connectivity/browser-online")>();
  return {
    ...actual,
    useBrowserOnline: () => true,
  };
});

vi.mock("@/features/customer-ordering/useLinkedMerchantShopContext", () => ({
  useLinkedMerchantShopContext: () => ({
    data: {
      organizationDisplayName: "Paul Coffee",
      customerDisplayName: "Ada",
      businessCustomerId: "22222222-2222-4222-8222-222222222222",
      statementTo: null,
    },
    isLoading: false,
  }),
}));

const organizationId = "11111111-1111-1111-1111-111111111111";
const businessCustomerId = "22222222-2222-4222-8222-222222222222";
const saleId = "33333333-3333-4333-8333-333333333333";

function receipt() {
  return {
    organizationId,
    platformBusinessCustomerId: businessCustomerId,
    posCustomerId: "44444444-4444-4444-8444-444444444444",
    saleId,
    receiptNumber: "SALE-100",
    occurredAtUtc: "2026-09-14T10:00:00Z",
    status: "Completed",
    paymentMethod: "Cash",
    currency: "PHP",
    merchantDisplayName: "Paul Coffee",
    customerDisplayName: "Ada",
    branchDisplayName: "Main",
    subtotal: 100,
    discountAmount: 0,
    taxAmount: 0,
    total: 100,
    paidAmount: 100,
    lines: [
      {
        lineNumber: 1,
        productNameSnapshot: "Espresso",
        quantity: 2,
        unitOfMeasure: "pc",
        sellingMode: "Unit",
        unitPriceSnapshot: 50,
        lineTotal: 100,
      },
    ],
  };
}

describe("LinkedMerchantReceiptPage document parity", () => {
  beforeEach(() => {
    vi.mocked(linkedClient.getLinkedCustomerSaleReceipt).mockReset();
    vi.mocked(linkedClient.getLinkedCustomerSaleReceipt).mockResolvedValue(receipt() as never);
    vi.spyOn(window, "print").mockImplementation(() => undefined);
  });

  function renderPage() {
    return render(
      <AppProviders>
        <MemoryRouter
          initialEntries={[
            `/personal/linked-merchants/${organizationId}/${businessCustomerId}/receipts/${saleId}`,
          ]}
        >
          <Routes>
            <Route
              path="/personal/linked-merchants/:organizationId/:businessCustomerId/receipts/:saleId"
              element={<LinkedMerchantReceiptPage />}
            />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );
  }

  it("renders canonical Customer Purchase Summary without standalone disclaimer or legacy card", async () => {
    renderPage();
    await waitFor(() =>
      expect(screen.getByTestId("linked-merchant-receipt-page")).toBeInTheDocument(),
    );

    expect(screen.queryByTestId("linked-merchant-receipt-disclaimer")).not.toBeInTheDocument();
    expect(screen.queryByTestId("linked-merchant-receipt-summary")).not.toBeInTheDocument();

    const doc = screen.getByTestId("customer-purchase-summary-document");
    expect(doc).toBeInTheDocument();
    expect(within(doc).getByTestId("business-document-title")).toHaveTextContent(
      "Customer Purchase Summary",
    );
    expect(within(doc).getByTestId("business-document-disclaimer")).toHaveTextContent(
      "NOT A BIR INVOICE",
    );
    expect(within(doc).getByTestId("business-document-extended-disclaimer")).toBeInTheDocument();
    expect(within(doc).getByTestId("business-document-business-name")).toHaveTextContent(
      "Paul Coffee",
    );
    expect(within(doc).getByTestId("business-document-business-name")).not.toHaveTextContent(
      /^Store$/,
    );
    expect(within(doc).getByText(/Iloilo City/)).toBeInTheDocument();
    expect(within(doc).getByTestId("business-document-phone")).toHaveTextContent("0917");
    expect(within(doc).getByTestId("business-document-email")).toHaveTextContent("a@b.com");
    expect(within(doc).queryByTestId("business-document-meta-cashier")).not.toBeInTheDocument();
    expect(screen.getByTestId("linked-merchant-receipt-print")).toBeInTheDocument();
    expect(screen.getByTestId("linked-merchant-receipt-pdf")).toBeInTheDocument();
  });

  it("hides cashier and shows Utang balance for customer audience", async () => {
    vi.mocked(linkedClient.getLinkedCustomerSaleReceipt).mockResolvedValue({
      ...receipt(),
      paymentMethod: "Utang",
      paidAmount: null,
      utangAmount: 100,
    } as never);
    renderPage();
    await waitFor(() =>
      expect(screen.getByTestId("customer-purchase-summary-document")).toBeInTheDocument(),
    );
    expect(screen.queryByTestId("business-document-meta-cashier")).not.toBeInTheDocument();
    expect(screen.getByTestId("business-document-total-utang")).toBeInTheDocument();
  });
});
