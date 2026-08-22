import { afterEach, describe, expect, it, vi } from "vitest";
import { resolveMailpitConvenienceUrl } from "@/lib/auth/mailpit-url";

describe("resolveMailpitConvenienceUrl", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("uses the current browser hostname and Mailpit UI port 8025", () => {
    expect(resolveMailpitConvenienceUrl("localhost")).toBe("http://localhost:8025");
    expect(resolveMailpitConvenienceUrl("127.0.0.1")).toBe("http://127.0.0.1:8025");
    expect(resolveMailpitConvenienceUrl("100.64.1.20")).toBe("http://100.64.1.20:8025");
  });

  it("does not hardcode a Tailscale address", () => {
    expect(resolveMailpitConvenienceUrl("localhost")).not.toContain("100.120.79.81");
  });

  it("returns null when no hostname is available", () => {
    expect(resolveMailpitConvenienceUrl("")).toBeNull();
    expect(resolveMailpitConvenienceUrl("   ")).toBeNull();
  });
});
