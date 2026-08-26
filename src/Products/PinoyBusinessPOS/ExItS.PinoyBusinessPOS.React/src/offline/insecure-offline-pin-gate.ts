import { isWebCryptoSubtleAvailable, isWebCryptoSecureContext } from "@/lib/web-crypto-capability";

/**
 * DEV-ONLY: allow Offline PIN enroll/unlock when Web Crypto is unavailable
 * (e.g. Tailscale HTTP on a physical device). Never active in production builds.
 *
 * Requires ALL of:
 * - Vite DEV (`import.meta.env.DEV`)
 * - `VITE_ALLOW_INSECURE_OFFLINE_PIN=true`
 * - insecure context OR missing crypto.subtle
 */
export function isInsecureOfflinePinFallbackAllowed(): boolean {
  if (typeof import.meta === "undefined") {
    return false;
  }
  // Production / preview builds: DEV is false — fail closed even if the Vite flag was set at build.
  if (import.meta.env.DEV !== true) {
    return false;
  }
  if (import.meta.env.PROD === true) {
    return false;
  }
  if (import.meta.env.MODE === "production") {
    return false;
  }
  if (import.meta.env.VITE_ALLOW_INSECURE_OFFLINE_PIN !== "true") {
    return false;
  }
  if (typeof window === "undefined") {
    return false;
  }
  const insecureContext = !isWebCryptoSecureContext();
  const subtleMissing = !isWebCryptoSubtleAvailable();
  return insecureContext || subtleMissing;
}

/** True when the UI should show the insecure-development warning. */
export function shouldShowInsecureOfflinePinWarning(): boolean {
  return isInsecureOfflinePinFallbackAllowed();
}
