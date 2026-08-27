import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  activatePersonalAccount,
  resetPasswordWithToken,
} from "@/api/platform/platform-auth-client";
import { ActivateAccountPage } from "@/features/auth/ActivateAccountPage";
import { ResetPasswordPage } from "@/features/auth/ResetPasswordPage";
import { SignInPage } from "@/features/auth/SignInPage";
import {
  assertStorageHasNoAuthToken,
  captureEmailCallbackToken,
  scrubTokenFromBrowserLocation,
} from "@/features/auth/callback-token";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";
import { SessionProvider } from "@/session/SessionProvider";

vi.mock("@/api/platform/platform-auth-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/platform/platform-auth-client")>();
  return {
    ...actual,
    activatePersonalAccount: vi.fn(),
    resetPasswordWithToken: vi.fn(),
    probeExternalAuthProvider: vi.fn(async () => "disabled" as const),
  };
});

vi.mock("@/offline/offline-pin-login-offer", () => ({
  evaluateOfflinePinLoginOffer: vi.fn(async () => ({
    canOfferPinUnlock: false,
    grantExpired: false,
    noEnrollment: true,
  })),
}));

vi.mock("@/session/SessionProvider", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/session/SessionProvider")>();
  return {
    ...actual,
    useSession: () => ({
      signIn: vi.fn(async () => ({ ok: true as const })),
      status: "unauthenticated" as const,
      coldStartDenial: null,
    }),
  };
});

function renderRoute(route: string) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <SessionProvider>
            <MemoryRouter initialEntries={[route]}>
              <Routes>
                <Route path="/sign-in" element={<SignInPage />} />
                <Route path="/activate-account" element={<ActivateAccountPage />} />
                <Route path="/reset-password" element={<ResetPasswordPage />} />
              </Routes>
            </MemoryRouter>
          </SessionProvider>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("PERS-AUTH-01 account lifecycle", () => {
  beforeEach(() => {
    window.localStorage.clear();
    window.sessionStorage.clear();
    vi.mocked(activatePersonalAccount).mockReset();
    vi.mocked(resetPasswordWithToken).mockReset();
    Object.defineProperty(window.navigator, "onLine", { configurable: true, value: true });
  });

  it("captures token from query and does not leave it in helpers after scrub", () => {
    const token = captureEmailCallbackToken("?token=secret-token-value");
    expect(token).toBe("secret-token-value");
    window.history.replaceState({}, "", "/activate-account?token=secret-token-value");
    scrubTokenFromBrowserLocation("/activate-account");
    expect(window.location.pathname).toBe("/activate-account");
    expect(window.location.search).toBe("");
    assertStorageHasNoAuthToken(window.localStorage);
    assertStorageHasNoAuthToken(window.sessionStorage);
  });

  it("shows missing-token state for activation", () => {
    renderRoute("/activate-account");
    expect(screen.getByTestId("activate-account-missing-token")).toBeInTheDocument();
    expect(screen.getByText(/missing or incomplete/i)).toBeInTheDocument();
  });

  it("scrubs activation token from URL and activates successfully", async () => {
    const user = userEvent.setup();
    vi.mocked(activatePersonalAccount).mockResolvedValue({ ok: true });
    window.history.replaceState({}, "", "/activate-account?token=act-token-1");
    renderRoute("/activate-account?token=act-token-1");

    await waitFor(() => {
      expect(window.location.search).not.toContain("token=");
    });
    expect(screen.getByTestId("activate-account-page")).toBeInTheDocument();

    await user.type(screen.getByLabelText(/new password/i), "ValidPass1!");
    await user.type(screen.getByLabelText(/confirm password/i), "ValidPass1!");
    await user.click(screen.getByRole("button", { name: /activate account/i }));

    await waitFor(() => {
      expect(activatePersonalAccount).toHaveBeenCalledWith("act-token-1", "ValidPass1!");
    });
    await waitFor(() => {
      expect(screen.getByText(/account is active/i)).toBeInTheDocument();
    });
    assertStorageHasNoAuthToken(window.localStorage);
    assertStorageHasNoAuthToken(window.sessionStorage);
  });

  it("maps expired activation token safely", async () => {
    const user = userEvent.setup();
    vi.mocked(activatePersonalAccount).mockResolvedValue({
      ok: false,
      status: 401,
      body: { errorCode: "application.auth.credential_token_expired" },
    });
    renderRoute("/activate-account?token=expired-token");

    await user.type(screen.getByLabelText(/new password/i), "ValidPass1!");
    await user.type(screen.getByLabelText(/confirm password/i), "ValidPass1!");
    await user.click(screen.getByRole("button", { name: /activate account/i }));

    await waitFor(() => {
      expect(screen.getByTestId("activate-account-error")).toHaveTextContent(/expired/i);
    });
  });

  it("prevents activation double-submit while pending", async () => {
    const user = userEvent.setup();
    let resolveActivation: (value: { ok: true }) => void = () => {};
    vi.mocked(activatePersonalAccount).mockImplementation(
      () =>
        new Promise<{ ok: true }>((resolve) => {
          resolveActivation = resolve;
        }),
    );
    renderRoute("/activate-account?token=pending-token");

    await user.type(screen.getByLabelText(/new password/i), "ValidPass1!");
    await user.type(screen.getByLabelText(/confirm password/i), "ValidPass1!");
    const submit = screen.getByRole("button", { name: /activate account/i });
    await user.click(submit);
    await waitFor(() => expect(submit).toBeDisabled());
    await user.click(submit);
    expect(activatePersonalAccount).toHaveBeenCalledTimes(1);
    resolveActivation({ ok: true });
  });

  it("shows missing-token state for reset", () => {
    renderRoute("/reset-password");
    expect(screen.getByTestId("reset-password-missing-token")).toBeInTheDocument();
  });

  it("resets password with scrubbed token and returns to sign-in", async () => {
    const user = userEvent.setup();
    vi.mocked(resetPasswordWithToken).mockResolvedValue({ ok: true });
    renderRoute("/reset-password?token=reset-token-1");

    await waitFor(() => {
      expect(window.location.search).not.toContain("token=");
    });

    await user.type(screen.getByLabelText(/new password/i), "ValidPass1!");
    await user.type(screen.getByLabelText(/confirm password/i), "ValidPass1!");
    await user.click(screen.getByRole("button", { name: /reset password/i }));

    await waitFor(() => {
      expect(resetPasswordWithToken).toHaveBeenCalledWith("reset-token-1", "ValidPass1!");
    });
    await waitFor(() => {
      expect(screen.getByText(/password was updated/i)).toBeInTheDocument();
    });
  });

  it("rejects mismatched confirm password without calling API", async () => {
    const user = userEvent.setup();
    renderRoute("/reset-password?token=reset-token-2");
    await user.type(screen.getByLabelText(/new password/i), "ValidPass1!");
    await user.type(screen.getByLabelText(/confirm password/i), "Different1!");
    await user.click(screen.getByRole("button", { name: /reset password/i }));
    expect(await screen.findByText(/must match/i)).toBeInTheDocument();
    expect(resetPasswordWithToken).not.toHaveBeenCalled();
  });
});
