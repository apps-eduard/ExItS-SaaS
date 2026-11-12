export const SELECT_MENU_MOTIONS = ["flip-up", "flip-down", "scale", "jump-up"] as const;

export type SelectMenuMotion = (typeof SELECT_MENU_MOTIONS)[number];

export const SELECT_MENU_MOTION_DEFAULT: SelectMenuMotion = "flip-down";

export function isSelectMenuMotion(value: string): value is SelectMenuMotion {
  return (SELECT_MENU_MOTIONS as readonly string[]).includes(value);
}

export function applySelectValues(select: HTMLSelectElement, values: readonly string[]) {
  for (const node of select.options) node.selected = values.includes(node.value);
}
