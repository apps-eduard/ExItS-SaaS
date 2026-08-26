import { describe, expect, it } from "vitest";
import {
  formatBytes,
  formatBytesPair,
  formatCpuPercent,
  formatDuration,
  formatLatency,
  formatRatioPercent,
} from "@/features/system-health/system-health-format";

describe("system health formatting", () => {
  it("formats memory as 6.2 GB / 16 GB", () => {
    expect(formatBytes(6_657_199_309)).toBe("6.2 GB");
    expect(formatBytes(17_179_869_184)).toBe("16 GB");
    expect(formatBytesPair(6_657_199_309, 17_179_869_184)).toBe("6.2 GB / 16 GB");
  });

  it("formats storage as 132 GB / 250 GB and percent", () => {
    expect(formatBytesPair(141_733_920_768, 268_435_456_000)).toBe("132 GB / 250 GB");
    expect(formatRatioPercent(141_733_920_768, 268_435_456_000)).toBe("53%");
    expect(formatRatioPercent(24, 100)).toBe("24%");
  });

  it("formats uptime as 18d 7h", () => {
    expect(formatDuration(18 * 86400 + 7 * 3600)).toBe("18d 7h");
  });

  it("formats latency as 21 ms", () => {
    expect(formatLatency(21)).toBe("21 ms");
  });

  it("formats CPU percent", () => {
    expect(formatCpuPercent(12.4)).toBe("12.4%");
  });

  it("uses em dash for unknown metrics", () => {
    expect(formatBytesPair(null, null)).toBe("—");
    expect(formatRatioPercent(null, 16)).toBe("—");
    expect(formatCpuPercent(null)).toBe("—");
    expect(formatDuration(null)).toBe("—");
    expect(formatLatency(null)).toBe("—");
  });
});
