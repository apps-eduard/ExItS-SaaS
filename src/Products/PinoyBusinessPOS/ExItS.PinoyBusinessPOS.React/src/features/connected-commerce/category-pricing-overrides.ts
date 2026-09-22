export type CategoryPricingOverrideRule = {
  categoryId: string;
  discountPercent: number;
};

export type CategoryPricingOption = {
  categoryId: string;
  name: string;
};

export function categoriesAvailableForPricingOverride<T extends CategoryPricingOption>(
  categories: ReadonlyArray<T>,
  rules: ReadonlyArray<Pick<CategoryPricingOverrideRule, "categoryId">>,
): T[] {
  const taken = new Set(rules.map((r) => r.categoryId));
  return categories.filter((c) => !taken.has(c.categoryId));
}

export function filterCategoryPricingOverrides<T extends CategoryPricingOverrideRule>(
  rules: ReadonlyArray<T>,
  options: {
    search: string;
    categoryNameById: ReadonlyMap<string, string>;
  },
): T[] {
  const q = options.search.trim().toLocaleLowerCase();
  if (!q) {
    return [...rules];
  }
  return rules.filter((rule) => {
    const name = options.categoryNameById.get(rule.categoryId) ?? rule.categoryId;
    return name.toLocaleLowerCase().includes(q);
  });
}

export function buildCategoryPricingOverrideDraft(
  categoryId: string,
  discountPercent: number,
): CategoryPricingOverrideRule {
  const clamped = Number.isFinite(discountPercent)
    ? Math.min(100, Math.max(0, discountPercent))
    : 0;
  return { categoryId, discountPercent: clamped };
}

export function formatCategoryPricingDiscountLabel(
  rule: Pick<CategoryPricingOverrideRule, "discountPercent">,
): string {
  const n = Number(rule.discountPercent);
  if (!Number.isFinite(n)) {
    return "0%";
  }
  const rounded = Math.round(n * 100) / 100;
  return `${rounded}%`;
}
