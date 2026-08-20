import { describe, expect, it } from "vitest";
import { stripSecureFlagFromSetCookie } from "./vite.proxy-cookie";

describe("vite proxy cookie rewrite", () => {
  it("strips Secure so HTTP emulator origins can store the session cookie", () => {
    const raw =
      ".ExItS.Platform.Auth=token; expires=Thu, 20 Aug 2026 11:12:19 GMT; path=/; secure; samesite=lax; httponly";
    expect(stripSecureFlagFromSetCookie(raw)).toBe(
      ".ExItS.Platform.Auth=token; expires=Thu, 20 Aug 2026 11:12:19 GMT; path=/; samesite=lax; httponly",
    );
  });

  it("leaves non-Secure cookies unchanged", () => {
    const raw = ".ExItS.Platform.Auth=token; path=/; samesite=lax; httponly";
    expect(stripSecureFlagFromSetCookie(raw)).toBe(raw);
  });
});
