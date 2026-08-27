import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import * as ownershipClient from "@/api/platform/ownership-transfer-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { PersonalOwnershipTransfersPage } from "@/features/personal/ownership/PersonalOwnershipTransfersPage";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";

vi.mock("@/api/platform/ownership-transfer-client", async (importOriginal) => {
  const actual = await importOriginal<typeof ownershipClient>();
  return {
    ...actual,
    listMyPendingOwnershipTransfers: vi.fn(),
    acceptOwnershipTransfer: vi.fn(),
    declineOwnershipTransfer: vi.fn(),
  };
});

const onlineMock = vi.hoisted(() => ({ current: true }));

vi.mock("@/connectivity/browser-online", () => ({
  useBrowserOnline: () => onlineMock.current,
  subscribeBrowserOnline: () => () => undefined,
}));

const switchMock = vi.hoisted(() => ({
  canSwitch: false,
  switching: false,
  switchToBusiness: vi.fn(),
  online: true,
}));

vi.mock("@/workspace/use-switch-to-business", () => ({
  useSwitchToBusiness: () => switchMock,
  ACCOUNT_CONTEXT_SWITCH_PATH: "/switching-context",
}));

const workspaceMock = vi.hoisted(() => ({
  refreshWorkspaces: vi.fn(async () => undefined),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    refreshWorkspaces: workspaceMock.refreshWorkspaces,
    workspaces: [],
    status: "ready",
  }),
  WorkspaceProvider: ({ children }: { children: ReactNode }) => children,
}));

const transferId = "11111111-1111-4111-8111-111111111111";
const orgId = "22222222-2222-4222-8222-222222222222";
const fromOwner = "33333333-3333-4333-8333-333333333333";
const toUser = "44444444-4444-4444-8444-444444444444";

function sampleTransfer(
  overrides: Partial<ownershipClient.OrganizationOwnershipTransferDto> = {},
): ownershipClient.OrganizationOwnershipTransferDto {
  return {
    id: transferId,
    organizationId: orgId,
    organizationDisplayName: "Org A Market",
    publicOrganizationId: "ORG111111",
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
          <MemoryRouter initialEntries={["/personal/ownership-transfers"]}>
            <Routes>
              <Route
                path="/personal/ownership-transfers"
                element={<PersonalOwnershipTransfersPage />}
              />
            </Routes>
          </MemoryRouter>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("PersonalOwnershipTransfersPage", () => {
  beforeEach(() => {
    onlineMock.current = true;
    switchMock.canSwitch = false;
    switchMock.switching = false;
    switchMock.switchToBusiness.mockReset();
    workspaceMock.refreshWorkspaces.mockReset();
    workspaceMock.refreshWorkspaces.mockResolvedValue(undefined);
    vi.mocked(ownershipClient.listMyPendingOwnershipTransfers).mockReset();
    vi.mocked(ownershipClient.acceptOwnershipTransfer).mockReset();
    vi.mocked(ownershipClient.declineOwnershipTransfer).mockReset();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it("renders pending list", async () => {
    vi.mocked(ownershipClient.listMyPendingOwnershipTransfers).mockResolvedValue([
      sampleTransfer(),
    ]);
    renderPage();

    expect(await screen.findByTestId("personal-ownership-transfers-page")).toBeInTheDocument();
    expect(await screen.findByTestId("ownership-transfer-card")).toBeInTheDocument();
    expect(screen.getByText("Org A Market")).toBeInTheDocument();
    expect(screen.getByText("ORG111111")).toBeInTheDocument();
    expect(screen.queryByText(fromOwner)).not.toBeInTheDocument();
  });

  it("shows empty state", async () => {
    vi.mocked(ownershipClient.listMyPendingOwnershipTransfers).mockResolvedValue([]);
    renderPage();
    expect(await screen.findByTestId("ownership-transfer-empty")).toBeInTheDocument();
  });

  it("shows loading skeleton", async () => {
    let resolveList!: (value: ownershipClient.OrganizationOwnershipTransferDto[]) => void;
    vi.mocked(ownershipClient.listMyPendingOwnershipTransfers).mockReturnValue(
      new Promise((resolve) => {
        resolveList = resolve;
      }),
    );
    renderPage();
    expect(await screen.findByTestId("loading-skeleton")).toBeInTheDocument();
    resolveList([]);
    expect(await screen.findByTestId("ownership-transfer-empty")).toBeInTheDocument();
  });

  it("shows API failure", async () => {
    vi.mocked(ownershipClient.listMyPendingOwnershipTransfers).mockRejectedValue(
      new PlatformApiError(500, { detail: "boom" }),
    );
    renderPage();
    expect(await screen.findByTestId("error-state")).toBeInTheDocument();
    expect(screen.getByText("Could not load transfers")).toBeInTheDocument();
  });

  it("disables mutations offline", async () => {
    onlineMock.current = false;
    vi.mocked(ownershipClient.listMyPendingOwnershipTransfers).mockResolvedValue([
      sampleTransfer(),
    ]);
    renderPage();
    expect(await screen.findByTestId("ownership-transfer-offline")).toBeInTheDocument();
    expect(await screen.findByTestId("ownership-transfer-accept")).toBeDisabled();
    expect(screen.getByTestId("ownership-transfer-decline")).toBeDisabled();
  });

  it("accept confirmation then success with go to business when orgs available", async () => {
    const user = userEvent.setup();
    vi.mocked(ownershipClient.listMyPendingOwnershipTransfers)
      .mockResolvedValueOnce([sampleTransfer()])
      .mockResolvedValue([]);
    vi.mocked(ownershipClient.acceptOwnershipTransfer).mockResolvedValue(
      sampleTransfer({ status: "Accepted", acceptedAtUtc: "2026-08-21T00:00:00Z" }),
    );

    renderPage();
    await screen.findByTestId("ownership-transfer-card");
    await user.click(screen.getByTestId("ownership-transfer-accept"));
    expect(await screen.findByTestId("ownership-transfer-accept-confirm")).toBeInTheDocument();
    expect(screen.getByText(/Become owner of Org A Market/i)).toBeInTheDocument();
    expect(
      screen.getByText(/does not transfer Personal data, Personal Utang/i),
    ).toBeInTheDocument();

    await user.click(screen.getByTestId("ownership-transfer-accept-submit"));

    expect(await screen.findByTestId("ownership-transfer-success")).toBeInTheDocument();
    expect(screen.getByText(/You're now the owner of Org A Market/i)).toBeInTheDocument();
    expect(screen.getByTestId("ownership-go-to-business")).toBeInTheDocument();
    expect(workspaceMock.refreshWorkspaces).not.toHaveBeenCalled();

    await user.click(screen.getByTestId("ownership-go-to-business"));
    await waitFor(() => {
      expect(workspaceMock.refreshWorkspaces).toHaveBeenCalled();
      expect(switchMock.switchToBusiness).toHaveBeenCalled();
    });
  });

  it("declines a pending transfer", async () => {
    const user = userEvent.setup();
    vi.mocked(ownershipClient.listMyPendingOwnershipTransfers)
      .mockResolvedValueOnce([sampleTransfer()])
      .mockResolvedValue([]);
    vi.mocked(ownershipClient.declineOwnershipTransfer).mockResolvedValue(
      sampleTransfer({ status: "Declined", declinedAtUtc: "2026-08-21T00:00:00Z" }),
    );

    renderPage();
    await screen.findByTestId("ownership-transfer-card");
    await user.click(screen.getByTestId("ownership-transfer-decline"));
    expect(await screen.findByTestId("ownership-transfer-decline-confirm")).toBeInTheDocument();
    await user.click(screen.getByTestId("ownership-transfer-decline-submit"));

    await waitFor(() => {
      expect(ownershipClient.declineOwnershipTransfer).toHaveBeenCalledWith(transferId);
    });
    expect(await screen.findByTestId("ownership-transfer-empty")).toBeInTheDocument();
  });

  it("hides actions when expired", async () => {
    vi.mocked(ownershipClient.listMyPendingOwnershipTransfers).mockResolvedValue([
      sampleTransfer({
        status: "Expired",
        expiresAtUtc: "2020-01-01T00:00:00Z",
      }),
    ]);
    renderPage();
    expect(await screen.findByTestId("ownership-transfer-expired")).toBeInTheDocument();
    expect(screen.queryByTestId("ownership-transfer-accept")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ownership-transfer-decline")).not.toBeInTheDocument();
  });

  it("disables double-submit while accept is pending", async () => {
    const user = userEvent.setup();
    let resolveAccept!: (
      value: ownershipClient.OrganizationOwnershipTransferDto,
    ) => void;
    vi.mocked(ownershipClient.listMyPendingOwnershipTransfers).mockResolvedValue([
      sampleTransfer(),
    ]);
    vi.mocked(ownershipClient.acceptOwnershipTransfer).mockReturnValue(
      new Promise((resolve) => {
        resolveAccept = resolve;
      }),
    );

    renderPage();
    await screen.findByTestId("ownership-transfer-card");
    await user.click(screen.getByTestId("ownership-transfer-accept"));
    await user.click(screen.getByTestId("ownership-transfer-accept-submit"));

    await waitFor(() => {
      expect(screen.getByTestId("ownership-transfer-accept")).toBeDisabled();
      expect(screen.getByTestId("ownership-transfer-decline")).toBeDisabled();
      expect(screen.getByTestId("ownership-transfer-accept-submit")).toBeDisabled();
    });

    resolveAccept(sampleTransfer({ status: "Accepted" }));
    vi.mocked(ownershipClient.listMyPendingOwnershipTransfers).mockResolvedValue([]);
    await screen.findByTestId("ownership-transfer-success");
  });
});
