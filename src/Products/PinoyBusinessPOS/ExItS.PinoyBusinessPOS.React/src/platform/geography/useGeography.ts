import { useQuery } from "@tanstack/react-query";
import {
  getCountryAddressConfig,
  listAdministrativeAreas,
  listBarangays,
  listCities,
  listCountries,
} from "@/platform/geography/geography-client";
import { GEOGRAPHY_STALE_TIME } from "@/platform/geography/types";

export function useCountries() {
  return useQuery({
    queryKey: ["platform", "geography", "countries"],
    queryFn: ({ signal }) => listCountries(signal),
    staleTime: GEOGRAPHY_STALE_TIME,
  });
}

export function useCountryAddressConfig(countryCode: string) {
  return useQuery({
    queryKey: ["platform", "geography", "config", countryCode],
    queryFn: ({ signal }) => getCountryAddressConfig(countryCode, signal),
    enabled: countryCode.trim().length > 0,
    staleTime: GEOGRAPHY_STALE_TIME,
  });
}

export function useAdministrativeAreas(countryCode: string) {
  return useQuery({
    queryKey: ["platform", "geography", "areas", countryCode],
    queryFn: ({ signal }) => listAdministrativeAreas(countryCode, signal),
    enabled: countryCode.trim().length > 0,
    staleTime: GEOGRAPHY_STALE_TIME,
  });
}

export function useBarangays(cityId: string) {
  return useQuery({
    queryKey: ["platform", "geography", "barangays", cityId],
    queryFn: ({ signal }) => listBarangays(cityId, signal),
    enabled: cityId.trim().length > 0,
    staleTime: GEOGRAPHY_STALE_TIME,
  });
}

export function useCities(areaId: string) {
  return useQuery({
    queryKey: ["platform", "geography", "cities", areaId],
    queryFn: ({ signal }) => listCities(areaId, signal),
    enabled: areaId.trim().length > 0,
    staleTime: GEOGRAPHY_STALE_TIME,
  });
}
