import type { MessageKey } from "@/i18n/messages";
import {
  ExItsQrParseError,
  type ExItsQrPurpose,
  parseExItsQr,
} from "@/lib/exits-qr/envelope";

/**
 * Maps QR parse / purpose mismatches to a clear user-facing message key.
 * Personal People flows reject organization IDs; org-ID entry rejects personal IDs.
 */
export function qrPurposeMismatchMessageKey(
  rawPayload: string,
  expectedPurpose: ExItsQrPurpose,
  err: ExItsQrParseError,
): MessageKey {
  if (err.code === "wrong_purpose" || err.code === "unknown_purpose") {
    try {
      const received = err.receivedPurpose ?? parseExItsQr(rawPayload).purpose;
      if (expectedPurpose === "personal" && received === "organization") {
        return "qr.organizationNotAllowedHere";
      }
      if (expectedPurpose === "organization" && received === "personal") {
        return "qr.personalNotAllowedHere";
      }
    } catch {
      // fall through to generic wrong-purpose copy
    }
    return "qr.wrongPurpose";
  }

  return "qr.invalidPayload";
}
