import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { I18nProvider } from "@/i18n/I18nProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { buildExItsQr } from "@/lib/exits-qr/envelope";
import { LiveQrCameraScanner } from "@/features/qr/LiveQrCameraScanner";
import * as cameraAccess from "@/lib/qr/camera-access";
import * as decodeFrame from "@/lib/qr/decode-qr-frame";

vi.mock("@/lib/qr/camera-access", async (importOriginal) => {
  const actual = await importOriginal<typeof cameraAccess>();
  return {
    ...actual,
    isCameraApiAvailable: vi.fn(() => true),
    isCameraSecureContext: vi.fn(() => true),
    openPreferredCamera: vi.fn(),
    stopMediaStream: vi.fn(),
  };
});

vi.mock("@/lib/qr/decode-qr-frame", async (importOriginal) => {
  const actual = await importOriginal<typeof decodeFrame>();
  return {
    ...actual,
    decodeQrFromVideoFrame: vi.fn(),
  };
});

function createMockTrack() {
  return {
    stop: vi.fn(),
    getCapabilities: vi.fn(() => ({})),
    applyConstraints: vi.fn().mockResolvedValue(undefined),
  } as unknown as MediaStreamTrack;
}

function renderScanner(
  props: Partial<React.ComponentProps<typeof LiveQrCameraScanner>> = {},
) {
  const onScan = props.onScan ?? vi.fn();
  const onClose = props.onClose ?? vi.fn();
  render(
    <PreferencesProvider>
      <I18nProvider>
        <LiveQrCameraScanner
          open
          expectedPurpose="personal"
          onClose={onClose}
          onScan={onScan}
          onUploadFallback={props.onUploadFallback}
          onManualFallback={props.onManualFallback}
          {...props}
        />
      </I18nProvider>
    </PreferencesProvider>,
  );
  return { onScan, onClose };
}

describe("LiveQrCameraScanner", () => {
  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    HTMLVideoElement.prototype.play = vi.fn().mockResolvedValue(undefined);
    vi.mocked(cameraAccess.isCameraApiAvailable).mockReturnValue(true);
    vi.mocked(cameraAccess.isCameraSecureContext).mockReturnValue(true);
    vi.mocked(cameraAccess.openPreferredCamera).mockReset();
    vi.mocked(cameraAccess.stopMediaStream).mockReset();
    vi.mocked(decodeFrame.decodeQrFromVideoFrame).mockReset();
    vi.mocked(decodeFrame.decodeQrFromVideoFrame).mockResolvedValue(null);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("shows permission denied with fallback controls", async () => {
    vi.mocked(cameraAccess.openPreferredCamera).mockResolvedValue({
      ok: false,
      reason: "permission_denied",
    });

    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderScanner();

    expect(await screen.findByText("Camera access is blocked.")).toBeInTheDocument();
    expect(screen.getByTestId("live-qr-upload-fallback")).toBeInTheDocument();
    expect(screen.getByTestId("live-qr-manual-fallback")).toBeInTheDocument();
    expect(screen.getByTestId("live-qr-try-again")).toBeInTheDocument();
  });

  it("shows no-camera state with fallback controls", async () => {
    vi.mocked(cameraAccess.openPreferredCamera).mockResolvedValue({
      ok: false,
      reason: "not_found",
    });

    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderScanner();

    expect(await screen.findByText("No camera is available on this device.")).toBeInTheDocument();
    expect(screen.getByTestId("live-qr-upload-fallback")).toBeInTheDocument();
  });

  it("shows unsupported browser state", async () => {
    vi.mocked(cameraAccess.isCameraApiAvailable).mockReturnValue(false);

    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderScanner();

    expect(
      await screen.findByText("Live scanning isn't supported in this browser."),
    ).toBeInTheDocument();
  });

  it("accepts a canonical Personal QR and stops the stream", async () => {
    const track = createMockTrack();
    const stream = {
      getTracks: () => [track],
      getVideoTracks: () => [track],
    } as unknown as MediaStream;
    const payload = buildExItsQr("personal", "EX-4827-1936");
    const onScan = vi.fn();

    vi.mocked(cameraAccess.openPreferredCamera).mockResolvedValue({
      ok: true,
      stream,
      facingMode: "environment",
    });
    vi.mocked(decodeFrame.decodeQrFromVideoFrame).mockResolvedValue(payload);

    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderScanner({ onScan });
    await waitFor(() => expect(screen.getByTestId("live-qr-preview")).toBeInTheDocument());

    await vi.advanceTimersByTimeAsync(200);

    await waitFor(() => {
      expect(onScan).toHaveBeenCalledTimes(1);
    });
    expect(onScan.mock.calls[0]?.[0]).toEqual(
      expect.objectContaining({ rawPayload: payload, parsed: expect.objectContaining({ purpose: "personal" }) }),
    );
    expect(cameraAccess.stopMediaStream).toHaveBeenCalledWith(stream);
  });

  it("accepts legacy Personal QR payloads", async () => {
    const track = createMockTrack();
    const stream = {
      getTracks: () => [track],
      getVideoTracks: () => [track],
    } as unknown as MediaStream;
    const payload = "exits://user/v1/EX-4827-1936";
    const onScan = vi.fn();

    vi.mocked(cameraAccess.openPreferredCamera).mockResolvedValue({
      ok: true,
      stream,
      facingMode: "environment",
    });
    vi.mocked(decodeFrame.decodeQrFromVideoFrame).mockResolvedValue(payload);

    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderScanner({ onScan });
    await vi.advanceTimersByTimeAsync(200);

    await waitFor(() => expect(onScan).toHaveBeenCalledTimes(1));
  });

  it("rejects wrong-purpose QR without closing the scanner", async () => {
    const track = createMockTrack();
    const stream = {
      getTracks: () => [track],
      getVideoTracks: () => [track],
    } as unknown as MediaStream;
    const onScan = vi.fn();

    vi.mocked(cameraAccess.openPreferredCamera).mockResolvedValue({
      ok: true,
      stream,
      facingMode: "environment",
    });
    vi.mocked(decodeFrame.decodeQrFromVideoFrame).mockResolvedValue(
      buildExItsQr("organization", "ORG000123"),
    );

    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderScanner({ onScan });
    await vi.advanceTimersByTimeAsync(200);

    expect(await screen.findByTestId("live-qr-inline-error")).toHaveTextContent(
      "This is an organization ExItS ID (ORG…). Only a personal ExItS ID (EX-…) can be added here.",
    );
    expect(onScan).not.toHaveBeenCalled();
    expect(screen.getByTestId("live-qr-preview")).toBeInTheDocument();
  });

  it("rejects malformed QR payloads and keeps scanning", async () => {
    const track = createMockTrack();
    const stream = {
      getTracks: () => [track],
      getVideoTracks: () => [track],
    } as unknown as MediaStream;
    const onScan = vi.fn();

    vi.mocked(cameraAccess.openPreferredCamera).mockResolvedValue({
      ok: true,
      stream,
      facingMode: "environment",
    });
    vi.mocked(decodeFrame.decodeQrFromVideoFrame).mockResolvedValue("https://evil.example/qr");

    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderScanner({ onScan });
    await vi.advanceTimersByTimeAsync(200);

    expect(await screen.findByTestId("live-qr-inline-error")).toHaveTextContent(
      "That's not a supported ExItS QR code.",
    );
    expect(onScan).not.toHaveBeenCalled();
  });

  it("processes duplicate detections only once", async () => {
    const track = createMockTrack();
    const stream = {
      getTracks: () => [track],
      getVideoTracks: () => [track],
    } as unknown as MediaStream;
    const payload = buildExItsQr("personal", "EX-4827-1936");
    const onScan = vi.fn();

    vi.mocked(cameraAccess.openPreferredCamera).mockResolvedValue({
      ok: true,
      stream,
      facingMode: "environment",
    });
    vi.mocked(decodeFrame.decodeQrFromVideoFrame).mockResolvedValue(payload);

    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderScanner({ onScan });
    await vi.advanceTimersByTimeAsync(600);

    expect(onScan).toHaveBeenCalledTimes(1);
  });

  it("stops tracks when the scanner closes", async () => {
    const track = createMockTrack();
    const stream = {
      getTracks: () => [track],
      getVideoTracks: () => [track],
    } as unknown as MediaStream;

    vi.mocked(cameraAccess.openPreferredCamera).mockResolvedValue({
      ok: true,
      stream,
      facingMode: "environment",
    });

    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    const { onClose } = renderScanner();
    await waitFor(() => expect(screen.getByTestId("live-qr-preview")).toBeInTheDocument());
    await user.click(screen.getByRole("button", { name: "Close" }));

    expect(cameraAccess.stopMediaStream).toHaveBeenCalledWith(stream);
    expect(onClose).toHaveBeenCalled();
  });

  it("stops tracks on unmount", async () => {
    const track = createMockTrack();
    const stream = {
      getTracks: () => [track],
      getVideoTracks: () => [track],
    } as unknown as MediaStream;

    vi.mocked(cameraAccess.openPreferredCamera).mockResolvedValue({
      ok: true,
      stream,
      facingMode: "environment",
    });

    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    const { unmount } = render(
      <PreferencesProvider>
        <I18nProvider>
          <LiveQrCameraScanner open expectedPurpose="personal" onClose={vi.fn()} onScan={vi.fn()} />
        </I18nProvider>
      </PreferencesProvider>,
    );
    await waitFor(() => expect(screen.getByTestId("live-qr-preview")).toBeInTheDocument());
    unmount();

    expect(cameraAccess.stopMediaStream).toHaveBeenCalledWith(stream);
  });

  it("accepts organization QR only in organization workflow", async () => {
    const track = createMockTrack();
    const stream = {
      getTracks: () => [track],
      getVideoTracks: () => [track],
    } as unknown as MediaStream;
    const payload = buildExItsQr("organization", "ORG000123");
    const onScan = vi.fn();

    vi.mocked(cameraAccess.openPreferredCamera).mockResolvedValue({
      ok: true,
      stream,
      facingMode: "environment",
    });
    vi.mocked(decodeFrame.decodeQrFromVideoFrame).mockResolvedValue(payload);

    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(
      <PreferencesProvider>
        <I18nProvider>
          <LiveQrCameraScanner
            open
            expectedPurpose="organization"
            onClose={vi.fn()}
            onScan={onScan}
          />
        </I18nProvider>
      </PreferencesProvider>,
    );
    await vi.advanceTimersByTimeAsync(200);

    await waitFor(() => expect(onScan).toHaveBeenCalledTimes(1));
  });

  it("accepts device registration QR only in device workflow", async () => {
    const track = createMockTrack();
    const stream = {
      getTracks: () => [track],
      getVideoTracks: () => [track],
    } as unknown as MediaStream;
    const payload = buildExItsQr("pos-device-registration", "opaque-token");
    const onScan = vi.fn();

    vi.mocked(cameraAccess.openPreferredCamera).mockResolvedValue({
      ok: true,
      stream,
      facingMode: "environment",
    });
    vi.mocked(decodeFrame.decodeQrFromVideoFrame).mockResolvedValue(payload);

    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(
      <PreferencesProvider>
        <I18nProvider>
          <LiveQrCameraScanner
            open
            expectedPurpose="pos-device-registration"
            onClose={vi.fn()}
            onScan={onScan}
          />
        </I18nProvider>
      </PreferencesProvider>,
    );
    await vi.advanceTimersByTimeAsync(200);

    await waitFor(() => expect(onScan).toHaveBeenCalledTimes(1));
  });
});
