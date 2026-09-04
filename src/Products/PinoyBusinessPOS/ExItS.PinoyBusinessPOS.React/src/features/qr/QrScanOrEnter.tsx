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
import { qrPurposeMismatchMessageKey } from "@/features/qr/qr-purpose-error";

type EntryMode = "manual" | "qr";

type Props = {
  expectedPurpose: ExItsQrPurpose;
  onResolvedPayload: (payload: string) => void;
  disabled?: boolean;
  onManualCleared?: () => void;
  /** Optional override for connect flows that also accept storefront acquisition URLs. */
  parseRawPayload?: (raw: string) => string;
};

export function QrScanOrEnter({
  expectedPurpose,
  onResolvedPayload,
  disabled,
  onManualCleared,
  parseRawPayload,
}: Props) {
  const { t } = useI18n();
  const fileRef = useRef<HTMLInputElement>(null);
  const manualInputRef = useRef<HTMLInputElement>(null);
  const [mode, setMode] = useState<EntryMode>("manual");
  const [manual, setManual] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [scanningFile, setScanningFile] = useState(false);
  const [liveOpen, setLiveOpen] = useState(false);

  function switchMode(next: EntryMode) {
    if (next === mode) return;
    setMode(next);
    setError(null);
    if (next === "manual") {
      setLiveOpen(false);
      window.setTimeout(() => manualInputRef.current?.focus(), 0);
    } else {
      setManual("");
    }
  }

  function mapSubject(parsedSubject: string, rawPayload: string): string {
    if (expectedPurpose === "pos-device-registration") {
      return rawPayload.trim();
    }

    return parsedSubject;
  }

  function applyRaw(raw: string) {
    try {
      if (parseRawPayload) {
        setError(null);
        onResolvedPayload(parseRawPayload(raw));
        return;
      }
      const parsed = assertExItsQrPurpose(raw, expectedPurpose);
      setError(null);
      onResolvedPayload(mapSubject(parsed.subject, raw));
    } catch (err) {
      if (err instanceof ExItsQrParseError) {
        setError(t(qrPurposeMismatchMessageKey(raw, expectedPurpose, err)));
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
    <div className="flex min-w-0 flex-col gap-3" data-testid="qr-scan-or-enter" data-mode={mode}>
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
        variant="filter"
        className="exits-chip-bar--equal"
        ariaLabel={t("qr.modeToggleAria")}
        testId="qr-entry-mode"
        items={[
          {
            key: "manual",
            label: t("qr.modeManual"),
            icon: <Keyboard />,
            state: mode === "manual" ? "active" : "idle",
            testId: "qr-mode-manual",
            disabled: Boolean(disabled),
            onSelect: () => switchMode("manual"),
          },
          {
            key: "qr",
            label: t("qr.modeScan"),
            icon: <Camera />,
            state: mode === "qr" ? "active" : "idle",
            testId: "qr-mode-scan",
            disabled: Boolean(disabled),
            onSelect: () => switchMode("qr"),
          },
        ]}
      />

      {mode === "manual" ? (
        <div className="flex min-w-0 flex-col gap-2" data-testid="qr-manual-panel">
          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t("qr.manualHint")}</p>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span className="inline-flex items-center gap-1.5">
              <Keyboard className="size-4" aria-hidden />
              {t("qr.enterId")}
            </span>
            <div className="qr-manual-entry flex min-w-0 gap-2">
              <div className="qr-manual-entry__field min-w-0 flex-1">
                <input
                  ref={manualInputRef}
                  className="qr-manual-entry__input w-full rounded border border-[var(--exits-border)] bg-transparent px-3 uppercase"
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
                {manual || error ? (
                  <button
                    type="button"
                    className="qr-manual-entry__clear"
                    data-testid="qr-manual-clear"
                    aria-label={t("qr.clear")}
                    disabled={Boolean(disabled)}
                    onClick={() => {
                      setManual("");
                      setError(null);
                      onManualCleared?.();
                      window.setTimeout(() => manualInputRef.current?.focus(), 0);
                    }}
                  >
                    <X className="size-4" aria-hidden />
                  </button>
                ) : null}
              </div>
              <Button
                type="button"
                variant="outline"
                className="qr-manual-entry__search shrink-0"
                data-testid="qr-manual-submit"
                disabled={Boolean(disabled) || !manual.trim()}
                onClick={() => applyRaw(manual)}
              >
                <Search className="size-4 shrink-0" aria-hidden />
                {t("qr.resolve")}
              </Button>
            </div>
          </label>
        </div>
      ) : (
        <div className="flex min-w-0 flex-col gap-2" data-testid="qr-scan-panel">
          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t("qr.scanHint")}</p>
          <ExitsChipBar
            variant="actions"
            className="exits-chip-bar--equal"
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
            ]}
          />
        </div>
      )}

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
          switchMode("manual");
        }}
      />
    </div>
  );
}
