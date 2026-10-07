import { describe, expect, it } from "vitest";
import {
  buildSessionEstablishUrl,
  chooseAutomaticWorkspace,
  choosePostAdminLoginWorkspace,
  decideProductEntry,
  planWorkspaceLaunch,
  readExternalCallbackQuery,
  resolveExperienceOrigins,
} from "@/lib/auth/cutover-routing";

const platform = { app: "platform", label: "Platform Admin", organizationId: null, organizationName: null, roleLabel: "Admin" };
const personal = { app: "personal", label: "Personal", organizationId: null, organizationName: null, roleLabel: null };
const organization = {
  app: "organization",
  label: "North Store",
  organizationId: "org-1",
  organizationName: "North Store",
  roleLabel: "Owner",
};

describe("admin cutover routing", () => {
  it("sends a non-platform account to POS when an organization exists, otherwise Personal", () => {
    expect(choosePostAdminLoginWorkspace([platform])).toBeNull();
    expect(choosePostAdminLoginWorkspace([personal, organization])?.app).toBe("organization");
    expect(choosePostAdminLoginWorkspace([personal, organization], "org-1")?.organizationId).toBe("org-1");
    expect(choosePostAdminLoginWorkspace([personal])?.app).toBe("personal");
    const preview = resolveExperienceOrigins({
      localValidation: true,
      pageHost: "admin.exitsapps.com",
    });
    expect(preview.personal).toBe("https://my.exitsapps.com");
    expect(preview.organization).toBe("https://pos.exitsapps.com");
  });

  it("sends a platform user to Platform Admin", () => {
    expect(chooseAutomaticWorkspace([platform])?.app).toBe("platform");
    expect(planWorkspaceLaunch(platform, {}).kind).toBe("platform");
  });

  it("sends a personal user to the React personal origin", () => {
    const origins = resolveExperienceOrigins({
      personal: "http://127.0.0.1:5177",
      localValidation: false,
    });
    const plan = planWorkspaceLaunch(personal, origins);
    expect(plan.kind).toBe("handoff");
    if (plan.kind === "handoff") {
      expect(plan.origin).toBe("http://127.0.0.1:5177");
      expect(plan.returnPath).toBe("/personal");
      expect(plan.targetApp).toBe("personal");
    }
  });

  it("sends an organization user to the React organization origin", () => {
    const plan = planWorkspaceLaunch(organization, {
      organization: "http://127.0.0.1:5177",
    });
    expect(plan.kind).toBe("handoff");
    if (plan.kind === "handoff") {
      expect(buildSessionEstablishUrl(plan.origin, "ticket-1", plan.returnPath)).toBe(
        "http://127.0.0.1:5177/session/establish?ticket=ticket-1&returnPath=%2F",
      );
    }
  });

  it("rejects unsafe establish origins and open return URLs", () => {
    expect(buildSessionEstablishUrl("javascript:alert(1)", "ticket", "/")).toBeNull();
    expect(buildSessionEstablishUrl("https://evil.example", "ticket", "https://evil.example")).toBe(
      "https://evil.example/session/establish?ticket=ticket&returnPath=%2F",
    );
    const unsafe = readExternalCallbackQuery(new URLSearchParams("sessionToken=abc&return=https://evil.example"));
    expect(unsafe.unsafeReturn).toBe(true);
    expect(unsafe.returnPath).toBeNull();
    const safe = readExternalCallbackQuery(new URLSearchParams("sessionToken=abc&return=%2Fadmin"));
    expect(safe.unsafeReturn).toBe(false);
    expect(safe.returnPath).toBe("/admin");
  });

  it("allows product entry only when the server grants access", () => {
    expect(
      decideProductEntry({
        accessToken: "secret-token",
        expiresAtUtc: "2026-10-04T00:00:00Z",
        productCode: "pinoy-business-pos",
        productAccessAllowed: true,
        productAccessReasonCode: null,
      }).status,
    ).toBe("allowed");
    const denied = decideProductEntry({
      accessToken: "secret-token",
      expiresAtUtc: "2026-10-04T00:00:00Z",
      productCode: "pinoy-business-pos",
      productAccessAllowed: false,
      productAccessReasonCode: "entitlement_missing",
    });
    expect(denied.status).toBe("denied");
    if (denied.status === "denied") {
      expect(denied.reasonCode).toBe("entitlement_missing");
      expect(denied).not.toHaveProperty("accessToken");
    }
  });
});
