export type CameraFacing = "environment" | "user" | "unknown";

export type CameraStartFailure =
  | "insecure_context"
  | "unsupported"
  | "permission_denied"
  | "not_found"
  | "error";

export type CameraStartResult =
  | { ok: true; stream: MediaStream; facingMode: CameraFacing }
  | { ok: false; reason: CameraStartFailure };

export function isCameraSecureContext(): boolean {
  return typeof window !== "undefined" && window.isSecureContext;
}

export function isCameraApiAvailable(): boolean {
  return typeof navigator !== "undefined" && Boolean(navigator.mediaDevices?.getUserMedia);
}

export function stopMediaStream(stream: MediaStream | null | undefined): void {
  if (!stream) {
    return;
  }

  for (const track of stream.getTracks()) {
    track.stop();
  }
}

function domErrorReason(error: unknown): CameraStartFailure {
  if (error instanceof DOMException) {
    if (error.name === "NotAllowedError" || error.name === "PermissionDeniedError") {
      return "permission_denied";
    }
    if (error.name === "NotFoundError" || error.name === "DevicesNotFoundError") {
      return "not_found";
    }
  }

  return "error";
}

async function getUserMediaVideo(
  constraints: MediaTrackConstraints,
): Promise<MediaStream> {
  return navigator.mediaDevices.getUserMedia({
    video: constraints,
    audio: false,
  });
}

/**
 * Prefer rear/environment camera; fall back to any available camera.
 */
export async function openPreferredCamera(): Promise<CameraStartResult> {
  if (!isCameraApiAvailable()) {
    return { ok: false, reason: "unsupported" };
  }

  if (!isCameraSecureContext()) {
    return { ok: false, reason: "insecure_context" };
  }

  try {
    const stream = await getUserMediaVideo({ facingMode: { ideal: "environment" } });
    return { ok: true, stream, facingMode: "environment" };
  } catch (environmentError) {
    const environmentReason = domErrorReason(environmentError);
    if (environmentReason === "permission_denied") {
      return { ok: false, reason: "permission_denied" };
    }

    try {
      const stream = await getUserMediaVideo({});
      return { ok: true, stream, facingMode: "unknown" };
    } catch (fallbackError) {
      return { ok: false, reason: domErrorReason(fallbackError) };
    }
  }
}
