import { useEffect } from "react";
import { ExitsSelect } from "@/components/exits/ExitsSelect";
import { BarangaySelect } from "@/platform/geography/BarangaySelect";
import { Input } from "@/components/ui/input";
import { useI18n } from "@/i18n/I18nProvider";
import { AdministrativeAreaSelect } from "@/platform/geography/AdministrativeAreaSelect";
import { CitySelect } from "@/platform/geography/CitySelect";
import { CountrySelect } from "@/platform/geography/CountrySelect";
import { PostalCodeInput } from "@/platform/geography/PostalCodeInput";
import type { SharedAddressValue } from "@/platform/geography/types";
import { useAdministrativeAreas, useCities, useCountries, useCountryAddressConfig } from "@/platform/geography/useGeography";
import { countryCodeForName } from "@/platform/geography/geography-client";

function sameName(left: string, right: string) {
  return left.trim().localeCompare(right.trim(), undefined, { sensitivity: "accent" }) === 0;
}

export function AddressForm({
  value,
  onChange,
  showAddressType = true,
  showPrimary = true,
  allowMultiple = false,
  requireAddress = false,
  testIdPrefix = "address",
}: {
  value: SharedAddressValue;
  onChange: (next: SharedAddressValue) => void;
  showAddressType?: boolean;
  showPrimary?: boolean;
  allowMultiple?: boolean;
  requireAddress?: boolean;
  testIdPrefix?: string;
}) {
  const { t } = useI18n();
  const countries = useCountries();
  const configQuery = useCountryAddressConfig(value.countryCode);
  const areas = useAdministrativeAreas(value.countryCode);
  const cities = useCities(value.administrativeAreaCode);
  const config = configQuery.data;
  const required = t("personal.profile.required");
  const optional = t("personal.profile.optional");

  useEffect(() => {
    if (value.countryCode || !value.countryName || !countries.data) {
      return;
    }
    const code = countryCodeForName(countries.data, value.countryName);
    if (!code) {
      return;
    }
    const country = countries.data.find((item) => item.code === code);
    onChange({ ...value, countryCode: code, countryName: country?.name ?? value.countryName });
  }, [countries.data, onChange, value]);

  useEffect(() => {
    if (!value.countryCode || value.administrativeAreaCode || !value.administrativeAreaName || !areas.data?.length) {
      return;
    }
    const match = areas.data.find((area) => sameName(area.name, value.administrativeAreaName));
    if (!match) {
      return;
    }
    onChange({ ...value, administrativeAreaCode: match.id, administrativeAreaName: match.name });
  }, [areas.data, onChange, value]);

  useEffect(() => {
    if (!value.administrativeAreaCode || value.cityCode || !value.cityName || !cities.data?.length) {
      return;
    }
    const match = cities.data.find((city) => sameName(city.name, value.cityName));
    if (!match) {
      return;
    }
    onChange({ ...value, cityCode: match.id, cityName: match.name });
  }, [cities.data, onChange, value]);

  const areaLabel = config?.administrativeAreaLabel ?? t("personal.profile.province");
  const cityLabel = config?.cityLabel ?? t("personal.profile.city");
  const postalLabel = config?.postalCodeLabel ?? t("personal.profile.postal");
  const postalRequirement = config?.requiresPostalCode ? required : optional;

  return (
    <div
      className="geography-address-form"
      data-testid={`${testIdPrefix}-form`}
      data-allow-multiple={allowMultiple ? "true" : "false"}
      data-require-address={requireAddress ? "true" : "false"}
    >
      {showAddressType ? (
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
          <span className="font-semibold">{t("personal.profile.addressType")}</span>
          <ExitsSelect
            value={value.addressType}
            options={[
              { value: "Home", label: t("personal.profile.typeHome") },
              { value: "Office", label: t("personal.profile.typeOffice") },
              { value: "Other", label: t("personal.profile.typeOther") },
            ]}
            onChange={(addressType) => onChange({ ...value, addressType })}
            testId={`${testIdPrefix}-type`}
          />
        </label>
      ) : null}
      <div className="geography-address-form__place">
      <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
        <span className="font-semibold">{t("personal.profile.country")} · {required}</span>
        <CountrySelect
          value={value.countryCode}
          searchable
          searchPlaceholder={t("personal.profile.searchCountry")}
          emptyLabel={t("personal.profile.noMatches")}
          placeholder={t("personal.profile.selectPlaceholder")}
          testId={`${testIdPrefix}-country`}
          onChange={(countryCode, countryName) => onChange({
            ...value,
            countryCode,
            countryName,
            administrativeAreaCode: "",
            administrativeAreaName: "",
            cityCode: "",
            cityName: "",
            barangay: "",
          })}
        />
      </label>
      <AdministrativeAreaSelect
        countryCode={value.countryCode}
        areaCode={value.administrativeAreaCode}
        areaName={value.administrativeAreaName}
        label={areaLabel}
        requiredLabel={required}
        placeholder={t("personal.profile.selectPlaceholder")}
        searchPlaceholder={t("personal.profile.searchProvince")}
        emptyLabel={t("personal.profile.noMatches")}
        testId={`${testIdPrefix}-province`}
        onChange={(administrativeAreaCode, administrativeAreaName) => onChange({
          ...value,
          administrativeAreaCode,
          administrativeAreaName,
          cityCode: "",
          cityName: "",
          barangay: "",
        })}
      />
      <CitySelect
        countryCode={value.countryCode}
        areaCode={value.administrativeAreaCode}
        cityCode={value.cityCode}
        cityName={value.cityName}
        label={cityLabel}
        requiredLabel={required}
        placeholder={t("personal.profile.selectPlaceholder")}
        searchPlaceholder={t("personal.profile.searchCity")}
        emptyLabel={t("personal.profile.noMatches")}
        testId={`${testIdPrefix}-city`}
        onChange={(cityCode, cityName) => onChange({
          ...value,
          cityCode,
          cityName,
          barangay: "",
        })}
      />
      </div>
      <div className="geography-address-form__lines">
        {config?.supportsBarangay ? (
          <BarangaySelect
            cityCode={value.cityCode}
            barangay={value.barangay}
            label={t("personal.profile.barangay")}
            requiredLabel={required}
            placeholder={t("personal.profile.selectPlaceholder")}
            searchPlaceholder={t("personal.profile.searchBarangay")}
            emptyLabel={t("personal.profile.noMatches")}
            testId={`${testIdPrefix}-barangay`}
            onChange={(barangay) => onChange({ ...value, barangay })}
          />
        ) : null}
        <Input
          label={`${t("personal.profile.address1")} · ${required}`}
          value={value.addressLine1}
          onChange={(event) => onChange({ ...value, addressLine1: event.target.value })}
          data-testid={`${testIdPrefix}-line1`}
        />
        <Input
          label={`${t("personal.profile.address2")} · ${optional}`}
          value={value.addressLine2}
          onChange={(event) => onChange({ ...value, addressLine2: event.target.value })}
        />
      </div>
      <div className="geography-address-form__postal">
        <PostalCodeInput
          value={value.postalCode}
          label={postalLabel}
          requirementLabel={postalRequirement}
          testId={`${testIdPrefix}-postal`}
          onChange={(postalCode) => onChange({ ...value, postalCode })}
        />
      </div>
      {showPrimary ? (
        <label className="flex items-end gap-2 pb-2 text-[length:var(--exits-text-sm)]">
          <input
            type="checkbox"
            checked={value.isPrimary}
            onChange={(event) => onChange({ ...value, isPrimary: event.target.checked })}
            data-testid={`${testIdPrefix}-primary`}
          />
          <span>{t("personal.profile.setPrimary")}</span>
        </label>
      ) : null}
    </div>
  );
}
