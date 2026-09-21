import { useMemo, useState } from "react";
import { MoreHorizontal, Pencil, Plus } from "lucide-react";
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
import { SearchField } from "@/components/exits/SearchField";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { DropdownMenu, MenuItem, useDismissibleOpen } from "@/components/ui/dropdown-menu";
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

function RowOverflowMenu({
  disabled,
  onEdit,
  onRemove,
  editLabel,
  removeLabel,
  moreLabel,
  testId,
}: {
  disabled: boolean;
  onEdit: () => void;
  onRemove: () => void;
  editLabel: string;
  removeLabel: string;
  moreLabel: string;
  testId: string;
}) {
  const menu = useDismissibleOpen(false);
  return (
    <DropdownMenu
      align="end"
      open={disabled ? false : menu.open}
      onOpenChange={(next) => {
        if (disabled) {
          menu.close();
          return;
        }
        menu.setOpen(next);
      }}
      menuLabel={moreLabel}
      trigger={({ id, expanded, controls, onClick, onKeyDown }) => (
        <Button
          type="button"
          id={id}
          variant="ghost"
          size="icon"
          shape="round"
          disabled={disabled}
          aria-label={moreLabel}
          aria-haspopup="menu"
          aria-expanded={expanded}
          aria-controls={controls}
          data-testid={testId}
          onClick={onClick}
          onKeyDown={onKeyDown}
        >
          <MoreHorizontal className="size-4" aria-hidden />
        </Button>
      )}
    >
      <MenuItem
        onSelect={() => {
          menu.close();
          onEdit();
        }}
      >
        {editLabel}
      </MenuItem>
      <MenuItem
        destructive
        onSelect={() => {
          menu.close();
          onRemove();
        }}
      >
        {removeLabel}
      </MenuItem>
    </DropdownMenu>
  );
}

export function CategoryReturnOverridesPanel({ rules, categories, canEdit, onChange }: Props) {
  const { t } = useI18n();
  const [search, setSearch] = useState("");
  const [policyFilter, setPolicyFilter] = useState<CategoryReturnPolicyFilter>("all");
  const [editor, setEditor] = useState<EditorState>(null);
  const [removeCategoryId, setRemoveCategoryId] = useState<string | null>(null);

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
    setDraftCategoryId(availableCategories[0]?.categoryId ?? "");
    setDraftMode("NonReturnable");
    setDraftWindowDays(null);
    setEditor({ kind: "add" });
  }

  function openEdit(rule: CategoryReturnOverrideRule) {
    setDraftCategoryId(rule.categoryId);
    setDraftMode(normalizeCategoryReturnMode(rule.mode));
    setDraftWindowDays(rule.returnWindowDays ?? null);
    setEditor({ kind: "edit", categoryId: rule.categoryId });
  }

  function closeEditor() {
    setEditor(null);
  }

  function saveEditor() {
    if (!editor) {
      return;
    }
    if (editor.kind === "add") {
      if (!draftCategoryId) {
        return;
      }
      if (rules.some((r) => r.categoryId === draftCategoryId)) {
        return;
      }
      onChange([
        ...rules,
        buildCategoryReturnOverrideDraft(draftCategoryId, draftMode, draftWindowDays),
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
                          <ExitsTableActions>
                            <Button
                              type="button"
                              variant="ghost"
                              size="icon"
                              shape="round"
                              disabled={!canEdit}
                              aria-label={t("connectedCommerce.returnOverride.edit")}
                              title={t("connectedCommerce.returnOverride.edit")}
                              data-testid={`category-return-override-edit-${rule.categoryId}`}
                              onClick={() => openEdit(rule)}
                            >
                              <Pencil className="size-4" aria-hidden />
                            </Button>
                            <RowOverflowMenu
                              disabled={!canEdit}
                              editLabel={t("connectedCommerce.returnOverride.edit")}
                              removeLabel={t("connectedCommerce.returnOverride.remove")}
                              moreLabel={t("connectedCommerce.returnOverride.moreActions")}
                              testId={`category-return-override-more-${rule.categoryId}`}
                              onEdit={() => openEdit(rule)}
                              onRemove={() => setRemoveCategoryId(rule.categoryId)}
                            />
                          </ExitsTableActions>
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
                        <ExitsTableActions>
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon"
                            shape="round"
                            disabled={!canEdit}
                            aria-label={t("connectedCommerce.returnOverride.edit")}
                            data-testid={`category-return-override-mobile-edit-${rule.categoryId}`}
                            onClick={() => openEdit(rule)}
                          >
                            <Pencil className="size-4" aria-hidden />
                          </Button>
                          <RowOverflowMenu
                            disabled={!canEdit}
                            editLabel={t("connectedCommerce.returnOverride.edit")}
                            removeLabel={t("connectedCommerce.returnOverride.remove")}
                            moreLabel={t("connectedCommerce.returnOverride.moreActions")}
                            testId={`category-return-override-mobile-more-${rule.categoryId}`}
                            onEdit={() => openEdit(rule)}
                            onRemove={() => setRemoveCategoryId(rule.categoryId)}
                          />
                        </ExitsTableActions>
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
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              {t("connectedCommerce.returnOverride.colCategory")}
              <select
                className="exits-select"
                value={draftCategoryId}
                onChange={(e) => setDraftCategoryId(e.target.value)}
                data-testid="category-return-override-category"
              >
                {availableCategories.length === 0 ? (
                  <option value="">{t("connectedCommerce.returnOverride.noCategoriesLeft")}</option>
                ) : (
                  availableCategories.map((c) => (
                    <option key={c.categoryId} value={c.categoryId}>
                      {c.name}
                    </option>
                  ))
                )}
              </select>
            </label>
          ) : (
            <div data-testid="category-return-override-category-readonly">
              <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                {t("connectedCommerce.returnOverride.colCategory")}
              </p>
              <p className="m-0 text-[length:var(--exits-text-sm)] font-medium">{editorCategoryName}</p>
            </div>
          )}

          <fieldset className="m-0 flex flex-col gap-2 border-0 p-0">
            <legend className="mb-1 text-[length:var(--exits-text-sm)] font-medium">
              {t("connectedCommerce.returnOverride.colPolicy")}
            </legend>
            {(
              [
                ["UseDefault", t("connectedCommerce.returnOverride.modeUseDefault")],
                ["Custom", t("connectedCommerce.returnOverride.modeCustom")],
                ["NonReturnable", t("connectedCommerce.returnOverride.modeNonReturnable")],
              ] as const
            ).map(([value, label]) => (
              <label
                key={value}
                className="flex items-center gap-2 text-[length:var(--exits-text-sm)]"
              >
                <input
                  type="radio"
                  name="category-return-override-mode"
                  value={value}
                  checked={draftMode === value}
                  onChange={() => setDraftMode(value)}
                  data-testid={`category-return-override-mode-${value}`}
                />
                {label}
              </label>
            ))}
          </fieldset>

          {draftMode === "Custom" ? (
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
          ) : null}

          <div className="flex flex-wrap justify-end gap-2">
            <Button type="button" variant="ghost" onClick={closeEditor}>
              {t("connectedCommerce.returnOverride.cancel")}
            </Button>
            <Button
              type="button"
              disabled={
                editor?.kind === "add" &&
                (availableCategories.length === 0 || draftCategoryId === "")
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
        onConfirm={confirmRemove}
        onCancel={() => setRemoveCategoryId(null)}
        testId="category-return-override-remove-confirm"
      />
    </div>
  );
}
