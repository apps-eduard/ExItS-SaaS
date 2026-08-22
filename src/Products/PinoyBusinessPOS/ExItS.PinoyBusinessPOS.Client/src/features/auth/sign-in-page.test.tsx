import "fake-indexeddb/auto";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { SignInPage } from "@/features/auth/SignInPage";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";
import { SessionProvider } from "@/session/SessionProvider";

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
    canOfferPinUnlock: false,
    grantExpired: false,
    noEnrollment: true,
  })),
}));

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

  it("exposes accessible social and PIN action labels", () => {
    renderSignInPage();
    expect(screen.getByRole("button", { name: "Continue with Facebook" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Continue with Google" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Use Offline PIN" })).toBeInTheDocument();
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

  it("keeps alternate sign-in buttons enabled when providers are unconfigured", () => {
    renderSignInPage();
    expect(screen.getByTestId("auth-facebook-button")).toBeEnabled();
    expect(screen.getByTestId("auth-google-button")).toBeEnabled();
    expect(screen.getByTestId("auth-pin-button")).toBeEnabled();
  });

  it("shows provider unavailable feedback without disabling social buttons", async () => {
    const user = userEvent.setup();
    renderSignInPage();
    await user.click(screen.getByTestId("auth-google-button"));
    expect(screen.getByTestId("auth-error")).toHaveTextContent(/not configured/i);
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
