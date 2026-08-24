import { useRef, useState } from "react";
import { Camera, Keyboard, Search, Upload, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { useI18n } from "@/i18n/I18nProvider";
import {
  assertExItsQrPurpose,
  type ExItsQrPurpose,
  ExItsQrParseError,
} from "@/lib/exits-qr/envelope";
import { decodeQrFromImageFile } from "@/features/qr/decode-qr-from-image";
import { LiveQrCameraScanner } from "@/features/qr/LiveQrCameraScanner";

type Props = {
  expectedPurpose: ExItsQrPurpose;
  onResolvedPayload: (payload: string) => void;
  disabled?: boolean;
};

export function QrScanOrEnter({ expectedPurpose, onResolvedPayload, disabled }: Props) {
  const { t } = useI18n();
  const fileRef = useRef<HTMLInputElement>(null);
  const manualInputRef = useRef<HTMLInputElement>(null);
  const [manual, setManual] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [scanningFile, setScanningFile] = useState(false);
  const [liveOpen, setLiveOpen] = useState(false);

  function mapSubject(parsedSubject: string, rawPayload: string): string {
    if (expectedPurpose === "pos-device-registration") {
      return rawPayload.trim();
    }

    return parsedSubject;
  }

  function applyRaw(raw: string) {
    try {
      const parsed = assertExItsQrPurpose(raw, expectedPurpose);
      setError(null);
      onResolvedPayload(mapSubject(parsed.subject, raw));
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
    setScanningFile(true);
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
      setScanningFile(false);
      if (fileRef.current) {
        fileRef.current.value = "";
      }
    }
  }

  return (
    <div className="flex min-w-0 flex-col gap-3" data-testid="qr-scan-or-enter">
      <input
        ref={fileRef}
        type="file"
        accept="image/*"
        className="sr-only"
        data-testid="qr-file-input"
        disabled={disabled || scanningFile}
        onChange={(event) => void onFileChange(event.target.files?.[0] ?? null)}
      />
      <ExitsChipBar
        variant="actions"
        ariaLabel={t("qr.scanWithCamera")}
        testId="qr-scan-actions"
        items={[
          {
            key: "camera",
            label: t("qr.scanWithCamera"),
            icon: <Camera />,
            emphasis: "primary",
            testId: "qr-live-camera-button",
            disabled: disabled || scanningFile,
            onSelect: () => setLiveOpen(true),
          },
          {
            key: "upload",
            label: scanningFile ? t("qr.scanning") : t("qr.uploadImage"),
            icon: <Upload />,
            testId: "qr-upload-button",
            disabled: disabled || scanningFile,
            onSelect: () => fileRef.current?.click(),
          },
          {
            key: "clear",
            label: t("qr.clear"),
            icon: <X />,
            testId: "qr-clear-button",
            disabled: Boolean(disabled),
            onSelect: () => {
              setManual("");
              setError(null);
            },
          },
        ]}
      />
      <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t("qr.inputHint")}</p>
      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        <span className="inline-flex items-center gap-1.5">
          <Keyboard className="size-4" aria-hidden />
          {t("qr.enterId")}
        </span>
        <div className="qr-manual-entry flex min-w-0 gap-2">
          <input
            ref={manualInputRef}
            className="min-h-11 min-w-0 flex-1 rounded border border-[var(--exits-border)] bg-transparent px-3 uppercase"
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
          <Button
            type="button"
            variant="outline"
            className="qr-manual-entry__search min-h-11 shrink-0"
            data-testid="qr-manual-submit"
            disabled={Boolean(disabled) || !manual.trim()}
            onClick={() => applyRaw(manual)}
          >
            <Search className="size-4 shrink-0" aria-hidden />
            {t("qr.resolve")}
          </Button>
        </div>
      </label>
      {error ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
          data-testid="qr-error"
          role="alert"
        >
          {error}
        </p>
      ) : null}

      <LiveQrCameraScanner
        open={liveOpen}
        expectedPurpose={expectedPurpose}
        onClose={() => setLiveOpen(false)}
        onScan={(result) => {
          applyRaw(result.rawPayload);
        }}
        onUploadFallback={() => fileRef.current?.click()}
        onManualFallback={() => {
          manualInputRef.current?.focus();
        }}
      />
    </div>
  );
}
