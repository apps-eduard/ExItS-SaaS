export type CountryDto = {
  code: string;
  name: string;
  isActive: boolean;
};

export type CountryAddressConfigDto = {
  countryCode: string;
  administrativeAreaLabel: string;
  cityLabel: string;
  postalCodeLabel: string;
  requiresAdministrativeArea: boolean;
  requiresCity: boolean;
  requiresPostalCode: boolean;
  supportsBarangay: boolean;
};

export type AdministrativeAreaDto = {
  id: string;
  countryCode: string;
  code: string;
  name: string;
  type: string;
  parentId: string | null;
  isActive: boolean;
};

export type GeographyBarangayDto = {
  id: string;
  cityId: string;
  code: string;
  name: string;
};

export type GeographyCityDto = {
  id: string;
  countryCode: string;
  administrativeAreaId: string;
  code: string;
  name: string;
  isActive: boolean;
};

export type SharedAddressValue = {
  addressType: string;
  countryCode: string;
  countryName: string;
  administrativeAreaCode: string;
  administrativeAreaName: string;
  cityCode: string;
  cityName: string;
  barangay: string;
  addressLine1: string;
  addressLine2: string;
  postalCode: string;
  isPrimary: boolean;
};

export const GEOGRAPHY_STALE_TIME = 24 * 60 * 60 * 1000;

export function emptySharedAddress(isPrimary: boolean): SharedAddressValue {
  return {
    addressType: "Home",
    countryCode: "PH",
    countryName: "Philippines",
    administrativeAreaCode: "",
    administrativeAreaName: "",
    cityCode: "",
    cityName: "",
    barangay: "",
    addressLine1: "",
    addressLine2: "",
    postalCode: "",
    isPrimary,
  };
}
