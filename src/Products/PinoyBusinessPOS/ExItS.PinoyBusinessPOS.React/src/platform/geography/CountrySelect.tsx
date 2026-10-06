import { useMemo } from "react";
import { ExitsSelect } from "@/components/exits/ExitsSelect";
import { useCountries } from "@/platform/geography/useGeography";

export type CountrySelectProps = {
  value: string;
  onChange: (countryCode: string, countryName: string) => void;
  disabled?: boolean;
  /** Shown first. Defaults to the Philippines. */
  preferredCountryCodes?: readonly string[];
  /** Reserved for a later flag treatment. The shared component owns the change. */
  showFlag?: boolean;
  /** Reserved for a later dial-code treatment. */
  showDialCode?: boolean;
  /** Reserved so localized names can be requested without changing callers. */
  locale?: string;
  searchable?: boolean;
  testId?: string;
  placeholder?: string;
  searchPlaceholder?: string;
  emptyLabel?: string;
};

function flagPrefix(code: string) {
  if (!/^[A-Z]{2}$/.test(code)) {
    return "";
  }
  const points = [...code].map((character) => 0x1f1e6 + character.charCodeAt(0) - 65);
  return `${String.fromCodePoint(...points)} `;
}

export function CountrySelect({
  value,
  onChange,
  disabled = false,
  preferredCountryCodes = ["PH"],
  showFlag = false,
  searchable = true,
  testId = "country-select",
  placeholder = "Select",
  searchPlaceholder = "Search country",
  emptyLabel = "No matches",
}: CountrySelectProps) {
  const countries = useCountries();
  const options = useMemo(() => {
    const rows = countries.data ?? [];
    const preferred = new Set(preferredCountryCodes.map((code) => code.toUpperCase()));
    const sorted = [...rows].sort((left, right) => {
      const leftPreferred = preferred.has(left.code.toUpperCase()) ? 0 : 1;
      const rightPreferred = preferred.has(right.code.toUpperCase()) ? 0 : 1;
      if (leftPreferred !== rightPreferred) {
        return leftPreferred - rightPreferred;
      }
      return left.name.localeCompare(right.name);
    });
    return sorted.map((country) => ({
      value: country.code,
      label: `${showFlag ? flagPrefix(country.code) : ""}${country.name}`,
    }));
  }, [countries.data, preferredCountryCodes, showFlag]);

  return (
    <ExitsSelect
      value={value}
      options={options}
      disabled={disabled}
      searchable={searchable}
      searchPlaceholder={searchPlaceholder}
      emptyLabel={emptyLabel}
      placeholder={placeholder}
      testId={testId}
      dataset={{ "country-code": value }}
      onChange={(countryCode) => {
        const country = countries.data?.find((item) => item.code === countryCode);
        onChange(countryCode, country?.name ?? countryCode);
      }}
    />
  );
}
