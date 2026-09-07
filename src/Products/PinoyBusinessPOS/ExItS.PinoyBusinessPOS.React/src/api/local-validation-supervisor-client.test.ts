import { describe, expect, it } from "vitest";
import {
  isLocalValidationControlHost,
  LOCAL_VALIDATION_SUPERVISOR_ORIGIN,
} from "@/api/local-validation-supervisor-client";

describe("local-validation-supervisor-client", () => {
  it("uses loopback supervisor origin", () => {
    expect(LOCAL_VALIDATION_SUPERVISOR_ORIGIN).toBe("http://127.0.0.1:8099");
  });

  it("treats localhost and 127.0.0.1 as control hosts", () => {
    expect(isLocalValidationControlHost()).toBe(
      window.location.hostname === "127.0.0.1" ||
        window.location.hostname === "localhost",
    );
  });
});
