import type { ExitsSelectOption } from "@/components/exits/ExitsSelect";
import { ExitsMultiSelect } from "@/components/exits/ExitsMultiSelect";
import { cn } from "@/lib/cn";

export type ProductCategoryOption = {
  categoryId: string;
  name: string;
  /** Product count for this category. `0` still renders normally. */
  count?: number;
};

export type ProductCategoryMultiSelectProps = {
  categories: ReadonlyArray<ProductCategoryOption>;
  selectedIds: ReadonlyArray<string>;
  onChange: (nextIds: string[]) => void;
  placeholder: string;
  selectedCountLabel: (count: number) => string;
  clearAllLabel: string;
  /** Optional visible field label. */
  label?: string;
  selectAllLabel?: string;
  /** When false, hide Select all. Default true. */
  showSelectAll?: boolean;
  searchPlaceholder?: string;
  menuLabel?: string;
  testId?: string;
  className?: string;
  "aria-label"?: string;
};

/**
 * Canonical searchable category multi-select for product selection surfaces.
 * Thin labeled shell over ExitsMultiSelect — no feature mode branching.
 */
export function ProductCategoryMultiSelect({
  categories,
  selectedIds,
  onChange,
  placeholder,
  selectedCountLabel,
  clearAllLabel,
  label,
  selectAllLabel = "Select all",
  showSelectAll = true,
  searchPlaceholder = "Search categories",
  menuLabel,
  testId = "product-category-multiselect",
  className,
  "aria-label": ariaLabel,
}: ProductCategoryMultiSelectProps) {
  const options: ExitsSelectOption<string>[] = categories.map((category) => ({
    value: category.categoryId,
    label: category.name,
    count: category.count,
  }));

  const control = (
    <ExitsMultiSelect
      searchable
      showSelectAll={showSelectAll}
      triggerMode="count"
      value={selectedIds}
      options={options}
      onChange={onChange}
      placeholder={placeholder}
      menuLabel={menuLabel ?? label ?? ariaLabel}
      searchPlaceholder={searchPlaceholder}
      selectAllLabel={selectAllLabel}
      clearAllLabel={clearAllLabel}
      selectedCountLabel={selectedCountLabel}
      aria-label={ariaLabel ?? label}
      testId={testId}
      className={label ? undefined : className}
    />
  );

  if (!label) {
    return control;
  }

  return (
    <label
      className={cn("product-selection__field receive-stock-field", className)}
      data-testid={`${testId}-field`}
    >
      <span className="product-selection__field-label exits-type-label">{label}</span>
      {control}
    </label>
  );
}
