import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { decodeQrFromVideoFrame, setLiveQrDecodeTestHook } from "@/lib/qr/decode-qr-frame";

describe("decode-qr-frame", () => {
  beforeEach(() => {
    setLiveQrDecodeTestHook(null);
  });

  afterEach(() => {
    setLiveQrDecodeTestHook(null);
  });

  it("uses the live decode test hook without logging payloads", async () => {
    const logSpy = vi.spyOn(console, "log").mockImplementation(() => undefined);
    const hook = vi.fn(async () => "exits://qr/v1/personal/EX-4827-1936");
    setLiveQrDecodeTestHook(hook);

    const video = document.createElement("video");
    const payload = await decodeQrFromVideoFrame(video);

    expect(payload).toBe("exits://qr/v1/personal/EX-4827-1936");
    expect(hook).toHaveBeenCalledWith(video);
    expect(logSpy).not.toHaveBeenCalledWith(expect.stringContaining("EX-4827-1936"));
    logSpy.mockRestore();
  });

  it("returns null when video frame is not ready", async () => {
    const video = document.createElement("video");
    Object.defineProperty(video, "readyState", { configurable: true, value: 0 });
    Object.defineProperty(video, "videoWidth", { configurable: true, value: 0 });
    Object.defineProperty(video, "videoHeight", { configurable: true, value: 0 });

    await expect(decodeQrFromVideoFrame(video)).resolves.toBeNull();
  });
});
