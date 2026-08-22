import { describe, expect, it } from "vitest";
import {
  DEVELOPMENT_OFFLINE_OPERATING_GRANT_PUBLIC_KEY_PEM,
  OfflineGrantVerificationKeyError,
  resolveOfflineOperatingGrantVerificationPublicKeyPemForTests,
} from "@/offline/offline-grant-verification-key";

describe("offline grant verification public key", () => {
  it("uses development default outside production when unset", () => {
    expect(resolveOfflineOperatingGrantVerificationPublicKeyPemForTests()).toBe(
      DEVELOPMENT_OFFLINE_OPERATING_GRANT_PUBLIC_KEY_PEM,
    );
  });

  it("requires explicit production configuration", () => {
    expect(() =>
      resolveOfflineOperatingGrantVerificationPublicKeyPemForTests({ production: true }),
    ).toThrow(OfflineGrantVerificationKeyError);
  });

  it("accepts configured production public key pem", () => {
    const pem = "-----BEGIN PUBLIC KEY-----\nTEST\n-----END PUBLIC KEY-----";
    expect(
      resolveOfflineOperatingGrantVerificationPublicKeyPemForTests({
        production: true,
        configuredPem: pem,
      }),
    ).toBe(pem);
  });
});
