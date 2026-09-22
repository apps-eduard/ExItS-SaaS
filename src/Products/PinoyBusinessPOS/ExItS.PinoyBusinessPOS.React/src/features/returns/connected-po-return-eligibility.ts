import type { ConnectedPoReturnableLineDto } from "@/api/pos/pos-connected-po-returns-client";

export type ConnectedPoReturnLineEligibilityView = {
  status: "eligible" | "non_returnable" | "expired" | "nothing_returnable";
  availableQty: number;
  expiresLabel: string | null;
  helper: string | null;
  showReturnAction: boolean;
};

export function describeConnectedPoReturnLineEligibility(
  line: Pick<
    ConnectedPoReturnableLineDto,
    | "returnableQuantity"
    | "returnsAllowed"
    | "earliestReturnExpiresAtUtc"
    | "latestReturnExpiresAtUtc"
    | "lineBlockedReason"
  >,
): ConnectedPoReturnLineEligibilityView {
  const availableQty = Math.max(0, line.returnableQuantity ?? 0);
  const blocked = (line.lineBlockedReason ?? "").toLowerCase();

  if (blocked === "non_returnable" || line.returnsAllowed === false) {
    return {
      status: "non_returnable",
      availableQty: 0,
      expiresLabel: null,
      helper: "Delivery problems can still be reported separately.",
      showReturnAction: false,
    };
  }

  if (blocked === "window_expired" || (availableQty <= 0 && blocked.includes("expir"))) {
    const until = line.latestReturnExpiresAtUtc ?? line.earliestReturnExpiresAtUtc;
    return {
      status: "expired",
      availableQty: 0,
      expiresLabel: until ? formatReturnExpiry(until) : null,
      helper: "Delivery problems can still be reported separately.",
      showReturnAction: false,
    };
  }

  if (availableQty <= 0) {
    return {
      status: "nothing_returnable",
      availableQty: 0,
      expiresLabel: null,
      helper: null,
      showReturnAction: false,
    };
  }

  const until = line.earliestReturnExpiresAtUtc ?? line.latestReturnExpiresAtUtc;
  return {
    status: "eligible",
    availableQty,
    expiresLabel: until ? formatReturnExpiry(until) : null,
    helper: null,
    showReturnAction: true,
  };
}

function formatReturnExpiry(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return iso;
  }
  return date.toLocaleDateString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}
