import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as linkRequestsClient from "@/api/platform/customer-link-requests-client";
import { PersonalCustomerLinksPage } from "@/features/personal/customer-links/PersonalCustomerLinksPage";

vi.mock("@/api/platform/customer-link-requests-client", async (importOriginal) => {
  const actual = await importOriginal<typeof linkRequestsClient>();
  return {
    ...actual,
    listPendingCustomerLinkRequests: vi.fn(),
    listResolvedCustomerLinkRequests: vi.fn(),
    acceptCustomerLinkRequest: vi.fn(),
    declineCustomerLinkRequest: vi.fn(),
    blockBusinessFromCustomerLinkRequest: vi.fn(),
  };
});

const historyRequestId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const orgId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const businessCustomerId = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";

describe("PersonalCustomerLinksPage history tab", () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it("loads resolved history when History tab is selected", async () => {
    vi.mocked(linkRequestsClient.listPendingCustomerLinkRequests).mockResolvedValue([]);
    vi.mocked(linkRequestsClient.listResolvedCustomerLinkRequests).mockResolvedValue([
      {
        id: historyRequestId,
        organizationId: orgId,
        organizationDisplayName: "Corner Store",
        businessCustomerId,
        status: "Active",
        createdAtUtc: "2026-08-20T00:00:00Z",
        expiresAtUtc: "2026-08-27T00:00:00Z",
        targetPublicUserId: "EX-4827-1936",
      },
    ]);

    const user = userEvent.setup();
    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/personal/customer-links"]}>
          <Routes>
            <Route path="/personal/customer-links" element={<PersonalCustomerLinksPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("personal-customer-links-page")).toBeInTheDocument();
    });
    expect(linkRequestsClient.listPendingCustomerLinkRequests).toHaveBeenCalled();
    expect(linkRequestsClient.listResolvedCustomerLinkRequests).not.toHaveBeenCalled();

    await user.click(screen.getByTestId("customer-links-tab-history"));

    await waitFor(() => {
      expect(linkRequestsClient.listResolvedCustomerLinkRequests).toHaveBeenCalled();
      expect(screen.getByTestId(`customer-link-history-${historyRequestId}`)).toBeInTheDocument();
    });
    expect(screen.getByText("Corner Store")).toBeInTheDocument();
  });
});
