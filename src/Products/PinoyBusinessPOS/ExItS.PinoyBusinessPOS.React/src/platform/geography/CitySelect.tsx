import { Input } from "@/components/ui/input";
import { ExitsSelect } from "@/components/exits/ExitsSelect";
import { useAdministrativeAreas, useCities } from "@/platform/geography/useGeography";

export function CitySelect({
  countryCode,
  areaCode,
  cityCode,
  cityName,
  onChange,
  label,
  requiredLabel,
  disabled = false,
  testId = "city-select",
  placeholder = "Select",
  searchPlaceholder = "Search",
  emptyLabel = "No matches",
}: {
  countryCode: string;
  areaCode: string;
  cityCode: string;
  cityName: string;
  onChange: (cityCode: string, cityName: string) => void;
  label: string;
  requiredLabel: string;
  disabled?: boolean;
  testId?: string;
  placeholder?: string;
  searchPlaceholder?: string;
  emptyLabel?: string;
}) {
  const areas = useAdministrativeAreas(countryCode);
  const cities = useCities(areaCode);
  const textMode = areas.isSuccess && areas.data.length === 0;
  if (textMode) {
    return (
      <Input
        label={`${label} · ${requiredLabel}`}
        value={cityName}
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
        value={cityCode}
        options={(cities.data ?? []).map((city) => ({ value: city.id, label: city.name }))}
        disabled={disabled || areaCode.trim().length === 0}
        searchable
        searchPlaceholder={searchPlaceholder}
        emptyLabel={emptyLabel}
        placeholder={placeholder}
        testId={testId}
        onChange={(nextCode) => {
          const city = cities.data?.find((item) => item.id === nextCode);
          onChange(nextCode, city?.name ?? "");
        }}
      />
    </label>
  );
}
