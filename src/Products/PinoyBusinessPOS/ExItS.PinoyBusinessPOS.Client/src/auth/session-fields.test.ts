import { describe, expect, it } from "vitest";
import { omitSessionToken, readSessionSnapshot } from "@/auth/session-fields";
import { collectWebStorageAuthHits } from "@/auth/web-storage-guard";

describe("session fields", () => {
  it("strips sessionToken before any client snapshot is kept", () => {
    const payload = omitSessionToken({
      sessionToken: "reusable-secret",
      SessionToken: "also-secret",
      sessionId: "11111111-1111-4111-8111-111111111111",
      userId: "22222222-2222-4222-8222-222222222222",
      username: "maria.santos",
      displayName: "Maria Santos",
      email: "maria.santos@exits.local",
    });
    expect(payload.sessionToken).toBeUndefined();
    expect(payload.SessionToken).toBeUndefined();
    expect(readSessionSnapshot(payload)?.displayName).toBe("Maria Santos");
  });

  it("does not treat UI preferences as an auth token store", () => {
    window.localStorage.setItem(
      "exits.mobile-client.ui-preferences.v1",
      JSON.stringify({ theme: "light" }),
    );
    expect(collectWebStorageAuthHits()).toEqual([]);
  });
});
