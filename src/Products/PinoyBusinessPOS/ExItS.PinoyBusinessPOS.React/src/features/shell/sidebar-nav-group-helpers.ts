/**
 * Single-item sections render as a flat link (no accordion header).
 * Avoids "OVERVIEW → Overview" / "SETTINGS → Preferences" duplication.
 */
export function isAccordionNavGroup(group: { items: ReadonlyArray<unknown> }): boolean {
  return group.items.length > 1;
}
