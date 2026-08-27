import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { resolvePublicUserId } from "@/api/platform/public-identity-client";
import {
  findCustomerByLinkedPersonalPublicUserId,
  searchCheckoutCustomers,
} from "@/api/pos/pos-customers-client";
import { buildExItsQr } from "@/lib/exits-qr/envelope";
import { CheckoutPersonalCustomerPicker } from "@/features/checkout/CheckoutPersonalCustomerPicker";
import * as cameraAccess from "@/lib/qr/camera-access";
import * as decodeFrame from "@/lib/qr/decode-qr-frame";

vi.mock("@/api/platform/public-identity-client", () => ({
  resolvePublicUserId: vi.fn(),
}));

vi.mock("@/api/pos/pos-customers-client", () => ({
  findCustomerByLinkedPersonalPublicUserId: vi.fn(),
  searchCheckoutCustomers: vi.fn(),
}));

vi.mock("@/lib/qr/camera-access", async (importOriginal) => {
  const actual = await importOriginal<typeof cameraAccess>();
  return {
    ...actual,
    isCameraApiAvailable: vi.fn(() => true),
    isCameraSecureContext: vi.fn(() => true),
    openPreferredCamera: vi.fn(),
    stopMediaStream: vi.fn(),
  };
});

vi.mock("@/lib/qr/decode-qr-frame", async (importOriginal) => {
  const actual = await importOriginal<typeof decodeFrame>();
  return {
    ...actual,
    decodeQrFromVideoFrame: vi.fn(),
  };
});

const workspace = {
  organizationId: "11111111-1111-1111-1111-111111111111",
  branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
};

const customerId = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
const publicId = "EX-4827-1936";
const resolvedPersonal = {
  publicUserId: publicId,
  displayName: "Rosa Santos",
  userIdentityId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
  maskedEmail: null,
  status: "Active",
  isSelf: false,
};

function renderPicker(
  options: {
    canLinkCustomer?: boolean;
    selectedCustomerId?: string | null;
    onCustomerSelected?: (customer: {
      customerId: string;
      displayName: string;
      status: string;
    }) => void;
  } = {},
) {
  const onCustomerSelected = options.onCustomerSelected ?? vi.fn();
  render(
    <AppProviders>
      <MemoryRouter>
        <CheckoutPersonalCustomerPicker
          workspace={workspace}
          canLinkCustomer={options.canLinkCustomer ?? true}
          returnTo="/checkout/cash"
          selectedCustomerId={options.selectedCustomerId}
          onCustomerSelected={onCustomerSelected}
        />
      </MemoryRouter>
    </AppProviders>,
  );
  return { onCustomerSelected };
}

async function expandLookup(user: ReturnType<typeof userEvent.setup>) {
  await user.click(screen.getByTestId("checkout-personal-customer-picker-toggle"));
  expect(screen.getByTestId("qr-manual-id")).toBeVisible();
}

describe("CheckoutPersonalCustomerPicker", () => {
  beforeEach(() => {
    HTMLVideoElement.prototype.play = vi.fn().mockResolvedValue(undefined);
    vi.mocked(resolvePublicUserId).mockReset();
    vi.mocked(findCustomerByLinkedPersonalPublicUserId).mockReset();
    vi.mocked(searchCheckoutCustomers).mockReset();
    vi.mocked(searchCheckoutCustomers).mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });
    vi.mocked(cameraAccess.openPreferredCamera).mockReset();
    vi.mocked(decodeFrame.decodeQrFromVideoFrame).mockReset();
    vi.mocked(decodeFrame.decodeQrFromVideoFrame).mockResolvedValue(null);
  });

  it("hides ExItS lookup until the cashier opens it", () => {
    renderPicker();
    expect(screen.queryByTestId("qr-manual-id")).not.toBeInTheDocument();
    expect(screen.getByTestId("checkout-personal-customer-picker-toggle")).toBeVisible();
  });

  it("does not render lookup when a customer is already selected", () => {
    renderPicker({ selectedCustomerId: customerId });
    expect(screen.queryByTestId("checkout-personal-customer-picker")).not.toBeInTheDocument();
    expect(resolvePublicUserId).not.toHaveBeenCalled();
  });

  it("selects an existing contact from search and does not offer add/link", async () => {
    const user = userEvent.setup();
    vi.mocked(resolvePublicUserId).mockResolvedValue(resolvedPersonal);
    vi.mocked(findCustomerByLinkedPersonalPublicUserId).mockResolvedValue(null);
    vi.mocked(searchCheckoutCustomers).mockResolvedValue({
      items: [
        {
          customerId,
          displayName: "Rosa Santos",
          mobileNumber: "09171234567",
          status: "Active",
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 20,
    });

    const { onCustomerSelected } = renderPicker();
    await expandLookup(user);
    await user.type(screen.getByTestId("qr-manual-id"), publicId);
    await user.click(screen.getByTestId("qr-manual-submit"));

    await waitFor(() => {
      expect(onCustomerSelected).toHaveBeenCalledWith(
        expect.objectContaining({ customerId, displayName: "Rosa Santos" }),
      );
    });
    expect(screen.queryByTestId("checkout-personal-add-link")).not.toBeInTheDocument();
  });

  it("selects an existing correlated customer from manual ExItS ID", async () => {
    const user = userEvent.setup();
    vi.mocked(resolvePublicUserId).mockResolvedValue(resolvedPersonal);
    vi.mocked(findCustomerByLinkedPersonalPublicUserId).mockResolvedValue({
      customerId,
      displayName: "Rosa Santos",
      mobileNumber: "09171234567",
      status: "Active",
    });

    const { onCustomerSelected } = renderPicker();
    await expandLookup(user);

    await user.type(screen.getByTestId("qr-manual-id"), publicId);
    await user.click(screen.getByTestId("qr-manual-submit"));

    await waitFor(() => {
      expect(onCustomerSelected).toHaveBeenCalledWith({
        customerId,
        displayName: "Rosa Santos",
        mobileNumber: "09171234567",
        status: "Active",
        linkedPersonalPublicUserId: publicId,
        resolvedPersonalDisplayName: "Rosa Santos",
      });
    });
    expect(resolvePublicUserId).toHaveBeenCalledWith(publicId, "SaleCustomer");
    expect(findCustomerByLinkedPersonalPublicUserId).toHaveBeenCalledWith(
      workspace,
      publicId,
      undefined,
    );
  });

  it("passes the resolved Personal name when the POS record still has a Local Validation label", async () => {
    const user = userEvent.setup();
    vi.mocked(resolvePublicUserId).mockResolvedValue(resolvedPersonal);
    vi.mocked(findCustomerByLinkedPersonalPublicUserId).mockResolvedValue({
      customerId,
      displayName: "Local Walkin 20260826230002",
      mobileNumber: "09171110001",
      status: "Active",
    });

    const { onCustomerSelected } = renderPicker();
    await expandLookup(user);

    await user.type(screen.getByTestId("qr-manual-id"), publicId);
    await user.click(screen.getByTestId("qr-manual-submit"));

    await waitFor(() => {
      expect(onCustomerSelected).toHaveBeenCalledWith(
        expect.objectContaining({
          customerId,
          displayName: "Local Walkin 20260826230002",
          resolvedPersonalDisplayName: "Rosa Santos",
          linkedPersonalPublicUserId: publicId,
        }),
      );
    });
  });

  it("selects an existing correlated customer from Personal QR payload", async () => {
    const user = userEvent.setup();
    const payload = buildExItsQr("personal", publicId);
    vi.mocked(resolvePublicUserId).mockResolvedValue(resolvedPersonal);
    vi.mocked(findCustomerByLinkedPersonalPublicUserId).mockResolvedValue({
      customerId,
      displayName: "Rosa Santos",
      mobileNumber: null,
      status: "Active",
    });

    const { onCustomerSelected } = renderPicker();
    await expandLookup(user);

    await user.type(screen.getByTestId("qr-manual-id"), payload);
    await user.click(screen.getByTestId("qr-manual-submit"));

    await waitFor(() => {
      expect(onCustomerSelected).toHaveBeenCalledWith(
        expect.objectContaining({ customerId, displayName: "Rosa Santos" }),
      );
    });
  });

  it("shows not-linked UI without creating a customer", async () => {
    const user = userEvent.setup();
    vi.mocked(resolvePublicUserId).mockResolvedValue(resolvedPersonal);
    vi.mocked(findCustomerByLinkedPersonalPublicUserId).mockResolvedValue(null);

    renderPicker();
    await expandLookup(user);

    await user.type(screen.getByTestId("qr-manual-id"), publicId);
    await user.click(screen.getByTestId("qr-manual-submit"));

    await waitFor(() => {
      expect(screen.getByTestId("checkout-personal-not-linked")).toBeInTheDocument();
    });
    expect(screen.getByText("Customer not linked to this business")).toBeInTheDocument();
    expect(screen.getByTestId("checkout-personal-add-link")).toHaveAttribute(
      "href",
      `/customers/new?linkPublicId=${encodeURIComponent(publicId)}&returnTo=${encodeURIComponent("/checkout/cash")}`,
    );
  });

  it("hides add/link when the staff member lacks customer-create permission", async () => {
    const user = userEvent.setup();
    vi.mocked(resolvePublicUserId).mockResolvedValue(resolvedPersonal);
    vi.mocked(findCustomerByLinkedPersonalPublicUserId).mockResolvedValue(null);

    renderPicker({ canLinkCustomer: false });
    await expandLookup(user);

    await user.type(screen.getByTestId("qr-manual-id"), publicId);
    await user.click(screen.getByTestId("qr-manual-submit"));

    await waitFor(() => {
      expect(screen.getByTestId("checkout-personal-not-linked")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("checkout-personal-add-link")).not.toBeInTheDocument();
    expect(screen.getByText(/Ask a manager to add or link the customer/i)).toBeInTheDocument();
  });

  it("rejects organization QR in Personal customer selection", async () => {
    const user = userEvent.setup();
    renderPicker();
    await expandLookup(user);

    await user.type(screen.getByTestId("qr-manual-id"), buildExItsQr("organization", "ORG000123"));
    await user.click(screen.getByTestId("qr-manual-submit"));

    await waitFor(() => {
      expect(screen.getByTestId("qr-error")).toHaveTextContent(
        "This is an organization ExItS ID (ORG…). Only a personal ExItS ID (EX-…) can be added here.",
      );
    });
    expect(resolvePublicUserId).not.toHaveBeenCalled();
  });

  it("rejects device-registration QR in Personal customer selection", async () => {
    const user = userEvent.setup();
    renderPicker();
    await expandLookup(user);

    await user.type(
      screen.getByTestId("qr-manual-id"),
      buildExItsQr("pos-device-registration", "opaque-token"),
    );
    await user.click(screen.getByTestId("qr-manual-submit"));

    await waitFor(() => {
      expect(screen.getByTestId("qr-error")).toHaveTextContent("This QR code can't be used here.");
    });
    expect(resolvePublicUserId).not.toHaveBeenCalled();
  });

  it("rejects malformed QR payloads", async () => {
    const user = userEvent.setup();
    renderPicker();
    await expandLookup(user);

    await user.type(screen.getByTestId("qr-manual-id"), "https://evil.example/qr");
    await user.click(screen.getByTestId("qr-manual-submit"));

    await waitFor(() => {
      expect(screen.getByTestId("qr-error")).toHaveTextContent(/not a valid ExItS QR/i);
    });
    expect(resolvePublicUserId).not.toHaveBeenCalled();
  });

  it("resolves linked customer from live Personal QR without auto-finalizing sale", async () => {
    vi.mocked(cameraAccess.openPreferredCamera).mockResolvedValue({
      ok: true,
      stream: {
        getTracks: () => [],
        getVideoTracks: () => [],
      } as unknown as MediaStream,
      facingMode: "environment",
    });
    vi.mocked(decodeFrame.decodeQrFromVideoFrame).mockResolvedValue(
      buildExItsQr("personal", publicId),
    );

    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    vi.useFakeTimers({ shouldAdvanceTime: true });
    vi.mocked(resolvePublicUserId).mockResolvedValue(resolvedPersonal);
    vi.mocked(findCustomerByLinkedPersonalPublicUserId).mockResolvedValue({
      customerId,
      displayName: "Rosa Santos",
      mobileNumber: null,
      status: "Active",
    });

    const { onCustomerSelected } = renderPicker();
    await expandLookup(user);

    await user.click(screen.getByTestId("qr-mode-scan"));
    await user.click(screen.getByTestId("qr-live-camera-button"));
    // LiveQrCameraScanner auto-starts the camera when the sheet opens.
    await vi.advanceTimersByTimeAsync(200);

    await waitFor(() => {
      expect(onCustomerSelected).toHaveBeenCalledWith(
        expect.objectContaining({ customerId, displayName: "Rosa Santos" }),
      );
    });
    vi.useRealTimers();
  });
});
