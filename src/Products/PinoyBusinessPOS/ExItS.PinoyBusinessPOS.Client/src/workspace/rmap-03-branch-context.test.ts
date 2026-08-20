import { describe, expect, it } from "vitest";
import {
  DEFERRED_POS_DEVICE_CONTEXT,
  isPosDeviceReadyForMoney,
} from "@/workspace/pos-device-context";
import {
  buildAccessibleWorkspaces,
  resolveWorkspaceRoutingPlan,
} from "@/workspace/workspace-resolver";
import type { PlatformBranch } from "@/api/platform/platform-auth-client";

describe("RMAP-03 branch / device context", () => {
  it("does not invent an authorized POS device", () => {
    expect(DEFERRED_POS_DEVICE_CONTEXT.status).toBe("deferred");
    expect(DEFERRED_POS_DEVICE_CONTEXT.installationDeviceId).toBeNull();
    expect(isPosDeviceReadyForMoney(DEFERRED_POS_DEVICE_CONTEXT)).toBe(false);
  });

  it("routes zero accessible Active branches to NoAccessibleBranch", () => {
    const plan = resolveWorkspaceRoutingPlan({
      organizationCount: 1,
      workspaces: [],
      accountClass: "Organization",
    });
    expect(plan.outcome).toBe("NoAccessibleBranch");
  });

  it("defers single Active branch auto-select to destination probe", () => {
    const branches = new Map<string, PlatformBranch[]>([
      [
        "org-1",
        [
          {
            id: "branch-1",
            organizationId: "org-1",
            code: "MAIN",
            name: "Main",
            isPrimary: true,
            status: "Active",
          },
        ],
      ],
    ]);
    const workspaces = buildAccessibleWorkspaces(
      [{ organizationId: "org-1", displayName: "Store" }],
      branches,
    );
    const plan = resolveWorkspaceRoutingPlan({
      organizationCount: 1,
      workspaces,
      accountClass: "Organization",
    });
    expect(plan.outcome).toBe("ShowChooser");
  });

  it("shows chooser for multiple Active branches", () => {
    const branches = new Map<string, PlatformBranch[]>([
      [
        "org-1",
        [
          {
            id: "b1",
            organizationId: "org-1",
            code: "A",
            name: "A",
            isPrimary: true,
            status: "Active",
          },
          {
            id: "b2",
            organizationId: "org-1",
            code: "B",
            name: "B",
            isPrimary: false,
            status: "Active",
          },
        ],
      ],
    ]);
    const workspaces = buildAccessibleWorkspaces(
      [{ organizationId: "org-1", displayName: "Store" }],
      branches,
    );
    const plan = resolveWorkspaceRoutingPlan({
      organizationCount: 1,
      workspaces,
      accountClass: "Organization",
    });
    expect(plan.outcome).toBe("ShowChooser");
  });

  it("ignores Inactive branches for accessibility", () => {
    const branches = new Map<string, PlatformBranch[]>([
      [
        "org-1",
        [
          {
            id: "inactive",
            organizationId: "org-1",
            code: "X",
            name: "Closed",
            isPrimary: true,
            status: "Suspended",
          },
        ],
      ],
    ]);
    const workspaces = buildAccessibleWorkspaces(
      [{ organizationId: "org-1", displayName: "Store" }],
      branches,
    );
    expect(workspaces).toHaveLength(0);
  });
});
