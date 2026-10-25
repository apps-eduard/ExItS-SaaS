import { Fragment, jsx, jsxs } from "react/jsx-runtime";
import { useEffect, useMemo, useRef, useState } from "react";
import { usePreferences } from "@/hooks/usePreferences";
import { ensurePersonalSymbols } from "@/components/icons/MaterialSymbol";
import { CompactSelect, PlaygroundPreviewCard, PlaygroundSearchField, playgroundIntentColor } from "@/features/ui-standards/UiStandardsPlaygrounds";
import {
  ExitsFileFormatIcon,
  ExitsPrintFormatIcon,
  exitsTablePageSizesForTotal
} from "@/components/exits/ExitsTable";
import {
  TABLE_EXPORT_GLOBAL_COLOR,
  TABLE_EXPORT_GLOBAL_CSV_INTENT,
  TABLE_EXPORT_GLOBAL_EXCEL_INTENT,
  TABLE_EXPORT_GLOBAL_PDF_INTENT,
  TABLE_EXPORT_GLOBAL_PRINT_INTENT
} from "@/features/ui-standards/table-export-global-default";
import { RADIO_STANDARD_COLOR } from "@/features/ui-standards/UiStandardsRadioGallery";
import { UiStandardsQslider } from "@/features/ui-standards/UiStandardsSliderGallery";
import { SettingsSelect } from "@/components/ui/settings-select";
import { Switch } from "@/components/ui/switch";
import { ExitsTabs } from "@/components/exits/ExitsTabs";
import { UiStandardsQcheckbox } from "@/features/ui-standards/UiStandardsCheckboxGallery";
import { useResponsiveDataLayout } from "@/components/exits/useResponsiveDataLayout";
import {
  UI_STANDARDS_DATA_PREVIEW_DEFAULT,
  UI_STANDARDS_DATA_PREVIEW_WIDTHS,
  uiStandardsDataPreviewLabel
} from "@/features/ui-standards/UiStandardsResponsiveDataDeviceFrame";
import { INPUT_SAMPLES, inputAccent } from "@/features/ui-standards/UiStandardsInputGallery";
import { PlaygroundSection } from "@/features/ui-standards/UiStandardsSnippetBlock";
import { UiStandardsCopyCommand } from "@/features/ui-standards/UiStandardsCopyCommand";
const HEADER_APPEARANCES = [
  { label: "Standard", value: "standard" },
  { label: "Soft", value: "soft" },
  { label: "Soft outline", value: "soft-outline" },
  { label: "Raised", value: "raised" },
  { label: "Ghost", value: "ghost" },
  { label: "Glossy", value: "glossy" },
  { label: "Gradient", value: "gradient" },
  { label: "Rounded", value: "rounded" },
  { label: "Flat", value: "flat" }
];
const DENSITY_OPTIONS = [
  { label: "Compact", value: "compact" },
  { label: "Standard", value: "balance" },
  { label: "Comfort", value: "comfort" }
];
const SEPARATOR_OPTIONS = [
  { label: "Horizontal", value: "horizontal" },
  { label: "Vertical", value: "vertical" },
  { label: "Cell", value: "cell" },
  { label: "None", value: "none" }
];
const ACTION_OPTIONS = [
  { label: "View", value: "view", icon: "visibility", labelFor: "View" },
  { label: "Edit", value: "edit", icon: "edit", labelFor: "Edit" },
  { label: "Delete", value: "delete", icon: "delete", labelFor: "Delete" },
  { label: "More", value: "more", icon: "more_horiz", labelFor: "More actions for" }
];
const SHAPE_OPTIONS = [
  { label: "Round", value: "round" },
  { label: "Square", value: "square" },
  { label: "Rounded", value: "rounded" }
];
const PAGE_SIZES = [10, 25, 50, 100, 200, 500];
const DEFAULT_PAGE_SIZE = 10;
const DEFAULT_ELEVATION = 3;
const ELEVATION_SHADOWS = [
  "none",
  "0 1px 3px rgba(0, 0, 0, 0.2), 0 1px 1px rgba(0, 0, 0, 0.14), 0 2px 1px -1px rgba(0, 0, 0, 0.12)",
  "0 1px 5px rgba(0, 0, 0, 0.2), 0 2px 2px rgba(0, 0, 0, 0.14), 0 3px 1px -2px rgba(0, 0, 0, 0.12)",
  "0 2px 4px -1px rgba(0, 0, 0, 0.2), 0 4px 5px rgba(0, 0, 0, 0.14), 0 1px 10px rgba(0, 0, 0, 0.12)",
  "0 3px 5px -1px rgba(0, 0, 0, 0.2), 0 6px 10px rgba(0, 0, 0, 0.14), 0 1px 18px rgba(0, 0, 0, 0.12)"
];
function stepCss(steps, value) {
  return steps[Math.min(steps.length, Math.max(1, value)) - 1] ?? steps[0];
}
const COLUMNS = [
  { id: "dessert", label: "Dessert", numeric: false },
  { id: "calories", label: "Calories", numeric: true },
  { id: "fat", label: "Fat (g)", numeric: true },
  { id: "carbs", label: "Carbs (g)", numeric: true },
  { id: "protein", label: "Protein (g)", numeric: true }
];
const EXPORT_OPTIONS = [
  { label: "CSV", value: "csv", format: "csv", intent: TABLE_EXPORT_GLOBAL_CSV_INTENT, color: TABLE_EXPORT_GLOBAL_COLOR.csv },
  { label: "Excel", value: "excel", format: "xlsx", intent: TABLE_EXPORT_GLOBAL_EXCEL_INTENT, color: TABLE_EXPORT_GLOBAL_COLOR.xlsx },
  { label: "PDF", value: "pdf", format: "pdf", intent: TABLE_EXPORT_GLOBAL_PDF_INTENT, color: TABLE_EXPORT_GLOBAL_COLOR.pdf },
  { label: "Print", value: "print", intent: TABLE_EXPORT_GLOBAL_PRINT_INTENT, color: TABLE_EXPORT_GLOBAL_COLOR.print }
];
const EDIT_KIND_OPTIONS = [
  { label: "Inline", value: "inline" },
  { label: "Dialog", value: "dialog" }
];
function initialRows() {
  return ROWS.map((row) => ({ id: row.dessert, ...row }));
}
const ROWS = [
  { dessert: "Frozen Yogurt", calories: "159", fat: "6.0", carbs: "24", protein: "4.0" },
  { dessert: "Ice cream sandwich", calories: "237", fat: "9.0", carbs: "37", protein: "4.3" },
  { dessert: "Eclair", calories: "262", fat: "16.0", carbs: "23", protein: "6.0" },
  { dessert: "Cupcake", calories: "305", fat: "3.7", carbs: "67", protein: "4.3" },
  { dessert: "Gingerbread", calories: "356", fat: "16.0", carbs: "49", protein: "3.9" },
  { dessert: "Donut", calories: "452", fat: "25.0", carbs: "51", protein: "4.9" },
  { dessert: "Jelly bean", calories: "375", fat: "0.0", carbs: "94", protein: "0.0" },
  { dessert: "KitKat", calories: "518", fat: "26.0", carbs: "65", protein: "7.0" },
  { dessert: "Lollipop", calories: "392", fat: "0.2", carbs: "98", protein: "0.0" },
  { dessert: "Honeycomb", calories: "408", fat: "3.2", carbs: "87", protein: "6.5" },
  { dessert: "Nougat", calories: "360", fat: "8.0", carbs: "73", protein: "2.2" },
  { dessert: "Marshmallow", calories: "318", fat: "0.2", carbs: "81", protein: "1.8" },
  { dessert: "Brownie", calories: "466", fat: "29.0", carbs: "50", protein: "5.4" },
  { dessert: "Cheesecake", calories: "321", fat: "23.0", carbs: "26", protein: "5.5" },
  { dessert: "Macaron", calories: "407", fat: "21.0", carbs: "49", protein: "7.1" },
  { dessert: "Tiramisu", calories: "283", fat: "15.0", carbs: "28", protein: "5.2" },
  { dessert: "Pudding", calories: "158", fat: "4.6", carbs: "26", protein: "3.8" },
  { dessert: "Cookie", calories: "488", fat: "24.0", carbs: "64", protein: "5.9" },
  { dessert: "Waffle", calories: "291", fat: "14.0", carbs: "33", protein: "7.9" },
  { dessert: "Pancake", calories: "227", fat: "9.0", carbs: "28", protein: "6.4" },
  { dessert: "Muffin", calories: "377", fat: "16.0", carbs: "53", protein: "5.4" },
  { dessert: "Tart", calories: "274", fat: "14.0", carbs: "34", protein: "3.1" },
  { dessert: "Cannoli", calories: "339", fat: "18.0", carbs: "38", protein: "6.8" },
  { dessert: "Baklava", calories: "428", fat: "23.0", carbs: "50", protein: "6.2" },
  { dessert: "Churro", calories: "385", fat: "19.0", carbs: "48", protein: "4.1" }
];
function exportIntentColor(option) {
  return option.color;
}
function rowSelectId(dessert) {
  return dessert.toLowerCase().replace(/\s+/g, "-");
}
function tableCursorCommand(intent, separator, density, elevation, loading, alternate, multiSelect, actions, iconShape, editKind, editable, headerIntent, headerAppearance, footerOn, footerIntent, footerAppearance, sort, search, filter, exports) {
  const parts = ["ext table"];
  if (intent !== "standard") parts.push(intent);
  if (alternate) parts.push("alternate");
  if (separator !== "horizontal") parts.push(`separator-${separator}`);
  if (density !== "balance") parts.push(density);
  if (elevation === 1) parts.push("flat");
  else if (elevation !== DEFAULT_ELEVATION) parts.push(`raised-${elevation}`);
  if (loading) parts.push("loading");
  if (multiSelect) parts.push("multi-select");
  for (const option of ACTION_OPTIONS) {
    if (actions.includes(option.value)) parts.push(`action-${option.value}`);
  }
  if (actions.length > 0 && iconShape !== "round") parts.push(`icon-${iconShape}`);
  if (editKind === "dialog") parts.push("edit-dialog");
  if (editable.length === 0) parts.push("editable-none");
  else if (editable.length !== COLUMNS.length) {
    for (const column of COLUMNS) {
      if (editable.includes(column.id)) parts.push(`editable-${column.id}`);
    }
  }
  if (headerIntent !== "standard") parts.push(`header-${headerIntent}`);
  if (headerAppearance !== "standard") parts.push(`header-${headerAppearance}`);
  if (!footerOn) parts.push("no-footer");
  else {
    if (footerIntent !== "standard") parts.push(`footer-${footerIntent}`);
    if (footerAppearance !== "standard") parts.push(`footer-${footerAppearance}`);
  }
  if (sort) parts.push("sort");
  if (search) parts.push("search");
  for (const option of EXPORT_OPTIONS) {
    if (exports.includes(option.value)) parts.push(`export-${option.value}`);
  }
  if (filter) parts.push("filter");
  return parts.join(" ");
}
function GoogleIcon({ name }) {
  return /* @__PURE__ */ jsx("span", { className: "material-symbols-outlined", "aria-hidden": true, children: name });
}
function sortIconName(active, direction) {
  if (!active) return "unfold_more";
  return direction === "asc" ? "arrow_upward" : "arrow_downward";
}
function RowActions({
  actions,
  shape,
  name,
  editing,
  onEdit,
  onSave,
  onCancel
}) {
  const slug = rowSelectId(name);
  if (editing) {
    return /* @__PURE__ */ jsxs("div", { className: "ui-standards-qtable__actions", "data-testid": `ui-standard-table-pg-actions-${slug}`, children: [
      /* @__PURE__ */ jsx(
        "button",
        {
          type: "button",
          className: "ui-standards-qtable__action ui-standards-qtable__action--save",
          "data-shape": shape,
          "aria-label": `Save ${name}`,
          "data-testid": `ui-standard-table-pg-edit-save-${slug}`,
          onClick: onSave,
          children: /* @__PURE__ */ jsx(GoogleIcon, { name: "save" })
        }
      ),
      /* @__PURE__ */ jsx(
        "button",
        {
          type: "button",
          className: "ui-standards-qtable__action ui-standards-qtable__action--reset",
          "data-shape": shape,
          "aria-label": `Reset ${name} to original`,
          "data-testid": `ui-standard-table-pg-edit-reset-${slug}`,
          onClick: onCancel,
          children: /* @__PURE__ */ jsx(GoogleIcon, { name: "restart_alt" })
        }
      )
    ] });
  }
  const chosen = ACTION_OPTIONS.filter((option) => actions.includes(option.value));
  if (chosen.length === 0) return null;
  return /* @__PURE__ */ jsx("div", { className: "ui-standards-qtable__actions", "data-testid": `ui-standard-table-pg-actions-${slug}`, children: chosen.map((option) => /* @__PURE__ */ jsx(
    "button",
    {
      type: "button",
      className: "ui-standards-qtable__action" + (option.value === "delete" ? " ui-standards-qtable__action--danger" : ""),
      "data-shape": shape,
      "aria-label": `${option.labelFor} ${name}`,
      onClick: option.value === "edit" ? onEdit : void 0,
      children: /* @__PURE__ */ jsx(GoogleIcon, { name: option.icon })
    },
    option.value
  )) });
}
function SettingSlider({
  label,
  hint,
  value,
  onChange,
  testId
}) {
  return /* @__PURE__ */ jsxs("div", { className: "ui-standards-qcard-slider", children: [
    /* @__PURE__ */ jsxs("div", { className: "ui-standards-qcard-slider__label", children: [
      /* @__PURE__ */ jsx("span", { children: label }),
      /* @__PURE__ */ jsx("span", { "data-testid": `${testId}-value`, children: value })
    ] }),
    /* @__PURE__ */ jsx(
      UiStandardsQslider,
      {
        color: RADIO_STANDARD_COLOR,
        ink: "#ffffff",
        value,
        min: 1,
        max: 5,
        step: 1,
        snap: true,
        showLabel: true,
        labelAlways: true,
        onChange,
        testId
      }
    ),
    /* @__PURE__ */ jsx("div", { className: "ui-standards-qcard-slider__hint", children: hint })
  ] });
}
function SettingCard({ title, testId, children }) {
  return /* @__PURE__ */ jsxs("article", { className: "ui-standards-qcard ui-standards-qcard--settings", "data-testid": testId, children: [
    /* @__PURE__ */ jsx("div", { className: "ui-standards-qcard__section", children: /* @__PURE__ */ jsx("div", { className: "ui-standards-qcard__title", children: title }) }),
    /* @__PURE__ */ jsx("div", { className: "ui-standards-qcard__section ui-standards-qcard__section--tight ui-standards-qcard__settings", children })
  ] });
}
function UiStandardsTablePlayground() {
  const [separator, setSeparator] = useState("horizontal");
  const { preferences, setDensity } = usePreferences();
  const density = preferences.density;
  const [elevation, setElevation] = useState(DEFAULT_ELEVATION);
  const [loading, setLoading] = useState(false);
  const [alternate, setAlternate] = useState(false);
  const [multiSelect, setMultiSelect] = useState(false);
  const [selected, setSelected] = useState(() => /* @__PURE__ */ new Set());
  const [actions, setActions] = useState([]);
  const [iconShape, setIconShape] = useState("round");
  const [editKind, setEditKind] = useState("inline");
  const [editable, setEditable] = useState(() => COLUMNS.map((column) => column.id));
  const [rows, setRows] = useState(initialRows);
  const [editingId, setEditingId] = useState(null);
  const [editDraft, setEditDraft] = useState(null);
  const [editBaseline, setEditBaseline] = useState(null);
  const sampleRef = useRef(null);
  const [headerIntent, setHeaderIntent] = useState("standard");
  const [headerAppearance, setHeaderAppearance] = useState("standard");
  const [footerOn, setFooterOn] = useState(true);
  const [footerIntent, setFooterIntent] = useState("standard");
  const [footerAppearance, setFooterAppearance] = useState("standard");
  const [sort, setSort] = useState(false);
  const [sortColumn, setSortColumn] = useState(null);
  const [sortDir, setSortDir] = useState("asc");
  const [search, setSearch] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [filter, setFilter] = useState(false);
  const [filterValues, setFilterValues] = useState([]);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);
  const [page, setPage] = useState(1);
  const [exports, setExports] = useState([]);
  const [previewSize, setPreviewSize] = useState(UI_STANDARDS_DATA_PREVIEW_DEFAULT);
  const [settingsTab, setSettingsTab] = useState("table");
  const [intent, setIntent] = useState("standard");
  useEffect(() => {
    ensurePersonalSymbols();
  }, []);
  const showActions = actions.length > 0;
  const tableSample = INPUT_SAMPLES.find((item) => item.id === intent) ?? INPUT_SAMPLES[0];
  const headerSample = INPUT_SAMPLES.find((item) => item.id === headerIntent) ?? INPUT_SAMPLES[0];
  const footerSample = INPUT_SAMPLES.find((item) => item.id === footerIntent) ?? INPUT_SAMPLES[0];
  const checkBorder = headerIntent === "standard" ? "#9e9e9e" : playgroundIntentColor(headerSample.id, inputAccent(headerSample));
  const { layout } = useResponsiveDataLayout({
    tableMinWidthPx: 768,
    layoutWidthPx: UI_STANDARDS_DATA_PREVIEW_WIDTHS[previewSize]
  });
  const matchedRows = rows.filter((row) => {
    if (search && searchQuery.trim()) {
      const query = searchQuery.trim().toLowerCase();
      const hit = COLUMNS.some((column) => row[column.id].toLowerCase().includes(query));
      if (!hit) return false;
    }
    if (filter && filterValues.length > 0 && !filterValues.includes(row.dessert)) return false;
    return true;
  });
  const visibleRows = sort && sortColumn ? [...matchedRows].sort((a, b) => {
    const column = COLUMNS.find((item) => item.id === sortColumn);
    const left = a[sortColumn];
    const right = b[sortColumn];
    const compared = column?.numeric ? Number(left) - Number(right) : left.localeCompare(right);
    return sortDir === "asc" ? compared : -compared;
  }) : matchedRows;
  const useDialog = layout === "list" || editKind === "dialog";
  const firstEditable = COLUMNS.find((column) => editable.includes(column.id))?.id ?? null;
  const totalCount = visibleRows.length;
  const pageSizeChoices = useMemo(
    () => exitsTablePageSizesForTotal(totalCount, PAGE_SIZES),
    [totalCount]
  );
  const effectivePageSize = pageSizeChoices.includes(pageSize) ? pageSize : pageSizeChoices[0] ?? pageSize;
  const pageCount = Math.max(1, Math.ceil(totalCount / Math.max(effectivePageSize, 1)) || 1);
  const safePage = Math.min(Math.max(page, 1), pageCount);
  const pageStart = (safePage - 1) * effectivePageSize;
  const pageRows = visibleRows.slice(pageStart, pageStart + effectivePageSize);
  const rangeFrom = totalCount === 0 ? 0 : pageStart + 1;
  const rangeTo = Math.min(pageStart + effectivePageSize, totalCount);
  const rangeText = `${rangeFrom}-${rangeTo} of ${totalCount}`;
  const showPageNav = totalCount >= 10;
  const showPageSize = pageSizeChoices.length > 0;
  const selectedCount = pageRows.filter((row) => selected.has(row.id)).length;
  const allSelected = pageRows.length > 0 && selectedCount === pageRows.length;
  const someSelected = selectedCount > 0 && !allSelected;
  useEffect(() => {
    if (pageSizeChoices.length === 0 || pageSizeChoices.includes(pageSize)) {
      return;
    }
    setPageSize(pageSizeChoices[0]);
    setPage(1);
  }, [pageSize, pageSizeChoices]);
  function toggleAll() {
    setSelected(allSelected ? /* @__PURE__ */ new Set() : new Set(pageRows.map((row) => row.id)));
  }
  function toggleRow(dessert) {
    setSelected((current) => {
      const next = new Set(current);
      if (next.has(dessert)) next.delete(dessert);
      else next.add(dessert);
      return next;
    });
  }
  function chooseSort(columnId) {
    if (sortColumn !== columnId) {
      setSortColumn(columnId);
      setSortDir("asc");
      return;
    }
    setSortDir((dir) => dir === "asc" ? "desc" : "asc");
  }
  function snapshotRow(row) {
    return {
      dessert: row.dessert,
      calories: row.calories,
      fat: row.fat,
      carbs: row.carbs,
      protein: row.protein
    };
  }
  function beginEdit(row) {
    if (editable.length === 0) return;
    const snapshot = snapshotRow(row);
    setEditingId(row.id);
    setEditDraft(snapshot);
    setEditBaseline(snapshot);
  }
  function cancelEdit() {
    setEditingId(null);
    setEditDraft(null);
    setEditBaseline(null);
  }
  function resetEdit() {
    if (!editBaseline || !editDraft) return;
    const unchanged = COLUMNS.every((column) => editDraft[column.id] === editBaseline[column.id]);
    if (unchanged) {
      cancelEdit();
      return;
    }
    setEditDraft({ ...editBaseline });
  }
  function saveEdit() {
    if (!editingId || !editDraft) return;
    setRows((current) => current.map((row) => row.id === editingId ? { ...row, ...editDraft } : row));
    setEditingId(null);
    setEditDraft(null);
    setEditBaseline(null);
  }
  function updateDraft(columnId, value) {
    setEditDraft((current) => current ? { ...current, [columnId]: value } : current);
  }
  function editField(row, columnId) {
    const column = COLUMNS.find((item) => item.id === columnId);
    return /* @__PURE__ */ jsx(
      "input",
      {
        className: "ui-standards-qtable__editor",
        "aria-label": `${column?.label ?? columnId} for ${row.dessert}`,
        "data-testid": `ui-standard-table-pg-cell-${rowSelectId(row.id)}-${columnId}`,
        "data-edit-focus": columnId === firstEditable ? "true" : void 0,
        value: editDraft?.[columnId] ?? "",
        onChange: (event) => updateDraft(columnId, event.target.value)
      }
    );
  }
  useEffect(() => {
    if (!editingId || !firstEditable) return;
    sampleRef.current?.querySelector("[data-edit-focus='true']")?.focus();
  }, [editingId, firstEditable, useDialog]);
  function resetPreview() {
    setSeparator("horizontal");
    setDensity("balance");
    setElevation(DEFAULT_ELEVATION);
    setLoading(false);
    setAlternate(false);
    setMultiSelect(false);
    setSelected(/* @__PURE__ */ new Set());
    setActions([]);
    setIconShape("round");
    setEditKind("inline");
    setEditable(COLUMNS.map((column) => column.id));
    setRows(initialRows());
    setEditingId(null);
    setEditDraft(null);
    setEditBaseline(null);
    setIntent("standard");
    setHeaderIntent("standard");
    setHeaderAppearance("standard");
    setFooterOn(true);
    setFooterIntent("standard");
    setFooterAppearance("standard");
    setSort(false);
    setSortColumn(null);
    setSortDir("asc");
    setSearch(false);
    setSearchQuery("");
    setFilter(false);
    setFilterValues([]);
    setPageSize(DEFAULT_PAGE_SIZE);
    setPage(1);
    setExports([]);
    setPreviewSize(UI_STANDARDS_DATA_PREVIEW_DEFAULT);
    setSettingsTab("table");
  }
  const tableClass = [
    "ui-standards-qtable",
    `ui-standards-qtable--${separator}`,
    "ui-standards-qtable--bordered",
    loading ? "ui-standards-qtable--loading" : ""
  ].filter(Boolean).join(" ");
  return /* @__PURE__ */ jsxs(PlaygroundSection, { testId: "ui-standard-table-playground", className: "", children: [
    /* @__PURE__ */ jsxs(
      PlaygroundPreviewCard,
      {
        previewTestId: "ui-standard-table-pg-preview",
        themeTestId: "ui-standard-table-pg-theme",
        resetTestId: "ui-standard-table-pg-reset",
        onReset: resetPreview,
        previewClassName: "ui-standards-qtable-stage",
        children: [
          /* @__PURE__ */ jsx(
            ExitsTabs,
            {
              variant: "underline",
              layout: "equal",
              ariaLabel: "Table settings",
              value: settingsTab,
              onValueChange: setSettingsTab,
              testId: "ui-standard-table-pg-tabs",
              items: [
                { key: "table", label: "Table", testId: "ui-standard-table-pg-tab-table" },
                { key: "header", label: "Header", testId: "ui-standard-table-pg-tab-header" },
                { key: "footer", label: "Footer", testId: "ui-standard-table-pg-tab-footer" },
                { key: "features", label: "Features", testId: "ui-standard-table-pg-tab-features" }
              ],
              panels: {
                table: /* @__PURE__ */ jsxs(SettingCard, { title: "Table", testId: "ui-standard-table-pg-table-card", children: [
                  /* @__PURE__ */ jsx(
                    CompactSelect,
                    {
                      label: "Intent",
                      value: intent,
                      options: INPUT_SAMPLES.map((item) => ({ label: item.label, value: item.id, color: playgroundIntentColor(item.id, inputAccent(item)) })),
                      onChange: setIntent,
                      testId: "ui-standard-table-pg-intent"
                    }
                  ),
                  /* @__PURE__ */ jsx(
                    CompactSelect,
                    {
                      label: "Separator",
                      value: separator,
                      options: SEPARATOR_OPTIONS,
                      onChange: setSeparator,
                      testId: "ui-standard-table-pg-separator"
                    }
                  ),
                  /* @__PURE__ */ jsx(
                    CompactSelect,
                    {
                      label: "Density",
                      value: density,
                      options: DENSITY_OPTIONS,
                      onChange: setDensity,
                      testId: "ui-standard-table-pg-density"
                    }
                  ),
                  /* @__PURE__ */ jsx(
                    SettingSlider,
                    {
                      label: "Elevation",
                      hint: "1 flat \xB7 5 raised",
                      value: elevation,
                      onChange: setElevation,
                      testId: "ui-standard-table-pg-elevation"
                    }
                  ),
                  /* @__PURE__ */ jsxs("label", { className: "inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]", children: [
                    /* @__PURE__ */ jsx(
                      Switch,
                      {
                        checked: loading,
                        onCheckedChange: setLoading,
                        "aria-label": "Loading",
                        "data-testid": "ui-standard-table-pg-loading"
                      }
                    ),
                    "Loading"
                  ] }),
                  /* @__PURE__ */ jsxs("label", { className: "inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]", children: [
                    /* @__PURE__ */ jsx(
                      Switch,
                      {
                        checked: alternate,
                        onCheckedChange: setAlternate,
                        "aria-label": "Alternate",
                        "data-testid": "ui-standard-table-pg-alternate"
                      }
                    ),
                    "Alternate"
                  ] }),
                  /* @__PURE__ */ jsxs("label", { className: "inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]", children: [
                    /* @__PURE__ */ jsx(
                      Switch,
                      {
                        checked: multiSelect,
                        onCheckedChange: (next) => {
                          setMultiSelect(next);
                          if (!next) setSelected(/* @__PURE__ */ new Set());
                        },
                        "aria-label": "Multi select",
                        "data-testid": "ui-standard-table-pg-multi-select"
                      }
                    ),
                    "Multi select"
                  ] }),
                  /* @__PURE__ */ jsx(
                    CompactSelect,
                    {
                      label: "Action",
                      value: actions[0] ?? "view",
                      values: actions,
                      options: ACTION_OPTIONS.map((option) => ({ label: option.label, value: option.value })),
                      multiple: true,
                      onChangeValues: setActions,
                      testId: "ui-standard-table-pg-action"
                    }
                  ),
                  /* @__PURE__ */ jsx(
                    CompactSelect,
                    {
                      label: "Shape",
                      value: iconShape,
                      options: SHAPE_OPTIONS,
                      onChange: setIconShape,
                      testId: "ui-standard-table-pg-icon-shape"
                    }
                  ),
                  /* @__PURE__ */ jsx(
                    CompactSelect,
                    {
                      label: "Edit",
                      value: editKind,
                      options: EDIT_KIND_OPTIONS,
                      onChange: setEditKind,
                      testId: "ui-standard-table-pg-edit-kind"
                    }
                  ),
                  /* @__PURE__ */ jsx(
                    CompactSelect,
                    {
                      label: "Editable",
                      value: editable[0] ?? COLUMNS[0].id,
                      values: editable,
                      options: COLUMNS.map((column) => ({ label: column.label, value: column.id })),
                      multiple: true,
                      onChangeValues: setEditable,
                      testId: "ui-standard-table-pg-editable"
                    }
                  )
                ] }),
                header: /* @__PURE__ */ jsxs(SettingCard, { title: "Header", testId: "ui-standard-table-pg-header-card", children: [
                  /* @__PURE__ */ jsx(
                    CompactSelect,
                    {
                      label: "Intent",
                      value: headerIntent,
                      options: INPUT_SAMPLES.map((item) => ({ label: item.label, value: item.id, color: playgroundIntentColor(item.id, inputAccent(item)) })),
                      onChange: setHeaderIntent,
                      testId: "ui-standard-table-pg-header-intent"
                    }
                  ),
                  /* @__PURE__ */ jsx(
                    CompactSelect,
                    {
                      label: "Appearance",
                      value: headerAppearance,
                      options: HEADER_APPEARANCES,
                      onChange: setHeaderAppearance,
                      testId: "ui-standard-table-pg-header-appearance"
                    }
                  ),
                  /* @__PURE__ */ jsxs("label", { className: "inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]", children: [
                    /* @__PURE__ */ jsx(
                      Switch,
                      {
                        checked: sort,
                        onCheckedChange: (next) => {
                          setSort(next);
                          if (!next) {
                            setSortColumn(null);
                            setSortDir("asc");
                          }
                        },
                        "aria-label": "Sort",
                        "data-testid": "ui-standard-table-pg-sort"
                      }
                    ),
                    "Sort"
                  ] })
                ] }),
                footer: /* @__PURE__ */ jsxs(SettingCard, { title: "Footer", testId: "ui-standard-table-pg-footer-card", children: [
                  /* @__PURE__ */ jsxs("label", { className: "inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]", children: [
                    /* @__PURE__ */ jsx(
                      Switch,
                      {
                        checked: footerOn,
                        onCheckedChange: setFooterOn,
                        "aria-label": "Footer",
                        "data-testid": "ui-standard-table-pg-footer-toggle"
                      }
                    ),
                    "Footer"
                  ] }),
                  /* @__PURE__ */ jsx(
                    CompactSelect,
                    {
                      label: "Intent",
                      value: footerIntent,
                      options: INPUT_SAMPLES.map((item) => ({ label: item.label, value: item.id, color: playgroundIntentColor(item.id, inputAccent(item)) })),
                      onChange: setFooterIntent,
                      testId: "ui-standard-table-pg-footer-intent"
                    }
                  ),
                  /* @__PURE__ */ jsx(
                    CompactSelect,
                    {
                      label: "Appearance",
                      value: footerAppearance,
                      options: HEADER_APPEARANCES,
                      onChange: setFooterAppearance,
                      testId: "ui-standard-table-pg-footer-appearance"
                    }
                  )
                ] }),
                features: /* @__PURE__ */ jsxs(SettingCard, { title: "Features", testId: "ui-standard-table-pg-features-card", children: [
                  /* @__PURE__ */ jsxs("label", { className: "inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]", children: [
                    /* @__PURE__ */ jsx(
                      Switch,
                      {
                        checked: search,
                        onCheckedChange: (next) => {
                          setSearch(next);
                          if (!next) setSearchQuery("");
                        },
                        "aria-label": "Search",
                        "data-testid": "ui-standard-table-pg-search-toggle"
                      }
                    ),
                    "Search"
                  ] }),
                  /* @__PURE__ */ jsx(
                    CompactSelect,
                    {
                      label: "Export",
                      value: exports[0] ?? "pdf",
                      values: exports,
                      options: EXPORT_OPTIONS.map((option) => ({ label: option.label, value: option.value })),
                      multiple: true,
                      placeholderOnly: true,
                      onChangeValues: setExports,
                      testId: "ui-standard-table-pg-export"
                    }
                  ),
                  /* @__PURE__ */ jsxs("label", { className: "inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]", children: [
                    /* @__PURE__ */ jsx(
                      Switch,
                      {
                        checked: filter,
                        onCheckedChange: (next) => {
                          setFilter(next);
                          if (!next) setFilterValues([]);
                        },
                        "aria-label": "Filter",
                        "data-testid": "ui-standard-table-pg-filter-toggle"
                      }
                    ),
                    "Filter"
                  ] })
                ] })
              }
            }
          ),
          /* @__PURE__ */ jsx(
            SettingsSelect,
            {
              label: "Preview size",
              value: previewSize,
              onChange: setPreviewSize,
              variant: "segmented",
              testId: "ui-standard-table-pg-preview-size",
              options: [
                { value: "desktop", label: "Desktop" },
                { value: "tablet", label: "Tablet" },
                { value: "mobile", label: "Mobile" }
              ]
            }
          ),
          /* @__PURE__ */ jsx("p", { className: "ui-standards-qtable-mode", "data-testid": "ui-standard-table-pg-mode", children: uiStandardsDataPreviewLabel(previewSize, layout) }),
          /* @__PURE__ */ jsx("div", { className: "flex min-w-0 justify-center", children: /* @__PURE__ */ jsx(
            "div",
            {
              className: "min-w-0 w-full",
              style: { maxWidth: UI_STANDARDS_DATA_PREVIEW_WIDTHS[previewSize] },
              "data-preview-device": previewSize,
              "data-preview-width": UI_STANDARDS_DATA_PREVIEW_WIDTHS[previewSize],
              "data-testid": "ui-standard-table-pg-frame",
              children: /* @__PURE__ */ jsxs(
                "div",
                {
                  ref: sampleRef,
                  className: tableClass,
                  "data-testid": "ui-standard-table-pg-sample",
                  "data-separator": separator,
                  "data-density": density,
                  "data-elevation": elevation,
                  "data-bordered": "true",
                  "data-loading": loading ? "true" : "false",
                  "data-alternate": alternate ? "true" : "false",
                  "data-search": search ? "true" : "false",
                  "data-filter": filter ? "true" : "false",
                  "data-export": exports.length > 0 ? exports.join(",") : "none",
                  "data-multi-select": multiSelect ? "true" : "false",
                  "data-action": showActions ? actions.join(",") : "none",
                  "data-icon-shape": iconShape,
                  "data-edit-kind": editKind,
                  "data-edit-effective": useDialog ? "dialog" : "inline",
                  "data-editable": editable.length > 0 ? editable.join(",") : "none",
                  "data-intent": intent,
                  "data-sort": sort ? "true" : "false",
                  "data-layout": layout,
                  style: {
                    "--qtable-shadow": stepCss(ELEVATION_SHADOWS, elevation),
                    "--qtable-bg": playgroundIntentColor(tableSample.id, tableSample.bg),
                    "--qtable-ink": tableSample.fg,
                    "--qtable-accent": playgroundIntentColor(tableSample.id, inputAccent(tableSample)),
                    "--qheader-bg": playgroundIntentColor(headerSample.id, headerSample.bg),
                    "--qheader-ink": headerSample.fg,
                    "--qheader-accent": playgroundIntentColor(headerSample.id, inputAccent(headerSample))
                  },
                  children: [
                    /* @__PURE__ */ jsxs("div", { className: "ui-standards-qtable__middle", children: [
                      search || filter || exports.length > 0 ? /* @__PURE__ */ jsxs("div", { className: "ui-standards-qtable__toolbar", "data-testid": "ui-standard-table-pg-toolbar", children: [
                        search ? /* @__PURE__ */ jsx(
                          PlaygroundSearchField,
                          {
                            label: "Search",
                            value: searchQuery,
                            onChange: setSearchQuery,
                            className: "ui-standards-qtable__search",
                            inputTestId: "ui-standard-table-pg-search",
                            placeholderOnly: true
                          }
                        ) : null,
                        filter ? /* @__PURE__ */ jsx("div", { className: "ui-standards-qtable__filter", children: /* @__PURE__ */ jsx(
                          CompactSelect,
                          {
                            label: "All",
                            value: filterValues[0] ?? rows[0]?.dessert ?? "all",
                            values: filterValues,
                            options: rows.map((row) => ({ value: row.dessert, label: row.dessert })),
                            multiple: true,
                            placeholderOnly: true,
                            onChangeValues: setFilterValues,
                            testId: "ui-standard-table-pg-filter"
                          }
                        ) }) : null,
                        exports.length > 0 ? /* @__PURE__ */ jsx("div", { className: "ui-standards-qtable__exports", children: EXPORT_OPTIONS.filter((option) => exports.includes(option.value)).map((option) => /* @__PURE__ */ jsx(
                          "button",
                          {
                            type: "button",
                            className: "ui-standards-qbtn ui-standards-qbtn--playground ui-standards-qbtn--ghost",
                            "data-shape": "standard",
                            "data-export": option.value,
                            "data-intent": option.intent,
                            "data-testid": `ui-standard-table-pg-export-${option.value}`,
                            style: { color: exportIntentColor(option) },
                            children: option.format ? /* @__PURE__ */ jsx(ExitsFileFormatIcon, { format: option.format }) : /* @__PURE__ */ jsx(ExitsPrintFormatIcon, {})
                          },
                          option.value
                        )) }) : null
                      ] }) : null,
                      layout === "list" ? /* @__PURE__ */ jsxs(Fragment, { children: [
                        sort ? /* @__PURE__ */ jsx("div", { className: "ui-standards-qtable-list-sort", "data-testid": "ui-standard-table-pg-list-sort", children: COLUMNS.map((column) => {
                          const active = sortColumn === column.id;
                          return /* @__PURE__ */ jsxs(
                            "button",
                            {
                              type: "button",
                              "data-testid": `ui-standard-table-pg-sort-${column.id}`,
                              "aria-pressed": active,
                              onClick: () => chooseSort(column.id),
                              children: [
                                /* @__PURE__ */ jsx("span", { children: column.label }),
                                /* @__PURE__ */ jsx(GoogleIcon, { name: sortIconName(active, sortDir) })
                              ]
                            },
                            column.id
                          );
                        }) }) : null,
                        multiSelect ? /* @__PURE__ */ jsx("div", { className: "ui-standards-qtable-list__select-all", children: /* @__PURE__ */ jsx(
                          UiStandardsQcheckbox,
                          {
                            label: "Select all",
                            color: checkBorder,
                            dense: true,
                            checked: allSelected,
                            indeterminate: someSelected,
                            onSelect: toggleAll,
                            testId: "ui-standard-table-pg-select-all"
                          }
                        ) }) : null,
                        /* @__PURE__ */ jsx("ul", { className: "ui-standards-qtable-list", children: pageRows.map((row, index) => {
                          const inline = editingId === row.id && !useDialog && editDraft;
                          const tone = alternate && index % 2 === 1 ? "soft" : "solid";
                          return /* @__PURE__ */ jsxs("li", { "data-tone": alternate ? tone : void 0, children: [
                            /* @__PURE__ */ jsxs("div", { className: "ui-standards-qtable-list__title", children: [
                              multiSelect ? /* @__PURE__ */ jsx(
                                UiStandardsQcheckbox,
                                {
                                  label: `Select ${row.dessert}`,
                                  color: checkBorder,
                                  dense: true,
                                  checked: selected.has(row.id),
                                  onSelect: () => toggleRow(row.id),
                                  testId: `ui-standard-table-pg-select-${rowSelectId(row.id)}`
                                }
                              ) : null,
                              inline && editable.includes("dessert") ? editField(row, "dessert") : /* @__PURE__ */ jsx("span", { children: row.dessert })
                            ] }),
                            COLUMNS.filter((column) => column.id !== "dessert").map((column) => /* @__PURE__ */ jsxs("div", { className: "ui-standards-qtable-list__row", children: [
                              /* @__PURE__ */ jsx("span", { children: column.label }),
                              inline && editable.includes(column.id) ? editField(row, column.id) : /* @__PURE__ */ jsx("span", { children: row[column.id] })
                            ] }, column.id)),
                            showActions ? /* @__PURE__ */ jsxs("div", { className: "ui-standards-qtable-list__row", children: [
                              /* @__PURE__ */ jsx("span", { children: "Actions" }),
                              /* @__PURE__ */ jsx(
                                RowActions,
                                {
                                  actions,
                                  shape: iconShape,
                                  name: row.dessert,
                                  editing: Boolean(inline),
                                  onEdit: () => beginEdit(row),
                                  onSave: saveEdit,
                                  onCancel: resetEdit
                                }
                              )
                            ] }) : null
                          ] }, row.id);
                        }) })
                      ] }) : /* @__PURE__ */ jsxs("table", { children: [
                        /* @__PURE__ */ jsx(
                          "thead",
                          {
                            "data-testid": "ui-standard-table-pg-header",
                            "data-intent": headerIntent,
                            "data-appearance": headerAppearance,
                            "data-sort": sort ? "true" : "false",
                            style: {
                              "--qheader-bg": playgroundIntentColor(headerSample.id, headerSample.bg),
                              "--qheader-ink": headerSample.fg,
                              "--qheader-accent": playgroundIntentColor(headerSample.id, inputAccent(headerSample))
                            },
                            children: /* @__PURE__ */ jsxs("tr", { children: [
                              multiSelect ? /* @__PURE__ */ jsx("th", { className: "ui-standards-qtable__check", children: /* @__PURE__ */ jsx(
                                UiStandardsQcheckbox,
                                {
                                  label: "Select all",
                                  color: checkBorder,
                                  dense: true,
                                  checked: allSelected,
                                  indeterminate: someSelected,
                                  onSelect: toggleAll,
                                  testId: "ui-standard-table-pg-select-all"
                                }
                              ) }) : null,
                              COLUMNS.map((column) => {
                                const active = sort && sortColumn === column.id;
                                return /* @__PURE__ */ jsx("th", { "aria-sort": active ? sortDir === "asc" ? "ascending" : "descending" : "none", children: sort ? /* @__PURE__ */ jsxs(
                                  "button",
                                  {
                                    type: "button",
                                    "data-testid": `ui-standard-table-pg-sort-${column.id}`,
                                    onClick: () => chooseSort(column.id),
                                    children: [
                                      /* @__PURE__ */ jsx("span", { children: column.label }),
                                      /* @__PURE__ */ jsx(GoogleIcon, { name: sortIconName(active, sortDir) })
                                    ]
                                  }
                                ) : column.label }, column.id);
                              }),
                              showActions ? /* @__PURE__ */ jsx("th", { className: "ui-standards-qtable__actions-head", children: "Actions" }) : null
                            ] })
                          }
                        ),
                        /* @__PURE__ */ jsx("tbody", { children: pageRows.map((row, index) => {
                          const inline = editingId === row.id && !useDialog && editDraft;
                          const tone = alternate && index % 2 === 1 ? "soft" : "solid";
                          return /* @__PURE__ */ jsxs("tr", { "data-testid": `ui-standard-table-pg-row-${rowSelectId(row.id)}`, "data-editing": inline ? "true" : "false", "data-tone": alternate ? tone : void 0, children: [
                            multiSelect ? /* @__PURE__ */ jsx("td", { className: "ui-standards-qtable__check", children: /* @__PURE__ */ jsx(
                              UiStandardsQcheckbox,
                              {
                                label: `Select ${row.dessert}`,
                                color: checkBorder,
                                dense: true,
                                checked: selected.has(row.id),
                                onSelect: () => toggleRow(row.id),
                                testId: `ui-standard-table-pg-select-${rowSelectId(row.id)}`
                              }
                            ) }) : null,
                            COLUMNS.map((column) => /* @__PURE__ */ jsx("td", { children: inline && editable.includes(column.id) ? editField(row, column.id) : row[column.id] }, column.id)),
                            showActions ? /* @__PURE__ */ jsx("td", { className: "ui-standards-qtable__actions-cell", children: /* @__PURE__ */ jsx(
                              RowActions,
                              {
                                actions,
                                shape: iconShape,
                                name: row.dessert,
                                editing: Boolean(inline),
                                onEdit: () => beginEdit(row),
                                onSave: saveEdit,
                                onCancel: resetEdit
                              }
                            ) }) : null
                          ] }, row.id);
                        }) })
                      ] }),
                      editingId && editDraft && useDialog ? /* @__PURE__ */ jsx("div", { className: "ui-standards-qtable__dialog-backdrop", "data-testid": "ui-standard-table-pg-edit-dialog", children: /* @__PURE__ */ jsxs("div", { role: "dialog", "aria-modal": "true", "aria-label": `Edit ${rows.find((row) => row.id === editingId)?.dessert ?? ""}`, children: [
                        /* @__PURE__ */ jsx("div", { className: "ui-standards-qtable__dialog-title", children: "Edit row" }),
                        COLUMNS.filter((column) => editable.includes(column.id)).map((column) => /* @__PURE__ */ jsxs("label", { children: [
                          /* @__PURE__ */ jsx("span", { children: column.label }),
                          editField(
                            rows.find((row) => row.id === editingId) ?? { id: editingId, ...editDraft },
                            column.id
                          )
                        ] }, column.id)),
                        /* @__PURE__ */ jsxs("div", { className: "ui-standards-qtable__edit-actions", children: [
                          /* @__PURE__ */ jsx("button", { type: "button", "data-testid": "ui-standard-table-pg-edit-save", onClick: saveEdit, children: "Save" }),
                          /* @__PURE__ */ jsx("button", { type: "button", "data-testid": "ui-standard-table-pg-edit-cancel", onClick: cancelEdit, children: "Cancel" })
                        ] })
                      ] }) }) : null,
                      loading ? /* @__PURE__ */ jsx("div", { className: "ui-standards-qtable__loading", children: "Loading..." }) : null
                    ] }),
                    footerOn ? /* @__PURE__ */ jsx(
                      "div",
                      {
                        className: "ui-standards-qtable__bottom ui-standards-qtable__footer",
                        "data-testid": "ui-standard-table-pg-footer",
                        "data-intent": footerIntent,
                        "data-appearance": footerAppearance,
                        style: {
                          "--qfooter-bg": playgroundIntentColor(footerSample.id, footerSample.bg),
                          "--qfooter-ink": footerSample.fg,
                          "--qfooter-accent": playgroundIntentColor(footerSample.id, inputAccent(footerSample))
                        },
                        children: /* @__PURE__ */ jsxs("div", { className: "ui-standards-qtable__footer-cluster", children: [
                          showPageSize ? /* @__PURE__ */ jsxs("label", { className: "ui-standards-qtable__footer-size", "data-testid": "ui-standard-table-pg-page-size-field", children: [
                            /* @__PURE__ */ jsx("span", { className: "ui-standards-qtable__footer-size-label", children: "page size" }),
                            /* @__PURE__ */ jsx(
                              "select",
                              {
                                className: "ui-standards-qtable__footer-size-select",
                                value: effectivePageSize,
                                "aria-label": "page size",
                                "data-testid": "ui-standard-table-pg-page-size",
                                onChange: (event) => {
                                  setPageSize(Number(event.target.value));
                                  setPage(1);
                                },
                                children: pageSizeChoices.map((size) => /* @__PURE__ */ jsx("option", { value: size, children: size }, size))
                              }
                            )
                          ] }) : null,
                          showPageNav ? /* @__PURE__ */ jsxs("div", { className: "ui-standards-qtable__footer-nav", children: [
                            /* @__PURE__ */ jsx("button", { type: "button", "aria-label": "First page", disabled: safePage <= 1, onClick: () => setPage(1), children: /* @__PURE__ */ jsx(GoogleIcon, { name: "keyboard_double_arrow_left" }) }),
                            /* @__PURE__ */ jsx("button", { type: "button", "aria-label": "Previous page", disabled: safePage <= 1, onClick: () => setPage(safePage - 1), children: /* @__PURE__ */ jsx(GoogleIcon, { name: "chevron_left" }) }),
                            /* @__PURE__ */ jsx("span", { className: "ui-standards-qtable__footer-range", "data-testid": "ui-standard-table-pg-range", children: rangeText }),
                            /* @__PURE__ */ jsx("button", { type: "button", "aria-label": "Next page", disabled: safePage >= pageCount, onClick: () => setPage(safePage + 1), children: /* @__PURE__ */ jsx(GoogleIcon, { name: "chevron_right" }) }),
                            /* @__PURE__ */ jsx("button", { type: "button", "aria-label": "Last page", disabled: safePage >= pageCount, onClick: () => setPage(pageCount), children: /* @__PURE__ */ jsx(GoogleIcon, { name: "keyboard_double_arrow_right" }) })
                          ] }) : null
                        ] })
                      }
                    ) : null
                  ]
                }
              )
            }
          ) })
        ]
      }
    ),
    /* @__PURE__ */ jsx(UiStandardsCopyCommand, { standard: "Table", standardStatus: "pilot", command: tableCursorCommand(intent, separator, density, elevation, loading, alternate, multiSelect, actions, iconShape, editKind, editable, headerIntent, headerAppearance, footerOn, footerIntent, footerAppearance, sort, search, filter, exports) })
  ] });
}
export {
  UiStandardsTablePlayground
};
