import { describe, expect, it, vi } from "vitest";
import { resolveAmbiguousMutationOutcome } from "@/runtime/ambiguous-mutation-outcome";

describe("resolveAmbiguousMutationOutcome", () => {
  it("passes through non-network failures without lookup", async () => {
    const lookup = vi.fn();
    const outcome = await resolveAmbiguousMutationOutcome({
      error: Object.assign(new Error("validation"), { status: 400 }),
      lookup,
    });
    expect(outcome.kind).toBe("not_network");
    expect(lookup).not.toHaveBeenCalled();
  });

  it("confirms when lookup succeeds after network loss", async () => {
    const outcome = await resolveAmbiguousMutationOutcome({
      error: new TypeError("Failed to fetch"),
      lookup: async () => ({ id: "ok" }),
    });
    expect(outcome).toEqual({ kind: "confirmed", value: { id: "ok" } });
  });

  it("reports still_unknown when lookup also fails as network", async () => {
    const outcome = await resolveAmbiguousMutationOutcome({
      error: new TypeError("Failed to fetch"),
      lookup: async () => {
        throw new TypeError("Failed to fetch");
      },
    });
    expect(outcome.kind).toBe("still_unknown");
  });

  it("reports not_found when lookup reaches the server with an app error", async () => {
    const outcome = await resolveAmbiguousMutationOutcome({
      error: new TypeError("Failed to fetch"),
      lookup: async () => {
        throw Object.assign(new Error("missing"), { status: 404 });
      },
    });
    expect(outcome.kind).toBe("not_found");
  });
});
