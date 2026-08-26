import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  closeOrganization,
  reactivateOrganization,
  suspendOrganization,
  updateOrganization,
  updateOrganizationBranding,
  type UpdateOrganizationBody,
  type UpdateOrganizationBrandingBody,
} from "@/api/organizations/organization-mutations-client";
import {
  addOrganizationMember,
  changeMembershipRole,
  createOrganizationInvitation,
  reactivateMembership,
  resendOrganizationInvitation,
  revokeMembership,
  revokeOrganizationInvitation,
  suspendMembership,
  type AddMemberBody,
  type ChangeMembershipRoleBody,
  type CreateInvitationBody,
  type MembershipLifecycleBody,
} from "@/api/organizations/people-mutations-client";
import {
  grantProductAccess,
  revokeProductAccess,
  type GrantProductAccessBody,
  type RevokeProductAccessBody,
} from "@/api/organizations/product-access-client";
import {
  assignProductLocalRole,
  launchProduct,
  revokeProductLocalRole,
  type AssignProductLocalRoleBody,
  type RevokeProductLocalRoleBody,
} from "@/api/organizations/enabled-products-client";
import {
  activateOrganizationRoleDefinition,
  createOrganizationRoleDefinition,
  deactivateOrganizationRoleDefinition,
  retireOrganizationRoleDefinition,
  updateOrganizationRoleDefinition,
  type CreateOrganizationRoleBody,
  type RoleLifecycleBody,
  type UpdateOrganizationRoleBody,
} from "@/api/organizations/organization-roles-client";
import {
  organizationDetailQueryKey,
  organizationInvitationsQueryKey,
  organizationMembersQueryKey,
} from "@/features/organizations/use-organization-workspace-queries";
import { env } from "@/lib/env";

const noRetry = { retry: false as const };

function invalidateOrganizationDetail(
  queryClient: ReturnType<typeof useQueryClient>,
  organizationId: string,
) {
  return queryClient.invalidateQueries({ queryKey: organizationDetailQueryKey(organizationId) });
}

function invalidatePeopleQueries(
  queryClient: ReturnType<typeof useQueryClient>,
  organizationId: string,
) {
  return Promise.all([
    queryClient.invalidateQueries({ queryKey: ["organizations", "members", organizationId] }),
    queryClient.invalidateQueries({ queryKey: ["organizations", "invitations", organizationId] }),
  ]);
}

export function useUpdateOrganizationMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { organizationId: string; body: UpdateOrganizationBody }) =>
      updateOrganization(env.platformApiBaseUrl, input.organizationId, input.body),
    onSuccess: (organization) => invalidateOrganizationDetail(queryClient, organization.id),
  });
}

export function useUpdateOrganizationBrandingMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { organizationId: string; body: UpdateOrganizationBrandingBody }) =>
      updateOrganizationBranding(env.platformApiBaseUrl, input.organizationId, input.body),
    onSuccess: (organization) => invalidateOrganizationDetail(queryClient, organization.id),
  });
}

export function useSuspendOrganizationMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (organizationId: string) =>
      suspendOrganization(env.platformApiBaseUrl, organizationId),
    onSuccess: (organization) => invalidateOrganizationDetail(queryClient, organization.id),
  });
}

export function useReactivateOrganizationMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (organizationId: string) =>
      reactivateOrganization(env.platformApiBaseUrl, organizationId),
    onSuccess: (organization) => invalidateOrganizationDetail(queryClient, organization.id),
  });
}

export function useCloseOrganizationMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (organizationId: string) => closeOrganization(env.platformApiBaseUrl, organizationId),
    onSuccess: (organization) => invalidateOrganizationDetail(queryClient, organization.id),
  });
}

export function useCreateInvitationMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { organizationId: string; body: CreateInvitationBody }) =>
      createOrganizationInvitation(env.platformApiBaseUrl, input.organizationId, input.body),
    onSuccess: (_result, input) => invalidatePeopleQueries(queryClient, input.organizationId),
  });
}

export function useResendInvitationMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { organizationId: string; invitationId: string }) =>
      resendOrganizationInvitation(env.platformApiBaseUrl, input.invitationId),
    onSuccess: (_result, input) => invalidatePeopleQueries(queryClient, input.organizationId),
  });
}

export function useRevokeInvitationMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { organizationId: string; invitationId: string }) =>
      revokeOrganizationInvitation(env.platformApiBaseUrl, input.invitationId),
    onSuccess: (_result, input) => invalidatePeopleQueries(queryClient, input.organizationId),
  });
}

export function useAddMemberMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { organizationId: string; body: AddMemberBody }) =>
      addOrganizationMember(env.platformApiBaseUrl, input.organizationId, input.body),
    onSuccess: (_result, input) => invalidatePeopleQueries(queryClient, input.organizationId),
  });
}

export function useChangeMembershipRoleMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { organizationId: string; membershipId: string; body: ChangeMembershipRoleBody }) =>
      changeMembershipRole(env.platformApiBaseUrl, input.membershipId, input.body),
    onSuccess: (_result, input) => invalidatePeopleQueries(queryClient, input.organizationId),
  });
}

export function useSuspendMembershipMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { organizationId: string; membershipId: string; body: MembershipLifecycleBody }) =>
      suspendMembership(env.platformApiBaseUrl, input.membershipId, input.body),
    onSuccess: (_result, input) => invalidatePeopleQueries(queryClient, input.organizationId),
  });
}

export function useReactivateMembershipMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { organizationId: string; membershipId: string; body: MembershipLifecycleBody }) =>
      reactivateMembership(env.platformApiBaseUrl, input.membershipId, input.body),
    onSuccess: (_result, input) => invalidatePeopleQueries(queryClient, input.organizationId),
  });
}

export function useRevokeMembershipMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { organizationId: string; membershipId: string; body: MembershipLifecycleBody }) =>
      revokeMembership(env.platformApiBaseUrl, input.membershipId, input.body),
    onSuccess: (_result, input) => invalidatePeopleQueries(queryClient, input.organizationId),
  });
}

export function useGrantProductAccessMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { organizationId: string; body: GrantProductAccessBody }) =>
      grantProductAccess(env.platformApiBaseUrl, input.organizationId, input.body),
    onSuccess: (_result, input) =>
      queryClient.invalidateQueries({ queryKey: ["organizations", "product-access", input.organizationId] }),
  });
}

export function useRevokeProductAccessMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { organizationId: string; assignmentId: string; body: RevokeProductAccessBody }) =>
      revokeProductAccess(env.platformApiBaseUrl, input.assignmentId, input.body),
    onSuccess: (_result, input) =>
      queryClient.invalidateQueries({ queryKey: ["organizations", "product-access", input.organizationId] }),
  });
}

export function useLaunchProductMutation() {
  return useMutation({
    ...noRetry,
    mutationFn: (input: { organizationId: string; productCode: string }) =>
      launchProduct(env.platformApiBaseUrl, input.organizationId, input.productCode),
  });
}

export function useAssignProductLocalRoleMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { organizationId: string; body: AssignProductLocalRoleBody }) =>
      assignProductLocalRole(env.platformApiBaseUrl, input.organizationId, input.body),
    onSuccess: (_result, input) =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: ["organizations", "enabled-products", input.organizationId] }),
        queryClient.invalidateQueries({ queryKey: ["organizations", "product-local-roles", input.organizationId] }),
      ]),
  });
}

export function useRevokeProductLocalRoleMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { organizationId: string; grantId: string; body: RevokeProductLocalRoleBody }) =>
      revokeProductLocalRole(env.platformApiBaseUrl, input.organizationId, input.grantId, input.body),
    onSuccess: (_result, input) =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: ["organizations", "enabled-products", input.organizationId] }),
        queryClient.invalidateQueries({ queryKey: ["organizations", "product-local-roles", input.organizationId] }),
      ]),
  });
}

export function useCreateOrganizationRoleMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { organizationId: string; body: CreateOrganizationRoleBody }) =>
      createOrganizationRoleDefinition(env.platformApiBaseUrl, input.organizationId, input.body),
    onSuccess: (_result, input) =>
      queryClient.invalidateQueries({ queryKey: ["organizations", "roles", input.organizationId] }),
  });
}

export function useUpdateOrganizationRoleMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: {
      organizationId: string;
      roleId: string;
      body: UpdateOrganizationRoleBody;
    }) => updateOrganizationRoleDefinition(env.platformApiBaseUrl, input.organizationId, input.roleId, input.body),
    onSuccess: (_result, input) =>
      queryClient.invalidateQueries({ queryKey: ["organizations", "roles", input.organizationId] }),
  });
}

export function useActivateOrganizationRoleMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { organizationId: string; roleId: string; body?: RoleLifecycleBody }) =>
      activateOrganizationRoleDefinition(env.platformApiBaseUrl, input.organizationId, input.roleId, input.body),
    onSuccess: (_result, input) =>
      queryClient.invalidateQueries({ queryKey: ["organizations", "roles", input.organizationId] }),
  });
}

export function useDeactivateOrganizationRoleMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { organizationId: string; roleId: string; body?: RoleLifecycleBody }) =>
      deactivateOrganizationRoleDefinition(env.platformApiBaseUrl, input.organizationId, input.roleId, input.body),
    onSuccess: (_result, input) =>
      queryClient.invalidateQueries({ queryKey: ["organizations", "roles", input.organizationId] }),
  });
}

export function useRetireOrganizationRoleMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { organizationId: string; roleId: string; body?: RoleLifecycleBody }) =>
      retireOrganizationRoleDefinition(env.platformApiBaseUrl, input.organizationId, input.roleId, input.body),
    onSuccess: (_result, input) =>
      queryClient.invalidateQueries({ queryKey: ["organizations", "roles", input.organizationId] }),
  });
}

export { organizationMembersQueryKey, organizationInvitationsQueryKey };
