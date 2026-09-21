import type { ExitsSelectOption } from "@/components/exits/ExitsSelect";
import { ExitsMultiSelect } from "@/components/exits/ExitsMultiSelect";
import { cn } from "@/lib/cn";

export type ProductBrandOption = {
  brandId: string;
  name: string;
  /** Product count for this brand. `0` still renders normally. */
  count?: number;
};

export type ProductBrandMultiSelectProps = {
  brands: ReadonlyArray<ProductBrandOption>;
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
 * Canonical searchable brand multi-select for product selection surfaces.
 * Thin labeled shell over ExitsMultiSelect — no feature mode branching.
 */
export function ProductBrandMultiSelect({
  brands,
  selectedIds,
  onChange,
  placeholder,
  selectedCountLabel,
  clearAllLabel,
  label,
  selectAllLabel = "Select all",
  showSelectAll = true,
  searchPlaceholder = "Search brands",
  menuLabel,
  testId = "product-brand-multiselect",
  className,
  "aria-label": ariaLabel,
}: ProductBrandMultiSelectProps) {
  const options: ExitsSelectOption<string>[] = brands.map((brand) => ({
    value: brand.brandId,
    label: brand.name,
    count: brand.count,
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
