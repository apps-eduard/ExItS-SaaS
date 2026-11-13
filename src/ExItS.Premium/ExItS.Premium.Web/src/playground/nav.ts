import type { IconName } from "../icons/registry";

export const SECTION_IDS = [
  "overview",
  "colors",
  "typography",
  "spacing",
  "radius",
  "elevation",
  "motion",
  "icons",
  "button",
  "button-group",
  "button-dropdown",
  "inputs",
  "search",
  "selects",
  "date-time",
  "cards",
  "badges",
  "toggles",
  "checkboxes",
  "radio",
  "segmented",
  "advanced",
  "mobile-preview",
  "component-states",
  "personal-concept",
] as const;

export type SectionId = (typeof SECTION_IDS)[number];

/** Submenu readiness — shown as a colored status dot. */
export type NavStatus = "ready" | "next" | "planned";

export type NavLeaf = {
  id: SectionId;
  label: string;
  icon: IconName;
  status?: NavStatus;
};

export type NavBranch = {
  label: string;
  icon: IconName;
  children: NavLeaf[];
};

export const NAV_STATUS_LABELS: Record<NavStatus, string> = {
  ready: "Ready",
  next: "Coming next",
  planned: "Planned",
};

export type NavEntry = NavLeaf | NavBranch;

export type NavItem = NavLeaf;

export function isNavBranch(entry: NavEntry): entry is NavBranch {
  return "children" in entry;
}

export const NAV_GROUPS: Array<{ label: string; items: NavEntry[] }> = [
  {
    label: "Foundations",
    items: [
      { id: "overview", label: "Overview", icon: "dashboard" },
      { id: "colors", label: "Colors", icon: "palette" },
      { id: "typography", label: "Typography", icon: "text_fields" },
      { id: "spacing", label: "Spacing", icon: "straighten" },
      { id: "radius", label: "Radius", icon: "crop_square" },
      { id: "elevation", label: "Elevation", icon: "layers" },
      { id: "motion", label: "Motion", icon: "animation" },
      { id: "icons", label: "Icons", icon: "interests" },
    ],
  },
  {
    label: "Components",
    items: [
      {
        label: "Buttons",
        icon: "left_click",
        children: [
          { id: "button", label: "Button", icon: "left_click", status: "ready" },
          { id: "button-group", label: "Button group", icon: "view_column", status: "next" },
          { id: "button-dropdown", label: "Button dropdown", icon: "unfold_more", status: "planned" },
        ],
      },
      { id: "inputs", label: "Inputs", icon: "edit_note" },
      { id: "search", label: "Search", icon: "search" },
      { id: "selects", label: "Selects", icon: "unfold_more" },
      { id: "date-time", label: "Date & Time Pickers", icon: "edit_note" },
      { id: "cards", label: "Cards", icon: "credit_card" },
      { id: "badges", label: "Badges", icon: "sell" },
      { id: "toggles", label: "Toggles", icon: "toggle_on" },
      { id: "checkboxes", label: "Checkboxes", icon: "check_box" },
      { id: "radio", label: "Radio", icon: "radio_button_checked" },
      { id: "segmented", label: "Segmented Controls", icon: "view_column" },
      { id: "advanced", label: "Advanced Components", icon: "widgets" },
    ],
  },
  {
    label: "Previews",
    items: [
      { id: "mobile-preview", label: "Mobile Preview", icon: "smartphone" },
      { id: "component-states", label: "Component States", icon: "checklist" },
      { id: "personal-concept", label: "Personal UI Concept", icon: "account_balance_wallet" },
    ],
  },
];

/** Flatten leaves for lookup / active title. */
export function navLeaves(groups: typeof NAV_GROUPS = NAV_GROUPS): NavLeaf[] {
  return groups.flatMap((group) =>
    group.items.flatMap((entry) => (isNavBranch(entry) ? entry.children : [entry])),
  );
}

export function branchContains(branch: NavBranch, section: SectionId) {
  return branch.children.some((child) => child.id === section);
}

export function isSectionId(value: string): value is SectionId {
  return (SECTION_IDS as readonly string[]).includes(value);
}

/** Legacy `#buttons` opens the Button page. */
export function resolveSectionId(value: string): SectionId | null {
  if (value === "buttons") return "button";
  return isSectionId(value) ? value : null;
}
