import { describe, expect, it } from "vitest";
import {
  isLocalValidationControlHost,
  LOCAL_VALIDATION_SUPERVISOR_ORIGIN,
  LOCAL_VALIDATION_SUPERVISOR_PROXY_PREFIX,
  resolveLocalValidationProfile,
  resolveSupervisorBaseUrl,
} from "@/api/local-validation-supervisor-client";

describe("local-validation-supervisor-client", () => {
  it("uses loopback supervisor origin and Vite proxy prefix", () => {
    expect(LOCAL_VALIDATION_SUPERVISOR_ORIGIN).toBe("http://127.0.0.1:8099");
    expect(LOCAL_VALIDATION_SUPERVISOR_PROXY_PREFIX).toBe("/__dev__/lv-supervisor");
    expect(resolveSupervisorBaseUrl()).toBe(LOCAL_VALIDATION_SUPERVISOR_PROXY_PREFIX);
  });

  it("uses the fast-dev profile only for the local login port", () => {
    expect(resolveLocalValidationProfile("5178")).toBe("local-dev");
    expect(resolveLocalValidationProfile("5177")).toBe("validation");
    expect(resolveLocalValidationProfile("8095")).toBe("validation");
  });

  it("treats localhost and 127.0.0.1 as control hosts", () => {
    expect(isLocalValidationControlHost()).toBe(
      window.location.hostname === "127.0.0.1" ||
        window.location.hostname === "localhost",
    );
  });
});
