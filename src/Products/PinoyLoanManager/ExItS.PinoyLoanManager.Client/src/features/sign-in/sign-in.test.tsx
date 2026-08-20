import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ProductAccessProvider } from "@/access/ProductAccessProvider";
import { RequireProductAccess } from "@/access/RequireProductAccess";
import { AppProviders } from "@/app/providers";
import { HomePage } from "@/features/home/HomePage";
import { SignInPage } from "@/features/sign-in/SignInPage";
import { AppShell } from "@/layouts/AppShell";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";
import { GuestOnly, RequireSession } from "@/session/SessionGuards";
import { SessionProvider } from "@/session/SessionProvider";
import { stubAccessFetch } from "@/test/access-mocks";
import { jsonResponse } from "@/test/render";

function mockUnauthenticated() {
  vi.stubGlobal("fetch", (input: RequestInfo | URL) => {
    const url = String(input);
    if (url.includes("/auth/me")) {
      return jsonResponse(401, { errorCode: "application.auth.session_invalid" });
    }
    if (url.includes("/local-validation/enabled")) {
      return jsonResponse(200, false);
    }
    return jsonResponse(404, null);
  });
}

function renderRoutes(route: string) {
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

describe("sign-in session UX", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("redirects unauthenticated users to /sign-in", async () => {
    mockUnauthenticated();
    renderRoutes("/");
    expect(await screen.findByRole("heading", { name: "Sign In" })).toBeInTheDocument();
  });

  it("bootstraps an authenticated cookie session onto the landing", async () => {
    stubAccessFetch({ displayName: "Olivia Mendoza", username: "olivia" });
    renderRoutes("/");
    expect(await screen.findByRole("heading", { name: "Pinoy Loan Manager" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Olivia Mendoza" })).toBeInTheDocument();
    expect(document.body.textContent).not.toMatch(/1,250.00|borrower|Synced/i);
  });

  it("shows a generic invalid credential message", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("fetch", (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.includes("/auth/me")) {
        return jsonResponse(401, { errorCode: "application.auth.session_invalid" });
      }
      if (url.includes("/local-validation/enabled")) {
        return jsonResponse(200, false);
      }
      if (url.includes("/auth/login")) {
        expect(init?.body).not.toMatch(/sessionToken/i);
        return jsonResponse(401, { errorCode: "application.auth.login_failed" });
      }
      return jsonResponse(404, null);
    });
    renderRoutes("/sign-in");
    await screen.findByRole("heading", { name: "Sign In" });
    await user.type(screen.getByLabelText("Username or email"), "olivia");
    await user.type(screen.getByLabelText("Password"), "wrong");
    await user.click(screen.getByRole("button", { name: "Sign in" }));
    expect(
      await screen.findByText("Sign in failed. Check your username and password."),
    ).toBeInTheDocument();
  });

  it("toggles password visibility and submits on Enter", async () => {
    const user = userEvent.setup();
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/auth/me")) {
        return jsonResponse(401, { errorCode: "application.auth.session_invalid" });
      }
      if (url.includes("/local-validation/enabled")) {
        return jsonResponse(200, false);
      }
      if (url.includes("/auth/login")) {
        return jsonResponse(200, {
          username: "olivia",
          displayName: "Olivia Mendoza",
          accountClass: "Organization",
          selectedOrganizationId: "11111111-1111-4111-8111-111111111111",
        });
      }
      if (url.includes("/auth/organizations")) {
        return jsonResponse(200, [
          {
            organizationId: "11111111-1111-4111-8111-111111111111",
            displayName: "ABC Sari-Sari Store",
            slug: "abc-sari-sari",
          },
        ]);
      }
      if (url.includes("/auth/product-access/effective")) {
        return jsonResponse(200, {
          allowed: true,
          reasonCode: "allowed",
          productCode: "pinoy-loan-manager",
        });
      }
      return jsonResponse(404, null);
    });
    vi.stubGlobal("fetch", fetchMock);
    renderRoutes("/sign-in");
    await screen.findByRole("heading", { name: "Sign In" });
    const password = screen.getByLabelText("Password");
    expect(password).toHaveAttribute("type", "password");
    await user.click(screen.getByRole("button", { name: "Show password" }));
    expect(password).toHaveAttribute("type", "text");
    await user.type(screen.getByLabelText("Username or email"), "olivia");
    await user.type(password, "secret{Enter}");
    expect(await screen.findByRole("heading", { name: "Pinoy Loan Manager" })).toBeInTheDocument();
    expect(fetchMock.mock.calls.some((call) => String(call[0]).includes("/auth/login"))).toBe(true);
  });

  it("prevents duplicate login submits", async () => {
    const user = userEvent.setup();
    let loginCalls = 0;
    vi.stubGlobal("fetch", (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/auth/me")) {
        return jsonResponse(401, { errorCode: "application.auth.session_invalid" });
      }
      if (url.includes("/local-validation/enabled")) {
        return jsonResponse(200, false);
      }
      if (url.includes("/auth/login")) {
        loginCalls += 1;
        return jsonResponse(
          200,
          {
            username: "olivia",
            displayName: "Olivia Mendoza",
            accountClass: "Organization",
            selectedOrganizationId: "11111111-1111-4111-8111-111111111111",
          },
          80,
        );
      }
      if (url.includes("/auth/organizations")) {
        return jsonResponse(200, [
          {
            organizationId: "11111111-1111-4111-8111-111111111111",
            displayName: "ABC Sari-Sari Store",
            slug: "abc-sari-sari",
          },
        ]);
      }
      if (url.includes("/auth/product-access/effective")) {
        return jsonResponse(200, {
          allowed: true,
          reasonCode: "allowed",
          productCode: "pinoy-loan-manager",
        });
      }
      return jsonResponse(404, null);
    });
    renderRoutes("/sign-in");
    await screen.findByRole("heading", { name: "Sign In" });
    await user.type(screen.getByLabelText("Username or email"), "olivia");
    await user.type(screen.getByLabelText("Password"), "secret");
    const submit = screen.getByRole("button", { name: "Sign in" });
    await user.click(submit);
    await user.click(submit);
    await screen.findByRole("heading", { name: "Pinoy Loan Manager" });
    expect(loginCalls).toBe(1);
  });

  it("redirects authenticated users away from /sign-in", async () => {
    stubAccessFetch({ displayName: "Olivia Mendoza", username: "olivia" });
    renderRoutes("/sign-in");
    expect(await screen.findByRole("heading", { name: "Pinoy Loan Manager" })).toBeInTheDocument();
  });

  it("signs out and stays signed out", async () => {
    const user = userEvent.setup();
    stubAccessFetch({ displayName: "Olivia Mendoza", username: "olivia" });
    renderRoutes("/");
    await user.click(await screen.findByRole("button", { name: "Olivia Mendoza" }));
    await user.click(screen.getByRole("menuitem", { name: "Sign out" }));
    expect(await screen.findByRole("heading", { name: "Sign In" })).toBeInTheDocument();
  });

  it("fills Test User identity only when both gates pass", async () => {
    const user = userEvent.setup();
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/auth/me")) {
        return jsonResponse(401, { errorCode: "application.auth.session_invalid" });
      }
      if (url.includes("/local-validation/enabled")) {
        return jsonResponse(200, true);
      }
      if (url.includes("quick-login-identities")) {
        return jsonResponse(200, [
          {
            key: "olivia",
            username: "olivia",
            email: "olivia.mendoza@exits.local",
            listLabel: "Olivia Mendoza",
          },
        ]);
      }
      if (url.includes("/auth/login")) {
        throw new Error("must not auto-submit");
      }
      return jsonResponse(404, null);
    });
    vi.stubGlobal("fetch", fetchMock);
    renderRoutes("/sign-in");
    await screen.findByLabelText("Test User");
    await user.selectOptions(screen.getByLabelText("Test User"), "olivia");
    expect(screen.getByLabelText("Username or email")).toHaveValue("olivia.mendoza@exits.local");
    expect(screen.getByLabelText("Password")).toHaveValue("");
    await waitFor(() => {
      expect(fetchMock.mock.calls.some((call) => String(call[0]).includes("/auth/login"))).toBe(
        false,
      );
    });
  });

  it("keeps EN, Filipino, and theme controls on Sign In", async () => {
    const user = userEvent.setup();
    mockUnauthenticated();
    renderRoutes("/sign-in");
    await screen.findByRole("heading", { name: "Sign In" });
    await user.click(screen.getByRole("radio", { name: "Filipino" }));
    expect(screen.getByRole("heading", { name: "Mag-sign in" })).toBeInTheDocument();
    await user.click(screen.getByRole("radio", { name: "Dark" }));
    expect(document.documentElement.dataset.theme).toBe("dark");
    expect(window.localStorage.getItem(UI_PREFERENCES_STORAGE_KEY)).toMatch(/fil-PH/);
  });
});
