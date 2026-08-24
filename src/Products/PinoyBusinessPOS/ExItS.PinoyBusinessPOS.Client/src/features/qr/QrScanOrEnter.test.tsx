import { describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { I18nProvider } from "@/i18n/I18nProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { buildExItsQr } from "@/lib/exits-qr/envelope";
import { QrScanOrEnter } from "@/features/qr/QrScanOrEnter";
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

function renderInput(
  props: Partial<React.ComponentProps<typeof QrScanOrEnter>> = {},
) {
  const onResolvedPayload = props.onResolvedPayload ?? vi.fn();
  render(
    <PreferencesProvider>
      <I18nProvider>
        <QrScanOrEnter
          expectedPurpose="personal"
          onResolvedPayload={onResolvedPayload}
          {...props}
        />
      </I18nProvider>
    </PreferencesProvider>,
  );
  return { onResolvedPayload };
}

describe("QrScanOrEnter", () => {
  it("defaults to manual entry and toggles QR controls", async () => {
    const user = userEvent.setup();
    renderInput();

    expect(screen.getByTestId("qr-scan-or-enter")).toHaveAttribute("data-mode", "manual");
    expect(screen.getByTestId("qr-manual-panel")).toBeInTheDocument();
    expect(screen.getByTestId("qr-manual-id")).toBeInTheDocument();
    expect(screen.queryByTestId("qr-live-camera-button")).not.toBeInTheDocument();

    await user.click(screen.getByTestId("qr-mode-scan"));

    expect(screen.getByTestId("qr-scan-or-enter")).toHaveAttribute("data-mode", "qr");
    expect(screen.getByTestId("qr-scan-panel")).toBeInTheDocument();
    expect(screen.getByTestId("qr-live-camera-button")).toBeInTheDocument();
    expect(screen.getByTestId("qr-upload-button")).toBeInTheDocument();
    expect(screen.queryByTestId("qr-manual-id")).not.toBeInTheDocument();

    await user.click(screen.getByTestId("qr-mode-manual"));

    expect(screen.getByTestId("qr-manual-id")).toBeInTheDocument();
    expect(screen.queryByTestId("qr-live-camera-button")).not.toBeInTheDocument();
  });

  it("resolves manual Personal ExItS ID", async () => {
    const user = userEvent.setup();
    const { onResolvedPayload } = renderInput();

    await user.type(screen.getByTestId("qr-manual-id"), "EX-4827-1936");
    await user.click(screen.getByTestId("qr-manual-submit"));

    expect(onResolvedPayload).toHaveBeenCalledWith("EX-4827-1936");
  });

  it("resolves manual organization ExItS ID", async () => {
    const user = userEvent.setup();
    const { onResolvedPayload } = renderInput({ expectedPurpose: "organization" });

    await user.type(screen.getByTestId("qr-manual-id"), "org000042");
    await user.click(screen.getByTestId("qr-manual-submit"));

    expect(onResolvedPayload).toHaveBeenCalledWith("ORG000042");
  });

  it("rejects organization QR for personal workflow", async () => {
    const user = userEvent.setup();
    renderInput();

    await user.type(
      screen.getByTestId("qr-manual-id"),
      buildExItsQr("organization", "ORG000123"),
    );
    await user.click(screen.getByTestId("qr-manual-submit"));

    await waitFor(() => {
      expect(screen.getByTestId("qr-error")).toHaveTextContent(
        "This is an organization ExItS ID (ORG…). Only a personal ExItS ID (EX-…) can be added here.",
      );
    });
  });

  it("rejects personal ID for organization workflow", async () => {
    const user = userEvent.setup();
    renderInput({ expectedPurpose: "organization" });

    await user.type(screen.getByTestId("qr-manual-id"), "EX-4827-1936");
    await user.click(screen.getByTestId("qr-manual-submit"));

    await waitFor(() => {
      expect(screen.getByTestId("qr-error")).toHaveTextContent(
        "This is a personal ExItS ID (EX-…). Only an organization ExItS ID (ORG…) is allowed here.",
      );
    });
  });

  it("resolves Personal QR from live camera scan", async () => {
    HTMLVideoElement.prototype.play = vi.fn().mockResolvedValue(undefined);
    const stream = {
      getTracks: () => [],
      getVideoTracks: () => [],
    } as unknown as MediaStream;
    const payload = buildExItsQr("personal", "EX-4827-1936");

    vi.mocked(cameraAccess.openPreferredCamera).mockResolvedValue({
      ok: true,
      stream,
      facingMode: "environment",
    });
    vi.mocked(decodeFrame.decodeQrFromVideoFrame).mockResolvedValue(payload);

    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    vi.useFakeTimers({ shouldAdvanceTime: true });
    const { onResolvedPayload } = renderInput();

    await user.click(screen.getByTestId("qr-mode-scan"));
    await user.click(screen.getByTestId("qr-live-camera-button"));
    await user.click(screen.getByTestId("live-qr-open-camera"));
    await vi.advanceTimersByTimeAsync(200);

    await waitFor(() => {
      expect(onResolvedPayload).toHaveBeenCalledWith("EX-4827-1936");
    });
    vi.useRealTimers();
  });
});
