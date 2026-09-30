/** Shared inspection disposition: sellable vs damaged/non-sellable must equal returned qty. */

export type InspectDispositionDraft = {
  recovered: string;
  confirmed: string;
};

export function emptyInspectDraft(): InspectDispositionDraft {
  return { recovered: "", confirmed: "" };
}

export function allSellableDraft(totalQty: number): InspectDispositionDraft {
  return { recovered: formatInspectQty(totalQty), confirmed: "0" };
}

export function allDamagedDraft(totalQty: number): InspectDispositionDraft {
  return { recovered: "0", confirmed: formatInspectQty(totalQty) };
}

export function formatInspectQty(value: number): string {
  if (!Number.isFinite(value)) {
    return "0";
  }
  const rounded = Math.round(value * 1000) / 1000;
  return String(rounded);
}

export function balanceFromRecovered(
  totalQty: number,
  recoveredText: string,
): InspectDispositionDraft {
  const recovered = Number(recoveredText);
  if (!Number.isFinite(recovered) || recovered < 0) {
    return { recovered: recoveredText, confirmed: "" };
  }
  const clamped = Math.min(recovered, totalQty);
  return {
    recovered: recoveredText,
    confirmed: formatInspectQty(Math.max(0, totalQty - clamped)),
  };
}

export function balanceFromConfirmed(
  totalQty: number,
  confirmedText: string,
): InspectDispositionDraft {
  const confirmed = Number(confirmedText);
  if (!Number.isFinite(confirmed) || confirmed < 0) {
    return { recovered: "", confirmed: confirmedText };
  }
  const clamped = Math.min(confirmed, totalQty);
  return {
    recovered: formatInspectQty(Math.max(0, totalQty - clamped)),
    confirmed: confirmedText,
  };
}

export type ParsedInspectDisposition =
  | { ok: true; recovered: number; confirmed: number }
  | { ok: false };

/** Valid when both are finite ≥ 0 and sum equals total (within 0.001). */
export function parseInspectDisposition(
  draft: InspectDispositionDraft,
  totalQty: number,
): ParsedInspectDisposition {
  if (draft.recovered.trim() === "" || draft.confirmed.trim() === "") {
    return { ok: false };
  }
  const recovered = Number(draft.recovered);
  const confirmed = Number(draft.confirmed);
  if (!Number.isFinite(recovered) || recovered < 0 || !Number.isFinite(confirmed) || confirmed < 0) {
    return { ok: false };
  }
  if (Math.abs(recovered + confirmed - totalQty) > 0.001) {
    return { ok: false };
  }
  return { ok: true, recovered, confirmed };
}
