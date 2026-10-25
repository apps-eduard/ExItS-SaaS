import { useEffect, useState, type ReactNode } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { getPersonalProfile, type PersonalProfileDto } from "@/api/platform/start-business-client";
import { PersonalAvatarContextProvider } from "@/features/personal/personal-avatar-context";
import { exitsRealtimeConnection } from "@/realtime/exits-realtime-connection";
import { RealtimeEventTypes } from "@/realtime/realtime-event-types";

export function PersonalAvatarProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [photoUrl, setPhotoUrl] = useState<string | null>(null);
  const profileQuery = useQuery({
    queryKey: ["personal", "profile"],
    queryFn: ({ signal }) => getPersonalProfile(signal),
    staleTime: 60_000,
  });

  useEffect(() => {
    setPhotoUrl(profileQuery.data?.profilePhotoUrl ?? null);
  }, [profileQuery.data?.profilePhotoUrl]);

  useEffect(() => {
    if (import.meta.env.MODE === "test") {
      return;
    }

    const applyPhoto = (next: string | null) => {
      setPhotoUrl(next);
      queryClient.setQueryData<PersonalProfileDto | undefined>(["personal", "profile"], (current) =>
        current ? { ...current, profilePhotoUrl: next } : current,
      );
    };

    const unsubscribe = exitsRealtimeConnection.subscribe((envelope) => {
      if (envelope.eventType !== RealtimeEventTypes.PersonalProfilePhotoChanged) {
        return;
      }
      const next = envelope.data?.profilePhotoUrl ?? null;
      applyPhoto(next);
    });

    // Legacy dual-publish still emits ProfilePhotoUpdated on personal-profile hub;
    // shared exits-realtime PersonalProfilePhotoChanged covers multi-device sync.

    return unsubscribe;
  }, [queryClient]);

  return (
    <PersonalAvatarContextProvider value={{ photoUrl, setPhotoUrl }}>
      {children}
    </PersonalAvatarContextProvider>
  );
}
