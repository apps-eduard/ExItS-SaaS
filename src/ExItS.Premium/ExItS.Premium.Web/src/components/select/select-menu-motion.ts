export const SELECT_MENU_MOTIONS = ["flip-up", "flip-down", "scale", "jump-up"] as const;

export type SelectMenuMotion = (typeof SELECT_MENU_MOTIONS)[number];

/** Same default as V1 Org EHG select-glass (`ORG_EHG_SELECT_GLASS_DEFAULT_MENU_TRANSITION`). */
export const SELECT_MENU_MOTION_DEFAULT: SelectMenuMotion = "scale";

export function isSelectMenuMotion(value: string): value is SelectMenuMotion {
  return (SELECT_MENU_MOTIONS as readonly string[]).includes(value);
}

export function applySelectValues(select: HTMLSelectElement, values: readonly string[]) {
  for (const node of select.options) node.selected = values.includes(node.value);
}
