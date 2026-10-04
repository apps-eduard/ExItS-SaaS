import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import * as cutoverRouting from "@/lib/auth/cutover-routing";
import { jsonResponse, sampleAuthorization, sampleSession } from "@/test/auth-fixtures";

const credentials = {
  userId: sampleSession.userId,
  hasPassword: true,
  emailVerified: true,
  recoveryEmail: "recovery@example.test",
  recoveryEmailVerified: true,
  pendingRecoveryEmail: null,
  needsRecoveryEmailPrompt: false,
};

function storageContains(value: string): boolean {
  return (
    JSON.stringify(window.localStorage).includes(value) ||
    JSON.stringify(window.sessionStorage).includes(value)
  );
}

function authenticatedFetch(
  handler: (url: string, init?: RequestInit) => Response | null,
) {
  vi.stubGlobal(
    "fetch",
    vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const handled = handler(url, init);
      if (handled) {
        return handled;
      }
      if (url.includes("/api/v1/platform/auth/me")) {
        return jsonResponse(200, sampleSession);
      }
      if (url.includes("/api/v1/platform/authorization/me")) {
        return jsonResponse(200, sampleAuthorization);
      }
      if (url.includes("/api/v1/platform/auth/credentials")) {
        return jsonResponse(200, credentials);
      }
      return jsonResponse(404, { title: "Not Found", status: 404 });
    }),
  );
}

describe("admin cutover pages", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    window.sessionStorage.clear();
    window.localStorage.clear();
  });

  it("loads the signed-in account and recovery email state", async () => {
    authenticatedFetch(() => null);
    window.history.replaceState({}, "", "/admin/account");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Account" })).toBeInTheDocument();
    expect(await screen.findByText("recovery@example.test")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Sign out" })).toBeInTheDocument();
  });

  it("changes the password and returns to sign-in without storing the password", async () => {
    const user = userEvent.setup();
    authenticatedFetch((url, init) => {
      if (url.includes("/api/v1/platform/auth/change-password") && init?.method === "POST") {
        const body = JSON.parse(String(init.body)) as { currentPassword: string; newPassword: string };
        expect(body.currentPassword).toBe("Current-pass-1!");
        expect(body.newPassword).toBe("New-password-1!");
        return jsonResponse(200, { userId: sampleSession.userId, hasPassword: true });
      }
      if (url.includes("/api/v1/platform/antiforgery/token")) {
        return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf" });
      }
      return null;
    });
    window.history.replaceState({}, "", "/admin/account/change-password");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Change password" })).toBeInTheDocument();
    await user.type(screen.getByLabelText("Current password"), "Current-pass-1!");
    await user.type(screen.getByLabelText("New password"), "New-password-1!");
    await user.click(screen.getByRole("button", { name: "Change password" }));
    expect(await screen.findByRole("heading", { name: "Sign In" })).toBeInTheDocument();
    expect(screen.getByText("Password changed. Sign in again.")).toBeInTheDocument();
    expect(storageContains("Current-pass-1!")).toBe(false);
    expect(storageContains("New-password-1!")).toBe(false);
  });

  it("shows a password-change validation error and clears the fields", async () => {
    const user = userEvent.setup();
    authenticatedFetch((url) => {
      if (url.includes("/api/v1/platform/auth/change-password")) {
        return jsonResponse(400, {
          title: "Invalid password",
          status: 400,
          detail: "Password does not meet the policy.",
          errorCode: "application.credential.password_invalid",
        });
      }
      if (url.includes("/api/v1/platform/antiforgery/token")) {
        return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf" });
      }
      return null;
    });
    window.history.replaceState({}, "", "/admin/account/change-password");
    render(<App />);
    await screen.findByRole("heading", { name: "Change password" });
    await user.type(screen.getByLabelText("Current password"), "Current-pass-1!");
    await user.type(screen.getByLabelText("New password"), "New-password-1!");
    await user.click(screen.getByRole("button", { name: "Change password" }));
    expect(await screen.findByText("Password does not meet the policy.")).toBeInTheDocument();
    expect(screen.getByLabelText("Current password")).toHaveValue("");
    expect(storageContains("New-password-1!")).toBe(false);
  });

  it("accepts a valid organization invitation and rejects an expired one", async () => {
    const user = userEvent.setup();
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input);
        if (url.includes("/api/v1/platform/auth/me")) {
          return jsonResponse(401, { status: 401, errorCode: "application.auth.session_invalid" });
        }
        if (url.includes("/api/v1/platform/invitations/accept") && init?.body) {
          const body = JSON.parse(String(init.body)) as { token: string };
          if (body.token === "expired-token") {
            return jsonResponse(400, {
              status: 400,
              detail: "Invitation has expired.",
              errorCode: "platform.invitation.expired",
            });
          }
          return jsonResponse(200, {
            staffLogin: "ana@ORG000001",
            contactEmail: "ana@example.test",
            organizationDisplayName: "North Store",
          });
        }
        return jsonResponse(404, { status: 404 });
      }),
    );
    window.history.replaceState({}, "", "/admin/accept-organization-invitation?token=good-token");
    const view = render(<App />);
    expect(await screen.findByRole("heading", { name: "Accept organization invitation" })).toBeInTheDocument();
    await user.type(screen.getByLabelText("Password"), "Staff-password-1!");
    await user.click(screen.getByRole("button", { name: "Accept invitation" }));
    expect(await screen.findByText("ana@ORG000001")).toBeInTheDocument();
    expect(storageContains("good-token")).toBe(false);
    view.unmount();

    window.history.replaceState({}, "", "/admin/accept-organization-invitation?token=expired-token");
    render(<App />);
    await screen.findByRole("heading", { name: "Accept organization invitation" });
    await user.type(screen.getByLabelText("Password"), "Staff-password-1!");
    await user.click(screen.getByRole("button", { name: "Accept invitation" }));
    expect(await screen.findByText("This invitation is invalid, expired, or already used.")).toBeInTheDocument();
  });

  it("adopts a safe external callback and rejects an unsafe return URL", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const headers = new Headers(init?.headers);
      if (url.includes("/api/v1/platform/auth/me") && headers.get("X-ExItS-Session-Token") === "once-token") {
        return jsonResponse(200, sampleSession);
      }
      if (url.includes("/api/v1/platform/auth/credentials")) {
        return jsonResponse(200, credentials);
      }
      if (url.includes("/api/v1/platform/auth/me")) {
        return jsonResponse(401, { status: 401, errorCode: "application.auth.session_invalid" });
      }
      return jsonResponse(404, { status: 404 });
    });
    vi.stubGlobal("fetch", fetchMock);
    const assign = vi.spyOn(cutoverRouting, "replaceBrowserLocation").mockImplementation(() => undefined);
    window.history.replaceState({}, "", "/admin/external-login-callback?sessionToken=once-token&return=%2Fadmin");
    render(<App />);
    await vi.waitFor(() => {
      expect(assign).toHaveBeenCalledWith("/admin");
    });
    expect(window.location.search).not.toContain("once-token");
    expect(storageContains("once-token")).toBe(false);
    assign.mockRestore();
  });

  it("does not navigate when the external return URL is unsafe", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => jsonResponse(401, { status: 401, errorCode: "application.auth.session_invalid" })),
    );
    const assign = vi.spyOn(cutoverRouting, "replaceBrowserLocation").mockImplementation(() => undefined);
    window.history.replaceState(
      {},
      "",
      "/admin/external-login-callback?sessionToken=once-token&return=https://evil.example/phish",
    );
    render(<App />);
    expect(await screen.findByText("The return address is not allowed.")).toBeInTheDocument();
    expect(assign).not.toHaveBeenCalled();
    expect(storageContains("once-token")).toBe(false);
    assign.mockRestore();
  });

  it("signs out from the account page", async () => {
    const user = userEvent.setup();
    let loggedOut = false;
    authenticatedFetch((url, init) => {
      if (url.includes("/api/v1/platform/auth/logout") && init?.method === "POST") {
        loggedOut = true;
        return jsonResponse(204, "");
      }
      if (url.includes("/api/v1/platform/antiforgery/token")) {
        return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf" });
      }
      return null;
    });
    window.history.replaceState({}, "", "/admin/account");
    render(<App />);
    await user.click(await screen.findByRole("button", { name: "Sign out" }));
    expect(await screen.findByRole("heading", { name: "Sign In" })).toBeInTheDocument();
    expect(loggedOut).toBe(true);
  });
});
