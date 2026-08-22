import { describe, expect, it } from "vitest";
import {
  assertExItsQrPurpose,
  buildExItsQr,
  ExItsQrParseError,
  parseExItsQr,
} from "@/lib/exits-qr/envelope";

describe("exits-qr envelope", () => {
  it("builds and parses personal QR without secrets", () => {
    const payload = buildExItsQr("personal", "ex-4827-1936");
    expect(payload).toBe("exits://qr/v1/personal/EX-4827-1936");
    expect(payload).not.toMatch(/@/);
    expect(payload).not.toMatch(/token/i);
    const parsed = parseExItsQr(payload);
    expect(parsed).toEqual({ purpose: "personal", subject: "EX-4827-1936", version: 1 });
  });

  it("builds and parses organization QR", () => {
    const payload = buildExItsQr("organization", "org000123");
    expect(payload).toBe("exits://qr/v1/organization/ORG000123");
    expect(parseExItsQr(payload).purpose).toBe("organization");
  });

  it("accepts legacy personal form and bare public IDs", () => {
    expect(parseExItsQr("exits://user/v1/EX-4827-1936").purpose).toBe("personal");
    expect(parseExItsQr("EX-4827-1936").subject).toBe("EX-4827-1936");
    expect(parseExItsQr("ORG000001").purpose).toBe("organization");
  });

  it("rejects wrong purpose and device registration for personal flows", () => {
    expect(() =>
      assertExItsQrPurpose(buildExItsQr("organization", "ORG000001"), "personal"),
    ).toThrow(ExItsQrParseError);
    expect(() =>
      assertExItsQrPurpose(buildExItsQr("pos-device-registration", "opaque-token"), "personal"),
    ).toThrow(ExItsQrParseError);
  });

  it("rejects malformed payloads", () => {
    expect(() => parseExItsQr("https://evil.example/qr")).toThrow(ExItsQrParseError);
    expect(() => parseExItsQr("")).toThrow(ExItsQrParseError);
  });
});
