export type PersonalHomeKpiPeriodDeltaIdentity = "lent" | "owe";

export type PersonalHomeKpiPeriodDeltaTone = "good" | "caution" | "neutral";

export type PersonalHomeKpiPeriodDeltaView =
  | { visible: false }
  | { visible: true; label: string; tone: PersonalHomeKpiPeriodDeltaTone };

function formatSignedPercentOneDecimal(percent: number): string {
  const rounded = Math.round(percent * 10) / 10;
  if (rounded > 0) {
    return `↑ +${rounded.toFixed(1)}%`;
  }
  if (rounded < 0) {
    return `↓ ${rounded.toFixed(1)}%`;
  }
  return "→ 0.0%";
}

export function personalHomeKpiPeriodDeltaTone(
  identity: PersonalHomeKpiPeriodDeltaIdentity,
  direction: "up" | "down" | "flat",
): PersonalHomeKpiPeriodDeltaTone {
  if (direction === "flat") {
    return "neutral";
  }
  if (identity === "lent") {
    return direction === "up" ? "good" : "caution";
  }
  return direction === "up" ? "caution" : "good";
}

export function personalHomeKpiPeriodDeltaView(
  series: number[] | null,
  identity: PersonalHomeKpiPeriodDeltaIdentity,
  periodOpening?: number | null,
): PersonalHomeKpiPeriodDeltaView {
  if (series === null || series.length === 0) {
    return { visible: false };
  }

  const closing = series[series.length - 1]!;
  const opening =
    periodOpening !== undefined && periodOpening !== null ? periodOpening : series[0]!;

  if (opening === 0 && closing > 0) {
    return { visible: false };
  }

  if (opening === closing) {
    return {
      visible: true,
      label: "→ 0.0%",
      tone: personalHomeKpiPeriodDeltaTone(identity, "flat"),
    };
  }

  if (opening === 0) {
    return { visible: false };
  }

  const percent = ((closing - opening) / Math.abs(opening)) * 100;
  const direction: "up" | "down" = percent > 0 ? "up" : "down";

  return {
    visible: true,
    label: formatSignedPercentOneDecimal(percent),
    tone: personalHomeKpiPeriodDeltaTone(identity, direction),
  };
}