import { useMemo, useState } from "react";
import { Pencil, Plus, Trash2 } from "lucide-react";
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

export type CategoryExpirySaleOverrideRule = {
  categoryId: string;
  stopSellingDaysBeforeExpiry: number;
};

type CategoryOption = { categoryId: string; name: string };

type EditorMode = "UseDefault" | "Custom";

type EditorState =
  | { kind: "add" }
  | { kind: "edit"; categoryId: string }
  | null;

type Props = {
  rules: CategoryExpirySaleOverrideRule[];
  categories: CategoryOption[];
  canEdit: boolean;
  onChange: (next: CategoryExpirySaleOverrideRule[]) => void;
  /** Optional copy prefix for branch vs org labels. Defaults to inventory.expirySaleOverride.* */
  testIdPrefix?: string;
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

export function CategoryExpirySaleOverridesPanel({
  rules,
  categories,
  canEdit,
  onChange,
  testIdPrefix = "category-expiry-sale-overrides",
}: Props) {
  const { t } = useI18n();
  const [search, setSearch] = useState("");
  const [editor, setEditor] = useState<EditorState>(null);
  const [removeCategoryId, setRemoveCategoryId] = useState<string | null>(null);

  const [draftCategoryIds, setDraftCategoryIds] = useState<string[]>([]);
  const [draftCategoryId, setDraftCategoryId] = useState("");
  const [draftMode, setDraftMode] = useState<EditorMode>("Custom");
  const [draftDays, setDraftDays] = useState(0);

  const categoryNameById = useMemo(() => {
    const map = new Map<string, string>();
    for (const c of categories) {
      map.set(c.categoryId, c.name);
    }
    return map;
  }, [categories]);

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    return rules
      .filter((rule) => {
        if (!q) return true;
        const name = categoryNameById.get(rule.categoryId) ?? rule.categoryId;
        return name.toLowerCase().includes(q);
      })
      .sort((a, b) => {
        const an = categoryNameById.get(a.categoryId) ?? a.categoryId;
        const bn = categoryNameById.get(b.categoryId) ?? b.categoryId;
        return an.localeCompare(bn);
      });
  }, [rules, search, categoryNameById]);

  const availableCategories = useMemo(() => {
    const taken = new Set(rules.map((r) => r.categoryId));
    return categories.filter((c) => !taken.has(c.categoryId));
  }, [categories, rules]);

  function openAdd() {
    setDraftCategoryIds([]);
    setDraftCategoryId("");
    setDraftMode("Custom");
    setDraftDays(0);
    setEditor({ kind: "add" });
  }

  function openEdit(rule: CategoryExpirySaleOverrideRule) {
    setDraftCategoryIds([]);
    setDraftCategoryId(rule.categoryId);
    setDraftMode("Custom");
    setDraftDays(rule.stopSellingDaysBeforeExpiry);
    setEditor({ kind: "edit", categoryId: rule.categoryId });
  }

  function closeEditor() {
    setEditor(null);
    setDraftCategoryIds([]);
  }

  function saveEditor() {
    if (!editor) return;

    if (editor.kind === "add") {
      const existing = new Set(rules.map((r) => r.categoryId));
      const toAdd = draftCategoryIds.filter((id) => id && !existing.has(id));
      if (toAdd.length === 0 || draftMode !== "Custom") return;
      const days = Number.isFinite(draftDays) ? Math.max(0, Math.min(365, Math.trunc(draftDays))) : 0;
      onChange([
        ...rules,
        ...toAdd.map((id) => ({
          categoryId: id,
          stopSellingDaysBeforeExpiry: days,
        })),
      ]);
      closeEditor();
      return;
    }

    if (draftMode === "UseDefault") {
      onChange(rules.filter((r) => r.categoryId !== editor.categoryId));
      closeEditor();
      return;
    }

    const days = Number.isFinite(draftDays) ? Math.max(0, Math.min(365, Math.trunc(draftDays))) : 0;
    onChange(
      rules.map((r) =>
        r.categoryId === editor.categoryId
          ? { categoryId: editor.categoryId, stopSellingDaysBeforeExpiry: days }
          : r,
      ),
    );
    closeEditor();
  }

  function confirmRemove() {
    if (!removeCategoryId) return;
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

  const daysLabel = (n: number) =>
    t("inventory.expirySaleOverride.days").replace("{n}", String(n));

  return (
    <div className="flex flex-col gap-2" data-testid={testIdPrefix}>
      <div className="flex flex-col gap-1">
        <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
          {t("inventory.expirySaleOverride.title")}
        </h3>
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
          {t("inventory.expirySaleOverride.hierarchyHelp")}
        </p>
      </div>

      {rules.length === 0 ? (
        <div
          className="flex flex-col items-start gap-2 rounded-[var(--exits-radius-md)] border border-dashed border-border p-3"
          data-testid={`${testIdPrefix}-empty`}
        >
          <p className="m-0 text-[length:var(--exits-text-sm)] font-medium">
            {t("inventory.expirySaleOverride.emptyTitle")}
          </p>
          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
            {t("inventory.expirySaleOverride.emptyDetail")}
          </p>
          {canEdit ? (
            <Button
              type="button"
              appearance="outline"
              size="default"
              disabled={availableCategories.length === 0}
              onClick={openAdd}
              data-testid={`${testIdPrefix}-add`}
            >
              <Plus className="size-4" aria-hidden />
              {t("inventory.expirySaleOverride.add")}
            </Button>
          ) : null}
        </div>
      ) : (
        <ExitsTableContainer data-testid={`${testIdPrefix}-table`}>
          <ExitsTableToolbar
            search={
              <SearchField
                label={t("inventory.expirySaleOverride.search")}
                value={search}
                placeholder={t("inventory.expirySaleOverride.search")}
                onChange={(e) => setSearch(e.target.value)}
                onClear={() => setSearch("")}
                data-testid={`${testIdPrefix}-search`}
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
                data-testid={`${testIdPrefix}-add`}
              >
                <Plus className="size-4" aria-hidden />
                {t("inventory.expirySaleOverride.add")}
              </Button>
            ) : null}
          </ExitsTableToolbar>

          {filtered.length === 0 ? (
            <p
              className="m-0 p-3 text-[length:var(--exits-text-sm)] text-muted"
              data-testid={`${testIdPrefix}-no-match`}
            >
              {t("inventory.expirySaleOverride.noMatch")}
            </p>
          ) : (
            <>
              <ExitsTable data-testid={`${testIdPrefix}-desktop`}>
                <ExitsTableHeader>
                  <ExitsTableRow>
                    <ExitsTableHead cellAlign="text">
                      {t("inventory.expirySaleOverride.colCategory")}
                    </ExitsTableHead>
                    <ExitsTableHead cellAlign="text">
                      {t("inventory.expirySaleOverride.colDays")}
                    </ExitsTableHead>
                    <ExitsTableHead cellAlign="actions">
                      {t("inventory.expirySaleOverride.colActions")}
                    </ExitsTableHead>
                  </ExitsTableRow>
                </ExitsTableHeader>
                <ExitsTableBody>
                  {filtered.map((rule) => {
                    const name = categoryNameById.get(rule.categoryId) ?? rule.categoryId;
                    return (
                      <ExitsTableRow
                        key={rule.categoryId}
                        data-testid={`${testIdPrefix}-row-${rule.categoryId}`}
                      >
                        <ExitsTableCell cellAlign="text" className="font-medium">
                          {name}
                        </ExitsTableCell>
                        <ExitsTableCell cellAlign="text">
                          {daysLabel(rule.stopSellingDaysBeforeExpiry)}
                        </ExitsTableCell>
                        <ExitsTableCell cellAlign="actions">
                          <RowActionIcons
                            disabled={!canEdit}
                            editLabel={t("inventory.expirySaleOverride.edit")}
                            removeLabel={t("inventory.expirySaleOverride.remove")}
                            editTestId={`${testIdPrefix}-edit-${rule.categoryId}`}
                            removeTestId={`${testIdPrefix}-remove-${rule.categoryId}`}
                            onEdit={() => openEdit(rule)}
                            onRemove={() => setRemoveCategoryId(rule.categoryId)}
                          />
                        </ExitsTableCell>
                      </ExitsTableRow>
                    );
                  })}
                </ExitsTableBody>
              </ExitsTable>

              <ExitsTableMobile data-testid={`${testIdPrefix}-mobile`}>
                {filtered.map((rule) => {
                  const name = categoryNameById.get(rule.categoryId) ?? rule.categoryId;
                  return (
                    <ExitsTableMobileRow
                      key={rule.categoryId}
                      data-testid={`${testIdPrefix}-mobile-${rule.categoryId}`}
                    >
                      <div className="exits-table-mobile__title-row">
                        <span className="exits-table-mobile__title">{name}</span>
                        <RowActionIcons
                          disabled={!canEdit}
                          editLabel={t("inventory.expirySaleOverride.edit")}
                          removeLabel={t("inventory.expirySaleOverride.remove")}
                          editTestId={`${testIdPrefix}-mobile-edit-${rule.categoryId}`}
                          removeTestId={`${testIdPrefix}-mobile-remove-${rule.categoryId}`}
                          onEdit={() => openEdit(rule)}
                          onRemove={() => setRemoveCategoryId(rule.categoryId)}
                        />
                      </div>
                      <p className="exits-table-mobile__meta m-0">
                        {daysLabel(rule.stopSellingDaysBeforeExpiry)}
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
            ? t("inventory.expirySaleOverride.editTitle")
            : t("inventory.expirySaleOverride.addTitle")
        }
        panelId={`${testIdPrefix}-editor`}
        testId={`${testIdPrefix}-editor`}
        closeLabel={t("inventory.expirySaleOverride.cancel")}
        presentation="sheet-mobile-dialog-desktop"
      >
        <div className="flex flex-col gap-3">
          {editor?.kind === "add" ? (
            <ProductCategoryMultiSelect
              label={t("inventory.expirySaleOverride.colCategory")}
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
              menuLabel={t("inventory.expirySaleOverride.addTitle")}
              testId={`${testIdPrefix}-category`}
            />
          ) : (
            <div
              className="flex flex-col gap-1.5"
              data-testid={`${testIdPrefix}-category-readonly`}
            >
              <p className="m-0 text-[length:var(--exits-text-sm)] font-bold">
                {t("inventory.expirySaleOverride.colCategory")}
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

          {editor?.kind === "edit" ? (
            <div className="flex flex-col gap-1.5">
              <span className="exits-type-label" id={`${testIdPrefix}-mode-label`}>
                {t("inventory.expirySaleOverride.colPolicy")}
              </span>
              <ExitsPillSelect
                mode="single"
                aria-label={t("inventory.expirySaleOverride.colPolicy")}
                value={draftMode}
                onChange={setDraftMode}
                options={[
                  {
                    value: "UseDefault",
                    label: t("inventory.expirySaleOverride.modeUseDefault"),
                  },
                  {
                    value: "Custom",
                    label: t("inventory.expirySaleOverride.modeCustom"),
                  },
                ]}
                testId={`${testIdPrefix}-mode`}
              />
            </div>
          ) : null}

          {draftMode === "Custom" ? (
            <div className="w-[20%] min-w-[4.5rem] max-w-full">
              <Input
                label={t("inventory.expirySaleOverride.daysLabel")}
                type="number"
                min={0}
                max={365}
                step={1}
                value={draftDays}
                onChange={(e) => setDraftDays(Number(e.target.value || 0))}
                data-testid={`${testIdPrefix}-days`}
              />
            </div>
          ) : null}

          <div className="flex flex-wrap justify-end gap-2 border-t border-border pt-3">
            <Button type="button" intent="danger" appearance="solid" onClick={closeEditor}>
              {t("inventory.expirySaleOverride.cancel")}
            </Button>
            <Button
              type="button"
              disabled={
                editor?.kind === "add" &&
                (availableCategories.length === 0 || draftCategoryIds.length === 0)
              }
              onClick={saveEditor}
              data-testid={`${testIdPrefix}-save`}
            >
              {t("inventory.expirySaleOverride.save")}
            </Button>
          </div>
        </div>
      </BottomSheet>

      <ConfirmationDialog
        open={removeCategoryId != null}
        title={t("inventory.expirySaleOverride.removeTitle")}
        detail={t("inventory.expirySaleOverride.removeDetail").replace("{category}", removeName)}
        confirmLabel={t("inventory.expirySaleOverride.removeConfirm")}
        cancelLabel={t("inventory.expirySaleOverride.cancel")}
        confirmTone="danger"
        cancelTone="danger-soft"
        onConfirm={confirmRemove}
        onCancel={() => setRemoveCategoryId(null)}
        testId={`${testIdPrefix}-remove-confirm`}
      />
    </div>
  );
}
