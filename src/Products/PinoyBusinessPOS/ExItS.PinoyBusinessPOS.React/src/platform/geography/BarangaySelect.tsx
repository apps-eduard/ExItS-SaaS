import { Input } from "@/components/ui/input";
import { ExitsSelect } from "@/components/exits/ExitsSelect";
import { useBarangays } from "@/platform/geography/useGeography";

export function BarangaySelect({
  cityCode,
  barangay,
  onChange,
  label,
  requiredLabel,
  disabled = false,
  testId = "barangay-select",
  placeholder = "Select",
  searchPlaceholder = "Search barangay",
  emptyLabel = "No matches",
}: {
  cityCode: string;
  barangay: string;
  onChange: (barangay: string) => void;
  label: string;
  requiredLabel: string;
  disabled?: boolean;
  testId?: string;
  placeholder?: string;
  searchPlaceholder?: string;
  emptyLabel?: string;
}) {
  const barangays = useBarangays(cityCode);
  const rows = barangays.data ?? [];
  const textMode = barangays.isSuccess && rows.length === 0 && cityCode.trim().length > 0;
  if (textMode) {
    return (
      <Input
        label={`${label} · ${requiredLabel}`}
        value={barangay}
        disabled={disabled || cityCode.trim().length === 0}
        onChange={(event) => onChange(event.target.value)}
        data-testid={testId}
      />
    );
  }

  const options = rows.map((row) => ({ value: row.code, label: row.name }));
  const selected = options.find((option) => option.label.localeCompare(barangay, undefined, { sensitivity: "accent" }) === 0);
  if (barangay.trim() && !selected) {
    options.unshift({ value: barangay, label: barangay });
  }

  return (
    <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
      <span className="font-semibold">{label} · {requiredLabel}</span>
      <ExitsSelect
        value={selected?.value ?? (barangay.trim() && !selected ? barangay : "")}
        options={options}
        disabled={disabled || cityCode.trim().length === 0}
        searchable
        searchPlaceholder={searchPlaceholder}
        emptyLabel={emptyLabel}
        placeholder={placeholder}
        testId={testId}
        onChange={(next) => {
          const match = rows.find((row) => row.code === next);
          onChange(match?.name ?? next);
        }}
      />
    </label>
  );
}
