import { describe, expect, it } from "vitest";
import {
  UTANG_OWNERSHIP_MINE,
  UTANG_OWNERSHIP_SHARED,
  UTANG_READ_ONLY_CHIP,
} from "@/features/personal/utang/utang-ownership-ui";

describe("utang ownership UI color standard", () => {
  it("locks My record / Shared with me / Read only colors for familiarization", () => {
    expect(UTANG_OWNERSHIP_MINE).toMatchObject({
      metricTone: "emphasis",
      chipTone: "primary",
      appearance: "emphasis",
      shape: "square",
    });
    expect(UTANG_OWNERSHIP_SHARED).toMatchObject({
      metricTone: "shared",
      chipTone: "info",
      appearance: "emphasis",
      shape: "square",
    });
    expect(UTANG_READ_ONLY_CHIP).toMatchObject({
      tone: "warning",
      appearance: "emphasis",
      shape: "square",
    });
  });
});
