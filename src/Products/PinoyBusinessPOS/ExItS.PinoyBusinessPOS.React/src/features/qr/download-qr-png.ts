import QRCode from "qrcode";

/** Download a PNG QR for the given payload (client-side only; nothing persisted). */
export async function downloadQrPng(options: {
  payload: string;
  filename: string;
  sizePx?: number;
}): Promise<void> {
  const dataUrl = await QRCode.toDataURL(options.payload, {
    errorCorrectionLevel: "M",
    margin: 2,
    width: options.sizePx ?? 512,
    color: { dark: "#111111", light: "#ffffff" },
  });
  const anchor = document.createElement("a");
  anchor.href = dataUrl;
  anchor.download = options.filename.endsWith(".png")
    ? options.filename
    : `${options.filename}.png`;
  anchor.rel = "noopener";
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
}
