import { useEffect, useMemo, useRef, useState, type CSSProperties, type ReactNode } from "react";
import { usePreferences } from "@/hooks/usePreferences";
import { ensurePersonalSymbols } from "@/components/icons/MaterialSymbol";
import { CompactSelect, PlaygroundPreviewCard, PlaygroundSearchField, playgroundIntentColor } from "@/features/ui-standards/UiStandardsPlaygrounds";
import {
  ExitsFileFormatIcon,
  ExitsPrintFormatIcon,
  ExitsTablePageSizeSelect,
  exitsTablePageSizesForTotal,
  exitsTableResolvePageSize,
  type ExitsFileFormat,
} from "@/components/exits/ExitsTable";
import {
  TABLE_EXPORT_GLOBAL_COLOR,
  TABLE_EXPORT_GLOBAL_CSV_INTENT,
  TABLE_EXPORT_GLOBAL_EXCEL_INTENT,
  TABLE_EXPORT_GLOBAL_PDF_INTENT,
  TABLE_EXPORT_GLOBAL_PRINT_INTENT,
} from "@/features/ui-standards/table-export-global-default";
import {
  TABLE_FOOTER_GLOBAL_CARET_SIZE,
  TABLE_FOOTER_GLOBAL_DEFAULT_APPEARANCE,
  TABLE_FOOTER_GLOBAL_DEFAULT_INTENT,
} from "@/features/ui-standards/table-footer-global-default";
import {
  TABLE_GLOBAL_DEFAULT_COMMAND,
  TABLE_GLOBAL_DEFAULT_FOOTER_APPEARANCE,
  TABLE_GLOBAL_DEFAULT_FOOTER_INTENT,
  TABLE_GLOBAL_DEFAULT_HEADER_APPEARANCE,
  TABLE_GLOBAL_DEFAULT_HEADER_INTENT,
  TABLE_GLOBAL_DEFAULT_SEARCH,
  TABLE_GLOBAL_DEFAULT_SHELL,
} from "@/features/ui-standards/table-global-default";
import { Lock } from "lucide-react";
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
  uiStandardsDataPreviewLabel,
  type UiStandardsDataPreviewDevice,
} from "@/features/ui-standards/UiStandardsResponsiveDataDeviceFrame";
import { INPUT_SAMPLES, inputAccent } from "@/features/ui-standards/UiStandardsInputGallery";
import { PlaygroundLabel, PlaygroundSection } from "@/features/ui-standards/UiStandardsSnippetBlock";
import { UiStandardsCopyCommand } from "@/features/ui-standards/UiStandardsCopyCommand";
import { UiStandardsSampleCard } from "@/features/ui-standards/UiStandardsSampleCard";

/**
 * Quasar-style table playground. Settings adjust one preview table.
 * Showcase only — not a product table.
 * @see https://quasar.dev/vue-components/table
 */
type HeaderAppearance =
  | "standard"
  | "soft"
  | "soft-outline"
  | "raised"
  | "ghost"
  | "glossy"
  | "glossy-raise"
  | "gradient"
  | "rounded"
  | "flat";

const HEADER_APPEARANCES: ReadonlyArray<{ label: string; value: HeaderAppearance }> = [
  { label: "Standard", value: "standard" },
  { label: "Soft", value: "soft" },
  { label: "Soft outline", value: "soft-outline" },
  { label: "Raised", value: "raised" },
  { label: "Ghost", value: "ghost" },
  { label: "Glossy", value: "glossy" },
  { label: "Glossy raise", value: "glossy-raise" },
  { label: "Gradient", value: "gradient" },
  { label: "Rounded", value: "rounded" },
  { label: "Flat", value: "flat" },
];

type TableSeparator = "horizontal" | "vertical" | "cell" | "none";
type TableDensity = "compact" | "balance" | "comfort";

const DENSITY_OPTIONS: ReadonlyArray<{ label: string; value: TableDensity }> = [
  { label: "Compact", value: "compact" },
  { label: "Standard", value: "balance" },
  { label: "Comfort", value: "comfort" },
];

const SEPARATOR_OPTIONS: ReadonlyArray<{ label: string; value: TableSeparator }> = [
  { label: "Horizontal", value: "horizontal" },
  { label: "Vertical", value: "vertical" },
  { label: "Cell", value: "cell" },
  { label: "None", value: "none" },
];

type TableActionId = "view" | "edit" | "delete" | "more";
type IconShape = "round" | "square" | "rounded";

const ACTION_OPTIONS: ReadonlyArray<{ label: string; value: TableActionId; icon: string; labelFor: string }> = [
  { label: "View", value: "view", icon: "visibility", labelFor: "View" },
  { label: "Edit", value: "edit", icon: "edit", labelFor: "Edit" },
  { label: "Delete", value: "delete", icon: "delete", labelFor: "Delete" },
  { label: "More", value: "more", icon: "more_horiz", labelFor: "More actions for" },
];

const SHAPE_OPTIONS: ReadonlyArray<{ label: string; value: IconShape }> = [
  { label: "Round", value: "round" },
  { label: "Square", value: "square" },
  { label: "Rounded", value: "rounded" },
];

const PAGE_SIZES = [10, 25, 50, 100, 200, 500] as const;
const DEFAULT_PAGE_SIZE = 10;
const DEFAULT_ELEVATION = 3;
/** 1 is 4px. Default 3 = 8px. 10 is the largest. */
const RADIUS_SIZES = ["4px", "6px", "8px", "10px", "12px", "14px", "16px", "18px", "20px", "24px"] as const;
const DEFAULT_RADIUS = 3;
const RADIUS_MAX = 10;
const HEADER_GLOBAL_STORAGE_KEY = "exits-table-header-global-default";
const FOOTER_GLOBAL_STORAGE_KEY = "exits-table-footer-global-default";

function isHeaderAppearance(value: string): value is HeaderAppearance {
  return HEADER_APPEARANCES.some((item) => item.value === value);
}

function readHeaderGlobalDefault(): { intent: string; appearance: HeaderAppearance } {
  try {
    const raw = localStorage.getItem(HEADER_GLOBAL_STORAGE_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as { intent?: string; appearance?: string };
      if (
        typeof parsed.intent === "string" &&
        typeof parsed.appearance === "string" &&
        isHeaderAppearance(parsed.appearance)
      ) {
        return { intent: parsed.intent, appearance: parsed.appearance };
      }
    }
  } catch {
    /* Use file default. */
  }
  return {
    intent: TABLE_GLOBAL_DEFAULT_HEADER_INTENT,
    appearance: TABLE_GLOBAL_DEFAULT_HEADER_APPEARANCE,
  };
}

function lockHeaderGlobalDefault(intent: string, appearance: HeaderAppearance) {
  localStorage.setItem(HEADER_GLOBAL_STORAGE_KEY, JSON.stringify({ intent, appearance }));
}

function readFooterGlobalDefault(): { intent: string; appearance: HeaderAppearance } {
  try {
    const raw = localStorage.getItem(FOOTER_GLOBAL_STORAGE_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as { intent?: string; appearance?: string };
      if (
        typeof parsed.intent === "string" &&
        typeof parsed.appearance === "string" &&
        isHeaderAppearance(parsed.appearance)
      ) {
        return { intent: parsed.intent, appearance: parsed.appearance };
      }
    }
  } catch {
    /* Use file default. */
  }
  return {
    intent: TABLE_FOOTER_GLOBAL_DEFAULT_INTENT,
    appearance: TABLE_FOOTER_GLOBAL_DEFAULT_APPEARANCE,
  };
}

function lockFooterGlobalDefault(intent: string, appearance: HeaderAppearance) {
  localStorage.setItem(FOOTER_GLOBAL_STORAGE_KEY, JSON.stringify({ intent, appearance }));
}

/** 1 is flat. 3 matches the current table shadow. 5 is the highest raise. */
const ELEVATION_SHADOWS = [
  "none",
  "0 1px 3px rgba(0, 0, 0, 0.2), 0 1px 1px rgba(0, 0, 0, 0.14), 0 2px 1px -1px rgba(0, 0, 0, 0.12)",
  "0 1px 5px rgba(0, 0, 0, 0.2), 0 2px 2px rgba(0, 0, 0, 0.14), 0 3px 1px -2px rgba(0, 0, 0, 0.12)",
  "0 2px 4px -1px rgba(0, 0, 0, 0.2), 0 4px 5px rgba(0, 0, 0, 0.14), 0 1px 10px rgba(0, 0, 0, 0.12)",
  "0 3px 5px -1px rgba(0, 0, 0, 0.2), 0 6px 10px rgba(0, 0, 0, 0.14), 0 1px 18px rgba(0, 0, 0, 0.12)",
] as const;

function stepCss(steps: readonly string[], value: number): string {
  return steps[Math.min(steps.length, Math.max(1, value)) - 1] ?? steps[0];
}

const COLUMNS = [
  { id: "dessert", label: "Dessert", numeric: false },
  { id: "calories", label: "Calories", numeric: true },
  { id: "fat", label: "Fat (g)", numeric: true },
  { id: "carbs", label: "Carbs (g)", numeric: true },
  { id: "protein", label: "Protein (g)", numeric: true },
] as const;

type ColumnId = (typeof COLUMNS)[number]["id"];
type EditKind = "inline" | "dialog";
type PlaygroundRow = { id: string } & Record<ColumnId, string>;

type ExportId = "pdf" | "print" | "csv" | "excel";

const EXPORT_OPTIONS: ReadonlyArray<{
  label: string;
  value: ExportId;
  format?: ExitsFileFormat;
  intent: "primary" | "secondary" | "success" | "info";
  color: string;
}> = [
  { label: "CSV", value: "csv", format: "csv", intent: TABLE_EXPORT_GLOBAL_CSV_INTENT, color: TABLE_EXPORT_GLOBAL_COLOR.csv },
  { label: "Excel", value: "excel", format: "xlsx", intent: TABLE_EXPORT_GLOBAL_EXCEL_INTENT, color: TABLE_EXPORT_GLOBAL_COLOR.xlsx },
  { label: "PDF", value: "pdf", format: "pdf", intent: TABLE_EXPORT_GLOBAL_PDF_INTENT, color: TABLE_EXPORT_GLOBAL_COLOR.pdf },
  { label: "Print", value: "print", intent: TABLE_EXPORT_GLOBAL_PRINT_INTENT, color: TABLE_EXPORT_GLOBAL_COLOR.print },
];

const EDIT_KIND_OPTIONS: ReadonlyArray<{ label: string; value: EditKind }> = [
  { label: "Inline", value: "inline" },
  { label: "Dialog", value: "dialog" },
];

const SAMPLE_ROW_COUNT = 110;

function initialRows(): PlaygroundRow[] {
  const templates = ROWS;
  return Array.from({ length: SAMPLE_ROW_COUNT }, (_, index) => {
    const template = templates[index % templates.length]!;
    const n = index + 1;
    const dessert = index < templates.length ? template.dessert : `${template.dessert} ${n}`;
    return {
      id: dessert,
      dessert,
      calories: template.calories,
      fat: template.fat,
      carbs: template.carbs,
      protein: template.protein,
    };
  });
}

type SortColumn = (typeof COLUMNS)[number]["id"];
type SortDir = "asc" | "desc";

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
  { dessert: "Churro", calories: "385", fat: "19.0", carbs: "48", protein: "4.1" },
] as const;

/** Fixed sample below the playground — locked default standard Cursor shorthand. */
const DEFAULT_STANDARD_SAMPLE_ROWS = ROWS.slice(0, 5);

function exportIntentColor(option: (typeof EXPORT_OPTIONS)[number]) {
  return option.color;
}

function rowSelectId(dessert: string) {
  return dessert.toLowerCase().replace(/\s+/g, "-");
}

function tableCursorCommand(
  intent: string,
  separator: TableSeparator,
  density: TableDensity,
  elevation: number,
  radius: number,
  loading: boolean,
  alternate: boolean,
  multiSelect: boolean,
  actions: readonly TableActionId[],
  iconShape: IconShape,
  editKind: EditKind,
  editable: readonly ColumnId[],
  headerIntent: string,
  headerAppearance: HeaderAppearance,
  footerOn: boolean,
  footerIntent: string,
  footerAppearance: HeaderAppearance,
  sort: boolean,
  search: boolean,
  filter: boolean,
  exports: readonly ExportId[],
  shell: typeof TABLE_GLOBAL_DEFAULT_SHELL | "none" = TABLE_GLOBAL_DEFAULT_SHELL,
): string {
  const parts = ["ext table"];
  if (intent !== "standard") parts.push(intent);
  if (shell === "soft-glass") parts.push("soft-glass");
  if (alternate) parts.push("alternate");
  if (separator !== "horizontal") parts.push(`separator-${separator}`);
  if (density !== "balance") parts.push(density);
  if (elevation === 1) parts.push("flat");
  else if (elevation !== DEFAULT_ELEVATION) parts.push(`raised-${elevation}`);
  if (radius !== DEFAULT_RADIUS) parts.push(`radius-${radius}`);
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

function GoogleIcon({ name }: { name: string }) {
  return (
    <span className="material-symbols-outlined" aria-hidden>
      {name}
    </span>
  );
}

function sortIconName(active: boolean, direction: "asc" | "desc") {
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
  onCancel,
}: {
  actions: readonly TableActionId[];
  shape: IconShape;
  name: string;
  editing: boolean;
  onEdit: () => void;
  onSave: () => void;
  onCancel: () => void;
}) {
  const slug = rowSelectId(name);
  if (editing) {
    return (
      <div className="ui-standards-qtable__actions" data-testid={`ui-standard-table-pg-actions-${slug}`}>
        <button
          type="button"
          className="ui-standards-qtable__action ui-standards-qtable__action--save"
          data-shape={shape}
          aria-label={`Save ${name}`}
          data-testid={`ui-standard-table-pg-edit-save-${slug}`}
          onClick={onSave}
        >
          <GoogleIcon name="save" />
        </button>
        <button
          type="button"
          className="ui-standards-qtable__action ui-standards-qtable__action--reset"
          data-shape={shape}
          aria-label={`Reset ${name} to original`}
          data-testid={`ui-standard-table-pg-edit-reset-${slug}`}
          onClick={onCancel}
        >
          <GoogleIcon name="restart_alt" />
        </button>
      </div>
    );
  }
  const chosen = ACTION_OPTIONS.filter((option) => actions.includes(option.value));
  if (chosen.length === 0) return null;
  return (
    <div className="ui-standards-qtable__actions" data-testid={`ui-standard-table-pg-actions-${slug}`}>
      {chosen.map((option) => (
          <button
            key={option.value}
            type="button"
            className={
              "ui-standards-qtable__action" + (option.value === "delete" ? " ui-standards-qtable__action--danger" : "")
            }
            data-shape={shape}
            aria-label={`${option.labelFor} ${name}`}
            onClick={option.value === "edit" ? onEdit : undefined}
          >
            <GoogleIcon name={option.icon} />
          </button>
        ))}
    </div>
  );
}

function SettingSlider({
  label,
  hint,
  value,
  onChange,
  testId,
  max = 5,
}: {
  label: string;
  hint: string;
  value: number;
  onChange: (value: number) => void;
  testId: string;
  max?: number;
}) {
  return (
    <div className="ui-standards-qcard-slider">
      <div className="ui-standards-qcard-slider__label">
        <span>{label}</span>
        <span data-testid={`${testId}-value`}>{value}</span>
      </div>
      <UiStandardsQslider
        color={RADIO_STANDARD_COLOR}
        ink="#ffffff"
        value={value}
        min={1}
        max={max}
        step={1}
        snap
        showLabel
        labelAlways
        onChange={onChange}
        testId={testId}
      />
      <div className="ui-standards-qcard-slider__hint">{hint}</div>
    </div>
  );
}

function SettingCard({ title, testId, children }: { title: string; testId: string; children: ReactNode }) {
  return (
    <article className="ui-standards-qcard ui-standards-qcard--settings" data-testid={testId}>
      <div className="ui-standards-qcard__section">
        <div className="ui-standards-qcard__title">{title}</div>
      </div>
      <div className="ui-standards-qcard__section ui-standards-qcard__section--tight ui-standards-qcard__settings">{children}</div>
    </article>
  );
}

function TableDefaultStandardSample() {
  const standard = INPUT_SAMPLES.find((item) => item.id === "standard") ?? INPUT_SAMPLES[0];
  const accent = playgroundIntentColor(standard.id, inputAccent(standard));
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);
  const [page, setPage] = useState(1);
  const totalCount = DEFAULT_STANDARD_SAMPLE_ROWS.length;
  const pageSizeChoices = useMemo(
    () => exitsTablePageSizesForTotal(totalCount, PAGE_SIZES),
    [totalCount],
  );
  const selectedPageSize = pageSizeChoices.includes(pageSize) ? pageSize : (pageSizeChoices[0] ?? pageSize);
  const effectivePageSize = exitsTableResolvePageSize(selectedPageSize, totalCount);
  const pageCount = Math.max(1, Math.ceil(totalCount / effectivePageSize) || 1);
  const safePage = Math.min(Math.max(1, page), pageCount);
  const pageStart = (safePage - 1) * effectivePageSize;
  const pageRows = DEFAULT_STANDARD_SAMPLE_ROWS.slice(pageStart, pageStart + effectivePageSize);
  const rangeFrom = totalCount === 0 ? 0 : pageStart + 1;
  const rangeTo = Math.min(pageStart + effectivePageSize, totalCount);
  const rangeText = totalCount === 0 ? "0-0 of 0" : `${rangeFrom}-${rangeTo} of ${totalCount}`;
  const chromeStyle = {
    "--qheader-bg": "color-mix(in srgb, var(--exits-primary) 8%, transparent)",
    "--qheader-ink": "var(--exits-text, #000)",
    "--qheader-accent": accent,
    "--qfooter-bg": "color-mix(in srgb, var(--exits-primary) 8%, transparent)",
    "--qfooter-ink": "var(--exits-text, #000)",
    "--qfooter-accent": accent,
    "--qfooter-caret-size": TABLE_FOOTER_GLOBAL_CARET_SIZE,
  } as CSSProperties;

  return (
    <section className="flex min-w-0 flex-col gap-2" data-testid="ui-standard-table-pg-default-standard">
      <PlaygroundLabel>Default standard</PlaygroundLabel>
      <UiStandardsSampleCard
        label="DEFAULT STANDARD"
        testId="ui-standard-table-pg-default-standard-sample"
        standard="Table"
        standardStatus="pilot"
        command={TABLE_GLOBAL_DEFAULT_COMMAND}
        commandContext="Locked default standard — Soft Glass shell, header glossy-raise, footer glossy-raise with page size and next, search."
        bordered={false}
        contentClassName="min-w-0"
      >
        <div
          className="ui-standards-qtable ui-standards-qtable--horizontal ui-standards-qtable--bordered"
          data-testid="ui-standard-table-pg-default-standard-table"
          data-intent="standard"
          data-shell={TABLE_GLOBAL_DEFAULT_SHELL}
          data-separator="horizontal"
          data-density="balance"
          data-elevation={DEFAULT_ELEVATION}
          data-radius={DEFAULT_RADIUS}
          data-bordered="true"
          data-search="true"
          data-header-appearance={TABLE_GLOBAL_DEFAULT_HEADER_APPEARANCE}
          data-footer-appearance={TABLE_GLOBAL_DEFAULT_FOOTER_APPEARANCE}
          style={chromeStyle}
        >
          <div className="ui-standards-qtable__middle">
            <div className="ui-standards-qtable__toolbar" data-testid="ui-standard-table-pg-default-standard-toolbar">
              <PlaygroundSearchField
                label="Search"
                className="ui-standards-qtable__search"
                inputTestId="ui-standard-table-pg-default-standard-search"
                placeholderOnly
              />
            </div>
            <table>
              <thead
                data-testid="ui-standard-table-pg-default-standard-header"
                data-intent={TABLE_GLOBAL_DEFAULT_HEADER_INTENT}
                data-appearance={TABLE_GLOBAL_DEFAULT_HEADER_APPEARANCE}
                style={
                  {
                    "--qheader-bg": "color-mix(in srgb, var(--exits-primary) 8%, transparent)",
                    "--qheader-ink": "var(--exits-text, #000)",
                    "--qheader-accent": accent,
                  } as CSSProperties
                }
              >
                <tr>
                  {COLUMNS.map((column) => (
                    <th key={column.id}>{column.label}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {pageRows.map((row) => (
                  <tr key={row.dessert} data-testid={`ui-standard-table-pg-default-standard-row-${rowSelectId(row.dessert)}`}>
                    {COLUMNS.map((column) => (
                      <td key={column.id}>{row[column.id]}</td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div
            className="ui-standards-qtable__bottom ui-standards-qtable__footer"
            data-testid="ui-standard-table-pg-default-standard-footer"
            data-intent={TABLE_GLOBAL_DEFAULT_FOOTER_INTENT}
            data-appearance={TABLE_GLOBAL_DEFAULT_FOOTER_APPEARANCE}
            style={
              {
                "--qfooter-bg": "color-mix(in srgb, var(--exits-primary) 8%, transparent)",
                "--qfooter-ink": "var(--exits-text, #000)",
                "--qfooter-accent": accent,
                "--qfooter-caret-size": TABLE_FOOTER_GLOBAL_CARET_SIZE,
              } as CSSProperties
            }
          >
            <div className="ui-standards-qtable__footer-cluster">
              <ExitsTablePageSizeSelect
                className="ui-standards-qtable__footer-size"
                label="Page size:"
                value={selectedPageSize}
                options={pageSizeChoices}
                testId="ui-standard-table-pg-default-standard-page-size"
                onChange={(next) => {
                  setPageSize(next);
                  setPage(1);
                }}
              />
              <div className="ui-standards-qtable__footer-nav">
                <button type="button" aria-label="First page" disabled={safePage <= 1} onClick={() => setPage(1)}>
                  <GoogleIcon name="keyboard_double_arrow_left" />
                </button>
                <button type="button" aria-label="Previous page" disabled={safePage <= 1} onClick={() => setPage(safePage - 1)}>
                  <GoogleIcon name="chevron_left" />
                </button>
                <span
                  className="ui-standards-qtable__footer-range"
                  data-testid="ui-standard-table-pg-default-standard-range"
                >
                  {rangeText}
                </span>
                <button
                  type="button"
                  aria-label="Next page"
                  disabled={safePage >= pageCount}
                  onClick={() => setPage(safePage + 1)}
                >
                  <GoogleIcon name="chevron_right" />
                </button>
                <button
                  type="button"
                  aria-label="Last page"
                  disabled={safePage >= pageCount}
                  onClick={() => setPage(pageCount)}
                >
                  <GoogleIcon name="keyboard_double_arrow_right" />
                </button>
              </div>
            </div>
          </div>
        </div>
      </UiStandardsSampleCard>
    </section>
  );
}

export function UiStandardsTablePlayground() {
  const [separator, setSeparator] = useState<TableSeparator>("horizontal");
  const { preferences, setDensity } = usePreferences();
  const density = preferences.density;
  const [elevation, setElevation] = useState(DEFAULT_ELEVATION);
  const [radius, setRadius] = useState(DEFAULT_RADIUS);
  const [loading, setLoading] = useState(false);
  const [alternate, setAlternate] = useState(false);
  const [multiSelect, setMultiSelect] = useState(false);
  const [selected, setSelected] = useState<ReadonlySet<string>>(() => new Set());
  const [actions, setActions] = useState<readonly TableActionId[]>([]);
  const [iconShape, setIconShape] = useState<IconShape>("round");
  const [editKind, setEditKind] = useState<EditKind>("inline");
  const [editable, setEditable] = useState<readonly ColumnId[]>(() => COLUMNS.map((column) => column.id));
  const [rows, setRows] = useState<PlaygroundRow[]>(initialRows);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editDraft, setEditDraft] = useState<Record<ColumnId, string> | null>(null);
  const [editBaseline, setEditBaseline] = useState<Record<ColumnId, string> | null>(null);
  const sampleRef = useRef<HTMLDivElement>(null);
  const [headerGlobalDefault, setHeaderGlobalDefault] = useState(readHeaderGlobalDefault);
  const [headerIntent, setHeaderIntent] = useState(headerGlobalDefault.intent);
  const [headerAppearance, setHeaderAppearance] = useState<HeaderAppearance>(headerGlobalDefault.appearance);
  const [footerOn, setFooterOn] = useState(true);
  const [footerGlobalDefault, setFooterGlobalDefault] = useState(readFooterGlobalDefault);
  const [footerIntent, setFooterIntent] = useState(footerGlobalDefault.intent);
  const [footerAppearance, setFooterAppearance] = useState<HeaderAppearance>(footerGlobalDefault.appearance);
  const [sort, setSort] = useState(false);
  const [sortColumn, setSortColumn] = useState<SortColumn | null>(null);
  const [sortDir, setSortDir] = useState<SortDir>("asc");
  const [search, setSearch] = useState(TABLE_GLOBAL_DEFAULT_SEARCH);
  const [searchQuery, setSearchQuery] = useState("");
  const [filter, setFilter] = useState(false);
  const [filterValues, setFilterValues] = useState<string[]>([]);
  const [pageSize, setPageSize] = useState<number>(DEFAULT_PAGE_SIZE);
  const [page, setPage] = useState(1);
  const [exports, setExports] = useState<readonly ExportId[]>([]);
  const [previewSize, setPreviewSize] = useState<UiStandardsDataPreviewDevice>(UI_STANDARDS_DATA_PREVIEW_DEFAULT);
  const [settingsTab, setSettingsTab] = useState("table");
  const [intent, setIntent] = useState("standard");
  useEffect(() => {
    ensurePersonalSymbols();
  }, []);
  const showActions = actions.length > 0;
  const tableSample = INPUT_SAMPLES.find((item) => item.id === intent) ?? INPUT_SAMPLES[0];
  const headerSample = INPUT_SAMPLES.find((item) => item.id === headerIntent) ?? INPUT_SAMPLES[0];
  const footerSample = INPUT_SAMPLES.find((item) => item.id === footerIntent) ?? INPUT_SAMPLES[0];
  const checkBorder =
    headerIntent === "standard"
      ? "#9e9e9e"
      : playgroundIntentColor(headerSample.id, inputAccent(headerSample));
  const { layout } = useResponsiveDataLayout({
    tableMinWidthPx: 768,
    layoutWidthPx: UI_STANDARDS_DATA_PREVIEW_WIDTHS[previewSize],
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
  const visibleRows =
    sort && sortColumn
      ? [...matchedRows].sort((a, b) => {
          const column = COLUMNS.find((item) => item.id === sortColumn);
          const left = a[sortColumn];
          const right = b[sortColumn];
          const compared = column?.numeric ? Number(left) - Number(right) : left.localeCompare(right);
          return sortDir === "asc" ? compared : -compared;
        })
      : matchedRows;
  const useDialog = layout === "list" || editKind === "dialog";
  const firstEditable = COLUMNS.find((column) => editable.includes(column.id))?.id ?? null;
  const totalCount = visibleRows.length;
  const pageSizeChoices = useMemo(
    () => exitsTablePageSizesForTotal(totalCount, PAGE_SIZES),
    [totalCount],
  );
  const selectedPageSize = pageSizeChoices.includes(pageSize) ? pageSize : (pageSizeChoices[0] ?? pageSize);
  const effectivePageSize = exitsTableResolvePageSize(selectedPageSize, totalCount);
  const pageCount = Math.max(1, Math.ceil(totalCount / effectivePageSize) || 1);
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
    setPageSize(pageSizeChoices[0]!);
    setPage(1);
  }, [pageSize, pageSizeChoices]);

  function toggleAll() {
    setSelected(allSelected ? new Set() : new Set(pageRows.map((row) => row.id)));
  }

  function toggleRow(dessert: string) {
    setSelected((current) => {
      const next = new Set(current);
      if (next.has(dessert)) next.delete(dessert);
      else next.add(dessert);
      return next;
    });
  }

  function chooseSort(columnId: SortColumn) {
    if (sortColumn !== columnId) {
      setSortColumn(columnId);
      setSortDir("asc");
      return;
    }
    setSortDir((dir) => (dir === "asc" ? "desc" : "asc"));
  }

  function snapshotRow(row: PlaygroundRow): Record<ColumnId, string> {
    return {
      dessert: row.dessert,
      calories: row.calories,
      fat: row.fat,
      carbs: row.carbs,
      protein: row.protein,
    };
  }

  function beginEdit(row: PlaygroundRow) {
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
    setRows((current) => current.map((row) => (row.id === editingId ? { ...row, ...editDraft } : row)));
    setEditingId(null);
    setEditDraft(null);
    setEditBaseline(null);
  }

  function updateDraft(columnId: ColumnId, value: string) {
    setEditDraft((current) => (current ? { ...current, [columnId]: value } : current));
  }

  function editField(row: PlaygroundRow, columnId: ColumnId) {
    const column = COLUMNS.find((item) => item.id === columnId);
    return (
      <input
        className="ui-standards-qtable__editor"
        aria-label={`${column?.label ?? columnId} for ${row.dessert}`}
        data-testid={`ui-standard-table-pg-cell-${rowSelectId(row.id)}-${columnId}`}
        data-edit-focus={columnId === firstEditable ? "true" : undefined}
        value={editDraft?.[columnId] ?? ""}
        onChange={(event) => updateDraft(columnId, event.target.value)}
      />
    );
  }

  useEffect(() => {
    if (!editingId || !firstEditable) return;
    sampleRef.current?.querySelector<HTMLInputElement>("[data-edit-focus='true']")?.focus();
  }, [editingId, firstEditable, useDialog]);

  function resetPreview() {
    setSeparator("horizontal");
    setDensity("balance");
    setElevation(DEFAULT_ELEVATION);
    setRadius(DEFAULT_RADIUS);
    setLoading(false);
    setAlternate(false);
    setMultiSelect(false);
    setSelected(new Set());
    setActions([]);
    setIconShape("round");
    setEditKind("inline");
    setEditable(COLUMNS.map((column) => column.id));
    setRows(initialRows());
    setEditingId(null);
    setEditDraft(null);
    setEditBaseline(null);
    setIntent("standard");
    setHeaderIntent(headerGlobalDefault.intent);
    setHeaderAppearance(headerGlobalDefault.appearance);
    setFooterOn(true);
    setFooterIntent(footerGlobalDefault.intent);
    setFooterAppearance(footerGlobalDefault.appearance);
    setSort(false);
    setSortColumn(null);
    setSortDir("asc");
    setSearch(TABLE_GLOBAL_DEFAULT_SEARCH);
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
    loading ? "ui-standards-qtable--loading" : "",
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <PlaygroundSection testId="ui-standard-table-playground" className="">
      <PlaygroundPreviewCard
        previewTestId="ui-standard-table-pg-preview"
        themeTestId="ui-standard-table-pg-theme"
        resetTestId="ui-standard-table-pg-reset"
        onReset={resetPreview}
        previewClassName="ui-standards-qtable-stage"
      >
        <ExitsTabs
          variant="underline"
          layout="equal"
          ariaLabel="Table settings"
          value={settingsTab}
          onValueChange={setSettingsTab}
          testId="ui-standard-table-pg-tabs"
          items={[
            { key: "table", label: "Table", testId: "ui-standard-table-pg-tab-table" },
            { key: "header", label: "Header", testId: "ui-standard-table-pg-tab-header" },
            { key: "footer", label: "Footer", testId: "ui-standard-table-pg-tab-footer" },
            { key: "features", label: "Features", testId: "ui-standard-table-pg-tab-features" },
          ]}
          panels={{
            table: (
          <SettingCard title="Table" testId="ui-standard-table-pg-table-card">
            <CompactSelect
              label="Intent"
              value={intent}
              options={INPUT_SAMPLES.map((item) => ({ label: item.label, value: item.id, color: playgroundIntentColor(item.id, inputAccent(item)) }))}
              onChange={setIntent}
              testId="ui-standard-table-pg-intent"
            />
            <CompactSelect
              label="Separator"
              value={separator}
              options={SEPARATOR_OPTIONS}
              onChange={setSeparator}
              testId="ui-standard-table-pg-separator"
            />
            <CompactSelect
              label="Density"
              value={density}
              options={DENSITY_OPTIONS}
              onChange={setDensity}
              testId="ui-standard-table-pg-density"
            />
            <SettingSlider
              label="Elevation"
              hint="1 flat · 5 raised"
              value={elevation}
              onChange={setElevation}
              testId="ui-standard-table-pg-elevation"
            />
            <SettingSlider
              label="Radius"
              hint={`1 ${RADIUS_SIZES[0]} · ${RADIUS_MAX} ${RADIUS_SIZES[RADIUS_MAX - 1]}`}
              value={radius}
              onChange={setRadius}
              testId="ui-standard-table-pg-radius"
              max={RADIUS_MAX}
            />
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={loading}
                onCheckedChange={setLoading}
                aria-label="Loading"
                data-testid="ui-standard-table-pg-loading"
              />
              Loading
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={alternate}
                onCheckedChange={setAlternate}
                aria-label="Alternate"
                data-testid="ui-standard-table-pg-alternate"
              />
              Alternate
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={multiSelect}
                onCheckedChange={(next) => {
                  setMultiSelect(next);
                  if (!next) setSelected(new Set());
                }}
                aria-label="Multi select"
                data-testid="ui-standard-table-pg-multi-select"
              />
              Multi select
            </label>
            <CompactSelect
              label="Action"
              value={actions[0] ?? "view"}
              values={actions}
              options={ACTION_OPTIONS.map((option) => ({ label: option.label, value: option.value }))}
              multiple
              onChangeValues={setActions}
              testId="ui-standard-table-pg-action"
            />
            <CompactSelect
              label="Shape"
              value={iconShape}
              options={SHAPE_OPTIONS}
              onChange={setIconShape}
              testId="ui-standard-table-pg-icon-shape"
            />
            <CompactSelect
              label="Edit"
              value={editKind}
              options={EDIT_KIND_OPTIONS}
              onChange={setEditKind}
              testId="ui-standard-table-pg-edit-kind"
            />
            <CompactSelect
              label="Editable"
              value={editable[0] ?? COLUMNS[0].id}
              values={editable}
              options={COLUMNS.map((column) => ({ label: column.label, value: column.id }))}
              multiple
              onChangeValues={setEditable}
              testId="ui-standard-table-pg-editable"
            />
          </SettingCard>
            ),
            header: (
          <SettingCard title="Header" testId="ui-standard-table-pg-header-card">
            <CompactSelect
              label="Intent"
              value={headerIntent}
              options={INPUT_SAMPLES.map((item) => ({ label: item.label, value: item.id, color: playgroundIntentColor(item.id, inputAccent(item)) }))}
              onChange={setHeaderIntent}
              testId="ui-standard-table-pg-header-intent"
            />
            <CompactSelect
              label="Appearance"
              value={headerAppearance}
              options={HEADER_APPEARANCES}
              onChange={setHeaderAppearance}
              testId="ui-standard-table-pg-header-appearance"
            />
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={sort}
                onCheckedChange={(next) => {
                  setSort(next);
                  if (!next) {
                    setSortColumn(null);
                    setSortDir("asc");
                  }
                }}
                aria-label="Sort"
                data-testid="ui-standard-table-pg-sort"
              />
              Sort
            </label>
            <button
              type="button"
              className="inline-flex items-center gap-1.5 text-[length:var(--exits-text-sm)]"
              aria-pressed={
                headerGlobalDefault.intent === headerIntent &&
                headerGlobalDefault.appearance === headerAppearance
              }
              data-testid="ui-standard-table-pg-header-default-global"
              data-default-intent={headerGlobalDefault.intent}
              data-default-appearance={headerGlobalDefault.appearance}
              onClick={() => {
                setHeaderGlobalDefault({ intent: headerIntent, appearance: headerAppearance });
                lockHeaderGlobalDefault(headerIntent, headerAppearance);
              }}
            >
              <Lock className="size-4" aria-hidden />
              default global
            </button>
          </SettingCard>
            ),
            footer: (
          <SettingCard title="Footer" testId="ui-standard-table-pg-footer-card">
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={footerOn}
                onCheckedChange={setFooterOn}
                aria-label="Footer"
                data-testid="ui-standard-table-pg-footer-toggle"
              />
              Footer
            </label>
            <CompactSelect
              label="Intent"
              value={footerIntent}
              options={INPUT_SAMPLES.map((item) => ({ label: item.label, value: item.id, color: playgroundIntentColor(item.id, inputAccent(item)) }))}
              onChange={setFooterIntent}
              testId="ui-standard-table-pg-footer-intent"
            />
            <CompactSelect
              label="Appearance"
              value={footerAppearance}
              options={HEADER_APPEARANCES}
              onChange={setFooterAppearance}
              testId="ui-standard-table-pg-footer-appearance"
            />
            <button
              type="button"
              className="inline-flex items-center gap-1.5 text-[length:var(--exits-text-sm)]"
              aria-pressed={
                footerGlobalDefault.intent === footerIntent &&
                footerGlobalDefault.appearance === footerAppearance
              }
              data-testid="ui-standard-table-pg-footer-default-global"
              data-default-intent={footerGlobalDefault.intent}
              data-default-appearance={footerGlobalDefault.appearance}
              onClick={() => {
                setFooterGlobalDefault({ intent: footerIntent, appearance: footerAppearance });
                lockFooterGlobalDefault(footerIntent, footerAppearance);
              }}
            >
              <Lock className="size-4" aria-hidden />
              default global
            </button>
          </SettingCard>
            ),
            features: (
          <SettingCard title="Features" testId="ui-standard-table-pg-features-card">
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={search}
                onCheckedChange={(next) => {
                  setSearch(next);
                  if (!next) setSearchQuery("");
                }}
                aria-label="Search"
                data-testid="ui-standard-table-pg-search-toggle"
              />
              Search
            </label>
            <CompactSelect
              label="Export"
              value={exports[0] ?? "pdf"}
              values={exports}
              options={EXPORT_OPTIONS.map((option) => ({ label: option.label, value: option.value }))}
              multiple
              placeholderOnly
              onChangeValues={setExports}
              testId="ui-standard-table-pg-export"
            />
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={filter}
                onCheckedChange={(next) => {
                  setFilter(next);
                  if (!next) setFilterValues([]);
                }}
                aria-label="Filter"
                data-testid="ui-standard-table-pg-filter-toggle"
              />
              Filter
            </label>
          </SettingCard>
            ),
          }}
        />
        <SettingsSelect<UiStandardsDataPreviewDevice>
          label="Preview size"
          value={previewSize}
          onChange={setPreviewSize}
          variant="segmented"
          testId="ui-standard-table-pg-preview-size"
          options={[
            { value: "desktop", label: "Desktop" },
            { value: "tablet", label: "Tablet" },
            { value: "mobile", label: "Mobile" },
          ]}
        />
        <p className="ui-standards-qtable-mode" data-testid="ui-standard-table-pg-mode">
          {uiStandardsDataPreviewLabel(previewSize, layout)}
        </p>
        <div className="flex min-w-0 justify-center">
          <div
            className="min-w-0 w-full"
            style={{ maxWidth: UI_STANDARDS_DATA_PREVIEW_WIDTHS[previewSize] }}
            data-preview-device={previewSize}
            data-preview-width={UI_STANDARDS_DATA_PREVIEW_WIDTHS[previewSize]}
            data-testid="ui-standard-table-pg-frame"
          >
        <div
          ref={sampleRef}
          className={tableClass}
          data-testid="ui-standard-table-pg-sample"
          data-shell={TABLE_GLOBAL_DEFAULT_SHELL}
          data-separator={separator}
          data-density={density}
          data-elevation={elevation}
          data-radius={radius}
          data-bordered="true"
          data-loading={loading ? "true" : "false"}
          data-alternate={alternate ? "true" : "false"}
          data-search={search ? "true" : "false"}
          data-filter={filter ? "true" : "false"}
          data-export={exports.length > 0 ? exports.join(",") : "none"}
          data-multi-select={multiSelect ? "true" : "false"}
          data-action={showActions ? actions.join(",") : "none"}
          data-icon-shape={iconShape}
          data-edit-kind={editKind}
          data-edit-effective={useDialog ? "dialog" : "inline"}
          data-editable={editable.length > 0 ? editable.join(",") : "none"}
          data-intent={intent}
          data-sort={sort ? "true" : "false"}
          data-layout={layout}
          style={
            {
              "--qtable-shadow": stepCss(ELEVATION_SHADOWS, elevation),
              "--qtable-radius": stepCss(RADIUS_SIZES, radius),
              "--qtable-bg": playgroundIntentColor(tableSample.id, tableSample.bg),
              "--qtable-ink": tableSample.fg,
              "--qtable-accent": playgroundIntentColor(tableSample.id, inputAccent(tableSample)),
              "--qheader-bg": playgroundIntentColor(headerSample.id, headerSample.bg),
              "--qheader-ink": headerSample.fg,
              "--qheader-accent": playgroundIntentColor(headerSample.id, inputAccent(headerSample)),
            } as CSSProperties
          }
        >
          <div className="ui-standards-qtable__middle">
            {search || filter || exports.length > 0 ? (
              <div className="ui-standards-qtable__toolbar" data-testid="ui-standard-table-pg-toolbar">
                {search ? (
                  <PlaygroundSearchField
                    label="Search"
                    value={searchQuery}
                    onChange={setSearchQuery}
                    className="ui-standards-qtable__search"
                    inputTestId="ui-standard-table-pg-search"
                    placeholderOnly
                  />
                ) : null}
                {filter ? (
                  <div className="ui-standards-qtable__filter">
                    <CompactSelect
                      label="All"
                      value={filterValues[0] ?? rows[0]?.dessert ?? "all"}
                      values={filterValues}
                      options={rows.map((row) => ({ value: row.dessert, label: row.dessert }))}
                      multiple
                      placeholderOnly
                      onChangeValues={setFilterValues}
                      testId="ui-standard-table-pg-filter"
                    />
                  </div>
                ) : null}
                {exports.length > 0 ? (
                  <div className="ui-standards-qtable__exports">
                    {EXPORT_OPTIONS.filter((option) => exports.includes(option.value)).map((option) => (
                      <button
                        key={option.value}
                        type="button"
                        className="ui-standards-qbtn ui-standards-qbtn--playground ui-standards-qbtn--ghost"
                        data-shape="standard"
                        data-export={option.value}
                        data-intent={option.intent}
                        data-testid={`ui-standard-table-pg-export-${option.value}`}
                        style={{ color: exportIntentColor(option) }}
                      >
                        {option.format ? (
                          <ExitsFileFormatIcon format={option.format} />
                        ) : (
                          <ExitsPrintFormatIcon />
                        )}
                      </button>
                    ))}
                  </div>
                ) : null}
              </div>
            ) : null}
            {layout === "list" ? (
              <>
                {sort ? (
                  <div className="ui-standards-qtable-list-sort" data-testid="ui-standard-table-pg-list-sort">
                    {COLUMNS.map((column) => {
                      const active = sortColumn === column.id;
                      return (
                        <button
                          key={column.id}
                          type="button"
                          data-testid={`ui-standard-table-pg-sort-${column.id}`}
                          aria-pressed={active}
                          onClick={() => chooseSort(column.id)}
                        >
                          <span>{column.label}</span>
                          <GoogleIcon name={sortIconName(active, sortDir)} />
                        </button>
                      );
                    })}
                  </div>
                ) : null}
                {multiSelect ? (
                  <div className="ui-standards-qtable-list__select-all">
                    <UiStandardsQcheckbox
                      label="Select all"
                      color={checkBorder}
                      dense
                      checked={allSelected}
                      indeterminate={someSelected}
                      onSelect={toggleAll}
                      testId="ui-standard-table-pg-select-all"
                    />
                  </div>
                ) : null}
              <ul className="ui-standards-qtable-list">
                {pageRows.map((row, index) => {
                  const inline = editingId === row.id && !useDialog && editDraft;
                  const tone = alternate && index % 2 === 1 ? "soft" : "solid";
                  return (
                  <li key={row.id} data-tone={alternate ? tone : undefined}>
                    <div className="ui-standards-qtable-list__title">
                      {multiSelect ? (
                        <UiStandardsQcheckbox
                          label={`Select ${row.dessert}`}
                          color={checkBorder}
                          dense
                          checked={selected.has(row.id)}
                          onSelect={() => toggleRow(row.id)}
                          testId={`ui-standard-table-pg-select-${rowSelectId(row.id)}`}
                        />
                      ) : null}
                      {inline && editable.includes("dessert") ? editField(row, "dessert") : <span>{row.dessert}</span>}
                    </div>
                    {COLUMNS.filter((column) => column.id !== "dessert").map((column) => (
                      <div key={column.id} className="ui-standards-qtable-list__row">
                        <span>{column.label}</span>
                        {inline && editable.includes(column.id) ? editField(row, column.id) : <span>{row[column.id]}</span>}
                      </div>
                    ))}
                    {showActions ? (
                      <div className="ui-standards-qtable-list__row">
                        <span>Actions</span>
                        <RowActions
                          actions={actions}
                          shape={iconShape}
                          name={row.dessert}
                          editing={Boolean(inline)}
                          onEdit={() => beginEdit(row)}
                          onSave={saveEdit}
                          onCancel={resetEdit}
                        />
                      </div>
                    ) : null}
                  </li>
                  );
                })}
              </ul>
              </>
            ) : (
            <table>
              <thead
                data-testid="ui-standard-table-pg-header"
                data-intent={headerIntent}
                data-appearance={headerAppearance}
                data-sort={sort ? "true" : "false"}
                style={
                  {
                    "--qheader-bg": playgroundIntentColor(headerSample.id, headerSample.bg),
                    "--qheader-ink": headerSample.fg,
                    "--qheader-accent": playgroundIntentColor(headerSample.id, inputAccent(headerSample)),
                  } as CSSProperties
                }
              >
                <tr>
                  {multiSelect ? (
                    <th className="ui-standards-qtable__check">
                      <UiStandardsQcheckbox
                        label="Select all"
                        color={checkBorder}
                        dense
                        checked={allSelected}
                        indeterminate={someSelected}
                        onSelect={toggleAll}
                        testId="ui-standard-table-pg-select-all"
                      />
                    </th>
                  ) : null}
                  {COLUMNS.map((column) => {
                    const active = sort && sortColumn === column.id;
                    return (
                      <th key={column.id} aria-sort={active ? (sortDir === "asc" ? "ascending" : "descending") : "none"}>
                        {sort ? (
                          <button
                            type="button"
                            data-testid={`ui-standard-table-pg-sort-${column.id}`}
                            onClick={() => chooseSort(column.id)}
                          >
                            <span>{column.label}</span>
                            <GoogleIcon name={sortIconName(active, sortDir)} />
                          </button>
                        ) : (
                          column.label
                        )}
                      </th>
                    );
                  })}
                  {showActions ? <th className="ui-standards-qtable__actions-head">Actions</th> : null}
                </tr>
              </thead>
              <tbody>
                {pageRows.map((row, index) => {
                  const inline = editingId === row.id && !useDialog && editDraft;
                  const tone = alternate && index % 2 === 1 ? "soft" : "solid";
                  return (
                  <tr key={row.id} data-testid={`ui-standard-table-pg-row-${rowSelectId(row.id)}`} data-editing={inline ? "true" : "false"} data-tone={alternate ? tone : undefined}>
                    {multiSelect ? (
                      <td className="ui-standards-qtable__check">
                        <UiStandardsQcheckbox
                          label={`Select ${row.dessert}`}
                          color={checkBorder}
                          dense
                          checked={selected.has(row.id)}
                          onSelect={() => toggleRow(row.id)}
                          testId={`ui-standard-table-pg-select-${rowSelectId(row.id)}`}
                        />
                      </td>
                    ) : null}
                    {COLUMNS.map((column) => (
                      <td key={column.id}>
                        {inline && editable.includes(column.id) ? editField(row, column.id) : row[column.id]}
                      </td>
                    ))}
                    {showActions ? (
                      <td className="ui-standards-qtable__actions-cell">
                        <RowActions
                          actions={actions}
                          shape={iconShape}
                          name={row.dessert}
                          editing={Boolean(inline)}
                          onEdit={() => beginEdit(row)}
                          onSave={saveEdit}
                          onCancel={resetEdit}
                        />
                      </td>
                    ) : null}
                  </tr>
                  );
                })}
              </tbody>
            </table>
            )}
            {editingId && editDraft && useDialog ? (
              <div className="ui-standards-qtable__dialog-backdrop" data-testid="ui-standard-table-pg-edit-dialog">
                <div role="dialog" aria-modal="true" aria-label={`Edit ${rows.find((row) => row.id === editingId)?.dessert ?? ""}`}>
                  <div className="ui-standards-qtable__dialog-title">Edit row</div>
                  {COLUMNS.filter((column) => editable.includes(column.id)).map((column) => (
                    <label key={column.id}>
                      <span>{column.label}</span>
                      {editField(
                        rows.find((row) => row.id === editingId) ?? { id: editingId, ...editDraft },
                        column.id,
                      )}
                    </label>
                  ))}
                  <div className="ui-standards-qtable__edit-actions">
                    <button type="button" data-testid="ui-standard-table-pg-edit-save" onClick={saveEdit}>
                      Save
                    </button>
                    <button type="button" data-testid="ui-standard-table-pg-edit-cancel" onClick={cancelEdit}>
                      Cancel
                    </button>
                  </div>
                </div>
              </div>
            ) : null}
            {loading ? <div className="ui-standards-qtable__loading">Loading...</div> : null}
          </div>
          {footerOn ? (
          <div
            className="ui-standards-qtable__bottom ui-standards-qtable__footer"
            data-testid="ui-standard-table-pg-footer"
            data-intent={footerIntent}
            data-appearance={footerAppearance}
            style={
              {
                "--qfooter-bg": playgroundIntentColor(footerSample.id, footerSample.bg),
                "--qfooter-ink": footerSample.fg,
                "--qfooter-accent": playgroundIntentColor(footerSample.id, inputAccent(footerSample)),
                "--qfooter-caret-size": TABLE_FOOTER_GLOBAL_CARET_SIZE,
              } as CSSProperties
            }
          >
            <div className="ui-standards-qtable__footer-cluster">
            {showPageSize ? (
              <ExitsTablePageSizeSelect
                className="ui-standards-qtable__footer-size"
                label="Page size:"
                value={selectedPageSize}
                options={pageSizeChoices}
                testId="ui-standard-table-pg-page-size"
                onChange={(next) => {
                  setPageSize(next);
                  setPage(1);
                }}
              />
            ) : null}
            {showPageNav ? (
              <div className="ui-standards-qtable__footer-nav">
                <button type="button" aria-label="First page" disabled={safePage <= 1} onClick={() => setPage(1)}>
                  <GoogleIcon name="keyboard_double_arrow_left" />
                </button>
                <button type="button" aria-label="Previous page" disabled={safePage <= 1} onClick={() => setPage(safePage - 1)}>
                  <GoogleIcon name="chevron_left" />
                </button>
                <span className="ui-standards-qtable__footer-range" data-testid="ui-standard-table-pg-range">{rangeText}</span>
                <button type="button" aria-label="Next page" disabled={safePage >= pageCount} onClick={() => setPage(safePage + 1)}>
                  <GoogleIcon name="chevron_right" />
                </button>
                <button type="button" aria-label="Last page" disabled={safePage >= pageCount} onClick={() => setPage(pageCount)}>
                  <GoogleIcon name="keyboard_double_arrow_right" />
                </button>
              </div>
            ) : null}
            </div>
          </div>
          ) : null}
        </div>
          </div>
        </div>
      </PlaygroundPreviewCard>
      <UiStandardsCopyCommand
        testId="ui-standard-table-pg-copy-command"
        standard="Table"
        standardStatus="pilot"
        command={tableCursorCommand(intent, separator, density, elevation, radius, loading, alternate, multiSelect, actions, iconShape, editKind, editable, headerIntent, headerAppearance, footerOn, footerIntent, footerAppearance, sort, search, filter, exports)}
      />
      <TableDefaultStandardSample />
    </PlaygroundSection>
  );
}
