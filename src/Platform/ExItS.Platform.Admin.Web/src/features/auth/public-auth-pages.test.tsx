import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import { AUTH_ERROR_CODES } from "@/api/auth/auth-types";
import { jsonResponse, mockUnauthenticatedFetch } from "@/test/auth-fixtures";

function unauthenticatedMe(url: string) {
  if (url.includes("/api/v1/platform/auth/me")) {
    return jsonResponse(401, {
      title: "Unauthorized",
      status: 401,
      errorCode: AUTH_ERROR_CODES.sessionInvalid,
    });
  }
  if (url.includes("/api/v1/platform/local-validation/enabled")) {
    return jsonResponse(200, false);
  }
  return null;
}

function storageContains(value: string): boolean {
  return (
    JSON.stringify(window.localStorage).includes(value) ||
    JSON.stringify(window.sessionStorage).includes(value)
  );
}

describe("public auth pages", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    delete window.__EXITS_PLATFORM_ADMIN_WEB__;
    window.sessionStorage.clear();
  });

  it("navigates from Sign In to Forgot password and Create account", async () => {
    mockUnauthenticatedFetch();
    window.history.replaceState({}, "", "/admin/login");
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Sign In" });

    await user.click(screen.getByRole("link", { name: "Forgot password?" }));
    expect(await screen.findByRole("heading", { name: "Forgot password" })).toBeInTheDocument();
    expect(screen.queryByText(/not implemented/i)).not.toBeInTheDocument();
    expect(window.location.pathname).toBe("/admin/forgot-password");

    await user.click(screen.getByRole("link", { name: "Sign In" }));
    await screen.findByRole("heading", { name: "Sign In" });
    await user.click(screen.getByRole("link", { name: "Create account" }));
    expect(
      await screen.findByRole("heading", { name: "Create your ExItS account" }),
    ).toBeInTheDocument();
    expect(window.location.pathname).toBe("/admin/register");
  });

  it("renders registration, validates, and shows a generic success without Mailpit in production", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const unauthenticated = unauthenticatedMe(url);
      if (unauthenticated) {
        return unauthenticated;
      }
      if (url.includes("/api/v1/platform/auth/register")) {
        const body = JSON.parse(String(init?.body)) as { displayName?: string; email?: string };
        expect(body).toEqual({ displayName: "Ana Cruz", email: "ana@example.test" });
        return jsonResponse(200, {
          message: "If the email is eligible, a verification message was sent.",
          debugToken: "must-not-render",
        });
      }
      return jsonResponse(404, { title: "Not Found", status: 404 });
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "/admin/register");
    const user = userEvent.setup();
    render(<App />);

    expect(
      await screen.findByRole("heading", { name: "Create your ExItS account" }),
    ).toBeInTheDocument();
    expect(screen.getByLabelText("Display name")).toHaveAttribute("autocomplete", "name");
    expect(screen.getByLabelText("Email")).toHaveAttribute("autocomplete", "email");

    await user.click(screen.getByRole("button", { name: "Create account" }));
    expect(await screen.findByText("Enter your display name.")).toBeInTheDocument();
    expect(screen.getByText("Enter your email.")).toBeInTheDocument();

    await user.type(screen.getByLabelText("Display name"), "Ana Cruz");
    await user.type(screen.getByLabelText("Email"), "ana@example.test");
    await user.click(screen.getByRole("button", { name: "Create account" }));

    expect(await screen.findByRole("heading", { name: "Check your email" })).toBeInTheDocument();
    expect(screen.getByText(/activation link/i)).toBeInTheDocument();
    expect(screen.queryByText("Open Mailpit")).not.toBeInTheDocument();
    expect(screen.queryByText("must-not-render")).not.toBeInTheDocument();
    expect(screen.queryByText(/already exists/i)).not.toBeInTheDocument();
  });

  it("shows Mailpit convenience only when Local Validation tools are enabled", async () => {
    window.__EXITS_PLATFORM_ADMIN_WEB__ = { localValidationToolsEnabled: true };
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        return (
          unauthenticatedMe(url) ??
          jsonResponse(200, {
            message: "If the email is eligible, a verification message was sent.",
          })
        );
      }),
    );
    window.history.replaceState({}, "", "/admin/register");
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Create your ExItS account" });
    await user.type(screen.getByLabelText("Display name"), "Ana Cruz");
    await user.type(screen.getByLabelText("Email"), "ana@example.test");
    await user.click(screen.getByRole("button", { name: "Create account" }));
    expect(await screen.findByRole("link", { name: "Open Mailpit" })).toHaveAttribute(
      "href",
      "http://localhost:8025",
    );
  });

  it("treats duplicate-email registration as the same generic check-email success", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        const unauthenticated = unauthenticatedMe(url);
        if (unauthenticated) {
          return unauthenticated;
        }
        if (url.includes("/api/v1/platform/auth/register")) {
          return jsonResponse(200, {
            message: "If the email is eligible, a verification message was sent.",
          });
        }
        return jsonResponse(404, {});
      }),
    );
    window.history.replaceState({}, "", "/admin/register");
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Create your ExItS account" });
    await user.type(screen.getByLabelText("Display name"), "Ana Cruz");
    await user.type(screen.getByLabelText("Email"), "ana@example.test");
    await user.click(screen.getByRole("button", { name: "Create account" }));
    expect(await screen.findByRole("heading", { name: "Check your email" })).toBeInTheDocument();
    expect(screen.queryByText(/already exists/i)).not.toBeInTheDocument();
  });

  it("handles registration API failure without exposing internals", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        const unauthenticated = unauthenticatedMe(url);
        if (unauthenticated) {
          return unauthenticated;
        }
        if (url.includes("/api/v1/platform/auth/register")) {
          return jsonResponse(500, {
            title: "Error",
            status: 500,
            detail: "stack-trace-secret",
          });
        }
        return jsonResponse(404, {});
      }),
    );
    window.history.replaceState({}, "", "/admin/register");
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Create your ExItS account" });
    await user.type(screen.getByLabelText("Display name"), "Ana Cruz");
    await user.type(screen.getByLabelText("Email"), "ana@example.test");
    await user.click(screen.getByRole("button", { name: "Create account" }));
    expect(
      await screen.findByText("Unable to complete this request. Please try again."),
    ).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Create your ExItS account" })).toBeInTheDocument();
  });

  it("rejects a missing activation token without sending a request", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      return unauthenticatedMe(url) ?? jsonResponse(404, {});
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "/admin/activate-account");
    render(<App />);
    expect(
      await screen.findByRole("heading", { name: "Activate your account" }),
    ).toBeInTheDocument();
    expect(screen.getByRole("alert")).toHaveTextContent(
      "This activation link is invalid or has expired.",
    );
    expect(screen.getByRole("button", { name: "Activate account" })).toBeDisabled();
    expect(fetchMock.mock.calls.some((call) => String(call[0]).includes("activate-account"))).toBe(
      false,
    );
  });

  it("validates password confirmation and activates from the query token", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const unauthenticated = unauthenticatedMe(url);
      if (unauthenticated) {
        return unauthenticated;
      }
      if (url.includes("/api/v1/platform/auth/activate-account")) {
        expect(JSON.parse(String(init?.body))).toEqual({
          token: "opaque-activation-token",
          password: "replacement-password",
        });
        return jsonResponse(200, { hasPassword: true });
      }
      return jsonResponse(404, {});
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "/admin/activate-account?token=opaque-activation-token");
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Activate your account" });
    expect(screen.getByLabelText("New password")).toHaveAttribute("autocomplete", "new-password");
    expect(screen.getByLabelText("Confirm password")).toHaveAttribute(
      "autocomplete",
      "new-password",
    );

    await user.type(screen.getByLabelText("New password"), "replacement-password");
    await user.type(screen.getByLabelText("Confirm password"), "different-password");
    await user.click(screen.getByRole("button", { name: "Activate account" }));
    expect(await screen.findByText("Passwords do not match.")).toBeInTheDocument();

    await user.clear(screen.getByLabelText("Confirm password"));
    await user.type(screen.getByLabelText("Confirm password"), "replacement-password");
    await user.click(screen.getByRole("button", { name: "Activate account" }));
    expect(await screen.findByRole("heading", { name: "Account activated" })).toBeInTheDocument();
    expect(storageContains("opaque-activation-token")).toBe(false);
  });

  it("shows a safe message for an invalid or expired activation token", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        const unauthenticated = unauthenticatedMe(url);
        if (unauthenticated) {
          return unauthenticated;
        }
        if (url.includes("/api/v1/platform/auth/activate-account")) {
          return jsonResponse(401, {
            status: 401,
            errorCode: AUTH_ERROR_CODES.credentialTokenExpired,
            detail: "Verification token has expired.",
          });
        }
        return jsonResponse(404, {});
      }),
    );
    window.history.replaceState({}, "", "/admin/activate-account?token=expired-token");
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Activate your account" });
    await user.type(screen.getByLabelText("New password"), "replacement-password");
    await user.type(screen.getByLabelText("Confirm password"), "replacement-password");
    await user.click(screen.getByRole("button", { name: "Activate account" }));
    expect(await screen.findByRole("alert")).toHaveTextContent(
      "This activation link is invalid or has expired.",
    );
    expect(screen.queryByText("Verification token has expired.")).not.toBeInTheDocument();
  });

  it("always shows the same forgot-password confirmation for known and unknown emails", async () => {
    const bodies: string[] = [];
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input);
        const unauthenticated = unauthenticatedMe(url);
        if (unauthenticated) {
          return unauthenticated;
        }
        if (url.includes("/api/v1/platform/auth/forgot-password")) {
          bodies.push(String(init?.body));
          return jsonResponse(200, {
            message: "If an eligible account exists, a password reset token was issued.",
          });
        }
        return jsonResponse(404, {});
      }),
    );
    window.history.replaceState({}, "", "/admin/forgot-password");
    const user = userEvent.setup();
    const { unmount } = render(<App />);
    await screen.findByRole("heading", { name: "Forgot password" });
    await user.type(screen.getByLabelText("Email or username"), "unknown@example.test");
    await user.click(screen.getByRole("button", { name: "Send reset link" }));
    expect(await screen.findByRole("heading", { name: "Check your email" })).toBeInTheDocument();
    expect(
      screen.getByText("If an eligible account exists, a password reset link has been sent."),
    ).toBeInTheDocument();
    expect(screen.queryByText(/does not exist/i)).not.toBeInTheDocument();
    unmount();

    window.history.replaceState({}, "", "/admin/forgot-password");
    render(<App />);
    await screen.findByRole("heading", { name: "Forgot password" });
    await userEvent.setup().type(screen.getByLabelText("Email or username"), "ana@example.test");
    await userEvent.setup().click(screen.getByRole("button", { name: "Send reset link" }));
    expect(await screen.findByRole("heading", { name: "Check your email" })).toBeInTheDocument();
    expect(bodies).toHaveLength(2);
    expect(JSON.parse(bodies[0]!)).toEqual({ usernameOrEmail: "unknown@example.test" });
    expect(JSON.parse(bodies[1]!)).toEqual({ usernameOrEmail: "ana@example.test" });
  });

  it("resets a password from the query token and does not persist it", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const unauthenticated = unauthenticatedMe(url);
      if (unauthenticated) {
        return unauthenticated;
      }
      if (url.includes("/api/v1/platform/auth/reset-password")) {
        expect(JSON.parse(String(init?.body))).toEqual({
          token: "opaque-reset-token",
          newPassword: "replacement-password",
        });
        return jsonResponse(200, { hasPassword: true });
      }
      return jsonResponse(404, {});
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "/admin/reset-password?token=opaque-reset-token");
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Reset password" });
    await user.type(screen.getByLabelText("New password"), "replacement-password");
    await user.type(screen.getByLabelText("Confirm password"), "replacement-password");
    await user.click(screen.getByRole("button", { name: "Change password" }));
    expect(
      await screen.findByRole("heading", { name: "Password changed successfully." }),
    ).toBeInTheDocument();
    expect(storageContains("opaque-reset-token")).toBe(false);
  });

  it("shows a safe message for a consumed reset token", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        const unauthenticated = unauthenticatedMe(url);
        if (unauthenticated) {
          return unauthenticated;
        }
        if (url.includes("/api/v1/platform/auth/reset-password")) {
          return jsonResponse(401, {
            status: 401,
            errorCode: AUTH_ERROR_CODES.credentialTokenInvalid,
            detail: "Reset token is invalid.",
          });
        }
        return jsonResponse(404, {});
      }),
    );
    window.history.replaceState({}, "", "/admin/reset-password?token=consumed-token");
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Reset password" });
    await user.type(screen.getByLabelText("New password"), "replacement-password");
    await user.type(screen.getByLabelText("Confirm password"), "replacement-password");
    await user.click(screen.getByRole("button", { name: "Change password" }));
    expect(await screen.findByRole("alert")).toHaveTextContent(
      "This password reset link is invalid or has expired.",
    );
    expect(screen.queryByText("Reset token is invalid.")).not.toBeInTheDocument();
  });
});
