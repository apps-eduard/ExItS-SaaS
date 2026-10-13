export { AddressDisplay } from "@/platform/geography/AddressDisplay";
export { BarangaySelect } from "@/platform/geography/BarangaySelect";
export { AddressForm } from "@/platform/geography/AddressForm";
export { AdministrativeAreaSelect } from "@/platform/geography/AdministrativeAreaSelect";
export { CitySelect } from "@/platform/geography/CitySelect";
export { CountrySelect } from "@/platform/geography/CountrySelect";
export { PostalCodeInput } from "@/platform/geography/PostalCodeInput";
export {
  countryCodeForName,
  getCountryAddressConfig,
  listAdministrativeAreas,
  listBarangays,
  listCities,
  listCountries,
} from "@/platform/geography/geography-client";
export type {
  AdministrativeAreaDto,
  CountryAddressConfigDto,
  CountryDto,
  GeographyCityDto,
  SharedAddressValue,
} from "@/platform/geography/types";
export { emptySharedAddress } from "@/platform/geography/types";
export {
  useAdministrativeAreas,
  useBarangays,
  useCities,
  useCountries,
  useCountryAddressConfig,
} from "@/platform/geography/useGeography";
