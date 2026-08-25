import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { PosDeviceDto } from "@/api/platform/pos-devices-client";
import { I18nProvider } from "@/i18n/I18nProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { INITIAL_POS_DEVICE_CONTEXT } from "@/workspace/pos-device-context";

const LOCAL_ID = "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee";
const OTHER_ID = "11111111-2222-4333-8444-555555555555";
const ORG_ID = "99999999-9999-4999-8999-999999999999";
const BRANCH_ID = "88888888-8888-4888-8888-888888888888";
const CURRENT_DEVICE_ID = "device-current";
const OTHER_DEVICE_ID = "device-other";

const listPosDevices = vi.fn();
const getPosDeviceCapacity = vi.fn();
const revokePosDevice = vi.fn();
const registerPosDevice = vi.fn();
const getPlatformCredentialStatus = vi.fn();
const issuePosDeviceRevokeStepUp = vi.fn();
const refreshPosDevice = vi.fn(async () => undefined);

let registrationStatus = "authorized";

vi.mock("@/api/platform/pos-devices-client", () => ({
  listPosDevices: (...args: unknown[]) => listPosDevices(...args),
  getPosDeviceCapacity: (...args: unknown[]) => getPosDeviceCapacity(...args),
  revokePosDevice: (...args: unknown[]) => revokePosDevice(...args),
  registerPosDevice: (...args: unknown[]) => registerPosDevice(...args),
}));

vi.mock("@/api/platform/platform-credentials-client", () => ({
  getPlatformCredentialStatus: (...args: unknown[]) => getPlatformCredentialStatus(...args),
}));

vi.mock("@/api/platform/governance-step-up-client", () => ({
  issuePosDeviceRevokeStepUp: (...args: unknown[]) => issuePosDeviceRevokeStepUp(...args),
  POS_DEVICE_REVOKE_ACTION: "platform.pos_device.revoke",
  TARGET_POS_DEVICE: "PosDevice",
}));

vi.mock("@/workspace/browser-installation-identity", () => ({
  peekDurableInstallationDeviceId: () => LOCAL_ID,
  getDurableInstallationDeviceId: () => ({
    ok: true as const,
    installationDeviceId: LOCAL_ID,
    created: false,
  }),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: ORG_ID,
      organizationDisplayName: "Sari Store",
      branchId: BRANCH_ID,
      branchName: "Main branch",
      experience: "managing",
    },
    sessionGrant: { organizationManagementAuthority: true, productAccessAllowed: true },
    workspaces: [
      {
        organizationId: ORG_ID,
        displayName: "Sari Store",
        branches: [
          {
            branchId: BRANCH_ID,
            name: "Main branch",
            secondaryLine: "",
            isPrimary: true,
            isActive: true,
          },
        ],
      },
    ],
    posDevice: {
      ...INITIAL_POS_DEVICE_CONTEXT,
      status: "authorized",
      registrationStatus,
      installationDeviceId: LOCAL_ID,
      durableIdentityAvailable: true,
    },
    refreshPosDevice,
    deviceEnforcementEnabled: false,
  }),
}));

const { OrgPosDevicesPage } = await import("@/features/devices/OrgPosDevicesPage");

function deviceDto(overrides: Partial<PosDeviceDto> = {}): PosDeviceDto {
  return {
    id: CURRENT_DEVICE_ID,
    organizationId: ORG_ID,
    branchId: BRANCH_ID,
    installationDeviceId: LOCAL_ID,
    friendlyName: "Counter browser",
    platform: "Browser",
    model: "Chrome on Windows",
    appVersion: "1.0.0",
    status: "Active",
    registeredAtUtc: "2026-08-20T01:00:00Z",
    lastSeenAtUtc: "2026-08-22T01:00:00Z",
    revokedAtUtc: null,
    ...overrides,
  };
}

function renderPage(ui: ReactNode = <OrgPosDevicesPage />) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <MemoryRouter>
      <QueryClientProvider client={queryClient}>
        <PreferencesProvider>
          <I18nProvider>{ui}</I18nProvider>
        </PreferencesProvider>
      </QueryClientProvider>
    </MemoryRouter>,
  );
}

async function openRevokeSheet(deviceId = CURRENT_DEVICE_ID) {
  const user = userEvent.setup();
  await user.click(await screen.findByTestId(`device-revoke-${deviceId}`));
  return user;
}

beforeEach(() => {
  vi.clearAllMocks();
  registrationStatus = "authorized";
  listPosDevices.mockResolvedValue({ ok: true, value: [deviceDto()] });
  getPosDeviceCapacity.mockResolvedValue({ ok: true, value: { used: 1, allowed: 5 } });
  getPlatformCredentialStatus.mockResolvedValue({
    ok: true,
    value: { hasPassword: true, emailVerified: true, isLockedOut: false },
  });
  issuePosDeviceRevokeStepUp.mockResolvedValue({
    ok: true,
    value: {
      stepUpToken: "opaque-step-up",
      expiresAtUtc: "2026-08-22T01:05:00Z",
      actionCode: "platform.pos_device.revoke",
      targetType: "PosDevice",
      targetId: CURRENT_DEVICE_ID,
    },
  });
  revokePosDevice.mockResolvedValue({
    ok: true,
    value: deviceDto({ status: "Revoked", revokedAtUtc: "2026-08-22T02:00:00Z" }),
  });
});

describe("OrgPosDevicesPage this-device awareness", () => {
  it("badges the matching installation and not the others", async () => {
    listPosDevices.mockResolvedValue({
      ok: true,
      value: [
        deviceDto(),
        deviceDto({
          id: OTHER_DEVICE_ID,
          installationDeviceId: OTHER_ID,
          friendlyName: "Stock room tablet",
        }),
      ],
    });
    renderPage();

    expect(await screen.findByTestId(`device-this-device-${CURRENT_DEVICE_ID}`)).toBeVisible();
    expect(screen.queryByTestId(`device-this-device-${OTHER_DEVICE_ID}`)).not.toBeInTheDocument();
  });

  it("hides the register form while this browser is active", async () => {
    renderPage();
    await screen.findByTestId(`device-row-${CURRENT_DEVICE_ID}`);
    expect(screen.queryByTestId("devices-register-browser")).not.toBeInTheDocument();
    expect(screen.getByTestId("devices-this-browser")).toHaveAttribute("data-state", "active");
  });

  it("does not expose registration-code customer UX", async () => {
    renderPage();
    await screen.findByTestId("devices-list");
    expect(screen.queryByText(/registration code/i)).toBeNull();
    expect(screen.queryByText(/register with a code/i)).toBeNull();
    // Active browser under optional PWA: no register push CTA.
    expect(screen.queryByTestId("devices-open-register")).toBeNull();
    expect(screen.queryByTestId("devices-register-optional")).toBeNull();
  });

  it("keeps unregistered registration optional without auto-opening the form", async () => {
    registrationStatus = "unregistered";
    listPosDevices.mockResolvedValue({ ok: true, value: [] });
    renderPage();

    expect(await screen.findByTestId("devices-enforcement-paused-hint")).toBeVisible();
    expect(screen.queryByTestId("devices-register-form")).not.toBeInTheDocument();
    expect(screen.getByTestId("devices-register-optional")).toHaveTextContent(/register this browser/i);

    await userEvent.setup().click(screen.getByTestId("devices-register-optional"));
    expect(screen.getByTestId("devices-register-form")).toBeVisible();
  });

  it("hides revoked devices from the normal active list", async () => {
    listPosDevices.mockResolvedValue({
      ok: true,
      value: [
        deviceDto({ id: CURRENT_DEVICE_ID, status: "Active", friendlyName: "Shop PC" }),
        deviceDto({
          id: OTHER_DEVICE_ID,
          status: "Revoked",
          friendlyName: "Old Phone",
          revokedAtUtc: "2026-08-22T02:00:00Z",
          installationDeviceId: "other-install",
        }),
      ],
    });

    renderPage();

    expect(await screen.findByTestId(`device-row-${CURRENT_DEVICE_ID}`)).toBeVisible();
    expect(screen.queryByTestId(`device-row-${OTHER_DEVICE_ID}`)).toBeNull();
    expect(screen.getByTestId(`device-revoke-${CURRENT_DEVICE_ID}`)).toHaveTextContent(
      /remove device/i,
    );
  });

  it("keeps register hidden behind an explicit action when this browser is revoked", async () => {
    registrationStatus = "revoked";
    listPosDevices.mockResolvedValue({
      ok: true,
      value: [],
    });
    renderPage();

    expect(await screen.findByTestId("devices-this-browser-revoked")).toBeVisible();
    expect(screen.queryByTestId("devices-register-browser")).not.toBeInTheDocument();

    await userEvent.setup().click(screen.getByTestId("devices-register-again"));
    expect(screen.getByTestId("devices-register-browser")).toBeVisible();
  });
});

describe("OrgPosDevicesPage revoke governance", () => {
  it("issues a step-up token before revoking and clears the password", async () => {
    renderPage();
    const user = await openRevokeSheet();

    await user.type(screen.getByTestId("devices-revoke-reason"), "Lost at the counter");
    await user.type(screen.getByTestId("devices-revoke-password"), "owner-password");
    await user.click(screen.getByTestId("devices-revoke-confirm"));

    await waitFor(() => expect(revokePosDevice).toHaveBeenCalledTimes(1));

    expect(issuePosDeviceRevokeStepUp).toHaveBeenCalledWith(
      ORG_ID,
      CURRENT_DEVICE_ID,
      "owner-password",
    );
    expect(issuePosDeviceRevokeStepUp.mock.invocationCallOrder[0]).toBeLessThan(
      revokePosDevice.mock.invocationCallOrder[0],
    );
    expect(revokePosDevice).toHaveBeenCalledWith(ORG_ID, CURRENT_DEVICE_ID, {
      reason: "Lost at the counter",
      stepUpToken: "opaque-step-up",
    });

    await waitFor(() => expect(screen.getByTestId("devices-revoked-current-notice")).toBeVisible());
    expect(registerPosDevice).not.toHaveBeenCalled();
  });

  it("blocks a reason shorter than eight characters without calling the API", async () => {
    renderPage();
    const user = await openRevokeSheet();

    await user.type(screen.getByTestId("devices-revoke-reason"), "lost");
    await user.type(screen.getByTestId("devices-revoke-password"), "owner-password");

    expect(screen.getByTestId("devices-revoke-confirm")).toBeDisabled();
    expect(issuePosDeviceRevokeStepUp).not.toHaveBeenCalled();
    expect(revokePosDevice).not.toHaveBeenCalled();
  });

  it("blocks confirm until a password is entered", async () => {
    renderPage();
    const user = await openRevokeSheet();

    await user.type(screen.getByTestId("devices-revoke-reason"), "Retired terminal");
    expect(screen.getByTestId("devices-revoke-confirm")).toBeDisabled();
  });

  it("explains the platform gap when the account has no password", async () => {
    getPlatformCredentialStatus.mockResolvedValue({
      ok: true,
      value: { hasPassword: false, emailVerified: true, isLockedOut: false },
    });
    renderPage();
    const user = await openRevokeSheet();

    await user.type(screen.getByTestId("devices-revoke-reason"), "Retired terminal");
    await user.type(screen.getByTestId("devices-revoke-password"), "anything");
    await user.click(screen.getByTestId("devices-revoke-confirm"));

    expect(await screen.findByTestId("devices-revoke-error")).toHaveTextContent(/no password yet/i);
    expect(issuePosDeviceRevokeStepUp).not.toHaveBeenCalled();
    expect(revokePosDevice).not.toHaveBeenCalled();
  });

  it("reports a wrong password without revoking", async () => {
    issuePosDeviceRevokeStepUp.mockResolvedValue({
      ok: false,
      reason: "wrong_password",
      status: 400,
      body: null,
    });
    renderPage();
    const user = await openRevokeSheet();

    await user.type(screen.getByTestId("devices-revoke-reason"), "Retired terminal");
    await user.type(screen.getByTestId("devices-revoke-password"), "wrong");
    await user.click(screen.getByTestId("devices-revoke-confirm"));

    expect(await screen.findByTestId("devices-revoke-error")).toHaveTextContent(/did not match/i);
    expect(revokePosDevice).not.toHaveBeenCalled();
    expect(screen.getByTestId("devices-revoke-password")).toHaveValue("");
  });

  it("warns harder when the target is the browser in use", async () => {
    listPosDevices.mockResolvedValue({
      ok: true,
      value: [
        deviceDto(),
        deviceDto({
          id: OTHER_DEVICE_ID,
          installationDeviceId: OTHER_ID,
          friendlyName: "Stock room tablet",
        }),
      ],
    });
    renderPage();

    const user = await openRevokeSheet(OTHER_DEVICE_ID);
    expect(screen.getByTestId("devices-revoke-warning")).not.toHaveTextContent(/currently using/i);
    await user.click(screen.getByTestId("devices-revoke-cancel"));

    await openRevokeSheet(CURRENT_DEVICE_ID);
    expect(screen.getByTestId("devices-revoke-warning")).toHaveTextContent(/currently using/i);
  });

  it("uses a masked password field bound to the current-password autofill slot", async () => {
    renderPage();
    const user = await openRevokeSheet();

    const password = screen.getByTestId("devices-revoke-password");
    expect(password).toHaveAttribute("type", "password");
    expect(password).toHaveAttribute("autocomplete", "current-password");

    await user.click(screen.getByTestId("devices-revoke-password-toggle"));
    expect(screen.getByTestId("devices-revoke-password")).toHaveAttribute("type", "text");
  });
});

describe("OrgPosDevicesPage registration metadata", () => {
  it("sends browser platform, model, and app version when registering", async () => {
    registrationStatus = "unregistered";
    listPosDevices.mockResolvedValue({ ok: true, value: [] });
    registerPosDevice.mockResolvedValue({ ok: true, value: deviceDto() });
    renderPage();

    const user = userEvent.setup();
    await user.click(await screen.findByTestId("devices-register-optional"));
    await user.selectOptions(await screen.findByTestId("devices-branch-select"), BRANCH_ID);
    await user.click(screen.getByTestId("devices-register-browser"));

    await waitFor(() => expect(registerPosDevice).toHaveBeenCalledTimes(1));
    const [, body] = registerPosDevice.mock.calls[0] as [string, Record<string, unknown>];
    expect(body).toMatchObject({
      branchId: BRANCH_ID,
      installationDeviceId: LOCAL_ID,
      platform: "Browser",
    });
    expect(typeof body.model).toBe("string");
  });

  it("blocks registration when capacity is full", async () => {
    registrationStatus = "unregistered";
    listPosDevices.mockResolvedValue({ ok: true, value: [] });
    getPosDeviceCapacity.mockResolvedValue({ ok: true, value: { used: 5, allowed: 5 } });
    renderPage();

    await userEvent.setup().click(await screen.findByTestId("devices-register-optional"));
    expect(await screen.findByTestId("devices-register-blocked")).toBeVisible();
    expect(screen.getByTestId("devices-register-browser")).toBeDisabled();
    expect(
      within(screen.getByTestId("devices-capacity-limit")).getByText(/Device limit reached/i),
    ).toBeVisible();
  });
});
