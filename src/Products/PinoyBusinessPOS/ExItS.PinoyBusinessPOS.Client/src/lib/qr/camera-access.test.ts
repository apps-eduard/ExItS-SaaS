import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  isCameraApiAvailable,
  isCameraSecureContext,
  openPreferredCamera,
  stopMediaStream,
} from "@/lib/qr/camera-access";

function createMockTrack() {
  return {
    stop: vi.fn(),
    getCapabilities: vi.fn(() => ({})),
  } as unknown as MediaStreamTrack;
}

function createMockStream(track = createMockTrack()) {
  return {
    getTracks: () => [track],
    getVideoTracks: () => [track],
  } as unknown as MediaStream;
}

describe("camera-access", () => {
  const getUserMedia = vi.fn();

  beforeEach(() => {
    getUserMedia.mockReset();
    vi.stubGlobal("navigator", {
      mediaDevices: { getUserMedia },
    });
    Object.defineProperty(window, "isSecureContext", {
      configurable: true,
      value: true,
    });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("reports secure context and API availability", () => {
    expect(isCameraSecureContext()).toBe(true);
    expect(isCameraApiAvailable()).toBe(true);
  });

  it("requests environment camera first", async () => {
    const stream = createMockStream();
    getUserMedia.mockResolvedValueOnce(stream);

    const result = await openPreferredCamera();

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.facingMode).toBe("environment");
      expect(result.stream).toBe(stream);
    }
    expect(getUserMedia).toHaveBeenCalledWith({
      video: { facingMode: { ideal: "environment" } },
      audio: false,
    });
  });

  it("falls back when environment camera is unavailable", async () => {
    const stream = createMockStream();
    getUserMedia
      .mockRejectedValueOnce(new DOMException("not found", "NotFoundError"))
      .mockResolvedValueOnce(stream);

    const result = await openPreferredCamera();

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.facingMode).toBe("unknown");
    }
    expect(getUserMedia).toHaveBeenNthCalledWith(1, {
      video: { facingMode: { ideal: "environment" } },
      audio: false,
    });
    expect(getUserMedia).toHaveBeenNthCalledWith(2, {
      video: {},
      audio: false,
    });
  });

  it("returns permission_denied without fallback retry", async () => {
    getUserMedia.mockRejectedValueOnce(new DOMException("denied", "NotAllowedError"));

    const result = await openPreferredCamera();

    expect(result).toEqual({ ok: false, reason: "permission_denied" });
    expect(getUserMedia).toHaveBeenCalledTimes(1);
  });

  it("returns not_found when no camera is available", async () => {
    getUserMedia
      .mockRejectedValueOnce(new DOMException("not found", "NotFoundError"))
      .mockRejectedValueOnce(new DOMException("not found", "NotFoundError"));

    const result = await openPreferredCamera();

    expect(result).toEqual({ ok: false, reason: "not_found" });
  });

  it("returns unsupported when getUserMedia is unavailable", async () => {
    vi.stubGlobal("navigator", {});

    const result = await openPreferredCamera();

    expect(result).toEqual({ ok: false, reason: "unsupported" });
  });

  it("returns insecure_context outside a secure context", async () => {
    Object.defineProperty(window, "isSecureContext", {
      configurable: true,
      value: false,
    });

    const result = await openPreferredCamera();

    expect(result).toEqual({ ok: false, reason: "insecure_context" });
    expect(getUserMedia).not.toHaveBeenCalled();
  });

  it("stops every track in a stream", () => {
    const track = createMockTrack();
    stopMediaStream(createMockStream(track));
    expect(track.stop).toHaveBeenCalledTimes(1);
  });
});
