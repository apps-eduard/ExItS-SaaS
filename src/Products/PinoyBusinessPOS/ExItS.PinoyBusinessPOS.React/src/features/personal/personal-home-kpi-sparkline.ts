/**
 * Placeholder sparkline series for Personal home KPI cards.
 * Personal dashboard has no time-series API yet — values are derived deterministically
 * from the anchor metric so the chart shape is stable for a given balance/count.
 */
export const PERSONAL_HOME_KPI_SPARK_POINTS = 8;

export function personalHomeKpiSparkSeries(anchor: number, points = PERSONAL_HOME_KPI_SPARK_POINTS): number[] {
  const safeAnchor = Number.isFinite(anchor) ? Math.max(anchor, 0) : 0;
  const base = Math.max(safeAnchor, 1);
  const seed = Math.abs(Math.floor(base * 1000) % 9973) || 1;

  return Array.from({ length: points }, (_, index) => {
    const progress = index / Math.max(points - 1, 1);
    const wobble =
      Math.sin((seed + index * 17) * 0.31) * 0.14 + Math.cos(index * 0.72 + seed * 0.11) * 0.09;
    return base * (0.52 + 0.48 * progress + wobble * 0.22);
  });
}

export type PersonalHomeKpiSparkGeometry = {
  linePath: string;
  fillPath: string;
  endX: number;
  endY: number;
};

export function personalHomeKpiSparkGeometry(
  values: number[],
  width = 160,
  height = 40,
): PersonalHomeKpiSparkGeometry {
  if (values.length === 0) {
    return { linePath: "", fillPath: "", endX: width, endY: height / 2 };
  }

  const min = Math.min(...values);
  const max = Math.max(...values);
  const range = max - min || 1;
  const insetY = 6;
  const plotHeight = height - insetY * 2;
  const step = values.length > 1 ? width / (values.length - 1) : 0;

  const coords = values.map((value, index) => {
    const x = index * step;
    const normalized = (value - min) / range;
    const y = height - insetY - normalized * plotHeight;
    return { x, y };
  });

  const linePath = coords
    .map((point, index) => `${index === 0 ? "M" : "L"}${point.x.toFixed(2)} ${point.y.toFixed(2)}`)
    .join(" ");

  const fillPath = `${linePath} L${width.toFixed(2)} ${height.toFixed(2)} L0 ${height.toFixed(2)} Z`;
  const last = coords[coords.length - 1] ?? { x: width, y: height / 2 };

  return {
    linePath,
    fillPath,
    endX: last.x,
    endY: last.y,
  };
}