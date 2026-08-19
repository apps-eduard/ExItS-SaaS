import { describe, expect, it } from "vitest";
import {
  compactGuid,
  fallbackHumanizeCode,
  presentAuditAction,
  presentAuditActor,
  presentAuditType,
} from "@/lib/audit/audit-presentation";
import { translate } from "@/lib/i18n/messages";

const t = (key: Parameters<typeof translate>[1]) => translate("en", key);

describe("audit presentation", () => {
  it("maps known action codes and keeps the raw value", () => {
    expect(presentAuditAction("platform.auth.login_succeeded", t)).toEqual({
      label: "Signed in",
      raw: "platform.auth.login_succeeded",
    });
    expect(presentAuditAction("platform.auth.login_failed", t)).toEqual({
      label: "Sign-in failed",
      raw: "platform.auth.login_failed",
    });
    expect(presentAuditAction("platform.auth.logout", t)).toEqual({
      label: "Signed out",
      raw: "platform.auth.logout",
    });
  });

  it("maps known types and humanizes unknown codes conservatively", () => {
    expect(presentAuditType("PlatformAuthSession", t)).toEqual({
      label: "Authentication session",
      raw: "PlatformAuthSession",
    });
    expect(presentAuditType("PlatformUser", t).label).toBe("Platform user");
    expect(fallbackHumanizeCode("platform.access.checked")).toBe("Platform access checked");
    expect(presentAuditAction("platform.access.checked", t).raw).toBe("platform.access.checked");
  });

  it("compacts platform-user GUIDs without inventing a name", () => {
    const raw = "platform-user:89535ae2-1234-5678-9abc-def0123e987a";
    const presented = presentAuditActor(raw, t);
    expect(presented.label).toBe("Platform user");
    expect(presented.detail).toBe(compactGuid("89535ae2-1234-5678-9abc-def0123e987a"));
    expect(presented.raw).toBe(raw);
    expect(presentAuditActor("olivia@example.test", t)).toEqual({
      label: "olivia@example.test",
      raw: "olivia@example.test",
    });
  });
});
