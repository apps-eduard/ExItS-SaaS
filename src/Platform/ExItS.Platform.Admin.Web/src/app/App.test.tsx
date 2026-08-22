import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import { AUTH_ERROR_CODES } from "@/api/auth/auth-types";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";
import {
  jsonResponse,
  mockAuthenticatedFetch,
  mockUnauthenticatedFetch,
  sampleSession,
} from "@/test/auth-fixtures";

describe("App foundation", () => {
  beforeEach(() => {
    window.history.replaceState({}, "", "/");
    mockUnauthenticatedFetch();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("redirects unauthenticated users from the foundation route to Sign In", async () => {
    render(<App />);

    expect(await screen.findByRole("heading", { name: "Sign In" })).toBeInTheDocument();
    expect(window.location.pathname).toBe("/admin/login");
  });

  it("defaults to System theme, English, and Balanced density on Sign In", async () => {
    window.history.replaceState({}, "", "/admin/login");
    render(<App />);

    await screen.findByRole("heading", { name: "Sign In" });
    expect(document.documentElement.dataset.theme).toBe("system");
    expect(document.documentElement.lang).toBe("en");
    expect(document.documentElement.dataset.density).toBe("balanced");
    await userEvent.setup().click(screen.getByRole("button", { name: "Preferences" }));
    expect(await screen.findByRole("menuitem", { name: /System/ })).toHaveTextContent("✓");
    expect(screen.getByRole("menuitem", { name: /English/ })).toHaveTextContent("✓");
  });

  it("applies Light and Dark theme selections and persists them", async () => {
    window.history.replaceState({}, "", "/admin/login");
    const user = userEvent.setup();
    const { unmount } = render(<App />);
    await screen.findByRole("heading", { name: "Sign In" });

    await user.click(screen.getByRole("button", { name: "Preferences" }));
    await user.click(await screen.findByRole("menuitem", { name: /^Light/ }));
    expect(document.documentElement.dataset.theme).toBe("light");
    expect(JSON.parse(window.localStorage.getItem(UI_PREFERENCES_STORAGE_KEY) ?? "{}").theme).toBe(
      "light",
    );

    await user.click(screen.getByRole("button", { name: "Preferences" }));
    await user.click(await screen.findByRole("menuitem", { name: /^Dark/ }));
    expect(document.documentElement.dataset.theme).toBe("dark");
    unmount();

    render(<App />);
    await screen.findByRole("heading", { name: "Sign In" });
    expect(document.documentElement.dataset.theme).toBe("dark");
    await userEvent.setup().click(screen.getByRole("button", { name: "Preferences" }));
    expect(await screen.findByRole("menuitem", { name: /^Dark/ })).toHaveTextContent("✓");
  });

  it("keeps System mode as an explicit preference so OS color-scheme can drive tokens", async () => {
    window.history.replaceState({}, "", "/admin/login");
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Sign In" });

    await user.click(screen.getByRole("button", { name: "Preferences" }));
    await user.click(await screen.findByRole("menuitem", { name: /^Dark/ }));
    await user.click(screen.getByRole("button", { name: "Preferences" }));
    await user.click(await screen.findByRole("menuitem", { name: /^System/ }));
    expect(document.documentElement.dataset.theme).toBe("system");
  });

  it("switches to Filipino and shows translated Sign In labels", async () => {
    window.history.replaceState({}, "", "/admin/login");
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Sign In" });

    await user.click(screen.getByRole("button", { name: "Preferences" }));
    await user.click(await screen.findByRole("menuitem", { name: /Filipino/ }));
    expect(document.documentElement.lang).toBe("fil-PH");
    expect(screen.getByRole("heading", { name: "Mag-sign In" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Mag-sign In" })).toBeInTheDocument();
  });

  it("falls back to defaults when stored preferences are corrupt", async () => {
    window.history.replaceState({}, "", "/admin/login");
    window.localStorage.setItem(UI_PREFERENCES_STORAGE_KEY, "{not-json");
    render(<App />);
    await screen.findByRole("heading", { name: "Sign In" });
    expect(document.documentElement.dataset.theme).toBe("system");
    expect(document.documentElement.lang).toBe("en");
    expect(document.documentElement.dataset.density).toBe("balanced");
  });

  it("shows the authenticated foundation after auth/me bootstrap", async () => {
    mockAuthenticatedFetch();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Overview" })).toBeInTheDocument();
    expect(window.location.pathname).toBe("/admin");
  });
});

describe("Sign In", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("validates required fields and keeps keyboard-accessible labels", async () => {
    mockUnauthenticatedFetch();
    window.history.replaceState({}, "", "/admin/login");
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Sign In" });

    await user.click(screen.getByRole("button", { name: "Sign In" }));
    expect(await screen.findByText("Enter your email.")).toBeInTheDocument();
    expect(screen.getByText("Enter your password.")).toBeInTheDocument();
    expect(screen.getByLabelText("Email")).toHaveAttribute("autocomplete", "username");
    expect(screen.getByLabelText("Password")).toHaveAttribute("autocomplete", "current-password");
  });

  it("toggles password visibility", async () => {
    mockUnauthenticatedFetch();
    window.history.replaceState({}, "", "/admin/login");
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Sign In" });

    const password = screen.getByLabelText("Password");
    expect(password).toHaveAttribute("type", "password");
    await user.click(screen.getByRole("button", { name: "Show password" }));
    expect(password).toHaveAttribute("type", "text");
    await user.click(screen.getByRole("button", { name: "Hide password" }));
    expect(password).toHaveAttribute("type", "password");
  });

  it("shows a submitting state while login is in flight", async () => {
    let resolveLogin: ((value: Response) => void) | undefined;
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/auth/me")) {
          return jsonResponse(401, { status: 401, errorCode: AUTH_ERROR_CODES.sessionInvalid });
        }
        if (url.includes("/auth/login")) {
          return await new Promise<Response>((resolve) => {
            resolveLogin = resolve;
          });
        }
        return jsonResponse(404, {});
      }),
    );
    window.history.replaceState({}, "", "/admin/login");
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Sign In" });
    await user.type(screen.getByLabelText("Email"), "olivia@example.test");
    await user.type(screen.getByLabelText("Password"), "secret-password");
    await user.click(screen.getByRole("button", { name: "Sign In" }));
    expect(await screen.findByRole("button", { name: "Signing in" })).toBeDisabled();
    resolveLogin?.(jsonResponse(200, { ...sampleSession, sessionToken: "must-not-persist" }));
    await waitFor(() => {
      expect(window.location.pathname).toBe("/admin");
    });
  });

  it("shows invalid credentials, preserves email, and clears the password", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input);
        if (url.includes("/auth/me")) {
          return jsonResponse(401, { status: 401, errorCode: AUTH_ERROR_CODES.sessionInvalid });
        }
        if (url.includes("/local-validation/enabled")) {
          return jsonResponse(200, false);
        }
        if (url.includes("/auth/login")) {
          const body = JSON.parse(String(init?.body)) as { password?: string };
          expect(body.password).toBe("wrong-password");
          return jsonResponse(401, {
            status: 401,
            errorCode: AUTH_ERROR_CODES.loginFailed,
            detail: "Invalid credentials stack",
          });
        }
        return jsonResponse(404, {});
      }),
    );
    window.history.replaceState({}, "", "/admin/login");
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Sign In" });
    await user.type(screen.getByLabelText("Email"), "olivia@example.test");
    await user.type(screen.getByLabelText("Password"), "wrong-password");
    await user.click(screen.getByRole("button", { name: "Sign In" }));
    expect(await screen.findByText("Invalid credentials")).toBeInTheDocument();
    expect(screen.getByLabelText("Email")).toHaveValue("olivia@example.test");
    expect(screen.getByLabelText("Password")).toHaveValue("");
    expect(screen.queryByText(/stack/i)).not.toBeInTheDocument();
    expect(screen.queryByText("Something went wrong")).not.toBeInTheDocument();
  });

  it("maps login_failed without a camelCase errorCode as invalid credentials", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/auth/me")) {
          return jsonResponse(401, { status: 401, errorCode: AUTH_ERROR_CODES.sessionInvalid });
        }
        if (url.includes("/local-validation/enabled")) {
          return jsonResponse(200, false);
        }
        if (url.includes("/auth/login")) {
          return jsonResponse(400, {
            status: 400,
            extensions: { errorCode: AUTH_ERROR_CODES.loginFailed },
            detail: "Exception text must not render",
          });
        }
        return jsonResponse(404, {});
      }),
    );
    window.history.replaceState({}, "", "/admin/login");
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Sign In" });
    await user.type(screen.getByLabelText("Email"), "olivia@example.test");
    await user.type(screen.getByLabelText("Password"), "wrong-password");
    await user.click(screen.getByRole("button", { name: "Sign In" }));
    expect(await screen.findByText("Invalid credentials")).toBeInTheDocument();
    expect(screen.queryByText("Something went wrong")).not.toBeInTheDocument();
    expect(screen.queryByText(/Exception text/i)).not.toBeInTheDocument();
  });

  it("fills email only from a Test User selection and still requires a manual password on login", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.includes("/auth/me")) {
        return jsonResponse(401, { status: 401, errorCode: AUTH_ERROR_CODES.sessionInvalid });
      }
      if (url.includes("/local-validation/enabled")) {
        return jsonResponse(200, true);
      }
      if (url.includes("/local-validation/quick-login-identities")) {
        return jsonResponse(200, [
          {
            key: "olivia",
            username: "olivia",
            displayName: "Olivia Mendoza",
            email: "olivia.mendoza@exits.local",
            listLabel: "Olivia Mendoza",
          },
        ]);
      }
      if (url.includes("/auth/login")) {
        const body = JSON.parse(String(init?.body)) as {
          usernameOrEmail?: string;
          password?: string;
        };
        expect(body.usernameOrEmail).toBe("olivia.mendoza@exits.local");
        expect(body.password).toBe("typed-by-tester");
        return jsonResponse(200, { ...sampleSession, sessionToken: "must-not-persist" });
      }
      return jsonResponse(404, {});
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "/admin/login");
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Sign In" });
    const selector = await screen.findByLabelText("Test user");
    await user.selectOptions(selector, "olivia");
    expect(screen.getByLabelText("Email")).toHaveValue("olivia.mendoza@exits.local");
    expect(screen.getByLabelText("Password")).toHaveValue("");
    expect(fetchMock.mock.calls.some(([request]) => String(request).includes("/auth/login"))).toBe(
      false,
    );

    await user.type(screen.getByLabelText("Password"), "typed-by-tester");
    await user.click(screen.getByRole("button", { name: "Sign In" }));
    await waitFor(() => {
      expect(window.location.pathname).toBe("/admin");
    });
    expect(fetchMock.mock.calls.some(([request]) => String(request).includes("/auth/login"))).toBe(
      true,
    );
  });

  it("maps account lock and network failures", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/auth/me")) {
        return jsonResponse(401, { status: 401, errorCode: AUTH_ERROR_CODES.sessionInvalid });
      }
      if (url.includes("/local-validation/enabled")) {
        return jsonResponse(200, false);
      }
      if (url.includes("/auth/login")) {
        return jsonResponse(409, {
          status: 409,
          errorCode: AUTH_ERROR_CODES.credentialLockedOut,
        });
      }
      return jsonResponse(404, {});
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "/admin/login");
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Sign In" });
    await user.type(screen.getByLabelText("Email"), "olivia@example.test");
    await user.type(screen.getByLabelText("Password"), "secret-password");
    await user.click(screen.getByRole("button", { name: "Sign In" }));
    expect(await screen.findByText("Account is locked.")).toBeInTheDocument();

    fetchMock.mockImplementation(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/auth/me")) {
        return jsonResponse(401, { status: 401, errorCode: AUTH_ERROR_CODES.sessionInvalid });
      }
      if (url.includes("/local-validation/enabled")) {
        return jsonResponse(200, false);
      }
      throw new TypeError("Failed to fetch");
    });
    await user.type(screen.getByLabelText("Password"), "secret-password");
    await user.click(screen.getByRole("button", { name: "Sign In" }));
    expect(await screen.findByText("Unable to reach authentication service")).toBeInTheDocument();
    expect(screen.queryByText("Invalid credentials")).not.toBeInTheDocument();
  });

  it("shows the session-expired notice", async () => {
    mockUnauthenticatedFetch();
    window.history.replaceState({}, "", "/admin/login?notice=session-expired");
    render(<App />);
    expect(
      await screen.findByText("Your session has expired. Please sign in again."),
    ).toBeInTheDocument();
  });

  it("navigates to a safe return path after successful login", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/auth/me")) {
          return jsonResponse(401, { status: 401, errorCode: AUTH_ERROR_CODES.sessionInvalid });
        }
        if (url.includes("/auth/login")) {
          return jsonResponse(200, { ...sampleSession, sessionToken: "opaque-token" });
        }
        return jsonResponse(404, {});
      }),
    );
    window.history.replaceState({}, "", "/admin/login?return=/");
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Sign In" });
    await user.type(screen.getByLabelText("Email"), "olivia@example.test");
    await user.type(screen.getByLabelText("Password"), "secret-password");
    await user.click(screen.getByRole("button", { name: "Sign In" }));
    await waitFor(() => {
      expect(window.location.pathname).toBe("/admin");
    });
    expect(window.localStorage.getItem("sessionToken")).toBeNull();
    expect(document.cookie.includes("opaque-token")).toBe(false);
  });

  it("rejects an external return URL and uses the foundation route instead", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/auth/me")) {
          return jsonResponse(401, { status: 401, errorCode: AUTH_ERROR_CODES.sessionInvalid });
        }
        if (url.includes("/auth/login")) {
          return jsonResponse(200, sampleSession);
        }
        return jsonResponse(404, {});
      }),
    );
    window.history.replaceState(
      {},
      "",
      "/admin/login?return=" + encodeURIComponent("https://evil.example/phish"),
    );
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Sign In" });
    await user.type(screen.getByLabelText("Email"), "olivia@example.test");
    await user.type(screen.getByLabelText("Password"), "secret-password");
    await user.click(screen.getByRole("button", { name: "Sign In" }));
    await waitFor(() => {
      expect(window.location.pathname).toBe("/admin");
    });
    expect(window.location.href).not.toContain("evil.example");
  });
});
