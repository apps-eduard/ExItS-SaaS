import { describe, expect, it } from "vitest";
import {
  allDamagedDraft,
  allSellableDraft,
  balanceFromConfirmed,
  balanceFromRecovered,
  emptyInspectDraft,
  parseInspectDisposition,
} from "@/features/inventory/inventory-inspect-disposition";

describe("inventory-inspect-disposition", () => {
  it("starts empty so confirm cannot silently go all-damaged", () => {
    const draft = emptyInspectDraft();
    expect(parseInspectDisposition(draft, 5).ok).toBe(false);
  });

  it("all sellable / all damaged allocate the full qty", () => {
    expect(parseInspectDisposition(allSellableDraft(5), 5)).toEqual({
      ok: true,
      recovered: 5,
      confirmed: 0,
    });
    expect(parseInspectDisposition(allDamagedDraft(5), 5)).toEqual({
      ok: true,
      recovered: 0,
      confirmed: 5,
    });
  });

  it("balances the other side when one qty is edited", () => {
    expect(balanceFromRecovered(10, "4")).toEqual({ recovered: "4", confirmed: "6" });
    expect(balanceFromConfirmed(10, "3")).toEqual({ recovered: "7", confirmed: "3" });
  });

  it("rejects unbalanced or partial drafts", () => {
    expect(parseInspectDisposition({ recovered: "4", confirmed: "4" }, 10).ok).toBe(false);
    expect(parseInspectDisposition({ recovered: "4", confirmed: "" }, 4).ok).toBe(false);
  });
});
