import { describe, expect, it } from "vitest";
import { resolveHandoffDestination } from "@/features/auth/session-establish-destination";

describe("resolveHandoffDestination", () => {
  it("sends a Personal ticket to /personal", () => {
    expect(
      resolveHandoffDestination({
        targetApp: "personal",
        accountClass: "Personal",
        serverReturnPath: "/personal",
      }),
    ).toEqual({ ok: true, path: "/personal" });
  });

  it("sends an Organization ticket to the organization return path", () => {
    expect(
      resolveHandoffDestination({
        targetApp: "organization",
        accountClass: "Organization",
        serverReturnPath: "/",
      }),
    ).toEqual({ ok: true, path: "/" });
  });

  it("rejects a platform or unknown target before a session is adopted", () => {
    expect(
      resolveHandoffDestination({
        targetApp: "platform",
        accountClass: "Platform",
        queryReturnPath: "/personal",
      }),
    ).toEqual({ ok: false, reason: "wrong-target" });
  });

  it("rejects a target that does not match the server account class", () => {
    expect(
      resolveHandoffDestination({
        targetApp: "personal",
        accountClass: "Organization",
        queryReturnPath: "/personal",
      }),
    ).toEqual({ ok: false, reason: "class-mismatch" });
  });

  it("drops an unsafe or cross-target return path", () => {
    expect(
      resolveHandoffDestination({
        targetApp: "personal",
        accountClass: "Personal",
        queryReturnPath: "https://evil.example/personal",
        serverReturnPath: "/personal",
      }),
    ).toEqual({ ok: true, path: "/personal" });
    expect(
      resolveHandoffDestination({
        targetApp: "organization",
        accountClass: "Organization",
        queryReturnPath: "/personal",
      }),
    ).toEqual({ ok: true, path: "/" });
    expect(
      resolveHandoffDestination({
        targetApp: "personal",
        accountClass: "Personal",
        queryReturnPath: "//evil.example",
      }),
    ).toEqual({ ok: true, path: "/personal" });
  });
});
