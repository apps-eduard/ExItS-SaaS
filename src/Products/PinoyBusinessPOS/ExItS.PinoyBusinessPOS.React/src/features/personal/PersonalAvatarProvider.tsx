import { useEffect, useState, type ReactNode } from "react";
import { HubConnectionBuilder } from "@microsoft/signalr";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { platformApiUrl } from "@/api/platform/browser-session";
import { getPersonalProfile, type PersonalProfileDto } from "@/api/platform/start-business-client";
import { PersonalAvatarContextProvider } from "@/features/personal/personal-avatar-context";

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
    const connection = new HubConnectionBuilder()
      .withUrl(platformApiUrl("/hubs/personal-profile"), { withCredentials: true })
      .withAutomaticReconnect()
      .build();
    connection.on("ProfilePhotoUpdated", (payload: { profilePhotoUrl?: string | null }) => {
      const next = payload?.profilePhotoUrl ?? null;
      setPhotoUrl(next);
      queryClient.setQueryData<PersonalProfileDto | undefined>(["personal", "profile"], (current) =>
        current ? { ...current, profilePhotoUrl: next } : current,
      );
    });
    void connection.start().catch(() => undefined);
    return () => {
      void connection.stop();
    };
  }, [queryClient]);

  return (
    <PersonalAvatarContextProvider value={{ photoUrl, setPhotoUrl }}>
      {children}
    </PersonalAvatarContextProvider>
  );
}
