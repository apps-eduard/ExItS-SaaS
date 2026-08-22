import { useRef, useState } from "react";
import { Camera, Keyboard, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";
import {
  assertExItsQrPurpose,
  type ExItsQrPurpose,
  ExItsQrParseError,
} from "@/lib/exits-qr/envelope";
import { decodeQrFromImageFile } from "@/features/qr/decode-qr-from-image";

type Props = {
  expectedPurpose: ExItsQrPurpose;
  onResolvedPayload: (payload: string) => void;
  disabled?: boolean;
};

export function QrScanOrEnter({ expectedPurpose, onResolvedPayload, disabled }: Props) {
  const { t } = useI18n();
  const fileRef = useRef<HTMLInputElement>(null);
  const [manual, setManual] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [scanning, setScanning] = useState(false);

  function applyRaw(raw: string) {
    try {
      const parsed = assertExItsQrPurpose(raw, expectedPurpose);
      setError(null);
      onResolvedPayload(
        expectedPurpose === "personal"
          ? parsed.subject
          : expectedPurpose === "organization"
            ? parsed.subject
            : raw.trim(),
      );
    } catch (err) {
      if (err instanceof ExItsQrParseError) {
        setError(err.code === "unknown_purpose" ? t("qr.wrongPurpose") : t("qr.invalidPayload"));
        return;
      }
      setError(t("qr.invalidPayload"));
    }
  }

  async function onFileChange(file: File | null) {
    if (!file) return;
    setScanning(true);
    setError(null);
    try {
      const payload = await decodeQrFromImageFile(file);
      if (!payload) {
        setError(t("qr.decodeFailed"));
        return;
      }
      applyRaw(payload);
    } catch {
      setError(t("qr.cameraUnavailable"));
    } finally {
      setScanning(false);
      if (fileRef.current) fileRef.current.value = "";
    }
  }

  return (
    <div className="flex min-w-0 flex-col gap-3" data-testid="qr-scan-or-enter">
      <input
        ref={fileRef}
        type="file"
        accept="image/*"
        capture="environment"
        className="sr-only"
        data-testid="qr-file-input"
        disabled={disabled || scanning}
        onChange={(event) => void onFileChange(event.target.files?.[0] ?? null)}
      />
      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          className="min-h-11"
          data-testid="qr-scan-button"
          disabled={disabled || scanning}
          onClick={() => fileRef.current?.click()}
        >
          <Camera className="size-4" aria-hidden />
          {scanning ? t("qr.scanning") : t("qr.scan")}
        </Button>
        <Button
          type="button"
          variant="ghost"
          className="min-h-11"
          data-testid="qr-clear-button"
          disabled={disabled}
          onClick={() => {
            setManual("");
            setError(null);
          }}
        >
          <X className="size-4" aria-hidden />
          {t("qr.clear")}
        </Button>
      </div>
      <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t("qr.cameraHint")}</p>
      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        <span className="inline-flex items-center gap-1.5">
          <Keyboard className="size-4" aria-hidden />
          {t("qr.enterId")}
        </span>
        <input
          className="min-h-11 rounded border border-[var(--exits-border)] bg-transparent px-3 uppercase"
          data-testid="qr-manual-id"
          value={manual}
          disabled={disabled}
          placeholder={expectedPurpose === "personal" ? "EX-4827-1936" : "ORG000001"}
          onChange={(event) => setManual(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === "Enter") {
              event.preventDefault();
              applyRaw(manual);
            }
          }}
        />
      </label>
      <Button
        type="button"
        variant="ghost"
        className="min-h-11 w-fit"
        data-testid="qr-manual-submit"
        disabled={disabled || !manual.trim()}
        onClick={() => applyRaw(manual)}
      >
        {t("qr.resolve")}
      </Button>
      {error ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
          data-testid="qr-error"
          role="alert"
        >
          {error}
        </p>
      ) : null}
    </div>
  );
}
