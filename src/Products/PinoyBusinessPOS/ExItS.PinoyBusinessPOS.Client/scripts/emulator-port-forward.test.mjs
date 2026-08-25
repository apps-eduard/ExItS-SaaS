import { describe, expect, it } from "vitest";
import {
  formatPosDevStartupLines,
  parseAdbDevices,
  POS_DEV_CANONICAL_URL,
} from "./emulator-port-forward.mjs";

describe("emulator port forward", () => {
  it("parses adb devices output", () => {
    const output = [
      "List of devices attached",
      "emulator-5554\tdevice",
      "emulator-5556\toffline",
      "",
    ].join("\n");

    expect(parseAdbDevices(output)).toEqual([
      { serial: "emulator-5554", state: "device" },
      { serial: "emulator-5556", state: "offline" },
    ]);
  });

  it("prints canonical emulator URL when reverse is active", () => {
    const lines = formatPosDevStartupLines({
      adbFound: true,
      port: 5177,
      url: POS_DEV_CANONICAL_URL,
      reversed: ["emulator-5554"],
    });

    expect(lines).toEqual([
      "[pos-dev] Desktop:  http://127.0.0.1:5177",
      "[pos-dev] Emulator: http://127.0.0.1:5177  (adb reverse active on emulator-5554)",
    ]);
  });

  it("prints skip reason when adb is missing", () => {
    const lines = formatPosDevStartupLines({
      adbFound: false,
      port: 5177,
      url: POS_DEV_CANONICAL_URL,
      reversed: [],
      skippedReason: "adb-not-found",
    });

    expect(lines[1]).toContain("adb reverse skipped — adb not installed");
  });
});
