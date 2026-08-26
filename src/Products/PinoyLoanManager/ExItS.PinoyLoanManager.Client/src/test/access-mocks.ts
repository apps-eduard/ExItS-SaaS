import { expect, vi } from "vitest";
import { jsonResponse } from "@/test/render";

export const ORG_ALLOWED = {
  organizationId: "11111111-1111-4111-8111-111111111111",
  displayName: "ABC Sari-Sari Store",
  slug: "abc-sari-sari",
  membershipRole: "OrganizationOwner",
  membershipId: "22222222-2222-4222-8222-222222222222",
};

export const ORG_DENIED = {
  organizationId: "33333333-3333-4333-8333-333333333333",
  displayName: "XYZ Mini Grocery",
  slug: "xyz-mini-grocery",
  membershipRole: "OrganizationOwner",
  membershipId: "44444444-4444-4444-8444-444444444444",
};

type AccessMockOptions = {
  signedIn?: boolean;
  accountClass?: string;
  selectedOrganizationId?: string | null;
  displayName?: string;
  username?: string;
  organizations?: (typeof ORG_ALLOWED)[];
  productAccess?: { allowed: boolean; reasonCode?: string; subscriptionStatus?: string | null };
  organizationsStatus?: number;
  productAccessStatus?: number;
};

export function stubAccessFetch(options: AccessMockOptions = {}) {
  let signedIn = options.signedIn ?? true;
  const accountClass = options.accountClass ?? "Organization";
  const selectedOrganizationId =
    options.selectedOrganizationId === undefined
      ? ORG_ALLOWED.organizationId
      : options.selectedOrganizationId;
  const organizations = options.organizations ?? [ORG_ALLOWED];
  const productAccess = options.productAccess ?? { allowed: true, reasonCode: "allowed" };

  vi.stubGlobal("fetch", (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);

    if (url.includes("/antiforgery/token")) {
      return jsonResponse(200, {
        headerName: "X-XSRF-TOKEN",
        token: "test-csrf-token",
      });
    }

    if (url.includes("/local-validation/enabled")) {
      return jsonResponse(200, false);
    }

    if (url.includes("/auth/me")) {
      if (!signedIn) {
        return jsonResponse(401, { errorCode: "application.auth.session_invalid" });
      }
      return jsonResponse(200, {
        username: options.username ?? "maria.santos",
        displayName: options.displayName ?? "Maria Santos",
        accountClass,
        selectedOrganizationId,
        organizationSelectionState: selectedOrganizationId ? "Selected" : "SelectionRequired",
      });
    }

    if (url.includes("/auth/login")) {
      signedIn = true;
      return jsonResponse(200, {
        username: options.username ?? "maria.santos",
        displayName: options.displayName ?? "Maria Santos",
        accountClass,
        selectedOrganizationId,
        sessionToken: "must-not-escape",
      });
    }

    if (url.includes("/auth/logout")) {
      expect(new Headers(init?.headers).get("X-XSRF-TOKEN")).toBe("test-csrf-token");
      signedIn = false;
      return jsonResponse(204, null);
    }

    if (url.includes("/auth/organizations")) {
      if ((options.organizationsStatus ?? 200) !== 200) {
        return jsonResponse(options.organizationsStatus ?? 500, {
          errorCode: "application.auth.session_invalid",
        });
      }
      return jsonResponse(200, organizations);
    }

    if (url.includes("/auth/organization-context")) {
      expect(init?.method).toBe("PUT");
      expect(new Headers(init?.headers).get("X-XSRF-TOKEN")).toBe("test-csrf-token");
      const body = JSON.parse(String(init?.body)) as { organizationId?: string };
      expect(body.organizationId).toBeTruthy();
      return jsonResponse(200, {
        selectedOrganizationId: body.organizationId,
        organizationSelectionState: "Selected",
      });
    }

    if (url.includes("/auth/product-access/effective")) {
      expect(url).not.toMatch(/[?&]userId=/i);
      expect(url).not.toMatch(/[?&]organizationId=/i);
      if ((options.productAccessStatus ?? 200) !== 200) {
        return jsonResponse(options.productAccessStatus ?? 500, {
          errorCode: "application.auth.session_invalid",
        });
      }
      return jsonResponse(200, {
        allowed: productAccess.allowed,
        reasonCode:
          productAccess.reasonCode ??
          (productAccess.allowed ? "allowed" : "product_assignment_missing"),
        userId: "55555555-5555-4555-8555-555555555555",
        organizationId: selectedOrganizationId ?? ORG_ALLOWED.organizationId,
        productCode: "pinoy-loan-manager",
        subscriptionStatus: productAccess.subscriptionStatus ?? null,
      });
    }

    if (url.includes("/auth/account-profiles")) {
      return jsonResponse(200, []);
    }

    return jsonResponse(404, null);
  });
}
