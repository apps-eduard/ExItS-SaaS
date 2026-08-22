import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import { jsonResponse, mockAuthenticatedFetch, pagedJson } from "@/test/auth-fixtures";

const samplePermission = {
  code: "platform.permission.view_portfolio",
  description: "view portfolio",
  area: "platform",
};

const builtinRole = {
  id: "11111111-1111-1111-1111-111111111111",
  code: "PlatformAdministrator",
  name: "Platform Administrator",
  description: "Built-in admin",
  kind: "BuiltIn",
  status: "Active",
  permissions: ["platform.permission.manage_platform_users"],
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-08-01T00:00:00Z",
  version: 1,
};

const customRole = {
  id: "22222222-2222-2222-2222-222222222222",
  code: "OpsViewer",
  name: "Ops Viewer",
  description: "Custom ops",
  kind: "Custom",
  status: "Active",
  permissions: ["platform.permission.view_portfolio"],
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-08-01T00:00:00Z",
  version: 3,
};

function mockRolesFetch(options?: {
  permissions?: string[];
  listItems?: typeof customRole[];
  createFailStatus?: number;
  mutationFail?: boolean;
}) {
  let role = { ...customRole };
  const fetchMock = mockAuthenticatedFetch({
    permissions: options?.permissions ?? [
      "platform.permission.manage_platform_users",
      "platform.permission.view_portfolio",
    ],
  });

  fetchMock.mockImplementation(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();

    if (url.includes("/auth/me")) {
      return jsonResponse(200, {
        sessionId: "11111111-1111-1111-1111-111111111111",
        userId: "22222222-2222-2222-2222-222222222222",
        username: "olivia",
        displayName: "Olivia Mendoza",
        email: "olivia@example.test",
        expiresAtUtc: "2026-08-19T12:00:00Z",
        absoluteExpiresAtUtc: "2026-08-20T12:00:00Z",
        selectedOrganizationId: null,
        selectedOrganizationDisplayName: null,
        organizationSelectionState: "None",
        activeOrganizationCount: 0,
        accountClass: "Platform",
      });
    }
    if (url.includes("/authorization/me")) {
      return jsonResponse(200, {
        actorIdentifier: "olivia@example.test",
        actorType: "PlatformUser",
        platformUserId: "22222222-2222-2222-2222-222222222222",
        organizationId: null,
        permissions: options?.permissions ?? [
          "platform.permission.manage_platform_users",
          "platform.permission.view_portfolio",
        ],
      });
    }
    if (url.includes("/antiforgery/token")) {
      return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "test-antiforgery-token" });
    }
    if (url.includes("/authorization/permissions")) {
      return jsonResponse(200, [samplePermission]);
    }
    if (method === "POST" && url.endsWith("/authorization/role-definitions")) {
      if (options?.createFailStatus) {
        return jsonResponse(options.createFailStatus, {
          detail: "code already exists",
          title: "Conflict",
          status: options.createFailStatus,
        });
      }
      const body = typeof init?.body === "string" ? JSON.parse(init.body) : {};
      const created = {
        ...customRole,
        id: "33333333-3333-3333-3333-333333333333",
        code: body.code,
        name: body.name,
        description: body.description ?? null,
        permissions: body.permissions ?? [],
        version: 1,
      };
      return jsonResponse(201, created);
    }
    if (url.includes(`/role-definitions/${role.id}/activate`) && method === "POST") {
      if (options?.mutationFail) {
        return jsonResponse(500, { detail: "activate failed", status: 500 });
      }
      role = { ...role, status: "Active", version: role.version + 1 };
      return jsonResponse(200, role);
    }
    if (url.includes(`/role-definitions/${role.id}/deactivate`) && method === "POST") {
      if (options?.mutationFail) {
        return jsonResponse(500, { detail: "deactivate failed", status: 500 });
      }
      role = { ...role, status: "Inactive", version: role.version + 1 };
      return jsonResponse(200, role);
    }
    if (url.includes(`/role-definitions/${role.id}/retire`) && method === "POST") {
      if (options?.mutationFail) {
        return jsonResponse(500, { detail: "retire failed", status: 500 });
      }
      role = { ...role, status: "Retired", version: role.version + 1 };
      return jsonResponse(200, role);
    }
    if (url.includes(`/role-definitions/${role.id}`) && method === "PUT") {
      if (options?.mutationFail) {
        return jsonResponse(500, { detail: "save failed", status: 500 });
      }
      const body = typeof init?.body === "string" ? JSON.parse(init.body) : {};
      role = {
        ...role,
        name: body.name,
        description: body.description ?? null,
        permissions: body.permissions ?? role.permissions,
        version: role.version + 1,
      };
      return jsonResponse(200, role);
    }
    if (url.includes(`/role-definitions/${builtinRole.id}`)) {
      return jsonResponse(200, builtinRole);
    }
    if (url.includes(`/role-definitions/${role.id}`)) {
      return jsonResponse(200, role);
    }
    if (url.includes(`/role-definitions/33333333-3333-3333-3333-333333333333`)) {
      return jsonResponse(200, {
        ...customRole,
        id: "33333333-3333-3333-3333-333333333333",
        code: "NewRole",
        name: "New Role",
      });
    }
    if (url.includes("/authorization/role-definitions")) {
      return jsonResponse(200, pagedJson(options?.listItems ?? [builtinRole, role], 2, 20));
    }
    return jsonResponse(404, {});
  });

  return fetchMock;
}

describe("Platform Roles pages", () => {
  beforeEach(() => {
    window.history.replaceState({}, "", "/admin/platform-roles");
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("fails closed without manage_platform_users", async () => {
    mockRolesFetch({ permissions: ["platform.permission.view_portfolio"] });
    render(<App />);
    expect(await screen.findByRole("heading", { name: /not found/i })).toBeInTheDocument();
    expect(screen.queryByTestId("platform-roles-list-page")).not.toBeInTheDocument();
  });

  it("lists and filters roles", async () => {
    const user = userEvent.setup();
    mockRolesFetch();
    render(<App />);
    expect(await screen.findByTestId("platform-roles-list-page")).toBeInTheDocument();
    expect(await screen.findByText("OpsViewer")).toBeInTheDocument();
    expect(screen.getByText("PlatformAdministrator")).toBeInTheDocument();

    await user.type(screen.getByLabelText(/search/i), "Ops");
    await user.selectOptions(screen.getByLabelText(/^kind$/i), "Custom");
    await user.click(screen.getByRole("button", { name: /^apply$/i }));
    await waitFor(() => {
      expect(window.location.search).toContain("search=Ops");
      expect(window.location.search).toContain("kind=Custom");
    });
  });

  it("creates a custom role and navigates to detail", async () => {
    const user = userEvent.setup();
    mockRolesFetch();
    render(<App />);
    await user.click(await screen.findByTestId("platform-roles-toggle-create"));
    const form = await screen.findByTestId("platform-roles-create-form");
    await user.type(within(form).getByTestId("platform-role-new-code"), "NewRole");
    await user.type(within(form).getByTestId("platform-role-new-name"), "New Role");
    await user.click(within(form).getByRole("checkbox"));
    await user.click(within(form).getByTestId("platform-role-create-submit"));
    expect(await screen.findByTestId("platform-role-detail-page")).toBeInTheDocument();
    expect(window.location.pathname).toContain("/admin/platform-roles/33333333-3333-3333-3333-333333333333");
  });

  it("shows truthful create conflict feedback", async () => {
    const user = userEvent.setup();
    mockRolesFetch({ createFailStatus: 409 });
    render(<App />);
    await user.click(await screen.findByTestId("platform-roles-toggle-create"));
    const form = await screen.findByTestId("platform-roles-create-form");
    await user.type(within(form).getByTestId("platform-role-new-code"), "Dup");
    await user.type(within(form).getByTestId("platform-role-new-name"), "Dup");
    await user.click(within(form).getByTestId("platform-role-create-submit"));
    expect(await screen.findByTestId("platform-roles-create-conflict")).toBeInTheDocument();
    expect(screen.getByText(/code already exists/i)).toBeInTheDocument();
  });

  it("edits permissions and lifecycle for custom roles", async () => {
    const user = userEvent.setup();
    const confirmSpy = vi.spyOn(window, "confirm").mockReturnValue(true);
    mockRolesFetch();
    window.history.replaceState({}, "", `/admin/platform-roles/${customRole.id}`);
    render(<App />);

    expect(await screen.findByTestId("platform-role-detail-page")).toBeInTheDocument();
    expect(screen.getByTestId("platform-roles-pos-warning")).toBeInTheDocument();
    expect(await screen.findByTestId("platform-role-manage")).toBeInTheDocument();

    await user.clear(screen.getByTestId("platform-role-edit-name"));
    await user.type(screen.getByTestId("platform-role-edit-name"), "Ops Viewer 2");
    await user.click(screen.getByTestId("platform-role-save"));
    expect(await screen.findByTestId("platform-role-mutation-success")).toBeInTheDocument();

    await user.click(screen.getByTestId("platform-role-deactivate"));
    expect(confirmSpy).toHaveBeenCalled();
    await waitFor(() => {
      expect(screen.getByTestId("platform-role-activate")).toBeInTheDocument();
    });

    await user.click(screen.getByTestId("platform-role-activate"));
    await waitFor(() => {
      expect(screen.getByTestId("platform-role-deactivate")).toBeInTheDocument();
    });

    await user.click(screen.getByTestId("platform-role-retire"));
    await waitFor(() => {
      expect(screen.queryByTestId("platform-role-manage")).not.toBeInTheDocument();
    });
  });

  it("keeps built-in roles view-only", async () => {
    mockRolesFetch();
    window.history.replaceState({}, "", `/admin/platform-roles/${builtinRole.id}`);
    render(<App />);
    expect(await screen.findByTestId("platform-role-detail-page")).toBeInTheDocument();
    expect(await screen.findByTestId("platform-role-builtin-readonly")).toBeInTheDocument();
    expect(screen.queryByTestId("platform-role-manage")).not.toBeInTheDocument();
  });

  it("shows truthful mutation failure without success banner", async () => {
    const user = userEvent.setup();
    mockRolesFetch({ mutationFail: true });
    window.history.replaceState({}, "", `/admin/platform-roles/${customRole.id}`);
    render(<App />);
    await user.click(await screen.findByTestId("platform-role-save"));
    expect(await screen.findByTestId("platform-role-mutation-error")).toBeInTheDocument();
    expect(screen.getByText(/save failed/i)).toBeInTheDocument();
    expect(screen.queryByTestId("platform-role-mutation-success")).not.toBeInTheDocument();
  });
});
