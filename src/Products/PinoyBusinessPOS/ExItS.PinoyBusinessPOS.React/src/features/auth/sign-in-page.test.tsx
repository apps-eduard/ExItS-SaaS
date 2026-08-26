import "fake-indexeddb/auto";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { SignInPage } from "@/features/auth/SignInPage";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";
import { SessionProvider } from "@/session/SessionProvider";
import { probeExternalAuthProvider } from "@/api/platform/platform-auth-client";

vi.mock("@/api/platform/platform-auth-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/platform/platform-auth-client")>();
  return {
    ...actual,
    probeExternalAuthProvider: vi.fn(async () => "disabled" as const),
    registerPersonalAccount: vi.fn(async () => ({ ok: true as const })),
  };
});

vi.mock("@/offline/offline-pin-login-offer", () => ({
  evaluateOfflinePinLoginOffer: vi.fn(async () => ({
    canOfferPinUnlock: true,
    grantExpired: false,
    noEnrollment: false,
  })),
}));

const signInMock = vi.fn<() => Promise<{ ok: true } | { ok: false; failure: Record<string, unknown> }>>(
  async () => ({ ok: true }),
);

vi.mock("@/session/SessionProvider", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/session/SessionProvider")>();
  return {
    ...actual,
    useSession: () => ({
      signIn: signInMock,
      status: "unauthenticated" as const,
      coldStartDenial: null,
    }),
  };
});

function renderSignInPage() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <SessionProvider>
            <MemoryRouter>
              <SignInPage />
            </MemoryRouter>
          </SessionProvider>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("SignInPage LOGIN-UX-01", () => {
  beforeEach(() => {
    window.localStorage.clear();
    Object.defineProperty(window.navigator, "onLine", { configurable: true, value: true });
    signInMock.mockClear();
    vi.mocked(probeExternalAuthProvider).mockClear();
  });

  it("defaults to Sign In tab with active indicator", () => {
    renderSignInPage();
    expect(screen.getByTestId("auth-tab-sign-in")).toHaveAttribute("aria-selected", "true");
    expect(screen.getByTestId("auth-tab-sign-up")).toHaveAttribute("aria-selected", "false");
  });

  it("switches to Sign Up tab", async () => {
    const user = userEvent.setup();
    renderSignInPage();
    await user.click(screen.getByTestId("auth-tab-sign-up"));
    expect(screen.getByTestId("auth-tab-sign-up")).toHaveAttribute("aria-selected", "true");
    expect(screen.getByRole("button", { name: "Create Personal account" })).toBeInTheDocument();
  });

  it("toggles password visibility without clearing value", async () => {
    const user = userEvent.setup();
    renderSignInPage();
    const password = screen.getByLabelText("Password");
    await user.type(password, "secret123");
    await user.click(screen.getByRole("button", { name: "Toggle visibility" }));
    expect(password).toHaveAttribute("type", "text");
    expect(password).toHaveValue("secret123");
  });

  it("holds Google and Facebook login UI while keeping offline PIN alternative", () => {
    renderSignInPage();
    expect(screen.queryByRole("button", { name: "Continue with Facebook" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Continue with Google" })).not.toBeInTheDocument();
    expect(screen.queryByTestId("auth-facebook-button")).not.toBeInTheDocument();
    expect(screen.queryByTestId("auth-google-button")).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Use Offline PIN" })).toBeInTheDocument();
  });

  it("does not probe external auth providers on sign-in load", async () => {
    renderSignInPage();
    await waitFor(() => {
      expect(probeExternalAuthProvider).not.toHaveBeenCalled();
    });
  });

  it("submits username and password sign-in", async () => {
    const user = userEvent.setup();
    renderSignInPage();
    await user.type(screen.getByLabelText("Email or staff login"), "owner@example.com");
    await user.type(screen.getByLabelText("Password"), "secret123");
    await user.click(screen.getByTestId("sign-in-submit"));
    await waitFor(() => {
      expect(signInMock).toHaveBeenCalledWith("owner@example.com", "secret123");
    });
  });

  it("shows sign-in ErrorState with diagnostics instead of generic-only failure", async () => {
    signInMock.mockResolvedValueOnce({
      ok: false,
      failure: {
        failureStage: "platform.auth.login",
        httpMethod: "POST",
        path: "/api/v1/platform/auth/login",
        status: 502,
        errorCode: "application.upstream_error",
        detail: "Platform API gateway timeout.",
        traceId: "trace-sign-in-502",
        requestCorrelationId: "corr-sign-in-502",
      },
    });
    const user = userEvent.setup();
    renderSignInPage();
    await user.type(screen.getByLabelText("Email or staff login"), "kizy@gmail.com");
    await user.type(screen.getByLabelText("Password"), "1");
    await user.click(screen.getByTestId("sign-in-submit"));
    await waitFor(() => {
      expect(screen.getByTestId("error-state")).toBeInTheDocument();
    });
    expect(screen.getByText("Sign in failed.")).toBeInTheDocument();
    expect(screen.getByText("Platform API gateway timeout.")).toBeInTheDocument();
    expect(screen.getByTestId("copy-error-details")).toBeInTheDocument();
    expect(screen.queryByText(/check your credentials/i)).not.toBeInTheDocument();
  });

  it("shows inline sign-in error instead of crashing when login fails with server error", async () => {
    signInMock.mockResolvedValueOnce({
      ok: false,
      failure: {
        failureStage: "platform.auth.login",
        httpMethod: "POST",
        path: "/api/v1/platform/auth/login",
        status: 401,
        errorCode: "application.auth.login_failed",
        detail: "Invalid username or password.",
      },
    });
    const user = userEvent.setup();
    renderSignInPage();
    await user.type(screen.getByLabelText("Email or staff login"), "owner@example.com");
    await user.type(screen.getByLabelText("Password"), "secret123");
    await user.click(screen.getByTestId("sign-in-submit"));
    await waitFor(() => {
      expect(screen.getByTestId("error-state")).toBeInTheDocument();
    });
    expect(screen.getByTestId("copy-error-details")).toBeInTheDocument();
    expect(screen.queryByText(/check your credentials/i)).not.toBeInTheDocument();
    expect(screen.getByTestId("sign-in-page")).toBeInTheDocument();
  });

  it("routes to offline PIN unlock from the alternate sign-in action", async () => {
    const user = userEvent.setup();
    render(
      <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
        <PreferencesProvider>
          <I18nProvider>
            <SessionProvider>
              <MemoryRouter initialEntries={["/sign-in"]}>
                <Routes>
                  <Route path="/sign-in" element={<SignInPage />} />
                  <Route path="/offline-pin" element={<div data-testid="offline-pin-page" />} />
                </Routes>
              </MemoryRouter>
            </SessionProvider>
          </I18nProvider>
        </PreferencesProvider>
      </QueryClientProvider>,
    );
    await user.click(screen.getByTestId("auth-pin-button"));
    expect(screen.getByTestId("offline-pin-page")).toBeInTheDocument();
  });

  it("toggles username help from the info icon", async () => {
    const user = userEvent.setup();
    renderSignInPage();
    expect(screen.queryByTestId("sign-in-username-hint")).not.toBeInTheDocument();
    await user.click(screen.getByTestId("sign-in-username-hint-toggle"));
    expect(screen.getByTestId("sign-in-username-hint")).toBeInTheDocument();
    await user.click(screen.getByTestId("sign-in-username-hint-toggle"));
    expect(screen.queryByTestId("sign-in-username-hint")).not.toBeInTheDocument();
  });

  it("shows staff login hint for org-scoped aliases", async () => {
    const user = userEvent.setup();
    renderSignInPage();
    await user.type(screen.getByLabelText("Email or staff login"), "cashier@ORG123456");
    expect(screen.getByTestId("staff-login-hint")).toBeInTheDocument();
  });

  it("does not persist plaintext password in localStorage", async () => {
    const user = userEvent.setup();
    renderSignInPage();
    await user.type(screen.getByLabelText("Password"), "secret123");
    await user.click(screen.getByRole("checkbox", { name: "Remember Me" }));
    await user.click(screen.getByTestId("sign-in-submit"));
    expect(JSON.stringify(window.localStorage)).not.toContain("secret123");
  });

  it("keeps offline PIN action enabled when PIN unlock is offered", () => {
    renderSignInPage();
    expect(screen.getByTestId("auth-pin-button")).toBeEnabled();
  });

  it("blocks password submit while offline", async () => {
    Object.defineProperty(window.navigator, "onLine", { configurable: true, value: false });
    renderSignInPage();
    fireEvent(window, new Event("offline"));
    expect(screen.getByTestId("sign-in-offline-banner")).toBeInTheDocument();
    expect(screen.getByTestId("sign-in-submit")).toBeDisabled();
  });

  it("does not render dev test user selector in production builds", () => {
    vi.stubEnv("MODE", "production");
    renderSignInPage();
    expect(screen.queryByText(/Development Test User/i)).not.toBeInTheDocument();
    vi.unstubAllEnvs();
  });

  it("renders MAUI-inspired hero branding", () => {
    renderSignInPage();
    expect(screen.getByText("Expert IT Solutions")).toBeInTheDocument();
    expect(screen.getByText("Pinoy Business POS")).toBeInTheDocument();
    expect(screen.getByTestId("auth-experience-hero")).toBeInTheDocument();
    expect(screen.getByTestId("auth-experience-sheet")).toBeInTheDocument();
  });
});
