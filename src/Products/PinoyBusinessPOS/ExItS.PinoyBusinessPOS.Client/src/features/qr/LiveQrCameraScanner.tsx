import { useCallback, useEffect, useId, useRef, useState, type RefObject } from "react";
import { Camera, Flashlight, RefreshCw, Upload, Keyboard } from "lucide-react";
import { BottomSheet } from "@/components/exits/SheetDialog";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";
import {
  assertExItsQrPurpose,
  type ExItsQrPurpose,
  ExItsQrParseError,
  type ParsedExItsQr,
} from "@/lib/exits-qr/envelope";
import { qrPurposeMismatchMessageKey } from "@/features/qr/qr-purpose-error";
import {
  isCameraApiAvailable,
  isCameraSecureContext,
  openPreferredCamera,
  stopMediaStream,
} from "@/lib/qr/camera-access";
import { decodeQrFromVideoFrame } from "@/lib/qr/decode-qr-frame";

export type LiveQrScanResult = {
  rawPayload: string;
  parsed: ParsedExItsQr;
};

type CameraUiState =
  | "initial"
  | "requesting"
  | "scanning"
  | "permission_denied"
  | "not_found"
  | "unsupported"
  | "insecure";

type Props = {
  open: boolean;
  expectedPurpose: ExItsQrPurpose;
  onClose: () => void;
  onScan: (result: LiveQrScanResult) => void;
  onUploadFallback?: () => void;
  onManualFallback?: () => void;
};

const SCAN_INTERVAL_MS = 180;

async function waitForVideoElement(
  videoRef: RefObject<HTMLVideoElement | null>,
): Promise<HTMLVideoElement | null> {
  for (let attempt = 0; attempt < 20; attempt += 1) {
    if (videoRef.current) {
      return videoRef.current;
    }
    await new Promise<void>((resolve) => {
      requestAnimationFrame(() => resolve());
    });
  }
  return null;
}

export function LiveQrCameraScanner({
  open,
  expectedPurpose,
  onClose,
  onScan,
  onUploadFallback,
  onManualFallback,
}: Props) {
  const { t } = useI18n();
  const panelId = useId();
  const videoRef = useRef<HTMLVideoElement>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const scanTimerRef = useRef<number | null>(null);
  const scanLockedRef = useRef(false);
  const lastPayloadRef = useRef<string | null>(null);

  const [uiState, setUiState] = useState<CameraUiState>("initial");
  const [inlineError, setInlineError] = useState<string | null>(null);
  const [torchAvailable, setTorchAvailable] = useState(false);
  const [torchOn, setTorchOn] = useState(false);

  const stopCamera = useCallback(() => {
    if (scanTimerRef.current !== null) {
      window.clearInterval(scanTimerRef.current);
      scanTimerRef.current = null;
    }

    const video = videoRef.current;
    if (video) {
      video.srcObject = null;
    }

    stopMediaStream(streamRef.current);
    streamRef.current = null;
    setTorchAvailable(false);
    setTorchOn(false);
  }, []);

  const resetScanLock = useCallback(() => {
    scanLockedRef.current = false;
    lastPayloadRef.current = null;
  }, []);

  const handleClose = useCallback(() => {
    stopCamera();
    resetScanLock();
    setInlineError(null);
    setUiState("initial");
    onClose();
  }, [onClose, resetScanLock, stopCamera]);

  const processPayload = useCallback(
    (rawPayload: string) => {
      if (scanLockedRef.current) {
        return;
      }

      if (lastPayloadRef.current === rawPayload) {
        return;
      }

      lastPayloadRef.current = rawPayload;

      try {
        const parsed = assertExItsQrPurpose(rawPayload, expectedPurpose);
        scanLockedRef.current = true;
        setInlineError(null);
        stopCamera();
        onScan({ rawPayload, parsed });
        handleClose();
      } catch (err) {
        if (err instanceof ExItsQrParseError) {
          if (err.code === "wrong_purpose" || err.code === "unknown_purpose") {
            setInlineError(t(qrPurposeMismatchMessageKey(rawPayload, expectedPurpose, err)));
          } else {
            setInlineError(t("qr.unsupportedExitsQr"));
          }
          return;
        }

        setInlineError(t("qr.unsupportedExitsQr"));
      }
    },
    [expectedPurpose, handleClose, onScan, stopCamera, t],
  );

  const startDecodeLoop = useCallback(() => {
    if (scanTimerRef.current !== null) {
      window.clearInterval(scanTimerRef.current);
    }

    scanTimerRef.current = window.setInterval(() => {
      if (scanLockedRef.current) {
        return;
      }

      const video = videoRef.current;
      if (!video) {
        return;
      }

      void decodeQrFromVideoFrame(video).then((payload) => {
        if (payload) {
          processPayload(payload);
        }
      });
    }, SCAN_INTERVAL_MS);
  }, [processPayload]);

  const startCamera = useCallback(async () => {
    resetScanLock();
    setInlineError(null);
    setUiState("requesting");
    stopCamera();

    if (!isCameraApiAvailable()) {
      setUiState("unsupported");
      return;
    }

    if (!isCameraSecureContext()) {
      setUiState("insecure");
      return;
    }

    const result = await openPreferredCamera();
    if (!result.ok) {
      if (result.reason === "permission_denied") {
        setUiState("permission_denied");
        return;
      }
      if (result.reason === "not_found") {
        setUiState("not_found");
        return;
      }
      if (result.reason === "insecure_context") {
        setUiState("insecure");
        return;
      }
      if (result.reason === "unsupported") {
        setUiState("unsupported");
        return;
      }
      setUiState("not_found");
      return;
    }

    streamRef.current = result.stream;
    const video = await waitForVideoElement(videoRef);
    if (!video) {
      stopCamera();
      setUiState("unsupported");
      return;
    }

    video.srcObject = result.stream;
    try {
      await video.play();
    } catch {
      stopCamera();
      setUiState("unsupported");
      return;
    }

    const track = result.stream.getVideoTracks()[0];
    const capabilities = track?.getCapabilities?.();
    setTorchAvailable(Boolean(capabilities && "torch" in capabilities));
    setUiState("scanning");
    startDecodeLoop();
  }, [resetScanLock, startDecodeLoop, stopCamera]);

  useEffect(() => {
    if (!open) {
      stopCamera();
      resetScanLock();
      setInlineError(null);
      setUiState("initial");
      return;
    }

    return () => {
      stopCamera();
    };
  }, [open, resetScanLock, stopCamera]);

  useEffect(() => {
    if (!open) {
      return;
    }

    function onVisibilityChange() {
      if (document.hidden) {
        if (scanTimerRef.current !== null) {
          window.clearInterval(scanTimerRef.current);
          scanTimerRef.current = null;
        }
      } else if (uiState === "scanning" && !scanLockedRef.current) {
        startDecodeLoop();
      }
    }

    document.addEventListener("visibilitychange", onVisibilityChange);
    return () => document.removeEventListener("visibilitychange", onVisibilityChange);
  }, [open, startDecodeLoop, uiState]);

  async function toggleTorch() {
    const track = streamRef.current?.getVideoTracks()[0];
    if (!track?.applyConstraints) {
      return;
    }

    const next = !torchOn;
    try {
      await track.applyConstraints({ advanced: [{ torch: next }] } as unknown as MediaTrackConstraints);
      setTorchOn(next);
    } catch {
      setTorchAvailable(false);
      setTorchOn(false);
    }
  }

  function renderFallbackActions() {
    return (
      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          variant="ghost"
          className="min-h-11"
          data-testid="live-qr-upload-fallback"
          onClick={() => {
            handleClose();
            onUploadFallback?.();
          }}
        >
          <Upload className="size-4" aria-hidden />
          {t("qr.uploadImage")}
        </Button>
        <Button
          type="button"
          variant="ghost"
          className="min-h-11"
          data-testid="live-qr-manual-fallback"
          onClick={() => {
            handleClose();
            onManualFallback?.();
          }}
        >
          <Keyboard className="size-4" aria-hidden />
          {t("qr.enterId")}
        </Button>
      </div>
    );
  }

  return (
    <BottomSheet
      open={open}
      onClose={handleClose}
      title={t("qr.liveScanTitle")}
      panelId={panelId}
      testId="live-qr-camera-scanner"
      closeLabel={t("qr.closeScanner")}
    >
      <div className="flex min-w-0 flex-col gap-3" data-testid="live-qr-camera-body">
        {uiState === "initial" ? (
          <>
            <p className="m-0 text-[length:var(--exits-text-sm)]">{t("qr.liveScanPrompt")}</p>
            <Button
              type="button"
              className="min-h-11 w-full sm:w-auto"
              data-testid="live-qr-open-camera"
              onClick={() => void startCamera()}
            >
              <Camera className="size-4" aria-hidden />
              {t("qr.openCamera")}
            </Button>
            {renderFallbackActions()}
          </>
        ) : null}

        {uiState === "requesting" || uiState === "scanning" ? (
          <div className="flex min-w-0 flex-col gap-3">
            {uiState === "requesting" ? (
              <p
                className="m-0 text-[length:var(--exits-text-sm)]"
                role="status"
                data-testid="live-qr-requesting"
              >
                {t("qr.startingCamera")}
              </p>
            ) : null}
            <div
              className={
                uiState === "scanning"
                  ? "relative mx-auto aspect-[4/3] w-full max-w-[min(100%,720px)] overflow-hidden rounded-[var(--exits-radius-md)] bg-black"
                  : "pointer-events-none absolute size-px overflow-hidden opacity-0"
              }
              data-testid={uiState === "scanning" ? "live-qr-preview" : "live-qr-video-mount"}
            >
              <video
                ref={videoRef}
                className={uiState === "scanning" ? "size-full object-cover" : "size-px"}
                playsInline
                muted
                autoPlay
                aria-label={uiState === "scanning" ? t("qr.liveScanTitle") : undefined}
                aria-hidden={uiState === "requesting"}
                tabIndex={uiState === "requesting" ? -1 : undefined}
              />
              {uiState === "scanning" ? (
                <div
                  className="pointer-events-none absolute inset-[12%] rounded-[var(--exits-radius-md)] border-2 border-white/80 shadow-[0_0_0_9999px_rgba(0,0,0,0.35)]"
                  aria-hidden
                />
              ) : null}
            </div>
            {uiState === "scanning" ? (
              <>
                <p className="m-0 text-center text-[length:var(--exits-text-xs)] text-muted">
                  {t("qr.scanFrameHint")}
                </p>
                <div className="flex flex-wrap gap-2">
                  {torchAvailable ? (
                    <Button
                      type="button"
                      variant="ghost"
                      className="min-h-11"
                      data-testid="live-qr-torch-toggle"
                      onClick={() => void toggleTorch()}
                    >
                      <Flashlight className="size-4" aria-hidden />
                      {torchOn ? t("qr.torchOff") : t("qr.torchOn")}
                    </Button>
                  ) : null}
                  <Button
                    type="button"
                    variant="ghost"
                    className="min-h-11"
                    data-testid="live-qr-retry-camera"
                    onClick={() => void startCamera()}
                  >
                    <RefreshCw className="size-4" aria-hidden />
                    {t("qr.tryAgain")}
                  </Button>
                </div>
              </>
            ) : null}
          </div>
        ) : null}

        {uiState === "permission_denied" ? (
          <div className="flex flex-col gap-2" role="alert">
            <p className="m-0 text-[length:var(--exits-text-sm)]">{t("qr.permissionDenied")}</p>
            <Button
              type="button"
              className="min-h-11 w-fit"
              data-testid="live-qr-try-again"
              onClick={() => void startCamera()}
            >
              {t("qr.tryAgain")}
            </Button>
            {renderFallbackActions()}
          </div>
        ) : null}

        {uiState === "not_found" ? (
          <div className="flex flex-col gap-2" role="alert">
            <p className="m-0 text-[length:var(--exits-text-sm)]">{t("qr.noCamera")}</p>
            {renderFallbackActions()}
          </div>
        ) : null}

        {uiState === "unsupported" || uiState === "insecure" ? (
          <div className="flex flex-col gap-2" role="alert">
            <p className="m-0 text-[length:var(--exits-text-sm)]">
              {uiState === "insecure" ? t("qr.insecureContext") : t("qr.cameraUnsupported")}
            </p>
            {renderFallbackActions()}
          </div>
        ) : null}

        {inlineError ? (
          <p
            className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
            role="alert"
            data-testid="live-qr-inline-error"
          >
            {inlineError}
          </p>
        ) : null}
      </div>
    </BottomSheet>
  );
}
