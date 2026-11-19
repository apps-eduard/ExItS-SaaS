/** Sparkline geometry for Personal home KPI cards (SVG path from dated balance series). */

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
  /* Subtle edge breathing room so stroke/fill do not clip card corners. */
  const insetX = 4;
  const insetY = 8;
  const plotWidth = width - insetX * 2;
  const plotHeight = height - insetY * 2;
  const step = values.length > 1 ? plotWidth / (values.length - 1) : 0;

  const coords = values.map((value, index) => {
    const x = insetX + index * step;
    const normalized = (value - min) / range;
    const y = height - insetY - normalized * plotHeight;
    return { x, y };
  });

  const linePath = coords
    .map((point, index) => `${index === 0 ? "M" : "L"}${point.x.toFixed(2)} ${point.y.toFixed(2)}`)
    .join(" ");

  const fillPath = `${linePath} L${(width - insetX).toFixed(2)} ${height.toFixed(2)} L${insetX.toFixed(2)} ${height.toFixed(2)} Z`;
  const last = coords[coords.length - 1] ?? { x: width - insetX, y: height / 2 };

  return {
    linePath,
    fillPath,
    endX: last.x,
    endY: last.y,
  };
}