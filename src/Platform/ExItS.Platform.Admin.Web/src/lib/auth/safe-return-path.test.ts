import { describe, expect, it } from "vitest";
import {
  buildLoginPath,
  resolvePostLoginPath,
  sanitizeReturnPath,
} from "@/lib/auth/safe-return-path";

describe("sanitizeReturnPath", () => {
  it("accepts same-origin relative paths", () => {
    expect(sanitizeReturnPath("/")).toBe("/");
    expect(sanitizeReturnPath("/admin/login")).toBeNull();
  });

  it("rejects absolute, protocol-relative, and encoded external URLs", () => {
    expect(sanitizeReturnPath("https://evil.example/x")).toBeNull();
    expect(sanitizeReturnPath("//evil.example")).toBeNull();
    expect(sanitizeReturnPath("https://evil.example")).toBeNull();
    expect(sanitizeReturnPath("/\\evil.example")).toBeNull();
  });

  it("resolves missing or unsafe returns to the foundation route", () => {
    expect(resolvePostLoginPath(null)).toBe("/");
    expect(resolvePostLoginPath("https://evil.example")).toBe("/");
    expect(buildLoginPath({ returnPath: "https://evil.example", notice: "session-expired" })).toBe(
      "/admin/login?notice=session-expired",
    );
  });
});
