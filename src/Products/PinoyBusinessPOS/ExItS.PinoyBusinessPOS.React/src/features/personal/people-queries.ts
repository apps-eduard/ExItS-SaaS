import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  acceptPersonalConnectionRequest,
  blockPersonalContact,
  createPersonalContact,
  createPersonalDebtRelationship,
  declinePersonalConnectionRequest,
  listBorrowedRelationships,
  listLentRelationships,
  listPersonalConnectionRequests,
  listPersonalContacts,
  listPersonalNotifications,
  markPersonalNotificationRead,
  personalPeopleKeys,
  requestPersonalConnection,
  resolvePublicUserId,
  revokePersonalConnectionRequest,
  unlinkPersonalContact,
  unblockPersonalContact,
} from "@/api/platform/personal-people-client";
import type { CreatePersonalContactRequest, CreatePersonalDebtRelationshipRequest } from "@/api/platform/personal-types";

export function usePersonalContactsQuery() {
  return useQuery({
    queryKey: personalPeopleKeys.contacts(),
    queryFn: ({ signal }) => listPersonalContacts(signal),
  });
}

export function usePersonalConnectionRequestsQuery() {
  return useQuery({
    queryKey: personalPeopleKeys.connections(),
    queryFn: ({ signal }) => listPersonalConnectionRequests(signal),
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
      queryClient.invalidateQueries({ queryKey: personalPeopleKeys.connections() }),
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
    mutationFn: (input: CreatePersonalContactRequest) => createPersonalContact(input),
    onSuccess: async () => {
      await invalidate();
    },
  });
}

export function useRequestConnectionMutation() {
  const invalidate = useInvalidatePersonalPeople();
  return useMutation({
    mutationFn: (contactId: string) => requestPersonalConnection(contactId),
    onSuccess: async () => {
      await invalidate();
    },
  });
}

export function useAcceptConnectionMutation() {
  const invalidate = useInvalidatePersonalPeople();
  return useMutation({
    mutationFn: (requestId: string) => acceptPersonalConnectionRequest(requestId),
    onSuccess: async () => {
      await invalidate();
    },
  });
}

export function useDeclineConnectionMutation() {
  const invalidate = useInvalidatePersonalPeople();
  return useMutation({
    mutationFn: (requestId: string) => declinePersonalConnectionRequest(requestId),
    onSuccess: async () => {
      await invalidate();
    },
  });
}

export function useRevokeConnectionMutation() {
  const invalidate = useInvalidatePersonalPeople();
  return useMutation({
    mutationFn: (requestId: string) => revokePersonalConnectionRequest(requestId),
    onSuccess: async () => {
      await invalidate();
    },
  });
}

export function useUnlinkContactMutation() {
  const invalidate = useInvalidatePersonalPeople();
  return useMutation({
    mutationFn: (contactId: string) => unlinkPersonalContact(contactId),
    onSuccess: async () => {
      await invalidate();
    },
  });
}

export function useBlockContactMutation() {
  const invalidate = useInvalidatePersonalPeople();
  return useMutation({
    mutationFn: (contactId: string) => blockPersonalContact(contactId),
    onSuccess: async () => {
      await invalidate();
    },
  });
}

export function useUnblockContactMutation() {
  const invalidate = useInvalidatePersonalPeople();
  return useMutation({
    mutationFn: (contactId: string) => unblockPersonalContact(contactId),
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

export function useCreateUtangMutation() {
  const invalidate = useInvalidatePersonalPeople();
  return useMutation({
    mutationFn: (relationship: CreatePersonalDebtRelationshipRequest) =>
      createPersonalDebtRelationship(relationship),
    onSuccess: async () => {
      await invalidate();
    },
  });
}
