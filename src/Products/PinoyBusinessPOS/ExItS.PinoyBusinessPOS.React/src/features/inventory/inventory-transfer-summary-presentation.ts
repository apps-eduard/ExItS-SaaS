import type {
  InventoryTransferDto,
  InventoryTransferLineDto,
} from "@/api/pos/pos-inventory-transfer-client";
import type { MessageKey } from "@/i18n/messages";
import { inventoryTransferDiscrepancyLabelKey } from "@/features/inventory/inventory-transfer-labels";
import { lineDamagedQty } from "@/features/inventory/inventory-transfer-receive-helpers";
import { requiresActualProduct } from "@/features/inventory/transfer-exception-custody-policy";

export type ThisShipmentTotals = {
  sent: number;
  goodReceived: number;
  damaged: number;
  missing: number;
  other: number;
};

export type ReceivingDecisionView = {
  hasDiscrepancy: boolean;
  damagedQty: number;
  damagedFollowUp: string | null;
  custodyDecision: string | null;
  custodyStatus: string | null;
  missingQty: number;
  missingDisposition: string | null;
  otherQty: number;
  otherReasonCode: string | null;
  otherReasonNote: string | null;
  otherFollowUp: string | null;
  otherCustodyDecision: string | null;
  otherCustodyStatus: string | null;
  expectedProductId: string | null;
  expectedProductName: string | null;
  actualReceivedProductId: string | null;
  actualReceivedProductName: string | null;
  /** WrongItem / WrongVariant: show Expected + Actual as distinct rows. */
  showExpectedAndActualItems: boolean;
  confirmedNonSellableQty: number | null;
};

function sumFinite(values: number[]): number {
  return values.reduce((sum, v) => sum + (Number.isFinite(v) ? v : 0), 0);
}

/** Physical classification for the transfer currently on screen — never family totals. */
export function computeThisShipmentTotals(
  transfer: Pick<InventoryTransferDto, "totalSentQty" | "totalReceivedQty" | "receipts">,
): ThisShipmentTotals {
  const receipts = transfer.receipts ?? [];
  if (receipts.length === 0) {
    return {
      sent: transfer.totalSentQty,
      goodReceived: transfer.totalReceivedQty,
      damaged: 0,
      missing: 0,
      other: 0,
    };
  }

  let good = 0;
  let damaged = 0;
  let missing = 0;
  let other = 0;
  for (const receipt of receipts) {
    for (const line of receipt.lines ?? []) {
      good += line.quantityReceived ?? 0;
      damaged += line.quantityDamaged ?? 0;
      missing += line.quantityMissing ?? 0;
      other += line.quantityOther ?? 0;
    }
  }
  return {
    sent: transfer.totalSentQty,
    goodReceived: good,
    damaged,
    missing,
    other,
  };
}

/** Authoritative waived qty for THIS transfer only (never family aggregate). */
export function computeThisTransferWaivedQty(
  transfer: Pick<InventoryTransferDto, "lines" | "receipts">,
): number {
  const fromLines = sumFinite((transfer.lines ?? []).map((l) => l.waivedQty ?? 0));
  if (fromLines > 1e-9) {
    return fromLines;
  }
  return sumFinite(
    (transfer.receipts ?? []).flatMap((r) =>
      (r.lines ?? []).map((l) => l.quantityWaived ?? 0),
    ),
  );
}

export type OverallFulfillmentView = {
  target: number;
  goodReceived: number;
  openInTransit: number;
  waived: number;
  remainingToDispatch: number;
};

/** Family coverage metrics from authoritative transfer DTO fields. */
export function buildOverallFulfillmentView(
  transfer: Pick<
    InventoryTransferDto,
    | "totalSentQty"
    | "familyMembers"
    | "rootTransferId"
    | "satisfiedAtDestinationQty"
    | "totalReceivedQty"
    | "openInTransitQty"
    | "totalOutstandingQty"
    | "waivedQty"
    | "remainingToDispatchQty"
  >,
): OverallFulfillmentView {
  return {
    target: familyFulfillmentTargetQty(transfer),
    goodReceived: transfer.satisfiedAtDestinationQty ?? transfer.totalReceivedQty,
    openInTransit: transfer.openInTransitQty ?? transfer.totalOutstandingQty,
    waived: transfer.waivedQty ?? 0,
    remainingToDispatch: transfer.remainingToDispatchQty ?? 0,
  };
}

export function transferHasReceivingDiscrepancy(
  transfer: Pick<InventoryTransferDto, "totalSentQty" | "totalReceivedQty" | "receipts">,
): boolean {
  const totals = computeThisShipmentTotals(transfer);
  return totals.damaged > 1e-9 || totals.missing > 1e-9 || totals.other > 1e-9;
}

/**
 * Persisted receiving decisions for THIS transfer only.
 * Prefer damage-custody fields when present; otherwise receipt-line follow-ups.
 */
function trimNonEmpty(value: string | null | undefined): string | null {
  const trimmed = value?.trim();
  return trimmed ? trimmed : null;
}

/**
 * Persisted receiving/discrepancy note for display.
 * Prefer otherReasonNote, then receipt-line note; omit empties; never invent text.
 */
export function pickReceivingDecisionNote(
  otherReasonNote: string | null | undefined,
  receiptLineNote: string | null | undefined,
  discrepancyNote: string | null | undefined,
): string | null {
  const primary = trimNonEmpty(otherReasonNote);
  if (primary) {
    return primary;
  }
  const secondary = trimNonEmpty(receiptLineNote);
  if (secondary) {
    return secondary;
  }
  const tertiary = trimNonEmpty(discrepancyNote);
  return tertiary;
}

export function buildReceivingDecisionView(
  transfer: Pick<
    InventoryTransferDto,
    "transferId" | "receipts" | "damageCustodies" | "exceptionCustodies" | "lines"
  >,
): ReceivingDecisionView {
  const receipts = transfer.receipts ?? [];
  let damagedQty = 0;
  let missingQty = 0;
  let otherQty = 0;
  let damagedFollowUp: string | null = null;
  let missingDisposition: string | null = null;
  let otherFollowUp: string | null = null;
  let otherReasonCode: string | null = null;
  let otherReasonNote: string | null = null;
  let receiptLineNote: string | null = null;
  let receiptLineExpectedProductId: string | null = null;

  for (const receipt of receipts) {
    for (const line of receipt.lines ?? []) {
      const d = line.quantityDamaged ?? 0;
      const m = line.quantityMissing ?? 0;
      const o = line.quantityOther ?? 0;
      damagedQty += d;
      missingQty += m;
      otherQty += o;
      if (d > 1e-9 && line.damagedFollowUp && !damagedFollowUp) {
        damagedFollowUp = line.damagedFollowUp;
      }
      if (m > 1e-9 && line.missingDisposition && !missingDisposition) {
        missingDisposition = line.missingDisposition;
      }
      if (o > 1e-9) {
        if (line.otherFollowUp && !otherFollowUp) {
          otherFollowUp = line.otherFollowUp;
        }
        if (line.otherReasonCode && !otherReasonCode) {
          otherReasonCode = line.otherReasonCode;
        }
        if (!otherReasonNote) {
          otherReasonNote = trimNonEmpty(line.otherReasonNote);
        }
        if (!receiptLineExpectedProductId) {
          receiptLineExpectedProductId = line.productId;
        }
      }
      // Remarks/note apply to any discrepancy classification (damaged / missing / other).
      if ((d > 1e-9 || m > 1e-9 || o > 1e-9) && !receiptLineNote) {
        receiptLineNote = trimNonEmpty(line.note);
      }
      if ((d > 1e-9 || m > 1e-9 || o > 1e-9) && !otherReasonNote) {
        otherReasonNote = trimNonEmpty(line.otherReasonNote);
      }
    }
  }

  const custodies = (transfer.damageCustodies ?? []).filter(
    (c) => c.transferId.toLowerCase() === transfer.transferId.toLowerCase(),
  );
  let custodyDecision: string | null = null;
  let custodyStatus: string | null = null;
  if (custodies.length > 0) {
    const primary = custodies[0]!;
    custodyDecision = primary.decision;
    custodyStatus = primary.status;
    if (primary.followUpIntent) {
      damagedFollowUp = primary.followUpIntent;
    }
    if (damagedQty < 1e-9) {
      damagedQty = sumFinite(custodies.map((c) => c.quantity));
    }
  }

  const exceptionCustodies = (transfer.exceptionCustodies ?? []).filter(
    (c) => c.transferId.toLowerCase() === transfer.transferId.toLowerCase(),
  );
  let otherCustodyDecision: string | null = null;
  let otherCustodyStatus: string | null = null;
  let expectedProductId: string | null = null;
  let expectedProductName: string | null = null;
  let actualReceivedProductId: string | null = null;
  let actualReceivedProductName: string | null = null;
  let confirmedNonSellableQty: number | null = null;
  if (exceptionCustodies.length > 0) {
    const primaryException = exceptionCustodies[0]!;
    otherCustodyDecision = primaryException.decision;
    otherCustodyStatus = primaryException.status;
    expectedProductId = primaryException.expectedProductId;
    actualReceivedProductId = primaryException.actualProductId;
    expectedProductName = trimNonEmpty(primaryException.expectedProductName);
    actualReceivedProductName = trimNonEmpty(primaryException.actualProductName);
    confirmedNonSellableQty = primaryException.confirmedNonSellableQty;
    if (primaryException.followUpIntent) {
      otherFollowUp = primaryException.followUpIntent;
    }
    if (!otherReasonCode && primaryException.reasonCode) {
      otherReasonCode = primaryException.reasonCode;
    }
    if (otherQty < 1e-9) {
      otherQty = sumFinite(exceptionCustodies.map((c) => c.quantity));
    }
  }

  for (const receipt of receipts) {
    for (const line of receipt.lines ?? []) {
      if ((line.quantityOther ?? 0) > 1e-9 && line.actualReceivedProductId && !actualReceivedProductId) {
        actualReceivedProductId = line.actualReceivedProductId;
      }
      if ((line.quantityOther ?? 0) > 1e-9 && line.otherCustodyDecision && !otherCustodyDecision) {
        otherCustodyDecision = line.otherCustodyDecision;
      }
    }
  }

  if (!expectedProductId) {
    expectedProductId = receiptLineExpectedProductId;
  }

  const lineDiscrepancyNote =
    trimNonEmpty(
      transfer.lines?.find((l) => {
        if (expectedProductId != null) {
          return l.productId.toLowerCase() === expectedProductId.toLowerCase();
        }
        return trimNonEmpty(l.discrepancyNote) != null;
      })?.discrepancyNote,
    );

  const resolvedNote = pickReceivingDecisionNote(
    otherReasonNote,
    receiptLineNote,
    lineDiscrepancyNote,
  );

  if (!expectedProductName && expectedProductId) {
    expectedProductName = resolveTransferProductDisplayName(transfer, expectedProductId);
    if (expectedProductName === "—") {
      expectedProductName = null;
    }
  }
  if (!actualReceivedProductName && actualReceivedProductId) {
    // Resolve actual name only from custody/catalog-style sources — never expected line.
    const fromLines = transfer.lines?.find(
      (entry) => entry.productId.toLowerCase() === actualReceivedProductId!.toLowerCase(),
    )?.productName;
    const candidate = trimNonEmpty(fromLines);
    if (
      candidate &&
      !looksLikeProductIdFragment(candidate, actualReceivedProductId)
    ) {
      actualReceivedProductName = candidate;
    }
  }

  const showExpectedAndActualItems = requiresActualProduct(otherReasonCode ?? "");

  return {
    hasDiscrepancy: damagedQty > 1e-9 || missingQty > 1e-9 || otherQty > 1e-9,
    damagedQty,
    damagedFollowUp,
    custodyDecision,
    custodyStatus,
    missingQty,
    missingDisposition,
    otherQty,
    otherReasonCode,
    otherReasonNote: resolvedNote,
    otherFollowUp,
    otherCustodyDecision,
    otherCustodyStatus,
    expectedProductId,
    expectedProductName,
    actualReceivedProductId,
    actualReceivedProductName,
    showExpectedAndActualItems,
    confirmedNonSellableQty,
  };
}

export function transferDiscrepancyFollowUpLabelKey(
  code: string | null | undefined,
): MessageKey | null {
  if (!code) {
    return null;
  }
  switch (code) {
    case "RequestReplacement":
      return "transfer.decision.replacementRequested";
    case "AcceptShortage":
      return "transfer.decision.acceptedNoReplacement";
    default:
      return null;
  }
}

export function transferMissingDispositionLabelKey(
  code: string | null | undefined,
): MessageKey | null {
  if (!code) {
    return null;
  }
  switch (code) {
    case "ExpectedLater":
      return "transfer.followUp.waitOriginal";
    case "CloseMissing":
      return "transfer.decision.replacementRequested";
    case "AcceptShortage":
      return "transfer.decision.acceptedShortage";
    default:
      return null;
  }
}

export function transferCustodyDecisionLabelKey(
  code: string | null | undefined,
): MessageKey | null {
  if (!code) {
    return null;
  }
  switch (code) {
    case "KeepAtDestination":
      return "transfer.custody.keepAtDestination";
    case "ReturnToSource":
      return "transfer.custody.returnToSource";
    default:
      return null;
  }
}

/**
 * Branch-aware "Return to {source}" for exception/damage ReturnToSource custody.
 * Uses the physical transfer's source branch name — never destination or current login branch.
 */
export function formatReturnToSourceCustodyLabel(
  sourceBranchName: string | null | undefined,
  templateWithBranch: string,
  fallback: string,
): string {
  const branch = sourceBranchName?.trim();
  return branch ? templateWithBranch.replace("{branch}", branch) : fallback;
}

/**
 * Branch-aware return status for exception custody presentation.
 * Internal status codes stay unchanged; only display copy is branch-aware.
 */
export function formatExceptionCustodyReturnStatusLabel(
  status: string | null | undefined,
  sourceBranchName: string | null | undefined,
  templates: {
    awaitingReturn: string;
    returningToBranch: string;
    returnInTransitFallback: string;
    returnedToBranch: string;
    receivedAtSourceFallback: string;
    heldAtDestination: string;
    awaitingInspection: string;
  },
): string | null {
  if (!status) {
    return null;
  }
  const branch = sourceBranchName?.trim();
  switch (status) {
    case "AwaitingReturn":
      return templates.awaitingReturn;
    case "ReturnInTransit":
      return branch
        ? templates.returningToBranch.replace("{branch}", branch)
        : templates.returnInTransitFallback;
    case "ReceivedAtSource":
      return branch
        ? templates.returnedToBranch.replace("{branch}", branch)
        : templates.receivedAtSourceFallback;
    case "HeldAtDestination":
      return templates.heldAtDestination;
    case "AwaitingInspection":
      return templates.awaitingInspection;
    default:
      return status;
  }
}

export function transferCustodyStatusLabelKey(
  code: string | null | undefined,
): MessageKey | null {
  if (!code) {
    return null;
  }
  switch (code) {
    case "HeldAtDestination":
      return "transfer.custody.heldAtDestination";
    case "AwaitingReturn":
      return "transfer.custody.awaitingReturn";
    case "ReturnInTransit":
      return "transfer.custody.returnInTransit";
    case "ReceivedAtSource":
      return "transfer.custody.receivedAtSource";
    case "AwaitingInspection":
      return "transfer.custody.awaitingInspection";
    default:
      return null;
  }
}

export function isKeepAtDestinationCustody(decision: string | null | undefined): boolean {
  return decision === "KeepAtDestination";
}

export function isReturnToSourceCustody(decision: string | null | undefined): boolean {
  return decision === "ReturnToSource";
}

export function lineMissingQty(
  transfer: Pick<InventoryTransferDto, "receipts">,
  lineId: string,
): number {
  let total = 0;
  for (const receipt of transfer.receipts ?? []) {
    for (const rl of receipt.lines ?? []) {
      if (rl.lineId === lineId) {
        total += rl.quantityMissing ?? 0;
      }
    }
  }
  return total;
}

export function lineOtherQty(
  transfer: Pick<InventoryTransferDto, "receipts">,
  lineId: string,
): number {
  let total = 0;
  for (const receipt of transfer.receipts ?? []) {
    for (const rl of receipt.lines ?? []) {
      if (rl.lineId === lineId) {
        total += rl.quantityOther ?? 0;
      }
    }
  }
  return total;
}

export type LineFollowUpDisplay = {
  labelKey: MessageKey;
  qty?: number;
} | null;

/** Compact follow-up cell for THIS transfer's line from persisted receipt decisions. */
export function resolveTransferProductDisplayName(
  transfer: Pick<InventoryTransferDto, "lines" | "exceptionCustodies">,
  productId: string,
): string {
  const match = transfer.lines.find(
    (entry) => entry.productId.toLowerCase() === productId.toLowerCase(),
  );
  if (
    match?.productName?.trim() &&
    !looksLikeProductIdFragment(match.productName.trim(), productId)
  ) {
    return match.productName.trim();
  }
  for (const custody of transfer.exceptionCustodies ?? []) {
    if (
      custody.actualProductId.toLowerCase() === productId.toLowerCase() &&
      custody.actualProductName?.trim() &&
      !looksLikeProductIdFragment(custody.actualProductName.trim(), productId)
    ) {
      return custody.actualProductName.trim();
    }
    if (
      custody.expectedProductId.toLowerCase() === productId.toLowerCase() &&
      custody.expectedProductName?.trim() &&
      !looksLikeProductIdFragment(custody.expectedProductName.trim(), productId)
    ) {
      return custody.expectedProductName.trim();
    }
  }
  return "—";
}

function looksLikeProductIdFragment(label: string, productId: string): boolean {
  const trimmed = label.trim();
  if (!trimmed) {
    return true;
  }
  return (
    trimmed.length <= 8 &&
    productId.toLowerCase().startsWith(trimmed.toLowerCase())
  );
}

export { looksLikeProductIdFragment as looksLikeTransferProductIdFragment };

/**
 * User-facing product label for the ACTUAL exception custody product.
 * Never substitutes the expected transfer-line product when actual differs.
 */
export function resolveExceptionCustodyItemLabel(
  transfer: Pick<InventoryTransferDto, "lines" | "exceptionCustodies">,
  custody: {
    actualProductId: string;
    expectedProductId: string;
    actualProductName?: string | null;
    expectedProductName?: string | null;
  },
): string {
  const actualCandidates = [
    custody.actualProductName?.trim(),
    transfer.exceptionCustodies?.find(
      (c) => c.actualProductId.toLowerCase() === custody.actualProductId.toLowerCase(),
    )?.actualProductName?.trim(),
    transfer.lines.find(
      (line) => line.productId.toLowerCase() === custody.actualProductId.toLowerCase(),
    )?.productName?.trim(),
  ];
  for (const candidate of actualCandidates) {
    if (candidate && !looksLikeProductIdFragment(candidate, custody.actualProductId)) {
      return candidate;
    }
  }

  const sameAsExpected =
    custody.actualProductId.toLowerCase() === custody.expectedProductId.toLowerCase();
  if (sameAsExpected) {
    const expectedCandidates = [
      custody.expectedProductName?.trim(),
      transfer.exceptionCustodies?.find(
        (c) =>
          c.expectedProductId.toLowerCase() === custody.expectedProductId.toLowerCase(),
      )?.expectedProductName?.trim(),
      transfer.lines.find(
        (line) => line.productId.toLowerCase() === custody.expectedProductId.toLowerCase(),
      )?.productName?.trim(),
    ];
    for (const candidate of expectedCandidates) {
      if (candidate && !looksLikeProductIdFragment(candidate, custody.expectedProductId)) {
        return candidate;
      }
    }
  }

  // When actual ≠ expected and actual catalog name is unavailable, do not show expected name.
  return "—";
}

/** Expected (sent) product label for WrongItem / WrongVariant receiving decision. */
export function resolveExceptionCustodyExpectedItemLabel(
  transfer: Pick<InventoryTransferDto, "lines" | "exceptionCustodies">,
  custody: {
    expectedProductId: string;
    expectedProductName?: string | null;
  },
): string {
  const candidates = [
    custody.expectedProductName?.trim(),
    transfer.exceptionCustodies?.find(
      (c) => c.expectedProductId.toLowerCase() === custody.expectedProductId.toLowerCase(),
    )?.expectedProductName?.trim(),
    transfer.lines.find(
      (line) => line.productId.toLowerCase() === custody.expectedProductId.toLowerCase(),
    )?.productName?.trim(),
  ];
  for (const candidate of candidates) {
    if (candidate && !looksLikeProductIdFragment(candidate, custody.expectedProductId)) {
      return candidate;
    }
  }
  return resolveTransferProductDisplayName(transfer, custody.expectedProductId);
}

export function lineOtherExceptionSecondaryText(
  transfer: Pick<
    InventoryTransferDto,
    "transferId" | "receipts" | "exceptionCustodies" | "lines"
  >,
  line: Pick<InventoryTransferLineDto, "lineId" | "productId">,
): string | null {
  let reasonCode: string | null = null;
  let actualProductId: string | null = null;
  for (const receipt of transfer.receipts ?? []) {
    for (const rl of receipt.lines ?? []) {
      if (rl.lineId !== line.lineId || (rl.quantityOther ?? 0) <= 1e-9) {
        continue;
      }
      reasonCode = rl.otherReasonCode ?? null;
      actualProductId = rl.actualReceivedProductId ?? null;
    }
  }
  const custody = (transfer.exceptionCustodies ?? []).find(
    (c) =>
      c.transferId.toLowerCase() === transfer.transferId.toLowerCase() &&
      c.expectedProductId.toLowerCase() === line.productId.toLowerCase(),
  );
  if (custody) {
    reasonCode = reasonCode ?? custody.reasonCode;
    actualProductId = actualProductId ?? custody.actualProductId;
  }
  if (reasonCode !== "WrongItem" && reasonCode !== "WrongVariant") {
    return null;
  }
  const reasonLabel = reasonCode === "WrongVariant" ? "Wrong variant" : "Wrong item";
  if (!actualProductId) {
    return reasonLabel;
  }
  const actualNameCandidates = [
    custody?.actualProductName?.trim() ?? null,
    transfer.lines.find(
      (entry) => entry.productId.toLowerCase() === actualProductId!.toLowerCase(),
    )?.productName?.trim() ?? null,
  ];
  const actualName =
    actualNameCandidates.find(
      (candidate) =>
        !!candidate && !looksLikeProductIdFragment(candidate, actualProductId!),
    ) ?? null;
  if (!actualName) {
    return reasonLabel;
  }
  return `${reasonLabel} · Actual: ${actualName}`;
}

export function lineFollowUpDisplay(
  transfer: Pick<
    InventoryTransferDto,
    "receipts" | "damageCustodies" | "exceptionCustodies" | "transferId" | "lines"
  >,
  line: Pick<InventoryTransferLineDto, "lineId">,
): LineFollowUpDisplay {
  const damaged = lineDamagedQty(transfer, line.lineId);
  const missing = lineMissingQty(transfer, line.lineId);
  const other = lineOtherQty(transfer, line.lineId);

  let damagedFollowUp: string | null = null;
  let missingDisposition: string | null = null;
  let otherFollowUp: string | null = null;

  for (const receipt of transfer.receipts ?? []) {
    for (const rl of receipt.lines ?? []) {
      if (rl.lineId !== line.lineId) {
        continue;
      }
      if ((rl.quantityDamaged ?? 0) > 1e-9 && rl.damagedFollowUp) {
        damagedFollowUp = rl.damagedFollowUp;
      }
      if ((rl.quantityMissing ?? 0) > 1e-9 && rl.missingDisposition) {
        missingDisposition = rl.missingDisposition;
      }
      if ((rl.quantityOther ?? 0) > 1e-9 && rl.otherFollowUp) {
        otherFollowUp = rl.otherFollowUp;
      }
    }
  }

  const custody = (transfer.damageCustodies ?? []).find(
    (c) => c.transferId.toLowerCase() === transfer.transferId.toLowerCase(),
  );
  if (custody?.followUpIntent && damaged > 1e-9) {
    damagedFollowUp = custody.followUpIntent;
  }

  if (damaged > 1e-9 && damagedFollowUp === "RequestReplacement") {
    return { labelKey: "transfer.followUp.replaceQty", qty: damaged };
  }
  if (damaged > 1e-9 && damagedFollowUp === "AcceptShortage") {
    return { labelKey: "transfer.decision.acceptedNoReplacement" };
  }
  if (missing > 1e-9 && missingDisposition === "ExpectedLater") {
    return { labelKey: "transfer.followUp.waitOriginal" };
  }
  if (missing > 1e-9 && missingDisposition === "CloseMissing") {
    return { labelKey: "transfer.followUp.replaceQty", qty: missing };
  }
  if (missing > 1e-9 && missingDisposition === "AcceptShortage") {
    return { labelKey: "transfer.decision.acceptedShortage" };
  }
  if (other > 1e-9 && otherFollowUp === "RequestReplacement") {
    return { labelKey: "transfer.followUp.replaceQty", qty: other };
  }
  if (other > 1e-9 && otherFollowUp === "AcceptShortage") {
    return { labelKey: "transfer.decision.acceptedNoReplacement" };
  }
  return null;
}

export function otherReasonLabelKey(code: string | null | undefined): MessageKey {
  if (!code) {
    return "transfer.discrepancy.other";
  }
  return inventoryTransferDiscrepancyLabelKey(code);
}

export function familyFulfillmentTargetQty(
  transfer: Pick<InventoryTransferDto, "totalSentQty" | "familyMembers" | "rootTransferId">,
): number {
  const members = transfer.familyMembers ?? [];
  const root = members.find((m) => m.isRoot);
  if (root) {
    return root.totalSentQty;
  }
  if (!transfer.rootTransferId) {
    return transfer.totalSentQty;
  }
  return transfer.totalSentQty;
}

export function familyMemberDamagedQty(member: {
  totalDamagedQty?: number;
  totalSentQty: number;
  totalReceivedQty: number;
  totalOutstandingQty: number;
}): number {
  if (member.totalDamagedQty != null && Number.isFinite(member.totalDamagedQty)) {
    return member.totalDamagedQty;
  }
  return 0;
}
