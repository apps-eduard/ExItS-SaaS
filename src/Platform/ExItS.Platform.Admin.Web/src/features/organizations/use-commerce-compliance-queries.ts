import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  getOrganizationComplianceStatus,
  getOrganizationOnlineSupplierPaymentsCapability,
  transitionOrganizationCompliance,
  transitionOrganizationOnlineSupplierPayments,
  type BirComplianceStatus,
  type OnlineSupplierPaymentsStatus,
} from "@/api/organizations/commerce-compliance-client";
import { env } from "@/lib/env";

const noRetry = { retry: false as const };

export const onlineSupplierPaymentsQueryKey = (organizationId: string) =>
  ["organizations", "online-supplier-payments", organizationId] as const;

export const organizationComplianceStatusQueryKey = (organizationId: string) =>
  ["organizations", "compliance-status", organizationId] as const;

export function useOnlineSupplierPaymentsQuery(organizationId: string | null) {
  return useQuery({
    queryKey: onlineSupplierPaymentsQueryKey(organizationId ?? ""),
    enabled: organizationId != null,
    queryFn: ({ signal }) =>
      getOrganizationOnlineSupplierPaymentsCapability(
        env.platformApiBaseUrl,
        organizationId!,
        signal,
      ),
  });
}

export function useOrganizationComplianceStatusQuery(organizationId: string | null) {
  return useQuery({
    queryKey: organizationComplianceStatusQueryKey(organizationId ?? ""),
    enabled: organizationId != null,
    queryFn: ({ signal }) =>
      getOrganizationComplianceStatus(env.platformApiBaseUrl, organizationId!, signal),
  });
}

export function useTransitionOnlineSupplierPaymentsMutation(organizationId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { status: OnlineSupplierPaymentsStatus; reason?: string }) =>
      transitionOrganizationOnlineSupplierPayments(env.platformApiBaseUrl, organizationId, input),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: onlineSupplierPaymentsQueryKey(organizationId) }),
  });
}

export function useTransitionBirComplianceMutation(organizationId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (targetStatus: BirComplianceStatus) =>
      transitionOrganizationCompliance(env.platformApiBaseUrl, organizationId, targetStatus),
    onSuccess: () =>
      queryClient.invalidateQueries({
        queryKey: organizationComplianceStatusQueryKey(organizationId),
      }),
  });
}
