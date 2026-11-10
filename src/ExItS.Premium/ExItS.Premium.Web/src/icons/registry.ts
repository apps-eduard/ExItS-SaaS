/** Ligatures included in the self-hosted Material Symbols Rounded subset. */
export const ICON_NAMES = [
  "account_balance_wallet",
  "add",
  "animation",
  "arrow_outward",
  "check",
  "check_box",
  "checklist",
  "close",
  "credit_card",
  "crop_square",
  "dashboard",
  "drag_handle",
  "edit_note",
  "error",
  "expand_more",
  "group",
  "inbox",
  "info",
  "interests",
  "layers",
  "left_click",
  "left_panel_close",
  "left_panel_open",
  "menu",
  "more_vert",
  "notifications",
  "palette",
  "payments",
  "radio_button_checked",
  "request_quote",
  "search",
  "sell",
  "smartphone",
  "south_west",
  "storefront",
  "straighten",
  "text_fields",
  "toggle_on",
  "tune",
  "unfold_more",
  "view_column",
  "warning",
  "widgets",
] as const;

export type IconName = (typeof ICON_NAMES)[number];

export const FEATURE_TONES = ["lent", "owe", "people", "stores", "reminders"] as const;

export type FeatureTone = (typeof FEATURE_TONES)[number];

export const FEATURE_ICONS: Record<FeatureTone, IconName> = {
  lent: "payments",
  owe: "request_quote",
  people: "group",
  stores: "storefront",
  reminders: "notifications",
};
