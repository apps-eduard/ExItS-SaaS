import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import * as ownershipClient from "@/api/platform/ownership-transfer-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { OrgOwnershipTransferPage } from "@/features/org/ownership/OrgOwnershipTransferPage";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";

vi.mock("@/api/platform/ownership-transfer-client", async (importOriginal) => {
  const actual = await importOriginal<typeof ownershipClient>();
  return {
    ...actual,
    getPendingOwnershipTransferForOrg: vi.fn(),
    resolveOwnershipTransferTarget: vi.fn(),
    requestOwnershipTransfer: vi.fn(),
    cancelOwnershipTransfer: vi.fn(),
  };
});

const onlineMock = vi.hoisted(() => ({ current: true }));

vi.mock("@/connectivity/browser-online", () => ({
  useBrowserOnline: () => onlineMock.current,
  subscribeBrowserOnline: () => () => undefined,
}));

const orgId = "22222222-2222-4222-8222-222222222222";
const transferId = "11111111-1111-4111-8111-111111111111";
const fromOwner = "33333333-3333-4333-8333-333333333333";
const toUser = "44444444-4444-4444-8444-444444444444";

const workspaceMock = vi.hoisted(() => ({
  organizationId: "22222222-2222-4222-8222-222222222222",
  organizationDisplayName: "Corner Store",
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: workspaceMock.organizationId,
      organizationDisplayName: workspaceMock.organizationDisplayName,
      branchId: null,
      branchName: null,
    },
    sessionGrant: { membershipRole: "OrganizationOwner" },
    status: "ready",
  }),
  WorkspaceProvider: ({ children }: { children: ReactNode }) => children,
}));

function sampleTransfer(
  overrides: Partial<ownershipClient.OrganizationOwnershipTransferDto> = {},
): ownershipClient.OrganizationOwnershipTransferDto {
  return {
    id: transferId,
    organizationId: orgId,
    organizationDisplayName: "Corner Store",
    publicOrganizationId: "ORG123456",
    fromOwnerUserId: fromOwner,
    toUserId: toUser,
    toDisplayName: "Paul",
    toPublicUserId: "EX-1111-2222",
    status: "Pending",
    createdAtUtc: "2026-08-20T00:00:00Z",
    expiresAtUtc: "2099-08-27T00:00:00Z",
    acceptedAtUtc: null,
    declinedAtUtc: null,
    cancelledAtUtc: null,
    completedAtUtc: null,
    updatedAtUtc: "2026-08-20T00:00:00Z",
    ...overrides,
  };
}

function renderPage() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <MemoryRouter initialEntries={["/org/ownership-transfer"]}>
            <Routes>
              <Route path="/org/ownership-transfer" element={<OrgOwnershipTransferPage />} />
            </Routes>
          </MemoryRouter>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("OrgOwnershipTransferPage", () => {
  beforeEach(() => {
    onlineMock.current = true;
    workspaceMock.organizationId = orgId;
    workspaceMock.organizationDisplayName = "Corner Store";
    vi.mocked(ownershipClient.getPendingOwnershipTransferForOrg).mockReset();
    vi.mocked(ownershipClient.resolveOwnershipTransferTarget).mockReset();
    vi.mocked(ownershipClient.requestOwnershipTransfer).mockReset();
    vi.mocked(ownershipClient.cancelOwnershipTransfer).mockReset();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it("shows empty initiate form when no pending transfer", async () => {
    vi.mocked(ownershipClient.getPendingOwnershipTransferForOrg).mockResolvedValue(null);
    renderPage();

    expect(await screen.findByTestId("org-ownership-transfer-page")).toBeInTheDocument();
    expect(await screen.findByTestId("ownership-initiate-form")).toBeInTheDocument();
    expect(screen.getByTestId("ownership-target-input")).toBeInTheDocument();
    expect(screen.getByTestId("ownership-resolve")).toBeDisabled();
    expect(screen.queryByTestId("ownership-pending-card")).not.toBeInTheDocument();
  });

  it("renders pending card", async () => {
    vi.mocked(ownershipClient.getPendingOwnershipTransferForOrg).mockResolvedValue(
      sampleTransfer(),
    );
    renderPage();

    expect(await screen.findByTestId("ownership-pending-card")).toBeInTheDocument();
    expect(screen.getByText("Corner Store")).toBeInTheDocument();
    expect(screen.getByTestId("ownership-pending-target")).toHaveTextContent("Paul");
    expect(screen.getByTestId("ownership-pending-target")).toHaveTextContent("EX-1111-2222");
    expect(screen.getByTestId("ownership-cancel")).toBeInTheDocument();
    expect(screen.queryByTestId("ownership-initiate-form")).not.toBeInTheDocument();
  });

  it("resolve then request confirm flow", async () => {
    const user = userEvent.setup();
    vi.mocked(ownershipClient.getPendingOwnershipTransferForOrg)
      .mockResolvedValueOnce(null)
      .mockResolvedValue(sampleTransfer());
    vi.mocked(ownershipClient.resolveOwnershipTransferTarget).mockResolvedValue({
      publicUserId: "EX-1111-2222",
      displayName: "Paul",
    });
    vi.mocked(ownershipClient.requestOwnershipTransfer).mockResolvedValue(sampleTransfer());

    renderPage();
    await screen.findByTestId("ownership-initiate-form");

    await user.type(screen.getByTestId("ownership-target-input"), "EX-1111-2222");
    await user.click(screen.getByTestId("ownership-resolve"));

    expect(await screen.findByTestId("ownership-resolved-target")).toBeInTheDocument();
    expect(screen.getByTestId("ownership-resolved-name")).toHaveTextContent("Paul");
    expect(screen.getByTestId("ownership-resolved-id")).toHaveTextContent("EX-1111-2222");

    await user.click(screen.getByTestId("ownership-request"));
    expect(await screen.findByTestId("ownership-request-confirm")).toBeInTheDocument();
    expect(
      screen.getByText(/Personal data, Personal Utang, payment methods/i),
    ).toBeInTheDocument();

    await user.click(screen.getByTestId("ownership-request-submit"));

    await waitFor(() => {
      expect(ownershipClient.requestOwnershipTransfer).toHaveBeenCalledWith(
        orgId,
        "EX-1111-2222",
      );
    });
    expect(await screen.findByTestId("ownership-pending-card")).toBeInTheDocument();
  });

  it("cancel confirm withdraws pending transfer", async () => {
    const user = userEvent.setup();
    vi.mocked(ownershipClient.getPendingOwnershipTransferForOrg)
      .mockResolvedValueOnce(sampleTransfer())
      .mockResolvedValue(null);
    vi.mocked(ownershipClient.cancelOwnershipTransfer).mockResolvedValue(
      sampleTransfer({ status: "Cancelled", cancelledAtUtc: "2026-08-21T00:00:00Z" }),
    );

    renderPage();
    await screen.findByTestId("ownership-pending-card");
    await user.click(screen.getByTestId("ownership-cancel"));
    expect(await screen.findByTestId("ownership-cancel-confirm")).toBeInTheDocument();
    await user.click(screen.getByTestId("ownership-cancel-submit"));

    await waitFor(() => {
      expect(ownershipClient.cancelOwnershipTransfer).toHaveBeenCalledWith(transferId);
    });
    expect(await screen.findByTestId("ownership-initiate-form")).toBeInTheDocument();
  });

  it("disables mutations offline", async () => {
    onlineMock.current = false;
    vi.mocked(ownershipClient.getPendingOwnershipTransferForOrg).mockResolvedValue(
      sampleTransfer(),
    );
    renderPage();

    expect(await screen.findByTestId("ownership-transfer-offline")).toBeInTheDocument();
    expect(await screen.findByTestId("ownership-cancel")).toBeDisabled();
  });

  it("disables initiate actions offline", async () => {
    onlineMock.current = false;
    vi.mocked(ownershipClient.getPendingOwnershipTransferForOrg).mockResolvedValue(null);
    renderPage();

    expect(await screen.findByTestId("ownership-transfer-offline")).toBeInTheDocument();
    expect(await screen.findByTestId("ownership-initiate-form")).toBeInTheDocument();
    expect(screen.getByTestId("ownership-target-input")).toBeDisabled();
    expect(screen.getByTestId("ownership-resolve")).toBeDisabled();
  });

  it("shows API failure", async () => {
    vi.mocked(ownershipClient.getPendingOwnershipTransferForOrg).mockRejectedValue(
      new PlatformApiError(500, { detail: "boom" }),
    );
    renderPage();
    expect(await screen.findByTestId("error-state")).toBeInTheDocument();
    expect(screen.getByText("Could not load ownership transfer")).toBeInTheDocument();
  });
});
