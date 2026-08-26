import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ProductAccessProvider } from "@/access/ProductAccessProvider";
import { RequireProductAccess } from "@/access/RequireProductAccess";
import { AppProviders } from "@/app/providers";
import { ActivateAccountPage } from "@/features/auth/ActivateAccountPage";
import { ForgotPasswordPage } from "@/features/auth/ForgotPasswordPage";
import { ResetPasswordPage } from "@/features/auth/ResetPasswordPage";
import { SignUpPage } from "@/features/auth/SignUpPage";
import {
  assertStorageHasNoAuthToken,
  captureEmailCallbackToken,
} from "@/features/auth/callback-token";
import { HomePage } from "@/features/home/HomePage";
import { SignInPage } from "@/features/sign-in/SignInPage";
import { AppShell } from "@/layouts/AppShell";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";
import { GuestOnly, RequireSession } from "@/session/SessionGuards";
import { SessionProvider } from "@/session/SessionProvider";
import { stubAccessFetch } from "@/test/access-mocks";
import { jsonResponse } from "@/test/render";
import { clearPlatformAntiforgeryToken } from "@/api/platform-auth/platform-antiforgery";

function mockUnauthenticated() {
  vi.stubGlobal("fetch", (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    if (url.includes("/antiforgery/token")) {
      return jsonResponse(200, {
        headerName: "X-XSRF-TOKEN",
        token: "test-csrf-token",
      });
    }
    if (url.includes("/auth/me")) {
      return jsonResponse(401, { errorCode: "application.auth.session_invalid" });
    }
    if (url.includes("/local-validation/enabled")) {
      return jsonResponse(200, false);
    }
    if (url.includes("/auth/register")) {
      const posted = JSON.parse(String(init?.body)) as Record<string, string>;
      expect(posted.publicSurface).toBe("pinoy-loan-manager");
      expect(posted).not.toHaveProperty("callbackUrl");
      expect(posted).not.toHaveProperty("redirectUrl");
      expect(posted).not.toHaveProperty("returnUrl");
      return jsonResponse(409, { errorCode: "application.auth.email_conflict" });
    }
    if (url.includes("/auth/forgot-password")) {
      const posted = JSON.parse(String(init?.body)) as Record<string, string>;
      expect(posted.publicSurface).toBe("pinoy-loan-manager");
      return jsonResponse(200, {
        message: "If an eligible account exists, a password reset token was issued.",
      });
    }
    if (url.includes("/auth/activate-account")) {
      expect(new Headers(init?.headers).get("X-XSRF-TOKEN")).toBe("test-csrf-token");
      return jsonResponse(200, { hasPassword: true });
    }
    if (url.includes("/auth/reset-password")) {
      return jsonResponse(200, { hasPassword: true });
    }
    return jsonResponse(404, null);
  });
}

function renderAuth(route: string) {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={[route]}>
        <SessionProvider>
          <Routes>
            <Route
              path="/sign-in"
              element={
                <GuestOnly>
                  <SignInPage />
                </GuestOnly>
              }
            />
            <Route
              path="/sign-up"
              element={
                <GuestOnly>
                  <SignUpPage />
                </GuestOnly>
              }
            />
            <Route
              path="/forgot-password"
              element={
                <GuestOnly>
                  <ForgotPasswordPage />
                </GuestOnly>
              }
            />
            <Route path="/activate-account" element={<ActivateAccountPage />} />
            <Route path="/reset-password" element={<ResetPasswordPage />} />
            <Route
              path="/"
              element={
                <RequireSession>
                  <ProductAccessProvider>
                    <RequireProductAccess>
                      <AppShell />
                    </RequireProductAccess>
                  </ProductAccessProvider>
                </RequireSession>
              }
            >
              <Route index element={<HomePage />} />
            </Route>
          </Routes>
        </SessionProvider>
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("account lifecycle", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
    clearPlatformAntiforgeryToken();
  });

  it("registers without enumerating accounts and without creating product access copy", async () => {
    const user = userEvent.setup();
    const log = vi.spyOn(console, "log").mockImplementation(() => undefined);
    mockUnauthenticated();
    renderAuth("/sign-up");
    expect(await screen.findByRole("heading", { name: "Create account" })).toBeInTheDocument();
    await user.type(screen.getByLabelText("Display name"), "Pat Lender");
    await user.type(screen.getByLabelText("Email"), "pat@example.com");
    await user.click(screen.getByRole("button", { name: "Create account" }));
    expect(await screen.findByText("Check your email to continue.")).toBeInTheDocument();
    expect(document.body.textContent).not.toMatch(
      /already exists|conflict|borrower|loan product|organization chooser/i,
    );
    expect(log.mock.calls.flat().join(" ")).not.toMatch(/token=/i);
  });

  it("shows generic forgot-password acknowledgement", async () => {
    const user = userEvent.setup();
    mockUnauthenticated();
    renderAuth("/forgot-password");
    await screen.findByRole("heading", { name: "Forgot password" });
    await user.type(screen.getByLabelText("Username or email"), "pat@example.com");
    await user.click(screen.getByRole("button", { name: "Send reset email" }));
    expect(
      await screen.findByText(
        "If an eligible account exists, a password reset email has been sent.",
      ),
    ).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Back to Sign In" })).toHaveAttribute(
      "href",
      "/sign-in",
    );
  });

  it("shows a missing activation token state", async () => {
    mockUnauthenticated();
    renderAuth("/activate-account");
    expect(await screen.findByText("Activation link is invalid or missing.")).toBeInTheDocument();
  });

  it("activates, matches passwords, scrubs the token, and does not persist it", async () => {
    const user = userEvent.setup();
    const log = vi.spyOn(console, "log").mockImplementation(() => undefined);
    const historySpy = vi.spyOn(window.history, "replaceState");
    mockUnauthenticated();
    renderAuth("/activate-account?token=one-time-handoff");
    expect(captureEmailCallbackToken("?token=one-time-handoff")).toBe("one-time-handoff");
    await screen.findByRole("heading", { name: "Activate account" });
    await user.type(screen.getByLabelText("New password"), "secret");
    await user.type(screen.getByLabelText("Confirm password"), "other");
    await user.click(screen.getByRole("button", { name: "Activate account" }));
    expect(await screen.findByText("Passwords must match.")).toBeInTheDocument();
    await user.clear(screen.getByLabelText("Confirm password"));
    await user.type(screen.getByLabelText("Confirm password"), "secret");
    await user.click(screen.getByRole("button", { name: "Activate account" }));
    expect(
      await screen.findByText("Account activated. Sign in with your password."),
    ).toBeInTheDocument();
    expect(historySpy).toHaveBeenCalled();
    expect(historySpy.mock.calls.some((call) => String(call[2] ?? "").includes("token="))).toBe(
      false,
    );
    assertStorageHasNoAuthToken(window.localStorage);
    assertStorageHasNoAuthToken(window.sessionStorage);
    expect(log.mock.calls.flat().join(" ")).not.toMatch(/one-time-handoff/);
    expect(document.body.textContent).not.toMatch(/one-time-handoff/);
  });

  it("resets password and shows the sign-in notice", async () => {
    const user = userEvent.setup();
    mockUnauthenticated();
    renderAuth("/reset-password?token=reset-handoff");
    await screen.findByRole("heading", { name: "Reset password" });
    await user.type(screen.getByLabelText("New password"), "secret");
    await user.type(screen.getByLabelText("Confirm password"), "secret");
    await user.click(screen.getByRole("button", { name: "Reset password" }));
    expect(
      await screen.findByText("Password reset. Sign in with your new password."),
    ).toBeInTheDocument();
  });

  it("shows missing reset token state", async () => {
    mockUnauthenticated();
    renderAuth("/reset-password");
    expect(await screen.findByText("Reset link is invalid or missing.")).toBeInTheDocument();
  });

  it("redirects authenticated users away from sign-up and forgot-password", async () => {
    stubAccessFetch({ displayName: "Olivia Mendoza", username: "olivia" });
    renderAuth("/sign-up");
    expect(await screen.findByRole("heading", { name: "Pinoy Loan Manager" })).toBeInTheDocument();
    renderAuth("/forgot-password");
    expect(await screen.findByRole("heading", { name: "Pinoy Loan Manager" })).toBeInTheDocument();
  });

  it("keeps Filipino copy and theme controls on Sign Up", async () => {
    const user = userEvent.setup();
    mockUnauthenticated();
    renderAuth("/sign-up");
    await screen.findByRole("heading", { name: "Create account" });
    await user.click(screen.getByRole("radio", { name: "Filipino" }));
    expect(screen.getByRole("heading", { name: "Gumawa ng account" })).toBeInTheDocument();
    await user.click(screen.getByRole("radio", { name: "Dark" }));
    expect(document.documentElement.dataset.theme).toBe("dark");
    expect(window.localStorage.getItem(UI_PREFERENCES_STORAGE_KEY)).toMatch(/fil-PH/);
  });

  it("exposes real Sign In recovery links", async () => {
    mockUnauthenticated();
    renderAuth("/sign-in");
    await screen.findByRole("heading", { name: "Sign In" });
    expect(screen.getAllByRole("link", { name: "Forgot password" })[0]).toHaveAttribute(
      "href",
      "/forgot-password",
    );
    expect(screen.getAllByRole("link", { name: "Create account" })[0]).toHaveAttribute(
      "href",
      "/sign-up",
    );
    expect(screen.queryByText("Sign in trouble?")).not.toBeInTheDocument();
  });
});
