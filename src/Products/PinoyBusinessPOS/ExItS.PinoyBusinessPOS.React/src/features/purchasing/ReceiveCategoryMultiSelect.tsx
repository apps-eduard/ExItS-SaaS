import {
  ProductCategoryMultiSelect,
  type ProductCategoryOption,
  type ProductCategoryMultiSelectProps,
} from "@/components/exits/ProductCategoryMultiSelect";

export type CategoryOption = ProductCategoryOption;

type ReceiveCategoryMultiSelectProps = Omit<
  ProductCategoryMultiSelectProps,
  "clearAllLabel" | "showSelectAll"
> & {
  selectAllLabel: string;
  deselectAllLabel: string;
};

/**
 * Receive Stock labeled category filter — thin adapter over ProductCategoryMultiSelect.
 */
export function ReceiveCategoryMultiSelect({
  deselectAllLabel,
  selectAllLabel,
  testId = "direct-category-multiselect",
  ...rest
}: ReceiveCategoryMultiSelectProps) {
  return (
    <ProductCategoryMultiSelect
      {...rest}
      testId={testId}
      showSelectAll
      selectAllLabel={selectAllLabel}
      clearAllLabel={deselectAllLabel}
    />
  );
}
