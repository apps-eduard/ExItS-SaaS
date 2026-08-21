import { afterEach, describe, expect, it, vi } from "vitest";
import { clearPlatformAntiforgeryToken } from "@/api/platform/platform-http";
import {
  getPersonalProfile,
  listOnboardingBusinessTypes,
  startBusiness,
} from "@/api/platform/start-business-client";

describe("start-business-client", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
  });

  it("lists onboarding business types with PascalCase normalize", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/api/v1/platform/antiforgery/token")) {
          return {
            ok: true,
            status: 200,
            json: async () => ({ headerName: "X-XSRF-TOKEN", token: "csrf" }),
            text: async () => "",
          } as Response;
        }
        expect(url).toContain("/api/v1/personal/onboarding/business-types");
        return {
          ok: true,
          status: 200,
          json: async () => [
            {
              Id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
              Code: "retail",
              Name: "General Retail",
              Description: null,
              Status: "Active",
              SortOrder: 10,
            },
          ],
          text: async () => "",
        } as Response;
      }),
    );

    const types = await listOnboardingBusinessTypes();
    expect(types).toEqual([
      {
        id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
        code: "retail",
        name: "General Retail",
        description: null,
        status: "Active",
        sortOrder: 10,
      },
    ]);
  });

  it("loads personal profile for contact prefill", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () =>
        ({
          ok: true,
          status: 200,
          json: async () => ({
            UserIdentityId: "11111111-1111-1111-1111-111111111111",
            AccountProfileId: "22222222-2222-2222-2222-222222222222",
            Username: "ana",
            DisplayName: "Ana",
            Email: "ana@example.com",
            AccountClass: "Personal",
            Status: "Active",
            Phone: "+639171234567",
          }),
          text: async () => "",
        }) as Response,
      ),
    );

    const profile = await getPersonalProfile();
    expect(profile.email).toBe("ana@example.com");
    expect(profile.phone).toBe("+639171234567");
  });

  it("posts start-business and strips SessionToken from client result", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.includes("/api/v1/platform/antiforgery/token")) {
        return {
          ok: true,
          status: 200,
          json: async () => ({ headerName: "X-XSRF-TOKEN", token: "csrf" }),
          text: async () => "",
        } as Response;
      }
      expect(url).toContain("/api/v1/personal/start-business");
      expect(init?.method).toBe("POST");
      const body = JSON.parse(String(init?.body));
      expect(body.displayName).toBe("Ana Store");
      expect(body.startAsTrial).toBe(true);
      expect(body.payNow).toBe(false);
      return {
        ok: true,
        status: 201,
        json: async () => ({
          OrganizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          MembershipId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
          OrganizationAccountProfileId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          SessionToken: "must-not-leak",
          SessionId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
          AccountClass: "Organization",
          AllowedScope: "Organization",
          SelectedOrganizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          SubscriptionId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
          OrganizationOwnerGranted: true,
          PosEntitlementActivated: true,
          PosOwnerRoleGranted: true,
          ProductCode: "pinoy-business-pos",
          PrimaryBranchId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
        }),
        text: async () => "",
      } as Response;
    });
    vi.stubGlobal("fetch", fetchMock);

    const result = await startBusiness({
      displayName: "Ana Store",
      slug: "ana-store",
      primaryBusinessTypeId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      planKey: "business",
      startAsTrial: true,
      payNow: false,
    });

    expect(result.organizationId).toBe("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    expect(result.subscriptionId).toBe("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    expect(result).not.toHaveProperty("sessionToken");
    expect(JSON.stringify(result)).not.toContain("must-not-leak");
  });
});
