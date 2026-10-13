import { platformRequest } from "@/api/platform/platform-http";
import type {
  AdministrativeAreaDto,
  CountryAddressConfigDto,
  CountryDto,
  GeographyBarangayDto,
  GeographyCityDto,
} from "@/platform/geography/types";

function asRecord(value: unknown): Record<string, unknown> {
  return typeof value === "object" && value !== null ? (value as Record<string, unknown>) : {};
}

function readString(raw: Record<string, unknown>, camel: string, pascal: string): string {
  const value = raw[camel] ?? raw[pascal];
  return value == null ? "" : String(value);
}

function readNullable(raw: Record<string, unknown>, camel: string, pascal: string): string | null {
  const value = raw[camel] ?? raw[pascal];
  return value == null ? null : String(value);
}

function readBool(raw: Record<string, unknown>, camel: string, pascal: string): boolean {
  const value = raw[camel] ?? raw[pascal];
  return value === true;
}

export function normalizeCountry(raw: unknown): CountryDto {
  const record = asRecord(raw);
  return {
    code: readString(record, "code", "Code"),
    name: readString(record, "name", "Name"),
    isActive: record.isActive === undefined && record.IsActive === undefined
      ? true
      : readBool(record, "isActive", "IsActive"),
  };
}

export function normalizeCountryAddressConfig(raw: unknown): CountryAddressConfigDto {
  const record = asRecord(raw);
  return {
    countryCode: readString(record, "countryCode", "CountryCode"),
    administrativeAreaLabel: readString(record, "administrativeAreaLabel", "AdministrativeAreaLabel"),
    cityLabel: readString(record, "cityLabel", "CityLabel"),
    postalCodeLabel: readString(record, "postalCodeLabel", "PostalCodeLabel"),
    requiresAdministrativeArea: readBool(record, "requiresAdministrativeArea", "RequiresAdministrativeArea"),
    requiresCity: readBool(record, "requiresCity", "RequiresCity"),
    requiresPostalCode: readBool(record, "requiresPostalCode", "RequiresPostalCode"),
    supportsBarangay: readBool(record, "supportsBarangay", "SupportsBarangay"),
  };
}

export function normalizeAdministrativeArea(raw: unknown): AdministrativeAreaDto {
  const record = asRecord(raw);
  return {
    id: readString(record, "id", "Id"),
    countryCode: readString(record, "countryCode", "CountryCode"),
    code: readString(record, "code", "Code"),
    name: readString(record, "name", "Name"),
    type: readString(record, "type", "Type"),
    parentId: readNullable(record, "parentId", "ParentId"),
    isActive: readBool(record, "isActive", "IsActive"),
  };
}

export function normalizeGeographyBarangay(raw: unknown): GeographyBarangayDto {
  const record = asRecord(raw);
  return {
    id: readString(record, "id", "Id"),
    cityId: readString(record, "cityId", "CityId"),
    code: readString(record, "code", "Code"),
    name: readString(record, "name", "Name"),
  };
}

export function normalizeGeographyCity(raw: unknown): GeographyCityDto {
  const record = asRecord(raw);
  return {
    id: readString(record, "id", "Id"),
    countryCode: readString(record, "countryCode", "CountryCode"),
    administrativeAreaId: readString(record, "administrativeAreaId", "AdministrativeAreaId"),
    code: readString(record, "code", "Code"),
    name: readString(record, "name", "Name"),
    isActive: readBool(record, "isActive", "IsActive"),
  };
}

export async function listCountries(signal?: AbortSignal): Promise<CountryDto[]> {
  const body = await platformRequest<unknown>({
    path: "/api/v1/platform/reference/geography/countries",
    signal,
  });
  const items = Array.isArray(body) ? body : [];
  return items.map(normalizeCountry).filter((country) => country.code.length > 0 && country.isActive);
}

export async function getCountryAddressConfig(countryCode: string, signal?: AbortSignal): Promise<CountryAddressConfigDto> {
  const body = await platformRequest<unknown>({
    path: `/api/v1/platform/reference/geography/countries/${encodeURIComponent(countryCode)}/config`,
    signal,
  });
  return normalizeCountryAddressConfig(body);
}

export async function listAdministrativeAreas(countryCode: string, signal?: AbortSignal): Promise<AdministrativeAreaDto[]> {
  const body = await platformRequest<unknown>({
    path: `/api/v1/platform/reference/geography/countries/${encodeURIComponent(countryCode)}/areas`,
    signal,
  });
  const items = Array.isArray(body) ? body : [];
  return items.map(normalizeAdministrativeArea).filter((area) => area.id.length > 0);
}

export async function listBarangays(cityId: string, signal?: AbortSignal): Promise<GeographyBarangayDto[]> {
  const body = await platformRequest<unknown>({
    path: `/api/v1/platform/reference/geography/cities/${encodeURIComponent(cityId)}/barangays`,
    signal,
  });
  const items = Array.isArray(body) ? body : [];
  return items.map(normalizeGeographyBarangay).filter((barangay) => barangay.code.length > 0 && barangay.name.length > 0);
}

export async function listCities(areaId: string, signal?: AbortSignal): Promise<GeographyCityDto[]> {
  const body = await platformRequest<unknown>({
    path: `/api/v1/platform/reference/geography/areas/${encodeURIComponent(areaId)}/cities`,
    signal,
  });
  const items = Array.isArray(body) ? body : [];
  return items.map(normalizeGeographyCity).filter((city) => city.id.length > 0);
}

export function countryCodeForName(countries: readonly CountryDto[], countryOrCode: string): string {
  const key = countryOrCode.trim();
  const byCode = countries.find((country) => country.code.localeCompare(key, undefined, { sensitivity: "accent" }) === 0);
  if (byCode) {
    return byCode.code;
  }
  const byName = countries.find((country) => country.name.localeCompare(key, undefined, { sensitivity: "accent" }) === 0);
  return byName?.code ?? "";
}
