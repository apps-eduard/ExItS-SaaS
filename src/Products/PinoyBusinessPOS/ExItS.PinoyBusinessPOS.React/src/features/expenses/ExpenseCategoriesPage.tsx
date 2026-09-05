import { useEffect, useMemo, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { canManageExpenses } from "@/access/pos-capabilities";
import {
  EXPENSE_CATEGORY_NAME_MAX,
  createExpenseCategory,
  deactivateExpenseCategory,
  expenseWorkspaceScope,
  listExpenseCategories,
  reactivateExpenseCategory,
  updateExpenseCategory,
  type PosExpenseCategoryDto,
} from "@/api/pos/pos-expense-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { expenseCategoryStatusLabelKey } from "@/features/expenses/expense-labels";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function ExpenseCategoriesPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageExpenses(sessionGrant);
  const [statusFilter, setStatusFilter] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [name, setName] = useState("");
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<PosExpenseCategoryDto | null>(null);
  const [editName, setEditName] = useState("");
  const [busyId, setBusyId] = useState<string | null>(null);

  const organizationId = boundWorkspace?.organizationId ?? null;
  const workspace = useMemo(
    () => (organizationId ? expenseWorkspaceScope(organizationId, boundWorkspace?.branchId) : null),
    [organizationId, boundWorkspace?.branchId],
  );

  useEffect(() => {
    setStatusFilter("");
    setName("");
    setEditing(null);
  }, [organizationId]);

  const query = useQuery({
    queryKey: ["expense-categories", organizationId, statusFilter],
    enabled: Boolean(workspace) && online,
    queryFn: ({ signal }) =>
      listExpenseCategories(
        workspace!,
        { status: statusFilter || undefined, page: 1, pageSize: 100 },
        signal,
      ),
  });

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  const items = query.data?.items ?? [];

  async function onCreate() {
    if (!workspace || !allowManage || creating || !online) {
      return;
    }
    const trimmed = name.trim();
    if (!trimmed) {
      setError(t("expense.validation.categoryNameRequired"));
      return;
    }
    setCreating(true);
    setError(null);
    try {
      await createExpenseCategory(workspace, { name: trimmed });
      setName("");
      await queryClient.invalidateQueries({ queryKey: ["expense-categories"] });
    } catch (err) {
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("expense.categoryCreateFailed"))
          : t("expense.categoryCreateFailed"),
      );
    } finally {
      setCreating(false);
    }
  }

  async function onSaveEdit() {
    if (!workspace || !editing || !allowManage || !online) {
      return;
    }
    const trimmed = editName.trim();
    if (!trimmed) {
      setError(t("expense.validation.categoryNameRequired"));
      return;
    }
    setBusyId(editing.categoryId);
    setError(null);
    try {
      await updateExpenseCategory(workspace, editing.categoryId, {
        name: trimmed,
        expectedUpdatedAtUtc: editing.updatedAtUtc,
      });
      setEditing(null);
      await queryClient.invalidateQueries({ queryKey: ["expense-categories"] });
    } catch (err) {
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("expense.categoryUpdateFailed"))
          : t("expense.categoryUpdateFailed"),
      );
    } finally {
      setBusyId(null);
    }
  }

  async function onDeactivate(category: PosExpenseCategoryDto) {
    if (!workspace || !allowManage || !online) {
      return;
    }
    if (!window.confirm(t("expense.category.deactivateConfirm"))) {
      return;
    }
    setBusyId(category.categoryId);
    setError(null);
    try {
      await deactivateExpenseCategory(workspace, category.categoryId);
      await queryClient.invalidateQueries({ queryKey: ["expense-categories"] });
    } catch (err) {
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("expense.categoryDeactivateFailed"))
          : t("expense.categoryDeactivateFailed"),
      );
    } finally {
      setBusyId(null);
    }
  }

  async function onReactivate(category: PosExpenseCategoryDto) {
    if (!workspace || !allowManage || !online) {
      return;
    }
    setBusyId(category.categoryId);
    setError(null);
    try {
      await reactivateExpenseCategory(workspace, category.categoryId);
      await queryClient.invalidateQueries({ queryKey: ["expense-categories"] });
    } catch (err) {
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("expense.categoryReactivateFailed"))
          : t("expense.categoryReactivateFailed"),
      );
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="expense-categories-page">
      <PageHeader
        title={t("expense.categories")}
        description={t("expense.categoriesLede")}
        backTo="/expenses"
        backLabel={t("expense.backList")}
        backTestId="page-header-back-expenses"
      />

      {!online ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("expense.offline")}</p>
      ) : null}

      {error ? <ErrorState title={t("expense.errorTitle")} detail={error} /> : null}

      {allowManage ? (
        <Card className="flex flex-col gap-2 p-3" data-testid="expense-category-create">
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span className="font-medium">{t("expense.createCategory")}</span>
            <input
              className="rounded-[var(--exits-radius-md)] border border-border bg-background px-3"
              value={name}
              maxLength={EXPENSE_CATEGORY_NAME_MAX}
              onChange={(e) => setName(e.target.value)}
              data-testid="expense-category-name"
            />
          </label>
          <Button
            type="button"
            className="w-fit"
            disabled={!online || creating || !name.trim()}
            onClick={() => void onCreate()}
            data-testid="expense-category-create-submit"
          >
            {creating ? t("expense.category.creating") : t("expense.createCategory")}
          </Button>
        </Card>
      ) : null}

      <label className="flex max-w-xs flex-col gap-1 text-[length:var(--exits-text-sm)]">
        <span className="text-muted">{t("expense.filter.status")}</span>
        <select
          className="exits-select"
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          data-testid="expense-category-status-filter"
        >
          <option value="">{t("expense.filter.all")}</option>
          <option value="Active">{t("expense.category.active")}</option>
          <option value="Inactive">{t("expense.category.inactive")}</option>
        </select>
      </label>

      {query.isLoading ? <LoadingState label={t("expense.loadingCategories")} /> : null}
      {query.isError ? (
        <ErrorState title={t("expense.errorTitle")} detail={t("expense.categoriesLoadFailed")} />
      ) : null}
      {query.isSuccess && items.length === 0 ? (
        <EmptyState
          title={t("expense.categoriesEmpty")}
          detail={
            allowManage ? t("expense.categoriesEmptyDetail") : t("expense.categoriesEmptyReadonly")
          }
        />
      ) : null}

      <ul className="m-0 grid list-none gap-2 p-0" data-testid="expense-category-list">
        {items.map((category) => {
          const isActive = category.status === "Active";
          const isEditing = editing?.categoryId === category.categoryId;
          return (
            <li key={category.categoryId}>
              <Card
                className="flex flex-col gap-2 p-3 sm:flex-row sm:items-center sm:justify-between"
                data-testid={`expense-category-row-${category.categoryId}`}
              >
                <div className="min-w-0 flex-1">
                  {isEditing ? (
                    <input
                      className="w-full rounded-[var(--exits-radius-md)] border border-border bg-background px-3"
                      value={editName}
                      maxLength={EXPENSE_CATEGORY_NAME_MAX}
                      onChange={(e) => setEditName(e.target.value)}
                      data-testid="expense-category-edit-name"
                    />
                  ) : (
                    <p className="m-0 truncate font-semibold">{category.name}</p>
                  )}
                  <div className="mt-1">
                    <StatusChip tone={isActive ? "success" : "info"}>
                      {t(expenseCategoryStatusLabelKey(category.status))}
                    </StatusChip>
                  </div>
                </div>
                {allowManage ? (
                  <div className="flex flex-wrap gap-2">
                    {isEditing ? (
                      <>
                        <Button
                          type="button"
                          disabled={busyId === category.categoryId}
                          onClick={() => void onSaveEdit()}
                          data-testid="expense-category-save"
                        >
                          {t("expense.save")}
                        </Button>
                        <Button
                          type="button"
                          variant="outline"
                          onClick={() => setEditing(null)}
                        >
                          {t("expense.cancel")}
                        </Button>
                      </>
                    ) : (
                      <>
                        <Button
                          type="button"
                          variant="outline"
                          disabled={busyId === category.categoryId}
                          onClick={() => {
                            setEditing(category);
                            setEditName(category.name);
                          }}
                          data-testid={`expense-category-edit-${category.categoryId}`}
                        >
                          {t("expense.editCategory")}
                        </Button>
                        {isActive ? (
                          <Button
                            type="button"
                            variant="outline"
                            disabled={busyId === category.categoryId}
                            onClick={() => void onDeactivate(category)}
                            data-testid={`expense-category-deactivate-${category.categoryId}`}
                          >
                            {t("expense.deactivate")}
                          </Button>
                        ) : (
                          <Button
                            type="button"
                            variant="outline"
                            disabled={busyId === category.categoryId}
                            onClick={() => void onReactivate(category)}
                            data-testid={`expense-category-reactivate-${category.categoryId}`}
                          >
                            {t("expense.reactivate")}
                          </Button>
                        )}
                      </>
                    )}
                  </div>
                ) : null}
              </Card>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
