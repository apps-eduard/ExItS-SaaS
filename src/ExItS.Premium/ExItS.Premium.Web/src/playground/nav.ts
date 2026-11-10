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
  "buttons",
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

export type NavItem = {
  id: SectionId;
  label: string;
  icon: IconName;
};

export const NAV_GROUPS: Array<{ label: string; items: NavItem[] }> = [
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
      { id: "buttons", label: "Buttons", icon: "left_click" },
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

export function isSectionId(value: string): value is SectionId {
  return (SECTION_IDS as readonly string[]).includes(value);
}
