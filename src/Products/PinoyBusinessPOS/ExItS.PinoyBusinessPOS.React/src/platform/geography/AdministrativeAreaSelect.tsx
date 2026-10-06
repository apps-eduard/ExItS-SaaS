import { Input } from "@/components/ui/input";
import { ExitsSelect } from "@/components/exits/ExitsSelect";
import { useAdministrativeAreas } from "@/platform/geography/useGeography";

export function AdministrativeAreaSelect({
  countryCode,
  areaCode,
  areaName,
  onChange,
  label,
  requiredLabel,
  disabled = false,
  testId = "administrative-area-select",
  placeholder = "Select",
  searchPlaceholder = "Search",
  emptyLabel = "No matches",
}: {
  countryCode: string;
  areaCode: string;
  areaName: string;
  onChange: (areaCode: string, areaName: string) => void;
  label: string;
  requiredLabel: string;
  disabled?: boolean;
  testId?: string;
  placeholder?: string;
  searchPlaceholder?: string;
  emptyLabel?: string;
}) {
  const areas = useAdministrativeAreas(countryCode);
  const textMode = areas.isSuccess && areas.data.length === 0;
  if (textMode) {
    return (
      <Input
        label={`${label} · ${requiredLabel}`}
        value={areaName}
        disabled={disabled}
        onChange={(event) => onChange("", event.target.value)}
        data-testid={testId}
      />
    );
  }

  return (
    <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
      <span className="font-semibold">{label} · {requiredLabel}</span>
      <ExitsSelect
        value={areaCode}
        options={(areas.data ?? []).map((area) => ({ value: area.id, label: area.name }))}
        disabled={disabled || countryCode.trim().length === 0}
        searchable
        searchPlaceholder={searchPlaceholder}
        emptyLabel={emptyLabel}
        placeholder={placeholder}
        testId={testId}
        onChange={(nextCode) => {
          const area = areas.data?.find((item) => item.id === nextCode);
          onChange(nextCode, area?.name ?? "");
        }}
      />
    </label>
  );
}
