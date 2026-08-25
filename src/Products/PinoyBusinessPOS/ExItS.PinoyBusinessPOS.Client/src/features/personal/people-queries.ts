import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  acceptPersonalInvitation,
  createPersonalContact,
  createPersonalDebtRelationship,
  createPersonalInvitation,
  declinePersonalInvitation,
  listBorrowedRelationships,
  listLentRelationships,
  listPersonalContacts,
  listPersonalInvitations,
  listPersonalNotifications,
  markPersonalNotificationRead,
  personalPeopleKeys,
  resendPersonalInvitation,
  resolvePublicUserId,
  revokePersonalInvitation,
} from "@/api/platform/personal-people-client";
import type { CreatePersonalDebtRelationshipRequest } from "@/api/platform/personal-types";

export function usePersonalContactsQuery() {
  return useQuery({
    queryKey: personalPeopleKeys.contacts(),
    queryFn: ({ signal }) => listPersonalContacts(signal),
  });
}

export function usePersonalInvitationsQuery() {
  return useQuery({
    queryKey: personalPeopleKeys.invitations(),
    queryFn: ({ signal }) => listPersonalInvitations(signal),
  });
}

export function usePersonalNotificationsQuery() {
  return useQuery({
    queryKey: personalPeopleKeys.notifications(),
    queryFn: ({ signal }) => listPersonalNotifications(signal),
  });
}

export function usePersonalUtangSummariesQuery() {
  return useQuery({
    queryKey: [...personalPeopleKeys.all, "utang-summaries"] as const,
    queryFn: async ({ signal }) => {
      const [lent, borrowed] = await Promise.all([
        listLentRelationships(signal),
        listBorrowedRelationships(signal),
      ]);
      return { lent, borrowed };
    },
  });
}

export function useInvalidatePersonalPeople() {
  const queryClient = useQueryClient();
  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: personalPeopleKeys.contacts() }),
      queryClient.invalidateQueries({ queryKey: personalPeopleKeys.invitations() }),
      queryClient.invalidateQueries({ queryKey: personalPeopleKeys.notifications() }),
      queryClient.invalidateQueries({ queryKey: [...personalPeopleKeys.all, "utang-summaries"] }),
    ]);
}

export function useResolvePublicUserMutation() {
  return useMutation({
    mutationFn: (value: string) => resolvePublicUserId(value.trim(), "utang-people"),
  });
}

export function useCreateContactMutation() {
  const invalidate = useInvalidatePersonalPeople();
  return useMutation({
    mutationFn: (input: { displayName: string; email?: string | null }) =>
      createPersonalContact({
        displayName: input.displayName,
        email: input.email ?? null,
        phone: null,
      }),
    onSuccess: async () => {
      await invalidate();
    },
  });
}

export function useAcceptInvitationMutation() {
  const invalidate = useInvalidatePersonalPeople();
  return useMutation({
    mutationFn: (token: string) => acceptPersonalInvitation(token),
    onSuccess: async () => {
      await invalidate();
    },
  });
}

export function useDeclineInvitationMutation() {
  const invalidate = useInvalidatePersonalPeople();
  return useMutation({
    mutationFn: (token: string) => declinePersonalInvitation(token),
    onSuccess: async () => {
      await invalidate();
    },
  });
}

export function useRevokeInvitationMutation() {
  const invalidate = useInvalidatePersonalPeople();
  return useMutation({
    mutationFn: (invitationId: string) => revokePersonalInvitation(invitationId),
    onSuccess: async () => {
      await invalidate();
    },
  });
}

export function useResendInvitationMutation() {
  const invalidate = useInvalidatePersonalPeople();
  return useMutation({
    mutationFn: (invitationId: string) => resendPersonalInvitation(invitationId),
    onSuccess: async () => {
      await invalidate();
    },
  });
}

export function useMarkNotificationReadMutation() {
  const invalidate = useInvalidatePersonalPeople();
  return useMutation({
    mutationFn: (notificationId: string) => markPersonalNotificationRead(notificationId),
    onSuccess: async () => {
      await invalidate();
    },
  });
}

export function useCreateUtangWithOptionalInviteMutation() {
  const invalidate = useInvalidatePersonalPeople();
  return useMutation({
    mutationFn: async (input: {
      relationship: CreatePersonalDebtRelationshipRequest;
      inviteeContactId?: string;
      shouldInvite: boolean;
    }) => {
      const relationship = await createPersonalDebtRelationship(input.relationship);
      if (input.shouldInvite && input.inviteeContactId) {
        await createPersonalInvitation(relationship.id, input.inviteeContactId);
      }
      return relationship;
    },
    onSuccess: async () => {
      await invalidate();
    },
  });
}
