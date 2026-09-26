export type UiStandardFilterCategory =
  | "all"
  | "buttons"
  | "messages"
  | "feedback"
  | "overlays"
  | "forms"
  | "upload"
  | "cards"
  | "selects"
  | "navigation"
  | "data"
  | "states";

export type UiStandardLiveCardId =
  | "buttons"
  | "messages"
  | "toasts"
  | "confirm"
  | "drawer"
  | "modal"
  | "status"
  | "forms"
  | "quantityStepper"
  | "upload"
  | "selects"
  | "nav"
  | "table"
  | "cards"
  | "states"
  | "dodont";

export const UI_STANDARD_FILTER_OPTIONS: ReadonlyArray<{
  id: UiStandardFilterCategory;
  label: string;
}> = [
  { id: "all", label: "All" },
  { id: "buttons", label: "Buttons" },
  { id: "messages", label: "Messages" },
  { id: "feedback", label: "Feedback" },
  { id: "overlays", label: "Overlays" },
  { id: "forms", label: "Forms" },
  { id: "upload", label: "Upload" },
  { id: "cards", label: "Cards" },
  { id: "selects", label: "Selects" },
  { id: "navigation", label: "Navigation" },
  { id: "data", label: "Data" },
  { id: "states", label: "States" },
];

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
    id: "toasts",
    title: "Toasts",
    category: "feedback",
    keywords: ["toast", "success", "info", "warning", "error", "notification"],
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
      "cleared",
      "declined",
      "cancelled",
      "playground",
      "auto",
      "shape",
      "appearance",
      "soft",
      "outline",
      "solid",
      "preferred",
      "snippet",
      "copy",
    ],
  },
  {
    id: "forms",
    title: "Form Controls",
    category: "forms",
    keywords: [
      "form",
      "input",
      "textarea",
      "checkbox",
      "validation",
      "disabled",
      "email",
    ],
  },
  {
    id: "quantityStepper",
    title: "QuantityStepper",
    category: "forms",
    keywords: [
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
    id: "selects",
    title: "Selects",
    category: "selects",
    keywords: [
      "select",
      "exitsselect",
      "multi",
      "multiselect",
      "pill",
      "size",
      "attribute",
      "search",
      "searchable",
      "switch",
      "combobox",
      "creatable",
      "settings",
      "segmented",
      "dropdown",
      "filter",
      "payment method",
      "playground",
      "snippet",
      "copy",
      "disabled",
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
    id: "cards",
    title: "Cards",
    category: "cards",
    keywords: [
      "card",
      "basic",
      "summary",
      "invoice",
      "kpi",
      "action",
      "entity",
      "product",
      "selectable",
      "status",
      "compact",
      "featured",
      "treatment",
      "bordered",
      "elevated",
      "purchase summary",
    ],
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
    summary: "Intent + appearance compose; shapes; states; icon motion",
    keywords: ["button", "primary", "neutral", "danger", "solid", "outline", "ghost", "elevated", "gradient", "save", "edit", "delete", "action"],
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
    component: "UiStandardsMessageGallery",
    standard: "Message (Diamond sample)",
    category: "messages",
    status: "Pilot",
    summary: "Diamond Message gallery: toast, severity, inline, filled/outlined/simple",
    keywords: ["message", "toast", "inline", "severity", "outlined", "simple", "diamond"],
  },
  {
    id: "toast",
    component: "ToastProvider / useExitsToast",
    standard: "Toast",
    category: "feedback",
    status: "Locked",
    summary: "Success / Info / Warning / Error with semantic icons",
    keywords: ["toast", "success", "info", "warning", "error"],
  },
  {
    id: "status-chip",
    component: "StatusChip",
    standard: "Chip",
    category: "feedback",
    status: "Locked",
    summary: "Tone + appearance + shape (auto follows Control Shape)",
    keywords: [
      "status",
      "chip",
      "pending",
      "active",
      "danger",
      "soft",
      "outline",
      "solid",
      "appearance",
      "auto",
      "standard",
      "pill",
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
    id: "input",
    component: "Input / SearchField / Switch",
    standard: "Form controls",
    category: "forms",
    status: "Locked",
    summary: "Canonical form fields, search, validation, disabled",
    keywords: [
      "form",
      "input",
      "search",
      "switch",
      "validation",
      "textarea",
      "checkbox",
      "email",
    ],
  },
  {
    id: "quantity-stepper",
    component: "QuantityStepper",
    standard: "Quantity stepper",
    category: "forms",
    status: "Locked",
    summary:
      "Standard = variant outline (surface + primary border, Control Shape). Also auto / field / standard / soft / pill. Forms filter card shows all capsule variants.",
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
    category: "selects",
    status: "Locked",
    summary: "Themed single-select listbox (standard + searchable)",
    keywords: ["select", "exitsselect", "dropdown", "searchable", "single"],
  },
  {
    id: "exits-multi-select",
    component: "ExitsMultiSelect",
    standard: "Multi select",
    category: "selects",
    status: "Pilot",
    summary: "Themed multi-select with select-all / clear / optional search",
    keywords: ["multi", "multiselect", "select", "checkbox", "search"],
  },
  {
    id: "exits-pill-select",
    component: "ExitsPillSelect",
    standard: "Pill select",
    category: "selects",
    status: "Pilot",
    summary: "In-flow attribute select — pill chips or tile (forms); single or multi",
    keywords: ["pill", "tile", "size", "attribute", "variant", "chip", "select", "multi"],
  },
  {
    id: "creatable-combobox",
    component: "CreatableCombobox",
    standard: "Creatable combobox",
    category: "selects",
    status: "Pilot",
    summary: "Searchable select with create-new free text",
    keywords: ["combobox", "creatable", "search", "select"],
  },
  {
    id: "settings-select",
    component: "SettingsSelect",
    standard: "Settings select",
    category: "selects",
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
    summary: "Underline / soft / pill / segmented selection",
    keywords: ["tabs", "segmented", "navigation", "filter"],
  },
  {
    id: "module-subnav",
    component: "ModuleSubnav",
    standard: "Module Subnav",
    category: "navigation",
    status: "Pilot",
    summary: "Route-module navigation patterns",
    keywords: ["module", "subnav", "navigation"],
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
    category: "cards",
    status: "Locked",
    summary: "Types, treatments, KPI, summary/invoice, selectable cards",
    keywords: ["card", "kpi", "entity", "summary", "invoice", "treatment", "featured"],
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

export function parseUiStandardCategoryParam(raw: string | null): UiStandardFilterCategory {
  const value = (raw ?? "").trim().toLowerCase();
  // Legacy Actions filter → Buttons
  if (value === "actions") {
    return "buttons";
  }
  const allowed = UI_STANDARD_FILTER_OPTIONS.map((o) => o.id);
  return (allowed.includes(value as UiStandardFilterCategory)
    ? value
    : "all") as UiStandardFilterCategory;
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
