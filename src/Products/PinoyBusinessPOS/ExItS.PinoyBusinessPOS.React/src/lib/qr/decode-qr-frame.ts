import jsQR from "jsqr";

/** Max canvas width for live decode — balances detection vs CPU/battery. */
export const LIVE_QR_DECODE_MAX_WIDTH = 640;

export async function decodeQrFromImageData(imageData: ImageData): Promise<string | null> {
  const Detector = (
    globalThis as unknown as {
      BarcodeDetector?: new (options?: { formats: string[] }) => {
        detect: (source: ImageBitmap) => Promise<Array<{ rawValue?: string }>>;
      };
    }
  ).BarcodeDetector;

  if (typeof Detector === "function") {
    try {
      const bitmap = await createImageBitmap(imageData);
      try {
        const detector = new Detector({ formats: ["qr_code"] });
        const codes = await detector.detect(bitmap);
        const value = codes.find((c) => c.rawValue)?.rawValue?.trim();
        if (value) {
          return value;
        }
      } finally {
        bitmap.close();
      }
    } catch {
      // Fall through to jsQR.
    }
  }

  const decoded = jsQR(imageData.data, imageData.width, imageData.height);
  return decoded?.data?.trim() || null;
}

export function captureVideoFrame(
  video: HTMLVideoElement,
  maxWidth = LIVE_QR_DECODE_MAX_WIDTH,
): ImageData | null {
  if (video.readyState < HTMLMediaElement.HAVE_CURRENT_DATA) {
    return null;
  }

  const sourceWidth = video.videoWidth;
  const sourceHeight = video.videoHeight;
  if (!sourceWidth || !sourceHeight) {
    return null;
  }

  const scale = sourceWidth > maxWidth ? maxWidth / sourceWidth : 1;
  const width = Math.max(1, Math.round(sourceWidth * scale));
  const height = Math.max(1, Math.round(sourceHeight * scale));

  const canvas = document.createElement("canvas");
  canvas.width = width;
  canvas.height = height;
  const ctx = canvas.getContext("2d", { willReadFrequently: true });
  if (!ctx) {
    return null;
  }

  ctx.drawImage(video, 0, 0, width, height);
  return ctx.getImageData(0, 0, width, height);
}

export async function decodeQrFromVideoFrame(
  video: HTMLVideoElement,
  maxWidth = LIVE_QR_DECODE_MAX_WIDTH,
): Promise<string | null> {
  const testHook = readLiveDecodeTestHook();
  if (testHook) {
    return testHook(video);
  }

  const frame = captureVideoFrame(video, maxWidth);
  if (!frame) {
    return null;
  }

  return decodeQrFromImageData(frame);
}

export type LiveQrDecodeTestHook = (video: HTMLVideoElement) => Promise<string | null> | string | null;

let liveDecodeTestHook: LiveQrDecodeTestHook | null = null;

/** Vitest / Playwright harness — never set in production UI paths. */
export function setLiveQrDecodeTestHook(hook: LiveQrDecodeTestHook | null): void {
  liveDecodeTestHook = hook;
}

function readLiveDecodeTestHook(): LiveQrDecodeTestHook | null {
  if (liveDecodeTestHook) {
    return liveDecodeTestHook;
  }

  const globalHook = (
    globalThis as unknown as {
      __EXITS_LIVE_QR_DECODE__?: LiveQrDecodeTestHook;
    }
  ).__EXITS_LIVE_QR_DECODE__;

  return globalHook ?? null;
}
