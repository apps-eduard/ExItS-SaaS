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

/** Lets a percent field be cleared while typing. Empty stays empty. */
export function normalizeDiscountPercentTyping(raw: string): string {
  const cleaned = raw.replace(/[^\d.]/g, "");
  if (cleaned === "" || cleaned === ".") {
    return cleaned === "." ? "0." : "";
  }
  const dot = cleaned.indexOf(".");
  const whole = dot === -1 ? cleaned : cleaned.slice(0, dot);
  const fraction = dot === -1 ? "" : cleaned.slice(dot + 1).replace(/\./g, "").slice(0, 2);
  const joined = dot === -1 ? whole : `${whole}.${fraction}`;
  const value = Number(joined);
  if (!Number.isFinite(value)) {
    return "";
  }
  if (value > 100) {
    return "100";
  }
  return joined;
}

/** Empty or unfinished input saves as 0. */
export function parseDiscountPercentInput(raw: string): number {
  const normalized = normalizeDiscountPercentTyping(raw);
  if (normalized === "" || normalized === "." || normalized === "0.") {
    return 0;
  }
  const value = Number(normalized);
  return Number.isFinite(value) ? Math.min(100, Math.max(0, value)) : 0;
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
