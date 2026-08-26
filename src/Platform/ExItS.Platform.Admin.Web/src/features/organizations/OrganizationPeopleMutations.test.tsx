import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import {
  jsonResponse,
  mockAuthenticatedFetch,
  sampleAuthorization,
  sampleSession,
  textResponse,
} from "@/test/auth-fixtures";

const sampleOrg = {
  id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  displayName: "Northwind Market",
  slug: "northwind-market",
  status: "Active",
};

function stubDesktop() {
  vi.spyOn(window, "matchMedia").mockImplementation((query: string) => {
    return {
      matches: query.includes("min-width: 768px"),
      media: query,
      onchange: null,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      addListener: () => undefined,
      removeListener: () => undefined,
      dispatchEvent: () => true,
    } as MediaQueryList;
  });
}

describe("organization people invite mutations", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("shows invite error detail instead of silent failure", async () => {
    stubDesktop();
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const method = init?.method ?? "GET";
      if (url.includes("/auth/me")) return jsonResponse(200, sampleSession);
      if (url.includes("/authorization/me")) return jsonResponse(200, sampleAuthorization);
      if (url.includes("/health")) return textResponse(200, "Healthy");
      if (url.includes("/commercial-summary")) {
        return jsonResponse(200, { subscriptions: [], payments: [], latestEntitlements: [] });
      }
      if (url.includes("/invitations") && method === "POST") {
        return jsonResponse(500, { title: "Error", status: 500, detail: "invite failed" });
      }
      if (url.includes("/members")) {
        return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 20 });
      }
      if (url.includes("/invitations")) {
        return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 20 });
      }
      if (url.includes(`/organizations/${sampleOrg.id}`)) {
        return jsonResponse(200, sampleOrg);
      }
      if (url.includes("/antiforgery/token")) {
        return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "test-token" });
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/people`);
    const user = userEvent.setup();
    render(<App />);
    await user.click(await screen.findByRole("button", { name: /invite/i }));
    await user.type(screen.getByLabelText(/contact/i), "invitee@example.test");
    await user.click(screen.getByRole("button", { name: /send invitation/i }));
    await waitFor(() => {
      expect(screen.getByText(/unable to complete this action/i)).toBeInTheDocument();
    });
    expect(screen.getByText("invite failed")).toBeInTheDocument();
  });

  it("completes invite successfully", async () => {
    stubDesktop();
    mockAuthenticatedFetch({ organizationItems: [sampleOrg], memberItems: [], invitationItems: [] });
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/people`);
    const user = userEvent.setup();
    render(<App />);
    await user.click(await screen.findByRole("button", { name: /invite/i }));
    await user.type(screen.getByLabelText(/contact/i), "invitee@example.test");
    await user.click(screen.getByRole("button", { name: /send invitation/i }));
    await waitFor(() => {
      expect(screen.queryByRole("button", { name: /send invitation/i })).not.toBeInTheDocument();
    });
  });
});
