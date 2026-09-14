import { describe, expect, it } from "vitest";
import { render } from "@testing-library/react";
import { CustomerPurchaseSummaryDocument } from "@/features/documents/SaleBusinessDocument";
import {
  PurchaseOrderBusinessDocument,
  GoodsReceiptBusinessDocument,
} from "@/features/documents/PurchasingBusinessDocuments";
import { DEFAULT_DOCUMENT_SETTINGS } from "@/features/documents/document-settings";
import type { PosSaleDto } from "@/api/pos/pos-sales-client";
import type {
  PosGoodsReceiptDto,
  PosPurchaseOrderDto,
} from "@/api/pos/pos-purchase-orders-client";

const identity = {
  businessName: "Paul Coffee",
  address: "Iloilo City",
  phone: "0917",
  email: "a@b.com",
};
const visibility = {
  showLogo: false,
  showBusinessName: true,
  showBusinessAddress: true,
  showBusinessPhone: true,
  showBusinessEmail: true,
  showWebsite: false,
  showBranchName: true,
  showBranchAddress: false,
};

const sale = {
  saleId: "11111111-1111-4111-8111-111111111111",
  organizationId: "22222222-2222-4222-8222-222222222222",
  saleNumber: "S-1",
  status: "Completed",
  paymentMethod: "Cash",
  subtotal: 100,
  total: 100,
  taxAmount: 0,
  recordedAtUtc: "2026-09-14T10:00:00Z",
  recordedBy: "33333333-3333-4333-8333-333333333333",
  lines: [
    {
      saleLineId: "44444444-4444-4444-8444-444444444444",
      productId: "55555555-5555-4555-8555-555555555555",
      lineNumber: 1,
      name: "Espresso",
      sku: "ESP",
      unitOfMeasure: "pc",
      sellingMode: "Unit",
      unitPrice: 50,
      quantity: 2,
      lineTotal: 100,
      lineDiscountAmount: 0,
    },
  ],
  customerDisplayName: "Ada",
} as PosSaleDto;

describe("document adapters", () => {
  it("sale document uses Customer Purchase Summary and BIR disclaimer", () => {
    const { getByTestId } = render(
      <CustomerPurchaseSummaryDocument
        sale={sale}
        settings={DEFAULT_DOCUMENT_SETTINGS}
        identity={identity}
        headerVisibility={visibility}
        paymentLabel="Cash"
        cashierLabel="Cashier One"
        audience="Seller"
      />,
    );
    expect(getByTestId("business-document-title")).toHaveTextContent("Customer Purchase Summary");
    expect(getByTestId("business-document-disclaimer")).toHaveTextContent("NOT A BIR INVOICE");
    expect(getByTestId("business-document-extended-disclaimer")).toBeInTheDocument();
    expect(getByTestId("business-document-footer-thanks")).toHaveTextContent(
      "Thank you for your business.",
    );
    expect(getByTestId("customer-purchase-summary-document")).toBeInTheDocument();
    expect(getByTestId("business-document-meta-cashier")).toHaveTextContent("Cashier One");
  });

  it("customer audience forces disclaimer and hides cashier", () => {
    const settings = structuredClone(DEFAULT_DOCUMENT_SETTINGS);
    settings.footer.showDocumentDisclaimer = false;
    settings.sales.showCashier = true;
    const { getByTestId, queryByTestId } = render(
      <CustomerPurchaseSummaryDocument
        sale={sale}
        settings={settings}
        identity={identity}
        headerVisibility={visibility}
        paymentLabel="Cash"
        cashierLabel="Hidden Staff"
        audience="Customer"
      />,
    );
    expect(getByTestId("business-document-disclaimer")).toHaveTextContent("NOT A BIR INVOICE");
    expect(queryByTestId("business-document-meta-cashier")).toBeNull();
  });

  it("hides customer block when customer name visibility is off", () => {
    const settings = structuredClone(DEFAULT_DOCUMENT_SETTINGS);
    settings.sales.showCustomerName = false;
    settings.sales.showCustomerAddress = false;
    settings.sales.showCustomerContact = false;
    const { queryByTestId } = render(
      <CustomerPurchaseSummaryDocument
        sale={sale}
        settings={settings}
        identity={identity}
        headerVisibility={visibility}
        paymentLabel="Cash"
      />,
    );
    expect(queryByTestId("business-document-party")).toBeNull();
  });

  it("shows Utang / balance on utang sales", () => {
    const utangSale = {
      ...sale,
      paymentMethod: "Utang",
      amountTendered: null,
      changeAmount: null,
    } as PosSaleDto;
    const { getByTestId, queryByTestId } = render(
      <CustomerPurchaseSummaryDocument
        sale={utangSale}
        settings={DEFAULT_DOCUMENT_SETTINGS}
        identity={identity}
        headerVisibility={visibility}
        paymentLabel="Utang"
      />,
    );
    expect(getByTestId("business-document-total-utang")).toHaveTextContent("100.00");
    expect(queryByTestId("business-document-total-paid")).toBeNull();
    expect(queryByTestId("business-document-total-change")).toBeNull();
  });

  it("PO document does not show sales BIR disclaimer and uses PO columns", () => {
    const po = {
      purchaseOrderId: "66666666-6666-4666-8666-666666666666",
      organizationId: "22222222-2222-4222-8222-222222222222",
      poNumber: "PO-2026-00124",
      supplierId: "77777777-7777-4777-8777-777777777777",
      status: "Ordered",
      orderDate: "2026-09-14",
      createdAtUtc: "2026-09-14T08:00:00Z",
      updatedAtUtc: "2026-09-14T08:00:00Z",
      lines: [
        {
          lineId: "88888888-8888-4888-8888-888888888888",
          lineNumber: 1,
          nameSnapshot: "Beans",
          uomSnapshot: "kg",
          orderedQty: 5,
          unitPurchaseCost: 20,
          lineTotal: 100,
          receivedQty: 0,
          outstandingQty: 5,
        },
      ],
      supplierName: "Supplier Co",
    } as PosPurchaseOrderDto;

    const { getByTestId, queryByTestId } = render(
      <PurchaseOrderBusinessDocument
        po={po}
        supplierName="Supplier Co"
        settings={DEFAULT_DOCUMENT_SETTINGS}
        identity={identity}
        headerVisibility={visibility}
      />,
    );
    expect(getByTestId("business-document-title")).toHaveTextContent("Purchase Order");
    expect(queryByTestId("business-document-disclaimer")).toBeNull();
    expect(getByTestId("business-document-line-table")).toHaveTextContent("Unit Cost");
  });

  it("goods receipt uses GRN columns including damaged", () => {
    const receipt = {
      goodsReceiptId: "99999999-9999-4999-8999-999999999999",
      organizationId: "22222222-2222-4222-8222-222222222222",
      purchaseOrderId: "66666666-6666-4666-8666-666666666666",
      supplierId: "77777777-7777-4777-8777-777777777777",
      grnNumber: "GRN-1",
      receivedDate: "2026-09-14",
      receivedAtUtc: "2026-09-14T12:00:00Z",
      receivedBy: "33333333-3333-4333-8333-333333333333",
      status: "Posted",
      lines: [
        {
          lineId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
          purchaseOrderLineId: "88888888-8888-4888-8888-888888888888",
          productId: "55555555-5555-4555-8555-555555555555",
          lineNumber: 1,
          nameSnapshot: "Beans",
          uomSnapshot: "kg",
          quantityReceived: 2,
          unitPurchaseCostSnapshot: 20,
          lineTotalSnapshot: 40,
          damagedQty: 1,
        },
      ],
    } as PosGoodsReceiptDto;

    const { getByTestId } = render(
      <GoodsReceiptBusinessDocument
        receipt={receipt}
        settings={DEFAULT_DOCUMENT_SETTINGS}
        identity={identity}
        headerVisibility={visibility}
        supplierName="Supplier Co"
        poNumber="PO-2026-00124"
      />,
    );
    expect(getByTestId("business-document-title")).toHaveTextContent("Goods Receipt");
    expect(getByTestId("business-document-line-table")).toHaveTextContent("Damaged");
    expect(getByTestId("business-document-line-table")).toHaveTextContent("Received Now");
  });
});
