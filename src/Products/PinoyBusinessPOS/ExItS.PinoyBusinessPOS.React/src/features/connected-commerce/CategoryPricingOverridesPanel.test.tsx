import { describe, expect, it, vi } from "vitest";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ComponentProps } from "react";
import { CategoryPricingOverridesPanel } from "@/features/connected-commerce/CategoryPricingOverridesPanel";
import {
  categoriesAvailableForPricingOverride,
  filterCategoryPricingOverrides,
  formatCategoryPricingDiscountLabel,
} from "@/features/connected-commerce/category-pricing-overrides";
import { I18nProvider } from "@/i18n/I18nProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";

const CAT_A = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const CAT_B = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const CAT_C = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const CAT_D = "dddddddd-dddd-dddd-dddd-dddddddddddd";

const categories = [
  { categoryId: CAT_A, name: "Beverages" },
  { categoryId: CAT_B, name: "Baked Goods" },
  { categoryId: CAT_C, name: "Electronics" },
  { categoryId: CAT_D, name: "Condiments" },
];

const sampleRules = [
  { categoryId: CAT_A, discountPercent: 5 },
  { categoryId: CAT_B, discountPercent: 12.5 },
  { categoryId: CAT_C, discountPercent: 0 },
];

function renderPanel(
  props: Partial<ComponentProps<typeof CategoryPricingOverridesPanel>> = {},
) {
  const onChange = props.onChange ?? vi.fn();
  render(
    <PreferencesProvider>
      <I18nProvider>
        <CategoryPricingOverridesPanel
          rules={props.rules ?? sampleRules}
          categories={props.categories ?? categories}
          canEdit={props.canEdit ?? true}
          onChange={onChange}
        />
      </I18nProvider>
    </PreferencesProvider>,
  );
  return { onChange };
}

describe("category-pricing-overrides helpers", () => {
  it("formats discount labels and filters by search", () => {
    expect(formatCategoryPricingDiscountLabel({ discountPercent: 12.5 })).toBe("12.5%");
    expect(formatCategoryPricingDiscountLabel({ discountPercent: 0 })).toBe("0%");

    const nameById = new Map(categories.map((c) => [c.categoryId, c.name]));
    const filtered = filterCategoryPricingOverrides(sampleRules, {
      search: "bake",
      categoryNameById: nameById,
    });
    expect(filtered).toHaveLength(1);
    expect(filtered[0]?.categoryId).toBe(CAT_B);

    const available = categoriesAvailableForPricingOverride(categories, sampleRules);
    expect(available.map((c) => c.categoryId)).toEqual([CAT_D]);
  });
});

describe("CategoryPricingOverridesPanel", () => {
  it("renders configured overrides in desktop table and hides non-override categories", () => {
    renderPanel();
    const desktop = screen.getByTestId("category-pricing-overrides-desktop");
    expect(desktop).toBeInTheDocument();
    expect(within(desktop).getByText("Beverages")).toBeInTheDocument();
    expect(within(desktop).getByText("Electronics")).toBeInTheDocument();
    expect(within(desktop).queryByText("Condiments")).not.toBeInTheDocument();
    expect(within(desktop).getByText("5%")).toBeInTheDocument();
    expect(within(desktop).getByText("12.5%")).toBeInTheDocument();
  });

  it("shows empty state when no overrides", () => {
    renderPanel({ rules: [] });
    expect(screen.getByTestId("category-pricing-overrides-empty")).toBeInTheDocument();
    expect(screen.getByTestId("category-pricing-overrides-add")).toBeInTheDocument();
  });

  it("opens add editor with multi-select and blocks already configured categories", async () => {
    const user = userEvent.setup();
    renderPanel();
    await user.click(screen.getByTestId("category-pricing-overrides-add"));
    expect(screen.getByTestId("category-pricing-override-editor")).toBeInTheDocument();
    expect(screen.getByTestId("category-pricing-override-category")).toBeInTheDocument();
    await user.click(screen.getByTestId("category-pricing-override-category"));
    expect(screen.getByTestId(`category-pricing-override-category-option-${CAT_D}`)).toBeInTheDocument();
    expect(
      screen.queryByTestId(`category-pricing-override-category-option-${CAT_A}`),
    ).not.toBeInTheDocument();
  });

  it("adds override for selected categories", async () => {
    const user = userEvent.setup();
    const { onChange } = renderPanel();
    await user.click(screen.getByTestId("category-pricing-overrides-add"));
    await user.click(screen.getByTestId("category-pricing-override-category"));
    await user.click(screen.getByTestId(`category-pricing-override-category-option-${CAT_D}`));
    await user.clear(screen.getByTestId("category-pricing-override-discount"));
    await user.type(screen.getByTestId("category-pricing-override-discount"), "8");
    await user.click(screen.getByTestId("category-pricing-override-save"));
    expect(onChange).toHaveBeenCalledWith(
      expect.arrayContaining([
        expect.objectContaining({ categoryId: CAT_D, discountPercent: 8 }),
      ]),
    );
  });

  it("edit opens existing values", async () => {
    const user = userEvent.setup();
    renderPanel();
    await user.click(screen.getByTestId(`category-pricing-override-edit-${CAT_B}`));
    expect(screen.getByTestId("category-pricing-override-editor")).toBeInTheDocument();
    expect(screen.getByTestId("category-pricing-override-category-readonly")).toHaveTextContent(
      "Baked Goods",
    );
    expect(screen.getByTestId("category-pricing-override-discount")).toHaveValue(12.5);
  });

  it("remove override confirms fallback to organization default", async () => {
    const user = userEvent.setup();
    const { onChange } = renderPanel();
    await user.click(screen.getByTestId(`category-pricing-override-remove-${CAT_A}`));
    expect(screen.getByTestId("category-pricing-override-remove-confirm")).toBeInTheDocument();
    expect(screen.getByText(/organization default B2B discount/i)).toBeInTheDocument();
    await user.click(
      within(screen.getByTestId("category-pricing-override-remove-confirm")).getByRole("button", {
        name: /Remove override/i,
      }),
    );
    expect(onChange).toHaveBeenCalledWith(
      expect.not.arrayContaining([expect.objectContaining({ categoryId: CAT_A })]),
    );
  });
});
