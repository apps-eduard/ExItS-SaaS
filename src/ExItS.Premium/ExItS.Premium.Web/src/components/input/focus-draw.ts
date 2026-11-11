import { INPUT_FOCUS_DRAW_STROKE } from "./focus-motion";

export type FocusDrawGeometry = {
  width: number;
  height: number;
  radius: number;
  startX: number;
  startY: number;
  endX: number;
  endY: number;
  left: string;
  right: string;
};

function px(value: number) {
  return String(Math.round(value * 100) / 100);
}

function corner(radius: number, sweep: 0 | 1, x: number, y: number) {
  if (radius <= 0) return `L ${px(x)} ${px(y)}`;
  return `A ${px(radius)} ${px(radius)} 0 0 ${sweep} ${px(x)} ${px(y)}`;
}

/**
 * Two strokes that start at the bottom center and meet at the top center.
 * Each path follows the field's real rounded-rect border, including pill ends.
 */
export function focusDrawGeometry(width: number, height: number, radius: number, stroke = INPUT_FOCUS_DRAW_STROKE): FocusDrawGeometry {
  const safeWidth = Math.max(width, stroke);
  const safeHeight = Math.max(height, stroke);
  const inset = stroke / 2;
  const maxRadius = Math.max(0, Math.min(safeWidth / 2 - inset, safeHeight / 2 - inset));
  const curve = Math.max(0, Math.min(radius, maxRadius));
  const leftEdge = inset;
  const rightEdge = safeWidth - inset;
  const top = inset;
  const bottom = safeHeight - inset;
  const center = safeWidth / 2;

  const left = [
    `M ${px(center)} ${px(bottom)}`,
    `L ${px(leftEdge + curve)} ${px(bottom)}`,
    corner(curve, 1, leftEdge, bottom - curve),
    `L ${px(leftEdge)} ${px(top + curve)}`,
    corner(curve, 1, leftEdge + curve, top),
    `L ${px(center)} ${px(top)}`,
  ].join(" ");

  const right = [
    `M ${px(center)} ${px(bottom)}`,
    `L ${px(rightEdge - curve)} ${px(bottom)}`,
    corner(curve, 0, rightEdge, bottom - curve),
    `L ${px(rightEdge)} ${px(top + curve)}`,
    corner(curve, 0, rightEdge - curve, top),
    `L ${px(center)} ${px(top)}`,
  ].join(" ");

  return {
    width: safeWidth,
    height: safeHeight,
    radius: curve,
    startX: center,
    startY: bottom,
    endX: center,
    endY: top,
    left,
    right,
  };
}

/** Mirror of the bottom stroke. Starts at the top middle and follows the real right border to the bottom middle. */
export function borderRevealGeometry(width: number, height: number, radius: number, stroke = INPUT_FOCUS_DRAW_STROKE): FocusDrawGeometry {
  const safeWidth = Math.max(width, stroke);
  const safeHeight = Math.max(height, stroke);
  const inset = stroke / 2;
  const maxRadius = Math.max(0, Math.min(safeWidth / 2 - inset, safeHeight / 2 - inset));
  const curve = Math.max(0, Math.min(radius, maxRadius));
  const leftEdge = inset;
  const rightEdge = safeWidth - inset;
  const top = inset;
  const bottom = safeHeight - inset;
  const center = safeWidth / 2;

  const down = [
    `M ${px(center)} ${px(top)}`,
    `L ${px(rightEdge - curve)} ${px(top)}`,
    corner(curve, 1, rightEdge, top + curve),
    `L ${px(rightEdge)} ${px(bottom - curve)}`,
    corner(curve, 1, rightEdge - curve, bottom),
    `L ${px(center)} ${px(bottom)}`,
  ].join(" ");

  const up = [
    `M ${px(center)} ${px(bottom)}`,
    `L ${px(leftEdge + curve)} ${px(bottom)}`,
    corner(curve, 1, leftEdge, bottom - curve),
    `L ${px(leftEdge)} ${px(top + curve)}`,
    corner(curve, 1, leftEdge + curve, top),
    `L ${px(center)} ${px(top)}`,
  ].join(" ");

  return {
    width: safeWidth,
    height: safeHeight,
    radius: curve,
    startX: center,
    startY: top,
    endX: center,
    endY: top,
    left: up,
    right: down,
  };
}
