import { platformRequest } from "@/api/platform/platform-http";

export type PhilippineLocalityDto = {
  psgcCode: string;
  name: string;
  localityType: string;
  regionCode: string;
  regionName: string;
  provinceCode: string | null;
  provinceName: string | null;
  displayLabel: string;
};

export type PhilippineRegionDto = {
  regionCode: string;
  regionName: string;
};

export type PhilippineProvinceDto = {
  provinceCode: string;
  provinceName: string;
};

function asRecord(value: unknown): Record<string, unknown> {
  return typeof value === "object" && value !== null ? (value as Record<string, unknown>) : {};
}

function readString(raw: Record<string, unknown>, camel: string, pascal: string): string | null {
  const value = raw[camel] ?? raw[pascal];
  if (value == null) {
    return null;
  }
  return String(value);
}

export function normalizePhilippineLocality(raw: unknown): PhilippineLocalityDto {
  const r = asRecord(raw);
  return {
    psgcCode: String(r.psgcCode ?? r.PsgcCode ?? ""),
    name: String(r.name ?? r.Name ?? ""),
    localityType: String(r.localityType ?? r.LocalityType ?? ""),
    regionCode: String(r.regionCode ?? r.RegionCode ?? ""),
    regionName: String(r.regionName ?? r.RegionName ?? ""),
    provinceCode: readString(r, "provinceCode", "ProvinceCode"),
    provinceName: readString(r, "provinceName", "ProvinceName"),
    displayLabel: String(r.displayLabel ?? r.DisplayLabel ?? r.name ?? r.Name ?? ""),
  };
}

export function normalizePhilippineRegion(raw: unknown): PhilippineRegionDto {
  const r = asRecord(raw);
  return {
    regionCode: String(r.regionCode ?? r.RegionCode ?? ""),
    regionName: String(r.regionName ?? r.RegionName ?? ""),
  };
}

export async function searchPhilippineLocalities(
  query: string,
  limit = 20,
  signal?: AbortSignal,
): Promise<PhilippineLocalityDto[]> {
  const params = new URLSearchParams();
  params.set("query", query);
  params.set("limit", String(limit));
  const body = await platformRequest<unknown>({
    path: `/api/v1/platform/reference/ph/localities?${params.toString()}`,
    signal,
  });
  const items = Array.isArray(body) ? body : [];
  return items.map(normalizePhilippineLocality).filter((x) => x.psgcCode.length > 0);
}

export async function listPhilippineRegions(signal?: AbortSignal): Promise<PhilippineRegionDto[]> {
  const body = await platformRequest<unknown>({
    path: "/api/v1/platform/reference/ph/regions",
    signal,
  });
  const items = Array.isArray(body) ? body : [];
  return items.map(normalizePhilippineRegion).filter((x) => x.regionCode.length > 0);
}

function normalizePhilippineProvince(raw: unknown): PhilippineProvinceDto {
  const r = asRecord(raw);
  return {
    provinceCode: String(r.provinceCode ?? r.ProvinceCode ?? ""),
    provinceName: String(r.provinceName ?? r.ProvinceName ?? ""),
  };
}

export async function listPhilippineProvinces(signal?: AbortSignal): Promise<PhilippineProvinceDto[]> {
  const body = await platformRequest<unknown>({
    path: "/api/v1/platform/reference/ph/provinces",
    signal,
  });
  const items = Array.isArray(body) ? body : [];
  return items.map(normalizePhilippineProvince).filter((item) => item.provinceCode.length > 0);
}

export async function listPhilippineLocalitiesByProvince(
  provinceCode: string,
  signal?: AbortSignal,
): Promise<PhilippineLocalityDto[]> {
  const encoded = encodeURIComponent(provinceCode);
  const body = await platformRequest<unknown>({
    path: `/api/v1/platform/reference/ph/provinces/${encoded}/localities`,
    signal,
  });
  const items = Array.isArray(body) ? body : [];
  return items.map(normalizePhilippineLocality).filter((item) => item.psgcCode.length > 0);
}

export async function listPhilippineLocalitiesByRegion(
  regionCode: string,
  signal?: AbortSignal,
): Promise<PhilippineLocalityDto[]> {
  const encoded = encodeURIComponent(regionCode);
  const body = await platformRequest<unknown>({
    path: `/api/v1/platform/reference/ph/regions/${encoded}/localities`,
    signal,
  });
  const items = Array.isArray(body) ? body : [];
  return items.map(normalizePhilippineLocality).filter((x) => x.psgcCode.length > 0);
}
