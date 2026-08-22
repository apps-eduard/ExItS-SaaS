import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { getLocalValidationEnabled, listQuickLoginIdentities } from "@/api/auth/auth-client";
import { DevelopmentTestUserTools } from "@/features/auth/DevelopmentTestUserTools";
import { PreferencesProvider } from "@/hooks/use-preferences";
import { areTestUserToolsPermitted } from "@/lib/auth/development-tools";

vi.mock("@/api/auth/auth-client", () => ({
  getLocalValidationEnabled: vi.fn(),
  listQuickLoginIdentities: vi.fn(),
}));

vi.mock("@/lib/auth/development-tools", () => ({
  areTestUserToolsPermitted: vi.fn(),
  areDevelopmentToolsAllowed: vi.fn(),
}));

const olivia = {
  key: "olivia",
  username: "olivia",
  displayName: "Olivia Mendoza",
  email: "olivia.mendoza@exits.local",
  listLabel: "Olivia Mendoza",
};

function renderTools(onSelectLogin: (loginId: string) => void = () => undefined) {
  return render(
    <PreferencesProvider>
      <DevelopmentTestUserTools onSelectLogin={onSelectLogin} />
    </PreferencesProvider>,
  );
}

describe("DevelopmentTestUserTools", () => {
  afterEach(() => {
    vi.clearAllMocks();
    delete window.__EXITS_PLATFORM_ADMIN_WEB__;
  });

  it("renders nothing and makes no Local Validation API calls when the frontend gate is closed", () => {
    vi.mocked(areTestUserToolsPermitted).mockReturnValue(false);
    renderTools();

    expect(screen.queryByTestId("dev-test-user-tools")).not.toBeInTheDocument();
    expect(getLocalValidationEnabled).not.toHaveBeenCalled();
    expect(listQuickLoginIdentities).not.toHaveBeenCalled();
  });

  it("keeps the section visible with an empty state when Local Validation is disabled", async () => {
    vi.mocked(areTestUserToolsPermitted).mockReturnValue(true);
    vi.mocked(getLocalValidationEnabled).mockResolvedValue(false);
    renderTools();

    expect(await screen.findByText("Development Test User")).toBeInTheDocument();
    expect(screen.getByText("No test users are available.")).toBeInTheDocument();
    expect(listQuickLoginIdentities).not.toHaveBeenCalled();
    expect(screen.queryByLabelText(/test user/i)).not.toBeInTheDocument();
  });

  it("keeps the section visible and shows ErrorState when identity discovery fails", async () => {
    vi.mocked(areTestUserToolsPermitted).mockReturnValue(true);
    vi.mocked(getLocalValidationEnabled).mockRejectedValue(new TypeError("Failed to fetch"));
    renderTools();

    expect(await screen.findByText("Development Test User")).toBeInTheDocument();
    expect(screen.getByText("Unable to load development test users.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /retry/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /copy/i })).toBeInTheDocument();
    expect(listQuickLoginIdentities).not.toHaveBeenCalled();
  });

  it("retries identity discovery after a failure", async () => {
    vi.mocked(areTestUserToolsPermitted).mockReturnValue(true);
    vi.mocked(getLocalValidationEnabled)
      .mockRejectedValueOnce(new TypeError("Failed to fetch"))
      .mockResolvedValueOnce(true);
    vi.mocked(listQuickLoginIdentities).mockResolvedValue([olivia]);
    const user = userEvent.setup();
    renderTools();

    await screen.findByRole("button", { name: /retry/i });
    await user.click(screen.getByRole("button", { name: /retry/i }));

    expect(await screen.findByLabelText(/test user/i)).toBeInTheDocument();
    expect(getLocalValidationEnabled).toHaveBeenCalledTimes(2);
    expect(listQuickLoginIdentities).toHaveBeenCalledOnce();
  });

  it("keeps the section visible when the identity list is empty", async () => {
    vi.mocked(areTestUserToolsPermitted).mockReturnValue(true);
    vi.mocked(getLocalValidationEnabled).mockResolvedValue(true);
    vi.mocked(listQuickLoginIdentities).mockResolvedValue([]);
    renderTools();

    expect(await screen.findByText("No test users are available.")).toBeInTheDocument();
    expect(screen.getByTestId("dev-test-user-tools")).toBeInTheDocument();
    expect(screen.queryByLabelText(/test user/i)).not.toBeInTheDocument();
  });

  it("shows the selector when frontend tools and backend Local Validation are both enabled", async () => {
    vi.mocked(areTestUserToolsPermitted).mockReturnValue(true);
    vi.mocked(getLocalValidationEnabled).mockResolvedValue(true);
    vi.mocked(listQuickLoginIdentities).mockResolvedValue([olivia]);

    renderTools();

    expect(await screen.findByLabelText(/test user/i)).toBeInTheDocument();
    expect(screen.queryByText("No test users are available.")).not.toBeInTheDocument();
    expect(getLocalValidationEnabled).toHaveBeenCalledOnce();
    expect(listQuickLoginIdentities).toHaveBeenCalledOnce();
  });

  it("shows a loading status before identities resolve", async () => {
    let resolveEnabled: (value: boolean) => void = () => undefined;
    vi.mocked(areTestUserToolsPermitted).mockReturnValue(true);
    vi.mocked(getLocalValidationEnabled).mockImplementation(
      () =>
        new Promise<boolean>((resolve) => {
          resolveEnabled = resolve;
        }),
    );
    renderTools();

    expect(screen.getByText("Loading test users...")).toBeInTheDocument();
    resolveEnabled(true);
    vi.mocked(listQuickLoginIdentities).mockResolvedValue([olivia]);
    await waitFor(() => {
      expect(listQuickLoginIdentities).toHaveBeenCalled();
    });
  });

  it("selecting an identity reports the login id and does not include a password", async () => {
    vi.mocked(areTestUserToolsPermitted).mockReturnValue(true);
    vi.mocked(getLocalValidationEnabled).mockResolvedValue(true);
    vi.mocked(listQuickLoginIdentities).mockResolvedValue([olivia]);
    const onSelectLogin = vi.fn();
    const user = userEvent.setup();

    renderTools(onSelectLogin);
    await screen.findByLabelText(/test user/i);
    await user.selectOptions(screen.getByLabelText(/test user/i), "olivia");

    expect(onSelectLogin).toHaveBeenCalledExactlyOnceWith("olivia.mendoza@exits.local");
    expect(JSON.stringify(onSelectLogin.mock.calls)).not.toMatch(/password/i);
    expect(JSON.stringify(olivia)).not.toMatch(/password/i);
  });

  it("drops identities that include a password field", async () => {
    vi.mocked(areTestUserToolsPermitted).mockReturnValue(true);
    vi.mocked(getLocalValidationEnabled).mockResolvedValue(true);
    vi.mocked(listQuickLoginIdentities).mockResolvedValue([
      { ...olivia, password: "should-not-render" } as typeof olivia & { password: string },
    ]);
    renderTools();

    expect(await screen.findByText("No test users are available.")).toBeInTheDocument();
    expect(screen.queryByText("should-not-render")).not.toBeInTheDocument();
    expect(screen.queryByLabelText(/test user/i)).not.toBeInTheDocument();
  });
});
