import { useEffect, useState } from "react";
import QRCode from "qrcode";

type QrCodeImageProps = {
  payload: string;
  label: string;
  testId?: string;
  /** Cap visual size so tablets/desktops do not stretch the QR. */
  maxPx?: number;
};

export function QrCodeImage({
  payload,
  label,
  testId = "qr-code-image",
  maxPx = 240,
}: QrCodeImageProps) {
  const [dataUrl, setDataUrl] = useState<string | null>(null);
  const [error, setError] = useState(false);

  useEffect(() => {
    let cancelled = false;
    setError(false);
    void QRCode.toDataURL(payload, {
      errorCorrectionLevel: "M",
      margin: 2,
      width: maxPx,
      color: { dark: "#111111", light: "#ffffff" },
    })
      .then((url) => {
        if (!cancelled) setDataUrl(url);
      })
      .catch(() => {
        if (!cancelled) {
          setDataUrl(null);
          setError(true);
        }
      });
    return () => {
      cancelled = true;
    };
  }, [payload, maxPx]);

  if (error) {
    return (
      <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]" role="alert">
        Could not render QR.
      </p>
    );
  }

  if (!dataUrl) {
    return (
      <div
        className="mx-auto aspect-square w-full animate-pulse rounded bg-[var(--exits-surface-muted)]"
        style={{ maxWidth: maxPx }}
        aria-hidden
      />
    );
  }

  return (
    <img
      src={dataUrl}
      alt={label}
      data-testid={testId}
      className="mx-auto h-auto w-full rounded border border-[var(--exits-border)] bg-white p-2"
      style={{ maxWidth: maxPx }}
    />
  );
}
