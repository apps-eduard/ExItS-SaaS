import QRCode from "qrcode";

async function qrPngDataUrl(payload: string, sizePx = 512): Promise<string> {
  return QRCode.toDataURL(payload, {
    errorCorrectionLevel: "M",
    margin: 2,
    width: sizePx,
    color: { dark: "#111111", light: "#ffffff" },
  });
}

function withPngExtension(filename: string): string {
  return filename.endsWith(".png") ? filename : `${filename}.png`;
}

/** Build a PNG File for Web Share (client-side only; nothing persisted). */
export async function buildQrPngFile(options: {
  payload: string;
  filename: string;
  sizePx?: number;
}): Promise<File> {
  const dataUrl = await qrPngDataUrl(options.payload, options.sizePx);
  const response = await fetch(dataUrl);
  const blob = await response.blob();
  return new File([blob], withPngExtension(options.filename), { type: "image/png" });
}

/** Download a PNG QR for the given payload (client-side only; nothing persisted). */
export async function downloadQrPng(options: {
  payload: string;
  filename: string;
  sizePx?: number;
}): Promise<void> {
  const dataUrl = await qrPngDataUrl(options.payload, options.sizePx);
  const anchor = document.createElement("a");
  anchor.href = dataUrl;
  anchor.download = withPngExtension(options.filename);
  anchor.rel = "noopener";
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
}
