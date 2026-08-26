import jsQR from "jsqr";

/**
 * Still-image QR decode for PWA (file/camera capture).
 * Live viewfinder is optional when BarcodeDetector exists; manual ID always remains available.
 */
export async function decodeQrFromImageFile(file: File): Promise<string | null> {
  const bitmap = await createImageBitmap(file);
  try {
    const canvas = document.createElement("canvas");
    canvas.width = bitmap.width;
    canvas.height = bitmap.height;
    const ctx = canvas.getContext("2d");
    if (!ctx) return null;
    ctx.drawImage(bitmap, 0, 0);
    const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);

    const Detector = (
      globalThis as unknown as {
        BarcodeDetector?: new (options?: { formats: string[] }) => {
          detect: (source: ImageBitmap) => Promise<Array<{ rawValue?: string }>>;
        };
      }
    ).BarcodeDetector;

    if (typeof Detector === "function") {
      try {
        const detector = new Detector({ formats: ["qr_code"] });
        const codes = await detector.detect(bitmap);
        const value = codes.find((c) => c.rawValue)?.rawValue?.trim();
        if (value) return value;
      } catch {
        // Fall through to jsQR.
      }
    }

    const decoded = jsQR(imageData.data, imageData.width, imageData.height);
    return decoded?.data?.trim() || null;
  } finally {
    bitmap.close();
  }
}
