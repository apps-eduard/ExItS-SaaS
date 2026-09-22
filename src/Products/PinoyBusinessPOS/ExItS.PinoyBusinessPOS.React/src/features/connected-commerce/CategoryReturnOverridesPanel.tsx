import { useMemo, useState } from "react";
import { Pencil, Plus, Trash2 } from "lucide-react";
import {
  buildCategoryReturnOverrideDraft,
  categoriesAvailableForReturnOverride,
  filterCategoryReturnOverrides,
  formatCategoryReturnPolicyLabel,
  formatCategoryReturnWindowLabel,
  normalizeCategoryReturnMode,
  type CategoryReturnOverrideRule,
  type CategoryReturnPolicyFilter,
  type CategoryReturnPolicyMode,
} from "@/features/connected-commerce/category-return-overrides";
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
import { ExitsPillSelect } from "@/components/exits/ExitsPillSelect";
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
  rules: CategoryReturnOverrideRule[];
  categories: CategoryOption[];
  canEdit: boolean;
  onChange: (next: CategoryReturnOverrideRule[]) => void;
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

export function CategoryReturnOverridesPanel({ rules, categories, canEdit, onChange }: Props) {
  const { t } = useI18n();
  const [search, setSearch] = useState("");
  const [policyFilter, setPolicyFilter] = useState<CategoryReturnPolicyFilter>("all");
  const [editor, setEditor] = useState<EditorState>(null);
  const [removeCategoryId, setRemoveCategoryId] = useState<string | null>(null);

  const [draftCategoryIds, setDraftCategoryIds] = useState<string[]>([]);
  const [draftCategoryId, setDraftCategoryId] = useState("");
  const [draftMode, setDraftMode] = useState<CategoryReturnPolicyMode>("NonReturnable");
  const [draftWindowDays, setDraftWindowDays] = useState<number | null>(null);

  const categoryNameById = useMemo(() => {
    const map = new Map<string, string>();
    for (const c of categories) {
      map.set(c.categoryId, c.name);
    }
    return map;
  }, [categories]);

  const policyLabels = useMemo(
    () => ({
      useDefault: t("connectedCommerce.returnOverride.policyUseDefault"),
      custom: t("connectedCommerce.returnOverride.policyCustom"),
      nonReturnable: t("connectedCommerce.returnOverride.policyNonReturnable"),
      days: (n: number) =>
        t("connectedCommerce.returnOverride.days").replace("{n}", String(n)),
      noTimeLimit: t("connectedCommerce.returnOverride.noTimeLimit"),
      dash: "—",
    }),
    [t],
  );

  const filtered = useMemo(
    () =>
      filterCategoryReturnOverrides(rules, {
        search,
        policyFilter,
        categoryNameById,
      }).sort((a, b) => {
        const an = categoryNameById.get(a.categoryId) ?? a.categoryId;
        const bn = categoryNameById.get(b.categoryId) ?? b.categoryId;
        return an.localeCompare(bn);
      }),
    [rules, search, policyFilter, categoryNameById],
  );

  const availableCategories = useMemo(
    () => categoriesAvailableForReturnOverride(categories, rules),
    [categories, rules],
  );

  function openAdd() {
    setDraftCategoryIds([]);
    setDraftCategoryId("");
    setDraftMode("NonReturnable");
    setDraftWindowDays(null);
    setEditor({ kind: "add" });
  }

  function openEdit(rule: CategoryReturnOverrideRule) {
    setDraftCategoryIds([]);
    setDraftCategoryId(rule.categoryId);
    setDraftMode(normalizeCategoryReturnMode(rule.mode));
    setDraftWindowDays(rule.returnWindowDays ?? null);
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
        ...toAdd.map((id) => buildCategoryReturnOverrideDraft(id, draftMode, draftWindowDays)),
      ]);
      closeEditor();
      return;
    }

    onChange(
      rules.map((r) =>
        r.categoryId === editor.categoryId
          ? buildCategoryReturnOverrideDraft(editor.categoryId, draftMode, draftWindowDays)
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
    <div className="flex flex-col gap-2" data-testid="category-return-overrides-panel">
      <div className="flex flex-col gap-1">
        <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
          {t("connectedCommerce.returnOverride.title")}
        </h3>
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
          {t("connectedCommerce.returnOverride.hierarchyHelp")}
        </p>
      </div>

      {rules.length === 0 ? (
        <div
          className="flex flex-col items-start gap-2 rounded-[var(--exits-radius-md)] border border-dashed border-border p-3"
          data-testid="category-return-overrides-empty"
        >
          <p className="m-0 text-[length:var(--exits-text-sm)] font-medium">
            {t("connectedCommerce.returnOverride.emptyTitle")}
          </p>
          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
            {t("connectedCommerce.returnOverride.emptyDetail")}
          </p>
          {canEdit ? (
            <Button
              type="button"
              appearance="outline"
              size="default"
              disabled={availableCategories.length === 0}
              onClick={openAdd}
              data-testid="category-return-overrides-add"
            >
              <Plus className="size-4" aria-hidden />
              {t("connectedCommerce.returnOverride.add")}
            </Button>
          ) : null}
        </div>
      ) : (
        <ExitsTableContainer data-testid="category-return-overrides-table">
          <ExitsTableToolbar
            search={
              <SearchField
                label={t("connectedCommerce.returnOverride.search")}
                value={search}
                placeholder={t("connectedCommerce.returnOverride.search")}
                onChange={(e) => setSearch(e.target.value)}
                onClear={() => setSearch("")}
                data-testid="category-return-overrides-search"
              />
            }
            filter={
              <label className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                <span className="sr-only">{t("connectedCommerce.returnOverride.policyFilter")}</span>
                <select
                  className="exits-select"
                  value={policyFilter}
                  onChange={(e) => setPolicyFilter(e.target.value as CategoryReturnPolicyFilter)}
                  aria-label={t("connectedCommerce.returnOverride.policyFilter")}
                  data-testid="category-return-overrides-policy-filter"
                >
                  <option value="all">{t("connectedCommerce.returnOverride.filterAll")}</option>
                  <option value="NonReturnable">
                    {t("connectedCommerce.returnOverride.policyNonReturnable")}
                  </option>
                  <option value="Custom">{t("connectedCommerce.returnOverride.policyCustom")}</option>
                  <option value="UseDefault">
                    {t("connectedCommerce.returnOverride.policyUseDefault")}
                  </option>
                </select>
              </label>
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
                data-testid="category-return-overrides-add"
              >
                <Plus className="size-4" aria-hidden />
                {t("connectedCommerce.returnOverride.add")}
              </Button>
            ) : null}
          </ExitsTableToolbar>

          {filtered.length === 0 ? (
            <p
              className="m-0 p-3 text-[length:var(--exits-text-sm)] text-muted"
              data-testid="category-return-overrides-no-match"
            >
              {t("connectedCommerce.returnOverride.noMatch")}
            </p>
          ) : (
            <>
              <ExitsTable data-testid="category-return-overrides-desktop">
                <ExitsTableHeader>
                  <ExitsTableRow>
                    <ExitsTableHead cellAlign="text">
                      {t("connectedCommerce.returnOverride.colCategory")}
                    </ExitsTableHead>
                    <ExitsTableHead cellAlign="text">
                      {t("connectedCommerce.returnOverride.colPolicy")}
                    </ExitsTableHead>
                    <ExitsTableHead cellAlign="text">
                      {t("connectedCommerce.returnOverride.colWindow")}
                    </ExitsTableHead>
                    <ExitsTableHead cellAlign="actions">
                      {t("connectedCommerce.returnOverride.colActions")}
                    </ExitsTableHead>
                  </ExitsTableRow>
                </ExitsTableHeader>
                <ExitsTableBody>
                  {filtered.map((rule) => {
                    const name = categoryNameById.get(rule.categoryId) ?? rule.categoryId;
                    return (
                      <ExitsTableRow
                        key={rule.categoryId}
                        data-testid={`category-return-override-row-${rule.categoryId}`}
                      >
                        <ExitsTableCell cellAlign="text" className="font-medium">
                          {name}
                        </ExitsTableCell>
                        <ExitsTableCell cellAlign="text">
                          {formatCategoryReturnPolicyLabel(rule, policyLabels)}
                        </ExitsTableCell>
                        <ExitsTableCell cellAlign="text">
                          {formatCategoryReturnWindowLabel(rule, policyLabels)}
                        </ExitsTableCell>
                        <ExitsTableCell cellAlign="actions">
                          <RowActionIcons
                            disabled={!canEdit}
                            editLabel={t("connectedCommerce.returnOverride.edit")}
                            removeLabel={t("connectedCommerce.returnOverride.remove")}
                            editTestId={`category-return-override-edit-${rule.categoryId}`}
                            removeTestId={`category-return-override-remove-${rule.categoryId}`}
                            onEdit={() => openEdit(rule)}
                            onRemove={() => setRemoveCategoryId(rule.categoryId)}
                          />
                        </ExitsTableCell>
                      </ExitsTableRow>
                    );
                  })}
                </ExitsTableBody>
              </ExitsTable>

              <ExitsTableMobile data-testid="category-return-overrides-mobile">
                {filtered.map((rule) => {
                  const name = categoryNameById.get(rule.categoryId) ?? rule.categoryId;
                  return (
                    <ExitsTableMobileRow
                      key={rule.categoryId}
                      data-testid={`category-return-override-mobile-${rule.categoryId}`}
                    >
                      <div className="exits-table-mobile__title-row">
                        <span className="exits-table-mobile__title">{name}</span>
                        <RowActionIcons
                          disabled={!canEdit}
                          editLabel={t("connectedCommerce.returnOverride.edit")}
                          removeLabel={t("connectedCommerce.returnOverride.remove")}
                          editTestId={`category-return-override-mobile-edit-${rule.categoryId}`}
                          removeTestId={`category-return-override-mobile-remove-${rule.categoryId}`}
                          onEdit={() => openEdit(rule)}
                          onRemove={() => setRemoveCategoryId(rule.categoryId)}
                        />
                      </div>
                      <p className="exits-table-mobile__meta m-0">
                        {formatCategoryReturnPolicyLabel(rule, policyLabels)}
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
            ? t("connectedCommerce.returnOverride.editTitle")
            : t("connectedCommerce.returnOverride.addTitle")
        }
        panelId="category-return-override-editor"
        testId="category-return-override-editor"
        closeLabel={t("connectedCommerce.returnOverride.cancel")}
        presentation="sheet-mobile-dialog-desktop"
      >
        <div className="flex flex-col gap-3">
          {editor?.kind === "add" ? (
            <ProductCategoryMultiSelect
              label={t("connectedCommerce.returnOverride.colCategory")}
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
              menuLabel={t("connectedCommerce.returnOverride.addTitle")}
              testId="category-return-override-category"
            />
          ) : (
            <div
              className="flex flex-col gap-1.5"
              data-testid="category-return-override-category-readonly"
            >
              <p className="m-0 text-[length:var(--exits-text-sm)] font-bold">
                {t("connectedCommerce.returnOverride.colCategory")}
              </p>
              <StatusChip
                tone="primary"
                shape="standard"
                appearance="soft"
                className="w-fit max-w-full self-start [--exits-status-chip-font-size:var(--exits-text-md)] [--exits-status-chip-height:2.125rem] [--exits-status-chip-padding-x:0.75rem]"
              >
                {editorCategoryName}
              </StatusChip>
            </div>
          )}

          <div className="flex flex-col gap-1.5">
            <span className="exits-type-label" id="category-return-override-mode-label">
              {t("connectedCommerce.returnOverride.colPolicy")}
            </span>
            <ExitsPillSelect
              mode="single"
              aria-label={t("connectedCommerce.returnOverride.colPolicy")}
              value={draftMode}
              onChange={setDraftMode}
              options={[
                {
                  value: "UseDefault",
                  label: t("connectedCommerce.returnOverride.modeUseDefault"),
                },
                {
                  value: "Custom",
                  label: t("connectedCommerce.returnOverride.modeCustom"),
                },
                {
                  value: "NonReturnable",
                  label: t("connectedCommerce.returnOverride.modeNonReturnable"),
                },
              ]}
              testId="category-return-override-mode"
            />
          </div>

          {draftMode === "Custom" ? (
            <div className="w-[20%] min-w-[4.5rem] max-w-full">
              <Input
                label={t("connectedCommerce.returnOverride.windowLabel")}
                type="number"
                min={0}
                step={1}
                placeholder={t("connectedCommerce.returnOverride.noTimeLimit")}
                value={draftWindowDays ?? ""}
                onChange={(e) =>
                  setDraftWindowDays(e.target.value === "" ? null : Number(e.target.value))
                }
                data-testid="category-return-override-window-days"
              />
            </div>
          ) : null}

          <div className="flex flex-wrap justify-end gap-2 border-t border-border pt-3">
            <Button type="button" intent="danger" appearance="solid" onClick={closeEditor}>
              {t("connectedCommerce.returnOverride.cancel")}
            </Button>
            <Button
              type="button"
              disabled={
                editor?.kind === "add" &&
                (availableCategories.length === 0 || draftCategoryIds.length === 0)
              }
              onClick={saveEditor}
              data-testid="category-return-override-save"
            >
              {t("connectedCommerce.returnOverride.save")}
            </Button>
          </div>
        </div>
      </BottomSheet>

      <ConfirmationDialog
        open={removeCategoryId != null}
        title={t("connectedCommerce.returnOverride.removeTitle")}
        detail={t("connectedCommerce.returnOverride.removeDetail").replace(
          "{category}",
          removeName,
        )}
        confirmLabel={t("connectedCommerce.returnOverride.removeConfirm")}
        cancelLabel={t("connectedCommerce.returnOverride.cancel")}
        confirmTone="danger"
        cancelTone="danger-soft"
        onConfirm={confirmRemove}
        onCancel={() => setRemoveCategoryId(null)}
        testId="category-return-override-remove-confirm"
      />
    </div>
  );
}
