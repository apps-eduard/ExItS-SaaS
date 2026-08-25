/**
 * Canonical ExItS QR envelope (v1) — mirrors Platform ExItsQrEnvelope.
 * Do not encode secrets, email, phone, or internal UUIDs.
 */

export type ExItsQrPurpose = "personal" | "organization" | "pos-device-registration";

export type ParsedExItsQr = {
  purpose: ExItsQrPurpose;
  subject: string;
  version: number;
};

export class ExItsQrParseError extends Error {
  readonly code:
    | "empty"
    | "unrecognized"
    | "malformed"
    | "unknown_purpose"
    | "wrong_purpose"
    | "invalid_subject";
  readonly receivedPurpose?: ExItsQrPurpose;

  constructor(
    code: ExItsQrParseError["code"],
    message: string,
    receivedPurpose?: ExItsQrPurpose,
  ) {
    super(message);
    this.name = "ExItsQrParseError";
    this.code = code;
    this.receivedPurpose = receivedPurpose;
  }
}

const CANONICAL_PREFIX = "exits://qr/v1/";
const LEGACY_PERSONAL_PREFIX = "exits://user/v1/";

const PUBLIC_USER_ID = /^EX-\d{4}-\d{4}$/i;
const PUBLIC_ORG_ID = /^ORG\d{6}$/i;

export function normalizePublicUserId(value: string): string {
  return value.trim().toUpperCase();
}

export function normalizePublicOrganizationId(value: string): string {
  return value.trim().toUpperCase();
}

export function isPublicUserId(value: string): boolean {
  return PUBLIC_USER_ID.test(value.trim());
}

export function isPublicOrganizationId(value: string): boolean {
  return PUBLIC_ORG_ID.test(value.trim());
}

export function buildExItsQr(purpose: ExItsQrPurpose, subject: string): string {
  const trimmed = subject.trim();
  if (!trimmed) {
    throw new ExItsQrParseError("invalid_subject", "QR subject cannot be empty.");
  }
  switch (purpose) {
    case "personal":
      return `${CANONICAL_PREFIX}personal/${normalizePublicUserId(trimmed)}`;
    case "organization":
      return `${CANONICAL_PREFIX}organization/${normalizePublicOrganizationId(trimmed)}`;
    case "pos-device-registration":
      return `${CANONICAL_PREFIX}pos-device-registration/${trimmed}`;
    default:
      throw new ExItsQrParseError("unknown_purpose", "Unknown ExItS QR purpose.");
  }
}

export function parseExItsQr(payload: string | null | undefined): ParsedExItsQr {
  if (!payload || !payload.trim()) {
    throw new ExItsQrParseError("empty", "QR payload is empty.");
  }
  const trimmed = payload.trim();

  if (trimmed.toLowerCase().startsWith(LEGACY_PERSONAL_PREFIX)) {
    const subject = normalizePublicUserId(trimmed.slice(LEGACY_PERSONAL_PREFIX.length));
    if (!isPublicUserId(subject)) {
      throw new ExItsQrParseError("invalid_subject", "Personal ExItS ID is invalid.");
    }
    return { purpose: "personal", subject, version: 1 };
  }

  if (!trimmed.toLowerCase().startsWith(CANONICAL_PREFIX)) {
    // Bare public IDs are allowed as manual entry subjects, not as envelope payloads.
    if (isPublicUserId(trimmed)) {
      return { purpose: "personal", subject: normalizePublicUserId(trimmed), version: 1 };
    }
    if (isPublicOrganizationId(trimmed)) {
      return {
        purpose: "organization",
        subject: normalizePublicOrganizationId(trimmed),
        version: 1,
      };
    }
    throw new ExItsQrParseError("unrecognized", "QR payload scheme is not recognized.");
  }

  const remainder = trimmed.slice(CANONICAL_PREFIX.length);
  const slash = remainder.indexOf("/");
  if (slash <= 0 || slash >= remainder.length - 1) {
    throw new ExItsQrParseError("malformed", "QR payload is malformed.");
  }
  const type = remainder.slice(0, slash).toLowerCase();
  const subjectRaw = remainder.slice(slash + 1).trim();

  if (type === "personal") {
    const subject = normalizePublicUserId(subjectRaw);
    if (!isPublicUserId(subject)) {
      throw new ExItsQrParseError("invalid_subject", "Personal ExItS ID is invalid.");
    }
    return { purpose: "personal", subject, version: 1 };
  }
  if (type === "organization") {
    const subject = normalizePublicOrganizationId(subjectRaw);
    if (!isPublicOrganizationId(subject)) {
      throw new ExItsQrParseError("invalid_subject", "Organization public ID is invalid.");
    }
    return { purpose: "organization", subject, version: 1 };
  }
  if (type === "pos-device-registration") {
    if (!subjectRaw) {
      throw new ExItsQrParseError("invalid_subject", "Device registration token is empty.");
    }
    return { purpose: "pos-device-registration", subject: subjectRaw, version: 1 };
  }

  throw new ExItsQrParseError("unknown_purpose", "Unknown ExItS QR purpose.");
}

/** Rejects payloads that do not match the expected purpose for a given flow. */
export function assertExItsQrPurpose(payload: string, expected: ExItsQrPurpose): ParsedExItsQr {
  const parsed = parseExItsQr(payload);
  if (parsed.purpose !== expected) {
    throw new ExItsQrParseError(
      "wrong_purpose",
      `Expected ${expected} QR but received ${parsed.purpose}.`,
      parsed.purpose,
    );
  }
  return parsed;
}
