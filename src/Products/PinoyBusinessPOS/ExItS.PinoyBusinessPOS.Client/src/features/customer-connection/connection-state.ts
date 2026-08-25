import type { MessageKey } from "@/i18n/messages";
import {
  mapPlatformCustomerLinkStatus,
  type CustomerLinkUiStatus,
} from "@/features/customers/customer-link-status";

/**
 * Authoritative relationship presentation for Organization ↔ Personal customer links.
 * Derived from Platform link-status / linked-merchant / block APIs — never name/email/phone.
 *
 * Precedence when multiple facts exist for the same pair (highest first):
 * Blocked > Linked > Pending > Declined > Expired > Revoked/Disconnected > NotLinked > Unavailable
 */
export type ConnectionRelationshipState =
  | "NotLinked"
  | "Pending"
  | "Linked"
  | "Declined"
  | "Expired"
  | "Revoked"
  | "Blocked"
  | "Unavailable";

/** Independent of relationship — purchase history / statement projection load. */
export type ConnectionDataLoadState =
  | "Loading"
  | "Loaded"
  | "Empty"
  | "Unavailable"
  | "Error"
  | "Forbidden"
  | "HistoryNotReady";

const PRECEDENCE: Record<ConnectionRelationshipState, number> = {
  Blocked: 80,
  Linked: 70,
  Pending: 60,
  Declined: 50,
  Expired: 40,
  Revoked: 30,
  NotLinked: 20,
  Unavailable: 10,
};

export function connectionRelationshipPrecedence(state: ConnectionRelationshipState): number {
  return PRECEDENCE[state];
}

export function pickHigherConnectionState(
  a: ConnectionRelationshipState,
  b: ConnectionRelationshipState,
): ConnectionRelationshipState {
  return PRECEDENCE[a] >= PRECEDENCE[b] ? a : b;
}

export function mapOrgLinkStatusToRelationship(
  status: CustomerLinkUiStatus | string | null | undefined,
): ConnectionRelationshipState {
  const mapped =
    typeof status === "string"
      ? mapPlatformCustomerLinkStatus(status)
      : status == null
        ? "Unavailable"
        : status;
  return mapped;
}

/** Personal stores list: Active linked merchants are Connected. */
export function personalLinkedMerchantRelationship(): ConnectionRelationshipState {
  return "Linked";
}

export function connectionStatusLabelKey(
  state: ConnectionRelationshipState,
  audience: "personal" | "organization",
): MessageKey {
  if (audience === "personal") {
    switch (state) {
      case "Linked":
        return "connection.status.connected";
      case "Pending":
        return "connection.status.request";
      case "Declined":
        return "connection.status.declined";
      case "Expired":
        return "connection.status.expired";
      case "Revoked":
        return "connection.status.disconnected";
      case "Blocked":
        return "connection.status.blocked";
      case "NotLinked":
        return "connection.status.notConnected";
      case "Unavailable":
        return "connection.status.unavailable";
    }
  }

  switch (state) {
    case "Linked":
      return "connection.status.org.connected";
    case "Pending":
      return "connection.status.org.awaiting";
    case "Declined":
      return "connection.status.org.declined";
    case "Expired":
      return "connection.status.org.expired";
    case "Revoked":
      return "connection.status.org.disconnected";
    case "Blocked":
    case "Unavailable":
      return "connection.status.org.unavailable";
    case "NotLinked":
      return "connection.status.org.notConnected";
  }
}

export function connectionStatusDetailKey(
  state: ConnectionRelationshipState,
  audience: "personal" | "organization",
): MessageKey {
  if (audience === "personal") {
    switch (state) {
      case "Linked":
        return "connection.detail.personal.connected";
      case "Pending":
        return "connection.detail.personal.pending";
      case "Declined":
        return "connection.detail.personal.declined";
      case "Expired":
        return "connection.detail.personal.expired";
      case "Revoked":
        return "connection.detail.personal.disconnected";
      case "Blocked":
        return "connection.detail.personal.blocked";
      case "NotLinked":
        return "connection.detail.personal.notConnected";
      case "Unavailable":
        return "connection.detail.personal.unavailable";
    }
  }

  switch (state) {
    case "Linked":
      return "connection.detail.org.connected";
    case "Pending":
      return "connection.detail.org.awaiting";
    case "Declined":
      return "connection.detail.org.declined";
    case "Expired":
      return "connection.detail.org.expired";
    case "Revoked":
      return "connection.detail.org.disconnected";
    case "Blocked":
    case "Unavailable":
      return "connection.detail.org.unavailable";
    case "NotLinked":
      return "connection.detail.org.notConnected";
  }
}

export function connectionStatusTone(
  state: ConnectionRelationshipState,
  audience: "personal" | "organization",
): "success" | "info" | "warning" | "danger" {
  switch (state) {
    case "Linked":
      return "success";
    case "Pending":
      return "info";
    case "Declined":
    case "Expired":
    case "Revoked":
    case "NotLinked":
      return "info";
    case "Blocked":
      return audience === "personal" ? "warning" : "info";
    case "Unavailable":
      return audience === "organization" ? "info" : "warning";
  }
}

/**
 * Map POS statement HTTP failures while the Platform relationship is Linked.
 * Keep relationship and projection errors separate in the UI.
 */
export function mapLinkedStatementHttpToDataLoad(
  status: number | null | undefined,
): ConnectionDataLoadState {
  if (status === 403) {
    return "Forbidden";
  }
  if (status === 404) {
    return "HistoryNotReady";
  }
  if (status != null && status >= 500) {
    return "Error";
  }
  if (status != null && status >= 400) {
    return "Error";
  }
  return "Error";
}
