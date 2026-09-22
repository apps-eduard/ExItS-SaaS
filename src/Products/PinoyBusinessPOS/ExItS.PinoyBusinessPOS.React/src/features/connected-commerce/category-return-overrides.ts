export type CategoryReturnPolicyMode = "UseDefault" | "Custom" | "NonReturnable";

export type CategoryReturnOverrideRule = {
  categoryId: string;
  mode: string;
  returnsAllowed?: boolean | null;
  returnWindowDays?: number | null;
};

export type CategoryReturnPolicyFilter = "all" | "UseDefault" | "Custom" | "NonReturnable";

export function normalizeCategoryReturnMode(mode: string | null | undefined): CategoryReturnPolicyMode {
  if (mode === "Custom" || mode === "NonReturnable" || mode === "UseDefault") {
    return mode;
  }
  return "UseDefault";
}

export function formatCategoryReturnPolicyLabel(
  rule: Pick<CategoryReturnOverrideRule, "mode" | "returnWindowDays">,
  labels: {
    useDefault: string;
    custom: string;
    nonReturnable: string;
    days: (n: number) => string;
    noTimeLimit: string;
  },
): string {
  const mode = normalizeCategoryReturnMode(rule.mode);
  if (mode === "NonReturnable") {
    return labels.nonReturnable;
  }
  if (mode === "UseDefault") {
    return labels.useDefault;
  }
  if (rule.returnWindowDays == null) {
    return `${labels.custom} · ${labels.noTimeLimit}`;
  }
  return `${labels.custom} · ${labels.days(rule.returnWindowDays)}`;
}

export function formatCategoryReturnWindowLabel(
  rule: Pick<CategoryReturnOverrideRule, "mode" | "returnWindowDays">,
  labels: { days: (n: number) => string; noTimeLimit: string; dash: string },
): string {
  const mode = normalizeCategoryReturnMode(rule.mode);
  if (mode !== "Custom") {
    return labels.dash;
  }
  if (rule.returnWindowDays == null) {
    return labels.noTimeLimit;
  }
  return labels.days(rule.returnWindowDays);
}

export function filterCategoryReturnOverrides<T extends CategoryReturnOverrideRule>(
  rules: readonly T[],
  options: {
    search: string;
    policyFilter: CategoryReturnPolicyFilter;
    categoryNameById: ReadonlyMap<string, string>;
  },
): T[] {
  const q = options.search.trim().toLowerCase();
  return rules.filter((rule) => {
    const mode = normalizeCategoryReturnMode(rule.mode);
    if (options.policyFilter !== "all" && mode !== options.policyFilter) {
      return false;
    }
    if (!q) {
      return true;
    }
    const name = (options.categoryNameById.get(rule.categoryId) ?? rule.categoryId).toLowerCase();
    return name.includes(q);
  });
}

export function buildCategoryReturnOverrideDraft(
  categoryId: string,
  mode: CategoryReturnPolicyMode,
  returnWindowDays: number | null,
): CategoryReturnOverrideRule {
  if (mode === "Custom") {
    return {
      categoryId,
      mode: "Custom",
      returnsAllowed: true,
      returnWindowDays,
    };
  }
  return {
    categoryId,
    mode,
    returnsAllowed: null,
    returnWindowDays: null,
  };
}

export function categoriesAvailableForReturnOverride(
  categories: readonly { categoryId: string; name: string }[],
  rules: readonly CategoryReturnOverrideRule[],
): { categoryId: string; name: string }[] {
  const configured = new Set(rules.map((r) => r.categoryId));
  return categories.filter((c) => !configured.has(c.categoryId));
}
