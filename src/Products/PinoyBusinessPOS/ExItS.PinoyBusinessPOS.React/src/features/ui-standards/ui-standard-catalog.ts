export type UiStandardFilterCategory =
  | "all"
  | "buttons"
  | "button-glass"
  | "fab-glass"
  | "button-stepper"
  | "button-group"
  | "button-dropdown"
  | "inputs"
  | "input-glass"
  | "date-glass"
  | "quick-action-glass"
  | "card-glass-menu"
  | "card-glass-soft-kpi"
  | "card-glass-hybrid-kpi"
  | "card-glass-solid"
  | "card-glass-timeline"
  | "card-glass-table"
  | "card-glass-settings"
  | "radio"
  | "checkbox"
  | "toggle"
  | "dropdown"
  | "tabs-select"
  | "slider"
  | "range"
  | "date"
  | "time"
  | "breadcrumbs"
  | "messages"
  | "feedback"
  | "overlays"
  | "dialog"
  | "upload"
  | "card"
  | "card-receipt"
  | "shadow"
  | "tables"
  | "navigation"
  | "data"
  | "states"
  | "lucide"
  | "typography"
  /** Organization Enterprise Hybrid Glass (UI Standards live reference — not Personal Soft Glass). */
  | "org-ehg-dashboard"
  | "org-ehg-inventory"
  | "org-ehg-orders"
  | "org-ehg-stock-requests"
  | "org-ehg-branch-settings"
  | "org-ehg-button-glass"
  | "org-ehg-fab-glass"
  | "org-ehg-input-glass"
  | "org-ehg-date-glass"
  | "org-ehg-quick-action-glass"
  | "org-ehg-card-glass-menu"
  | "org-ehg-card-glass-soft-kpi"
  | "org-ehg-card-glass-hybrid-kpi"
  | "org-ehg-card-glass-solid"
  | "org-ehg-card-glass-timeline"
  | "org-ehg-card-glass-table"
  | "org-ehg-card-glass-settings";

export type UiStandardLiveCardId =
  | "buttons"
  | "buttonGroup"
  | "buttonDropdown"
  | "inputs"
  | "radio"
  | "checkbox"
  | "toggle"
  | "dropdown"
  | "tabsSelect"
  | "slider"
  | "range"
  | "date"
  | "time"
  | "breadcrumbs"
  | "messages"
  | "confirm"
  | "drawer"
  | "modal"
  | "dialogPlayground"
  | "dialog"
  | "status"
  | "quantityStepper"
  | "upload"
  | "nav"
  | "table"
  | "card"
  | "cardReceipt"
  | "shadow"
  | "tablePlayground"
  | "typographyPlayground"
  | "states"
  | "dodont";

export type UiStandardNavItem = {
  id: UiStandardFilterCategory;
  label: string;
};

export type UiStandardNavLink = {
  id: "google-icons";
  label: string;
  href: string;
};

/** Second-level (or nested) submenu inside a group (e.g. Glass components → Card glass). */
export type UiStandardNavSubgroup = {
  id: "select-components" | "glass-components" | "card-glass";
  label: string;
  children: readonly (UiStandardNavItem | UiStandardNavSubgroup)[];
};

export type UiStandardNavChild = UiStandardNavItem | UiStandardNavLink | UiStandardNavSubgroup;

export type UiStandardNavGroup = {
  id:
    | "button-components"
    | "form-components"
    | "glass-components"
    | "cards"
    | "icons"
    | "org-enterprise-hybrid-glass";
  label: string;
  children: readonly UiStandardNavChild[];
};

/** Which UI Standards surface the shell shortcut opens. */
export type UiStandardsProduct = "personal" | "organization";

export type UiStandardNavEntry = UiStandardNavItem | UiStandardNavGroup;

export function isUiStandardNavGroup(entry: UiStandardNavEntry): entry is UiStandardNavGroup {
  return "children" in entry;
}

export function isUiStandardNavSubgroup(child: UiStandardNavChild): child is UiStandardNavSubgroup {
  return "children" in child;
}

function navItemsOf(child: UiStandardNavChild): UiStandardNavItem[] {
  if (isUiStandardNavSubgroup(child)) {
    return child.children.flatMap((nested) =>
      isUiStandardNavSubgroup(nested) ? navItemsOf(nested) : [nested],
    );
  }
  return "href" in child ? [] : [child];
}

/** Card glass kinds under Glass components → Card glass. */
export const UI_STANDARD_CARD_GLASS_NAV: readonly UiStandardNavItem[] = [
  { id: "card-glass-menu", label: "Menu card" },
  { id: "card-glass-soft-kpi", label: "Soft KPI" },
  { id: "card-glass-hybrid-kpi", label: "Hybrid KPI" },
  { id: "card-glass-solid", label: "Solid data" },
  { id: "card-glass-timeline", label: "Activity timeline" },
  { id: "card-glass-table", label: "Orders table" },
  { id: "card-glass-settings", label: "Settings form" },
];

export const UI_STANDARD_ORG_CARD_GLASS_NAV: readonly UiStandardNavItem[] = [
  { id: "org-ehg-card-glass-menu", label: "Menu card" },
  { id: "org-ehg-card-glass-soft-kpi", label: "Soft KPI" },
  { id: "org-ehg-card-glass-hybrid-kpi", label: "Hybrid KPI" },
  { id: "org-ehg-card-glass-solid", label: "Solid data" },
  { id: "org-ehg-card-glass-timeline", label: "Activity timeline" },
  { id: "org-ehg-card-glass-table", label: "Orders table" },
  { id: "org-ehg-card-glass-settings", label: "Settings form" },
];

/**
 * Personal UI Standards sidenav — classic Soft Glass / component playground.
 * Opened from the Personal shell shortcut (`?product=personal`).
 */
export const UI_STANDARD_PERSONAL_NAV: readonly UiStandardNavEntry[] = [
  { id: "all", label: "Global preview" },
  {
    id: "button-components",
    label: "Buttons",
    children: [
      { id: "buttons", label: "Button" },
      { id: "button-stepper", label: "Button stepper" },
      { id: "button-group", label: "Button group" },
      { id: "button-dropdown", label: "Button dropdown" },
    ],
  },
  {
    id: "form-components",
    label: "Form Components",
    children: [
      { id: "inputs", label: "Input" },
      { id: "radio", label: "Radio" },
      { id: "checkbox", label: "Checkbox" },
      { id: "toggle", label: "Toggle" },
      {
        id: "select-components",
        label: "Selects",
        children: [
          { id: "dropdown", label: "Select" },
          { id: "tabs-select", label: "Tabs select" },
        ],
      },
      { id: "slider", label: "Slider" },
      { id: "range", label: "Range" },
      { id: "date", label: "Date picker" },
      { id: "time", label: "Time picker" },
    ],
  },
  {
    id: "glass-components",
    label: "Glass components",
    children: [
      { id: "button-glass", label: "Button glass" },
      { id: "fab-glass", label: "Floating action glass" },
      { id: "input-glass", label: "Input glass" },
      { id: "date-glass", label: "Date glass" },
      { id: "quick-action-glass", label: "Quick Action glass" },
      {
        id: "card-glass",
        label: "Card glass",
        children: UI_STANDARD_CARD_GLASS_NAV,
      },
    ],
  },
  { id: "breadcrumbs", label: "Breadcrumbs" },
  { id: "messages", label: "Messages" },
  { id: "feedback", label: "Feedback" },
  { id: "overlays", label: "Overlays" },
  { id: "dialog", label: "Dialog" },
  { id: "upload", label: "Upload" },
  {
    id: "cards",
    label: "Cards",
    children: [
      { id: "card", label: "Card" },
      { id: "card-receipt", label: "Card receipt" },
    ],
  },
  { id: "shadow", label: "Shadow" },
  { id: "tables", label: "Table" },
  { id: "navigation", label: "Navigation" },
  { id: "data", label: "Data" },
  { id: "states", label: "States" },
  {
    id: "icons",
    label: "Icons",
    children: [
      { id: "lucide", label: "Lucide" },
      { id: "google-icons", label: "Google icons", href: "https://fonts.google.com/icons" },
    ],
  },
  { id: "typography", label: "Typography" },
];

/**
 * Organization UI Standards sidenav — Enterprise Hybrid Glass live pages.
 * Opened from the Organization/business shell shortcut (`?product=organization`).
 */
export const UI_STANDARD_ORG_NAV: readonly UiStandardNavEntry[] = [
  {
    id: "org-enterprise-hybrid-glass",
    label: "Organization — Enterprise Hybrid Glass",
    children: [
      { id: "org-ehg-dashboard", label: "Organization Dashboard" },
      { id: "org-ehg-inventory", label: "Inventory & Warehouse" },
      { id: "org-ehg-orders", label: "Customer Orders & Payments" },
      { id: "org-ehg-stock-requests", label: "Stock Requests & Transfers" },
      { id: "org-ehg-branch-settings", label: "Branch Settings & Connected Commerce" },
      {
        id: "glass-components",
        label: "Glass components",
        children: [
          { id: "org-ehg-button-glass", label: "Button glass" },
          { id: "org-ehg-fab-glass", label: "Floating action glass" },
          { id: "org-ehg-input-glass", label: "Input glass" },
          { id: "org-ehg-date-glass", label: "Date glass" },
          { id: "org-ehg-quick-action-glass", label: "Quick Action glass" },
          {
            id: "card-glass",
            label: "Card glass",
            children: UI_STANDARD_ORG_CARD_GLASS_NAV,
          },
        ],
      },
    ],
  },
];

/** @deprecated Prefer UI_STANDARD_PERSONAL_NAV / UI_STANDARD_ORG_NAV by product. */
export const UI_STANDARD_NAV: readonly UiStandardNavEntry[] = UI_STANDARD_PERSONAL_NAV;

export const UI_STANDARD_PERSONAL_DEFAULT_CATEGORY: UiStandardFilterCategory = "all";
export const UI_STANDARD_ORG_DEFAULT_CATEGORY: UiStandardFilterCategory = "org-ehg-dashboard";

export const UI_STANDARD_FILTER_OPTIONS: readonly UiStandardNavItem[] = [
  ...UI_STANDARD_PERSONAL_NAV.flatMap((entry) =>
    isUiStandardNavGroup(entry) ? entry.children.flatMap(navItemsOf) : [entry],
  ),
  ...UI_STANDARD_ORG_NAV.flatMap((entry) =>
    isUiStandardNavGroup(entry) ? entry.children.flatMap(navItemsOf) : [entry],
  ),
];

export function parseUiStandardsProductParam(raw: string | null): UiStandardsProduct {
  const value = (raw ?? "").trim().toLowerCase();
  if (value === "organization" || value === "org") {
    return "organization";
  }
  return "personal";
}

export function uiStandardsHref(product: UiStandardsProduct): string {
  return product === "organization"
    ? `/ui-standards?product=organization&category=${UI_STANDARD_ORG_DEFAULT_CATEGORY}`
    : "/ui-standards";
}

export type UiStandardLiveCardDef = {
  id: UiStandardLiveCardId;
  title: string;
  category: Exclude<UiStandardFilterCategory, "all">;
  keywords: string[];
};

/** TASK-66A live cards — search/filter metadata. */
export const UI_STANDARD_LIVE_CARDS: ReadonlyArray<UiStandardLiveCardDef> = [
  {
    id: "buttons",
    title: "Buttons",
    category: "buttons",
    keywords: [
      "button",
      "buttons",
      "gallery",
      "severity",
      "raised",
      "text",
      "rounded",
      "outlined",
      "save",
      "create",
      "add",
      "edit",
      "cancel",
      "activate",
      "deactivate",
      "delete",
      "primary",
      "neutral",
      "secondary",
      "danger",
      "success",
      "warning",
      "info",
      "solid",
      "outline",
      "ghost",
      "elevated",
      "gradient",
      "loading",
      "icon",
      "table action",
      "hierarchy",
      "intent",
      "appearance",
      "playground",
      "auto",
      "shape",
      "pill",
      "round",
      "snippet",
      "copy",
      "actions",
    ],
  },
  {
    id: "buttonGroup",
    title: "Button group",
    category: "button-group",
    keywords: ["button", "group", "segmented", "toggle", "toolbar", "spread", "btngroup"],
  },
  {
    id: "buttonDropdown",
    title: "Button dropdown",
    category: "button-dropdown",
    keywords: ["button", "dropdown", "menu", "split", "actions", "more", "btndropdown"],
  },
  {
    id: "inputs",
    title: "Input",
    category: "inputs",
    keywords: [
      "input",
      "inputs",
      "field",
      "text",
      "playground",
      "standard",
      "filled",
      "outlined",
      "standout",
      "rounded",
      "clearable",
      "required",
      "type",
      "password",
      "email",
      "textarea",
      "label",
    ],
  },
  {
    id: "breadcrumbs",
    title: "Breadcrumbs",
    category: "breadcrumbs",
    keywords: ["breadcrumb", "breadcrumbs", "basic", "separator", "home", "navigation"],
  },
  {
    id: "radio",
    title: "Radio",
    category: "radio",
    keywords: ["radio", "radios", "option", "form"],
  },
  {
    id: "checkbox",
    title: "Checkbox",
    category: "checkbox",
    keywords: ["checkbox", "checkboxes", "check", "form"],
  },
  {
    id: "toggle",
    title: "Toggle",
    category: "toggle",
    keywords: ["toggle", "switch", "form"],
  },
  {
    id: "dropdown",
    title: "Dropdown",
    category: "dropdown",
    keywords: ["dropdown", "select", "menu", "form", "qselect"],
  },
  {
    id: "tabsSelect",
    title: "Tabs select",
    category: "tabs-select",
    keywords: [
      "tabs",
      "tab",
      "qtabs",
      "basic tabs",
      "select",
      "tablist",
      "inline label",
      "no caps",
      "icons only",
      "scroll",
    ],
  },
  {
    id: "slider",
    title: "Slider",
    category: "slider",
    keywords: ["slider", "qslider", "range", "track", "thumb", "form"],
  },
  {
    id: "range",
    title: "Range",
    category: "range",
    keywords: ["range", "qrange", "slider", "min", "max", "form"],
  },
  {
    id: "date",
    title: "Date picker",
    category: "date",
    keywords: ["date", "datepicker", "calendar", "qdate", "form"],
  },
  {
    id: "time",
    title: "Time picker",
    category: "time",
    keywords: ["time", "timepicker", "clock", "qtime", "form"],
  },
  {
    id: "messages",
    title: "Messages",
    category: "messages",
    keywords: [
      "message",
      "messages",
      "toast",
      "severity",
      "inline",
      "outlined",
      "simple",
      "validation",
      "notice",
      "alert",
      "success",
      "info",
      "warn",
      "error",
      "secondary",
      "contrast",
      "gallery",
      "diamond",
    ],
  },
  {
    id: "confirm",
    title: "Confirm Dialog",
    category: "overlays",
    keywords: [
      "confirm",
      "dialog",
      "deactivate",
      "delete",
      "danger",
      "warning",
      "confirmactiondialog",
    ],
  },
  {
    id: "drawer",
    title: "Form Drawer",
    category: "overlays",
    keywords: ["drawer", "formdrawer", "edit", "customer", "save"],
  },
  {
    id: "modal",
    title: "Modal",
    category: "overlays",
    keywords: ["modal", "exitsmodal", "payment", "record payment"],
  },
  {
    id: "dialogPlayground",
    title: "Dialog playground",
    category: "dialog",
    keywords: [
      "dialog",
      "modal",
      "exitsmodal",
      "playground",
      "intent",
      "size",
      "fullscreen",
      "persistent",
      "footer",
      "snippet",
      "copy",
    ],
  },
  {
    id: "dialog",
    title: "Dialog samples",
    category: "dialog",
    keywords: ["dialog", "modal", "alert", "confirm", "delete", "form", "scroll", "persistent"],
  },
  {
    id: "status",
    title: "Status & Chips",
    category: "feedback",
    keywords: [
      "status",
      "chip",
      "draft",
      "pending",
      "active",
      "danger",
      "warning",
      "success",
      "info",
      "neutral",
      "primary",
      "secondary",
      "help",
      "contrast",
      "tone",
      "color",
      "cleared",
      "declined",
      "cancelled",
      "playground",
      "auto",
      "shape",
      "square",
      "appearance",
      "soft",
      "emphasis",
      "outline",
      "solid",
      "preferred",
      "snippet",
      "copy",
    ],
  },
  {
    id: "quantityStepper",
    title: "QuantityStepper",
    category: "button-stepper",
    keywords: [
      "button",
      "increment",
      "decrement",
      "quantity",
      "stepper",
      "auto",
      "capsule",
      "cart",
      "control shape",
      "pill",
      "soft",
      "standard",
      "editOnClick",
      "variant",
    ],
  },
  {
    id: "upload",
    title: "Upload",
    category: "upload",
    keywords: [
      "upload",
      "file",
      "image",
      "dropzone",
      "cover",
      "gallery",
      "tile",
      "button",
      "drag",
      "drop",
      "exitsupload",
    ],
  },
  {
    id: "nav",
    title: "Navigation & Selection",
    category: "navigation",
    keywords: ["tabs", "underline", "soft", "pill", "segmented", "filter", "navigation"],
  },
  {
    id: "table",
    title: "Responsive Data View",
    category: "data",
    keywords: [
      "table",
      "exitstable",
      "responsive",
      "list",
      "card",
      "customer",
      "balance",
      "actions",
      "edit",
      "more",
      "desktop",
      "tablet",
      "mobile",
      "preview",
      "data",
    ],
  },
  {
    id: "card",
    title: "Card",
    category: "card",
    keywords: ["card", "qcard", "basic", "flat", "bordered", "square", "dark", "gradient", "playground"],
  },
  {
    id: "cardReceipt",
    title: "Card receipt",
    category: "card-receipt",
    keywords: ["card", "receipt", "invoice", "purchase", "summary", "document"],
  },
  {
    id: "shadow",
    title: "Shadow",
    category: "shadow",
    keywords: ["shadow", "elevation", "inset", "small", "medium", "large", "playground"],
  },
  {
    id: "tablePlayground",
    title: "Table",
    category: "tables",
    keywords: ["table", "qtable", "separator", "density", "flat", "search", "export", "filter", "playground"],
  },
  {
    id: "typographyPlayground",
    title: "Typography",
    category: "typography",
    keywords: ["typography", "font", "font-weight", "font-size", "playground"],
  },
  {
    id: "states",
    title: "States",
    category: "states",
    keywords: ["empty", "loading", "error", "retry", "emptystate", "loadingstate", "errorstate"],
  },
  {
    id: "dodont",
    title: "Do / Don’t",
    category: "buttons",
    keywords: [
      "do",
      "dont",
      "don't",
      "primary",
      "hierarchy",
      "intent",
      "appearance",
      "soft",
      "shape",
      "outline",
      "responsive",
      "table",
      "list",
      "guidance",
      "playground",
    ],
  },
];

/** Compact index of locked/pilot standards for filterable reference (ExitsTable). */
export type UiStandardCatalogRow = {
  id: string;
  component: string;
  standard: string;
  category: Exclude<UiStandardFilterCategory, "all">;
  status: "Locked" | "Pilot";
  summary: string;
  keywords: string[];
};

export const UI_STANDARD_CATALOG_ROWS: ReadonlyArray<UiStandardCatalogRow> = [
  {
    id: "button",
    component: "Button",
    standard: "Button",
    category: "buttons",
    status: "Locked",
    summary:
      "Locked global ExItS Button standard. Cursor command ext btn from the Button playground.",
    keywords: [
      "button",
      "primary",
      "neutral",
      "secondary",
      "danger",
      "help",
      "contrast",
      "solid",
      "outline",
      "ghost",
      "elevated",
      "gradient",
      "save",
      "edit",
      "delete",
      "action",
      "diamond",
      "gallery",
      "severity",
    ],
  },
  {
    id: "input-playground",
    component: "Input playground",
    standard: "Input",
    category: "inputs",
    status: "Locked",
    summary:
      "Locked global ExItS Input standard. Cursor command ext input from the Input playground.",
    keywords: [
      "input",
      "field",
      "filled",
      "outlined",
      "standout",
      "rounded",
      "clearable",
      "required",
      "password",
      "email",
      "textarea",
      "date",
      "time",
      "mobile",
      "playground",
    ],
  },
  {
    id: "dropdown",
    component: "Dropdown playground",
    standard: "Dropdown",
    category: "dropdown",
    status: "Locked",
    summary: "Locked global ExItS Dropdown standard. Cursor command ext dropdown from the Dropdown playground.",
    keywords: ["dropdown", "select", "menu", "multi", "transition", "playground"],
  },
  {
    id: "slider",
    component: "Slider playground",
    standard: "Slider",
    category: "slider",
    status: "Locked",
    summary: "Locked global ExItS Slider standard. Cursor command ext slider from the Slider playground.",
    keywords: ["slider", "thumb", "markers", "snap", "playground"],
  },
  {
    id: "range",
    component: "Range playground",
    standard: "Range",
    category: "range",
    status: "Locked",
    summary: "Locked global ExItS Range standard. Cursor command ext range from the Range playground.",
    keywords: ["range", "min", "max", "drag", "playground"],
  },
  {
    id: "date",
    component: "Date picker playground",
    standard: "Date",
    category: "date",
    status: "Locked",
    summary: "Locked global ExItS Date picker standard. Cursor command ext date from the Date picker playground.",
    keywords: ["date", "calendar", "roll", "range", "today", "playground"],
  },
  {
    id: "time",
    component: "Time picker playground",
    standard: "Time",
    category: "time",
    status: "Pilot",
    summary: "Pilot ExItS Time picker. Cursor command ext time from the Time picker playground.",
    keywords: ["time", "clock", "roll", "24h", "now", "playground"],
  },
  {
    id: "action-semantics",
    component: "EXITS_ACTIONS / action-icons",
    standard: "Action semantics",
    category: "buttons",
    status: "Locked",
    summary: "Canonical icons + default intents + primary hierarchy",
    keywords: ["action", "icon", "save", "create", "delete", "hierarchy", "primary"],
  },
  {
    id: "table-action",
    component: "TableActionButton",
    standard: "Table actions",
    category: "buttons",
    status: "Locked",
    summary: "Compact icon-only row actions with aria-label + tooltip",
    keywords: ["table", "action", "edit", "delete", "icon"],
  },
  {
    id: "message-gallery",
    component: "Notice / useExitsToast",
    standard: "Message",
    category: "messages",
    status: "Locked",
    summary:
      "Diamond Full sample is the locked Message standard; product uses Notice + Toast (severity showcase-only)",
    keywords: [
      "message",
      "notice",
      "toast",
      "inline",
      "severity",
      "outlined",
      "simple",
      "diamond",
      "gallery",
    ],
  },
  {
    id: "status-chip",
    component: "StatusChip",
    standard: "Chip",
    category: "feedback",
    status: "Locked",
    summary: "Diamond tones (Primary·Secondary·Success·Info·Warn·Help·Danger·Contrast) + Neutral; appearance + shape",
    keywords: [
      "status",
      "chip",
      "pending",
      "active",
      "danger",
      "secondary",
      "help",
      "contrast",
      "soft",
      "outline",
      "solid",
      "emphasis",
      "appearance",
      "auto",
      "standard",
      "pill",
      "square",
      "tone",
      "color",
      "shape",
    ],
  },
  {
    id: "confirm",
    component: "ConfirmActionDialog",
    standard: "Confirm dialog",
    category: "overlays",
    status: "Locked",
    summary: "Default / info / warning / danger confirmations",
    keywords: ["confirm", "dialog", "delete", "deactivate", "danger", "warning"],
  },
  {
    id: "form-drawer",
    component: "FormDrawer",
    standard: "Form Drawer",
    category: "overlays",
    status: "Locked",
    summary: "Default entity create/edit shell (desktop drawer / mobile full)",
    keywords: ["drawer", "formdrawer", "edit", "save"],
  },
  {
    id: "exits-modal",
    component: "ExitsModal",
    standard: "Modal",
    category: "overlays",
    status: "Locked",
    summary: "Short contextual modal shell",
    keywords: ["modal", "exitsmodal", "payment"],
  },
  {
    id: "exits-dialog",
    component: "ExitsModal",
    standard: "Dialog",
    category: "dialog",
    status: "Pilot",
    summary: "Dialog intent, size, mobile height, and dismiss options. Cursor command ext dialog from the Dialog playground.",
    keywords: ["dialog", "modal", "exitsmodal", "playground", "confirm", "persistent"],
  },
  {
    id: "quantity-stepper",
    component: "QuantityStepper",
    standard: "Quantity stepper",
    category: "button-stepper",
    status: "Locked",
    summary:
      "Standard = variant outline (surface + primary border, Control Shape). Also auto / field / standard / soft / pill. Button stepper card shows all capsule variants.",
    keywords: [
      "quantity",
      "stepper",
      "outline",
      "auto",
      "field",
      "capsule",
      "cart",
      "control shape",
      "pill",
      "soft",
      "standard",
      "editOnClick",
      "variant",
    ],
  },
  {
    id: "exits-upload",
    component: "ExitsUpload",
    standard: "Upload",
    category: "upload",
    status: "Pilot",
    summary: "Dropzone cover, gallery tiles, compact Upload button — no network in the control",
    keywords: ["upload", "dropzone", "tile", "image", "file", "gallery", "button"],
  },
  {
    id: "exits-select",
    component: "ExitsSelect",
    standard: "Select",
    category: "dropdown",
    status: "Locked",
    summary: "Themed single-select listbox (standard + searchable)",
    keywords: ["select", "exitsselect", "dropdown", "searchable", "single"],
  },
  {
    id: "exits-multi-select",
    component: "ExitsMultiSelect",
    standard: "Multi select",
    category: "dropdown",
    status: "Pilot",
    summary: "Themed multi-select with select-all / clear / optional search",
    keywords: ["multi", "multiselect", "select", "checkbox", "search"],
  },
  {
    id: "exits-pill-select",
    component: "ExitsPillSelect",
    standard: "Pill select",
    category: "tabs-select",
    status: "Pilot",
    summary: "In-flow attribute select — pill chips or tile (forms); single or multi",
    keywords: ["pill", "tile", "size", "attribute", "variant", "chip", "select", "multi"],
  },
  {
    id: "creatable-combobox",
    component: "CreatableCombobox",
    standard: "Creatable combobox",
    category: "dropdown",
    status: "Pilot",
    summary: "Searchable select with create-new free text",
    keywords: ["combobox", "creatable", "search", "select"],
  },
  {
    id: "settings-select",
    component: "SettingsSelect",
    standard: "Settings select",
    category: "tabs-select",
    status: "Locked",
    summary: "In-flow cards / segmented preference choices (not a popup)",
    keywords: ["settings", "segmented", "cards", "theme", "select"],
  },
  {
    id: "tabs",
    component: "ExitsTabs / UnderlineTabBar",
    standard: "Tabs",
    category: "navigation",
    status: "Locked",
    summary: "Underline / soft / pill / segmented selection (plus filter chip tabs)",
    keywords: ["tabs", "underline", "soft", "pill", "segmented", "navigation", "filter"],
  },
  {
    id: "module-subnav",
    component: "ModuleSubnav",
    standard: "Module Subnav",
    category: "navigation",
    status: "Pilot",
    summary: "Underline / pillBar / vertical route-module destinations",
    keywords: ["module", "subnav", "navigation", "underline", "pill", "pillBar", "vertical"],
  },
  {
    id: "exits-table",
    component: "ExitsTable",
    standard: "Table",
    category: "data",
    status: "Locked",
    summary: "Full table demo: actions, inline edit, alignment, output",
    keywords: ["table", "exitstable", "inline", "actions", "sku"],
  },
  {
    id: "responsive-data-view",
    component: "ExitsResponsiveDataView / ExitsDataRecordCard",
    standard: "Responsive Data View",
    category: "data",
    status: "Pilot",
    summary: "TABLE on wide viewports; LIST/cards on narrow; h-scroll exception only",
    keywords: [
      "responsive",
      "table",
      "list",
      "card",
      "mobile",
      "tablet",
      "desktop",
      "preview",
      "data",
      "priority",
      "multiselect",
    ],
  },
  {
    id: "card",
    component: "Card",
    standard: "Card",
    category: "card",
    status: "Pilot",
    summary: "Playground card with intent, header, body, and footer settings",
    keywords: ["card", "qcard", "basic", "flat", "bordered", "square", "dark", "playground"],
  },
  {
    id: "card-receipt",
    component: "CustomerPurchaseSummaryDocument",
    standard: "Card receipt",
    category: "card-receipt",
    status: "Pilot",
    summary: "Sale receipt sample using the customer purchase summary document",
    keywords: ["card", "receipt", "invoice", "purchase", "summary"],
  },
  {
    id: "shadow",
    component: "Shadow",
    standard: "Shadow",
    category: "shadow",
    status: "Pilot",
    summary: "Playground for none, small, medium, and large elevation, with inset",
    keywords: ["shadow", "elevation", "inset", "playground"],
  },
  {
    id: "table-playground",
    component: "Table",
    standard: "Table",
    category: "tables",
    status: "Pilot",
    summary: "Playground table with table, header, footer, and features cards. Border stays on.",
    keywords: ["table", "qtable", "separator", "density", "action", "intent", "playground"],
  },
  {
    id: "empty",
    component: "EmptyState",
    standard: "Empty state",
    category: "states",
    status: "Locked",
    summary: "Canonical empty content region",
    keywords: ["empty", "emptystate"],
  },
  {
    id: "loading",
    component: "LoadingState",
    standard: "Loading state",
    category: "states",
    status: "Locked",
    summary: "Canonical loading label/region",
    keywords: ["loading", "loadingstate"],
  },
  {
    id: "error",
    component: "ErrorState",
    standard: "Error state",
    category: "states",
    status: "Locked",
    summary: "Canonical error region + retry pattern",
    keywords: ["error", "errorstate", "retry"],
  },
];

function normalizeQuery(q: string): string {
  return q.trim().toLowerCase().replace(/\s+/g, " ");
}

function matchesQuery(haystack: string[], query: string): boolean {
  if (!query) return true;
  const q = normalizeQuery(query);
  return haystack.some((part) => part.toLowerCase().includes(q));
}

export function parseUiStandardCategoryParam(
  raw: string | null,
  product: UiStandardsProduct = "personal",
): UiStandardFilterCategory {
  const value = (raw ?? "").trim().toLowerCase();
  const fallback =
    product === "organization"
      ? UI_STANDARD_ORG_DEFAULT_CATEGORY
      : UI_STANDARD_PERSONAL_DEFAULT_CATEGORY;
  // Legacy Actions filter → Buttons
  if (value === "actions") {
    return "buttons";
  }
  if (value === "cards") {
    return "card";
  }
  // Legacy Org EHG sample ids → live five-page set
  // Background options live inside glass playgrounds (not a separate page).
  if (
    value === "org-ehg-overview" ||
    value === "org-ehg-components" ||
    value === "org-ehg-appearance"
  ) {
    return "org-ehg-dashboard";
  }
  if (value === "org-ehg-forms") {
    return "org-ehg-branch-settings";
  }
  if (value === "menu-glass" || value === "card-glass") {
    return "card-glass-menu";
  }
  if (value === "org-ehg-menu-glass" || value === "org-ehg-card-glass") {
    return "org-ehg-card-glass-menu";
  }
  if (value === "personal-soft-glass") {
    return fallback;
  }
  if (value === "") {
    return fallback;
  }
  const allowed = UI_STANDARD_FILTER_OPTIONS.map((o) => o.id);
  if (allowed.includes(value as UiStandardFilterCategory)) {
    const parsed = value as UiStandardFilterCategory;
    if (product === "organization" && !parsed.startsWith("org-ehg-")) {
      return UI_STANDARD_ORG_DEFAULT_CATEGORY;
    }
    if (product === "personal" && parsed.startsWith("org-ehg-")) {
      return UI_STANDARD_PERSONAL_DEFAULT_CATEGORY;
    }
    return parsed;
  }
  return fallback;
}

export function filterLiveCards(
  category: UiStandardFilterCategory,
  query: string,
): UiStandardLiveCardDef[] {
  return UI_STANDARD_LIVE_CARDS.filter((card) => {
    if (category !== "all" && card.category !== category) return false;
    return matchesQuery([card.title, card.id, ...card.keywords], query);
  });
}

export function filterCatalogRows(
  category: UiStandardFilterCategory,
  query: string,
): UiStandardCatalogRow[] {
  return UI_STANDARD_CATALOG_ROWS.filter((row) => {
    if (category !== "all" && row.category !== category) return false;
    return matchesQuery(
      [row.component, row.standard, row.summary, row.status, ...row.keywords],
      query,
    );
  });
}
