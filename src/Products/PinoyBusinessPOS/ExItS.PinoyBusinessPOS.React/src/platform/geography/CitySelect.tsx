import { ExitsOutlineInput, ExitsOutlineSelect } from "@/components/exits/ExitsOutlineField";
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
  outline = false,
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
  outline?: boolean;
}) {
  const areas = useAdministrativeAreas(countryCode);
  const cities = useCities(areaCode);
  const textMode = areas.isSuccess && areas.data.length === 0;
  if (textMode) {
    return outline ? (
      <ExitsOutlineInput
        label={label}
        value={cityName}
        required
        disabled={disabled}
        onChange={(next) => onChange("", next)}
        testId={testId}
      />
    ) : (
      <Input
        label={`${label} · ${requiredLabel}`}
        value={cityName}
        disabled={disabled}
        onChange={(event) => onChange("", event.target.value)}
        data-testid={testId}
      />
    );
  }

  if (outline) {
    return (
      <ExitsOutlineSelect
        label={label}
        required
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
