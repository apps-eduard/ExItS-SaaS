import { describe, expect, it, vi } from "vitest";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ComponentProps } from "react";
import { CategoryReturnOverridesPanel } from "@/features/connected-commerce/CategoryReturnOverridesPanel";
import {
  categoriesAvailableForReturnOverride,
  filterCategoryReturnOverrides,
  formatCategoryReturnPolicyLabel,
  formatCategoryReturnWindowLabel,
} from "@/features/connected-commerce/category-return-overrides";
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
  { categoryId: CAT_A, mode: "NonReturnable", returnsAllowed: null, returnWindowDays: null },
  { categoryId: CAT_B, mode: "NonReturnable", returnsAllowed: null, returnWindowDays: null },
  { categoryId: CAT_C, mode: "Custom", returnsAllowed: true, returnWindowDays: 14 },
];

function renderPanel(
  props: Partial<ComponentProps<typeof CategoryReturnOverridesPanel>> = {},
) {
  const onChange = props.onChange ?? vi.fn();
  render(
    <PreferencesProvider>
      <I18nProvider>
        <CategoryReturnOverridesPanel
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

describe("category-return-overrides helpers", () => {
  it("formats policy labels for scan-friendly table cells", () => {
    const labels = {
      useDefault: "Organization default",
      custom: "Custom",
      nonReturnable: "Non-returnable",
      days: (n: number) => `${n} days`,
      noTimeLimit: "No time limit",
      dash: "—",
    };
    expect(formatCategoryReturnPolicyLabel({ mode: "NonReturnable" }, labels)).toBe(
      "Non-returnable",
    );
    expect(
      formatCategoryReturnPolicyLabel({ mode: "Custom", returnWindowDays: 7 }, labels),
    ).toBe("Custom · 7 days");
    expect(formatCategoryReturnPolicyLabel({ mode: "Custom", returnWindowDays: null }, labels)).toBe(
      "Custom · No time limit",
    );
    expect(formatCategoryReturnWindowLabel({ mode: "NonReturnable" }, labels)).toBe("—");
  });

  it("filters by search and policy without listing non-override categories", () => {
    const nameById = new Map(categories.map((c) => [c.categoryId, c.name]));
    const filtered = filterCategoryReturnOverrides(sampleRules, {
      search: "bake",
      policyFilter: "all",
      categoryNameById: nameById,
    });
    expect(filtered).toHaveLength(1);
    expect(filtered[0]?.categoryId).toBe(CAT_B);

    const available = categoriesAvailableForReturnOverride(categories, sampleRules);
    expect(available.map((c) => c.categoryId)).toEqual([CAT_D]);
  });
});

describe("CategoryReturnOverridesPanel", () => {
  it("renders configured overrides in desktop table and hides non-override categories", () => {
    renderPanel();
    const desktop = screen.getByTestId("category-return-overrides-desktop");
    expect(desktop).toBeInTheDocument();
    expect(within(desktop).getByText("Beverages")).toBeInTheDocument();
    expect(within(desktop).getByText("Electronics")).toBeInTheDocument();
    expect(within(desktop).queryByText("Condiments")).not.toBeInTheDocument();
    expect(within(desktop).getAllByText("Non-returnable").length).toBeGreaterThan(0);
    expect(within(desktop).getByText("Custom · 14 days")).toBeInTheDocument();
  });

  it("shows empty state when there are no overrides", () => {
    renderPanel({ rules: [] });
    expect(screen.getByTestId("category-return-overrides-empty")).toBeInTheDocument();
    expect(screen.getByText(/No category overrides/i)).toBeInTheDocument();
    expect(screen.getByTestId("category-return-overrides-add")).toBeInTheDocument();
    expect(screen.queryByTestId("category-return-overrides-table")).not.toBeInTheDocument();
  });

  it("filters rows by search", async () => {
    const user = userEvent.setup();
    renderPanel();
    await user.type(screen.getByTestId("category-return-overrides-search"), "elect");
    const desktop = screen.getByTestId("category-return-overrides-desktop");
    expect(within(desktop).getByText("Electronics")).toBeInTheDocument();
    expect(within(desktop).queryByText("Beverages")).not.toBeInTheDocument();
  });

  it("opens add editor and blocks duplicate configured categories", async () => {
    const user = userEvent.setup();
    const { onChange } = renderPanel();
    await user.click(screen.getByTestId("category-return-overrides-add"));
    expect(screen.getByTestId("category-return-override-editor")).toBeInTheDocument();

    const select = screen.getByTestId("category-return-override-category");
    const options = within(select).getAllByRole("option");
    expect(options.map((o) => o.textContent)).toEqual(["Condiments"]);

    await user.click(screen.getByTestId("category-return-override-mode-Custom"));
    expect(screen.getByTestId("category-return-override-window-days")).toBeInTheDocument();
    await user.clear(screen.getByTestId("category-return-override-window-days"));
    await user.type(screen.getByTestId("category-return-override-window-days"), "7");
    await user.click(screen.getByTestId("category-return-override-save"));

    expect(onChange).toHaveBeenCalledWith(
      expect.arrayContaining([
        expect.objectContaining({
          categoryId: CAT_D,
          mode: "Custom",
          returnWindowDays: 7,
        }),
      ]),
    );
  });

  it("edit opens existing values and Non-returnable hides window field", async () => {
    const user = userEvent.setup();
    renderPanel();
    await user.click(screen.getByTestId(`category-return-override-edit-${CAT_C}`));
    expect(screen.getByTestId("category-return-override-editor")).toBeInTheDocument();
    expect(screen.getByTestId("category-return-override-category-readonly")).toHaveTextContent(
      "Electronics",
    );
    expect(screen.getByTestId("category-return-override-mode-Custom")).toBeChecked();
    expect(screen.getByTestId("category-return-override-window-days")).toHaveValue(14);

    await user.click(screen.getByTestId("category-return-override-mode-NonReturnable"));
    expect(screen.queryByTestId("category-return-override-window-days")).not.toBeInTheDocument();
  });

  it("remove override confirms fallback to organization policy", async () => {
    const user = userEvent.setup();
    const { onChange } = renderPanel();
    await user.click(screen.getByTestId(`category-return-override-more-${CAT_A}`));
    await user.click(screen.getByRole("menuitem", { name: /Remove override/i }));
    expect(screen.getByTestId("category-return-override-remove-confirm")).toBeInTheDocument();
    expect(screen.getByText(/organization return policy/i)).toBeInTheDocument();
    await user.click(
      within(screen.getByTestId("category-return-override-remove-confirm")).getByRole("button", {
        name: /Remove override/i,
      }),
    );
    expect(onChange).toHaveBeenCalledWith(
      expect.not.arrayContaining([expect.objectContaining({ categoryId: CAT_A })]),
    );
  });

  it("renders mobile list markup alongside desktop table (responsive CSS hides one)", () => {
    renderPanel();
    expect(screen.getByTestId("category-return-overrides-desktop")).toBeInTheDocument();
    expect(screen.getByTestId("category-return-overrides-mobile")).toBeInTheDocument();
    expect(screen.getByTestId(`category-return-override-mobile-${CAT_A}`)).toBeInTheDocument();
  });
});
