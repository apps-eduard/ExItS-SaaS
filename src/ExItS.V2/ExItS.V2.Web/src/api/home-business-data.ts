import {
  bindWorkspaceWithSessionGrant,
  listEligibleOrganizations,
  listOrganizationBranches,
  type BrowserSessionSnapshot,
} from "./auth";
import { getManagementOverview } from "./pos-home";
import {
  canCallOrganizationPlatformApis,
  isBusinessHomeActivationAllowed,
  sessionAccountClass,
} from "@/session/account-class";
import { type HomeWidget, widgetError } from "./widget";

export type BusinessAttentionItem = {
  id: string;
  kind: "low-stock" | "near-expiry" | "expired" | "utang" | "open-shifts";
  title: string;
  detail: string;
};

export type BusinessHomeData = {
  profile: {
    displayName: string;
    initials: string;
  };
  workspaceLabel?: string;
  today: HomeWidget<{
    salesTotal: number;
    saleCount: number;
    openUtang: number;
    lowStock: number;
    openShifts: number;
    businessDate?: string;
  }>;
  attention: HomeWidget<{ items: BusinessAttentionItem[] }>;
};

function initialsFromName(name: string): string {
  const parts = name.split(/\s+/).filter(Boolean);
  if (parts.length >= 2) {
    return `${parts[0]![0] ?? ""}${parts[1]![0] ?? ""}`.toUpperCase();
  }
  return name.slice(0, 2).toUpperCase() || "?";
}

export async function loadBusinessHome(
  session: BrowserSessionSnapshot,
  signal?: AbortSignal,
): Promise<BusinessHomeData> {
  const displayName =
    session.displayName?.trim() ||
    session.username?.trim() ||
    session.email?.trim() ||
    "Signed in";

  const profile = {
    displayName,
    initials: initialsFromName(displayName),
  };

  // V2 Personal-only gate + account-class guard: never hit org/POS APIs as Personal.
  if (
    !isBusinessHomeActivationAllowed(session) ||
    !canCallOrganizationPlatformApis(session) ||
    sessionAccountClass(session) === "Personal"
  ) {
    const deferred = {
      status: "unavailable" as const,
      message: "Business Home is deferred in this V2 build.",
    };
    return { profile, today: deferred, attention: deferred };
  }

  try {
    let organizationId = session.selectedOrganizationId?.trim() || "";
    let orgName = session.selectedOrganizationDisplayName?.trim() || "";
    if (!organizationId) {
      const orgs = await listEligibleOrganizations(signal);
      if (orgs.length === 0) {
        const unavailable = {
          status: "unavailable" as const,
          message: "No organization membership for business Home.",
        };
        return {
          profile,
          today: unavailable,
          attention: unavailable,
        };
      }
      organizationId = orgs[0]!.organizationId;
      orgName = orgs[0]!.displayName;
    }

    const branches = await listOrganizationBranches(organizationId, signal);
    const active = branches.filter(
      (branch) => !branch.status || branch.status.toLowerCase() === "active",
    );
    const branch =
      active.find((item) => item.isPrimary) ?? active[0] ?? branches[0];
    if (!branch) {
      const unavailable = {
        status: "unavailable" as const,
        message: "No branch is available for this organization.",
      };
      return { profile, today: unavailable, attention: unavailable };
    }

    const bind = await bindWorkspaceWithSessionGrant(
      organizationId,
      branch.id,
      signal,
    );
    if (!bind.ok) {
      const unavailable = {
        status: "unavailable" as const,
        message: bind.detail,
      };
      return { profile, today: unavailable, attention: unavailable };
    }

    const overview = await getManagementOverview(
      { organizationId, branchId: branch.id },
      signal,
    );

    const attentionItems: BusinessAttentionItem[] = [];
    if (overview.lowStockProductCount > 0) {
      attentionItems.push({
        id: "low-stock",
        kind: "low-stock",
        title: "Low stock",
        detail: `${overview.lowStockProductCount} product(s)`,
      });
    }
    if (overview.nearExpiryLotCount > 0) {
      attentionItems.push({
        id: "near-expiry",
        kind: "near-expiry",
        title: "Near expiry",
        detail: `${overview.nearExpiryLotCount} lot(s)`,
      });
    }
    if (overview.expiredLotCount > 0) {
      attentionItems.push({
        id: "expired",
        kind: "expired",
        title: "Expired lots",
        detail: `${overview.expiredLotCount} lot(s)`,
      });
    }
    if (overview.openUtangOutstanding > 0) {
      attentionItems.push({
        id: "utang",
        kind: "utang",
        title: "Open customer utang",
        detail: String(overview.openUtangOutstanding),
      });
    }
    if (overview.openShiftCount > 0) {
      attentionItems.push({
        id: "open-shifts",
        kind: "open-shifts",
        title: "Open shifts",
        detail: `${overview.openShiftCount} open`,
      });
    }

    return {
      profile,
      workspaceLabel: `${orgName || organizationId} · ${branch.name || branch.code}`,
      today: {
        status: overview.todaySalesTotal === 0 ? "empty" : "ready",
        data: {
          salesTotal: overview.todaySalesTotal,
          saleCount: overview.todaySaleCount,
          openUtang: overview.openUtangOutstanding,
          lowStock: overview.lowStockProductCount,
          openShifts: overview.openShiftCount,
          businessDate: overview.businessDate,
        },
      },
      attention: {
        status: attentionItems.length === 0 ? "empty" : "ready",
        data: { items: attentionItems },
      },
    };
  } catch (error) {
    const failed = widgetError(error);
    return { profile, today: failed, attention: failed };
  }
}
