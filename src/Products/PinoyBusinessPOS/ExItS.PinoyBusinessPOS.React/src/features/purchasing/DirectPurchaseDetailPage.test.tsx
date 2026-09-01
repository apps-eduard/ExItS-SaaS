import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { PosApiError } from "@/api/pos/pos-http";
import * as directClient from "@/api/pos/pos-direct-purchase-receipts-client";
import { DirectPurchaseDetailPage } from "@/features/purchasing/DirectPurchaseDetailPage";
import { formatPeso } from "@/lib/format-money";

const receiptId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
const workspace = {
  organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
};

const ownerGrant = {
  productAccessAllowed: true,
  mappedPosRoleCode: "Owner",
  productLocalRoleCode: "Owner",
  membershipRole: "OrganizationOwner",
  organizationManagementAuthority: true,
};

let sessionGrant: typeof ownerGrant = ownerGrant;

const remoteBranchId = "cccccccc-cccc-cccc-cccc-cccccccccccc";

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: workspace,
    workspaces: [
      {
        organizationId: workspace.organizationId,
        displayName: "Test Org",
        branches: [
          { branchId: workspace.branchId, name: "Main Branch", secondaryLine: "", isPrimary: true, isActive: true },
          { branchId: remoteBranchId, name: "Remote Branch", secondaryLine: "", isPrimary: false, isActive: true },
        ],
      },
    ],
    get sessionGrant() {
      return sessionGrant;
    },
  }),
}));

vi.mock("@/connectivity/browser-online", () => ({
  useBrowserOnline: () => true,
  subscribeBrowserOnline: (onChange: (online: boolean) => void) => {
    onChange(true);
    return () => undefined;
  },
}));

vi.mock("@/features/actors/useActorDirectory", () => ({
  useActorDirectory: () => ({
    resolve: () => ({
      actorId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
      displayName: "Maria Santos",
      actorStatus: "Active",
    }),
    isResolving: false,
    sortedIds: [],
    isLoading: false,
    isFetching: false,
    data: [],
  }),
}));

function postedReceipt(
  overrides: Partial<directClient.DirectPurchaseReceiptDto> = {},
): directClient.DirectPurchaseReceiptDto {
  return {
    directPurchaseReceiptId: receiptId,
    organizationId: workspace.organizationId,
    receiptNumber: "DP-000045",
    purchaseDate: "2026-08-28",
    sourceNameSnapshot: "ABC Trading",
    referenceNumber: "OR-12345",
    notes: "Morning delivery",
    totalCost: 4500,
    createdByUserId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
    createdAtUtc: "2026-08-28T14:15:00Z",
    status: "Posted",
    voidedAtUtc: null,
    voidedByUserId: null,
    voidReason: null,
    receivingBranchId: remoteBranchId,
    lines: [
      {
        lineId: "11111111-1111-4111-8111-111111111111",
        productId: "22222222-2222-4222-8222-222222222222",
        lineNumber: 1,
        productNameSnapshot: "Bath Soap",
        unitOfMeasure: "Piece",
        quantity: 24,
        unitCost: 18,
        lineTotal: 432,
        expiryDate: "2027-12-30",
        lotNumber: "LOT-A123",
      },
    ],
    ...overrides,
  };
}

function renderPage() {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={[`/purchasing/direct-purchases/${receiptId}`]}>
        <Routes>
          <Route
            path="/purchasing/direct-purchases/:receiptId"
            element={<DirectPurchaseDetailPage />}
          />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("DirectPurchaseDetailPage cost UX", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    sessionGrant = ownerGrant;
    vi.spyOn(directClient, "getDirectPurchaseReceipt").mockResolvedValue(postedReceipt());
  });

  it("shows purchase metadata, money formatting, expiry/lot, and actor", async () => {
    renderPage();

    await waitFor(() => {
      expect(screen.getByTestId("direct-purchase-detail-page")).toBeInTheDocument();
    });
    expect(screen.getByText("DP-000045")).toBeInTheDocument();
    expect(screen.getByTestId("direct-purchase-source")).toHaveTextContent("ABC Trading");
    expect(screen.getByTestId("direct-purchase-receiving-branch")).toHaveTextContent("Remote Branch");
    expect(screen.getByTestId("direct-purchase-reference")).toHaveTextContent("OR-12345");
    expect(screen.getByTestId("direct-purchase-notes")).toHaveTextContent("Morning delivery");
    expect(screen.getByTestId("direct-purchase-total")).toHaveTextContent(formatPeso(4500));
    expect(screen.getByText("Bath Soap")).toBeInTheDocument();
    expect(screen.getByText(formatPeso(18))).toBeInTheDocument();
    expect(screen.getByText(formatPeso(432))).toBeInTheDocument();
    expect(screen.getByText("2027-12-30")).toBeInTheDocument();
    expect(screen.getByText("LOT-A123")).toBeInTheDocument();
    expect(screen.getByText("Maria Santos")).toBeInTheDocument();
    expect(screen.getByTestId("direct-purchase-reverse")).toBeInTheDocument();
  });

  it("reverses receipt with reason and hides action when voided", async () => {
    const user = userEvent.setup();
    const voidSpy = vi.spyOn(directClient, "voidDirectPurchaseReceipt").mockResolvedValue(
      postedReceipt({
        status: "Voided",
        voidedAtUtc: "2026-08-30T12:00:00Z",
        voidedByUserId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
        voidReason: "Entered twice",
      }),
    );

    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId("direct-purchase-reverse")).toBeInTheDocument();
    });

    await user.click(screen.getByTestId("direct-purchase-reverse"));
    await user.type(screen.getByTestId("direct-purchase-reverse-reason"), "Entered twice");
    await user.click(screen.getByTestId("direct-purchase-reverse-confirm"));

    await waitFor(() => {
      expect(voidSpy).toHaveBeenCalledWith(
        workspace,
        receiptId,
        expect.objectContaining({ reason: "Entered twice" }),
      );
    });
    await waitFor(() => {
      expect(screen.getByText("Reversed")).toBeInTheDocument();
      expect(screen.queryByTestId("direct-purchase-reverse")).not.toBeInTheDocument();
      expect(screen.getByTestId("direct-purchase-void-reason")).toHaveTextContent("Entered twice");
    });
  });

  it("hides reverse for reporting users and shows API insufficient-stock error", async () => {
    sessionGrant = {
      productAccessAllowed: true,
      mappedPosRoleCode: "ReportingUser",
      productLocalRoleCode: "ReportingUser",
      membershipRole: "Member",
      organizationManagementAuthority: false,
    };
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId("direct-purchase-detail-page")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("direct-purchase-reverse")).not.toBeInTheDocument();
  });

  it("surfaces insufficient stock failure", async () => {
    const user = userEvent.setup();
    vi.spyOn(directClient, "voidDirectPurchaseReceipt").mockRejectedValue(
      new PosApiError(409, {
        title: "Conflict",
        status: 409,
        detail: "Cannot reverse: stock from this receipt is no longer available.",
        errorCode: "pos.direct_purchase_receipt.void.insufficient_stock",
      }),
    );

    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId("direct-purchase-reverse")).toBeInTheDocument();
    });
    await user.click(screen.getByTestId("direct-purchase-reverse"));
    await user.type(screen.getByTestId("direct-purchase-reverse-reason"), "Try");
    await user.click(screen.getByTestId("direct-purchase-reverse-confirm"));

    await waitFor(() => {
      expect(
        screen.getByText(/stock from this receipt is no longer available/i),
      ).toBeInTheDocument();
    });
  });

  it("shows friendly message when reverse is blocked by supplier payments", async () => {
    const user = userEvent.setup();
    vi.spyOn(directClient, "voidDirectPurchaseReceipt").mockRejectedValue(
      new PosApiError(409, {
        title: "Conflict",
        status: 409,
        detail: "raw detail",
        errorCode: "pos.supplier_payable.void.blocked_by_payments",
      }),
    );

    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId("direct-purchase-reverse")).toBeInTheDocument();
    });
    await user.click(screen.getByTestId("direct-purchase-reverse"));
    await user.type(screen.getByTestId("direct-purchase-reverse-reason"), "Undo");
    await user.click(screen.getByTestId("direct-purchase-reverse-confirm"));

    await waitFor(() => {
      expect(
        screen.getByText(
          /cannot be reversed because supplier payments have already been recorded/i,
        ),
      ).toBeInTheDocument();
    });
  });
});
