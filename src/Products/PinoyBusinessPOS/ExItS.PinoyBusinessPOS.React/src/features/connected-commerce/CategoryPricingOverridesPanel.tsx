import { useMemo, useState } from "react";
import { Pencil, Plus, Trash2 } from "lucide-react";
import {
  buildCategoryPricingOverrideDraft,
  categoriesAvailableForPricingOverride,
  filterCategoryPricingOverrides,
  formatCategoryPricingDiscountLabel,
  type CategoryPricingOverrideRule,
} from "@/features/connected-commerce/category-pricing-overrides";
import {
  ExitsTable,
  ExitsTableActions,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableMobile,
  ExitsTableMobileRow,
  ExitsTableRow,
  ExitsTableToolbar,
} from "@/components/exits/ExitsTable";
import { BottomSheet, ConfirmationDialog } from "@/components/exits/SheetDialog";
import { ProductCategoryMultiSelect } from "@/components/exits/ProductCategoryMultiSelect";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { Button, buttonIconMotion } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useI18n } from "@/i18n/I18nProvider";

type CategoryOption = { categoryId: string; name: string };

type EditorState =
  | { kind: "add" }
  | { kind: "edit"; categoryId: string }
  | null;

type Props = {
  rules: CategoryPricingOverrideRule[];
  categories: CategoryOption[];
  canEdit: boolean;
  onChange: (next: CategoryPricingOverrideRule[]) => void;
  /** Optional copy overrides (e.g. customer-level pricing surfaces). */
  title?: string;
  hierarchyHelp?: string;
  emptyTitle?: string;
  emptyDetail?: string;
  removeDetail?: string;
  /** When false, omit the panel title/help (e.g. host modal already titled). Default true. */
  showHeader?: boolean;
  /** Raise nested editor/confirm sheet above a host BottomSheet. */
  nested?: boolean;
};

function RowActionIcons({
  disabled,
  onEdit,
  onRemove,
  editLabel,
  removeLabel,
  editTestId,
  removeTestId,
}: {
  disabled: boolean;
  onEdit: () => void;
  onRemove: () => void;
  editLabel: string;
  removeLabel: string;
  editTestId: string;
  removeTestId: string;
}) {
  return (
    <ExitsTableActions>
      <Button
        type="button"
        appearance="ghost"
        intent="neutral"
        size="icon"
        shape="round"
        disabled={disabled}
        aria-label={editLabel}
        title={editLabel}
        data-testid={editTestId}
        onClick={onEdit}
      >
        <Pencil className="size-4" aria-hidden />
      </Button>
      <Button
        type="button"
        appearance="ghost"
        intent="danger"
        size="icon"
        shape="round"
        disabled={disabled}
        aria-label={removeLabel}
        title={removeLabel}
        data-testid={removeTestId}
        onClick={onRemove}
      >
        <Trash2 className={`size-4 ${buttonIconMotion.delete}`} aria-hidden />
      </Button>
    </ExitsTableActions>
  );
}

export function CategoryPricingOverridesPanel({
  rules,
  categories,
  canEdit,
  onChange,
  title,
  hierarchyHelp,
  emptyTitle,
  emptyDetail,
  removeDetail,
  showHeader = true,
  nested = false,
}: Props) {
  const { t } = useI18n();
  const panelTitle = title ?? t("connectedCommerce.categoryRules");
  const panelHelp = hierarchyHelp ?? t("connectedCommerce.pricingOverride.hierarchyHelp");
  const panelEmptyTitle = emptyTitle ?? t("connectedCommerce.pricingOverride.emptyTitle");
  const panelEmptyDetail = emptyDetail ?? t("connectedCommerce.pricingOverride.emptyDetail");
  const panelRemoveDetail =
    removeDetail ?? t("connectedCommerce.pricingOverride.removeDetail");
  const nestedBackdropClass = nested ? "!z-[85]" : undefined;
  const nestedPanelClass = nested ? "!z-[90]" : undefined;
  const nestedConfirmClass = nested ? "!z-[95]" : undefined;
  const [search, setSearch] = useState("");
  const [editor, setEditor] = useState<EditorState>(null);
  const [removeCategoryId, setRemoveCategoryId] = useState<string | null>(null);

  const [draftCategoryIds, setDraftCategoryIds] = useState<string[]>([]);
  const [draftCategoryId, setDraftCategoryId] = useState("");
  const [draftDiscountPercent, setDraftDiscountPercent] = useState(0);

  const categoryNameById = useMemo(() => {
    const map = new Map<string, string>();
    for (const c of categories) {
      map.set(c.categoryId, c.name);
    }
    return map;
  }, [categories]);

  const filtered = useMemo(
    () =>
      filterCategoryPricingOverrides(rules, {
        search,
        categoryNameById,
      }).sort((a, b) => {
        const an = categoryNameById.get(a.categoryId) ?? a.categoryId;
        const bn = categoryNameById.get(b.categoryId) ?? b.categoryId;
        return an.localeCompare(bn);
      }),
    [rules, search, categoryNameById],
  );

  const availableCategories = useMemo(
    () => categoriesAvailableForPricingOverride(categories, rules),
    [categories, rules],
  );

  function openAdd() {
    setDraftCategoryIds([]);
    setDraftCategoryId("");
    setDraftDiscountPercent(0);
    setEditor({ kind: "add" });
  }

  function openEdit(rule: CategoryPricingOverrideRule) {
    setDraftCategoryIds([]);
    setDraftCategoryId(rule.categoryId);
    setDraftDiscountPercent(rule.discountPercent);
    setEditor({ kind: "edit", categoryId: rule.categoryId });
  }

  function closeEditor() {
    setEditor(null);
    setDraftCategoryIds([]);
  }

  function saveEditor() {
    if (!editor) {
      return;
    }
    if (editor.kind === "add") {
      const existing = new Set(rules.map((r) => r.categoryId));
      const toAdd = draftCategoryIds.filter((id) => id && !existing.has(id));
      if (toAdd.length === 0) {
        return;
      }
      onChange([
        ...rules,
        ...toAdd.map((id) => buildCategoryPricingOverrideDraft(id, draftDiscountPercent)),
      ]);
      closeEditor();
      return;
    }

    onChange(
      rules.map((r) =>
        r.categoryId === editor.categoryId
          ? buildCategoryPricingOverrideDraft(editor.categoryId, draftDiscountPercent)
          : r,
      ),
    );
    closeEditor();
  }

  function confirmRemove() {
    if (!removeCategoryId) {
      return;
    }
    onChange(rules.filter((r) => r.categoryId !== removeCategoryId));
    setRemoveCategoryId(null);
  }

  const editorCategoryName =
    categoryNameById.get(draftCategoryId) ??
    (draftCategoryId ? draftCategoryId : t("connectedCommerce.selectCategory"));

  const removeName =
    removeCategoryId == null
      ? ""
      : (categoryNameById.get(removeCategoryId) ?? removeCategoryId);

  return (
    <div className="flex flex-col gap-2" data-testid="category-pricing-overrides-panel">
      {showHeader ? (
        <div className="flex flex-col gap-1">
          <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {panelTitle}
          </h3>
          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
            {panelHelp}
          </p>
        </div>
      ) : panelHelp ? (
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{panelHelp}</p>
      ) : null}

      {rules.length === 0 ? (
        <div
          className="flex flex-col items-start gap-2 rounded-[var(--exits-radius-md)] border border-dashed border-border p-3"
          data-testid="category-pricing-overrides-empty"
        >
          <p className="m-0 text-[length:var(--exits-text-sm)] font-medium">
            {panelEmptyTitle}
          </p>
          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
            {panelEmptyDetail}
          </p>
          {canEdit ? (
            <Button
              type="button"
              appearance="outline"
              size="default"
              disabled={availableCategories.length === 0}
              onClick={openAdd}
              data-testid="category-pricing-overrides-add"
            >
              <Plus className="size-4" aria-hidden />
              {t("connectedCommerce.pricingOverride.add")}
            </Button>
          ) : null}
        </div>
      ) : (
        <ExitsTableContainer data-testid="category-pricing-overrides-table">
          <ExitsTableToolbar
            search={
              <SearchField
                label={t("connectedCommerce.pricingOverride.search")}
                value={search}
                placeholder={t("connectedCommerce.pricingOverride.search")}
                onChange={(e) => setSearch(e.target.value)}
                onClear={() => setSearch("")}
                data-testid="category-pricing-overrides-search"
              />
            }
          >
            {canEdit ? (
              <Button
                type="button"
                appearance="outline"
                size="default"
                className="min-h-8"
                disabled={availableCategories.length === 0}
                onClick={openAdd}
                data-testid="category-pricing-overrides-add"
              >
                <Plus className="size-4" aria-hidden />
                {t("connectedCommerce.pricingOverride.add")}
              </Button>
            ) : null}
          </ExitsTableToolbar>

          {filtered.length === 0 ? (
            <p
              className="m-0 p-3 text-[length:var(--exits-text-sm)] text-muted"
              data-testid="category-pricing-overrides-no-match"
            >
              {t("connectedCommerce.pricingOverride.noMatch")}
            </p>
          ) : (
            <>
              <ExitsTable data-testid="category-pricing-overrides-desktop">
                <ExitsTableHeader>
                  <ExitsTableRow>
                    <ExitsTableHead cellAlign="text" className="category-pricing-col--name">
                      {t("connectedCommerce.pricingOverride.colCategory")}
                    </ExitsTableHead>
                    <ExitsTableHead cellAlign="text" className="category-pricing-col--discount">
                      {t("connectedCommerce.pricingOverride.colDiscount")}
                    </ExitsTableHead>
                    <ExitsTableHead cellAlign="actions" className="category-pricing-col--actions">
                      {t("connectedCommerce.pricingOverride.colActions")}
                    </ExitsTableHead>
                  </ExitsTableRow>
                </ExitsTableHeader>
                <ExitsTableBody>
                  {filtered.map((rule) => {
                    const name = categoryNameById.get(rule.categoryId) ?? rule.categoryId;
                    return (
                      <ExitsTableRow
                        key={rule.categoryId}
                        data-testid={`category-pricing-override-row-${rule.categoryId}`}
                      >
                        <ExitsTableCell
                          cellAlign="text"
                          className="category-pricing-col--name font-medium"
                          truncate
                          title={name}
                        >
                          {name}
                        </ExitsTableCell>
                        <ExitsTableCell cellAlign="text" className="category-pricing-col--discount">
                          {formatCategoryPricingDiscountLabel(rule)}
                        </ExitsTableCell>
                        <ExitsTableCell cellAlign="actions" className="category-pricing-col--actions">
                          <RowActionIcons
                            disabled={!canEdit}
                            editLabel={t("connectedCommerce.pricingOverride.edit")}
                            removeLabel={t("connectedCommerce.pricingOverride.remove")}
                            editTestId={`category-pricing-override-edit-${rule.categoryId}`}
                            removeTestId={`category-pricing-override-remove-${rule.categoryId}`}
                            onEdit={() => openEdit(rule)}
                            onRemove={() => setRemoveCategoryId(rule.categoryId)}
                          />
                        </ExitsTableCell>
                      </ExitsTableRow>
                    );
                  })}
                </ExitsTableBody>
              </ExitsTable>

              <ExitsTableMobile data-testid="category-pricing-overrides-mobile">
                {filtered.map((rule) => {
                  const name = categoryNameById.get(rule.categoryId) ?? rule.categoryId;
                  return (
                    <ExitsTableMobileRow
                      key={rule.categoryId}
                      data-testid={`category-pricing-override-mobile-${rule.categoryId}`}
                    >
                      <div className="exits-table-mobile__title-row">
                        <span className="exits-table-mobile__title">{name}</span>
                        <RowActionIcons
                          disabled={!canEdit}
                          editLabel={t("connectedCommerce.pricingOverride.edit")}
                          removeLabel={t("connectedCommerce.pricingOverride.remove")}
                          editTestId={`category-pricing-override-mobile-edit-${rule.categoryId}`}
                          removeTestId={`category-pricing-override-mobile-remove-${rule.categoryId}`}
                          onEdit={() => openEdit(rule)}
                          onRemove={() => setRemoveCategoryId(rule.categoryId)}
                        />
                      </div>
                      <p className="exits-table-mobile__meta m-0">
                        {formatCategoryPricingDiscountLabel(rule)}
                      </p>
                    </ExitsTableMobileRow>
                  );
                })}
              </ExitsTableMobile>
            </>
          )}
        </ExitsTableContainer>
      )}

      <BottomSheet
        open={editor != null}
        onClose={closeEditor}
        title={
          editor?.kind === "edit"
            ? t("connectedCommerce.pricingOverride.editTitle")
            : t("connectedCommerce.pricingOverride.addTitle")
        }
        panelId="category-pricing-override-editor"
        testId="category-pricing-override-editor"
        closeLabel={t("connectedCommerce.pricingOverride.cancel")}
        presentation="sheet-mobile-dialog-desktop"
        backdropClassName={nestedBackdropClass}
        panelClassName={nestedPanelClass}
      >
        <div className="flex flex-col gap-3">
          {editor?.kind === "add" ? (
            <>
              <ProductCategoryMultiSelect
                label={t("connectedCommerce.pricingOverride.colCategory")}
                categories={availableCategories}
                selectedIds={draftCategoryIds}
                onChange={setDraftCategoryIds}
                placeholder={t("connectedCommerce.selectCategory")}
                selectedCountLabel={(count) =>
                  t("purchasing.categoriesSelected").replace("{count}", String(count))
                }
                selectAllLabel={t("purchasing.selectAllCategories")}
                clearAllLabel={t("purchasing.deselectAllCategories")}
                searchPlaceholder={t("catalog.searchCategories")}
                menuLabel={t("connectedCommerce.pricingOverride.addTitle")}
                testId="category-pricing-override-category"
              />
              <div className="w-[20%] min-w-[4.5rem] max-w-full">
                <Input
                  label={t("connectedCommerce.pricingOverride.discountLabel")}
                  type="number"
                  min={0}
                  max={100}
                  step="0.01"
                  value={draftDiscountPercent}
                  onChange={(e) => setDraftDiscountPercent(Number(e.target.value || 0))}
                  data-testid="category-pricing-override-discount"
                />
              </div>
            </>
          ) : (
            <div className="grid w-full grid-cols-1 items-start gap-3 sm:grid-cols-2">
              <div
                className="flex min-w-0 w-full flex-col gap-1.5"
                data-testid="category-pricing-override-category-readonly"
              >
                <p className="m-0 text-[length:var(--exits-text-sm)] font-bold">
                  {t("connectedCommerce.pricingOverride.colCategory")}
                </p>
                <StatusChip
                  tone="primary"
                  shape="standard"
                  appearance="soft"
                  className="w-full max-w-full justify-start [--exits-status-chip-font-size:var(--exits-text-md)] [--exits-status-chip-height:2.125rem] [--exits-status-chip-padding-x:0.75rem]"
                >
                  {editorCategoryName}
                </StatusChip>
              </div>
              <div className="flex min-w-0 w-full flex-col gap-1.5">
                <p className="m-0 text-[length:var(--exits-text-sm)] font-bold">
                  {t("connectedCommerce.pricingOverride.discountLabel")}
                </p>
                <input
                  type="number"
                  min={0}
                  max={100}
                  step="0.01"
                  className="exits-input h-[var(--exits-control-height)] min-h-[var(--exits-control-height)] w-full rounded-[var(--exits-field-radius)] border border-border bg-surface px-[var(--exits-control-padding-x)] text-[length:var(--exits-text-md)] text-foreground"
                  value={draftDiscountPercent}
                  onChange={(e) => setDraftDiscountPercent(Number(e.target.value || 0))}
                  data-testid="category-pricing-override-discount"
                  aria-label={t("connectedCommerce.pricingOverride.discountLabel")}
                />
              </div>
            </div>
          )}

          <div className="flex flex-wrap justify-end gap-2 border-t border-border pt-3">
            <Button type="button" intent="danger" appearance="solid" onClick={closeEditor}>
              {t("connectedCommerce.pricingOverride.cancel")}
            </Button>
            <Button
              type="button"
              disabled={
                editor?.kind === "add" &&
                (draftCategoryIds.length === 0 || availableCategories.length === 0)
              }
              onClick={saveEditor}
              data-testid="category-pricing-override-save"
            >
              {t("connectedCommerce.pricingOverride.save")}
            </Button>
          </div>
        </div>
      </BottomSheet>

      <ConfirmationDialog
        open={removeCategoryId != null}
        title={t("connectedCommerce.pricingOverride.removeTitle")}
        detail={panelRemoveDetail.replace("{category}", removeName)}
        confirmLabel={t("connectedCommerce.pricingOverride.removeConfirm")}
        cancelLabel={t("connectedCommerce.pricingOverride.cancel")}
        confirmTone="danger"
        cancelTone="danger-soft"
        onConfirm={confirmRemove}
        onCancel={() => setRemoveCategoryId(null)}
        testId="category-pricing-override-remove-confirm"
        className={nestedConfirmClass}
      />
    </div>
  );
}
