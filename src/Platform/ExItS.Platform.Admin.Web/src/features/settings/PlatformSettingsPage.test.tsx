import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import { SETTINGS_BACKEND_API_GAPS } from "@/features/settings/settings-sections";
import { mockAuthenticatedFetch } from "@/test/auth-fixtures";

const sampleGeneral = {
  platformDisplayName: "ExItS",
  supportEmail: null,
  brandingLogoUrl: null,
  brandingPrimaryColor: null,
  brandingAccentColor: null,
  version: 1,
  updatedAtUtc: "2026-08-23T12:00:00Z",
  updatedByActorId: "olivia@example.test",
};

const sampleEmail = {
  providerMode: "Smtp",
  smtpHost: null,
  smtpPort: null,
  smtpUsername: null,
  passwordConfigured: false,
  fromDisplayName: "ExItS",
  fromAddress: "",
  securityMode: "None",
  adminPublicBaseUrl: null,
  isConfigured: false,
  version: 1,
  updatedAtUtc: "2026-08-23T12:00:00Z",
  updatedByActorId: "olivia@example.test",
};

const sampleRegional = {
  defaultTimeZoneId: "UTC",
  defaultLocale: "en-US",
  defaultCurrencyCode: "USD",
  defaultCountryCode: "US",
  dateFormat: null,
  timeFormat: null,
  version: 1,
  updatedAtUtc: "2026-08-23T12:00:00Z",
  updatedByActorId: "olivia@example.test",
};

function stubDesktop() {
  vi.spyOn(window, "matchMedia").mockImplementation((query: string) => {
    return {
      matches: query.includes("min-width: 1024px") || query.includes("min-width: 768px"),
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

function mockSettingsFetch() {
  return vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? "GET";
    if (url.includes("/api/v1/platform/auth/me")) {
      return {
        ok: true,
        status: 200,
        json: async () => ({
          sessionId: "11111111-1111-1111-1111-111111111111",
          userId: "22222222-2222-2222-2222-222222222222",
          username: "olivia",
          displayName: "Olivia Mendoza",
          email: "olivia@example.test",
          expiresAtUtc: "2026-08-19T12:00:00Z",
          absoluteExpiresAtUtc: "2026-08-20T12:00:00Z",
          lastActivityAtUtc: "2026-08-19T11:00:00Z",
          selectedOrganizationId: null,
          selectedOrganizationDisplayName: null,
          organizationSelectionState: "None",
          activeOrganizationCount: 0,
          accountClass: "Platform",
          allowedScope: "Platform",
        }),
        text: async () => "",
      } as Response;
    }
    if (url.includes("/api/v1/platform/authorization/me")) {
      return {
        ok: true,
        status: 200,
        json: async () => ({
          actorIdentifier: "olivia@example.test",
          actorType: "PlatformUser",
          platformUserId: "22222222-2222-2222-2222-222222222222",
          organizationId: null,
          permissions: ["platform.permission.view_portfolio"],
        }),
        text: async () => "",
      } as Response;
    }
    if (url.includes("/api/v1/platform/settings/general") && method === "GET") {
      return { ok: true, status: 200, json: async () => sampleGeneral, text: async () => "" } as Response;
    }
    if (url.includes("/api/v1/platform/settings/email") && method === "GET") {
      return { ok: true, status: 200, json: async () => sampleEmail, text: async () => "" } as Response;
    }
    if (url.includes("/api/v1/platform/settings/regional") && method === "GET") {
      return { ok: true, status: 200, json: async () => sampleRegional, text: async () => "" } as Response;
    }
    if (url.includes("/health")) {
      return { ok: true, status: 200, json: async () => "Healthy", text: async () => "Healthy" } as Response;
    }
    return { ok: true, status: 200, json: async () => ({}), text: async () => "" } as Response;
  });
}

describe("platform settings workspace", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("exposes a single Settings → Platform Settings nav link to /admin/settings", async () => {
    stubDesktop();
    mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin");
    render(<App />);

    const nav = await screen.findByRole("navigation", { name: "Primary" });
    const settingsLink = within(nav).getByRole("link", { name: "Platform Settings" });
    expect(settingsLink).toHaveAttribute("href", "/admin/settings");
  });

  it("loads general settings form and keeps security as backend gap", async () => {
    stubDesktop();
    vi.stubGlobal("fetch", mockSettingsFetch());
    window.history.replaceState({}, "", "/admin/settings/general");
    const user = userEvent.setup();
    render(<App />);

    expect(await screen.findByLabelText("Platform display name")).toHaveValue("ExItS");
    expect(screen.queryByText("BACKEND_API_GAP:PLATFORM_SETTINGS_GENERAL")).not.toBeInTheDocument();

    const workspaceNav = screen.getByRole("navigation", { name: "Settings categories" });
    await user.click(within(workspaceNav).getByRole("link", { name: "Security Policies" }));
    expect(await screen.findByText("BACKEND_API_GAP:PLATFORM_SETTINGS_SECURITY")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Save changes" })).not.toBeInTheDocument();
  });

  it("records remaining settings categories as backend API gaps", () => {
    expect(SETTINGS_BACKEND_API_GAPS).toEqual([
      "BACKEND_API_GAP:PLATFORM_SETTINGS_SECURITY",
      "BACKEND_API_GAP:PLATFORM_SETTINGS_INTEGRATIONS",
      "BACKEND_API_GAP:PLATFORM_SETTINGS_FEATURE_FLAGS",
      "BACKEND_API_GAP:PLATFORM_SETTINGS_ADVANCED",
    ]);
  });

  it("fail-closes when the actor is not a platform administrator", async () => {
    stubDesktop();
    mockAuthenticatedFetch({ permissions: [] });
    window.history.replaceState({}, "", "/admin/settings/general");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Page not found" })).toBeInTheDocument();
  });
});
