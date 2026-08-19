import { describe, expect, it } from "vitest";
import { getClientRuntime } from "@/adapters/runtime";

describe("runtime adapter", () => {
  it("reports web until a later Capacitor gate", () => {
    expect(getClientRuntime()).toBe("web");
  });
});
