import { afterEach, describe, expect, it, vi } from "vitest";
import type { BrowserSessionSnapshot } from "./auth";

const listEligibleOrganizations = vi.fn();
const listOrganizationBranches = vi.fn();
const bindWorkspaceWithSessionGrant = vi.fn();
const getManagementOverview = vi.fn();

vi.mock("./auth", async () => {
  const actual = await vi.importActual<typeof import("./auth")>("./auth");
  return {
    ...actual,
    listEligibleOrganizations: (...args: unknown[]) =>
      listEligibleOrganizations(...args),
    listOrganizationBranches: (...args: unknown[]) =>
      listOrganizationBranches(...args),
    bindWorkspaceWithSessionGrant: (...args: unknown[]) =>
      bindWorkspaceWithSessionGrant(...args),
  };
});

vi.mock("./pos-home", () => ({
  getManagementOverview: (...args: unknown[]) => getManagementOverview(...args),
}));

afterEach(() => {
  vi.clearAllMocks();
  vi.resetModules();
});

describe("loadBusinessHome Personal-only guard", () => {
  it("does not call organization or POS APIs for a Personal session", async () => {
    const { loadBusinessHome } = await import("./home-business-data");
    const session: BrowserSessionSnapshot = {
      userId: "u1",
      accountClass: "Personal",
      displayName: "Kizy",
      selectedOrganizationId: "11111111-1111-1111-1111-111111111111",
      organizationContextLocked: false,
    };

    const result = await loadBusinessHome(session);

    expect(listEligibleOrganizations).not.toHaveBeenCalled();
    expect(listOrganizationBranches).not.toHaveBeenCalled();
    expect(bindWorkspaceWithSessionGrant).not.toHaveBeenCalled();
    expect(getManagementOverview).not.toHaveBeenCalled();
    expect(result.today.status).toBe("unavailable");
    expect(result.attention.status).toBe("unavailable");
  });

  it("does not call organization APIs for Organization sessions while Business Home is deferred", async () => {
    const { loadBusinessHome } = await import("./home-business-data");
    const session: BrowserSessionSnapshot = {
      userId: "u2",
      accountClass: "Organization",
      displayName: "Owner Org",
      selectedOrganizationId: "22222222-2222-2222-2222-222222222222",
    };

    const result = await loadBusinessHome(session);

    expect(listEligibleOrganizations).not.toHaveBeenCalled();
    expect(listOrganizationBranches).not.toHaveBeenCalled();
    expect(bindWorkspaceWithSessionGrant).not.toHaveBeenCalled();
    expect(getManagementOverview).not.toHaveBeenCalled();
    expect(result.today.status).toBe("unavailable");
  });
});
